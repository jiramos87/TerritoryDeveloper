/**
 * backlog-yaml-loader.ts
 *
 * Load per-issue yaml records from ia/backlog/ and ia/backlog-archive/.
 * No external yaml dependency — uses a minimal inline parser matched to the
 * migrate-backlog-to-yaml.mjs emitter.
 *
 * Public API mirrors backlog-parser.ts so callers are transparent to the swap.
 */

import fs from "node:fs";
import path from "node:path";
import type {
  ParsedBacklogIssue,
  BacklogIssueStatus,
} from "./backlog-parser.js";

const BACKLOG_DIR = "ia/backlog";
const ARCHIVE_DIR = "ia/backlog-archive";

// ---------------------------------------------------------------------------
// Manifest cache (TECH-496 / B8) — dir-mtime-keyed id→path map.
// ---------------------------------------------------------------------------

interface ManifestCache {
  // Concatenated mtimeMs of both dirs (open + archive). Changes on any
  // add/rename/remove within either dir (POSIX mtime semantics).
  dirMtimeKey: string;
  map: Map<string, string>;
}

let manifestCache: ManifestCache | null = null;

function dirMtimeOrZero(absDir: string): number {
  try {
    return fs.statSync(absDir).mtimeMs;
  } catch {
    return 0;
  }
}

function computeDirMtimeKey(repoRoot: string): string {
  const open = dirMtimeOrZero(path.join(repoRoot, BACKLOG_DIR));
  const archive = dirMtimeOrZero(path.join(repoRoot, ARCHIVE_DIR));
  return `${open}|${archive}`;
}

/**
 * Build (or rebuild) the `{id → yaml-path}` map by scanning both backlog dirs.
 * Only called on cache miss / invalidation — top-frequency `backlog_issue`
 * calls return from the cache after the first scan per session.
 */
function buildManifestMap(repoRoot: string): Map<string, string> {
  const map = new Map<string, string>();
  for (const dir of [BACKLOG_DIR, ARCHIVE_DIR]) {
    const abs = path.join(repoRoot, dir);
    if (!fs.existsSync(abs)) continue;
    for (const f of fs.readdirSync(abs)) {
      if (!f.endsWith(".yaml")) continue;
      const id = f.slice(0, -".yaml".length);
      // Open dir wins if id appears in both (shouldn't happen normally).
      if (!map.has(id)) {
        map.set(id, path.join(abs, f));
      }
    }
  }
  return map;
}

function getManifestMap(repoRoot: string): Map<string, string> {
  const key = computeDirMtimeKey(repoRoot);
  if (manifestCache && manifestCache.dirMtimeKey === key) {
    return manifestCache.map;
  }
  const map = buildManifestMap(repoRoot);
  manifestCache = { dirMtimeKey: key, map };
  return map;
}

/**
 * Reset the manifest cache (tests only).
 */
export function resetManifestCache(): void {
  manifestCache = null;
}

// ---------------------------------------------------------------------------
// Minimal YAML parser (our schema only)
// ---------------------------------------------------------------------------

function unquote(s: string): string {
  const t = s.trim();
  if (
    (t.startsWith('"') && t.endsWith('"')) ||
    (t.startsWith("'") && t.endsWith("'"))
  ) {
    return t
      .slice(1, -1)
      .replace(/\\n/g, "\n")
      .replace(/\\"/g, '"')
      .replace(/\\\\/g, "\\");
  }
  return t;
}

interface YamlRecord {
  id: string;
  type?: string;
  title?: string;
  priority?: string;
  status?: string;
  section?: string;
  spec?: string;
  files?: string[];
  notes?: string;
  acceptance?: string;
  depends_on?: string[];
  depends_on_raw?: string; // raw prose preserving soft markers
  related?: string[];
  created?: string;
  raw_markdown?: string;
  // Locator fields (TECH-364 / schema v2) — all optional; absent in v1 records
  parent_plan?: string;
  task_key?: string;
  step?: string;   // scalar from yaml; coerced to number in yamlToIssue
  stage?: string;
  router_domain?: string;
  surfaces?: string[];
  mcp_slices?: string[];
  skill_hints?: string[];
}

function parseYamlRecord(content: string): YamlRecord {
  const lines = content.split("\n");
  const obj: Record<string, unknown> = {};
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;
    if (!line || line.startsWith("#")) { i++; continue; }

    const colonIdx = line.indexOf(": ");
    if (colonIdx < 0) {
      // Bare-colon key (block value follows)
      const bareColon = line.indexOf(":");
      if (bareColon >= 0 && bareColon === line.length - 1) {
        const key = line.slice(0, bareColon).trim();
        i++;
        if (i < lines.length && (lines[i] === "[]" || lines[i]!.startsWith("  - "))) {
          const items: string[] = [];
          while (i < lines.length && lines[i]!.startsWith("  - ")) {
            items.push(unquote(lines[i]!.slice(4)));
            i++;
          }
          obj[key] = items;
        } else {
          obj[key] = "";
        }
      } else {
        i++;
      }
      continue;
    }

    const key = line.slice(0, colonIdx).trim();
    const rawVal = line.slice(colonIdx + 2);

    if (rawVal === "|") {
      i++;
      const blockLines: string[] = [];
      while (i < lines.length && (lines[i]!.startsWith("  ") || lines[i] === "")) {
        blockLines.push(lines[i]!.startsWith("  ") ? lines[i]!.slice(2) : "");
        i++;
      }
      while (blockLines.length > 0 && !blockLines[blockLines.length - 1]) blockLines.pop();
      obj[key] = blockLines.join("\n");
      continue;
    }

    if (rawVal === "[]") {
      obj[key] = [];
    } else if (rawVal.trimStart() === "") {
      i++;
      if (i < lines.length && lines[i]!.trim().startsWith("- ")) {
        const items: string[] = [];
        while (i < lines.length && lines[i]!.trim().startsWith("- ")) {
          items.push(unquote(lines[i]!.trim().slice(2)));
          i++;
        }
        obj[key] = items;
      } else {
        obj[key] = "";
      }
      continue;
    } else {
      obj[key] = unquote(rawVal);
    }

    i++;
  }

  return obj as unknown as YamlRecord;
}

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

/**
 * Throw if a parsed yaml record is missing required fields.
 * Called after parseYamlRecord so malformed records are loud, not silent.
 */
function validateYamlRecord(rec: YamlRecord, file: string): void {
  if (!rec.id || typeof rec.id !== "string") {
    throw new Error(`missing required field 'id' in ${file}`);
  }
}

// ---------------------------------------------------------------------------
// Convert yaml record → ParsedBacklogIssue
// ---------------------------------------------------------------------------

const TASK_KEY_RE = /^T\d+\.\d+(\.\d+)?$/;

function validateTaskKey(value: string, fileHint?: string): void {
  if (!TASK_KEY_RE.test(value)) {
    const loc = fileHint ? ` in ${fileHint}` : "";
    throw new Error(
      `invalid task_key '${value}'${loc}: must match ^T\\d+\\.\\d+(\\.\\d+)?$`,
    );
  }
}

function yamlToIssue(rec: YamlRecord, fileHint?: string): ParsedBacklogIssue {
  const status: BacklogIssueStatus = rec.status === "closed" ? "completed" : "open";
  // depends_on: use raw prose (preserves soft markers for resolveDependsOnStatus)
  // Fall back to joined ids if raw not present (older yaml without depends_on_raw field)
  const dependsOnArr = Array.isArray(rec.depends_on) ? rec.depends_on : [];
  const dependsOnStr =
    rec.depends_on_raw
      ? rec.depends_on_raw
      : dependsOnArr.length
        ? dependsOnArr.join(", ")
        : undefined;
  // Reconstruct files as prose string
  const filesArr = Array.isArray(rec.files) ? rec.files : [];
  const filesStr = filesArr.length ? filesArr.map((f) => `\`${f}\``).join(", ") : undefined;

  return {
    issue_id: rec.id ?? "",
    title: rec.title ?? "",
    status,
    backlog_section: rec.section ?? "",
    type: rec.type || undefined,
    files: filesStr,
    spec: rec.spec && rec.spec !== '""' ? rec.spec : undefined,
    notes: rec.notes || undefined,
    acceptance: rec.acceptance || undefined,
    depends_on: dependsOnStr,
    priority: rec.priority ?? null,
    related: Array.isArray(rec.related) ? rec.related : undefined,
    created: rec.created ?? null,
    raw_markdown: rec.raw_markdown ?? "",
    // Locator fields (TECH-364 / schema v2)
    parent_plan: rec.parent_plan ?? null,
    task_key: (() => {
      if (rec.task_key != null) validateTaskKey(rec.task_key, fileHint);
      return rec.task_key ?? null;
    })(),
    step: (() => { const n = Number(rec.step); return rec.step != null && !isNaN(n) ? n : null; })(),
    stage: rec.stage ?? null,
    phase: null,
    router_domain: rec.router_domain ?? null,
    surfaces: Array.isArray(rec.surfaces) ? rec.surfaces : [],
    mcp_slices: Array.isArray(rec.mcp_slices) ? rec.mcp_slices : [],
    skill_hints: Array.isArray(rec.skill_hints) ? rec.skill_hints : [],
  };
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/**
 * Load a single issue yaml by id. Checks open dir first, then archive.
 * Returns null if yaml files are not present (caller falls back to md parser).
 */
export function loadYamlIssue(
  repoRoot: string,
  issueId: string,
): ParsedBacklogIssue | null {
  // Manifest cache lookup (TECH-496 / B8) — avoids per-call fs.existsSync hops.
  const map = getManifestMap(repoRoot);
  const p = map.get(issueId);
  if (!p) return null;
  try {
    const rec = parseYamlRecord(fs.readFileSync(p, "utf8"));
    return yamlToIssue(rec, p);
  } catch {
    return null;
  }
}

/**
 * True if the yaml dirs exist and contain at least one yaml file (migration has been run).
 */
export function yamlBacklogExists(repoRoot: string): boolean {
  const openDir = path.join(repoRoot, BACKLOG_DIR);
  const archiveDir = path.join(repoRoot, ARCHIVE_DIR);
  const openExists = fs.existsSync(openDir);
  const archiveExists = fs.existsSync(archiveDir);
  if (!openExists && !archiveExists) return false;
  const hasOpen =
    openExists && fs.readdirSync(openDir).some((f) => f.endsWith(".yaml"));
  const hasArchive =
    archiveExists &&
    fs.readdirSync(archiveDir).some((f) => f.endsWith(".yaml"));
  return hasOpen || hasArchive;
}

export interface LoadAllYamlResult {
  records: ParsedBacklogIssue[];
  parseErrorCount: number;
}

/**
 * Load all issues from yaml dirs.
 * scope: "open" = ia/backlog/, "archive" = ia/backlog-archive/, "all" = both.
 * Malformed files are skipped but logged to stderr; error count returned in result.
 */
export function loadAllYamlIssues(
  repoRoot: string,
  scope: "open" | "archive" | "all" = "all",
): LoadAllYamlResult {
  const records: ParsedBacklogIssue[] = [];
  let parseErrorCount = 0;

  const dirs: string[] = [];
  if (scope === "open" || scope === "all") dirs.push(BACKLOG_DIR);
  if (scope === "archive" || scope === "all") dirs.push(ARCHIVE_DIR);

  for (const dir of dirs) {
    const abs = path.join(repoRoot, dir);
    if (!fs.existsSync(abs)) continue;
    const files = fs
      .readdirSync(abs)
      .filter((f) => f.endsWith(".yaml"))
      .sort();
    for (const f of files) {
      const filePath = path.join(abs, f);
      try {
        const rec = parseYamlRecord(fs.readFileSync(filePath, "utf8"));
        validateYamlRecord(rec, filePath);
        records.push(yamlToIssue(rec, filePath));
      } catch (e) {
        const msg = e instanceof Error ? e.message : String(e);
        console.error(`[backlog-yaml] parse error ${filePath}: ${msg}`);
        parseErrorCount++;
      }
    }
  }

  return { records, parseErrorCount };
}
