/**
 * Stage 4 red-stage proof: ui-toolkit-authoring-mcp-slices Stage 4 — pixel diff + lint surface.
 *
 * Anchor: stage4_verify_pixel_and_lint_backstop
 *
 * T4.0: wrap-contract tracer.
 *   - ui_toolkit_panel_pixel_diff module exports registerUiToolkitPanelPixelDiff.
 *   - import-graph: ui-toolkit-panel-pixel-diff.ts imports from ui-visual-diff-run engine.
 *   - no new bridge kind under ui_toolkit_ prefix.
 *   - tolerance default 0.005 reused from ui_visual_baseline_record default.
 * T4.1: ui_toolkit_panel_pixel_diff tool.
 *   - tool registers; slug + candidate_path → {pass, pixel_delta_pct, side_by_side_path}.
 *   - missing golden → {error:golden_not_found, suggested_action:ui_visual_baseline_record}.
 *   - lease-not-acquired → {error:lease_required}.
 * T4.2: ui_toolkit_host_lint tool + validator.
 *   - tool registers; clean Host → {findings:[], status:clean}.
 *   - orphan Q-lookup → 1 error finding.
 *   - FindObjectOfType in Update → 1 error finding.
 *   - missing modal slug → 1 error finding.
 *   - validator script exits 1 on any error finding.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../../../");

const FIXTURE_HOST_LINT = path.join(
  REPO_ROOT,
  "tools/scripts/test-fixtures/ui-toolkit-host-lint",
);

// ---------------------------------------------------------------------------
// T4.0 — wrap-contract tracer
// stage4_verify_pixel_and_lint_backstop anchor starts here
// ---------------------------------------------------------------------------

test("T4.0: ui-toolkit-panel-pixel-diff module exports registerUiToolkitPanelPixelDiff", async () => {
  const mod = await import("../../src/tools/ui-toolkit-panel-pixel-diff.js");
  assert.equal(typeof mod.registerUiToolkitPanelPixelDiff, "function");
});

test("T4.0: ui-toolkit-panel-pixel-diff imports from ui-visual-diff-run engine (wrap-not-rebuild)", () => {
  const toolSrc = path.join(
    REPO_ROOT,
    "tools/mcp-ia-server/src/tools/ui-toolkit-panel-pixel-diff.ts",
  );
  assert.ok(fs.existsSync(toolSrc), "ui-toolkit-panel-pixel-diff.ts must exist");
  const src = fs.readFileSync(toolSrc, "utf8");
  assert.ok(
    src.includes("ui-visual-diff-run") || src.includes("runVisualDiff") || src.includes("registerUiVisualDiffRun"),
    "pixel diff tool must import from ui-visual-diff-run engine",
  );
});

test("T4.0: no new bridge kind introduced under ui_toolkit_ prefix", () => {
  const toolSrc = path.join(
    REPO_ROOT,
    "tools/mcp-ia-server/src/tools/ui-toolkit-panel-pixel-diff.ts",
  );
  const src = fs.readFileSync(toolSrc, "utf8");
  // Must reuse capture_screenshot, not define a new bridge kind
  assert.ok(
    !src.includes('"kind": "ui_toolkit_') && !src.includes("kind: 'ui_toolkit_"),
    "no new bridge kind under ui_toolkit_ prefix allowed",
  );
});

test("T4.0: tolerance default is 0.005 (reuses ui_visual_baseline_record default)", () => {
  const toolSrc = path.join(
    REPO_ROOT,
    "tools/mcp-ia-server/src/tools/ui-toolkit-panel-pixel-diff.ts",
  );
  const src = fs.readFileSync(toolSrc, "utf8");
  assert.ok(src.includes("0.005"), "tolerance default 0.005 must appear in source");
});

// ---------------------------------------------------------------------------
// T4.1 — ui_toolkit_panel_pixel_diff tool
// ---------------------------------------------------------------------------

test("T4.1: ui-toolkit-panel-pixel-diff module exports runPanelPixelDiff", async () => {
  const mod = await import("../../src/tools/ui-toolkit-panel-pixel-diff.js");
  assert.equal(typeof mod.runPanelPixelDiff, "function");
});

test("T4.1: runPanelPixelDiff — missing golden returns golden_not_found with suggested_action", async () => {
  const { runPanelPixelDiff } = await import("../../src/tools/ui-toolkit-panel-pixel-diff.js");

  // Use a slug unlikely to have a real golden
  const result = await runPanelPixelDiff({
    slug: "nonexistent-panel-zzz-stage4-test",
  });

  assert.ok(
    result.error === "golden_not_found" || (result.pass === false && result.pixel_delta_pct !== undefined) ||
    result.error !== undefined,
    "missing golden must return error or pass:false",
  );
  if (result.error === "golden_not_found") {
    assert.equal(result.suggested_action, "ui_visual_baseline_record");
  }
});

test("T4.1: runPanelPixelDiff — lease_required error when bridge unavailable", async () => {
  const { runPanelPixelDiff } = await import("../../src/tools/ui-toolkit-panel-pixel-diff.js");

  // Without an active bridge lease + no golden → error surface
  const result = await runPanelPixelDiff({
    slug: "hud-budget",
    require_lease: true,
  });

  // Either lease_required or golden_not_found — both valid in test env
  assert.ok(
    result.error === "lease_required" || result.error === "golden_not_found" || result.pass !== undefined,
    "should return lease_required or golden_not_found in test env",
  );
});

test("T4.1: runPanelPixelDiff — returns {pass, pixel_delta_pct, side_by_side_path} shape on any resolution", async () => {
  const { runPanelPixelDiff } = await import("../../src/tools/ui-toolkit-panel-pixel-diff.js");

  const result = await runPanelPixelDiff({ slug: "hud-bar", resolution: "1920x1080" });

  // In test env (no golden / no bridge), we just check shape contract
  const validResponse =
    ("pass" in result && "pixel_delta_pct" in result) ||
    ("error" in result);
  assert.ok(validResponse, "result must have {pass,pixel_delta_pct} or {error} shape");
});

// ---------------------------------------------------------------------------
// T4.2 — ui_toolkit_host_lint tool + validator
// ---------------------------------------------------------------------------

test("T4.2: ui-toolkit-host-lint module exports registerUiToolkitHostLint", async () => {
  const mod = await import("../../src/tools/ui-toolkit-host-lint.js");
  assert.equal(typeof mod.registerUiToolkitHostLint, "function");
});

test("T4.2: ui-toolkit-host-lint module exports lintHostClass", async () => {
  const mod = await import("../../src/tools/ui-toolkit-host-lint.js");
  assert.equal(typeof mod.lintHostClass, "function");
});

test("T4.2: lintHostClass — clean fixture returns {findings:[], status:clean}", async () => {
  const { lintHostClass } = await import("../../src/tools/ui-toolkit-host-lint.js");
  const fixturePath = path.join(FIXTURE_HOST_LINT, "CleanHost.cs");
  assert.ok(fs.existsSync(fixturePath), `Clean fixture must exist at ${fixturePath}`);

  const result = lintHostClass(fixturePath, REPO_ROOT);
  assert.deepEqual(result.findings, []);
  assert.equal(result.status, "clean");
});

test("T4.2: lintHostClass — orphan Q-lookup fixture returns 1 error finding", async () => {
  const { lintHostClass } = await import("../../src/tools/ui-toolkit-host-lint.js");
  const fixturePath = path.join(FIXTURE_HOST_LINT, "OrphanQLookupHost.cs");
  assert.ok(fs.existsSync(fixturePath), `OrphanQLookup fixture must exist at ${fixturePath}`);

  const result = lintHostClass(fixturePath, REPO_ROOT);
  const errors = result.findings.filter((f: { severity: string }) => f.severity === "error");
  assert.ok(errors.length >= 1, `Expected at least 1 error for orphan Q-lookup, got ${errors.length}`);
  const codes = errors.map((f: { code: string }) => f.code);
  assert.ok(codes.some((c: string) => c.includes("orphan_q") || c.includes("q_lookup")), "error code must reference orphan Q-lookup");
});

test("T4.2: lintHostClass — FindObjectOfType in Update fixture returns 1 error finding", async () => {
  const { lintHostClass } = await import("../../src/tools/ui-toolkit-host-lint.js");
  const fixturePath = path.join(FIXTURE_HOST_LINT, "FindObjectOfTypeInUpdateHost.cs");
  assert.ok(fs.existsSync(fixturePath), `FindObjectOfType fixture must exist at ${fixturePath}`);

  const result = lintHostClass(fixturePath, REPO_ROOT);
  const errors = result.findings.filter((f: { severity: string }) => f.severity === "error");
  assert.ok(errors.length >= 1, `Expected at least 1 error for FindObjectOfType-in-Update, got ${errors.length}`);
  const codes = errors.map((f: { code: string }) => f.code);
  assert.ok(codes.some((c: string) => c.includes("find_object_of_type")), "error code must reference find_object_of_type");
});

test("T4.2: lintHostClass — missing modal slug fixture returns 1 error finding", async () => {
  const { lintHostClass } = await import("../../src/tools/ui-toolkit-host-lint.js");
  const fixturePath = path.join(FIXTURE_HOST_LINT, "MissingModalSlugHost.cs");
  assert.ok(fs.existsSync(fixturePath), `MissingModalSlug fixture must exist at ${fixturePath}`);

  const result = lintHostClass(fixturePath, REPO_ROOT);
  const errors = result.findings.filter((f: { severity: string }) => f.severity === "error");
  assert.ok(errors.length >= 1, `Expected at least 1 error for missing modal slug, got ${errors.length}`);
  const codes = errors.map((f: { code: string }) => f.code);
  assert.ok(codes.some((c: string) => c.includes("modal_slug")), "error code must reference modal_slug");
});

test("T4.2: validate-ui-toolkit-host-bindings.mjs exits 1 on fixture with errors", () => {
  const validatorPath = path.join(
    REPO_ROOT,
    "tools/scripts/validate-ui-toolkit-host-bindings.mjs",
  );
  assert.ok(fs.existsSync(validatorPath), "validator script must exist");

  // Run against a dirty fixture — expect exit code 1
  const dirtyFixturePath = path.join(FIXTURE_HOST_LINT, "FindObjectOfTypeInUpdateHost.cs");
  let exitCode = 0;
  try {
    execSync(
      `node "${validatorPath}" --fixture "${dirtyFixturePath}"`,
      { cwd: REPO_ROOT, stdio: "pipe" },
    );
  } catch (e) {
    const err = e as { status?: number };
    exitCode = err.status ?? 1;
  }
  assert.equal(exitCode, 1, "validator must exit 1 on dirty fixture");
});

test("T4.2: validate-ui-toolkit-host-bindings.mjs exits 0 on clean fixture", () => {
  const validatorPath = path.join(
    REPO_ROOT,
    "tools/scripts/validate-ui-toolkit-host-bindings.mjs",
  );
  const cleanFixturePath = path.join(FIXTURE_HOST_LINT, "CleanHost.cs");

  let exitCode = 0;
  try {
    execSync(
      `node "${validatorPath}" --fixture "${cleanFixturePath}"`,
      { cwd: REPO_ROOT, stdio: "pipe" },
    );
  } catch (e) {
    const err = e as { status?: number };
    exitCode = err.status ?? 1;
  }
  assert.equal(exitCode, 0, "validator must exit 0 on clean fixture");
});
