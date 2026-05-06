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

function parseDiffPathsArg() {
  // --diff-paths <csv> overrides git diff source. Used by ship-final cumulative
  // validate to scope to plan's task commits instead of full HEAD diff.
  const idx = process.argv.indexOf("--diff-paths");
  if (idx === -1) return null;
  const raw = process.argv[idx + 1];
  if (!raw) return [];
  return raw
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

function gitDiffPaths() {
  const override = parseDiffPathsArg();
  if (override !== null) return override.sort();
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

function entryId(entry) {
  return typeof entry === "string" ? entry : entry.id;
}

function resolveScripts(pathMap, touched) {
  // Accumulator: scriptId → {scoped: bool, paths: Set<string>}.
  // A bare-string entry OR baseline membership marks scoped=false (whole-tree run).
  // An object entry {id, scope:"matched"} adds matched paths; only runs scoped if
  // never seen as bare-string. Collisions (string + object for same id) collapse
  // to whole-tree to preserve correctness.
  const acc = new Map();
  for (const id of pathMap.baseline) {
    acc.set(id, { scoped: false, paths: new Set() });
  }
  for (const path of touched) {
    for (const [glob, scriptList] of Object.entries(pathMap.path_globs)) {
      if (!micromatch.isMatch(path, glob, { dot: true })) continue;
      for (const entry of scriptList) {
        const id = entryId(entry);
        const wantsScope = typeof entry === "object" && entry.scope === "matched";
        const cur = acc.get(id) ?? { scoped: true, paths: new Set() };
        if (wantsScope) {
          // Stay scoped only if never marked whole-tree.
          if (cur.scoped) cur.paths.add(path);
        } else {
          cur.scoped = false;
          cur.paths.clear();
        }
        acc.set(id, cur);
      }
    }
  }
  return Array.from(acc.entries())
    .map(([id, meta]) => ({
      id,
      scoped: meta.scoped,
      paths: Array.from(meta.paths).sort(),
    }))
    .sort((a, b) => a.id.localeCompare(b.id));
}

function runScript(spec) {
  const { id, scoped, paths } = spec;
  const args = ["run", id];
  if (scoped && paths.length > 0) {
    args.push("--", ...paths);
  }
  return new Promise((res) => {
    const start = Date.now();
    const child = spawn("npm", args, {
      cwd: REPO_ROOT,
      stdio: ["ignore", "inherit", "inherit"],
      env: process.env,
    });
    child.on("exit", (code) => {
      res({
        script: id,
        scoped: scoped && paths.length > 0,
        scoped_paths: scoped ? paths : [],
        ok: code === 0,
        code: code ?? -1,
        elapsed_ms: Date.now() - start,
      });
    });
    child.on("error", (err) => {
      res({
        script: id,
        scoped: scoped && paths.length > 0,
        scoped_paths: scoped ? paths : [],
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
  const runSpecs = resolveScripts(pathMap, touched);
  const runIds = runSpecs.map((s) => s.id);

  const allScripts = new Set(pathMap.baseline);
  for (const list of Object.values(pathMap.path_globs)) {
    for (const s of list) allScripts.add(entryId(s));
  }
  const skipped = Array.from(allScripts)
    .filter((s) => !runIds.includes(s))
    .sort();

  console.error(
    `validate-fast: ${touched.length} touched paths → ${runSpecs.length} scripts (${skipped.length} skipped)`,
  );
  for (const s of runSpecs) {
    const tag = s.scoped && s.paths.length > 0 ? ` (scoped to ${s.paths.length} paths)` : "";
    console.error(`  run: ${s.id}${tag}`);
  }

  const results = [];
  for (const spec of runSpecs) {
    console.error(`\n--- ${spec.id} ---`);
    const r = await runScript(spec);
    results.push(r);
    if (!r.ok) {
      console.error(`validate-fast: ${spec.id} FAILED (code ${r.code})`);
      break;
    }
  }

  const ok = results.length === runSpecs.length && results.every((r) => r.ok);
  const payload = {
    touched_paths: touched,
    run_scripts: runIds,
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
