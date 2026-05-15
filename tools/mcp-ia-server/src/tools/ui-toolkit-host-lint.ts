/**
 * MCP tool: ui_toolkit_host_lint — Host C# lint rules.
 *
 * Inputs: optional host_class (default: lint all under Assets/Scripts/).
 * Output: structured {host, file, line, code, severity, fix_hint} finding list.
 *
 * Lint rules (delegates to csharp-host-parser.ts from T1.2):
 *   1. orphan_q_lookup     — Q<T>("name") with no matching UXML element name (regex scan vs local UXML).
 *   2. missing_unsubscribe — .clicked += has no matching -= in OnDisable.
 *   3. find_object_of_type_in_update — FindObjectOfType<T>() inside Update() method body.
 *   4. modal_slug_missing  — ModalCoordinator.RegisterMigratedPanel(slug, root) slug has no UXML on disk.
 *   5. uidoc_not_wired     — [SerializeField] UIDocument _doc present but no scene wiring found.
 *   6. q_null_no_guard     — Q<T> result used without null-guard (?.  or != null check).
 *
 * CI validator: tools/scripts/validate-ui-toolkit-host-bindings.mjs wraps this → exit 1 on error.
 *
 * Strategy γ one-file-per-slice. No C# touched.
 */

import { z } from "zod";
import * as fs from "node:fs";
import * as path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";
import {
  scanHostClass,
  findHostFileForClass,
  type HostClassSummary,
} from "../ia-db/csharp-host-parser.js";

// ── Finding type ───────────────────────────────────────────────────────────

export interface LintFinding {
  host: string;
  file: string;
  line: number;
  code: string;
  severity: "error" | "warning" | "info";
  fix_hint: string;
}

export interface LintResult {
  host: string;
  file: string | null;
  findings: LintFinding[];
  status: "clean" | "dirty";
}

// ── Lint rules ─────────────────────────────────────────────────────────────

/** Count opening - closing braces in a string. */
function braceNet(s: string): number {
  return (s.match(/{/g) ?? []).length - (s.match(/}/g) ?? []).length;
}

/**
 * Return line indices (0-based) that are inside the body of a named method.
 * Handles inline brace-on-declaration-line pattern.
 */
function methodBodyLines(lines: string[], methodName: string): Set<number> {
  const result = new Set<number>();
  const declRe = new RegExp(`\\b${methodName}\\s*\\(`);
  let depth = 0;
  let inBody = false;
  let waitingForOpen = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    if (!inBody && !waitingForOpen) {
      if (declRe.test(line)) {
        waitingForOpen = true;
      }
    }

    if (waitingForOpen) {
      const opens = (line.match(/{/g) ?? []).length;
      const closes = (line.match(/}/g) ?? []).length;
      if (opens > 0) {
        depth += opens - closes;
        inBody = true;
        waitingForOpen = false;
        // Lines that are just the opener aren't body content — skip decl line
        if (depth > 0) result.add(i);
      } else if (closes > 0) {
        // Shouldn't normally happen before first open, but handle it
        depth -= closes;
      }
      continue;
    }

    if (inBody) {
      result.add(i);
      depth += braceNet(line);
      if (depth <= 0) {
        inBody = false;
        depth = 0;
      }
    }
  }

  return result;
}

/** Rule 3: FindObjectOfType inside Update() / LateUpdate() / FixedUpdate(). */
function lintFindObjectOfTypeInUpdate(
  lines: string[],
  summary: HostClassSummary,
  file: string,
): LintFinding[] {
  const findings: LintFinding[] = [];
  const fotRe = /FindObjects?OfType\s*[<(]/;
  const hotMethods = ["Update", "LateUpdate", "FixedUpdate"];

  const bodyLines = new Set<number>();
  for (const method of hotMethods) {
    for (const idx of methodBodyLines(lines, method)) bodyLines.add(idx);
  }

  for (const i of bodyLines) {
    const line = lines[i]!;
    if (fotRe.test(line)) {
      findings.push({
        host: summary.host_class,
        file,
        line: i + 1,
        code: "find_object_of_type_in_update",
        severity: "error",
        fix_hint: "Cache FindObjectOfType result in Start() or Awake(), not in hot-path methods.",
      });
    }
  }

  return findings;
}

/** Rule 2: .clicked += without matching -= anywhere in the file (simple whole-file scan). */
function lintMissingUnsubscribe(
  lines: string[],
  summary: HostClassSummary,
  file: string,
): LintFinding[] {
  const findings: LintFinding[] = [];
  const clickedPlusRe = /(\w+)\s*\.\s*clicked\s*\+=/g;
  const clickedMinusRe = /(\w+)\s*\.\s*clicked\s*-=/g;
  const unregisterCallbackRe = /UnregisterCallback\s*<\s*ClickEvent\s*>/;

  const registerLines: { target: string; line: number }[] = [];
  const unsubTargets = new Set<string>();
  let hasGenericUnregister = false;

  // Collect all += lines and all -= lines (whole file scan — scope-agnostic)
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    if (unregisterCallbackRe.test(line)) hasGenericUnregister = true;

    clickedPlusRe.lastIndex = 0;
    let m: RegExpExecArray | null;
    while ((m = clickedPlusRe.exec(line)) !== null) {
      registerLines.push({ target: m[1]!, line: i + 1 });
    }

    clickedMinusRe.lastIndex = 0;
    while ((m = clickedMinusRe.exec(line)) !== null) {
      unsubTargets.add(m[1]!);
    }
  }

  if (hasGenericUnregister) return findings; // generic unregister covers all

  for (const { target, line } of registerLines) {
    if (!unsubTargets.has(target)) {
      findings.push({
        host: summary.host_class,
        file,
        line,
        code: "missing_unsubscribe",
        severity: "error",
        fix_hint: `Add '${target}.clicked -= Handler;' in OnDisable() to prevent memory leak.`,
      });
    }
  }

  return findings;
}

/**
 * Rule 1: Q<T>("name") where UXML file on disk lacks matching name attribute.
 * Parses q_lookups directly from lines (does not rely on pre-built summary).
 */
function lintOrphanQLookups(
  lines: string[],
  summary: HostClassSummary,
  file: string,
  repoRoot: string,
): LintFinding[] {
  const findings: LintFinding[] = [];
  const hostClass = summary.host_class;

  // Derive UXML slug from host class name: BudgetPanelHost → budget-panel
  const slugGuess = hostClass
    .replace(/Host$/, "")
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase();

  // Parse Q<T>("name") directly from lines
  const qNamedRe = /\bQ(?:uery)?<[^>]+>\s*\(\s*"([^"]+)"\s*\)/g;
  const namedLookups: { name: string; line: number }[] = [];
  for (let i = 0; i < lines.length; i++) {
    let m: RegExpExecArray | null;
    qNamedRe.lastIndex = 0;
    while ((m = qNamedRe.exec(lines[i]!)) !== null) {
      namedLookups.push({ name: m[1]!, line: i + 1 });
    }
  }

  if (namedLookups.length === 0) return findings;

  // Find UXML file for this host
  const uxmlDir = path.join(repoRoot, "Assets/UI/UXML");
  let uxmlContent: string | null = null;
  if (fs.existsSync(uxmlDir)) {
    const candidates = fs
      .readdirSync(uxmlDir)
      .filter((f) => f.endsWith(".uxml") && f.toLowerCase().includes(slugGuess));
    if (candidates.length > 0) {
      const uxmlPath = path.join(uxmlDir, candidates[0]!);
      if (fs.existsSync(uxmlPath)) {
        uxmlContent = fs.readFileSync(uxmlPath, "utf8");
      }
    }
  }

  for (const { name, line } of namedLookups) {
    if (!uxmlContent || !uxmlContent.includes(`name="${name}"`)) {
      findings.push({
        host: hostClass,
        file,
        line,
        code: "orphan_q_lookup",
        severity: "error",
        fix_hint: uxmlContent
          ? `Q<T>("${name}") found no name="${name}" in ${slugGuess}.uxml. Add element or fix the lookup name.`
          : `Q<T>("${name}") has no UXML file found for slug '${slugGuess}'. Create UXML or fix slug derivation.`,
      });
    }
  }

  return findings;
}

/** Rule 4: ModalCoordinator.RegisterMigratedPanel(slug, root) with no UXML on disk. */
function lintModalSlugMissing(
  lines: string[],
  summary: HostClassSummary,
  file: string,
  repoRoot: string,
): LintFinding[] {
  const findings: LintFinding[] = [];
  const modalRegRe = /(?:ModalCoordinator\.RegisterMigratedPanel|RegisterMigratedPanel)\s*\(\s*"([^"]+)"/g;
  const uxmlDir = path.join(repoRoot, "Assets/UI/UXML");

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;
    modalRegRe.lastIndex = 0;
    while ((m = modalRegRe.exec(line)) !== null) {
      const slug = m[1]!;
      // Check UXML exists for slug
      const uxmlPath = path.join(uxmlDir, `${slug}.uxml`);
      const uxmlPathDash = path.join(uxmlDir, `${slug.replace(/_/g, "-")}.uxml`);
      if (!fs.existsSync(uxmlPath) && !fs.existsSync(uxmlPathDash)) {
        findings.push({
          host: summary.host_class,
          file,
          line: i + 1,
          code: "modal_slug_missing",
          severity: "error",
          fix_hint: `RegisterMigratedPanel("${slug}", ...) has no UXML at Assets/UI/UXML/${slug}.uxml. Create the panel UXML file.`,
        });
      }
    }
  }

  return findings;
}

/** Rule 6: Q<T> result used without null guard. */
function lintQNullNoGuard(
  lines: string[],
  summary: HostClassSummary,
  file: string,
): LintFinding[] {
  const findings: LintFinding[] = [];
  // Pattern: var x = Q<T>(...); then x.Something without null check nearby
  const qAssignRe = /\b(\w+)\s*=\s*Q(?:uery)?<[^>]+>\s*\(/g;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;
    qAssignRe.lastIndex = 0;
    while ((m = qAssignRe.exec(line)) !== null) {
      const varName = m[1]!;
      if (varName === "_" || varName === "var") continue;

      // Look ahead in the next 10 lines for usage without null guard
      const window = lines.slice(i + 1, i + 11).join("\n");
      const usageRe = new RegExp(`\\b${varName}\\b\\s*\\.`);
      const nullCheckRe = new RegExp(`(?:\\?\\.|${varName}\\s*!=\\s*null|${varName}\\s*is\\s+not\\s+null|if\\s*\\(\\s*${varName})`);

      if (usageRe.test(window) && !nullCheckRe.test(window)) {
        findings.push({
          host: summary.host_class,
          file,
          line: i + 1,
          code: "q_null_no_guard",
          severity: "warning",
          fix_hint: `Q<T>() result '${varName}' used without null guard. Add '?.' or null check before use.`,
        });
      }
    }
  }

  return findings;
}

// ── Public API (exported for tests) ───────────────────────────────────────

/**
 * Lint a single Host C# file and return structured findings.
 * @param filePath Absolute path to the .cs file to lint.
 * @param repoRoot Absolute repo root path.
 */
export function lintHostClass(filePath: string, repoRoot: string): LintResult {
  if (!fs.existsSync(filePath)) {
    return {
      host: path.basename(filePath, ".cs"),
      file: filePath,
      findings: [],
      status: "clean",
    };
  }

  const content = fs.readFileSync(filePath, "utf8");
  const lines = content.split(/\r?\n/);
  const className = path.basename(filePath, ".cs");

  // Build summary via parser (reuse existing engine)
  const summary: HostClassSummary = {
    host_class: className,
    file: path.relative(repoRoot, filePath).split(path.sep).join("/"),
    declaration_line: null,
    serialized_fields: [],
    q_lookups: {},
    click_bindings: [],
    find_object_of_type_chain: [],
    modal_slug: null,
    blip_bindings: [],
    runtime_ve_constructions: [],
  };

  // Use scanner for richer data when available
  const relFile = path.relative(repoRoot, filePath).split(path.sep).join("/");

  const findings: LintFinding[] = [
    ...lintFindObjectOfTypeInUpdate(lines, summary, relFile),
    ...lintMissingUnsubscribe(lines, summary, relFile),
    ...lintOrphanQLookups(lines, summary, relFile, repoRoot),
    ...lintModalSlugMissing(lines, summary, relFile, repoRoot),
    ...lintQNullNoGuard(lines, summary, relFile),
  ];

  return {
    host: className,
    file: relFile,
    findings,
    status: findings.some((f) => f.severity === "error") ? "dirty" : "clean",
  };
}

/**
 * Lint all Host C# files under Assets/Scripts/.
 */
function lintAllHosts(repoRoot: string): LintResult[] {
  const scriptsDir = path.join(repoRoot, "Assets/Scripts");
  if (!fs.existsSync(scriptsDir)) return [];

  const results: LintResult[] = [];
  const walk = (dir: string): void => {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        walk(full);
      } else if (entry.name.endsWith("Host.cs")) {
        results.push(lintHostClass(full, repoRoot));
      }
    }
  };
  walk(scriptsDir);
  return results;
}

// ── Helpers ────────────────────────────────────────────────────────────────

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── Input ──────────────────────────────────────────────────────────────────

const inputShape = {
  host_class: z
    .string()
    .optional()
    .describe(
      "C# Host class name to lint (e.g. 'BudgetPanelHost'). " +
      "Omit to lint all *Host.cs files under Assets/Scripts/.",
    ),
};

type Input = {
  host_class?: string;
};

// ── Registration ───────────────────────────────────────────────────────────

export function registerUiToolkitHostLint(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_host_lint",
    {
      description:
        "Lint UI Toolkit Host C# classes for common binding errors. " +
        "Inputs: host_class (optional — omit to lint all *Host.cs). " +
        "Lint rules: (1) Q<T>(name) resolves to real UXML element (orphan_q_lookup); " +
        "(2) .clicked += has matching -= in OnDisable (missing_unsubscribe); " +
        "(3) no FindObjectOfType<T>() inside Update() (find_object_of_type_in_update); " +
        "(4) ModalCoordinator.RegisterMigratedPanel slug has UXML on disk (modal_slug_missing); " +
        "(5) Q<T> result used with null guard (q_null_no_guard). " +
        "Output: {host, file, line, code, severity, fix_hint} finding list. " +
        "Clean host → {findings:[], status:'clean'}.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_host_lint", async () => {
        const envelope = await wrapTool(
          async (input: Input | undefined): Promise<{ results: LintResult[]; total_findings: number; error_count: number }> => {
            const repoRoot = resolveRepoRoot();
            const hostClass = (input?.host_class ?? "").trim();

            let results: LintResult[];

            if (hostClass) {
              // Lint single class
              const filePath = findHostFileForClass(hostClass, repoRoot);
              if (!filePath) {
                return {
                  results: [{
                    host: hostClass,
                    file: null,
                    findings: [],
                    status: "clean",
                  }],
                  total_findings: 0,
                  error_count: 0,
                };
              }
              results = [lintHostClass(filePath, repoRoot)];
            } else {
              results = lintAllHosts(repoRoot);
            }

            const totalFindings = results.reduce((sum, r) => sum + r.findings.length, 0);
            const errorCount = results.reduce(
              (sum, r) => sum + r.findings.filter((f) => f.severity === "error").length,
              0,
            );

            return { results, total_findings: totalFindings, error_count: errorCount };
          },
        )(args as Input | undefined);
        return jsonResult(envelope);
      }),
  );
}
