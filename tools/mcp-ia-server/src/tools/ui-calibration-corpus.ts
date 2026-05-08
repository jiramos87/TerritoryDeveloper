/**
 * MCP tools: ui_calibration_corpus_query + ui_calibration_verdict_record.
 *
 * corpus_query: read-side filter on ia/state/ui-calibration-corpus.jsonl.
 * verdict_record: append-side idempotent on (panel_slug, rebake_n).
 * JSONL append-only model — no in-place edit.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";

const CORPUS_REL = "ia/state/ui-calibration-corpus.jsonl";
const VERDICTS_REL = "ia/state/ui-calibration-verdicts.jsonl";

interface CorpusRow {
  ts: string;
  panel_slug: string;
  decision_id: string;
  prompt: string;
  resolution: string;
  rationale: string;
  "agent|human": string;
  parent_decision_id: string | null;
}

interface VerdictRow {
  ts: string;
  panel_slug: string;
  rebake_n: number;
  bug_ids: string[];
  improvement_ids: string[];
  resolution_path: string;
  outcome: string;
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function readJsonl<T>(filePath: string): T[] {
  if (!fs.existsSync(filePath)) return [];
  const raw = fs.readFileSync(filePath, "utf8");
  return raw
    .split("\n")
    .filter((line) => line.trim().length > 0)
    .map((line) => JSON.parse(line) as T);
}

// ── ui_calibration_corpus_query ───────────────────────────────────────────

const corpusQuerySchema = z.object({
  panel_slug: z.string().optional().describe("Filter by panel slug (e.g. 'hud-bar')."),
  agent_or_human: z
    .enum(["agent", "human"])
    .optional()
    .describe("Filter by contributor: 'agent' or 'human'. Omit for all."),
  decision_id: z.string().optional().describe("Filter by exact decision_id (e.g. 'D001')."),
});

export function registerUiCalibrationCorpusQuery(server: McpServer): void {
  server.registerTool(
    "ui_calibration_corpus_query",
    {
      description:
        "Read-side filter on ia/state/ui-calibration-corpus.jsonl. Returns matching corpus rows filtered by panel_slug, agent_or_human, or decision_id. All filters optional (omit for full corpus). Append-only source — no mutation.",
      inputSchema: corpusQuerySchema,
    },
    async (args) =>
      runWithToolTiming("ui_calibration_corpus_query", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof corpusQuerySchema>) => {
          const repoRoot = resolveRepoRoot();
          const filePath = path.join(repoRoot, CORPUS_REL);
          const rows = readJsonl<CorpusRow>(filePath);

          let filtered = rows;
          if (input.panel_slug) {
            filtered = filtered.filter((r) => r.panel_slug === input.panel_slug);
          }
          if (input.agent_or_human) {
            filtered = filtered.filter((r) => r["agent|human"] === input.agent_or_human);
          }
          if (input.decision_id) {
            filtered = filtered.filter((r) => r.decision_id === input.decision_id);
          }

          return { rows: filtered, total: filtered.length };
        })(corpusQuerySchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── ui_calibration_verdict_record ────────────────────────────────────────

const verdictRecordSchema = z.object({
  panel_slug: z.string().describe("Panel slug this verdict applies to (e.g. 'hud-bar')."),
  rebake_n: z.number().int().positive().describe("Rebake iteration number (1-based)."),
  bug_ids: z.array(z.string()).default([]).describe("Bug ids resolved in this rebake (e.g. ['A','D'])."),
  improvement_ids: z.array(z.string()).default([]).describe("Improvement ids applied (e.g. ['Imp-1'])."),
  resolution_path: z.string().describe("Description of what was done + root causes addressed."),
  outcome: z.enum(["pass", "fail", "partial"]).describe("Bake outcome: pass | fail | partial."),
});

export function registerUiCalibrationVerdictRecord(server: McpServer): void {
  server.registerTool(
    "ui_calibration_verdict_record",
    {
      description:
        "Append a verdict row to ia/state/ui-calibration-verdicts.jsonl. Idempotent on (panel_slug, rebake_n) — second call with same pair is a no-op and returns {skipped:true}. JSONL append-only; no in-place edit.",
      inputSchema: verdictRecordSchema,
    },
    async (args) =>
      runWithToolTiming("ui_calibration_verdict_record", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof verdictRecordSchema>) => {
          const repoRoot = resolveRepoRoot();
          const filePath = path.join(repoRoot, VERDICTS_REL);

          // Idempotency check
          const existing = readJsonl<VerdictRow>(filePath);
          const duplicate = existing.find(
            (r) => r.panel_slug === input.panel_slug && r.rebake_n === input.rebake_n,
          );
          if (duplicate) {
            return { skipped: true, reason: "verdict already recorded for (panel_slug, rebake_n)", existing: duplicate };
          }

          const row: VerdictRow = {
            ts: new Date().toISOString(),
            panel_slug: input.panel_slug,
            rebake_n: input.rebake_n,
            bug_ids: input.bug_ids,
            improvement_ids: input.improvement_ids,
            resolution_path: input.resolution_path,
            outcome: input.outcome,
          };

          fs.appendFileSync(filePath, JSON.stringify(row) + "\n", "utf8");
          return { recorded: true, row };
        })(verdictRecordSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
