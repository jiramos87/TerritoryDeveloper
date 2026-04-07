/**
 * Repo paths, IA registry construction, spec aliases, and key resolution.
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
 * Short names for specs/root docs — values must match {@link buildRegistry} `key` fields (lowercase basenames).
 */
export const SPEC_KEY_ALIASES: Record<string, string> = {
  geo: "isometric-geography-system",
  geography: "isometric-geography-system",
  roads: "roads-system",
  water: "water-terrain-system",
  terrain: "water-terrain-system",
  sim: "simulation-system",
  simulation: "simulation-system",
  persist: "persistence-system",
  save: "persistence-system",
  load: "persistence-system",
  mgrs: "managers-reference",
  managers: "managers-reference",
  ui: "ui-design-system",
  arch: "architecture",
  agents: "agents",
  refspec: "reference-spec-structure",
  specstructure: "reference-spec-structure",
  unity: "unity-development-context",
  unityctx: "unity-development-context",
};

/**
 * Map user-facing short keys (e.g. `geo`) to registry keys before document lookup.
 */
export function resolveSpecKeyAlias(spec: string): string {
  const t = spec.trim().toLowerCase();
  if (!t) return spec.trim();
  return SPEC_KEY_ALIASES[t] ?? spec.trim();
}

const REPO_ROOT_MARKERS: readonly (readonly string[])[] = [
  ["config", "postgres-dev.json"],
  [".cursor", "specs", "glossary.md"],
];

/**
 * Walk parents from {@link startDir} looking for committed repo markers (Postgres dev config or glossary).
 */
export function findRepositoryRootWalkingUp(startDir: string): string | null {
  let dir = path.resolve(startDir);
  const fsRoot = path.parse(dir).root;
  for (let i = 0; i < 32 && dir !== fsRoot; i++) {
    for (const segs of REPO_ROOT_MARKERS) {
      const marker = path.join(dir, ...segs);
      if (fs.existsSync(marker)) return dir;
    }
    dir = path.dirname(dir);
  }
  return null;
}

/**
 * Resolve repository root: **`REPO_ROOT`** env (relative to cwd if not absolute), else walk up from **`cwd`**
 * for {@link REPO_ROOT_MARKERS}, else **`process.cwd()`** — so Node scripts work from **`tools/mcp-ia-server/`** without env.
 */
export function resolveRepoRoot(): string {
  const raw = process.env.REPO_ROOT;
  if (raw !== undefined && raw !== "") {
    return path.isAbsolute(raw) ? raw : path.resolve(process.cwd(), raw);
  }
  const found = findRepositoryRootWalkingUp(process.cwd());
  if (found) return found;
  return process.cwd();
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

/**
 * Resolve a spec/root document entry: alias map first, then {@link findEntryByKey}.
 */
export function findEntryForSpecDoc(
  registry: SpecRegistryEntry[],
  spec: string,
): SpecRegistryEntry | undefined {
  const aliased = resolveSpecKeyAlias(spec);
  return findEntryByKey(registry, aliased) ?? findEntryByKey(registry, spec);
}

/**
 * Find a `.mdc` rule by key or filename (never applies spec aliases — `roads` stays the rule file).
 */
export function findRuleEntry(
  registry: SpecRegistryEntry[],
  rule: string,
): SpecRegistryEntry | undefined {
  const rules = registry.filter((e) => e.category === "rule");
  const q = rule.trim().toLowerCase();
  if (!q) return undefined;
  const qBase = q.includes(".") ? path.basename(q).toLowerCase() : q;
  return rules.find((e) => {
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
