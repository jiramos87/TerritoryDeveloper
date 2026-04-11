/**
 * MCP tool: unity_callers_of — regex scan for method call sites across Assets/Scripts/**\/*.cs.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const inputShape = {
  method: z
    .string()
    .describe(
      "Method name to find callers of (e.g. 'ResolveAt'). May be unqualified or dotted (e.g. 'RoadResolver.ResolveAt'); when dotted, the left side fills the optional class filter.",
    ),
  class: z
    .string()
    .optional()
    .describe(
      "Optional class name to scope the search (e.g. 'RoadResolver'). When set, only `ClassName.MethodName(` call sites are reported.",
    ),
  path: z
    .string()
    .optional()
    .describe(
      "Repo-relative directory to scan (default: Assets/Scripts/). Scans *.cs files recursively.",
    ),
};

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

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * One match from the callers scan.
 */
export interface CallerHit {
  file: string;
  line: number;
  snippet: string;
}

const DECL_KEYWORDS =
  /\b(public|private|protected|internal|static|async|override|virtual|abstract|sealed|extern|partial)\b/;

/**
 * True when {@link line} looks like a method *declaration* for {@link method}, not a call site.
 * Heuristic: presence of a visibility / modifier keyword AND the method name immediately
 * preceded by a C# type fragment, with no receiver-dot before it.
 */
function isDeclarationLine(line: string, method: string): boolean {
  if (!DECL_KEYWORDS.test(line)) return false;
  const escaped = escapeRegex(method);
  // Type + method name + paren, with no `.` immediately preceding the method name.
  const declRe = new RegExp(
    `(?:^|[^.\\w])([\\w<>\\[\\],?]+\\s+)${escaped}\\s*\\(`,
  );
  return declRe.test(line);
}

/**
 * Skip single-line comments, XML doc comments, and empty lines fast.
 */
function isCommentOrEmpty(line: string): boolean {
  const t = line.trimStart();
  if (!t) return true;
  if (t.startsWith("//")) return true;
  if (t.startsWith("*")) return true;
  return false;
}

/**
 * Scan a single C# file for caller sites of {@link method} (optionally scoped by {@link classFilter}).
 * Exported for unit testing.
 */
export function scanFileForCallers(
  filePath: string,
  repoRoot: string,
  method: string,
  classFilter?: string,
): CallerHit[] {
  const content = fs.readFileSync(filePath, "utf8");
  const methodEscaped = escapeRegex(method);
  const callRe = classFilter
    ? new RegExp(`\\b${escapeRegex(classFilter)}\\.${methodEscaped}\\s*\\(`)
    : new RegExp(`\\b${methodEscaped}\\s*\\(`);
  if (!callRe.test(content)) return [];

  const lines = content.split(/\r?\n/);
  const hits: CallerHit[] = [];
  const relPath = path.relative(repoRoot, filePath).split(path.sep).join("/");
  let inBlockComment = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    // Strip block-comment state cheaply; we ignore lines fully inside /* ... */ ranges.
    if (inBlockComment) {
      const endIdx = line.indexOf("*/");
      if (endIdx === -1) continue;
      inBlockComment = false;
    }
    const startIdx = line.indexOf("/*");
    if (startIdx !== -1 && line.indexOf("*/", startIdx + 2) === -1) {
      inBlockComment = true;
    }

    if (isCommentOrEmpty(line)) continue;
    if (!callRe.test(line)) continue;
    if (isDeclarationLine(line, method)) continue;

    hits.push({
      file: relPath,
      line: i + 1,
      snippet: line.trim().slice(0, 160),
    });
  }

  return hits;
}

function globCsFiles(dir: string, results: string[] = []): string[] {
  if (!fs.existsSync(dir)) return results;

  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      globCsFiles(full, results);
    } else if (entry.name.endsWith(".cs")) {
      results.push(full);
    }
  }
  return results;
}

/**
 * Register the `unity_callers_of` MCP tool.
 */
export function registerUnityCallersOf(server: McpServer): void {
  server.registerTool(
    "unity_callers_of",
    {
      description:
        "Find C# call sites for a method across Assets/Scripts/**/*.cs. Input: method name (e.g. 'ResolveAt'), optional class filter (e.g. 'RoadResolver'). Returns file, line, and trimmed snippet for each caller, excluding the declaration itself and comment-only lines. Regex-based; refactoring-safe for the common case.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("unity_callers_of", async () => {
        const repoRoot = resolveRepoRoot();
        const rawMethod = (args?.method ?? "").trim();
        if (!rawMethod) {
          return jsonResult({
            error: "invalid_input",
            message: "method is required",
          });
        }

        let method = rawMethod;
        let classFilter = (args?.class ?? "").trim() || undefined;
        // Dotted form: 'RoadResolver.ResolveAt' → class='RoadResolver', method='ResolveAt'.
        const dotIdx = rawMethod.lastIndexOf(".");
        if (dotIdx > 0 && dotIdx < rawMethod.length - 1) {
          const left = rawMethod.slice(0, dotIdx).trim();
          const right = rawMethod.slice(dotIdx + 1).trim();
          if (left && right) {
            method = right;
            classFilter = classFilter ?? left;
          }
        }

        const scanPath = (args?.path ?? "Assets/Scripts/").trim();
        const absPath = path.isAbsolute(scanPath)
          ? scanPath
          : path.join(repoRoot, scanPath);

        if (!fs.existsSync(absPath)) {
          return jsonResult({
            error: "path_not_found",
            message: `Directory not found: ${scanPath}`,
          });
        }

        const csFiles = globCsFiles(absPath);
        const hits: CallerHit[] = [];
        for (const file of csFiles) {
          hits.push(...scanFileForCallers(file, repoRoot, method, classFilter));
        }

        return jsonResult({
          method,
          class: classFilter ?? null,
          scanned_path: scanPath,
          files_scanned: csFiles.length,
          caller_count: hits.length,
          callers: hits,
        });
      }),
  );
}
