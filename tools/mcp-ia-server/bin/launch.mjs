#!/usr/bin/env node
/**
 * MCP server launcher (TECH-495 / B4).
 *
 * Default: exec compiled `dist/{entry}.js` (~200 ms cold-start).
 * If `dist/` missing (gitignored) → same as dev: `tsx src/{entry}.ts` + stderr hint to run `npm run build`.
 * Dev override: `MCP_SOURCE_MODE=1` → always `tsx` (live reload).
 *
 * Keeps `.mcp.json` → a single `node tools/mcp-ia-server/bin/launch.mjs`
 * entry while preserving dev ergonomics for contributors editing source.
 */
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const serverRoot = path.resolve(here, "..");
const sourceMode = process.env.MCP_SOURCE_MODE === "1";

// TECH-525 / B1 server split: MCP_ENTRY selects entry module name.
// Valid values: "index" (default — backward compat), "index-ia" (IA-core split),
// "index-bridge" (Unity-bridge + compute split). Invalid values fall back to "index".
const entryRaw = process.env.MCP_ENTRY ?? "index";
const entry = ["index", "index-ia", "index-bridge"].includes(entryRaw) ? entryRaw : "index";

const distJs = path.join(serverRoot, "dist", `${entry}.js`);
// dist/ is gitignored; fresh clones / IDE MCP need a path without a prior `npm run build`.
const useSource = sourceMode || !fs.existsSync(distJs);

let command;
let args;

if (useSource) {
  if (!sourceMode && !fs.existsSync(distJs)) {
    console.error(
      `[mcp-launcher] ${distJs} missing — using tsx (run: cd tools/mcp-ia-server && npm run build for faster cold start)`,
    );
  }
  command = path.join(serverRoot, "node_modules", ".bin", "tsx");
  args = [path.join(serverRoot, "src", `${entry}.ts`)];
} else {
  command = process.execPath;
  args = [distJs];
}

const child = spawn(command, args, {
  stdio: "inherit",
  env: process.env,
});

child.on("exit", (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 0);
});

child.on("error", (err) => {
  console.error(`[mcp-launcher] failed to spawn server: ${err.message}`);
  process.exit(1);
});
