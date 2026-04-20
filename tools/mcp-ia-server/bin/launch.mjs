#!/usr/bin/env node
/**
 * MCP server launcher (TECH-495 / B4).
 *
 * Default: exec compiled `dist/index.js` (~200 ms cold-start).
 * Dev fallback: `MCP_SOURCE_MODE=1` → exec `tsx src/index.ts` (live reload).
 *
 * Keeps `.mcp.json` → a single `node tools/mcp-ia-server/bin/launch.mjs`
 * entry while preserving dev ergonomics for contributors editing source.
 */
import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const serverRoot = path.resolve(here, "..");
const sourceMode = process.env.MCP_SOURCE_MODE === "1";

let command;
let args;

if (sourceMode) {
  command = path.join(serverRoot, "node_modules", ".bin", "tsx");
  args = [path.join(serverRoot, "src", "index.ts")];
} else {
  command = process.execPath;
  args = [path.join(serverRoot, "dist", "index.js")];
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
