import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  parseDocument,
  clearDocumentParseCache,
} from "../../src/parser/markdown-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const geoPath = path.join(repoRoot, ".cursor/specs/isometric-geography-system.md");

test(
  "parseDocument geography spec under 500ms",
  { skip: !fs.existsSync(geoPath) },
  () => {
    clearDocumentParseCache();
    const t0 = performance.now();
    parseDocument(geoPath);
    const ms = performance.now() - t0;
    assert.ok(ms < 500, `parse took ${ms}ms`);
  },
);
