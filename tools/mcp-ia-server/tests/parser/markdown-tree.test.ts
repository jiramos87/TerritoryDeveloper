import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  buildHeadingTree,
  splitLines,
  getBodyStartLine1Based,
  clearDocumentParseCache,
} from "../../src/parser/markdown-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

test.beforeEach(() => {
  clearDocumentParseCache();
});

test("buildHeadingTree respects body start after frontmatter", () => {
  const raw = "---\na: 1\n---\n\n# H1\n\n## H2\n";
  const lines = splitLines(raw);
  const bodyStart = getBodyStartLine1Based(lines);
  const tree = buildHeadingTree(lines, bodyStart, lines.length);
  assert.equal(tree.length, 1);
  assert.equal(tree[0]!.title, "H1");
  assert.equal(tree[0]!.children[0]!.title, "H2");
});

test("frontmatter-only file yields empty heading tree in body", () => {
  const p = path.join(__dirname, "../fixtures/frontmatter-only.mdc");
  const raw = fs.readFileSync(p, "utf8");
  const lines = splitLines(raw);
  const bodyStart = getBodyStartLine1Based(lines);
  const tree = buildHeadingTree(lines, bodyStart, lines.length);
  assert.equal(tree.length, 0);
});
