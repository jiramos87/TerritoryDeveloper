#!/usr/bin/env node
/**
 * validate-asmdef-graph.mjs — structural asmdef graph + using-namespace leak gate.
 *
 * Layer 1 of the three-layer ship-cycle asmdef gate (TECH-30633):
 *   - Parses every `Assets/**\/*.asmdef`.
 *   - Builds GUID → asmdef-name index from sibling `.meta` files.
 *   - Resolves `references[]` (`GUID:xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` or bare-name form).
 *   - Tarjan SCC on directed graph; fail on any non-singleton SCC (cycle).
 *   - Per-`.cs` under `Assets/Scripts/Domains/**`: extract `using X.Y.Z;` clauses,
 *     resolve to owning asmdef via `rootNamespace` longest-prefix match,
 *     assert owning asmdef ∈ {self, self.references[]}. Leaks fail.
 *
 * Exit 0 = clean (stdout: `OK: N asmdef, M cs, 0 cycles, 0 leaks`).
 * Exit 1 = first violation (stderr: annotated path + offending edge).
 *
 * Wired into `validate:fast` baseline + `validate:all:readonly` chain.
 */

import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, dirname, resolve, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const REPO_ROOT = resolve(dirname(__filename), "..", "..");
const ASSETS_DIR = join(REPO_ROOT, "Assets");
const DOMAINS_DIR = join(REPO_ROOT, "Assets", "Scripts", "Domains");

function* walk(dir, pred) {
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
    if (st.isDirectory()) {
      yield* walk(full, pred);
    } else if (pred(entry, full)) {
      yield full;
    }
  }
}

function repoRel(abs) {
  return relative(REPO_ROOT, abs).split(sep).join("/");
}

function readGuidFromMeta(asmdefAbs) {
  const metaAbs = `${asmdefAbs}.meta`;
  let raw;
  try {
    raw = readFileSync(metaAbs, "utf8");
  } catch {
    return null;
  }
  const m = raw.match(/^guid:\s*([0-9a-fA-F]{32})/m);
  return m ? m[1].toLowerCase() : null;
}

function loadAsmdefs() {
  // asmdefByPath: { path, name, rootNamespace, references[], guid }
  const all = [];
  for (const abs of walk(ASSETS_DIR, (e) => e.endsWith(".asmdef"))) {
    let parsed;
    try {
      parsed = JSON.parse(readFileSync(abs, "utf8"));
    } catch (e) {
      throw new Error(`PARSE ${repoRel(abs)}: ${e.message}`);
    }
    const name = typeof parsed.name === "string" ? parsed.name : null;
    if (!name) throw new Error(`MISSING name in ${repoRel(abs)}`);
    const guid = readGuidFromMeta(abs);
    const refs = Array.isArray(parsed.references) ? parsed.references : [];
    const rootNs = typeof parsed.rootNamespace === "string" ? parsed.rootNamespace : "";
    all.push({ path: abs, name, rootNamespace: rootNs, references: refs, guid });
  }
  return all;
}

function buildIndex(asmdefs) {
  const byName = new Map();
  const byGuid = new Map();
  for (const a of asmdefs) {
    byName.set(a.name, a);
    if (a.guid) byGuid.set(a.guid, a);
  }
  return { byName, byGuid };
}

function resolveRef(ref, idx) {
  // `GUID:xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx` or bare name.
  if (ref.startsWith("GUID:")) {
    const g = ref.slice(5).toLowerCase();
    return idx.byGuid.get(g) ?? null;
  }
  return idx.byName.get(ref) ?? null;
}

function buildGraph(asmdefs, idx) {
  // adj: Map<asmdef-name, Set<asmdef-name>>. Unresolved refs ignored (external packages).
  const adj = new Map();
  for (const a of asmdefs) {
    const outs = new Set();
    for (const ref of a.references) {
      const target = resolveRef(ref, idx);
      if (target) outs.add(target.name);
    }
    adj.set(a.name, outs);
  }
  return adj;
}

function tarjanSCC(adj) {
  // Returns Array<Array<node>> — each inner array is one SCC.
  const sccs = [];
  let index = 0;
  const stack = [];
  const inStack = new Set();
  const meta = new Map(); // node → { index, lowlink }

  function strongconnect(v) {
    meta.set(v, { index, lowlink: index });
    index++;
    stack.push(v);
    inStack.add(v);
    for (const w of adj.get(v) ?? []) {
      if (!meta.has(w)) {
        strongconnect(w);
        meta.get(v).lowlink = Math.min(meta.get(v).lowlink, meta.get(w).lowlink);
      } else if (inStack.has(w)) {
        meta.get(v).lowlink = Math.min(meta.get(v).lowlink, meta.get(w).index);
      }
    }
    if (meta.get(v).lowlink === meta.get(v).index) {
      const comp = [];
      while (true) {
        const w = stack.pop();
        inStack.delete(w);
        comp.push(w);
        if (w === v) break;
      }
      sccs.push(comp);
    }
  }

  for (const v of adj.keys()) {
    if (!meta.has(v)) strongconnect(v);
  }
  return sccs;
}

function detectCycles(adj) {
  const sccs = tarjanSCC(adj);
  const cycles = [];
  for (const comp of sccs) {
    if (comp.length > 1) {
      cycles.push(comp);
      continue;
    }
    // Self-loop edge case: single-node SCC with self-edge.
    const node = comp[0];
    if ((adj.get(node) ?? new Set()).has(node)) cycles.push(comp);
  }
  return cycles;
}

function extractUsings(csText) {
  const out = [];
  const rx = /^\s*using\s+([A-Za-z_][A-Za-z0-9_.]*)\s*;/gm;
  let m;
  while ((m = rx.exec(csText)) !== null) {
    out.push({ ns: m[1], offset: m.index });
  }
  return out;
}

function lineOf(text, offset) {
  let line = 1;
  for (let i = 0; i < offset && i < text.length; i++) {
    if (text[i] === "\n") line++;
  }
  return line;
}

function findOwningAsmdef(csAbs, asmdefs) {
  // The asmdef owning a .cs file = nearest ancestor dir containing a *.asmdef.
  let cur = dirname(csAbs);
  while (cur.startsWith(ASSETS_DIR) || cur === ASSETS_DIR) {
    for (const a of asmdefs) {
      if (dirname(a.path) === cur) return a;
    }
    const parent = dirname(cur);
    if (parent === cur) break;
    cur = parent;
  }
  return null;
}

function findAsmdefByNamespace(ns, asmdefs) {
  // Longest rootNamespace prefix wins. Asmdef with empty rootNamespace = no claim.
  let best = null;
  let bestLen = -1;
  for (const a of asmdefs) {
    const rn = a.rootNamespace;
    if (!rn) continue;
    if (ns === rn || ns.startsWith(rn + ".")) {
      if (rn.length > bestLen) {
        best = a;
        bestLen = rn.length;
      }
    }
  }
  return best;
}

function isStdlibOrEngineNs(ns) {
  // Skip Unity engine / .NET stdlib / common 3p. Heuristic — keeps validator focused on
  // intra-project graph correctness rather than external dep noise.
  if (ns.startsWith("System")) return true;
  if (ns.startsWith("UnityEngine")) return true;
  if (ns.startsWith("UnityEditor")) return true;
  if (ns.startsWith("Unity.")) return true;
  if (ns.startsWith("TMPro")) return true;
  if (ns.startsWith("NUnit")) return true;
  if (ns === "Newtonsoft" || ns.startsWith("Newtonsoft.")) return true;
  return false;
}

function detectUsingLeaks(asmdefs, idx) {
  // Scope: .cs under Assets/Scripts/Domains/**. Verify each `using` resolves to either
  // the file's owning asmdef or one of its declared refs (resolved name).
  const leaks = [];
  let scanned = 0;
  for (const csAbs of walk(DOMAINS_DIR, (e) => e.endsWith(".cs"))) {
    scanned++;
    let text;
    try {
      text = readFileSync(csAbs, "utf8");
    } catch {
      continue;
    }
    const owner = findOwningAsmdef(csAbs, asmdefs);
    if (!owner) continue;
    const declaredRefNames = new Set();
    for (const ref of owner.references) {
      const target = resolveRef(ref, idx);
      if (target) declaredRefNames.add(target.name);
    }
    declaredRefNames.add(owner.name);
    const usings = extractUsings(text);
    for (const u of usings) {
      if (isStdlibOrEngineNs(u.ns)) continue;
      const decl = findAsmdefByNamespace(u.ns, asmdefs);
      if (!decl) continue; // namespace not owned by any asmdef in repo → external
      if (decl.name === owner.name) continue; // own asmdef
      if (declaredRefNames.has(decl.name)) continue; // declared ref
      leaks.push({
        path: csAbs,
        line: lineOf(text, u.offset),
        ns: u.ns,
        owner: owner.name,
        decl: decl.name,
      });
      if (leaks.length >= 200) return { leaks, scanned };
    }
  }
  return { leaks, scanned };
}

function main() {
  let asmdefs;
  try {
    asmdefs = loadAsmdefs();
  } catch (e) {
    process.stderr.write(`[validate-asmdef-graph] ${e.message}\n`);
    process.exit(1);
  }
  const idx = buildIndex(asmdefs);
  const adj = buildGraph(asmdefs, idx);
  const cycles = detectCycles(adj);
  if (cycles.length > 0) {
    for (const comp of cycles) {
      process.stderr.write(
        `[validate-asmdef-graph] CYCLE detected (SCC size ${comp.length}): ${comp.join(" → ")} → ${comp[0]}\n`,
      );
    }
    process.stderr.write(
      `[validate-asmdef-graph] FAIL: ${cycles.length} non-singleton SCC(s) in asmdef graph.\n`,
    );
    process.exit(1);
  }
  const { leaks, scanned } = detectUsingLeaks(asmdefs, idx);
  if (leaks.length > 0) {
    for (const l of leaks.slice(0, 50)) {
      process.stderr.write(
        `[validate-asmdef-graph] LEAK ${repoRel(l.path)}:${l.line} — using ${l.ns} leaks into ${l.owner} (decl in ${l.decl}; refs missing).\n`,
      );
    }
    if (leaks.length > 50) {
      process.stderr.write(`[validate-asmdef-graph] (... ${leaks.length - 50} more leak(s))\n`);
    }
    process.stderr.write(
      `[validate-asmdef-graph] FAIL: ${leaks.length} using-namespace leak(s) under Assets/Scripts/Domains/.\n`,
    );
    process.exit(1);
  }
  process.stdout.write(
    `[validate-asmdef-graph] OK: ${asmdefs.length} asmdef, ${scanned} cs, 0 cycles, 0 leaks\n`,
  );
  process.exit(0);
}

main();
