#!/usr/bin/env node
/**
 * validate-fast-coverage.mjs — meta-gate.
 *
 * Parses `package.json` `validate:all` script chain, asserts every chained
 * script appears in `tools/scripts/validate-fast-path-map.json` (either in
 * `baseline` or under >=1 entry in `path_globs`). Exit 1 on coverage gap —
 * forbids silent drift where a new validator escapes scoped runner.
 *
 * TECH-12640 (ship-protocol Stage 3). Escalation: path_map_coverage_gap.
 */

import { readFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "..", "..");

const PKG_PATH = resolve(REPO_ROOT, "package.json");
const PATH_MAP_PATH = resolve(__dirname, "validate-fast-path-map.json");

// validate:fast itself + meta-gate are excluded from coverage requirement —
// the runner does not invoke itself, and the meta-gate is the gate, not a
// gated script. Sub-orchestrators (validate:all:mutating, validate:all:readonly)
// are also excluded — they recursively chain into other npm run scripts that
// ARE in the path-map, so coverage propagates through them. Keep this set
// narrow — every other script must appear in baseline or path_globs.
const EXCLUDED_SCRIPTS = new Set([
  "validate:fast",
  "validate:fast-coverage",
  "validate:all:mutating",
  "validate:all:readonly",
]);

function extractNpmRunScripts(cmd) {
  // Extract every `npm run <script>` and `run-p|run-s <scripts...>` token from
  // a script body. Handles `&&` chains, run-p/run-s parallel groups, trailing
  // flags. Returns a flat list of script ids.
  const scripts = [];
  // Split on `&&` first to handle sequential chains.
  for (const seg of cmd.split("&&").map((s) => s.trim())) {
    // `npm run <id>` — direct invocation.
    const direct = seg.match(/^npm run ([^\s]+)/);
    if (direct) {
      scripts.push(direct[1]);
      continue;
    }
    // `run-p <ids...>` / `run-s <ids...>` — parallel/sequential group from
    // npm-run-all2. Each subsequent token (until first `--`) is a script id.
    const group = seg.match(/^run-[ps]\s+(.*)$/);
    if (group) {
      const tokens = group[1].split(/\s+/);
      for (const t of tokens) {
        if (t.startsWith("--") || t === "") break;
        scripts.push(t);
      }
    }
  }
  return scripts;
}

function parseValidateAll(pkg) {
  const cmd = pkg.scripts?.["validate:all"];
  if (!cmd) {
    throw new Error("package.json: validate:all script missing");
  }
  // Walk top-level + recurse into sub-orchestrators. `validate:all:mutating`
  // and `validate:all:readonly` are sub-orchestrators that chain into many
  // gated scripts — must expand them to get the real gated set.
  const seen = new Set();
  const queue = extractNpmRunScripts(cmd);
  const flat = [];
  while (queue.length > 0) {
    const id = queue.shift();
    if (seen.has(id)) continue;
    seen.add(id);
    const sub = pkg.scripts?.[id];
    if (sub && /^npm run|^run-[ps]\s/.test(sub.trim())) {
      // Sub-orchestrator — recurse.
      for (const child of extractNpmRunScripts(sub)) queue.push(child);
      continue;
    }
    flat.push(id);
  }
  return flat;
}

function loadPathMap() {
  const raw = readFileSync(PATH_MAP_PATH, "utf8");
  return JSON.parse(raw);
}

function entryId(entry) {
  return typeof entry === "string" ? entry : entry.id;
}

function pathMapScriptUniverse(pathMap) {
  const all = new Set(pathMap.baseline);
  for (const list of Object.values(pathMap.path_globs)) {
    for (const s of list) all.add(entryId(s));
  }
  return all;
}

function main() {
  const pkg = JSON.parse(readFileSync(PKG_PATH, "utf8"));
  const chained = parseValidateAll(pkg);
  const pathMap = loadPathMap();
  const coverage = pathMapScriptUniverse(pathMap);

  const gaps = [];
  for (const script of chained) {
    if (EXCLUDED_SCRIPTS.has(script)) continue;
    if (!coverage.has(script)) gaps.push(script);
  }

  if (gaps.length === 0) {
    console.error(
      `validate-fast-coverage: OK (${chained.length} scripts in validate:all chain, all covered)`,
    );
    process.exit(0);
  }

  console.error(
    `validate-fast-coverage: COVERAGE GAP — ${gaps.length} validate:all script(s) missing from path-map:`,
  );
  for (const g of gaps) console.error(`  - ${g}`);
  console.error(
    "\nFix: add these script ids to baseline or under >=1 path_globs entry in",
  );
  console.error("  tools/scripts/validate-fast-path-map.json");
  console.error("Escalation: path_map_coverage_gap");
  process.exit(1);
}

main();
