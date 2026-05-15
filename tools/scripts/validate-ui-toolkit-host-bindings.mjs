#!/usr/bin/env node
/**
 * validate-ui-toolkit-host-bindings.mjs
 *
 * CI validator: lint all UI Toolkit Host C# files under Assets/Scripts/.
 * Wraps ui_toolkit_host_lint rules (lintHostClass) directly — no MCP roundtrip needed.
 * Exit 1 on any finding with severity=error.
 *
 * Usage:
 *   node tools/scripts/validate-ui-toolkit-host-bindings.mjs
 *   node tools/scripts/validate-ui-toolkit-host-bindings.mjs --fixture <path-to-cs-file>
 *
 * --fixture <path>  Lint a single fixture file (for tests). Exits 1 on errors, 0 on clean.
 */

import { createRequire } from "node:module";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";
import fs from "node:fs";
import { parseArgs } from "node:util";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../");

// ── Parse args ──────────────────────────────────────────────────────────────

const { values: args } = parseArgs({
  args: process.argv.slice(2),
  options: {
    fixture: { type: "string" },
    host_class: { type: "string" },
    quiet: { type: "boolean", default: false },
  },
  strict: false,
});

// ── Load lintHostClass from compiled output or ts-node ─────────────────────

async function loadLintHostClass() {
  // Try compiled JS first (when available)
  const compiledPath = path.join(
    REPO_ROOT,
    "tools/mcp-ia-server/dist/tools/ui-toolkit-host-lint.js",
  );
  if (fs.existsSync(compiledPath)) {
    const mod = await import(pathToFileURL(compiledPath).href);
    return mod.lintHostClass;
  }

  // Fallback: use tsx/ts-node via child_process (avoids ESM import of .ts)
  // For validator purposes, implement a lightweight inline version
  return lintHostClassInline;
}

// ── Inline lint (no build required for CI) ─────────────────────────────────
// Mirrors the rules in ui-toolkit-host-lint.ts without requiring a build step.

function lintHostClassInline(filePath, repoRoot) {
  if (!fs.existsSync(filePath)) {
    return { host: path.basename(filePath, ".cs"), file: filePath, findings: [], status: "clean" };
  }

  const content = fs.readFileSync(filePath, "utf8");
  const lines = content.split(/\r?\n/);
  const className = path.basename(filePath, ".cs");
  const relFile = path.relative(repoRoot, filePath).split(path.sep).join("/");

  const findings = [];

  // Rule: FindObjectOfType in Update/LateUpdate/FixedUpdate
  const fotRe = /FindObjects?OfType\s*[<(]/;
  const methodDeclRe = /\b(void|bool|int|float|[\w<>\[\]]+)\s+(Update|LateUpdate|FixedUpdate)\s*\(/;
  let inHotMethod = false;
  let braceDepth = 0;
  let methodBraceStart = 0;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (!inHotMethod) {
      const m = methodDeclRe.exec(line);
      if (m) {
        inHotMethod = true;
        methodBraceStart = braceDepth;
      }
    }

    if (inHotMethod) {
      braceDepth += (line.match(/{/g) ?? []).length;
      braceDepth -= (line.match(/}/g) ?? []).length;

      if (fotRe.test(line)) {
        findings.push({
          host: className,
          file: relFile,
          line: i + 1,
          code: "find_object_of_type_in_update",
          severity: "error",
          fix_hint: "Cache FindObjectOfType result in Start() or Awake(), not in hot-path methods.",
        });
      }

      if (braceDepth <= methodBraceStart && i > 0) {
        inHotMethod = false;
      }
    } else {
      braceDepth += (line.match(/{/g) ?? []).length;
      braceDepth -= (line.match(/}/g) ?? []).length;
    }
  }

  // Rule: .clicked += without -= in OnDisable
  const clickedPlusRe = /(\w+)\s*\.\s*clicked\s*\+=/g;
  const onDisableClickedMinusRe = /(\w+)\s*\.\s*clicked\s*-=/;
  const plusTargets = [];
  let inOnDisable2 = false;
  let braceDepth2 = 0;
  let onDisableBraceStart2 = 0;
  const onDisableMinus = new Set();

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    if (!inOnDisable2 && /\bOnDisable\s*\(/.test(line)) {
      inOnDisable2 = true;
      onDisableBraceStart2 = braceDepth2;
    }

    if (inOnDisable2) {
      braceDepth2 += (line.match(/{/g) ?? []).length;
      braceDepth2 -= (line.match(/}/g) ?? []).length;
      const mm = onDisableClickedMinusRe.exec(line);
      if (mm) onDisableMinus.add(mm[1]);
      if (braceDepth2 <= onDisableBraceStart2 && i > 0) inOnDisable2 = false;
    } else {
      braceDepth2 += (line.match(/{/g) ?? []).length;
      braceDepth2 -= (line.match(/}/g) ?? []).length;
    }

    clickedPlusRe.lastIndex = 0;
    let m;
    while ((m = clickedPlusRe.exec(line)) !== null) {
      plusTargets.push({ target: m[1], line: i + 1 });
    }
  }

  for (const { target, line } of plusTargets) {
    if (!onDisableMinus.has(target)) {
      findings.push({
        host: className,
        file: relFile,
        line,
        code: "missing_unsubscribe",
        severity: "error",
        fix_hint: `Add '${target}.clicked -= Handler;' in OnDisable() to prevent memory leak.`,
      });
    }
  }

  // Rule: ModalCoordinator.RegisterMigratedPanel with missing UXML
  const modalRegRe = /(?:ModalCoordinator\.RegisterMigratedPanel|RegisterMigratedPanel)\s*\(\s*"([^"]+)"/g;
  const uxmlDir = path.join(repoRoot, "Assets/UI/UXML");
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let m;
    modalRegRe.lastIndex = 0;
    while ((m = modalRegRe.exec(line)) !== null) {
      const slug = m[1];
      const uxmlPath = path.join(uxmlDir, `${slug}.uxml`);
      const uxmlPathDash = path.join(uxmlDir, `${slug.replace(/_/g, "-")}.uxml`);
      if (!fs.existsSync(uxmlPath) && !fs.existsSync(uxmlPathDash)) {
        findings.push({
          host: className,
          file: relFile,
          line: i + 1,
          code: "modal_slug_missing",
          severity: "error",
          fix_hint: `RegisterMigratedPanel("${slug}", ...) has no UXML at Assets/UI/UXML/${slug}.uxml. Create the panel UXML file.`,
        });
      }
    }
  }

  // Rule: Q<T>("name") with no UXML on disk (orphan q lookup)
  const slugGuess = className
    .replace(/Host$/, "")
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase();
  const qRe = /\bQ(?:uery)?<([^>]+)>\s*\(\s*"([^"]+)"\s*\)/g;
  let uxmlContent = null;
  if (fs.existsSync(uxmlDir)) {
    const candidates = fs.readdirSync(uxmlDir).filter(
      (f) => f.endsWith(".uxml") && f.toLowerCase().includes(slugGuess),
    );
    if (candidates.length > 0) {
      const uxmlPath = path.join(uxmlDir, candidates[0]);
      if (fs.existsSync(uxmlPath)) uxmlContent = fs.readFileSync(uxmlPath, "utf8");
    }
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let m;
    qRe.lastIndex = 0;
    while ((m = qRe.exec(line)) !== null) {
      const name = m[2];
      if (!uxmlContent || !uxmlContent.includes(`name="${name}"`)) {
        findings.push({
          host: className,
          file: relFile,
          line: i + 1,
          code: "orphan_q_lookup",
          severity: "error",
          fix_hint: `Q<T>("${name}") has no matching element in UXML for slug '${slugGuess}'. Add element or fix lookup name.`,
        });
      }
    }
  }

  const status = findings.some((f) => f.severity === "error") ? "dirty" : "clean";
  return { host: className, file: relFile, findings, status };
}

// ── Main ────────────────────────────────────────────────────────────────────

async function main() {
  const lintHostClass = await loadLintHostClass();

  /** @type {import('./ui-toolkit-host-lint.js').LintResult[]} */
  let results = [];

  if (args.fixture) {
    // Single fixture mode (for tests)
    const fixturePath = path.resolve(args.fixture);
    results = [lintHostClass(fixturePath, REPO_ROOT)];
  } else {
    // Full scan: all *Host.cs under Assets/Scripts/
    const scriptsDir = path.join(REPO_ROOT, "Assets/Scripts");
    if (!fs.existsSync(scriptsDir)) {
      console.log("No Assets/Scripts directory found — skipping host lint.");
      process.exit(0);
    }

    const hostFiles = [];
    const walk = (dir) => {
      for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) walk(full);
        else if (entry.name.endsWith("Host.cs")) hostFiles.push(full);
      }
    };
    walk(scriptsDir);

    results = hostFiles.map((f) => lintHostClass(f, REPO_ROOT));
  }

  const totalFindings = results.reduce((sum, r) => sum + r.findings.length, 0);
  const errorCount = results.reduce(
    (sum, r) => sum + r.findings.filter((f) => f.severity === "error").length,
    0,
  );

  if (!args.quiet) {
    if (errorCount === 0) {
      console.log(`ui-toolkit-host-bindings: ${results.length} host(s) linted — clean.`);
    } else {
      console.error(`ui-toolkit-host-bindings: ${errorCount} error(s) in ${results.length} host(s):`);
      for (const result of results) {
        for (const f of result.findings.filter((x) => x.severity === "error")) {
          console.error(`  [${f.code}] ${f.file}:${f.line} — ${f.fix_hint}`);
        }
      }
    }
  }

  process.exit(errorCount > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error("validate-ui-toolkit-host-bindings: fatal error:", err);
  process.exit(1);
});
