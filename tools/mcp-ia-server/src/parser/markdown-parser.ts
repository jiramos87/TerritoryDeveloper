/**
 * Core Markdown / MDC parser: frontmatter (gray-matter), heading tree, absolute line ranges.
 */

import fs from "node:fs";
import path from "node:path";
import matter from "gray-matter";
import type { HeadingNode, ParsedDocument } from "./types.js";

const documentParseCache = new Map<string, ParsedDocument>();

/**
 * Clear the in-memory {@link parseDocument} cache (e.g. between tests).
 */
export function clearDocumentParseCache(): void {
  documentParseCache.clear();
}

const HEADING_LINE = /^\s{0,3}(#{1,6})\s+(.+?)\s*$/;
const SECTION_ID_NUMERIC = /^(?:§\s*)?(\d+(?:\.\d+)*)\b/;

/**
 * Split file text into lines (physical lines; 1-based indexing uses index + 1).
 */
export function splitLines(content: string): string[] {
  if (content.length === 0) return [""];
  return content.split(/\r?\n/);
}

/**
 * First 1-based line index of the Markdown body after YAML frontmatter, or 1 if none.
 */
export function getBodyStartLine1Based(lines: string[]): number {
  if (lines.length === 0) return 1;
  if (lines[0]?.trim() !== "---") return 1;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i]?.trim() === "---") return i + 2;
  }
  return 1;
}

/**
 * Extract YAML frontmatter via gray-matter; body string is excluded from metadata.
 */
export function extractFrontmatter(content: string): {
  frontmatter: Record<string, unknown> | null;
  body: string;
  hadFrontmatterBlock: boolean;
} {
  const hadFrontmatterBlock = content.startsWith("---");
  const parsed = matter(content);
  const data = parsed.data as Record<string, unknown>;
  const fm =
    hadFrontmatterBlock && data && typeof data === "object"
      ? { ...data }
      : null;
  return {
    frontmatter: fm,
    body: parsed.content,
    hadFrontmatterBlock,
  };
}

/**
 * Leading numeric section id (e.g. "13.4") or slugified full title.
 */
export function extractSectionId(title: string): string {
  const trimmed = title.trim();
  const m = trimmed.match(SECTION_ID_NUMERIC);
  if (m?.[1]) return m[1];
  return slugifyTitle(trimmed);
}

function slugifyTitle(title: string): string {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-+/g, "-");
}

export interface FlatHeading {
  depth: number;
  title: string;
  sectionId: string;
  lineStart: number;
}

/**
 * Scan physical file lines from body onward; lineStart is absolute (1-based).
 */
export function collectHeadings(
  lines: string[],
  bodyStartLine1Based: number,
): FlatHeading[] {
  const startIdx = Math.max(0, bodyStartLine1Based - 1);
  const out: FlatHeading[] = [];
  for (let i = startIdx; i < lines.length; i++) {
    const m = lines[i].match(HEADING_LINE);
    if (!m) continue;
    const depth = m[1]!.length;
    const title = m[2]!.trim();
    out.push({
      depth,
      title,
      sectionId: extractSectionId(title),
      lineStart: i + 1,
    });
  }
  return out;
}

export type FlatHeadingWithRange = FlatHeading & { lineEnd: number };

/**
 * Assign lineEnd: last line before next heading of depth ≤ current, else EOF.
 */
export function assignLineEnds(
  flat: FlatHeading[],
  lineCount: number,
): FlatHeadingWithRange[] {
  const n = flat.length;
  if (n === 0) return [];
  const lineEnd = new Array<number>(n);
  for (let i = 0; i < n; i++) {
    let end = lineCount;
    const d = flat[i]!.depth;
    for (let j = i + 1; j < n; j++) {
      if (flat[j]!.depth <= d) {
        end = flat[j]!.lineStart - 1;
        break;
      }
    }
    lineEnd[i] = end;
  }
  return flat.map((h, i) => ({ ...h, lineEnd: lineEnd[i]! }));
}

/**
 * Build parent/child tree from ordered flat headings with lineEnd set.
 */
export function flatToTree(flat: FlatHeadingWithRange[]): HeadingNode[] {
  const root: HeadingNode[] = [];
  const stack: HeadingNode[] = [];

  for (const h of flat) {
    const node: HeadingNode = {
      depth: h.depth,
      title: h.title,
      sectionId: h.sectionId,
      lineStart: h.lineStart,
      lineEnd: h.lineEnd,
      children: [],
    };
    while (stack.length > 0 && stack[stack.length - 1]!.depth >= h.depth) {
      stack.pop();
    }
    if (stack.length === 0) root.push(node);
    else stack[stack.length - 1]!.children.push(node);
    stack.push(node);
  }
  return root;
}

export function buildHeadingTree(
  lines: string[],
  bodyStartLine1Based: number,
  lineCount: number,
): HeadingNode[] {
  const flat = collectHeadings(lines, bodyStartLine1Based);
  const withEnds = assignLineEnds(flat, lineCount);
  return flatToTree(withEnds);
}

/**
 * Read a UTF-8 file and produce heading tree with absolute line numbers and frontmatter.
 * Results are cached per resolved path until process exit or {@link clearDocumentParseCache}.
 */
export function parseDocument(filePath: string): ParsedDocument {
  const key = path.resolve(filePath);
  const hit = documentParseCache.get(key);
  if (hit) return hit;
  const doc = parseDocumentUncached(key);
  documentParseCache.set(key, doc);
  return doc;
}

function parseDocumentUncached(resolvedPath: string): ParsedDocument {
  const raw = fs.readFileSync(resolvedPath, "utf8");
  const lines = splitLines(raw);
  const lineCount = lines.length;
  const { frontmatter, hadFrontmatterBlock } = extractFrontmatter(raw);
  const bodyStart = getBodyStartLine1Based(lines);
  const headings = buildHeadingTree(lines, bodyStart, lineCount);
  const fileName = resolvedPath.split(/[/\\]/).pop() ?? resolvedPath;

  return {
    filePath: resolvedPath,
    fileName,
    frontmatter: hadFrontmatterBlock ? frontmatter : null,
    headings,
    lineCount,
  };
}

/**
 * All heading nodes in document order (pre-order DFS).
 */
export function flattenHeadingTree(nodes: HeadingNode[]): HeadingNode[] {
  const out: HeadingNode[] = [];
  const walk = (list: HeadingNode[]) => {
    for (const n of list) {
      out.push(n);
      walk(n.children);
    }
  };
  walk(nodes);
  return out;
}

function normalizeSectionToken(s: string): string {
  return s
    .replace(/^§\s*/i, "")
    .trim()
    .toLowerCase();
}

export interface ExtractSectionMatch {
  heading: HeadingNode;
  content: string;
  lineStart: number;
  lineEnd: number;
}

export type SectionResolveResult =
  | ExtractSectionMatch
  | { kind: "ambiguous"; candidates: { sectionId: string; title: string }[] }
  | null;

/**
 * Find a section by numeric/slug sectionId (exact, case-insensitive, optional §) or by title substring.
 */
function resolveSectionHeading(
  doc: ParsedDocument,
  sectionQuery: string,
): SectionResolveResult {
  const q = sectionQuery.trim();
  if (!q) return null;

  const all = flattenHeadingTree(doc.headings);
  const normQ = normalizeSectionToken(q);

  const idMatches = all.filter(
    (n) => normalizeSectionToken(n.sectionId) === normQ,
  );
  if (idMatches.length === 1) {
    return buildExtractMatch(doc.filePath, idMatches[0]!);
  }
  if (idMatches.length > 1) {
    return {
      kind: "ambiguous",
      candidates: idMatches.map((n) => ({
        sectionId: n.sectionId,
        title: n.title,
      })),
    };
  }

  const qLower = q.toLowerCase();
  const titleMatches = all.filter((n) =>
    n.title.trim().toLowerCase().includes(qLower),
  );
  if (titleMatches.length === 1) {
    return buildExtractMatch(doc.filePath, titleMatches[0]!);
  }
  if (titleMatches.length > 1) {
    return {
      kind: "ambiguous",
      candidates: titleMatches.map((n) => ({
        sectionId: n.sectionId,
        title: n.title,
      })),
    };
  }

  return null;
}

/**
 * Build section slice text for a heading node (inclusive line range).
 */
export function buildExtractMatch(
  filePath: string,
  heading: HeadingNode,
): ExtractSectionMatch {
  const content = extractLines(filePath, heading.lineStart, heading.lineEnd);
  return {
    heading,
    content,
    lineStart: heading.lineStart,
    lineEnd: heading.lineEnd,
  };
}

/**
 * Read inclusive line range (1-based, physical file) as a single string.
 */
export function extractLines(
  filePath: string,
  lineStart: number,
  lineEnd: number,
): string {
  const raw = fs.readFileSync(filePath, "utf8");
  const lines = splitLines(raw);
  const start = Math.max(0, lineStart - 1);
  const end = Math.min(lines.length, lineEnd);
  return lines.slice(start, end).join("\n");
}

/**
 * Resolve section content; null if not found, or ambiguous match metadata.
 */
export function extractSection(
  doc: ParsedDocument,
  sectionQuery: string,
): SectionResolveResult {
  return resolveSectionHeading(doc, sectionQuery);
}
