/**
 * glossary_lookup — graph extensions (appears_in_code cache + scanner) +
 * bulk `terms` path (TECH-315) + envelope wrap assertions (TECH-400).
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";
import {
  clearGlossaryGraphCaches,
  lookupOneTerm,
  scanCodeAppearances,
} from "../../src/tools/glossary-lookup.js";
import { parseGlossary } from "../../src/parser/glossary-parser.js";
import { wrapTool } from "../../src/envelope.js";

/**
 * Scaffold a fake repo with a single `Assets/Scripts/Thing.cs` file. Returns
 * the temp dir path so the test can use it as `repoRoot`.
 */
function withFakeRepo(
  csContent: string,
  body: (repoRoot: string) => void,
): void {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "glossary-scan-"));
  const scriptsDir = path.join(repoRoot, "Assets", "Scripts");
  fs.mkdirSync(scriptsDir, { recursive: true });
  fs.writeFileSync(
    path.join(scriptsDir, "Thing.cs"),
    csContent,
    "utf8",
  );
  try {
    clearGlossaryGraphCaches();
    body(repoRoot);
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
}

test("scanCodeAppearances finds term mentions in .cs files", () => {
  withFakeRepo(
    `
public class Thing
{
    // references the HeightMap field
    int[,] HeightMap;
    void Use() { var h = HeightMap[0, 0]; }
}
`,
    (repoRoot) => {
      const hits = scanCodeAppearances("HeightMap", repoRoot);
      assert.ok(hits.length >= 2, "expected at least 2 hits");
      for (const hit of hits) {
        assert.ok(hit.file.endsWith("Thing.cs"));
        assert.ok(hit.line > 0);
      }
    },
  );
});

test("scanCodeAppearances returns empty when term absent", () => {
  withFakeRepo(
    `
public class Thing
{
    void Noop() { }
}
`,
    (repoRoot) => {
      const hits = scanCodeAppearances("UrbanCentroid", repoRoot);
      assert.equal(hits.length, 0);
    },
  );
});

test("scanCodeAppearances caches per-term results across calls", () => {
  withFakeRepo(
    `
public class Thing
{
    int HeightMap;
}
`,
    (repoRoot) => {
      const first = scanCodeAppearances("HeightMap", repoRoot);
      // Delete the file; a fresh scan would now return empty, but the cache
      // should still give us the original answer.
      const scriptPath = path.join(repoRoot, "Assets/Scripts/Thing.cs");
      fs.rmSync(scriptPath);
      const second = scanCodeAppearances("HeightMap", repoRoot);
      assert.deepEqual(second, first);
    },
  );
});

test("scanCodeAppearances handles missing Assets/Scripts gracefully", () => {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "glossary-empty-"));
  try {
    clearGlossaryGraphCaches();
    const hits = scanCodeAppearances("Anything", repoRoot);
    assert.equal(hits.length, 0);
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// TECH-315: bulk `terms` path tests — 4 cases per §2.1 Goals 1-4 of spec.
//
// Tests exercise the pure `lookupOneTerm` helper plus an inline aggregator
// that mirrors the handler's bulk-path code (fan out → results/errors →
// meta.partial). Approach mirrors existing `glossary-discover.test.ts` +
// `backlog-issue.test.ts` pattern: direct helper testing against real data.
// ---------------------------------------------------------------------------

const __dirname_bulk = path.dirname(fileURLToPath(import.meta.url));
const bulkRepoRoot = path.resolve(__dirname_bulk, "../../../../");
const bulkGlossaryPath = path.join(
  bulkRepoRoot,
  "ia",
  "specs",
  "glossary.md",
);

/**
 * Inline aggregator mirroring handler bulk-path logic. Returns the public
 * partial-result shape so tests pin both the per-term outcome routing AND
 * the counts exposed in `meta.partial`.
 */
function bulkAggregate(
  terms: string[],
  entries: ReturnType<typeof parseGlossary>,
  repoRoot: string,
): {
  results: Record<string, Record<string, unknown>>;
  errors: Record<string, { code: string; message: string }>;
  meta: { partial: { succeeded: number; failed: number } };
} {
  const results: Record<string, Record<string, unknown>> = {};
  const errors: Record<string, { code: string; message: string }> = {};
  for (const rawBulk of terms) {
    const outcome = lookupOneTerm(rawBulk.trim(), entries, repoRoot);
    if (outcome.kind === "hit") {
      results[rawBulk] = outcome.payload;
    } else {
      errors[rawBulk] = outcome.error;
    }
  }
  return {
    results,
    errors,
    meta: {
      partial: {
        succeeded: Object.keys(results).length,
        failed: Object.keys(errors).length,
      },
    },
  };
}

test(
  "bulk terms — happy path (all found)",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const out = bulkAggregate(
      ["HeightMap", "Zone S"],
      entries,
      bulkRepoRoot,
    );
    assert.ok(out.results["HeightMap"], "HeightMap in results");
    assert.ok(out.results["Zone S"], "Zone S in results");
    assert.deepEqual(out.errors, {});
    assert.deepEqual(out.meta.partial, { succeeded: 2, failed: 0 });
  },
);

test(
  "bulk terms — partial failure (mix hit + miss)",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const out = bulkAggregate(
      ["HeightMap", "xyznonexistentterm"],
      entries,
      bulkRepoRoot,
    );
    assert.ok(out.results["HeightMap"], "HeightMap in results");
    assert.equal(
      out.results["xyznonexistentterm"],
      undefined,
      "missing term not in results",
    );
    assert.ok(
      out.errors["xyznonexistentterm"],
      "missing term recorded in errors",
    );
    assert.equal(
      out.errors["xyznonexistentterm"]!.code,
      "term_not_found",
    );
    assert.deepEqual(out.meta.partial, { succeeded: 1, failed: 1 });
  },
);

test(
  "single-term back-compat — lookupOneTerm returns hit payload unchanged",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const outcome = lookupOneTerm("HeightMap", entries, bulkRepoRoot);
    assert.equal(outcome.kind, "hit");
    if (outcome.kind === "hit") {
      // Existing shape keys: term, definition, specReference, category,
      // matchType, related, cited_in, appears_in_code. No `results` /
      // `errors` wrapping on single path.
      assert.equal(outcome.payload.term, "HeightMap");
      assert.equal(outcome.payload.matchType, "exact");
      assert.ok("definition" in outcome.payload);
      assert.ok("specReference" in outcome.payload);
      assert.ok("related" in outcome.payload);
      assert.ok("cited_in" in outcome.payload);
      assert.ok("appears_in_code" in outcome.payload);
      assert.ok(
        !("results" in outcome.payload),
        "single path must not wrap in results",
      );
      assert.ok(
        !("errors" in outcome.payload),
        "single path must not wrap in errors",
      );
    }
  },
);

test("bulk terms — empty array returns empty results + zero counts", () => {
  // No filesystem dep for this edge: parseGlossary only called if entries
  // needed, and with empty input no lookups run. Use an empty entries array
  // to keep this test hermetic.
  const out = bulkAggregate([], [], bulkRepoRoot);
  assert.deepEqual(out.results, {});
  assert.deepEqual(out.errors, {});
  assert.deepEqual(out.meta.partial, { succeeded: 0, failed: 0 });
});

// ---------------------------------------------------------------------------
// TECH-400: envelope wrap shape assertions
// ---------------------------------------------------------------------------

test(
  "envelope — single hit → { ok:true, payload } with term fields",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  async () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const envelope = await wrapTool(async () => {
      const outcome = lookupOneTerm("HeightMap", entries, bulkRepoRoot);
      if (outcome.kind === "hit") return outcome.payload;
      throw { code: "term_not_found" as const, message: "miss" };
    })({});
    assert.equal(envelope.ok, true);
    if (envelope.ok) {
      assert.equal((envelope.payload as Record<string, unknown>).term, "HeightMap");
    }
  },
);

test(
  "envelope — single miss → { ok:false, error:{ code:'term_not_found', details } }",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  async () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const available_terms = [...new Set(entries.map((e) => e.term))].sort();
    const envelope = await wrapTool(async () => {
      throw {
        code: "term_not_found" as const,
        message: "No glossary entry for 'xyznonexistent'.",
        details: { available_terms, suggestions: [] },
      };
    })({});
    assert.equal(envelope.ok, false);
    if (!envelope.ok) {
      assert.equal(envelope.error.code, "term_not_found");
      assert.ok(envelope.error.details);
      const d = envelope.error.details as Record<string, unknown>;
      assert.ok(Array.isArray(d.available_terms), "details.available_terms is array");
      assert.ok(Array.isArray(d.suggestions), "details.suggestions is array");
    }
  },
);

test(
  "envelope — bulk partial → { ok:true, payload:{ results, errors }, meta:{ partial } }",
  { skip: !fs.existsSync(bulkGlossaryPath) },
  async () => {
    const entries = parseGlossary(bulkGlossaryPath);
    const envelope = await wrapTool(async () => {
      const results: Record<string, Record<string, unknown>> = {};
      const errors: Record<string, { code: string; message: string }> = {};
      for (const rawBulk of ["HeightMap", "xyznonexistentterm"]) {
        const outcome = lookupOneTerm(rawBulk.trim(), entries, bulkRepoRoot);
        if (outcome.kind === "hit") {
          results[rawBulk] = outcome.payload;
        } else {
          errors[rawBulk] = outcome.error;
        }
      }
      return {
        ok: true as const,
        payload: { results, errors },
        meta: { partial: { succeeded: Object.keys(results).length, failed: Object.keys(errors).length } },
      };
    })({});
    assert.equal(envelope.ok, true);
    if (envelope.ok) {
      const p = envelope.payload as Record<string, unknown>;
      assert.ok(p.results, "payload.results present");
      assert.ok(p.errors, "payload.errors present");
      assert.ok(envelope.meta?.partial, "meta.partial present");
      assert.equal(envelope.meta?.partial?.succeeded, 1);
      assert.equal(envelope.meta?.partial?.failed, 1);
    }
  },
);

test("envelope — invalid_input (mutually exclusive) → { ok:false, error:{ code:'invalid_input' } }", async () => {
  const envelope = await wrapTool(async () => {
    throw {
      code: "invalid_input" as const,
      message: "Pass exactly one of `term` or `terms`; they are mutually exclusive.",
    };
  })({});
  assert.equal(envelope.ok, false);
  if (!envelope.ok) {
    assert.equal(envelope.error.code, "invalid_input");
  }
});
