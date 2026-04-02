import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  parseDocument,
  extractSection,
  clearDocumentParseCache,
  splitLines,
  flattenHeadingTree,
  extractFrontmatter,
} from "../../src/parser/markdown-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const fixtures = path.join(__dirname, "../fixtures");

test.beforeEach(() => {
  clearDocumentParseCache();
});

test("parseDocument builds heading tree for sample spec", () => {
  const p = path.join(fixtures, "sample-spec.md");
  const doc = parseDocument(p);
  assert.ok(doc.headings.length >= 1);
  const flat = flattenHeadingTree(doc.headings);
  assert.ok(flat.some((h) => h.sectionId === "1"));
  assert.ok(flat.some((h) => h.title.includes("Nested")));
});

test("extractSection by numeric id", () => {
  const doc = parseDocument(path.join(fixtures, "sample-spec.md"));
  const m = extractSection(doc, "2.1");
  assert.ok(m && "heading" in m);
  if (m && "heading" in m) {
    assert.ok(m.content.includes("Nested"));
  }
});

test("extractSection by title substring shore", () => {
  const doc = parseDocument(path.join(fixtures, "sample-spec.md"));
  const m = extractSection(doc, "Shore band");
  assert.ok(m && "heading" in m);
});

test("empty markdown file parses", () => {
  const doc = parseDocument(path.join(fixtures, "empty.md"));
  assert.equal(doc.headings.length, 0);
  assert.ok(doc.lineCount >= 0);
});

test("parseDocument cache returns same object", () => {
  const p = path.join(fixtures, "sample-spec.md");
  const a = parseDocument(p);
  const b = parseDocument(p);
  assert.strictEqual(a, b);
});

test("splitLines empty string", () => {
  assert.deepEqual(splitLines(""), [""]);
});

test("extractFrontmatter on rule fixture", () => {
  const raw = `---\ndescription: Hi\n---\n\n# Body\n`;
  const { frontmatter, hadFrontmatterBlock } = extractFrontmatter(raw);
  assert.equal(hadFrontmatterBlock, true);
  assert.equal((frontmatter as { description?: string })?.description, "Hi");
});
