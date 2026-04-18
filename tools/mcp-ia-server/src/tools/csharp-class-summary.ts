/**
 * MCP tool: csharp_class_summary — regex-based structural summary of a C# class.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  class_name: z
    .string()
    .describe(
      "C# class name to summarize (e.g. 'RoadManager'). Case-sensitive; matches the `class X` declaration across Assets/Scripts/**/*.cs.",
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
 * One public method signature extracted from a class body.
 */
export interface ClassMethodSummary {
  name: string;
  signature: string;
  line: number;
}

/**
 * One field declared inside the class body.
 */
export interface ClassFieldSummary {
  name: string;
  type: string;
  visibility: string;
  line: number;
}

/**
 * Shape returned by {@link summarizeClassInFile}.
 */
export interface ClassSummary {
  file: string;
  class_name: string;
  base_types: string[];
  public_methods: ClassMethodSummary[];
  fields: ClassFieldSummary[];
  dependencies: string[];
  brief_xml_doc: string;
  declaration_line: number;
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
 * Walk the line stream above {@link declLineIndex} and collect `/// <summary>…</summary>` content.
 * Stops as soon as a non-comment, non-empty line is hit.
 */
function collectXmlSummary(lines: string[], declLine: number): string {
  // declLine is 1-based → 0-based index of the decl line is declLine - 1;
  // we walk UP starting from declLine - 2 so we do not include the decl line.
  const buf: string[] = [];
  for (let i = declLine - 2; i >= 0; i--) {
    const line = lines[i]!;
    const t = line.trim();
    if (!t) continue;
    if (!t.startsWith("///") && !t.startsWith("[")) break;
    if (t.startsWith("[")) continue; // skip attributes on the line above class decl
    const stripped = t.replace(/^\/\/\/\s?/, "");
    buf.push(stripped);
  }
  buf.reverse();

  const joined = buf.join("\n");
  const sumMatch = /<summary>([\s\S]*?)<\/summary>/.exec(joined);
  if (sumMatch) {
    return sumMatch[1]!
      .split("\n")
      .map((l) => l.replace(/^\s*\/?\/?\/?\s?/, "").trim())
      .filter((l) => l.length > 0)
      .join(" ")
      .slice(0, 500);
  }
  // Fallback: first non-empty doc line.
  const firstDoc = buf.find((l) => l.trim().length > 0);
  return firstDoc ? firstDoc.slice(0, 500) : "";
}

const VISIBILITY = "(public|private|protected|internal)";
const MODIFIERS =
  "(?:static|virtual|override|abstract|sealed|async|readonly|new|extern|partial|\\s)*";
const TYPE_FRAGMENT = "[\\w.<>\\[\\],?\\s]+?";

/**
 * Summarize a single class declared inside {@link filePath}. Returns `null` when
 * the file does not declare {@link className}. Exported for unit testing.
 */
export function summarizeClassInFile(
  filePath: string,
  repoRoot: string,
  className: string,
): ClassSummary | null {
  const content = fs.readFileSync(filePath, "utf8");
  const classRe = new RegExp(
    `\\bclass\\s+${escapeRegex(className)}\\b([^{]*)\\{`,
  );
  const classMatch = classRe.exec(content);
  if (!classMatch) return null;

  const lines = content.split(/\r?\n/);
  const relPath = path.relative(repoRoot, filePath).split(path.sep).join("/");

  // Find declaration line number (1-based).
  let declLine = 0;
  {
    let offset = 0;
    for (let i = 0; i < lines.length; i++) {
      const len = lines[i]!.length + 1;
      if (offset + len > classMatch.index) {
        declLine = i + 1;
        break;
      }
      offset += len;
    }
  }

  // Base types: everything between `: BaseType, IFoo` and `{`.
  const baseFragment = classMatch[1]!.trim();
  const baseTypes: string[] = [];
  if (baseFragment.startsWith(":")) {
    const rest = baseFragment.slice(1).trim();
    for (const part of rest.split(",")) {
      const p = part.trim();
      if (p && !/^where\s/.test(p)) baseTypes.push(p);
    }
  }

  // Class brace bounds via naive counting from declLine onward.
  let classBraceDepth = 0;
  let classStartLine = -1;
  let classEndLine = lines.length;
  for (let i = declLine - 1; i < lines.length; i++) {
    const line = lines[i]!;
    for (const ch of line) {
      if (ch === "{") {
        classBraceDepth++;
        if (classStartLine === -1) classStartLine = i;
      } else if (ch === "}") {
        classBraceDepth--;
        if (classBraceDepth === 0 && classStartLine !== -1) {
          classEndLine = i;
          break;
        }
      }
    }
    if (classBraceDepth === 0 && classStartLine !== -1) break;
  }

  // Dependencies — `using Foo.Bar;` directives above any class declaration in the file.
  const dependencies: string[] = [];
  for (const line of lines) {
    const m = /^\s*using\s+([\w.]+)\s*;/.exec(line);
    if (m) dependencies.push(m[1]!);
    if (/\bclass\s+/.test(line)) break;
  }

  // Walk class body: collect public methods and fields.
  const publicMethods: ClassMethodSummary[] = [];
  const fields: ClassFieldSummary[] = [];
  let braceDepth = 0;
  let inBody = false;
  let inBlockComment = false;

  const methodSigRe = new RegExp(
    `^\\s*${VISIBILITY}\\s+${MODIFIERS}(${TYPE_FRAGMENT})\\s+([A-Z_]\\w*)\\s*\\(([^)]*)\\)`,
  );
  const fieldRe = new RegExp(
    `^\\s*(?:\\[[^\\]]+\\]\\s*)?${VISIBILITY}\\s+${MODIFIERS}(${TYPE_FRAGMENT})\\s+([a-zA-Z_]\\w*)\\s*(=|;)`,
  );

  for (let i = classStartLine; i <= classEndLine && i < lines.length; i++) {
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
    const trimmed = line.trimStart();
    if (trimmed.startsWith("//")) continue;

    // Track brace depth to know when we are at class-member level (depth == 1).
    const prevDepth = braceDepth;
    for (const ch of line) {
      if (ch === "{") braceDepth++;
      else if (ch === "}") braceDepth--;
    }
    if (!inBody && braceDepth >= 1) inBody = true;
    if (!inBody) continue;

    // Only accept declarations at the class top level (entering line has depth == 1).
    const declLevel = prevDepth === 1 || (prevDepth === 0 && braceDepth >= 1);
    if (!declLevel) continue;

    const mMethod = methodSigRe.exec(line);
    if (mMethod && mMethod[1] === "public") {
      const retTypeRaw = mMethod[2]!.trim();
      const name = mMethod[3]!;
      const args = mMethod[4]!.trim();
      // Skip things that look like fields of generic-delegate type with parens inside initializer.
      if (retTypeRaw && !/^(event|delegate)$/.test(name)) {
        publicMethods.push({
          name,
          signature: `${retTypeRaw} ${name}(${args})`
            .replace(/\s+/g, " ")
            .trim(),
          line: i + 1,
        });
      }
      continue;
    }

    const mField = fieldRe.exec(line);
    if (mField) {
      const vis = mField[1]!;
      const typ = mField[2]!.trim();
      const name = mField[3]!;
      // Skip things that look like method signatures sneaking through (contains `(`).
      if (typ.includes("(") || name.includes("(")) continue;
      fields.push({
        name,
        type: typ.replace(/\s+/g, " "),
        visibility: vis,
        line: i + 1,
      });
    }
  }

  const xmlDoc = collectXmlSummary(lines, declLine);

  return {
    file: relPath,
    class_name: className,
    base_types: baseTypes,
    public_methods: publicMethods,
    fields,
    dependencies,
    brief_xml_doc: xmlDoc,
    declaration_line: declLine,
  };
}

/**
 * Register the `csharp_class_summary` MCP tool.
 */
export function registerCsharpClassSummary(server: McpServer): void {
  server.registerTool(
    "csharp_class_summary",
    {
      description:
        "Return a structural summary of a C# class: declaring file, base types, public methods (with signatures), declared fields, `using` dependencies, and the leading `/// <summary>` doc. Regex-based scan over Assets/Scripts/**/*.cs; best-effort for the common declaration patterns used in Territory Developer.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("csharp_class_summary", async () => {
        const envelope = await wrapTool(
          async (input: { class_name?: string; path?: string }) => {
            const repoRoot = resolveRepoRoot();
            const className = (input?.class_name ?? "").trim();
            if (!className) {
              throw {
                code: "invalid_input" as const,
                message: "class_name is required",
                hint: "Pass the C# class name (e.g. 'RoadManager').",
              };
            }

            const scanPath = (input?.path ?? "Assets/Scripts/").trim();
            const absPath = path.isAbsolute(scanPath)
              ? scanPath
              : path.join(repoRoot, scanPath);

            if (!fs.existsSync(absPath)) {
              throw {
                code: "invalid_input" as const,
                message: `Directory not found: ${scanPath}`,
                hint: "Provide a repo-relative path that exists (e.g. 'Assets/Scripts/').",
              };
            }

            const csFiles = globCsFiles(absPath);
            for (const file of csFiles) {
              const summary = summarizeClassInFile(file, repoRoot, className);
              if (summary) {
                return summary;
              }
            }

            // Class not found → ok:true with empty methods shape (spec §2 Phase 2).
            return {
              class_name: className,
              scanned_path: scanPath,
              files_scanned: csFiles.length,
              matches: [],
            };
          },
        )(args as { class_name?: string; path?: string });

        return jsonResult(envelope);
      }),
  );
}
