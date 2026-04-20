#!/usr/bin/env node
/**
 * validate-telemetry-schema.mjs
 * Validates every JSONL file under .claude/telemetry/ against the 8-field
 * telemetry schema authored by TECH-510.
 *
 * Required fields and types:
 *   ts                  — number, 13-digit epoch ms
 *   session_id          — string, non-empty
 *   total_input_tokens  — number, >= 0
 *   cache_read_tokens   — number, >= 0
 *   cache_write_tokens  — number, >= 0
 *   mcp_cold_start_ms   — number, >= 0
 *   hook_fork_count     — number, >= 0
 *   hook_fork_total_ms  — number, >= 0
 *
 * Exit 0: all files valid (or no .jsonl files found).
 * Exit 1: any schema violation or parse error.
 */

import { createReadStream, existsSync, readdirSync } from "fs";
import { createInterface } from "readline";
import { join } from "path";

// ---------------------------------------------------------------------------
// Schema definition
// ---------------------------------------------------------------------------

/** @typedef {{ name: string, type: string, check: (v: unknown) => boolean, note: string }} FieldRule */

/** @type {FieldRule[]} */
const REQUIRED_FIELDS = [
  {
    name: "ts",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 1e12,
    note: "13-digit epoch ms",
  },
  {
    name: "session_id",
    type: "string",
    check: (v) => typeof v === "string" && v.length > 0,
    note: "non-empty string",
  },
  {
    name: "total_input_tokens",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
  {
    name: "cache_read_tokens",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
  {
    name: "cache_write_tokens",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
  {
    name: "mcp_cold_start_ms",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
  {
    name: "hook_fork_count",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
  {
    name: "hook_fork_total_ms",
    type: "number",
    check: (v) => typeof v === "number" && Number.isFinite(v) && v >= 0,
    note: ">= 0",
  },
];

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Validate a parsed JSON object against REQUIRED_FIELDS.
 * @param {unknown} obj
 * @returns {{ missing: string[], typeMismatch: string[] }}
 */
function validateRow(obj) {
  const missing = [];
  const typeMismatch = [];
  if (typeof obj !== "object" || obj === null || Array.isArray(obj)) {
    return { missing: REQUIRED_FIELDS.map((f) => f.name), typeMismatch: [] };
  }
  for (const field of REQUIRED_FIELDS) {
    if (!(field.name in obj)) {
      missing.push(field.name);
    } else if (!field.check(/** @type {any} */ (obj)[field.name])) {
      typeMismatch.push(
        `${field.name} (expected ${field.type} ${field.note}, got ${JSON.stringify(/** @type {any} */ (obj)[field.name])})`
      );
    }
  }
  return { missing, typeMismatch };
}

/**
 * Process one JSONL file line-by-line using streaming readline.
 * @param {string} filePath
 * @returns {Promise<{ rowCount: number, errors: string[] }>}
 */
function processFile(filePath) {
  return new Promise((resolve, reject) => {
    const errors = [];
    let lineNumber = 0;
    let rowCount = 0;

    const rl = createInterface({
      input: createReadStream(filePath, { encoding: "utf8" }),
      crlfDelay: Infinity,
    });

    rl.on("line", (raw) => {
      lineNumber++;
      const line = raw.trim();
      if (line === "") return; // skip blank / trailing newlines

      let obj;
      try {
        obj = JSON.parse(line);
      } catch (e) {
        errors.push(
          `${filePath}:${lineNumber} — JSON parse error: ${e.message}`
        );
        return;
      }

      rowCount++;
      const { missing, typeMismatch } = validateRow(obj);
      if (missing.length > 0) {
        errors.push(
          `${filePath}:${lineNumber} — missing fields: ${missing.join(", ")}`
        );
      }
      if (typeMismatch.length > 0) {
        errors.push(
          `${filePath}:${lineNumber} — type mismatch: ${typeMismatch.join("; ")}`
        );
      }
    });

    rl.on("close", () => resolve({ rowCount, errors }));
    rl.on("error", reject);
  });
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const telemetryDir = ".claude/telemetry";

  if (!existsSync(telemetryDir)) {
    console.log("telemetry-schema: dir absent — ok (empty)");
    process.exit(0);
  }

  const allFiles = readdirSync(telemetryDir).filter((f) =>
    f.endsWith(".jsonl")
  );

  if (allFiles.length === 0) {
    console.log("telemetry-schema: 0 files, 0 rows — ok (empty)");
    process.exit(0);
  }

  let totalRows = 0;
  const allErrors = [];

  for (const filename of allFiles.sort()) {
    const filePath = join(telemetryDir, filename);
    const { rowCount, errors } = await processFile(filePath);
    totalRows += rowCount;
    allErrors.push(...errors);
  }

  if (allErrors.length > 0) {
    for (const err of allErrors) {
      console.error(err);
    }
    process.exit(1);
  }

  console.log(
    `telemetry-schema: ${allFiles.length} file${allFiles.length === 1 ? "" : "s"}, ${totalRows} row${totalRows === 1 ? "" : "s"} — ok`
  );
  process.exit(0);
}

main().catch((err) => {
  console.error(`telemetry-schema: unexpected error — ${err.message}`);
  process.exit(1);
});
