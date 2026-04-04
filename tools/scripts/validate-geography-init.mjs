#!/usr/bin/env node
/**
 * Light structural check for Unity-exported geography init report (TECH-39 §7.11.4).
 * Usage: node tools/scripts/validate-geography-init.mjs [path]
 * Default path: tools/reports/last-geography-init.json
 */
import fs from "node:fs";
import path from "node:path";

const defaultPath = path.join("tools", "reports", "last-geography-init.json");
const filePath = path.resolve(process.argv[2] ?? defaultPath);

if (!fs.existsSync(filePath)) {
  console.error(`validate-geography-init: file not found: ${filePath}`);
  process.exit(1);
}

let data;
try {
  data = JSON.parse(fs.readFileSync(filePath, "utf8"));
} catch (e) {
  console.error(`validate-geography-init: invalid JSON: ${e.message}`);
  process.exit(1);
}

const errors = [];
if (data.artifact !== "geography_init_report") {
  errors.push(`expected artifact "geography_init_report", got ${JSON.stringify(data.artifact)}`);
}
if (data.schema_version !== 1) {
  errors.push(`expected schema_version 1, got ${data.schema_version}`);
}
if (typeof data.master_seed !== "number" || !Number.isFinite(data.master_seed)) {
  errors.push("master_seed must be a finite number");
}
if (typeof data.map_width !== "number" || typeof data.map_height !== "number") {
  errors.push("map_width and map_height must be numbers");
}
if (typeof data.generate_standard_water_bodies !== "boolean") {
  errors.push("generate_standard_water_bodies must be boolean");
}
if (typeof data.procedural_rivers_enabled_effective !== "boolean") {
  errors.push("procedural_rivers_enabled_effective must be boolean");
}

const snapshotJson = data.interchange_snapshot_json;
if (snapshotJson === "") {
  errors.push(
    "interchange_snapshot_json must be omitted when empty, not a zero-length string (re-export from Unity)",
  );
}
const hasSnapshot =
  snapshotJson != null &&
  typeof snapshotJson === "string" &&
  snapshotJson.length > 0;
if (data.interchange_file_was_applied === true) {
  if (!hasSnapshot) {
    errors.push(
      "interchange_file_was_applied is true but interchange_snapshot_json is missing or empty",
    );
  } else {
    let inner;
    try {
      inner = JSON.parse(snapshotJson);
    } catch (e) {
      errors.push(`interchange_snapshot_json is not valid JSON: ${e.message}`);
    }
    if (inner != null && inner.artifact !== "geography_init_params") {
      errors.push(
        `expected interchange_snapshot_json.artifact "geography_init_params", got ${JSON.stringify(inner.artifact)}`,
      );
    }
  }
} else if (hasSnapshot) {
  errors.push(
    "interchange_file_was_applied is false but interchange_snapshot_json is set (use omitted field when no interchange)",
  );
}

// Legacy export shape: nested object was misleading when null (JsonUtility empty object).
if (data.interchange_snapshot != null) {
  errors.push(
    'obsolete field "interchange_snapshot" present — use interchange_snapshot_json only; re-export from Unity',
  );
}

if (errors.length) {
  console.error("validate-geography-init failed:");
  for (const msg of errors) console.error(`  - ${msg}`);
  process.exit(1);
}

console.log(`validate-geography-init: OK (${filePath})`);
process.exit(0);
