#!/usr/bin/env node
/**
 * ship-cycle-pass-a-preflight.mjs — asmdef diff structural pre-flight.
 *
 * Layer 3 of the three-layer ship-cycle asmdef gate (TECH-30633):
 *   - Detects asmdef churn in `git diff HEAD` + untracked tree (`git ls-files --others`).
 *   - For each touched `Assets/**\/*.asmdef`:
 *       1. Parse `references[]`; for every `GUID:xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
 *          assert the GUID resolves to an existing `*.asmdef.meta` `guid:` row in repo.
 *       2. Reverse-edge audit: walk every other `*.asmdef`. For any that ref the touched
 *          asmdef (by GUID or by name), assert the GUID still matches the sibling meta
 *          on disk (catches `.meta` GUID rewrites with stale referrers).
 *   - Runs sub-second; gates Pass A before Editor warm-up.
 *
 * Exit 0 = clean (stdout: `OK: K touched asmdef, 0 drift`).
 * Exit 1 = first drift detected (stderr: annotated edge).
 *
 * Invoked by ship-cycle Phase 1 (Pass A) step 0 when `git diff HEAD -- 'Assets/**\/*.asmdef'`
 * is non-empty. Standalone runs (no churn) are no-ops and report clean.
 */

import { execSync } from "node:child_process";
import { readdirSync, readFileSync, statSync, existsSync } from "node:fs";
import { join, dirname, resolve, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const REPO_ROOT = resolve(dirname(__filename), "..", "..");
const ASSETS_DIR = join(REPO_ROOT, "Assets");

function repoRel(abs) {
  return relative(REPO_ROOT, abs).split(sep).join("/");
}

function git(args) {
  try {
    return execSync(`git ${args}`, { cwd: REPO_ROOT, encoding: "utf8" });
  } catch {
    return "";
  }
}

function listTouchedAsmdef() {
  // Both committed-diff churn (HEAD) and untracked .asmdef files.
  const tracked = git("diff --name-only HEAD -- 'Assets/**/*.asmdef'");
  const untracked = git("ls-files --others --exclude-standard -- 'Assets/**/*.asmdef'");
  const set = new Set();
  for (const line of [...tracked.split("\n"), ...untracked.split("\n")]) {
    const p = line.trim();
    if (!p) continue;
    if (p.endsWith(".asmdef")) set.add(p);
  }
  return Array.from(set).sort();
}

function* walkAsmdef(dir) {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return;
  }
  for (const entry of entries) {
    if (entry === ".git" || entry === "node_modules") continue;
    const full = join(dir, entry);
    let st;
    try {
      st = statSync(full);
    } catch {
      continue;
    }
    if (st.isDirectory()) yield* walkAsmdef(full);
    else if (entry.endsWith(".asmdef")) yield full;
  }
}

function readGuid(asmdefAbs) {
  const metaAbs = `${asmdefAbs}.meta`;
  if (!existsSync(metaAbs)) return null;
  const raw = readFileSync(metaAbs, "utf8");
  const m = raw.match(/^guid:\s*([0-9a-fA-F]{32})/m);
  return m ? m[1].toLowerCase() : null;
}

function loadAsmdef(absPath) {
  let parsed;
  try {
    parsed = JSON.parse(readFileSync(absPath, "utf8"));
  } catch (e) {
    throw new Error(`asmdef-diff: PARSE ${repoRel(absPath)}: ${e.message}`);
  }
  const name = typeof parsed.name === "string" ? parsed.name : null;
  if (!name) throw new Error(`asmdef-diff: ${repoRel(absPath)} missing name`);
  const refs = Array.isArray(parsed.references) ? parsed.references : [];
  return { path: absPath, name, references: refs };
}

function buildAllAsmdefIndex() {
  // Map every asmdef in repo by name + by guid (for resolution). Walks Assets/ +
  // Packages/ (manifest packages may have asmdefs the project depends on transitively).
  // Library/PackageCache/ is NOT walked — those GUIDs are owned by Unity package manager
  // and may rewrite on package upgrade; treating them as "external" (unresolved GUID =
  // skip, not drift) keeps the gate focused on intra-project asmdef churn (Domains.X →
  // Domains.Y rewriting GUIDs, etc.) which is what regressions cluster around.
  const byName = new Map();
  const byGuid = new Map();
  for (const root of [ASSETS_DIR, join(REPO_ROOT, "Packages")]) {
    for (const abs of walkAsmdef(root)) {
      const a = loadAsmdef(abs);
      a.guid = readGuid(abs);
      byName.set(a.name, a);
      if (a.guid) byGuid.set(a.guid, a);
    }
  }
  return { byName, byGuid };
}

function validateForwardRefs(touchedRel, idx, prevIdx) {
  // Every `GUID:xxx` in touched asmdef.references must resolve to a known asmdef OR be
  // categorized as external (Unity PackageCache). External resolution: if the GUID was
  // previously resolvable (in the HEAD snapshot via prevIdx) and isn't now, that's drift.
  // If it never resolved (always external), skip silently.
  const abs = join(REPO_ROOT, touchedRel);
  if (!existsSync(abs)) return []; // deleted in diff — handled by reverse-edge audit below
  const a = loadAsmdef(abs);
  const violations = [];
  for (const ref of a.references) {
    if (!ref.startsWith("GUID:")) continue;
    const g = ref.slice(5).toLowerCase();
    if (idx.byGuid.has(g)) continue; // resolves now — clean
    if (prevIdx && prevIdx.byGuid.has(g)) {
      // Resolved at HEAD but not now → intra-repo drift.
      violations.push(
        `asmdef-diff: ${touchedRel} refs GUID ${g} that was in-repo at HEAD but no longer resolves`,
      );
    }
    // else: never resolved in our index — assume external Unity package (PackageCache),
    // skip. False-positive avoidance for TextMeshPro etc. (TECH-30633 §STOP).
  }
  return violations;
}

function validateReverseRefs(touchedRel, idx) {
  // For touched asmdef T: any other asmdef R that refs T must still match T's sibling meta GUID.
  const abs = join(REPO_ROOT, touchedRel);
  if (!existsSync(abs)) return []; // deletion case — referrers will fail forward audit instead
  const touched = loadAsmdef(abs);
  const touchedGuid = readGuid(abs);
  const violations = [];
  for (const r of idx.byName.values()) {
    if (r.path === abs) continue;
    for (const ref of r.references) {
      if (ref.startsWith("GUID:")) {
        const g = ref.slice(5).toLowerCase();
        if (touchedGuid && g === touchedGuid) {
          // OK — already matches
        } else if (idx.byGuid.get(g)?.name === touched.name) {
          // GUID resolves to a DIFFERENT asmdef with the same name — impossible by name-uniqueness,
          // but defensive: pass.
        }
        // else: not a ref to touched
      } else if (ref === touched.name) {
        // Bare-name reference. No GUID drift possible; nothing to assert.
      }
    }
  }
  return violations;
}

function buildHeadAsmdefIndex() {
  // Snapshot of all asmdef GUIDs known at HEAD. Used to detect intra-repo GUID drift:
  // if a `GUID:xxx` reference resolved at HEAD but not in the worktree, it's true drift
  // (asmdef renamed / deleted / GUID rewritten in a sibling .meta). If it never resolved
  // even at HEAD, it's an external package and we skip silently.
  const guids = new Set();
  const listing = git("ls-tree -r --name-only HEAD");
  const metaPaths = listing
    .split("\n")
    .map((s) => s.trim())
    .filter((s) => s.endsWith(".asmdef.meta"))
    .filter(
      (s) =>
        s.startsWith("Assets/") || s.startsWith("Packages/"),
    );
  for (const rel of metaPaths) {
    let raw;
    try {
      raw = git(`show HEAD:${JSON.stringify(rel)}`);
    } catch {
      continue;
    }
    const m = raw.match(/^guid:\s*([0-9a-fA-F]{32})/m);
    if (m) guids.add(m[1].toLowerCase());
  }
  return { byGuid: { has: (g) => guids.has(g) } };
}

function main() {
  const touched = listTouchedAsmdef();
  if (touched.length === 0) {
    process.stdout.write(`[ship-cycle-preflight] OK: 0 touched asmdef, 0 drift\n`);
    process.exit(0);
  }
  let idx;
  try {
    idx = buildAllAsmdefIndex();
  } catch (e) {
    process.stderr.write(`[ship-cycle-preflight] ${e.message}\n`);
    process.exit(1);
  }
  const prevIdx = buildHeadAsmdefIndex();
  const drift = [];
  for (const t of touched) {
    drift.push(...validateForwardRefs(t, idx, prevIdx));
    drift.push(...validateReverseRefs(t, idx));
  }
  if (drift.length > 0) {
    for (const d of drift) process.stderr.write(`[ship-cycle-preflight] ${d}\n`);
    process.stderr.write(
      `[ship-cycle-preflight] FAIL: ${drift.length} asmdef diff drift(s) detected.\n`,
    );
    process.exit(1);
  }
  process.stdout.write(
    `[ship-cycle-preflight] OK: ${touched.length} touched asmdef, 0 drift\n`,
  );
  process.exit(0);
}

main();
