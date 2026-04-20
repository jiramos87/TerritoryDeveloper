#!/usr/bin/env node

import { promises as fs } from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const allowlistPath = path.join(
  repoRoot,
  "tools",
  "mcp-ia-server",
  "src",
  "auth",
  "caller-allowlist.ts",
);
const outPath = path.join(
  repoRoot,
  ".cursor",
  "rules",
  "cursor-caller-agent-cheatsheet.mdc",
);

function parseAllowlist(source) {
  const blockMatch = source.match(/export const ALLOWLIST:[\s\S]*?=\s*\{([\s\S]*?)\}\s+as const;/);
  if (!blockMatch) {
    throw new Error("Could not locate ALLOWLIST object block.");
  }

  const body = blockMatch[1];
  const entries = [];
  const entryRegex = /^\s*([a-z_]+):\s*\[([\s\S]*?)\],/gm;
  let entryMatch = null;
  while ((entryMatch = entryRegex.exec(body)) !== null) {
    const tool = entryMatch[1];
    const listBody = entryMatch[2];
    const callers = [];
    const callerRegex = /"([^"]+)"/g;
    let callerMatch = null;
    while ((callerMatch = callerRegex.exec(listBody)) !== null) {
      callers.push(callerMatch[1]);
    }
    entries.push({ tool, callers });
  }
  return entries;
}

function buildMdc(entries) {
  const rows = entries
    .map(({ tool, callers }) => `| \`${tool}\` | ${callers.map((item) => `\`${item}\``).join(", ")} |`)
    .join("\n");

  return `---
description: "Quick mapping of MCP mutation tools to accepted caller_agent values."
alwaysApply: true
---

# MCP Caller-Agent Cheatsheet

Source of truth: \`tools/mcp-ia-server/src/auth/caller-allowlist.ts\`.

When invoking mutation/authorship tools, always pass \`caller_agent\`.

| MCP tool | Allowed \`caller_agent\` values |
| --- | --- |
${rows}

If tool not listed in allowlist map, caller gate is bypassed (read-only path).
`;
}

async function main() {
  const source = await fs.readFile(allowlistPath, "utf8");
  const entries = parseAllowlist(source);
  if (entries.length === 0) {
    throw new Error("No ALLOWLIST entries parsed.");
  }

  await fs.mkdir(path.dirname(outPath), { recursive: true });
  await fs.writeFile(outPath, buildMdc(entries), "utf8");
  console.log(`Wrote ${outPath} with ${entries.length} tool mappings.`);
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
