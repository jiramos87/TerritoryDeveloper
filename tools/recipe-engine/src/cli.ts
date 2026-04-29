#!/usr/bin/env node
/**
 * recipe:run CLI — DEC-A19 Phase B.
 *
 * Usage:
 *   npm run recipe:run -- {name} [--dry-run] [--inputs path/to/inputs.json] [--input k=v ...]
 *
 * Examples:
 *   npm run recipe:run -- release-rollout-track --dry-run
 *   npm run recipe:run -- author-spec --inputs /tmp/inputs.json
 *
 * Exit codes:
 *   0 = run ok
 *   1 = run failed (recipe not found / schema invalid / step failure)
 */

import fs from "node:fs";
import { runRecipe } from "./run.js";
import { setMcpInvoker } from "./steps/mcp.js";
import { createStdioMcpClient, type StdioMcpClient } from "./mcp-client.js";

interface CliArgs {
  name: string;
  dry_run: boolean;
  inputs: Record<string, unknown>;
  run_id?: string;
}

function parseArgs(argv: string[]): CliArgs {
  const out: CliArgs = { name: "", dry_run: false, inputs: {} };
  let i = 0;
  if (argv[0] && !argv[0].startsWith("--")) {
    out.name = argv[0];
    i = 1;
  }
  while (i < argv.length) {
    const a = argv[i];
    if (a === "--dry-run") {
      out.dry_run = true;
      i += 1;
    } else if (a === "--inputs") {
      const p = argv[i + 1];
      if (!p) throw new Error("--inputs requires a path");
      const raw = JSON.parse(fs.readFileSync(p, "utf8"));
      out.inputs = { ...out.inputs, ...raw };
      i += 2;
    } else if (a === "--input") {
      const kv = argv[i + 1] ?? "";
      const eq = kv.indexOf("=");
      if (eq <= 0) throw new Error(`--input expects key=value (got ${kv})`);
      const key = kv.slice(0, eq);
      const val = kv.slice(eq + 1);
      out.inputs[key] = coerce(val);
      i += 2;
    } else if (a === "--run-id") {
      out.run_id = argv[i + 1];
      i += 2;
    } else if (a === "--name") {
      out.name = argv[i + 1];
      i += 2;
    } else {
      throw new Error(`Unknown arg: ${a}`);
    }
  }
  if (!out.name) throw new Error("recipe name required (first positional or --name)");
  return out;
}

function coerce(s: string): unknown {
  if (s === "true") return true;
  if (s === "false") return false;
  if (s === "null") return null;
  if (/^-?\d+$/.test(s)) return Number(s);
  if (/^-?\d+\.\d+$/.test(s)) return Number(s);
  if (s.startsWith("{") || s.startsWith("[")) {
    try {
      return JSON.parse(s);
    } catch {
      return s;
    }
  }
  return s;
}

async function main(): Promise<void> {
  const argv = process.argv.slice(2);
  let parsed: CliArgs;
  try {
    parsed = parseArgs(argv);
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error(`[recipe:run] arg error: ${msg}`);
    process.exit(1);
    return;
  }

  let mcp: StdioMcpClient | undefined;
  try {
    if (!parsed.dry_run) {
      mcp = await createStdioMcpClient();
      setMcpInvoker(mcp.invoke);
    }
    const result = await runRecipe(parsed.name, {
      inputs: parsed.inputs,
      dry_run: parsed.dry_run,
      run_id: parsed.run_id,
    });
    if (!result.ok) {
      console.error(`[recipe:run] FAIL ${result.recipe || parsed.name} run_id=${result.run_id}`);
      console.error(JSON.stringify(result.error, null, 2));
      process.exit(1);
      return;
    }
    console.log(
      JSON.stringify(
        {
          ok: true,
          recipe: result.recipe,
          run_id: result.run_id,
          outputs: result.outputs,
          steps: result.step_results.length,
        },
        null,
        2,
      ),
    );
  } finally {
    setMcpInvoker(undefined);
    if (mcp) await mcp.close();
  }
}

main().catch((err) => {
  console.error(`[recipe:run] uncaught:`, err);
  process.exit(1);
});
