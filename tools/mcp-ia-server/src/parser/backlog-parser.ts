/**
 * Parse BACKLOG.md for a single issue block (supports nested checklist items).
 */

import fs from "node:fs";
import path from "node:path";
import { splitLines } from "./markdown-parser.js";

const BACKLOG_FILE = "BACKLOG.md";

/** Checklist item with **ISSUE_ID** (BUG-37, FEAT-37b, TECH-01, …). */
const CHECKLIST_HEADER =
  /^(\s*)-\s+\[([ x])\]\s+\*\*([A-Z]+-\d+[a-z]*)\*\*\s*(.*)$/;

const FIELD_LINE =
  /^\s+-\s+(Type|Files|Spec|Notes|Acceptance|Depends on|Proposed solution):\s*(.*)$/i;

export type BacklogIssueStatus = "open" | "completed";

export interface ParsedBacklogIssue {
  issue_id: string;
  title: string;
  status: BacklogIssueStatus;
  backlog_section: string;
  type?: string;
  files?: string;
  spec?: string;
  notes?: string;
  acceptance?: string;
  depends_on?: string;
  proposed_solution?: string;
  raw_markdown: string;
}

/**
 * Normalize user input (e.g. `bug-37`, `FEAT-37B`) to canonical backlog id form.
 */
export function normalizeIssueId(issueId: string): string {
  const t = issueId.trim();
  const m = t.match(/^([A-Za-z]+)-(\d+)([a-zA-Z]*)$/);
  if (!m) return t;
  return `${m[1]!.toUpperCase()}-${m[2]}${m[3]!.toLowerCase()}`;
}

/**
 * Leading whitespace length before the first non-space character.
 */
function leadingWhitespaceLen(line: string): number {
  const m = line.match(/^(\s*)/);
  return m ? m[1]!.length : 0;
}

/**
 * Extract title from the remainder after `**ID**` on the header line (em dash or hyphen).
 */
function parseTitleFromHeaderRest(rest: string): string {
  const t = rest.trim();
  const afterDash = t.replace(/^(?:—|--)\s*/, "").trim();
  return afterDash;
}

/**
 * Find line index of issue header and current `##` section name.
 */
export function findIssueHeaderLine(
  lines: string[],
  issueId: string,
): { lineIndex: number; backlog_section: string } | null {
  const canonical = normalizeIssueId(issueId);
  if (!canonical) return null;

  let backlog_section = "";
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    const h = line.match(/^##+\s+(.+)$/);
    if (h) {
      backlog_section = h[1]!.trim();
      continue;
    }
    const m = line.match(CHECKLIST_HEADER);
    if (m && m[3] === canonical) {
      return { lineIndex: i, backlog_section };
    }
  }
  return null;
}

/**
 * Slice lines for one issue block using indent + sibling checklist rule.
 */
export function sliceIssueBlock(lines: string[], startIndex: number): string[] {
  const startLine = lines[startIndex]!;
  const baseIndent = leadingWhitespaceLen(startLine);
  const out: string[] = [startLine];
  for (let j = startIndex + 1; j < lines.length; j++) {
    const line = lines[j]!;
    const cm = line.match(CHECKLIST_HEADER);
    if (cm) {
      const ind = leadingWhitespaceLen(line);
      if (ind <= baseIndent) break;
    }
    out.push(line);
  }
  return out;
}

/**
 * Scrape optional sub-fields from block body (first line wins per key).
 */
export function scrapeIssueFields(blockLines: string[]): Omit<
  ParsedBacklogIssue,
  | "issue_id"
  | "title"
  | "status"
  | "backlog_section"
  | "raw_markdown"
> & {
  type?: string;
  files?: string;
  spec?: string;
  notes?: string;
  acceptance?: string;
  depends_on?: string;
  proposed_solution?: string;
} {
  const fields: Record<string, string> = {};
  for (const line of blockLines.slice(1)) {
    const fm = line.match(FIELD_LINE);
    if (!fm) continue;
    const key = fm[1]!.toLowerCase();
    const mapKey =
      key === "depends on"
        ? "depends_on"
        : key === "proposed solution"
          ? "proposed_solution"
          : key;
    if (fields[mapKey] !== undefined) continue;
    fields[mapKey] = fm[2]!.trim();
  }
  return {
    type: fields.type,
    files: fields.files,
    spec: fields.spec,
    notes: fields.notes,
    acceptance: fields.acceptance,
    depends_on: fields.depends_on,
    proposed_solution: fields.proposed_solution,
  };
}

/**
 * Parse BACKLOG.md for `issueId`; returns null if not found.
 */
export function parseBacklogIssue(
  repoRoot: string,
  issueId: string,
): ParsedBacklogIssue | null {
  const filePath = path.join(repoRoot, BACKLOG_FILE);
  if (!fs.existsSync(filePath)) return null;

  const raw = fs.readFileSync(filePath, "utf8");
  const lines = splitLines(raw);
  const found = findIssueHeaderLine(lines, issueId);
  if (!found) return null;

  const blockLines = sliceIssueBlock(lines, found.lineIndex);
  const header = blockLines[0]!.match(CHECKLIST_HEADER);
  if (!header) return null;

  const status: BacklogIssueStatus = header[2] === "x" ? "completed" : "open";
  const id = header[3]!;
  const title = parseTitleFromHeaderRest(header[4] ?? "");
  const fieldParts = scrapeIssueFields(blockLines);

  return {
    issue_id: id,
    title,
    status,
    backlog_section: found.backlog_section,
    ...fieldParts,
    raw_markdown: blockLines.join("\n"),
  };
}
