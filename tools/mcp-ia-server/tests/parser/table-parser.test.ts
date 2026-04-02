import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import fs from "node:fs";
import { fileURLToPath } from "node:url";
import { parseMarkdownTables } from "../../src/parser/table-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const fixtures = path.join(__dirname, "../fixtures");

test("parseMarkdownTables reads glossary fixture", () => {
  const raw = fs.readFileSync(path.join(fixtures, "sample-glossary.md"), "utf8");
  const lines = raw.split(/\r?\n/);
  const tables = parseMarkdownTables(lines);
  assert.ok(tables.length >= 2);
  assert.ok(tables[0]!.rows.some((r) => r.Term === "HeightMap"));
});

test("escaped pipe inside cell does not split columns", () => {
  const lines = [
    "| A | B |",
    "|---|---|",
    "| x | foo \\| bar end |",
  ];
  const tables = parseMarkdownTables(lines);
  assert.equal(tables.length, 1);
  assert.equal(tables[0]!.rows[0]!["A"], "x");
  assert.equal(tables[0]!.rows[0]!["B"], "foo | bar end");
});
