/**
 * MCP tools: project_spec_journal_* — Postgres journal for Decision Log + Lessons learned (glossary **IA project spec journal**).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import {
  getProjectSpecJournalEntry,
  persistProjectSpecJournal,
  searchProjectSpecJournal,
  searchProjectSpecJournalByKeywords,
  tokenizeForJournalSearch,
  updateProjectSpecJournalEntry,
  type JournalEntryKind,
} from "../ia-db/journal-repo.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { queryTaskBody } from "../ia-db/queries.js";
import { runWithToolTiming } from "../instrumentation.js";
import { normalizeIssueId } from "../parser/project-spec-closeout-parse.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

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

const kindEnum = z.enum(["decision_log", "lessons_learned"]);

export function registerProjectSpecJournalTools(server: McpServer): void {
  server.registerTool(
    "project_spec_journal_persist",
    {
      description:
        "Append **Decision Log** and **Lessons learned** sections from a task spec body (DB-backed `ia_task_specs.body_md`) into **Postgres** table `ia_project_spec_journal` (requires `DATABASE_URL` or committed `config/postgres-dev.json` when not in CI). Use during umbrella close or per-stage progress capture. Inserts one row per non-empty section. Idempotent re-runs append additional rows.",
      inputSchema: {
        issue_id: z
          .string()
          .describe("Backlog id (e.g. TECH-776) — task spec body fetched from DB."),
        git_sha: z
          .string()
          .optional()
          .describe("Optional git commit SHA for traceability."),
      },
    },
    async (args) =>
      runWithToolTiming("project_spec_journal_persist", async () => {
        const envelope = await wrapTool(async (input: typeof args) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          const a = input as {
            issue_id?: string;
            git_sha?: string;
          };
          const rawId = (a.issue_id ?? "").trim();
          if (!rawId) {
            throw {
              code: "invalid_input" as const,
              message: "`issue_id` is required.",
            };
          }
          const issueId = normalizeIssueId(rawId);
          const markdown = await queryTaskBody(issueId);
          if (markdown == null) {
            throw {
              code: "task_not_found" as const,
              message: `No task spec body for '${issueId}' in ia_task_specs.`,
            };
          }
          const result = await persistProjectSpecJournal(pool, {
            markdown,
            issueId,
            gitSha: a.git_sha ?? null,
          });
          return result;
        })(args);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "project_spec_journal_search",
    {
      description:
        "Search **`ia_project_spec_journal`** by English full-text (`query`) and/or keyword overlap (`keyword_tokens`). Use when a new issue/spec is **ambiguous** or during **project-spec-kickoff** / **project-new** to surface past decisions and lessons **without** loading long Markdown specs. Prefer short `query` phrases; cap `max_results` (default 8). Fetch full text with **`project_spec_journal_get`** only when a hit applies.",
      inputSchema: {
        query: z
          .string()
          .optional()
          .describe(
            "English phrase for `plainto_tsquery` full-text search. Provide `query` and/or `keyword_tokens`.",
          ),
        keyword_tokens: z
          .array(z.string())
          .optional()
          .describe(
            "Lowercase tokens (length ≥ 3) for GIN overlap on `keywords` array.",
          ),
        max_results: z
          .number()
          .int()
          .min(1)
          .max(50)
          .optional()
          .describe("Max rows per search mode (default 8)."),
        kinds: z
          .array(kindEnum)
          .optional()
          .describe("Filter by `decision_log` and/or `lessons_learned`."),
        backlog_issue_id: z
          .string()
          .optional()
          .describe("Restrict full-text hits to one backlog id."),
        raw_text_for_tokens: z
          .string()
          .optional()
          .describe(
            "Optional English prose to derive `keyword_tokens` when the caller has no token list (same heuristics as glossary-oriented search).",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("project_spec_journal_search", async () => {
        const envelope = await wrapTool(async (input: typeof args) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          const a = input as {
            query?: string;
            keyword_tokens?: string[];
            max_results?: number;
            kinds?: JournalEntryKind[];
            backlog_issue_id?: string;
            raw_text_for_tokens?: string;
          };
          const max = a.max_results ?? 8;
          const kinds = a.kinds;

          let tokens = [...(a.keyword_tokens ?? [])];
          if (a.raw_text_for_tokens?.trim()) {
            tokens = [...tokens, ...tokenizeForJournalSearch(a.raw_text_for_tokens)];
          }
          tokens = [...new Set(tokens.map((t) => t.trim().toLowerCase()))];

          const q = (a.query ?? "").trim();
          const out: {
            full_text_hits?: unknown;
            keyword_hits?: unknown;
            note?: string;
          } = {};

          if (!q && tokens.length === 0) {
            throw {
              code: "invalid_input" as const,
              message: "Provide `query`, `keyword_tokens`, and/or `raw_text_for_tokens`.",
            };
          }

          if (q) {
            const ft = await searchProjectSpecJournal(pool, {
              query: q,
              limit: max,
              kinds,
              issueId: a.backlog_issue_id,
            });
            if ("error" in ft && ft.error) {
              throw { code: "db_error" as const, message: String(ft.error) };
            }
            out.full_text_hits = ft;
          }

          if (tokens.length > 0) {
            const kw = await searchProjectSpecJournalByKeywords(pool, {
              tokens,
              limit: max,
            });
            if ("error" in kw && kw.error) {
              throw { code: "db_error" as const, message: String(kw.error) };
            }
            out.keyword_hits = kw;
          }

          if (!q && tokens.length > 0) {
            out.note =
              "Keyword overlap only; add `query` for full-text when you have a short English phrase.";
          }

          return out;
        })(args);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "project_spec_journal_get",
    {
      description:
        "Return one **`ia_project_spec_journal`** row by `id` (full `body_markdown`). Use after **`project_spec_journal_search`** when an excerpt is not enough.",
      inputSchema: {
        id: z.number().int().positive().describe("Primary key from search results."),
      },
    },
    async (args) =>
      runWithToolTiming("project_spec_journal_get", async () => {
        const envelope = await wrapTool(async (input: typeof args) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          const id = (input as { id: number }).id;
          const row = await getProjectSpecJournalEntry(pool, id);
          return row;
        })(args);
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "project_spec_journal_update",
    {
      description:
        "Update `body_markdown` (and optionally `keywords`) for a journal row — corrections only; does not change `backlog_issue_id` or `entry_kind`.",
      inputSchema: {
        id: z.number().int().positive(),
        body_markdown: z.string().min(1),
        keywords: z.array(z.string()).optional().describe("Replace keyword array when set."),
      },
    },
    async (args) =>
      runWithToolTiming("project_spec_journal_update", async () => {
        const envelope = await wrapTool(async (input: typeof args) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          const a = input as {
            id: number;
            body_markdown: string;
            keywords?: string[];
          };
          const row = await updateProjectSpecJournalEntry(pool, {
            id: a.id,
            body_markdown: a.body_markdown,
            keywords: a.keywords,
          });
          return row;
        })(args);
        return jsonResult(envelope);
      }),
  );
}
