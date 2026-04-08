/**
 * Generate I1 (spec index) and I2 (glossary → spec anchor) JSON under tools/mcp-ia-server/data/.
 */

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { glossarySpecCellToIndex } from "../src/ia-index/glossary-spec-ref.js";
import {
  flattenHeadingTree,
  parseDocument,
} from "../src/parser/markdown-parser.js";
import { parseGlossary } from "../src/parser/glossary-parser.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");
const dataDir = path.join(__dirname, "../data");
const specIndexPath = path.join(dataDir, "spec-index.json");
const glossaryIndexPath = path.join(dataDir, "glossary-index.json");

function stableStringify(value: unknown): string {
  return JSON.stringify(value, null, 2) + "\n";
}

interface SpecIndexSection {
  section_id: string;
  title: string;
  depth: number;
  line_start: number;
}

interface SpecIndexEntry {
  key: string;
  path: string;
  sections: SpecIndexSection[];
}

interface SpecIndexDocument {
  artifact: string;
  schema_version: number;
  last_generated: string;
  specs: SpecIndexEntry[];
}

interface GlossaryIndexDocument {
  artifact: string;
  schema_version: number;
  last_generated: string;
  terms: Record<string, { spec_key: string; anchor: string }>;
}

function buildSpecIndex(): SpecIndexDocument {
  const specsDir = path.join(repoRoot, ".cursor", "specs");
  const names = fs
    .readdirSync(specsDir)
    .filter((n) => n.endsWith(".md"))
    .sort();

  const specs: SpecIndexEntry[] = [];

  for (const name of names) {
    const filePath = path.join(specsDir, name);
    const doc = parseDocument(filePath);
    const key = path.basename(name, ".md").toLowerCase();
    const rel = path
      .relative(repoRoot, filePath)
      .split(path.sep)
      .join("/");

    const flat = flattenHeadingTree(doc.headings);
    const sections: SpecIndexSection[] = flat.map((h) => ({
      section_id: h.sectionId,
      title: h.title,
      depth: h.depth,
      line_start: h.lineStart,
    }));

    specs.push({ key, path: rel, sections });
  }

  return {
    artifact: "spec_index",
    schema_version: 1,
    last_generated: new Date().toISOString(),
    specs,
  };
}

function buildGlossaryIndex(): GlossaryIndexDocument {
  const glossaryPath = path.join(repoRoot, ".cursor", "specs", "glossary.md");
  const entries = parseGlossary(glossaryPath);
  const terms: Record<string, { spec_key: string; anchor: string }> = {};

  for (const e of entries) {
    const term = e.term.trim();
    if (!term) continue;
    const idx = glossarySpecCellToIndex(e.specReference);
    if (!idx) continue;
    terms[term] = { spec_key: idx.spec_key, anchor: idx.anchor };
  }

  return {
    artifact: "glossary_index",
    schema_version: 1,
    last_generated: new Date().toISOString(),
    terms,
  };
}

function normalizeGeneratedJson(text: string): unknown {
  return JSON.parse(text) as unknown;
}

function assertIndexesMatchCommitted(
  specIndex: SpecIndexDocument,
  glossaryIndex: GlossaryIndexDocument,
): void {
  assert(fs.existsSync(specIndexPath), `Missing ${specIndexPath} (run without --check)`);
  assert(
    fs.existsSync(glossaryIndexPath),
    `Missing ${glossaryIndexPath} (run without --check)`,
  );

  const specOnDisk = normalizeGeneratedJson(
    fs.readFileSync(specIndexPath, "utf8"),
  );
  const glossOnDisk = normalizeGeneratedJson(
    fs.readFileSync(glossaryIndexPath, "utf8"),
  );

  const stripTimestamp = (doc: Record<string, unknown>) => {
    const { last_generated: _lg, ...rest } = doc;
    return rest;
  };

  assert.deepEqual(
    stripTimestamp(specOnDisk as Record<string, unknown>),
    stripTimestamp({ ...specIndex } as Record<string, unknown>),
  );
  assert.deepEqual(
    stripTimestamp(glossOnDisk as Record<string, unknown>),
    stripTimestamp({ ...glossaryIndex } as Record<string, unknown>),
  );
}

function main(): void {
  loadRepoDotenvIfNotCi(repoRoot);
  const check = process.argv.includes("--check");

  process.env.REPO_ROOT = repoRoot;

  const specIndex = buildSpecIndex();
  const glossaryIndex = buildGlossaryIndex();

  if (check) {
    assertIndexesMatchCommitted(specIndex, glossaryIndex);
    console.log("generate-ia-indexes --check: OK");
    return;
  }

  if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
  }

  fs.writeFileSync(specIndexPath, stableStringify(specIndex), "utf8");
  fs.writeFileSync(glossaryIndexPath, stableStringify(glossaryIndex), "utf8");
  console.log(`Wrote ${path.relative(repoRoot, specIndexPath)}`);
  console.log(`Wrote ${path.relative(repoRoot, glossaryIndexPath)}`);
}

main();
