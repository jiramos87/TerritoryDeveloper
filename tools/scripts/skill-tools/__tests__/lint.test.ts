import test from "node:test";
import assert from "node:assert/strict";
import { lint } from "../lint.js";

test("lint runs over all skills without throwing", () => {
  const report = lint();
  assert.ok(report.slugs_checked > 0, "expected at least one skill");
  assert.equal(typeof report.errors, "number");
  assert.equal(typeof report.warnings, "number");
});

test("lint --strict promotes warnings to errors", () => {
  const baseline = lint();
  const strict = lint({ promoteWarnings: true });
  assert.equal(strict.errors, baseline.errors + baseline.warnings);
  assert.equal(strict.warnings, 0);
});

test("lint over single slug returns subset", () => {
  const slug = "ship";
  const report = lint({ slugs: [slug] });
  assert.equal(report.slugs_checked, 1);
  assert.ok(report.findings.every((f) => f.slug === slug));
});
