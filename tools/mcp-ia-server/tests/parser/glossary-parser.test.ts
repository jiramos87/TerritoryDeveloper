import test from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseGlossary } from "../../src/parser/glossary-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const glossaryPath = path.join(__dirname, "../fixtures/sample-glossary.md");

test("parseGlossary extracts terms and categories", () => {
  const entries = parseGlossary(glossaryPath);
  const hm = entries.find((e) => e.term === "HeightMap");
  assert.ok(hm);
  assert.equal(hm!.category, "Category Alpha");
  assert.ok(hm!.definition.includes("Terrain"));
  const wr = entries.find((e) => e.term === "Wet run");
  assert.ok(wr);
});
