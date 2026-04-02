/**
 * Repo paths, IA registry construction, and key resolution.
 */

import fs from "node:fs";
import path from "node:path";
import matter from "gray-matter";
import type { SpecRegistryEntry } from "./parser/types.js";
import {
  getBodyStartLine1Based,
  splitLines,
} from "./parser/markdown-parser.js";

/**
 * Resolve repository root: REPO_ROOT env (relative to cwd if not absolute), else cwd.
 */
export function resolveRepoRoot(): string {
  const raw = process.env.REPO_ROOT;
  if (raw === undefined || raw === "") return process.cwd();
  return path.isAbsolute(raw) ? raw : path.resolve(process.cwd(), raw);
}

function toPosixRelative(repoRoot: string, absolutePath: string): string {
  const rel = path.relative(repoRoot, absolutePath);
  return rel.split(path.sep).join("/");
}

function entryKeyFromFileName(fileName: string): string {
  const base = path.basename(fileName, path.extname(fileName));
  return base.toLowerCase();
}

/**
 * First non-empty non-heading line, or first blockquote line, from body lines.
 */
export function extractMarkdownDescription(bodyLines: string[]): string {
  for (const line of bodyLines) {
    const t = line.trim();
    if (!t) continue;
    if (t.startsWith("#")) continue;
    if (t === "---") continue;
    const fromQuote = t.startsWith(">") ? t.replace(/^>\s*/, "") : t;
    const snippet = fromQuote.slice(0, 500);
    if (snippet) return snippet;
  }
  return "";
}

function readDescription(
  filePath: string,
  category: SpecRegistryEntry["category"],
): string {
  const raw = fs.readFileSync(filePath, "utf8");
  if (category === "rule" || raw.startsWith("---")) {
    const { data, content } = matter(raw);
    const d = data as Record<string, unknown>;
    if (typeof d.description === "string" && d.description.trim()) {
      return d.description.trim().slice(0, 500);
    }
    const bodyLines = splitLines(content);
    return extractMarkdownDescription(bodyLines) || "";
  }
  const lines = splitLines(raw);
  const bodyStart = getBodyStartLine1Based(lines);
  const bodyLines = lines.slice(Math.max(0, bodyStart - 1));
  return extractMarkdownDescription(bodyLines) || "";
}

function pushSortedUnique(paths: string[], dir: string, filter: (n: string) => boolean) {
  if (!fs.existsSync(dir)) return;
  const names = fs.readdirSync(dir).filter(filter).sort();
  for (const n of names) paths.push(path.join(dir, n));
}

/**
 * Scan specs, rules, and root docs; build registry entries (stable order).
 */
export function buildRegistry(): SpecRegistryEntry[] {
  const repoRoot = resolveRepoRoot();
  const specsDir = path.join(repoRoot, ".cursor", "specs");
  const rulesDir = path.join(repoRoot, ".cursor", "rules");

  const paths: string[] = [];
  pushSortedUnique(paths, specsDir, (n) => n.endsWith(".md"));
  pushSortedUnique(paths, rulesDir, (n) => n.endsWith(".mdc"));

  const rootDocs = ["AGENTS.md", "ARCHITECTURE.md"].map((f) =>
    path.join(repoRoot, f),
  );
  for (const p of rootDocs) {
    if (fs.existsSync(p)) paths.push(p);
  }

  const entries: SpecRegistryEntry[] = [];

  for (const filePath of paths) {
    const fileName = path.basename(filePath);
    let category: SpecRegistryEntry["category"];
    if (filePath.startsWith(specsDir + path.sep) || filePath === path.join(specsDir, fileName)) {
      category = "spec";
    } else if (filePath.startsWith(rulesDir + path.sep) || filePath === path.join(rulesDir, fileName)) {
      category = "rule";
    } else {
      category = "root-doc";
    }

    const key = entryKeyFromFileName(fileName);
    const description = readDescription(filePath, category);

    entries.push({
      key,
      fileName,
      filePath,
      description,
      category,
    });
  }

  return entries;
}

/**
 * Find registry entry by key, filename, or basename (case-insensitive).
 */
export function findEntryByKey(
  registry: SpecRegistryEntry[],
  spec: string,
): SpecRegistryEntry | undefined {
  const q = spec.trim().toLowerCase();
  if (!q) return undefined;
  const qBase = q.includes(".") ? path.basename(q).toLowerCase() : q;

  return registry.find((e) => {
    if (e.key === q) return true;
    if (e.fileName.toLowerCase() === q) return true;
    if (e.fileName.toLowerCase() === qBase) return true;
    if (entryKeyFromFileName(e.fileName) === qBase) return true;
    return false;
  });
}

export function relativePathForEntry(
  repoRoot: string,
  entry: SpecRegistryEntry,
): string {
  return toPosixRelative(repoRoot, entry.filePath);
}
