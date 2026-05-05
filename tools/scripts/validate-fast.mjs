#!/usr/bin/env node
/**
 * validate-fast.mjs — scoped validator runner.
 *
 * Reads `git diff --name-only HEAD` (staged + unstaged) plus untracked tracked
 * paths via `git ls-files --others --exclude-standard`, maps touched paths to
 * validator scripts via `tools/scripts/validate-fast-path-map.json`, runs the
 * union of `baseline` + scoped scripts. Emits structured JSON to stdout:
 *
 *   {
 *     "touched_paths": [...],
 *     "run_scripts": [...],
 *     "skipped_scripts": [...],
 *     "elapsed_ms": <int>,
 *     "results": [{ script, ok, code, elapsed_ms }, ...],
 *     "ok": <bool>
 *   }
 *
 * Exit code mirrors aggregate ok flag (0 = all green).
 *
 * TECH-12640 (ship-protocol Stage 3).
 */

import { spawn } from "node:child_process";
import { readFileSync } from "node:fs";
import { execSync } from "node:child_process";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import micromatch from "micromatch";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "..", "..");
const PATH_MAP_PATH = resolve(__dirname, "validate-fast-path-map.json");

function loadPathMap() {
  const raw = readFileSync(PATH_MAP_PATH, "utf8");
  const json = JSON.parse(raw);
  if (!Array.isArray(json.baseline)) {
    throw new Error("path-map: baseline must be an array");
  }
  if (typeof json.path_globs !== "object" || json.path_globs === null) {
    throw new Error("path-map: path_globs must be an object");
  }
  return json;
}

function gitDiffPaths() {
  const out = new Set();
  try {
    const tracked = execSync("git diff --name-only HEAD", {
      cwd: REPO_ROOT,
      encoding: "utf8",
    });
    tracked
      .split("\n")
      .map((s) => s.trim())
      .filter(Boolean)
      .forEach((p) => out.add(p));
  } catch (_) {
    // empty tree / no HEAD — leave set empty
  }
  try {
    const untracked = execSync("git ls-files --others --exclude-standard", {
      cwd: REPO_ROOT,
      encoding: "utf8",
    });
    untracked
      .split("\n")
      .map((s) => s.trim())
      .filter(Boolean)
      .forEach((p) => out.add(p));
  } catch (_) {
    // ignore
  }
  return Array.from(out).sort();
}

function resolveScripts(pathMap, touched) {
  const scripts = new Set(pathMap.baseline);
  for (const path of touched) {
    for (const [glob, scriptList] of Object.entries(pathMap.path_globs)) {
      if (micromatch.isMatch(path, glob, { dot: true })) {
        for (const s of scriptList) scripts.add(s);
      }
    }
  }
  return Array.from(scripts).sort();
}

function runScript(script) {
  return new Promise((res) => {
    const start = Date.now();
    const child = spawn("npm", ["run", script], {
      cwd: REPO_ROOT,
      stdio: ["ignore", "inherit", "inherit"],
      env: process.env,
    });
    child.on("exit", (code) => {
      res({
        script,
        ok: code === 0,
        code: code ?? -1,
        elapsed_ms: Date.now() - start,
      });
    });
    child.on("error", (err) => {
      res({
        script,
        ok: false,
        code: -1,
        elapsed_ms: Date.now() - start,
        error: String(err),
      });
    });
  });
}

async function main() {
  const start = Date.now();
  const pathMap = loadPathMap();
  const touched = gitDiffPaths();
  const runScripts = resolveScripts(pathMap, touched);

  const allScripts = new Set(pathMap.baseline);
  for (const list of Object.values(pathMap.path_globs)) {
    for (const s of list) allScripts.add(s);
  }
  const skipped = Array.from(allScripts)
    .filter((s) => !runScripts.includes(s))
    .sort();

  console.error(
    `validate-fast: ${touched.length} touched paths → ${runScripts.length} scripts (${skipped.length} skipped)`,
  );
  for (const s of runScripts) console.error(`  run: ${s}`);

  const results = [];
  for (const script of runScripts) {
    console.error(`\n--- ${script} ---`);
    const r = await runScript(script);
    results.push(r);
    if (!r.ok) {
      console.error(`validate-fast: ${script} FAILED (code ${r.code})`);
      break;
    }
  }

  const ok = results.length === runScripts.length && results.every((r) => r.ok);
  const payload = {
    touched_paths: touched,
    run_scripts: runScripts,
    skipped_scripts: skipped,
    results,
    ok,
    elapsed_ms: Date.now() - start,
  };
  process.stdout.write(JSON.stringify(payload, null, 2) + "\n");
  process.exit(ok ? 0 : 1);
}

main().catch((err) => {
  console.error("validate-fast: runner_diff_parse_fail", err);
  process.exit(2);
});
