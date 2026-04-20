/**
 * Manifest cache tests (TECH-496 / B8) — yaml-first lookup + dir-mtime invalidation.
 */
import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import {
  loadYamlIssue,
  resetManifestCache,
  yamlBacklogExists,
} from "../../src/parser/backlog-yaml-loader.js";

function makeFixtureRepo(entries: {
  open?: Record<string, string>;
  archive?: Record<string, string>;
}): string {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "manifest-cache-"));
  const openDir = path.join(root, "ia/backlog");
  const archDir = path.join(root, "ia/backlog-archive");
  fs.mkdirSync(openDir, { recursive: true });
  fs.mkdirSync(archDir, { recursive: true });
  for (const [id, body] of Object.entries(entries.open ?? {})) {
    fs.writeFileSync(path.join(openDir, `${id}.yaml`), body, "utf8");
  }
  for (const [id, body] of Object.entries(entries.archive ?? {})) {
    fs.writeFileSync(path.join(archDir, `${id}.yaml`), body, "utf8");
  }
  return root;
}

const MIN_YAML = (id: string, status: string) =>
  `id: ${id}\ntype: TECH\ntitle: fixture\nstatus: ${status}\nsection: test\n`;

test("manifest-cache: loads open yaml", () => {
  resetManifestCache();
  const root = makeFixtureRepo({ open: { "TECH-900": MIN_YAML("TECH-900", "open") } });
  assert.equal(yamlBacklogExists(root), true);
  const hit = loadYamlIssue(root, "TECH-900");
  assert.ok(hit);
  assert.equal(hit.issue_id, "TECH-900");
  assert.equal(hit.status, "open");
});

test("manifest-cache: archived yaml resolvable", () => {
  resetManifestCache();
  const root = makeFixtureRepo({
    archive: { "TECH-901": MIN_YAML("TECH-901", "closed") },
  });
  const hit = loadYamlIssue(root, "TECH-901");
  assert.ok(hit);
  assert.equal(hit.status, "completed");
});

test("manifest-cache: open wins over archive on id collision", () => {
  resetManifestCache();
  const root = makeFixtureRepo({
    open: { "TECH-902": MIN_YAML("TECH-902", "open") },
    archive: { "TECH-902": MIN_YAML("TECH-902", "closed") },
  });
  const hit = loadYamlIssue(root, "TECH-902");
  assert.ok(hit);
  assert.equal(hit.status, "open");
});

test("manifest-cache: unknown id → null", () => {
  resetManifestCache();
  const root = makeFixtureRepo({ open: { "TECH-903": MIN_YAML("TECH-903", "open") } });
  assert.equal(loadYamlIssue(root, "TECH-NOPE"), null);
});

test("manifest-cache: dir mtime change invalidates map", async () => {
  resetManifestCache();
  const root = makeFixtureRepo({
    open: { "TECH-910": MIN_YAML("TECH-910", "open") },
  });
  assert.ok(loadYamlIssue(root, "TECH-910"));
  assert.equal(loadYamlIssue(root, "TECH-911"), null);

  // Add a new yaml → dir mtime changes → next call sees it.
  await new Promise((r) => setTimeout(r, 20));
  fs.writeFileSync(
    path.join(root, "ia/backlog/TECH-911.yaml"),
    MIN_YAML("TECH-911", "open"),
    "utf8",
  );
  assert.ok(loadYamlIssue(root, "TECH-911"));
});
