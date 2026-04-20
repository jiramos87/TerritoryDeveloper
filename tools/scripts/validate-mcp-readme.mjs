#!/usr/bin/env node
/**
 * validate-mcp-readme (TECH-497 / B6).
 *
 * Compares the tool-table in `tools/mcp-ia-server/README.md` against the set of
 * `server.registerTool("name", …)` call sites in `tools/mcp-ia-server/src/index.ts`
 * and `tools/mcp-ia-server/src/tools/*.ts`. Exits non-zero with a descriptive
 * diff when the two sets disagree (missing rows / extra rows / typos).
 *
 * Heuristic (per source-doc B6):
 *  - README row  = line matching `^\| \*\*\`tool_name\`\*\* \|`.
 *  - Tool source = every `registerTool("name"` or `registerTool('name'` literal
 *    in src/**.ts.
 */
import fs from "node:fs";
import path from "node:path";
import url from "node:url";

const __dirname = path.dirname(url.fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");

const README = path.join(repoRoot, "tools/mcp-ia-server/README.md");
const SRC_DIR = path.join(repoRoot, "tools/mcp-ia-server/src");

function collectSrcFiles(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...collectSrcFiles(abs));
    else if (ent.isFile() && abs.endsWith(".ts") && !abs.endsWith(".d.ts")) {
      out.push(abs);
    }
  }
  return out;
}

function extractRegisteredTools() {
  const names = new Set();
  const re = /registerTool\s*\(\s*["']([^"']+)["']/g;
  for (const file of collectSrcFiles(SRC_DIR)) {
    const raw = fs.readFileSync(file, "utf8");
    let m;
    while ((m = re.exec(raw)) !== null) {
      names.add(m[1]);
    }
  }
  return names;
}

function extractReadmeTools() {
  const raw = fs.readFileSync(README, "utf8");
  const names = new Set();
  const re = /^\|\s*\*\*`([^`]+)`\*\*\s*\|/gm;
  let m;
  while ((m = re.exec(raw)) !== null) {
    names.add(m[1]);
  }
  return names;
}

function diff(setA, setB) {
  return [...setA].filter((x) => !setB.has(x)).sort();
}

const registered = extractRegisteredTools();
const documented = extractReadmeTools();
const missingFromReadme = diff(registered, documented);
const extraInReadme = diff(documented, registered);

if (missingFromReadme.length === 0 && extraInReadme.length === 0) {
  console.log(
    `validate-mcp-readme: OK (${registered.size} tools registered, ${documented.size} documented).`,
  );
  process.exit(0);
}

console.error("validate-mcp-readme: DRIFT detected.");
console.error(`  registered (src): ${registered.size}`);
console.error(`  documented (README): ${documented.size}`);
if (missingFromReadme.length > 0) {
  console.error(`  missing from README (registered but not documented):`);
  for (const n of missingFromReadme) console.error(`    - ${n}`);
}
if (extraInReadme.length > 0) {
  console.error(`  extra in README (documented but not registered):`);
  for (const n of extraInReadme) console.error(`    - ${n}`);
}
process.exit(1);
