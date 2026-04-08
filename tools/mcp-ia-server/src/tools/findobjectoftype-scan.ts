/**
 * MCP tool: findobjectoftype_scan — regex scan for FindObjectOfType in Update/LateUpdate/FixedUpdate methods.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const inputShape = {
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

/** Hot-path method names where FindObjectOfType is a performance anti-pattern. */
const HOT_PATH_METHODS = new Set([
  "Update",
  "LateUpdate",
  "FixedUpdate",
]);

/** Match FindObjectOfType / FindObjectsOfType calls. */
const FOT_PATTERN = /FindObjects?OfType\s*[<(]/;

/** Match a method declaration. */
const METHOD_DECL =
  /(?:void|bool|int|float|string|[\w<>\[\]]+)\s+([\w]+)\s*\(/;

export interface FotViolation {
  file: string;
  line: number;
  method: string;
  snippet: string;
}

/**
 * Scan a single C# file for FindObjectOfType inside hot-path methods.
 * Uses a simple brace-counting heuristic to track method scope.
 */
export function scanFileForFot(
  filePath: string,
  repoRoot: string,
): FotViolation[] {
  const content = fs.readFileSync(filePath, "utf8");
  if (!FOT_PATTERN.test(content)) return [];

  const lines = content.split(/\r?\n/);
  const violations: FotViolation[] = [];
  const relPath = path.relative(repoRoot, filePath).split(path.sep).join("/");

  let currentMethod = "";
  let inHotPath = false;
  let braceDepth = 0;
  let methodBraceStart = -1;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    const methodMatch = line.match(METHOD_DECL);
    if (methodMatch && braceDepth <= 1) {
      const methodName = methodMatch[1]!;
      if (HOT_PATH_METHODS.has(methodName)) {
        currentMethod = methodName;
        inHotPath = true;
        methodBraceStart = braceDepth;
      } else if (inHotPath && braceDepth <= methodBraceStart) {
        // Exited hot-path method scope, new method at same level
        inHotPath = false;
        currentMethod = "";
      }
    }

    for (const ch of line) {
      if (ch === "{") braceDepth++;
      if (ch === "}") {
        braceDepth--;
        if (inHotPath && braceDepth <= methodBraceStart) {
          inHotPath = false;
          currentMethod = "";
        }
      }
    }

    if (inHotPath && FOT_PATTERN.test(line)) {
      violations.push({
        file: relPath,
        line: i + 1,
        method: currentMethod,
        snippet: line.trim().slice(0, 120),
      });
    }
  }

  return violations;
}

/**
 * Recursively collect *.cs file paths into the accumulator array.
 */
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
 * Register the findobjectoftype_scan tool.
 */
export function registerFindObjectOfTypeScan(server: McpServer): void {
  server.registerTool(
    "findobjectoftype_scan",
    {
      description:
        "Scan C# files for FindObjectOfType / FindObjectsOfType calls inside Update, LateUpdate, or FixedUpdate methods. Returns file, line, method, and snippet for each violation. Helps prevent per-frame performance anti-patterns (BUG-14, TECH-26).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("findobjectoftype_scan", async () => {
        const repoRoot = resolveRepoRoot();
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
        const allViolations: FotViolation[] = [];

        for (const file of csFiles) {
          allViolations.push(...scanFileForFot(file, repoRoot));
        }

        return jsonResult({
          scanned_path: scanPath,
          files_scanned: csFiles.length,
          violation_count: allViolations.length,
          violations: allViolations,
        });
      }),
  );
}
