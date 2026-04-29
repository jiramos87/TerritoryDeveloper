#!/usr/bin/env node
/**
 * validate:seam-golden — DEC-A19 Phase A seam golden harness.
 *
 * Walks tools/seams/{name}/golden/*.json snapshot pairs and asserts each
 * { input, output } pair validates against the seam's input.schema.json /
 * output.schema.json contracts. Exits non-zero on first failure.
 *
 * No LLM dispatch — pure offline schema gate. Phase B layer (recipe engine)
 * will additionally re-run each input through the live seam and diff against
 * the recorded output for prompt-drift detection.
 */

import { promises as fs } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Ajv from "../mcp-ia-server/node_modules/ajv/dist/ajv.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const SEAMS_ROOT = path.join(REPO_ROOT, "tools", "seams");

async function listSeams() {
  const entries = await fs.readdir(SEAMS_ROOT, { withFileTypes: true });
  return entries
    .filter((e) => e.isDirectory())
    .map((e) => e.name)
    .sort();
}

async function readJson(p) {
  const txt = await fs.readFile(p, "utf8");
  try {
    return JSON.parse(txt);
  } catch (err) {
    throw new Error(`failed to parse JSON at ${path.relative(REPO_ROOT, p)}: ${err.message}`);
  }
}

async function listGoldens(seamDir) {
  const goldenDir = path.join(seamDir, "golden");
  try {
    const entries = await fs.readdir(goldenDir);
    return entries
      .filter((f) => f.endsWith(".json"))
      .map((f) => path.join(goldenDir, f))
      .sort();
  } catch (err) {
    if (err.code === "ENOENT") return [];
    throw err;
  }
}

function formatErrors(errs) {
  if (!errs || errs.length === 0) return "(no error detail)";
  return errs
    .map((e) => `    - ${e.instancePath || "/"} ${e.message}${e.params ? " " + JSON.stringify(e.params) : ""}`)
    .join("\n");
}

async function main() {
  const ajv = new Ajv.default({ allErrors: true, strict: false });
  const seams = await listSeams();
  if (seams.length === 0) {
    console.error(`[validate:seam-golden] no seams found under ${path.relative(REPO_ROOT, SEAMS_ROOT)}`);
    process.exit(1);
  }

  let totalGoldens = 0;
  let totalPairs = 0;
  const failures = [];

  for (const seam of seams) {
    const seamDir = path.join(SEAMS_ROOT, seam);
    const inputSchemaPath = path.join(seamDir, "input.schema.json");
    const outputSchemaPath = path.join(seamDir, "output.schema.json");

    let inputSchema;
    let outputSchema;
    try {
      inputSchema = await readJson(inputSchemaPath);
      outputSchema = await readJson(outputSchemaPath);
    } catch (err) {
      failures.push(`[${seam}] schema load failed: ${err.message}`);
      continue;
    }

    const validateInput = ajv.compile(inputSchema);
    const validateOutput = ajv.compile(outputSchema);

    const goldens = await listGoldens(seamDir);
    if (goldens.length === 0) {
      failures.push(`[${seam}] no golden snapshots under tools/seams/${seam}/golden/ (need ≥1)`);
      continue;
    }
    totalGoldens += goldens.length;

    for (const goldenPath of goldens) {
      const rel = path.relative(REPO_ROOT, goldenPath);
      let snapshot;
      try {
        snapshot = await readJson(goldenPath);
      } catch (err) {
        failures.push(`[${rel}] parse failed: ${err.message}`);
        continue;
      }
      if (typeof snapshot !== "object" || snapshot === null || !("input" in snapshot) || !("output" in snapshot)) {
        failures.push(`[${rel}] expected { input, output } pair`);
        continue;
      }
      totalPairs++;
      if (!validateInput(snapshot.input)) {
        failures.push(`[${rel}] input failed input.schema.json:\n${formatErrors(validateInput.errors)}`);
      }
      if (!validateOutput(snapshot.output)) {
        failures.push(`[${rel}] output failed output.schema.json:\n${formatErrors(validateOutput.errors)}`);
      }
    }
  }

  if (failures.length > 0) {
    console.error("[validate:seam-golden] FAIL");
    for (const f of failures) console.error(f);
    console.error(`[validate:seam-golden] ${failures.length} failure(s) across ${seams.length} seam(s)`);
    process.exit(1);
  }

  console.log(
    `[validate:seam-golden] OK — ${seams.length} seam(s), ${totalGoldens} golden(s), ${totalPairs} pair(s) validated`,
  );
}

main().catch((err) => {
  console.error("[validate:seam-golden] uncaught error:", err);
  process.exit(1);
});
