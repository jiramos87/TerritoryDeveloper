/**
 * MCP tool: web_findings_dedupe — collapse + group + recency-filter a batch of
 * `WebSearch` / `WebFetch` hits surfaced during the topic-research-survey
 * skill's Phase 1.
 *
 * Pure in-memory transform. No network. No filesystem.
 *
 * Inputs:
 *   - hits[]   — list of `{title, url, snippet?, publish_year?, source_type?}`.
 *   - as_of    — recency anchor `YYYY-MM`.
 *   - window_months — keep hits where `publish_year >= as_of_year - floor(window/12)`.
 *                     Default 24 (i.e. last two years).
 *   - keep_canonical_when_old — if true, hits whose `source_type ∈ {vendor, paper}`
 *                               survive the recency filter regardless of age.
 *                               Default true.
 *
 * Output:
 *   - kept[] — surviving hits, sorted by `publish_year desc` then `source_type` rank
 *              (vendor > paper > repo > community > blog).
 *   - groups{source_type: count}
 *   - dropped_duplicates — number of URL collisions removed.
 *   - dropped_stale — number of hits past the recency window (and not canonical).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const SOURCE_TYPES = ["vendor", "paper", "repo", "community", "blog"] as const;
type SourceType = (typeof SOURCE_TYPES)[number];

const sourceTypeRank: Record<SourceType, number> = {
  vendor: 5,
  paper: 4,
  repo: 3,
  community: 2,
  blog: 1,
};

const hitSchema = z.object({
  title: z.string().min(1),
  url: z.string().url(),
  snippet: z.string().optional(),
  publish_year: z.number().int().min(1990).max(3000).optional(),
  source_type: z.enum(SOURCE_TYPES).optional(),
});

const inputSchema = z.object({
  hits: z.array(hitSchema).min(1),
  as_of: z.string().regex(/^\d{4}-\d{2}$/),
  window_months: z.number().int().min(1).max(240).default(24),
  keep_canonical_when_old: z.boolean().default(true),
});

type Input = z.infer<typeof inputSchema>;
type Hit = z.infer<typeof hitSchema>;

interface DedupeResult {
  kept: Hit[];
  groups: Record<string, number>;
  dropped_duplicates: number;
  dropped_stale: number;
  as_of: string;
  cutoff_year: number;
}

function canonicalUrl(url: string): string {
  try {
    const u = new URL(url);
    u.hash = "";
    // Strip common tracking params.
    const stripped = new URLSearchParams(u.search);
    for (const k of Array.from(stripped.keys())) {
      if (/^utm_/i.test(k) || k === "ref" || k === "ref_src") stripped.delete(k);
    }
    u.search = stripped.toString();
    // Trim trailing slash for normalization.
    let s = u.toString();
    if (s.endsWith("/")) s = s.slice(0, -1);
    return s.toLowerCase();
  } catch {
    return url.trim().toLowerCase();
  }
}

export function runWebFindingsDedupe(input: Input): DedupeResult {
  const asOfYear = parseInt(input.as_of.slice(0, 4), 10);
  const yearsBack = Math.floor(input.window_months / 12);
  const cutoffYear = asOfYear - yearsBack;

  const byCanonical = new Map<string, Hit>();
  let droppedDuplicates = 0;
  for (const h of input.hits) {
    const key = canonicalUrl(h.url);
    if (byCanonical.has(key)) {
      droppedDuplicates++;
      // Keep the hit with the higher source rank if available.
      const prev = byCanonical.get(key)!;
      const prevRank = prev.source_type
        ? sourceTypeRank[prev.source_type]
        : 0;
      const newRank = h.source_type ? sourceTypeRank[h.source_type] : 0;
      if (newRank > prevRank) byCanonical.set(key, h);
      continue;
    }
    byCanonical.set(key, h);
  }

  const after: Hit[] = [];
  let droppedStale = 0;
  for (const h of byCanonical.values()) {
    const year = h.publish_year ?? null;
    const isCanonical =
      input.keep_canonical_when_old &&
      (h.source_type === "vendor" || h.source_type === "paper");
    if (year !== null && year < cutoffYear && !isCanonical) {
      droppedStale++;
      continue;
    }
    after.push(h);
  }

  after.sort((a, b) => {
    const ay = a.publish_year ?? 0;
    const by = b.publish_year ?? 0;
    if (by !== ay) return by - ay;
    const ar = a.source_type ? sourceTypeRank[a.source_type] : 0;
    const br = b.source_type ? sourceTypeRank[b.source_type] : 0;
    return br - ar;
  });

  const groups: Record<string, number> = {};
  for (const h of after) {
    const k = h.source_type ?? "unknown";
    groups[k] = (groups[k] ?? 0) + 1;
  }

  return {
    kept: after,
    groups,
    dropped_duplicates: droppedDuplicates,
    dropped_stale: droppedStale,
    as_of: input.as_of,
    cutoff_year: cutoffYear,
  };
}

export function registerWebFindingsDedupe(server: McpServer): void {
  server.registerTool(
    "web_findings_dedupe",
    {
      description:
        "Pure in-memory dedupe + recency filter for `WebSearch` / `WebFetch` hit batches. Collapses by canonical URL (strips utm_*, ref, hash, trailing slash, lowercases), drops hits older than `window_months` from `as_of` (vendor + paper sources kept regardless when `keep_canonical_when_old=true`), sorts by `publish_year desc` then `source_type` rank. No network, no DB.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("web_findings_dedupe", async () => {
        const envelope = await wrapTool(async (input: unknown) => {
          const parsed = inputSchema.safeParse(input ?? {});
          if (!parsed.success) {
            throw {
              code: "invalid_input" as const,
              message: parsed.error.issues
                .map((i) => `${i.path.join(".")}: ${i.message}`)
                .join("; "),
            };
          }
          return runWebFindingsDedupe(parsed.data);
        })(args);
        return jsonResult(envelope);
      }),
  );
}
