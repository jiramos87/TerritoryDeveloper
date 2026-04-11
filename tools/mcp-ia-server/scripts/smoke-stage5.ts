/**
 * One-shot manual smoke for the Stage 5 code-intelligence + glossary-graph tools.
 *
 * Runs each new scanner against the real Assets/Scripts/ tree so the caller can
 * spot-check the shape of the output without waiting for the MCP server to
 * restart (the long-running server caches its schema in memory at session
 * start; this script exercises the underlying functions directly via tsx).
 *
 * Usage:
 *   npx tsx tools/mcp-ia-server/scripts/smoke-stage5.ts
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { scanFileForCallers } from "../src/tools/unity-callers-of.js";
import { scanFileForSubscribers } from "../src/tools/unity-subscribers-of.js";
import { summarizeClassInFile } from "../src/tools/csharp-class-summary.js";
import {
  clearGlossaryGraphCaches,
  scanCodeAppearances,
} from "../src/tools/glossary-lookup.js";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");

function sectionHeader(title: string): void {
  console.log(`\n━━ ${title} ━━`);
}

function walkCs(dir: string, out: string[] = []): string[] {
  if (!fs.existsSync(dir)) return out;
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) walkCs(full, out);
    else if (e.name.endsWith(".cs")) out.push(full);
  }
  return out;
}

const assetsDir = path.join(repoRoot, "Assets", "Scripts");
const csFiles = walkCs(assetsDir);
console.log(`Found ${csFiles.length} .cs files under Assets/Scripts/`);

// 1) unity_callers_of — hunt callers of GridManager.GetCell (known method).
sectionHeader("unity_callers_of  GridManager.GetCell");
{
  const hits: { file: string; line: number; snippet: string }[] = [];
  for (const file of csFiles) {
    hits.push(...scanFileForCallers(file, repoRoot, "GetCell", "gridManager"));
    hits.push(...scanFileForCallers(file, repoRoot, "GetCell", "GridManager"));
  }
  console.log(`caller_count = ${hits.length}`);
  for (const h of hits.slice(0, 5)) {
    console.log(`  ${h.file}:${h.line}  ${h.snippet}`);
  }
}

// 2) unity_subscribers_of — hunt subscribers of onGridRestored (real event).
sectionHeader("unity_subscribers_of  onGridRestored");
{
  const hits: ReturnType<typeof scanFileForSubscribers> = [];
  for (const file of csFiles) {
    hits.push(...scanFileForSubscribers(file, repoRoot, "onGridRestored"));
  }
  console.log(`subscriber_count = ${hits.length}`);
  for (const h of hits.slice(0, 5)) {
    console.log(
      `  ${h.file}:${h.line}  ${h.className}.${h.method}  += ${h.handler}`,
    );
  }
}

// 3) csharp_class_summary — summarize RoadManager (known large class).
sectionHeader("csharp_class_summary  RoadManager");
{
  let summary: ReturnType<typeof summarizeClassInFile> = null;
  for (const file of csFiles) {
    summary = summarizeClassInFile(file, repoRoot, "RoadManager");
    if (summary) break;
  }
  if (!summary) {
    console.log("  <class not found>");
  } else {
    console.log(`  file: ${summary.file}:${summary.declaration_line}`);
    console.log(`  base_types: ${JSON.stringify(summary.base_types)}`);
    console.log(`  public_methods: ${summary.public_methods.length}`);
    for (const m of summary.public_methods.slice(0, 5)) {
      console.log(`    ${m.signature}  (line ${m.line})`);
    }
    console.log(`  fields: ${summary.fields.length}`);
    console.log(`  dependencies: ${summary.dependencies.length}`);
    console.log(`  brief_xml_doc: ${summary.brief_xml_doc.slice(0, 120)}...`);
  }
}

// 4) glossary_lookup graph data for "HeightMap".
sectionHeader("glossary_lookup  HeightMap (graph data)");
{
  const graphPath = path.join(
    repoRoot,
    "tools",
    "mcp-ia-server",
    "data",
    "glossary-graph-index.json",
  );
  if (!fs.existsSync(graphPath)) {
    console.log("  <graph index missing>");
  } else {
    const graph = JSON.parse(fs.readFileSync(graphPath, "utf8")) as {
      graph: Record<
        string,
        { related: string[]; cited_in: { spec: string; section_title: string }[] }
      >;
    };
    const entry = graph.graph["HeightMap"];
    if (!entry) {
      console.log("  <HeightMap not in graph>");
    } else {
      console.log(`  related: ${entry.related.join(", ")}`);
      console.log(`  cited_in count: ${entry.cited_in.length}`);
      for (const c of entry.cited_in.slice(0, 3)) {
        console.log(`    ${c.spec} — ${c.section_title}`);
      }
    }

    clearGlossaryGraphCaches();
    const appearances = scanCodeAppearances("HeightMap", repoRoot);
    console.log(`  appears_in_code count: ${appearances.length}`);
    for (const a of appearances.slice(0, 3)) {
      console.log(`    ${a.file}:${a.line}`);
    }
  }
}

console.log("\nSmoke complete.");
