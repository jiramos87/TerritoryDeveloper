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
  ".cursor/specs/isometric-geography-system.md (see Read sections column)";

const inputShape = {
  domain: z
    .string()
    .describe(
      "Keyword (e.g. 'roads', 'water', 'grid math', 'save'). Case-insensitive substring match against 'Task domain' (first table) OR 'Need to understand...' (second table).",
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

function collectRouterData(bodyLines: string[]): {
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
        "Query the agent-router to find which specs and sections to read. Searches both the task→spec table and the geography quick-reference table.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("router_for_task", async () => {
        const domain = args?.domain ?? "";
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
        const matches = matchesForDomain(domain);

        if (matches.length === 0) {
          const available_domains = [...new Set(allDomainLabels)].sort();
          return jsonResult({
            error: "no_matching_domain",
            message: `No router row matches domain '${domain}'.`,
            available_domains,
          });
        }

        return jsonResult({ matches });
      }),
  );
}
