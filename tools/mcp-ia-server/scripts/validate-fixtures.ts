/**
 * Validate JSON fixtures under docs/schemas/fixtures against checked-in JSON Schemas (CI).
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import { safeParseGeographyInitParamsV1 } from "../src/schemas/geography-init-params-zod.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../..");

interface FixtureSet {
  schemaRelative: string;
  valid: string[];
  invalid: string[];
}

const FIXTURE_SETS: FixtureSet[] = [
  {
    schemaRelative: "docs/schemas/geography-init-params.v1.schema.json",
    valid: ["docs/schemas/fixtures/geography-init-params.good.json"],
    invalid: [
      "docs/schemas/fixtures/geography-init-params.bad-wrong-artifact.json",
      "docs/schemas/fixtures/geography-init-params.bad-missing-map.json",
    ],
  },
];

function readJson(filePath: string): unknown {
  const raw = fs.readFileSync(filePath, "utf8");
  return JSON.parse(raw) as unknown;
}

function main(): void {
  const ajv = new Ajv2020({ allErrors: true, strict: true });
  let failed = false;

  for (const set of FIXTURE_SETS) {
    const schemaPath = path.join(repoRoot, set.schemaRelative);
    if (!fs.existsSync(schemaPath)) {
      console.error(`Missing schema: ${set.schemaRelative}`);
      failed = true;
      continue;
    }
    const schema = readJson(schemaPath);
    const validate = ajv.compile(schema);

    for (const rel of set.valid) {
      const p = path.join(repoRoot, rel);
      const data = readJson(p);
      if (!validate(data)) {
        console.error(`VALID fixture should pass: ${rel}`);
        console.error(validate.errors);
        failed = true;
      }
      if (set.schemaRelative.includes("geography-init-params")) {
        const zr = safeParseGeographyInitParamsV1(data);
        if (!zr.success) {
          console.error(`VALID fixture should pass Zod: ${rel}`);
          console.error(zr.error.flatten());
          failed = true;
        }
      }
    }

    for (const rel of set.invalid) {
      const p = path.join(repoRoot, rel);
      const data = readJson(p);
      if (validate(data)) {
        console.error(`INVALID fixture should fail: ${rel}`);
        failed = true;
      }
      if (set.schemaRelative.includes("geography-init-params")) {
        const zr = safeParseGeographyInitParamsV1(data);
        if (zr.success) {
          console.error(`INVALID fixture should fail Zod: ${rel}`);
          failed = true;
        }
      }
    }
  }

  if (failed) {
    process.exitCode = 1;
    return;
  }
  console.log("validate-fixtures: OK");
}

main();
