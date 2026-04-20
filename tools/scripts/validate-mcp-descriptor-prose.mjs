#!/usr/bin/env node
/**
 * validate-mcp-descriptor-prose (TECH-499 / B9).
 *
 * Enforces a 120-character cap on every `.describe("...")` string literal
 * under `tools/mcp-ia-server/src/tools/*.ts`. Param-level descriptors in
 * Zod schemas + top-level tool descriptions share the same budget so that
 * the MCP server's session-start tool-list payload stays bounded.
 *
 * Heuristic parser (regex):
 *  - matches `.describe("...")` and `.describe('...')`
 *  - counts characters of the inner string literal (excluding quotes)
 *  - ignores back-tick template strings (those are caught by tsc)
 *
 * Exit codes:
 *  - 0 : all descriptors within budget
 *  - 1 : one or more descriptors exceed 120 chars
 */
import fs from "node:fs";
import path from "node:path";
import url from "node:url";

const __dirname = path.dirname(url.fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");
const TOOLS_DIR = path.join(repoRoot, "tools/mcp-ia-server/src/tools");
const BUDGET = 120;

function collectFiles(dir) {
  const out = [];
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) out.push(...collectFiles(abs));
    else if (ent.isFile() && abs.endsWith(".ts") && !abs.endsWith(".d.ts")) {
      out.push(abs);
    }
  }
  return out;
}

/**
 * Walk file line-by-line; detect `.describe("...")` or `.describe('...')`
 * invocations and report overruns.
 */
function auditFile(filePath, offenders) {
  const raw = fs.readFileSync(filePath, "utf8");
  const lines = raw.split("\n");
  // Regex: capture the first string-literal argument. Non-greedy, single-line.
  const re = /\.describe\s*\(\s*(["'])((?:\\.|(?!\1).)*?)\1\s*\)/g;
  for (let i = 0; i < lines.length; i++) {
    let m;
    while ((m = re.exec(lines[i])) !== null) {
      const body = m[2];
      // Un-escape common sequences to compute the user-visible length.
      const rendered = body
        .replace(/\\n/g, "\n")
        .replace(/\\t/g, "\t")
        .replace(/\\"/g, '"')
        .replace(/\\'/g, "'")
        .replace(/\\\\/g, "\\");
      if (rendered.length > BUDGET) {
        offenders.push({
          file: path.relative(repoRoot, filePath),
          line: i + 1,
          length: rendered.length,
          preview: rendered.slice(0, 80),
        });
      }
    }
  }
}

const offenders = [];
for (const f of collectFiles(TOOLS_DIR)) {
  auditFile(f, offenders);
}

if (offenders.length === 0) {
  console.log(
    `validate-mcp-descriptor-prose: OK (all .describe() ≤ ${BUDGET} chars).`,
  );
  process.exit(0);
}

console.error(
  `validate-mcp-descriptor-prose: DRIFT — ${offenders.length} descriptor(s) over ${BUDGET} chars.`,
);
for (const o of offenders) {
  console.error(`  ${o.file}:${o.line} [${o.length}] "${o.preview}…"`);
}
process.exit(1);
