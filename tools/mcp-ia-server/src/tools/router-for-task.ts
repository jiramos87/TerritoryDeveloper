/**
 * MCP tool: router_for_task — substring match on agent-router.mdc (both tables).
 */

import { z } from "zod";
import matter from "gray-matter";
import fs from "node:fs";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { RouterMatchRow, SpecRegistryEntry } from "../parser/types.js";
import { splitLines } from "../parser/markdown-parser.js";
import { parseMarkdownTables } from "../parser/table-parser.js";
import { findEntryByKey } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const GEO_SPEC_REF =
  "ia/specs/isometric-geography-system.md (see Read sections column)";

const MAX_FILES = 40;

const inputShape = {
  domain: z
    .string()
    .optional()
    .describe(
      "Keyword (e.g. 'roads', 'water', 'grid math', 'save'). Case-insensitive substring match against 'Task domain' (first table) OR 'Need to understand...' (second table). Optional if `files` is provided.",
    ),
  files: z
    .array(z.string())
    .max(MAX_FILES)
    .optional()
    .describe(
      "Optional repo-relative paths or basenames (e.g. GridManager.cs, Assets/Scripts/.../WaterMap.cs). Heuristic domain hints; combine with `domain` when both are set.",
    ),
};

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

const MIN_TOKEN = 3;

/**
 * Case-insensitive substring match, plus token overlap (e.g. "roads" ↔ "Road logic…" via "road").
 */
function domainMatches(haystack: string, needle: string): boolean {
  const h = haystack.trim().toLowerCase();
  const n = needle.trim().toLowerCase();
  if (!n) return false;
  if (h.includes(n)) return true;

  const tokens = (s: string) =>
    s
      .split(/[^a-z0-9]+/)
      .map((t) => t.trim())
      .filter((t) => t.length >= MIN_TOKEN);

  const hs = tokens(h);
  const ns = tokens(n);
  for (const nt of ns) {
    for (const ht of hs) {
      if (ht.includes(nt) || nt.includes(ht)) return true;
    }
  }
  return false;
}

export function collectRouterData(bodyLines: string[]): {
  matchesForDomain: (d: string) => RouterMatchRow[];
  allDomainLabels: string[];
} {
  const tables = parseMarkdownTables(bodyLines);
  const taskTable = tables.find((t) =>
    t.rows.some((r) => Object.hasOwn(r, "Task domain")),
  );
  const geoTable = tables.find((t) =>
    t.rows.some((r) => Object.hasOwn(r, "Need to understand...")),
  );

  const allDomainLabels: string[] = [];

  if (taskTable) {
    for (const r of taskTable.rows) {
      const td = (r["Task domain"] ?? "").trim();
      if (td) allDomainLabels.push(td);
    }
  }
  if (geoTable) {
    for (const r of geoTable.rows) {
      const need = (r["Need to understand..."] ?? "").trim();
      if (need) allDomainLabels.push(need);
    }
  }

  const matchesForDomain = (domain: string): RouterMatchRow[] => {
    if (!domain.trim()) return [];
    const out: RouterMatchRow[] = [];

    if (taskTable) {
      for (const r of taskTable.rows) {
        const taskDomain = (r["Task domain"] ?? "").trim();
        const specToRead = (r["Spec to read"] ?? "").trim();
        const keySections = (r["Key sections"] ?? "").trim();
        if (domainMatches(taskDomain, domain)) {
          out.push({ taskDomain, specToRead, keySections });
        }
      }
    }

    if (geoTable) {
      for (const r of geoTable.rows) {
        const need = (r["Need to understand..."] ?? "").trim();
        const readSections = (r["Read sections"] ?? "").trim();
        if (domainMatches(need, domain)) {
          out.push({
            taskDomain: need,
            specToRead: GEO_SPEC_REF,
            keySections: readSections,
          });
        }
      }
    }

    return out;
  };

  return { matchesForDomain, allDomainLabels };
}

function mergeUniqueMatches(rows: RouterMatchRow[]): RouterMatchRow[] {
  const seen = new Set<string>();
  const out: RouterMatchRow[] = [];
  for (const r of rows) {
    const k = `${r.taskDomain}\0${r.specToRead}\0${r.keySections}`;
    if (seen.has(k)) continue;
    seen.add(k);
    out.push(r);
  }
  return out;
}

/**
 * Heuristic agent-router.mdc "Task domain" labels from a file path (spec-pipeline exploration §3.2).
 */
export function inferDomainHintsFromPath(filePath: string): string[] {
  const p = filePath.replace(/\\/g, "/");
  const lower = p.toLowerCase();
  const hints = new Set<string>();

  if (/road|street|interstate|wetrun|wet_run|roadstroke|road_stroke/i.test(p)) {
    hints.add("Road logic, placement, bridges");
  }
  if (/water|shore|river|lake|watermap|cliff|cascade/i.test(p)) {
    hints.add("Water, terrain, cliffs, shores");
  }
  if (
    /gridmanager|heightmap|terraform|terrainmanager|isometric|geographymanager/i.test(
      p,
    )
  ) {
    hints.add("Slopes, sorting, geography");
  }
  if (/simulation|citystats|demandmanager|automation/i.test(lower)) {
    hints.add("Simulation, AUTO growth");
  }
  if (/(save|load|persist|dto)/i.test(p) && /(scripts|persistence)/i.test(p)) {
    hints.add("Save / load");
  }
  if (/zonemanager|zone\.cs|building|rci/i.test(lower)) {
    hints.add("Zones, buildings, RCI");
  }
  if (/uicontroller|ui-design/i.test(p) || /ia\/specs\/ui/i.test(p)) {
    hints.add("UI changes");
  }
  if (
    /\/editor\//i.test(p) ||
    /tools\/reports/i.test(p) ||
    /agentdiagnostic/i.test(p)
  ) {
    hints.add(
      "Unity / MonoBehaviour / Inspector wiring, Script Execution Order, 2D renderer `sortingOrder` / layers (not isometric stacking rules), Editor `tools/reports/` exports",
    );
  }
  if (/mcp-ia-server|backlog-parser|validate-dead-project-spec-paths/i.test(p)) {
    hints.add("Backlog / issues");
  }
  if (/glossary\.md|\/glossary/i.test(p)) {
    hints.add("Domain terms");
  }
  if (/managers\/gamemanagers|managers\\gamemanagers/i.test(lower)) {
    hints.add("Manager responsibilities");
  }
  return [...hints];
}

/**
 * Register the router_for_task tool.
 */
export function registerRouterForTask(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "router_for_task",
    {
      description:
        "Query the agent-router to find which specs and sections to read. Pass `domain` (keyword), optional `files` (paths for heuristic domains), or both; merges matches. Searches task→spec and geography quick-reference tables.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("router_for_task", async () => {
        const domain = (args?.domain ?? "").trim();
        const files = Array.isArray(args?.files) ? args!.files! : [];
        if (!domain && files.length === 0) {
          return jsonResult({
            error: "invalid_input",
            message: "Provide `domain` and/or a non-empty `files` array.",
          });
        }
        if (files.length > MAX_FILES) {
          return jsonResult({
            error: "invalid_input",
            message: `At most ${MAX_FILES} paths in files.`,
          });
        }

        const entry = findEntryByKey(registry, "agent-router");
        if (!entry) {
          return jsonResult({
            error: "no_matching_domain",
            message: "agent-router.mdc is not registered.",
            available_domains: [] as string[],
          });
        }

        const raw = fs.readFileSync(entry.filePath, "utf8");
        const { content } = matter(raw);
        const bodyLines = splitLines(content);
        const { matchesForDomain, allDomainLabels } =
          collectRouterData(bodyLines);

        const fromDomain = domain ? matchesForDomain(domain) : [];
        const hintSet = new Set<string>();
        for (const f of files) {
          for (const h of inferDomainHintsFromPath(String(f))) {
            hintSet.add(h);
          }
        }
        const file_domain_hints = [...hintSet];
        const fromFiles: RouterMatchRow[] = [];
        for (const hint of file_domain_hints) {
          fromFiles.push(...matchesForDomain(hint));
        }

        const matches = mergeUniqueMatches([...fromDomain, ...fromFiles]);

        if (matches.length === 0) {
          const available_domains = [...new Set(allDomainLabels)].sort();
          return jsonResult({
            error: "no_matching_domain",
            message: domain
              ? `No router row matches domain '${domain}'${file_domain_hints.length ? ` or file heuristics (${file_domain_hints.join("; ")})` : ""}.`
              : `No router row matches file path heuristics (${file_domain_hints.join("; ") || "none"}).`,
            available_domains,
            ...(file_domain_hints.length ? { file_domain_hints } : {}),
          });
        }

        const payload: Record<string, unknown> = { matches };
        if (domain) payload.domain = domain;
        if (files.length) {
          payload.files = files;
          payload.file_domain_hints = file_domain_hints;
        }
        return jsonResult(payload);
      }),
  );
}
