/**
 * MCP tool: arch_decision_conflict_scan — score a list of free-text improvement
 * proposals against active arch_decisions and flag conflicts.
 *
 * Use in the topic-research-survey skill Phase 5 (§Exploration) to populate
 * the optional "Conflicts with locked decisions" subsection.
 *
 * Heuristic: each `proposal.text` is tokenized; tokens are matched against
 * each active arch_decision's `title` + `rationale` + linked `surface_slug`.
 * Score = unique token hits across the three fields. Conflicts returned with
 * score >= `min_score` (default 2).
 *
 * Read-only. DB-required (returns `db_unconfigured` error envelope when no
 * Postgres pool).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runArchDecisionList } from "./arch.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const proposalSchema = z.object({
  id: z.string().min(1).describe("Proposal identifier (e.g. '1', 'imp-1')."),
  text: z.string().min(5).describe("Free-text proposal body."),
});

const inputSchema = z.object({
  proposals: z.array(proposalSchema).min(1).max(50),
  min_score: z.number().int().min(1).max(10).default(2),
  fields: z
    .array(z.enum(["title", "rationale", "surface"]))
    .default(["title", "rationale", "surface"])
    .describe("Which decision fields to match against."),
});

type Input = z.infer<typeof inputSchema>;

interface ConflictRow {
  proposal_id: string;
  decision_slug: string;
  decision_title: string;
  surface_slug: string | null;
  score: number;
  matched_tokens: string[];
  matched_fields: string[];
}

interface ScanResult {
  conflicts: ConflictRow[];
  proposals_scanned: number;
  active_decisions_scanned: number;
  min_score: number;
}

const TOKEN_MIN_LEN = 4;
const STOPWORDS = new Set([
  "with",
  "that",
  "this",
  "from",
  "into",
  "have",
  "been",
  "will",
  "than",
  "their",
  "there",
  "would",
  "could",
  "should",
  "about",
  "which",
  "using",
  "where",
  "when",
]);

function tokenize(text: string): string[] {
  return Array.from(
    new Set(
      text
        .toLowerCase()
        .split(/[^a-z0-9]+/)
        .filter((t) => t.length >= TOKEN_MIN_LEN && !STOPWORDS.has(t)),
    ),
  );
}

export async function runArchDecisionConflictScan(
  input: Input,
): Promise<ScanResult> {
  const pool = getIaDatabasePool();
  if (!pool) throw dbUnconfiguredError();

  const { decisions } = await runArchDecisionList(pool, { status: "active" });

  const conflicts: ConflictRow[] = [];
  for (const proposal of input.proposals) {
    const tokens = tokenize(proposal.text);
    if (tokens.length === 0) continue;

    for (const d of decisions) {
      const fieldHits = new Map<string, string[]>(); // field → tokens matched

      if (input.fields.includes("title") && d.title) {
        const titleLower = d.title.toLowerCase();
        const hits = tokens.filter((t) => titleLower.includes(t));
        if (hits.length > 0) fieldHits.set("title", hits);
      }
      if (input.fields.includes("rationale") && d.rationale) {
        const rationaleLower = d.rationale.toLowerCase();
        const hits = tokens.filter((t) => rationaleLower.includes(t));
        if (hits.length > 0) fieldHits.set("rationale", hits);
      }
      if (input.fields.includes("surface") && d.surface_slug) {
        const surfaceLower = d.surface_slug.toLowerCase();
        const hits = tokens.filter((t) => surfaceLower.includes(t));
        if (hits.length > 0) fieldHits.set("surface", hits);
      }

      if (fieldHits.size === 0) continue;

      const matchedTokens = Array.from(
        new Set(Array.from(fieldHits.values()).flat()),
      );
      const score = matchedTokens.length;
      if (score < input.min_score) continue;

      conflicts.push({
        proposal_id: proposal.id,
        decision_slug: d.slug,
        decision_title: d.title,
        surface_slug: d.surface_slug,
        score,
        matched_tokens: matchedTokens,
        matched_fields: Array.from(fieldHits.keys()),
      });
    }
  }

  conflicts.sort(
    (a, b) =>
      b.score - a.score ||
      a.proposal_id.localeCompare(b.proposal_id) ||
      a.decision_slug.localeCompare(b.decision_slug),
  );

  return {
    conflicts,
    proposals_scanned: input.proposals.length,
    active_decisions_scanned: decisions.length,
    min_score: input.min_score,
  };
}

export function registerArchDecisionConflictScan(server: McpServer): void {
  server.registerTool(
    "arch_decision_conflict_scan",
    {
      description:
        "Score free-text improvement proposals against active arch_decisions. Tokenizes each proposal (len≥4, stopword-filtered) and counts hits across `title` / `rationale` / `surface_slug` fields. Returns conflicts where `score >= min_score` (default 2). Used by the topic-research-survey skill Phase 5 to populate the 'Conflicts with locked decisions' subsection. DB-required.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("arch_decision_conflict_scan", async () => {
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
          return await runArchDecisionConflictScan(parsed.data);
        })(args);
        return jsonResult(envelope);
      }),
  );
}
