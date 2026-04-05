/**
 * Postgres persistence for IA project spec journal (Decision Log + Lessons learned).
 * Optional: requires DATABASE_URL; callers handle null pool.
 */

import type { Pool } from "pg";
import {
  buildProjectSpecCloseoutDigest,
  extractProjectSpecSections,
  normalizeIssueId,
  suggestEnglishKeywords,
  type ProjectSpecSectionKey,
} from "../parser/project-spec-closeout-parse.js";

const STOPWORDS = new Set([
  "that",
  "this",
  "with",
  "from",
  "have",
  "been",
  "will",
  "when",
  "than",
  "into",
  "only",
  "also",
  "such",
  "must",
  "does",
  "each",
  "other",
  "what",
  "which",
  "while",
  "where",
  "after",
  "before",
  "without",
  "within",
  "between",
  "through",
  "about",
  "under",
  "above",
  "the",
  "and",
  "for",
  "are",
  "but",
  "not",
  "you",
  "all",
  "can",
  "her",
  "was",
  "one",
  "our",
  "out",
  "day",
  "get",
  "has",
  "him",
  "his",
  "how",
  "its",
  "may",
  "new",
  "now",
  "old",
  "see",
  "two",
  "way",
  "who",
  "boy",
  "did",
]);

/**
 * Merge digest summary keywords with tokens from a text snippet (deduped, lowercased for storage).
 */
export function mergeJournalKeywords(
  summaryKeywords: string[],
  bodySnippet: string,
  max = 32,
): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  const push = (raw: string) => {
    const t = raw.trim().toLowerCase();
    if (t.length < 4) return;
    if (/^\d+$/.test(t)) return;
    if (STOPWORDS.has(t)) return;
    if (seen.has(t)) return;
    seen.add(t);
    out.push(t);
  };
  for (const k of summaryKeywords) push(k);
  const slice = bodySnippet.slice(0, 800);
  const raw = slice.split(/[^A-Za-z0-9_\-/]+/);
  for (const w of raw) push(w);
  return out.slice(0, max);
}

export type JournalEntryKind = "decision_log" | "lessons_learned";

export type PersistJournalResult =
  | {
      ok: true;
      backlog_issue_id: string;
      source_spec_path: string;
      inserted: JournalEntryKind[];
      skipped_empty: JournalEntryKind[];
      git_sha: string | null;
    }
  | { ok: false; error: string; message: string };

/**
 * Insert journal rows for non-empty Decision Log and Lessons learned sections.
 */
export async function persistProjectSpecJournal(
  pool: Pool,
  params: {
    markdown: string;
    specPathPosix: string;
    issueId: string;
    gitSha?: string | null;
  },
): Promise<PersistJournalResult> {
  const issue = normalizeIssueId(params.issueId);
  const sections = extractProjectSpecSections(params.markdown);
  const digest = buildProjectSpecCloseoutDigest(
    params.markdown,
    params.specPathPosix,
    issue,
  );
  const baseKeywords = mergeJournalKeywords(
    digest.suggested_english_keywords,
    [
      sections.summary ?? "",
      sections.decision_log ?? "",
      sections.lessons_learned ?? "",
    ].join("\n"),
  );

  const toInsert: { kind: JournalEntryKind; body: string; keywords: string[] }[] =
    [];

  const pairs: { kind: JournalEntryKind; key: ProjectSpecSectionKey }[] = [
    { kind: "decision_log", key: "decision_log" },
    { kind: "lessons_learned", key: "lessons_learned" },
  ];

  const skipped_empty: JournalEntryKind[] = [];
  for (const { kind, key } of pairs) {
    const body = (sections[key] ?? "").trim();
    if (!body) {
      skipped_empty.push(kind);
      continue;
    }
    const kw = mergeJournalKeywords(baseKeywords, body, 32);
    toInsert.push({ kind, body, keywords: kw });
  }

  if (toInsert.length === 0) {
    return {
      ok: true,
      backlog_issue_id: issue,
      source_spec_path: params.specPathPosix.split("\\").join("/"),
      inserted: [],
      skipped_empty,
      git_sha: params.gitSha ?? null,
    };
  }

  const client = await pool.connect();
  try {
    const inserted: JournalEntryKind[] = [];
    for (const row of toInsert) {
      await client.query(
        `INSERT INTO ia_project_spec_journal
          (backlog_issue_id, entry_kind, body_markdown, keywords, source_spec_path, git_sha)
         VALUES ($1, $2, $3, $4, $5, $6)`,
        [
          issue,
          row.kind,
          row.body,
          row.keywords,
          params.specPathPosix.split("\\").join("/"),
          params.gitSha ?? null,
        ],
      );
      inserted.push(row.kind);
    }
    return {
      ok: true,
      backlog_issue_id: issue,
      source_spec_path: params.specPathPosix.split("\\").join("/"),
      inserted,
      skipped_empty,
      git_sha: params.gitSha ?? null,
    };
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { ok: false, error: "db_error", message: msg };
  } finally {
    client.release();
  }
}

export type SearchHit = {
  id: number;
  backlog_issue_id: string;
  entry_kind: JournalEntryKind;
  excerpt: string;
  rank: number;
  recorded_at: string;
  keywords: string[];
};

/**
 * Full-text search over journal bodies (English `plainto_tsquery`).
 */
export async function searchProjectSpecJournal(
  pool: Pool,
  params: {
    query: string;
    limit: number;
    kinds?: JournalEntryKind[];
    issueId?: string;
  },
): Promise<SearchHit[] | { error: string; message: string }> {
  const q = params.query.trim();
  if (!q) {
    return { error: "invalid_query", message: "`query` must be non-empty." };
  }
  const limit = Math.min(Math.max(1, params.limit | 0), 50);

  const kinds =
    params.kinds && params.kinds.length > 0
      ? params.kinds
      : (["decision_log", "lessons_learned"] as const);

  const client = await pool.connect();
  try {
    let sql = `
      SELECT id, backlog_issue_id, entry_kind,
             left(body_markdown, 480) AS excerpt,
             ts_rank(body_tsv, plainto_tsquery('english', $1)) AS rank,
             recorded_at,
             keywords
      FROM ia_project_spec_journal
      WHERE body_tsv @@ plainto_tsquery('english', $1)
        AND entry_kind = ANY($2::text[])`;
    const args: unknown[] = [q, kinds];
    if (params.issueId) {
      sql += ` AND backlog_issue_id = $3`;
      args.push(normalizeIssueId(params.issueId));
    }
    sql += ` ORDER BY rank DESC, recorded_at DESC LIMIT $${args.length + 1}`;
    args.push(limit);

    const { rows } = await client.query(sql, args);
    return rows.map((r) => ({
      id: Number(r.id),
      backlog_issue_id: String(r.backlog_issue_id),
      entry_kind: r.entry_kind as JournalEntryKind,
      excerpt: String(r.excerpt ?? ""),
      rank: Number(r.rank),
      recorded_at:
        r.recorded_at instanceof Date
          ? r.recorded_at.toISOString()
          : String(r.recorded_at),
      keywords: Array.isArray(r.keywords) ? (r.keywords as string[]) : [],
    }));
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { error: "db_error", message: msg };
  } finally {
    client.release();
  }
}

/**
 * Keyword overlap search (GIN on `keywords`) — complements full-text for short tokens.
 */
export async function searchProjectSpecJournalByKeywords(
  pool: Pool,
  params: { tokens: string[]; limit: number },
): Promise<SearchHit[] | { error: string; message: string }> {
  const tokens = params.tokens
    .map((t) => t.trim().toLowerCase())
    .filter((t) => t.length >= 3)
    .slice(0, 24);
  if (tokens.length === 0) {
    return {
      error: "invalid_query",
      message: "`tokens` must contain at least one token (length ≥ 3).",
    };
  }
  const limit = Math.min(Math.max(1, params.limit | 0), 50);
  const client = await pool.connect();
  try {
    const { rows } = await client.query(
      `SELECT id, backlog_issue_id, entry_kind,
              left(body_markdown, 480) AS excerpt,
              1.0::float AS rank,
              recorded_at,
              keywords
       FROM ia_project_spec_journal
       WHERE keywords && $1::text[]
       ORDER BY recorded_at DESC
       LIMIT $2::integer`,
      [tokens, limit],
    );
    return rows.map((r) => ({
      id: Number(r.id),
      backlog_issue_id: String(r.backlog_issue_id),
      entry_kind: r.entry_kind as JournalEntryKind,
      excerpt: String(r.excerpt ?? ""),
      rank: Number(r.rank),
      recorded_at:
        r.recorded_at instanceof Date
          ? r.recorded_at.toISOString()
          : String(r.recorded_at),
      keywords: Array.isArray(r.keywords) ? (r.keywords as string[]) : [],
    }));
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { error: "db_error", message: msg };
  } finally {
    client.release();
  }
}

export type JournalRowFull = {
  id: number;
  backlog_issue_id: string;
  entry_kind: JournalEntryKind;
  body_markdown: string;
  keywords: string[];
  source_spec_path: string;
  recorded_at: string;
  git_sha: string | null;
};

export async function getProjectSpecJournalEntry(
  pool: Pool,
  id: number,
): Promise<JournalRowFull | { error: string; message: string }> {
  if (!Number.isFinite(id) || id < 1) {
    return { error: "invalid_id", message: "`id` must be a positive integer." };
  }
  const client = await pool.connect();
  try {
    const { rows } = await client.query(
      `SELECT id, backlog_issue_id, entry_kind, body_markdown, keywords,
              source_spec_path, recorded_at, git_sha
       FROM ia_project_spec_journal WHERE id = $1`,
      [id],
    );
    if (rows.length === 0) {
      return { error: "not_found", message: `No row with id ${id}.` };
    }
    const r = rows[0];
    return {
      id: Number(r.id),
      backlog_issue_id: String(r.backlog_issue_id),
      entry_kind: r.entry_kind as JournalEntryKind,
      body_markdown: String(r.body_markdown ?? ""),
      keywords: Array.isArray(r.keywords) ? (r.keywords as string[]) : [],
      source_spec_path: String(r.source_spec_path ?? ""),
      recorded_at:
        r.recorded_at instanceof Date
          ? r.recorded_at.toISOString()
          : String(r.recorded_at),
      git_sha: r.git_sha != null ? String(r.git_sha) : null,
    };
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { error: "db_error", message: msg };
  } finally {
    client.release();
  }
}

export type UpdateJournalResult =
  | JournalRowFull
  | { error: string; message: string };

/**
 * Update body and optional keywords (e.g. redaction or typo fix); does not change `entry_kind` or `backlog_issue_id`.
 */
export async function updateProjectSpecJournalEntry(
  pool: Pool,
  params: {
    id: number;
    body_markdown: string;
    keywords?: string[] | null;
  },
): Promise<UpdateJournalResult> {
  const id = params.id | 0;
  if (!Number.isFinite(id) || id < 1) {
    return { error: "invalid_id", message: "`id` must be a positive integer." };
  }
  const body = params.body_markdown.trim();
  if (!body) {
    return { error: "invalid_body", message: "`body_markdown` must be non-empty." };
  }

  const client = await pool.connect();
  try {
    if (params.keywords != null) {
      const { rows } = await client.query(
        `UPDATE ia_project_spec_journal
         SET body_markdown = $2, keywords = $3
         WHERE id = $1
         RETURNING id, backlog_issue_id, entry_kind, body_markdown, keywords,
                   source_spec_path, recorded_at, git_sha`,
        [id, body, params.keywords],
      );
      if (rows.length === 0) {
        return { error: "not_found", message: `No row with id ${id}.` };
      }
      const r = rows[0];
      return mapReturningRow(r);
    }
    const { rows } = await client.query(
      `UPDATE ia_project_spec_journal
       SET body_markdown = $2
       WHERE id = $1
       RETURNING id, backlog_issue_id, entry_kind, body_markdown, keywords,
                 source_spec_path, recorded_at, git_sha`,
      [id, body],
    );
    if (rows.length === 0) {
      return { error: "not_found", message: `No row with id ${id}.` };
    }
    return mapReturningRow(rows[0]);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    return { error: "db_error", message: msg };
  } finally {
    client.release();
  }
}

function mapReturningRow(r: Record<string, unknown>): JournalRowFull {
  return {
    id: Number(r.id),
    backlog_issue_id: String(r.backlog_issue_id),
    entry_kind: r.entry_kind as JournalEntryKind,
    body_markdown: String(r.body_markdown ?? ""),
    keywords: Array.isArray(r.keywords) ? (r.keywords as string[]) : [],
    source_spec_path: String(r.source_spec_path ?? ""),
    recorded_at:
      r.recorded_at instanceof Date
        ? r.recorded_at.toISOString()
        : String(r.recorded_at),
    git_sha: r.git_sha != null ? String(r.git_sha) : null,
  };
}

/** @internal */
export function tokenizeForJournalSearch(text: string, maxTokens = 12): string[] {
  const slice = text.slice(0, 500);
  const raw = slice.split(/[^A-Za-z0-9_\-]+/);
  const out: string[] = [];
  const seen = new Set<string>();
  for (const w of raw) {
    const t = w.trim().toLowerCase();
    if (t.length < 4) continue;
    if (STOPWORDS.has(t)) continue;
    if (seen.has(t)) continue;
    seen.add(t);
    out.push(t);
    if (out.length >= maxTokens) break;
  }
  return out;
}
