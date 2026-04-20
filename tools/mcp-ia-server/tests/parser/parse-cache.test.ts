/**
 * parse-cache unit tests (TECH-495 / B4) — mtime-keyed hit/miss + write-through.
 */
import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import {
  readCached,
  writeCached,
  flushParseCache,
  resetParseCacheState,
} from "../../src/parser/parse-cache.js";
import type { ParsedDocument } from "../../src/parser/types.js";

function makeTmpRepoRoot(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), "parse-cache-test-"));
}

function sampleDoc(filePath: string): ParsedDocument {
  return {
    filePath,
    fileName: path.basename(filePath),
    frontmatter: null,
    headings: [
      {
        depth: 1,
        title: "root",
        sectionId: "root",
        lineStart: 1,
        lineEnd: 1,
        children: [],
      },
    ],
    lineCount: 1,
  };
}

test("parse-cache: miss returns null when unpopulated", () => {
  resetParseCacheState();
  const repoRoot = makeTmpRepoRoot();
  const abs = path.join(repoRoot, "fake.md");
  const out = readCached(repoRoot, abs, 123);
  assert.equal(out, null);
});

test("parse-cache: write → read round-trip at same mtime returns same doc", () => {
  resetParseCacheState();
  const repoRoot = makeTmpRepoRoot();
  const abs = path.join(repoRoot, "doc.md");
  const doc = sampleDoc(abs);
  writeCached(repoRoot, abs, 999, doc);
  flushParseCache(repoRoot);
  // Reset in-memory to force disk re-load.
  resetParseCacheState();
  const hit = readCached(repoRoot, abs, 999);
  assert.ok(hit, "expected cache hit");
  assert.equal(hit.fileName, "doc.md");
});

test("parse-cache: mtime mismatch → miss", () => {
  resetParseCacheState();
  const repoRoot = makeTmpRepoRoot();
  const abs = path.join(repoRoot, "doc.md");
  const doc = sampleDoc(abs);
  writeCached(repoRoot, abs, 100, doc);
  flushParseCache(repoRoot);
  resetParseCacheState();
  const miss = readCached(repoRoot, abs, 200);
  assert.equal(miss, null);
});

test("parse-cache: flushParseCache is a no-op when clean", () => {
  resetParseCacheState();
  const repoRoot = makeTmpRepoRoot();
  // No writes → flush should not create cache file.
  flushParseCache(repoRoot);
  const cacheFile = path.join(
    repoRoot,
    "tools/mcp-ia-server/.cache/parse-cache.json",
  );
  assert.equal(fs.existsSync(cacheFile), false);
});
