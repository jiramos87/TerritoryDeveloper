/**
 * MCP tool: unity_subscribers_of — regex scan for `event += handler` subscription sites.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const inputShape = {
  event: z
    .string()
    .describe(
      "Event / `System.Action` / delegate field name to find subscribers of (e.g. 'onGridRestored', 'EditorApplication.update'). Matches any line containing `event += handler` after the dot.",
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
 * One match from the subscribers scan.
 */
export interface SubscriberHit {
  file: string;
  line: number;
  className: string;
  method: string;
  handler: string;
  snippet: string;
}

const CLASS_DECL = /\bclass\s+([A-Z][\w]*)/;
const METHOD_DECL =
  /(?:public|private|protected|internal|static|virtual|override|async|\s)+[\w<>\[\]?,\s]+\s+([A-Z_][\w]*)\s*\(/;

/**
 * Scan a single C# file for `event += handler` subscription sites.
 * Tracks class and method context via brace depth (same heuristic used by
 * `findobjectoftype-scan.ts`). Exported for unit testing.
 */
export function scanFileForSubscribers(
  filePath: string,
  repoRoot: string,
  eventName: string,
): SubscriberHit[] {
  const content = fs.readFileSync(filePath, "utf8");
  const eventEscaped = escapeRegex(eventName);
  // `\bEvent\s*\+=\s*handler` — captures the handler expression on the right.
  const subRe = new RegExp(`\\b${eventEscaped}\\s*\\+=\\s*([\\w.]+)`);
  if (!subRe.test(content)) return [];

  const lines = content.split(/\r?\n/);
  const hits: SubscriberHit[] = [];
  const relPath = path.relative(repoRoot, filePath).split(path.sep).join("/");

  let currentClass = "";
  let currentMethod = "";
  let braceDepth = 0;
  let methodBraceStart = -1;
  let classBraceStart = -1;
  let inBlockComment = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    if (inBlockComment) {
      const endIdx = line.indexOf("*/");
      if (endIdx === -1) continue;
      inBlockComment = false;
    }
    const startIdx = line.indexOf("/*");
    if (startIdx !== -1 && line.indexOf("*/", startIdx + 2) === -1) {
      inBlockComment = true;
    }

    const classMatch = line.match(CLASS_DECL);
    if (classMatch) {
      currentClass = classMatch[1]!;
      classBraceStart = braceDepth;
    }

    const methodMatch = line.match(METHOD_DECL);
    if (methodMatch && braceDepth === classBraceStart + 1) {
      currentMethod = methodMatch[1]!;
      methodBraceStart = braceDepth;
    }

    for (const ch of line) {
      if (ch === "{") braceDepth++;
      if (ch === "}") {
        braceDepth--;
        if (currentMethod && braceDepth <= methodBraceStart) {
          currentMethod = "";
        }
        if (currentClass && braceDepth <= classBraceStart) {
          currentClass = "";
          classBraceStart = -1;
        }
      }
    }

    const match = line.match(subRe);
    if (!match) continue;
    // Skip commented-out subscription lines.
    const trimmed = line.trimStart();
    if (trimmed.startsWith("//")) continue;
    if (trimmed.startsWith("*")) continue;

    hits.push({
      file: relPath,
      line: i + 1,
      className: currentClass || "<unknown>",
      method: currentMethod || "<top-level>",
      handler: match[1]!,
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
 * Register the `unity_subscribers_of` MCP tool.
 */
export function registerUnitySubscribersOf(server: McpServer): void {
  server.registerTool(
    "unity_subscribers_of",
    {
      description:
        "Find C# `event += handler` subscription sites across Assets/Scripts/**/*.cs. Input: event / delegate field name (e.g. 'onGridRestored'). Returns class, method, file, line, handler, and snippet for each subscriber. Excludes commented-out lines. Regex-based brace-depth class/method tracking.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("unity_subscribers_of", async () => {
        const repoRoot = resolveRepoRoot();
        const rawEvent = (args?.event ?? "").trim();
        if (!rawEvent) {
          return jsonResult({
            error: "invalid_input",
            message: "event is required",
          });
        }

        // Support dotted form: 'EditorApplication.update' → last segment is the event leaf.
        const dotIdx = rawEvent.lastIndexOf(".");
        const eventLeaf =
          dotIdx > 0 && dotIdx < rawEvent.length - 1
            ? rawEvent.slice(dotIdx + 1)
            : rawEvent;

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
        const hits: SubscriberHit[] = [];
        for (const file of csFiles) {
          hits.push(...scanFileForSubscribers(file, repoRoot, eventLeaf));
        }

        return jsonResult({
          event: rawEvent,
          event_leaf: eventLeaf,
          scanned_path: scanPath,
          files_scanned: csFiles.length,
          subscriber_count: hits.length,
          subscribers: hits,
        });
      }),
  );
}
