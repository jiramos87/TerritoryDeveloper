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
  splitLines,
} from "../src/parser/markdown-parser.js";
import { parseGlossary } from "../src/parser/glossary-parser.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");
const dataDir = path.join(__dirname, "../data");
const specIndexPath = path.join(dataDir, "spec-index.json");
const glossaryIndexPath = path.join(dataDir, "glossary-index.json");
const glossaryGraphIndexPath = path.join(dataDir, "glossary-graph-index.json");

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

interface GlossaryCitation {
  spec: string;
  section_id: string;
  section_title: string;
}

interface GlossaryGraphEntry {
  related: string[];
  cited_in: GlossaryCitation[];
}

interface GlossaryGraphDocument {
  artifact: string;
  schema_version: number;
  last_generated: string;
  graph: Record<string, GlossaryGraphEntry>;
}

function buildSpecIndex(): SpecIndexDocument {
  const specsDir = path.join(repoRoot, "ia", "specs");
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
  const glossaryPath = path.join(repoRoot, "ia", "specs", "glossary.md");
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

const CO_OCCURRENCE_TOP_N = 5;

/**
 * Build `cited_in` + `related` per glossary term by scanning every section body
 * under `ia/specs/`. "Related" = top-N co-occurring terms in the same section.
 * "Cited in" = every section whose body matches the term via a word-boundary
 * regex (skipping the glossary's own row inside `ia/specs/glossary.md`).
 */
function buildGlossaryGraphIndex(): GlossaryGraphDocument {
  const glossaryPath = path.join(repoRoot, "ia", "specs", "glossary.md");
  const entries = parseGlossary(glossaryPath);

  // Deduplicate by term and sort by descending length so longer phrases match first.
  const termList = Array.from(
    new Set(entries.map((e) => e.term.trim()).filter((t) => t.length > 1)),
  ).sort((a, b) => b.length - a.length);

  const graph: Record<string, GlossaryGraphEntry> = {};
  for (const t of termList) {
    graph[t] = { related: [], cited_in: [] };
  }

  // Co-occurrence pair counters.
  const pairCount = new Map<string, number>(); // key = `${A}\t${B}`, A<B

  const specsDir = path.join(repoRoot, "ia", "specs");
  const names = fs
    .readdirSync(specsDir)
    .filter((n) => n.endsWith(".md"))
    .sort();

  for (const name of names) {
    const filePath = path.join(specsDir, name);
    const isGlossary = name === "glossary.md";
    const specKey = path.basename(name, ".md").toLowerCase();
    const doc = parseDocument(filePath);
    const raw = fs.readFileSync(filePath, "utf8");
    const lines = splitLines(raw);
    const flat = flattenHeadingTree(doc.headings);

    for (const h of flat) {
      // Skip depth-1 (document title) sections.
      if (h.depth <= 1) continue;

      const start = h.lineStart; // 1-based line of the heading
      const end = h.lineEnd;
      const bodyLines = lines.slice(start, end); // exclude heading line itself
      const body = bodyLines.join("\n");
      if (!body.trim()) continue;

      const termsInSection = new Set<string>();

      for (const term of termList) {
        if (!term) continue;
        // Word-boundary match; for multi-word phrases, allow inline punctuation.
        const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
        // Use a case-insensitive match; avoid a leading/trailing alnum around the term.
        const re = new RegExp(`(?:^|[^A-Za-z0-9_])${escaped}(?:$|[^A-Za-z0-9_])`, "i");
        if (!re.test(body)) continue;

        termsInSection.add(term);

        // Glossary's own section-body mentions are the authoritative definition —
        // skip them from `cited_in` so we only report usages from OTHER specs.
        if (isGlossary) continue;

        graph[term]!.cited_in.push({
          spec: specKey,
          section_id: h.sectionId,
          section_title: h.title,
        });
      }

      // Co-occurrence pairs — any two terms that appear in the same section body.
      const sectionTerms = Array.from(termsInSection).sort();
      for (let i = 0; i < sectionTerms.length; i++) {
        for (let j = i + 1; j < sectionTerms.length; j++) {
          const key = `${sectionTerms[i]}\t${sectionTerms[j]}`;
          pairCount.set(key, (pairCount.get(key) ?? 0) + 1);
        }
      }
    }
  }

  // Convert pair counts into top-N related-term lists per term.
  const relatedCounters = new Map<string, Map<string, number>>();
  for (const [key, count] of pairCount) {
    const [a, b] = key.split("\t") as [string, string];
    if (!relatedCounters.has(a)) relatedCounters.set(a, new Map());
    if (!relatedCounters.has(b)) relatedCounters.set(b, new Map());
    relatedCounters.get(a)!.set(b, count);
    relatedCounters.get(b)!.set(a, count);
  }

  for (const term of termList) {
    const counter = relatedCounters.get(term);
    if (!counter) continue;
    const ranked = Array.from(counter.entries())
      .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
      .slice(0, CO_OCCURRENCE_TOP_N)
      .map(([t]) => t);
    graph[term]!.related = ranked;
  }

  // Stable ordering: alphabetical by term so the JSON diffs cleanly.
  const stable: Record<string, GlossaryGraphEntry> = {};
  for (const term of termList.slice().sort((a, b) => a.localeCompare(b))) {
    stable[term] = graph[term]!;
  }

  return {
    artifact: "glossary_graph_index",
    schema_version: 1,
    last_generated: new Date().toISOString(),
    graph: stable,
  };
}

function normalizeGeneratedJson(text: string): unknown {
  return JSON.parse(text) as unknown;
}

function assertIndexesMatchCommitted(
  specIndex: SpecIndexDocument,
  glossaryIndex: GlossaryIndexDocument,
  glossaryGraph: GlossaryGraphDocument,
): void {
  assert(fs.existsSync(specIndexPath), `Missing ${specIndexPath} (run without --check)`);
  assert(
    fs.existsSync(glossaryIndexPath),
    `Missing ${glossaryIndexPath} (run without --check)`,
  );
  assert(
    fs.existsSync(glossaryGraphIndexPath),
    `Missing ${glossaryGraphIndexPath} (run without --check)`,
  );

  const specOnDisk = normalizeGeneratedJson(
    fs.readFileSync(specIndexPath, "utf8"),
  );
  const glossOnDisk = normalizeGeneratedJson(
    fs.readFileSync(glossaryIndexPath, "utf8"),
  );
  const graphOnDisk = normalizeGeneratedJson(
    fs.readFileSync(glossaryGraphIndexPath, "utf8"),
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
  assert.deepEqual(
    stripTimestamp(graphOnDisk as Record<string, unknown>),
    stripTimestamp({ ...glossaryGraph } as Record<string, unknown>),
  );
}

function main(): void {
  loadRepoDotenvIfNotCi(repoRoot);
  const check = process.argv.includes("--check");

  process.env.REPO_ROOT = repoRoot;

  const specIndex = buildSpecIndex();
  const glossaryIndex = buildGlossaryIndex();
  const glossaryGraph = buildGlossaryGraphIndex();

  if (check) {
    assertIndexesMatchCommitted(specIndex, glossaryIndex, glossaryGraph);
    console.log("generate-ia-indexes --check: OK");
    return;
  }

  if (!fs.existsSync(dataDir)) {
    fs.mkdirSync(dataDir, { recursive: true });
  }

  fs.writeFileSync(specIndexPath, stableStringify(specIndex), "utf8");
  fs.writeFileSync(glossaryIndexPath, stableStringify(glossaryIndex), "utf8");
  fs.writeFileSync(
    glossaryGraphIndexPath,
    stableStringify(glossaryGraph),
    "utf8",
  );
  console.log(`Wrote ${path.relative(repoRoot, specIndexPath)}`);
  console.log(`Wrote ${path.relative(repoRoot, glossaryIndexPath)}`);
  console.log(`Wrote ${path.relative(repoRoot, glossaryGraphIndexPath)}`);
}

main();
