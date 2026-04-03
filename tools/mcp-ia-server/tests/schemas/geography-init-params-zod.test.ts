/**
 * Zod parity with docs/schemas/geography-init-params.v1.schema.json fixtures.
 */

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import { safeParseGeographyInitParamsV1 } from "../../src/schemas/geography-init-params-zod.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../..");

function readJson(rel: string): unknown {
  const raw = fs.readFileSync(path.join(repoRoot, rel), "utf8");
  return JSON.parse(raw) as unknown;
}

describe("geographyInitParamsZodSchema", () => {
  it("accepts geography-init-params.good.json", () => {
    const data = readJson("docs/schemas/fixtures/geography-init-params.good.json");
    const r = safeParseGeographyInitParamsV1(data);
    assert.equal(r.success, true);
  });

  it("rejects bad-wrong-artifact.json", () => {
    const data = readJson("docs/schemas/fixtures/geography-init-params.bad-wrong-artifact.json");
    const r = safeParseGeographyInitParamsV1(data);
    assert.equal(r.success, false);
  });

  it("rejects bad-missing-map.json", () => {
    const data = readJson("docs/schemas/fixtures/geography-init-params.bad-missing-map.json");
    const r = safeParseGeographyInitParamsV1(data);
    assert.equal(r.success, false);
  });
});
