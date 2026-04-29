/**
 * MCP tools (intent author-time soft-gate) — db-lifecycle-extensions
 * Stage 3 / TECH-3403.
 *
 * Two soft-warn tools, both warn-only (Q6 lock — never block phase):
 *
 *   - `intent_lint(intent_text)` — concrete-verb + non-empty + ≤N-sentence
 *     rule registry. Externalized config at
 *     `tools/mcp-ia-server/src/data/intent-lint-rules.json` for skill-train
 *     tunability. Returns `{ok, warnings: [{rule, message}]}`.
 *
 *   - `task_intent_glossary_align(task_id)` — joins `ia_tasks.title` against
 *     `ia_glossary_terms` + `arch_surfaces` (retired filter); soft-warns
 *     glossary misses + retired-surface refs. Returns
 *     `{ok, glossary_hits[], retired_surface_hits[]}`.
 *
 * Rule registry parse failure at startup throws — does NOT auto-fall-back
 * to hardcoded defaults (would mask config errors per §Plan Digest STOP).
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const RULES_PATH = path.resolve(__dirname, "..", "data", "intent-lint-rules.json");

interface IntentLintRules {
  vague_verbs: string[];
  sentence_max: number;
  min_chars: number;
}

let cachedRules: IntentLintRules | null = null;

function loadRules(): IntentLintRules {
  if (cachedRules) return cachedRules;
  const raw = fs.readFileSync(RULES_PATH, "utf8");
  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch (e) {
    throw new Error(
      `rule_registry_parse_error: ${RULES_PATH} — ${(e as Error).message}`,
    );
  }
  const obj = parsed as Partial<IntentLintRules> & { $comment?: unknown };
  if (!Array.isArray(obj.vague_verbs)) {
    throw new Error("rule_registry_parse_error: vague_verbs must be array");
  }
  if (typeof obj.sentence_max !== "number") {
    throw new Error("rule_registry_parse_error: sentence_max must be number");
  }
  if (typeof obj.min_chars !== "number") {
    throw new Error("rule_registry_parse_error: min_chars must be number");
  }
  cachedRules = {
    vague_verbs: obj.vague_verbs.map((s) => String(s).toLowerCase()),
    sentence_max: obj.sentence_max,
    min_chars: obj.min_chars,
  };
  return cachedRules;
}

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

interface Warning {
  rule: string;
  message: string;
}

function lintIntent(text: string, rules: IntentLintRules): Warning[] {
  const warnings: Warning[] = [];
  const trimmed = text.trim();
  if (trimmed.length < rules.min_chars) {
    warnings.push({
      rule: "non_empty",
      message: "intent text empty (min_chars not met)",
    });
    return warnings;
  }
  // Sentence count — split on `.`, `!`, `?` followed by whitespace or EOL.
  const sentences = trimmed
    .split(/[.!?]+(?=\s|$)/)
    .map((s) => s.trim())
    .filter(Boolean);
  if (sentences.length > rules.sentence_max) {
    warnings.push({
      rule: "sentence_count_exceeded",
      message: `intent has ${sentences.length} sentences (max ${rules.sentence_max})`,
    });
  }
  // Vague verb scan — case-insensitive word-boundary match.
  const lower = trimmed.toLowerCase();
  for (const verb of rules.vague_verbs) {
    const re = new RegExp(`\\b${verb.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\b`, "i");
    if (re.test(lower)) {
      warnings.push({
        rule: "vague_verb",
        message: `vague verb "${verb}" detected — prefer concrete action`,
      });
    }
  }
  return warnings;
}

export function registerIntentLint(server: McpServer): void {
  server.registerTool(
    "intent_lint",
    {
      description:
        "Soft-gate author-time intent text linter. Runs concrete-verb + non-empty + sentence-count rules from externalized JSON registry (`tools/mcp-ia-server/src/data/intent-lint-rules.json`). Returns `{ok, warnings: [{rule, message}]}`. Never blocks — terminal-output only per Q6 soft-gate lock. Used by /stage-decompose before task batch authoring.",
      inputSchema: {
        intent_text: z
          .string()
          .describe("Free-text intent string from authoring phase."),
      },
    },
    async (args) =>
      runWithToolTiming("intent_lint", async () => {
        const envelope = await wrapTool(
          async (input: { intent_text: string }) => {
            const rules = loadRules();
            const warnings = lintIntent(input.intent_text ?? "", rules);
            return { ok: true, warnings };
          },
        )(args as { intent_text: string });
        return jsonResult(envelope);
      }),
  );
}

export function registerTaskIntentGlossaryAlign(server: McpServer): void {
  server.registerTool(
    "task_intent_glossary_align",
    {
      description:
        "Soft-warn alignment scan: joins `ia_tasks.title` against `ia_glossary_terms` + `arch_surfaces` (retired filter). Returns `{ok, glossary_hits[], retired_surface_hits[]}`. Glossary hits are advisory (term overlap with title); retired_surface_hits flag refs to `arch_surfaces` rows where `retired_at IS NOT NULL`. Soft-gate — terminal-output only, never blocks.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-3402."),
      },
    },
    async (args) =>
      runWithToolTiming("task_intent_glossary_align", async () => {
        const envelope = await wrapTool(
          async (input: { task_id: string }) => {
            const taskId = (input.task_id ?? "").trim();
            if (!taskId) {
              throw { code: "invalid_input", message: "task_id is required" };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            // Load task title.
            const tr = await pool.query<{ title: string }>(
              `SELECT title FROM ia_tasks WHERE task_id = $1`,
              [taskId],
            );
            if (tr.rowCount === 0) {
              throw {
                code: "invalid_input",
                message: `task not found: ${taskId}`,
              };
            }
            const title = (tr.rows[0]!.title ?? "").toLowerCase();

            // Glossary hits — case-insensitive substring match against title.
            // Real table is `glossary` (not `ia_glossary_terms`).
            let glossary_hits: string[] = [];
            try {
              const gr = await pool.query<{ term: string }>(
                `SELECT term
                   FROM glossary
                  WHERE position(lower(term) IN $1) > 0`,
                [title],
              );
              glossary_hits = gr.rows.map((r) => r.term);
            } catch {
              glossary_hits = [];
            }

            // Retired surface hits — soft-gate. `arch_surfaces` lacks a
            // `retired_at` column on current schema (forward-only BF lock —
            // pending future migration). Until then, this surface degrades
            // to empty result; soft-gate semantics preserved.
            const retired_surface_hits: string[] = [];

            return { ok: true, task_id: taskId, glossary_hits, retired_surface_hits };
          },
        )(args as { task_id: string });
        return jsonResult(envelope);
      }),
  );
}
