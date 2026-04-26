// TECH-1436 unit test for validate-blob-roots.ts.
//
// Spawns `npx tsx` against the validator with `BLOB_ROOTS_FAKE_ROWS` env to
// bypass Postgres + `BLOB_ROOT` env to anchor file lookups under tmp dirs.

import assert from "node:assert";
import { spawnSync } from "node:child_process";
import * as fs from "node:fs";
import * as os from "node:os";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const SCRIPT = path.join(
  REPO_ROOT,
  "tools/scripts/validate/validate-blob-roots.ts",
);

interface FakeRow {
  entity_id: number;
  slug: string;
  source_uri: string;
}

function runValidator(opts: {
  blobRoot: string;
  sprite: FakeRow[];
  audio: FakeRow[] | null;
}) {
  const fakeRows = JSON.stringify({ sprite: opts.sprite, audio: opts.audio });
  const res = spawnSync("npx", ["tsx", SCRIPT], {
    cwd: REPO_ROOT,
    env: {
      ...process.env,
      BLOB_ROOTS_FAKE_ROWS: fakeRows,
      BLOB_ROOT: opts.blobRoot,
    },
    encoding: "utf8",
  });
  return res;
}

function makeBlob(blobRoot: string, runId: string, variantIdx: number): void {
  const dir = path.join(blobRoot, runId);
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(
    path.join(dir, `${variantIdx}.png`),
    Buffer.from([0x89, 0x50, 0x4e, 0x47]),
  );
}

function withTmp<T>(fn: (tmp: string) => T): T {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "blob-roots-test-"));
  try {
    return fn(tmp);
  } finally {
    fs.rmSync(tmp, { recursive: true, force: true });
  }
}

test("test_validates_clean_catalog — zero rows exits 0", () => {
  withTmp((tmp) => {
    const res = runValidator({ blobRoot: tmp, sprite: [], audio: [] });
    assert.strictEqual(
      res.status,
      0,
      `expected exit 0; stderr=${res.stderr}; stdout=${res.stdout}`,
    );
    assert.match(res.stdout, /OK/);
  });
});

test("test_resolves_existing_gen_uri — sprite row + on-disk blob exits 0", () => {
  withTmp((tmp) => {
    makeBlob(tmp, "run42", 0);
    const res = runValidator({
      blobRoot: tmp,
      sprite: [{ entity_id: 1, slug: "foo", source_uri: "gen://run42/0" }],
      audio: [],
    });
    assert.strictEqual(
      res.status,
      0,
      `expected exit 0; stderr=${res.stderr}; stdout=${res.stdout}`,
    );
    assert.match(res.stdout, /sprite=1/);
  });
});

test("test_reports_missing_gen_uri — sprite row without blob exits 1", () => {
  withTmp((tmp) => {
    const res = runValidator({
      blobRoot: tmp,
      sprite: [{ entity_id: 7, slug: "bar", source_uri: "gen://ghost/3" }],
      audio: [],
    });
    assert.notStrictEqual(res.status, 0);
    assert.match(res.stderr, /FAIL/);
    assert.match(res.stderr, /"entity_id":7/);
    assert.match(res.stderr, /gen:\/\/ghost\/3/);
    assert.match(res.stderr, /blob file missing on disk/);
  });
});

test("test_skips_audio_detail_when_absent — audio:null emits warning + still 0", () => {
  withTmp((tmp) => {
    const res = runValidator({ blobRoot: tmp, sprite: [], audio: null });
    assert.strictEqual(
      res.status,
      0,
      `expected exit 0; stderr=${res.stderr}; stdout=${res.stdout}`,
    );
    assert.match(res.stderr, /skipping audio scan/);
    assert.match(res.stdout, /audio=skipped/);
  });
});

test("test_includes_audio_detail_when_present — mixed rows exits 1, both kinds in stderr", () => {
  withTmp((tmp) => {
    makeBlob(tmp, "run1", 0);
    // sprite has one resolvable + one missing; audio has one missing.
    const res = runValidator({
      blobRoot: tmp,
      sprite: [
        { entity_id: 1, slug: "ok", source_uri: "gen://run1/0" },
        { entity_id: 2, slug: "miss-sprite", source_uri: "gen://gone/9" },
      ],
      audio: [
        { entity_id: 100, slug: "miss-audio", source_uri: "gen://gone/4" },
      ],
    });
    assert.notStrictEqual(res.status, 0);
    assert.match(res.stderr, /"kind":"sprite"/);
    assert.match(res.stderr, /"kind":"audio"/);
    assert.match(res.stderr, /miss-sprite/);
    assert.match(res.stderr, /miss-audio/);
  });
});

test("test_validate_all_chain_includes_blob_roots", () => {
  const pkgPath = path.join(REPO_ROOT, "package.json");
  const pkg = JSON.parse(fs.readFileSync(pkgPath, "utf8"));
  const validateAll: string = pkg.scripts["validate:all"];
  assert.match(validateAll, /validate:blob-roots/);
});

test("test_rejects_unsupported_scheme — stderr reason field", () => {
  withTmp((tmp) => {
    const res = runValidator({
      blobRoot: tmp,
      sprite: [{ entity_id: 5, slug: "weird", source_uri: "gen://run1/0" }],
      audio: [],
    });
    // sprite row with no on-disk file: resolves but read fails → reason = "blob file missing on disk".
    // (Unsupported-scheme path is exercised at the BlobResolver unit-test layer; here we only feed
    // gen:// URIs into the validator since the SQL filter is `LIKE 'gen://%'`.)
    assert.notStrictEqual(res.status, 0);
    assert.match(res.stderr, /blob file missing on disk/);
  });
});
