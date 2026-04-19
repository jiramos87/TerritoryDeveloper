/**
 * Parse temporary project specs (`ia/projects/{ISSUE_ID}[-{description}].md`)
 * for closeout / agent workflows.
 */

import fs from "node:fs";
import path from "node:path";

/** Backlog issue id pattern (aligned with BACKLOG convention). */
export const PROJECT_SPEC_ISSUE_ID_RE =
  /^(BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?$/i;

/** Citation scan: issue tokens in prose (case-insensitive). */
export const CITED_ISSUE_ID_RE =
  /\b(BUG|FEAT|TECH|ART|AUDIO)-(\d+)([a-z]?)\b/gi;

/**
 * Repo-relative project spec path. Accepts:
 *   - `ia/projects/{ISSUE_ID}.md`
 *   - `ia/projects/{ISSUE_ID}-{description}.md` (descriptive variant)
 * Never matches traversal (`..` is rejected separately by callers).
 */
export const PROJECT_SPEC_REL_PATH_RE =
  /^ia\/projects\/(BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?(?:-[A-Za-z0-9._-]+)?\.md$/i;

/**
 * Pull the bare `{ISSUE_ID}` out of a project spec basename
 * such as `TECH-11-descriptive-suffix` or `BUG-12`. Returns null if the
 * basename does not start with a recognised issue id.
 */
export function issueIdFromProjectSpecBasename(base: string): string | null {
  const m =
    /^((?:BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?)(?:-[A-Za-z0-9._-]+)?$/i.exec(base);
  if (!m) return null;
  return normalizeIssueId(m[1]);
}

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
  "agent",
  "agents",
  "issue",
  "issues",
  "spec",
  "specs",
  "markdown",
  "closeout",
  "project",
  "backlog",
]);

export type ProjectSpecSectionKey =
  | "summary"
  | "goals"
  | "user_stories"
  | "current_state"
  | "proposed_design"
  | "decision_log"
  | "implementation_plan"
  | "acceptance"
  | "issues_found"
  | "lessons_learned"
  | "open_questions"
  | "audit"
  | "code_review"
  | "code_fix_plan"
  | "closeout_plan";

export type ChecklistHints = Partial<
  Record<"G1" | "R1" | "A1" | "U1" | "D1" | "M1" | "I1", string[]>
> & { _note?: string };

export type ProjectSpecCloseoutDigest = {
  schema_version: 1;
  issue_id: string | null;
  spec_path: string;
  sections: Partial<Record<ProjectSpecSectionKey, string>>;
  cited_issue_ids: string[];
  suggested_english_keywords: string[];
  checklist_hints?: ChecklistHints;
};

/**
 * Classify an H2 title (with or without numeric prefix) into a logical section key.
 */
export function sectionKeyFromH2Title(title: string): ProjectSpecSectionKey | null {
  const t = title.trim();
  const lower = t.toLowerCase();
  if (/^1\.\s*summary/i.test(t) || lower === "summary") return "summary";
  if (/^2\.\s*goals/i.test(t) || lower.startsWith("goals and non-goals"))
    return "goals";
  if (/^3\.\s*user/i.test(t) || lower.includes("developer stories"))
    return "user_stories";
  if (/^4\.\s*current state/i.test(t)) return "current_state";
  if (/^5\.\s*proposed design/i.test(t)) return "proposed_design";
  if (/^6\.\s*decision log/i.test(t) || lower === "decision log")
    return "decision_log";
  if (/^7\.\s*implementation plan/i.test(t)) return "implementation_plan";
  if (/^8\.\s*acceptance/i.test(t)) return "acceptance";
  if (/^9\.\s*issues found/i.test(t)) return "issues_found";
  if (/^10\.\s*lessons/i.test(t) || lower.startsWith("lessons learned"))
    return "lessons_learned";
  if (/^open questions/i.test(t)) return "open_questions";
  if (/^audit$/i.test(t)) return "audit";
  if (/^code review$/i.test(t)) return "code_review";
  if (/^code fix plan$/i.test(t)) return "code_fix_plan";
  if (/^closeout plan$/i.test(t)) return "closeout_plan";
  return null;
}

/**
 * Split markdown body into top-level `##` sections (not `###`).
 */
export function splitProjectSpecH2Sections(
  markdown: string,
): { title: string; body: string }[] {
  const lines = markdown.split(/\r?\n/);
  const out: { title: string; body: string[] }[] = [];
  let current: { title: string; body: string[] } | null = null;

  for (const line of lines) {
    const m = /^## (.+)$/.exec(line);
    if (m) {
      if (current) out.push(current);
      current = { title: m[1].trim(), body: [] };
    } else if (current) {
      current.body.push(line);
    }
  }
  if (current) out.push(current);

  return out.map((s) => ({
    title: s.title,
    body: s.body.join("\n").trim(),
  }));
}

/**
 * Extract `BUG-` / `FEAT-` / … ids from markdown; unique, sorted.
 */
export function extractCitedIssueIds(markdown: string): string[] {
  const seen = new Set<string>();
  let m: RegExpExecArray | null;
  const re = new RegExp(CITED_ISSUE_ID_RE.source, CITED_ISSUE_ID_RE.flags);
  while ((m = re.exec(markdown)) !== null) {
    const prefix = m[1].toUpperCase();
    const num = m[2];
    const suffix = (m[3] ?? "").toLowerCase();
    const id = `${prefix}-${num}${suffix}`;
    seen.add(id);
  }
  return [...seen].sort();
}

/**
 * Parse `> **Issue:** [TECH-58](...)` or `> **Issue:** TECH-58` from the header block.
 */
export function parseIssueIdFromSpecHeader(markdown: string): string | null {
  const head = markdown.slice(0, 2500);
  const m = />\s*\*\*Issue:\*\*\s*\[?([A-Z]+-\d+[a-z]?)\]?/i.exec(head);
  if (m) {
    const id = m[1].toUpperCase();
    if (PROJECT_SPEC_ISSUE_ID_RE.test(id)) return normalizeIssueId(id);
  }
  const h1 = /^#\s+([A-Z]+-\d+[a-z]?)\s+—/im.exec(markdown);
  if (h1) {
    const id = h1[1].toUpperCase();
    if (PROJECT_SPEC_ISSUE_ID_RE.test(id)) return normalizeIssueId(id);
  }
  return null;
}

/** Normalize `tech-58` → `TECH-58`, `TECH-55B` → `TECH-55b`. */
export function normalizeIssueId(id: string): string {
  const m = /^([A-Z]+)-(\d+)([a-z]?)$/i.exec(id.trim());
  if (!m) return id.trim().toUpperCase();
  return `${m[1].toUpperCase()}-${m[2]}${(m[3] ?? "").toLowerCase()}`;
}

/**
 * Heuristic tokens for `glossary_discover` from the Summary section.
 */
export function suggestEnglishKeywords(summaryText: string): string[] {
  const slice = summaryText.slice(0, 600);
  const raw = slice.split(/[^A-Za-z0-9_\-/]+/);
  const out: string[] = [];
  const seen = new Set<string>();
  for (const w of raw) {
    const t = w.trim();
    if (t.length < 4) continue;
    if (/^\d+$/.test(t)) continue;
    if (/[/*[\]`]/.test(t) || t.includes("/") || t.toLowerCase().endsWith(".md"))
      continue;
    const lower = t.toLowerCase();
    if (STOPWORDS.has(lower)) continue;
    if (!seen.has(lower)) {
      seen.add(lower);
      out.push(t);
    }
    if (out.length >= 15) break;
  }
  return out;
}

function extractBulletLines(text: string, max: number): string[] {
  const lines = text.split(/\r?\n/);
  const out: string[] = [];
  for (const line of lines) {
    const t = line.trim();
    const bullet = /^(?:[-*]|\d+\.)\s+(.+)/.exec(t);
    if (bullet) {
      const s = bullet[1].trim();
      if (s) out.push(s);
    }
    if (out.length >= max) break;
  }
  return out;
}

function linesMatching(
  text: string,
  re: RegExp,
  max: number,
): string[] {
  const out: string[] = [];
  for (const line of text.split(/\r?\n/)) {
    const t = line.trim();
    if (!t) continue;
    if (re.test(line)) {
      out.push(t);
      if (out.length >= max) break;
    }
    re.lastIndex = 0;
  }
  return out;
}

/**
 * Optional G1–I1 hints: extracted lines only (heuristic; agent validates).
 */
export function buildChecklistHints(
  sections: Partial<Record<ProjectSpecSectionKey, string>>,
): ChecklistHints {
  const lessons = sections.lessons_learned ?? "";
  const openQ = sections.open_questions ?? "";
  const decision = sections.decision_log ?? "";
  const impl = sections.implementation_plan ?? "";
  const summary = sections.summary ?? "";
  const goals = sections.goals ?? "";

  return {
    _note:
      "Heuristic extraction from project spec sections; agent maps to G1–I1 per project-spec-close checklist.",
    G1: [
      ...extractBulletLines(openQ, 8),
      ...extractBulletLines(lessons, 8),
    ].slice(0, 12),
    R1: linesMatching(decision, /\|/, 12),
    A1: linesMatching(
      `${summary}\n${goals}`,
      /ARCHITECTURE|architecture\.md|managers?/i,
      8,
    ),
    U1: linesMatching(
      `${summary}\n${goals}\n${impl}`,
      /ia\/rules|guardrail/i,
      8,
    ),
    D1: linesMatching(
      `${summary}\n${goals}\n${impl}`,
      /docs\//i,
      8,
    ),
    M1: linesMatching(
      `${summary}\n${impl}`,
      /mcp-ia-server|territory-ia|`[a-z_]+`/i,
      8,
    ),
    I1: linesMatching(
      `${summary}\n${impl}`,
      /generate:ia-indexes|IA index|spec-index|glossary-index/i,
      8,
    ),
  };
}

/**
 * Build structured sections map from raw project spec markdown.
 */
export function extractProjectSpecSections(
  markdown: string,
): Partial<Record<ProjectSpecSectionKey, string>> {
  const h2 = splitProjectSpecH2Sections(markdown);
  const sections: Partial<Record<ProjectSpecSectionKey, string>> = {};
  for (const { title, body } of h2) {
    const key = sectionKeyFromH2Title(title);
    if (key) sections[key] = body;
  }
  return sections;
}

/**
 * Full v1 digest from markdown + resolved relative spec path.
 */
export function buildProjectSpecCloseoutDigest(
  markdown: string,
  specPathPosix: string,
  issueIdOverride: string | null,
): ProjectSpecCloseoutDigest {
  const sections = extractProjectSpecSections(markdown);
  const fromHeader = parseIssueIdFromSpecHeader(markdown);
  const issue_id =
    issueIdOverride != null
      ? normalizeIssueId(issueIdOverride)
      : fromHeader;

  const cited = extractCitedIssueIds(markdown);
  const keywords = suggestEnglishKeywords(sections.summary ?? "");

  return {
    schema_version: 1,
    issue_id,
    spec_path: specPathPosix.split(path.sep).join("/"),
    sections,
    cited_issue_ids: cited,
    suggested_english_keywords: keywords,
    checklist_hints: buildChecklistHints(sections),
  };
}

export type ResolveProjectSpecPathResult =
  | {
      ok: true;
      absPath: string;
      relPosix: string;
      issue_id: string;
    }
  | { ok: false; error: string; message: string };

/**
 * Resolve and validate a project spec path under `repoRoot` with no `..` traversal.
 *
 * Accepts:
 *   - `spec_path` repo-relative under `ia/projects/`. Filename may be
 *     `{ISSUE_ID}.md` or `{ISSUE_ID}-{description}.md` (descriptive suffix).
 *   - `issue_id` only — resolution order:
 *       1. `ia/projects/{ISSUE_ID}.md`
 *       2. `ia/projects/{ISSUE_ID}-{description}.md` (first descriptive match by sort)
 *       3. Default to `ia/projects/{ISSUE_ID}.md` so the caller's read produces ENOENT.
 */
export function resolveProjectSpecFile(
  repoRoot: string,
  params: { issue_id?: string; spec_path?: string },
): ResolveProjectSpecPathResult {
  const hasIssue = params.issue_id != null && String(params.issue_id).trim() !== "";
  const hasPath = params.spec_path != null && String(params.spec_path).trim() !== "";

  if (hasIssue === hasPath) {
    if (!hasIssue) {
      return {
        ok: false,
        error: "invalid_arguments",
        message:
          "Provide exactly one of `issue_id` or `spec_path` (repo-relative `ia/projects/{ISSUE_ID}[-{description}].md`).",
      };
    }
  }

  let relPosix: string;
  let issue_id: string;

  if (hasPath) {
    let p = String(params.spec_path).trim().split(path.sep).join("/");
    if (p.includes("..")) {
      return {
        ok: false,
        error: "invalid_path",
        message: "`spec_path` must not contain `..`.",
      };
    }
    if (!p.startsWith("ia/")) {
      p = path.posix.join("ia/projects", path.basename(p));
    }
    if (!PROJECT_SPEC_REL_PATH_RE.test(p)) {
      return {
        ok: false,
        error: "invalid_path",
        message:
          "`spec_path` must match `ia/projects/{BUG|FEAT|TECH|ART|AUDIO}-<n>[suffix][-{description}].md`.",
      };
    }
    relPosix = p;
    const base = path.posix.basename(p, ".md");
    const derived = issueIdFromProjectSpecBasename(base);
    if (!derived) {
      return {
        ok: false,
        error: "invalid_path",
        message: "Could not derive a valid `issue_id` from `spec_path`.",
      };
    }
    issue_id = derived;
    if (hasIssue) {
      const other = normalizeIssueId(String(params.issue_id).trim());
      if (other !== issue_id) {
        return {
          ok: false,
          error: "mismatch",
          message: `\`issue_id\` (${other}) does not match \`spec_path\` (${issue_id}).`,
        };
      }
    }
  } else {
    issue_id = normalizeIssueId(String(params.issue_id).trim());
    if (!PROJECT_SPEC_ISSUE_ID_RE.test(issue_id)) {
      return {
        ok: false,
        error: "invalid_issue_id",
        message:
          "`issue_id` must match `BUG-|FEAT-|TECH-|ART-|AUDIO-` + digits + optional letter suffix.",
      };
    }

    const tryAbs = (rel: string) =>
      path.resolve(repoRoot, ...rel.split("/"));

    relPosix = `ia/projects/${issue_id}.md`;
    if (!fs.existsSync(tryAbs(relPosix))) {
      let pickedDescriptive: string | null = null;
      try {
        const iaProjectsDir = path.join(repoRoot, "ia", "projects");
        if (fs.existsSync(iaProjectsDir)) {
          const prefix = `${issue_id}-`;
          const entries = fs
            .readdirSync(iaProjectsDir)
            .filter(
              (n) =>
                n.startsWith(prefix) &&
                n.endsWith(".md") &&
                PROJECT_SPEC_REL_PATH_RE.test(`ia/projects/${n}`),
            )
            .sort();
          if (entries.length > 0) {
            pickedDescriptive = entries[0];
          }
        }
      } catch {
        // ignore directory read failures — fall through to legacy/default
      }

      if (pickedDescriptive) {
        relPosix = `ia/projects/${pickedDescriptive}`;
      }
      // else: keep the default `ia/projects/{ID}.md`; the caller's read will ENOENT honestly.
    }
  }

  const absPath = path.resolve(repoRoot, ...relPosix.split("/"));
  const resolvedRoot = path.resolve(repoRoot);
  if (!absPath.startsWith(resolvedRoot + path.sep) && absPath !== resolvedRoot) {
    return {
      ok: false,
      error: "invalid_path",
      message: "Resolved path escapes repository root.",
    };
  }

  return { ok: true, absPath, relPosix, issue_id };
}
