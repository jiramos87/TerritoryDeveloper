/**
 * Smoke test: spawn the IA MCP server the same way Cursor does and call tools via the SDK client.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
// scripts/ → mcp-ia-server/ → tools/ → repository root
const repoRoot = path.resolve(__dirname, "../../..");

function parseJsonFromToolResult(result: {
  content?: Array<{ type: string; text?: string }>;
}): unknown {
  const block = result.content?.find((c) => c.type === "text");
  if (!block?.text) throw new Error("No text content in tool result");
  return JSON.parse(block.text);
}

async function main(): Promise<void> {
  const transport = new StdioClientTransport({
    command: "npx",
    args: ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
    cwd: repoRoot,
    env: {
      ...process.env,
      REPO_ROOT: repoRoot,
    },
    stderr: "inherit",
  });

  const client = new Client(
    { name: "territory-ia-verify", version: "0.0.1" },
    { capabilities: {} },
  );

  await client.connect(transport);

  const server = client.getServerVersion();
  console.log("Connected. Server:", server?.name, server?.version);

  const { tools } = await client.listTools();
  const names = tools.map((t) => t.name).sort();
  console.log("Tools:", names.join(", "));
  const required = [
    "list_specs",
    "spec_outline",
    "spec_section",
    "glossary_lookup",
    "glossary_discover",
    "router_for_task",
    "invariants_summary",
    "list_rules",
    "rule_content",
    "backlog_issue",
  ];
  for (const n of required) {
    if (!names.includes(n)) throw new Error(`Missing MCP tool: ${n}`);
  }

  const all = parseJsonFromToolResult(
    await client.callTool({ name: "list_specs", arguments: {} }),
  ) as Array<{ key: string; category: string }>;
  console.log("list_specs count:", all.length);
  if (all.length !== 23) {
    throw new Error(`Expected 23 IA documents, got ${all.length}`);
  }

  const rules = parseJsonFromToolResult(
    await client.callTool({
      name: "list_specs",
      arguments: { category: "rule" },
    }),
  ) as Array<{ category: string }>;
  if (rules.length !== 11 || rules.some((r) => r.category !== "rule")) {
    throw new Error("list_specs category=rule filter failed");
  }

  const outline = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "isometric-geography-system" },
    }),
  ) as { outline: unknown[]; error?: string };
  if (outline.error || !Array.isArray(outline.outline)) {
    throw new Error("spec_outline isometric-geography-system failed");
  }
  console.log("spec_outline isometric-geography-system top-level headings:", outline.outline.length);

  const unityOutline = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "unity" },
    }),
  ) as { outline: unknown[]; error?: string };
  if (unityOutline.error || !Array.isArray(unityOutline.outline)) {
    throw new Error("spec_outline unity (alias → unity-development-context) failed");
  }

  const unitySec = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: { spec: "unity-development-context", section: "1" },
    }),
  ) as { content?: string; error?: string };
  if (unitySec.error || !unitySec.content?.includes("Purpose and scope")) {
    throw new Error("spec_section unity-development-context section 1 failed");
  }

  const inv = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "invariants" },
    }),
  ) as { frontmatter: Record<string, unknown> | null; outline: unknown[] };
  if (inv.frontmatter === null) {
    throw new Error("Expected invariants.mdc to have frontmatter");
  }

  const bad = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "nonexistent-xyz" },
    }),
  ) as { error?: string; available_keys?: string[] };
  if (bad.error !== "unknown_spec" || !Array.isArray(bad.available_keys)) {
    throw new Error("Expected unknown_spec error with available_keys");
  }

  const sec134 = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: { spec: "isometric-geography-system", section: "13.4" },
    }),
  ) as { content?: string; error?: string; title?: string };
  if (sec134.error || !sec134.content?.includes("Bridges and water")) {
    throw new Error("spec_section 13.4 expected bridge content");
  }

  const sec13 = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: { spec: "isometric-geography-system", section: "13" },
    }),
  ) as { content?: string; error?: string };
  if (sec13.error || !sec13.content?.includes("13.1")) {
    throw new Error("spec_section 13 should include subsection 13.1");
  }

  const secGloss = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: {
        spec: "glossary",
        section: "Roads & Bridges",
        max_chars: 20000,
      },
    }),
  ) as { content?: string; error?: string };
  if (secGloss.error || !secGloss.content?.includes("Wet run")) {
    throw new Error("spec_section glossary Roads & Bridges failed");
  }

  const secRoads = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: {
        spec: "roads-system",
        section: "Land slope stroke policy",
      },
    }),
  ) as { content?: string; error?: string };
  if (secRoads.error || !secRoads.content?.includes("BUG-51")) {
    throw new Error("spec_section roads-system title match failed");
  }

  const secBad = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: { spec: "isometric-geography-system", section: "999" },
    }),
  ) as { error?: string; available_sections?: unknown[] };
  if (secBad.error !== "unknown_section" || !Array.isArray(secBad.available_sections)) {
    throw new Error("spec_section unknown_section expected");
  }

  const secTrunc = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: {
        spec: "isometric-geography-system",
        section: "13",
        max_chars: 500,
      },
    }),
  ) as { truncated?: boolean; totalChars?: number };
  if (!secTrunc.truncated || (secTrunc.totalChars ?? 0) <= 500) {
    throw new Error("spec_section max_chars truncation expected");
  }

  const gloss = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_lookup",
      arguments: { term: "wet run" },
    }),
  ) as { term?: string; error?: string };
  if (gloss.error || !gloss.term?.toLowerCase().includes("wet")) {
    throw new Error("glossary_lookup wet run failed");
  }

  const glossCi = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_lookup",
      arguments: { term: "WET RUN" },
    }),
  ) as { term?: string; error?: string };
  if (glossCi.error) throw new Error("glossary_lookup case-insensitive failed");

  const glossBad = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_lookup",
      arguments: { term: "nonexistent-term-xyz" },
    }),
  ) as { error?: string; available_terms?: string[] };
  if (glossBad.error !== "term_not_found" || !Array.isArray(glossBad.available_terms)) {
    throw new Error("glossary_lookup term_not_found expected");
  }

  const routeRoads = parseJsonFromToolResult(
    await client.callTool({
      name: "router_for_task",
      arguments: { domain: "roads" },
    }),
  ) as { matches?: unknown[]; error?: string };
  if (routeRoads.error || !routeRoads.matches?.length) {
    throw new Error("router_for_task roads failed");
  }

  const routeSave = parseJsonFromToolResult(
    await client.callTool({
      name: "router_for_task",
      arguments: { domain: "save" },
    }),
  ) as { matches?: unknown[]; error?: string };
  if (routeSave.error || !routeSave.matches?.length) {
    throw new Error("router_for_task save failed");
  }

  const routeGrid = parseJsonFromToolResult(
    await client.callTool({
      name: "router_for_task",
      arguments: { domain: "grid math" },
    }),
  ) as { matches?: unknown[]; error?: string };
  if (routeGrid.error || !routeGrid.matches?.length) {
    throw new Error("router_for_task grid math failed");
  }
  const gridMatch = (routeGrid.matches as { specToRead?: string }[])[0];
  if (!gridMatch?.specToRead?.includes("isometric-geography-system")) {
    throw new Error("router_for_task grid math should point at geography spec");
  }

  const routeBad = parseJsonFromToolResult(
    await client.callTool({
      name: "router_for_task",
      arguments: { domain: "xyz-no-match-12345" },
    }),
  ) as { error?: string; available_domains?: string[] };
  if (routeBad.error !== "no_matching_domain" || !Array.isArray(routeBad.available_domains)) {
    throw new Error("router_for_task no_matching_domain expected");
  }

  const invSum = parseJsonFromToolResult(
    await client.callTool({ name: "invariants_summary", arguments: {} }),
  ) as { invariants?: string[]; guardrails?: string[]; error?: string };
  if (invSum.error) throw new Error("invariants_summary error");
  if ((invSum.invariants?.length ?? 0) !== 12) {
    throw new Error(`Expected 12 invariants, got ${invSum.invariants?.length}`);
  }
  if ((invSum.guardrails?.length ?? 0) !== 9) {
    throw new Error(`Expected 9 guardrails, got ${invSum.guardrails?.length}`);
  }

  const lr = parseJsonFromToolResult(
    await client.callTool({ name: "list_rules", arguments: {} }),
  ) as { rules?: unknown[] };
  if ((lr.rules?.length ?? 0) !== 11) {
    throw new Error(`list_rules expected 11 rules, got ${lr.rules?.length}`);
  }

  const rc = parseJsonFromToolResult(
    await client.callTool({
      name: "rule_content",
      arguments: { rule: "roads", max_chars: 50000 },
    }),
  ) as { content?: string; error?: string; key?: string };
  if (rc.error || !rc.content?.length || rc.key !== "roads") {
    throw new Error("rule_content roads failed");
  }

  const outlineGeo = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_outline",
      arguments: { spec: "geo" },
    }),
  ) as { key?: string; error?: string; outline?: unknown[] };
  if (outlineGeo.error || outlineGeo.key !== "isometric-geography-system") {
    throw new Error("spec_outline geo alias failed");
  }

  const secGeoAlias = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: {
        spec: "geo",
        section: "13.4",
        max_chars: 8000,
      },
    }),
  ) as { content?: string; error?: string; key?: string };
  if (secGeoAlias.error || secGeoAlias.key !== "isometric-geography-system") {
    throw new Error("spec_section geo alias failed");
  }
  if (!secGeoAlias.content?.includes("Bridges and water")) {
    throw new Error("spec_section geo + 13.4 content failed");
  }

  const secFuzz = parseJsonFromToolResult(
    await client.callTool({
      name: "spec_section",
      arguments: {
        spec: "geo",
        section: "Briges",
        max_chars: 8000,
      },
    }),
  ) as { content?: string; error?: string; matchType?: string };
  if (
    secFuzz.error ||
    secFuzz.matchType !== "fuzzy" ||
    !secFuzz.content?.includes("Bridges and water")
  ) {
    throw new Error("spec_section fuzzy typo Briges → bridges section failed");
  }

  const glossTypo = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_lookup",
      arguments: { term: "hight map" },
    }),
  ) as { term?: string; matchType?: string; error?: string };
  if (glossTypo.error || glossTypo.matchType !== "fuzzy") {
    throw new Error("glossary_lookup fuzzy typo hight map failed");
  }
  if (!glossTypo.term?.toLowerCase().includes("height")) {
    throw new Error("glossary_lookup expected HeightMap-style term");
  }

  const discover = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_discover",
      arguments: {
        query: "wet run lip grass stroke",
        max_results: 5,
      },
    }),
  ) as { matches?: Array<{ term?: string; matchReasons?: string[] }>; error?: string };
  if (discover.error) throw new Error("glossary_discover returned error");
  if (!discover.matches?.length) {
    throw new Error("glossary_discover expected at least one match for road/wet keywords");
  }
  const wetHit = discover.matches.find((m) =>
    m.term?.toLowerCase().includes("wet"),
  );
  if (!wetHit?.matchReasons?.length) {
    throw new Error("glossary_discover expected matchReasons on wet-run style hit");
  }

  const discoverEmpty = parseJsonFromToolResult(
    await client.callTool({
      name: "glossary_discover",
      arguments: { query: "zzzz-no-glossary-hit-99999" },
    }),
  ) as { matches?: unknown[]; suggestions?: string[]; message?: string };
  if ((discoverEmpty.matches?.length ?? 0) !== 0) {
    throw new Error("glossary_discover expected empty matches for nonsense query");
  }
  if (!Array.isArray(discoverEmpty.suggestions)) {
    throw new Error("glossary_discover expected suggestions array when no matches");
  }

  const bl25 = parseJsonFromToolResult(
    await client.callTool({
      name: "backlog_issue",
      arguments: { issue_id: "TECH-25" },
    }),
  ) as { issue_id?: string; status?: string; error?: string; files?: string; backlog_section?: string };
  if (bl25.error || bl25.issue_id !== "TECH-25" || bl25.status !== "open") {
    throw new Error("backlog_issue TECH-25 failed");
  }
  if (!bl25.files?.includes("unity-development-context")) {
    throw new Error("backlog_issue TECH-25 expected Files to mention unity-development-context");
  }
  if (!/agent|unity|mcp/i.test(bl25.backlog_section ?? "")) {
    throw new Error(
      "backlog_issue TECH-25 expected backlog_section to mention agent/Unity/MCP lane",
    );
  }

  const blBad = parseJsonFromToolResult(
    await client.callTool({
      name: "backlog_issue",
      arguments: { issue_id: "XYZ-99999" },
    }),
  ) as { error?: string };
  if (blBad.error !== "unknown_issue") {
    throw new Error("backlog_issue unknown_issue expected for fake id");
  }

  await transport.close();
  console.log("OK — MCP server and tools verified (stdio, same launch as .cursor/mcp.json).");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
