/**
 * MCP tools: project_spec_journal_* — Postgres journal for Decision Log + Lessons learned (glossary **IA project spec journal**).
 */

import fs from "node:fs";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
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
import { runWithToolTiming } from "../instrumentation.js";
import { resolveProjectSpecFile } from "../parser/project-spec-closeout-parse.js";

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
        "Append **Decision Log** and **Lessons learned** bodies from a project spec under `ia/projects/{ISSUE_ID}[-{description}].md` (or legacy `.cursor/projects/{ISSUE_ID}.md`) into **Postgres** table `ia_project_spec_journal` (requires `DATABASE_URL` or committed `config/postgres-dev.json` when not in CI). Use during **project-spec-close** (umbrella close) **before** deleting the spec, or during **project-stage-close** to capture per-stage progress. Inserts one row per non-empty section. Idempotent re-runs append additional rows.",
      inputSchema: {
        issue_id: z
          .string()
          .optional()
          .describe(
            "Backlog id. Exactly one of `issue_id` or `spec_path` required.",
          ),
        spec_path: z
          .string()
          .optional()
          .describe(
            "Repo-relative path under `ia/projects/` or `.cursor/projects/` (legacy). Filename may be `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md` (descriptive suffix). Exactly one of `issue_id` or `spec_path` required.",
          ),
        git_sha: z
          .string()
          .optional()
          .describe("Optional git commit SHA for traceability."),
      },
    },
    async (args) =>
      runWithToolTiming("project_spec_journal_persist", async () => {
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            error: "db_unconfigured",
            message:
              "No database URL: set DATABASE_URL, add config/postgres-dev.json, or see docs/postgres-ia-dev-setup.md.",
          });
        }
        const a = args as {
          issue_id?: string;
          spec_path?: string;
          git_sha?: string;
        };
        const repoRoot = resolveRepoRoot();
        const resolved = resolveProjectSpecFile(repoRoot, {
          issue_id: a.issue_id,
          spec_path: a.spec_path,
        });
        if (!resolved.ok) {
          return jsonResult({
            error: resolved.error,
            message: resolved.message,
          });
        }
        let markdown: string;
        try {
          markdown = fs.readFileSync(resolved.absPath, "utf8");
        } catch (e) {
          const msg = e instanceof Error ? e.message : String(e);
          return jsonResult({
            error: "read_failed",
            message: msg,
            spec_path: resolved.relPosix,
          });
        }
        const result = await persistProjectSpecJournal(pool, {
          markdown,
          specPathPosix: resolved.relPosix,
          issueId: resolved.issue_id,
          gitSha: a.git_sha ?? null,
        });
        return jsonResult(result);
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
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            error: "db_unconfigured",
            message:
              "No database URL: set DATABASE_URL or use config/postgres-dev.json (skipped in CI).",
          });
        }
        const a = args as {
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

        if (q) {
          const ft = await searchProjectSpecJournal(pool, {
            query: q,
            limit: max,
            kinds,
            issueId: a.backlog_issue_id,
          });
          if ("error" in ft && ft.error) {
            return jsonResult(ft);
          }
          out.full_text_hits = ft;
        }

        if (tokens.length > 0) {
          const kw = await searchProjectSpecJournalByKeywords(pool, {
            tokens,
            limit: max,
          });
          if ("error" in kw && kw.error) {
            return jsonResult(kw);
          }
          out.keyword_hits = kw;
        }

        if (!q && tokens.length === 0) {
          return jsonResult({
            error: "invalid_arguments",
            message:
              "Provide `query`, `keyword_tokens`, and/or `raw_text_for_tokens`.",
          });
        }

        if (!q && tokens.length > 0) {
          out.note =
            "Keyword overlap only; add `query` for full-text when you have a short English phrase.";
        }

        return jsonResult(out);
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
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            error: "db_unconfigured",
            message: "No database URL (env or config/postgres-dev.json).",
          });
        }
        const id = (args as { id: number }).id;
        const row = await getProjectSpecJournalEntry(pool, id);
        return jsonResult(row);
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
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            error: "db_unconfigured",
            message: "No database URL (env or config/postgres-dev.json).",
          });
        }
        const a = args as {
          id: number;
          body_markdown: string;
          keywords?: string[];
        };
        const row = await updateProjectSpecJournalEntry(pool, {
          id: a.id,
          body_markdown: a.body_markdown,
          keywords: a.keywords,
        });
        return jsonResult(row);
      }),
  );
}
