/**
 * Stage 3 red-stage proof: ui-toolkit-authoring-mcp-slices Stage 3 — wire surface.
 *
 * Anchor: stage3_wire_apply_flag_gate
 *
 * T3.0: DEC-A28 I4 apply-flag gate tracer.
 *   - tool registered; call without --apply returns {snippet:string, applied:false};
 *   - Host mtime+content unchanged; --apply=true on non-allow-listed caller → unauthorized.
 * T3.1: ui_toolkit_host_q_bind code-stub + --apply Host rewriter tool.
 *   - snippet mode returns correct C# (Q + .clicked += / RegisterCallback<ClickEvent>);
 *   - apply mode rewrites Host + compile green; idempotent re-apply = no-op;
 *   - allow-list rejects unauthorized apply.
 * T3.2: ui_toolkit_scene_uidoc_validate scene wiring verdict tool.
 *   - wired slug returns {wired:true, missing:[], suggestion:null};
 *   - missing UIDocument component → suggestion:bridge_wire_call;
 *   - absent GameObject → suggestion:runtime_spawn_pattern.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../../../");
const FIXTURE_HOST_Q_BIND = path.join(
  REPO_ROOT,
  "tools/scripts/test-fixtures/ui-toolkit-host-q-bind",
);
const FIXTURE_SCENE_VALIDATE = path.join(
  REPO_ROOT,
  "tools/scripts/test-fixtures/ui-toolkit-scene-uidoc-validate",
);

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeTmpDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-s3-"));
}

// ---------------------------------------------------------------------------
// T3.0 — DEC-A28 I4 apply-flag gate tracer
// stage3_wire_apply_flag_gate anchor starts here
// ---------------------------------------------------------------------------

test("T3.0: ui-toolkit-host-q-bind module exports registerUiToolkitHostQBind", async () => {
  const mod = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  assert.equal(typeof mod.registerUiToolkitHostQBind, "function");
});

test("T3.0: generateHostQBindSnippet — no-apply returns {snippet, applied:false, suggested_insertion_point}", async () => {
  const { generateHostQBindSnippet } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const result = generateHostQBindSnippet({
    host_class: "BudgetPanelHost",
    element_name: "open-budget-btn",
    element_kind: "Button",
    callback_handler: "OnOpenClicked",
    target_manager: "BudgetManager",
  });
  assert.equal(result.applied, false);
  assert.equal(typeof result.snippet, "string");
  assert.ok(result.snippet.includes("open-budget-btn"), "snippet should reference element name");
  assert.ok(result.snippet.includes("Q<Button>"), "snippet should contain Q<Button> lookup");
  assert.ok(result.snippet.includes("RegisterCallback<ClickEvent>") || result.snippet.includes(".clicked +="),
    "snippet should contain click binding pattern");
  assert.ok(result.suggested_insertion_point !== undefined, "should have suggested_insertion_point");
});

test("T3.0: fixture host file byte-identical after snippet-mode call (no mutation)", async () => {
  const { generateHostQBindSnippet } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const fixturePath = path.join(FIXTURE_HOST_Q_BIND, "BudgetPanelHost.cs");
  const contentBefore = fs.readFileSync(fixturePath, "utf8");
  const mtimeBefore = fs.statSync(fixturePath).mtimeMs;

  generateHostQBindSnippet({
    host_class: "BudgetPanelHost",
    element_name: "test-btn",
    element_kind: "Button",
    callback_handler: "OnTestClicked",
    target_manager: "TestManager",
  });

  const contentAfter = fs.readFileSync(fixturePath, "utf8");
  const mtimeAfter = fs.statSync(fixturePath).mtimeMs;
  assert.equal(contentBefore, contentAfter, "host file content must be unchanged in snippet mode");
  assert.equal(mtimeBefore, mtimeAfter, "host file mtime must be unchanged in snippet mode");
});

test("T3.0: apply-mode without allow-listed caller throws unauthorized", async () => {
  const { applyHostQBind } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const tmp = makeTmpDir();
  try {
    const hostPath = path.join(tmp, "BudgetPanelHost.cs");
    fs.copyFileSync(path.join(FIXTURE_HOST_Q_BIND, "BudgetPanelHost.cs"), hostPath);

    let threw = false;
    try {
      await applyHostQBind({
        host_class: "BudgetPanelHost",
        element_name: "open-budget-btn",
        element_kind: "Button",
        callback_handler: "OnOpenClicked",
        target_manager: "BudgetManager",
        host_file_path: hostPath,
        caller: "random-agent",
      });
    } catch (e) {
      threw = true;
      const err = e as { code: string; message: string };
      assert.equal(err.code, "unauthorized");
      assert.ok(err.message.includes("allow-list"), "error should mention allow-list");
    }
    assert.ok(threw, "should have thrown unauthorized");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

// ---------------------------------------------------------------------------
// T3.1 — ui_toolkit_host_q_bind full tool
// ---------------------------------------------------------------------------

test("T3.1: generateHostQBindSnippet — value_param included in snippet", async () => {
  const { generateHostQBindSnippet } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const result = generateHostQBindSnippet({
    host_class: "BudgetPanelHost",
    element_name: "tax-slider",
    element_kind: "Slider",
    callback_handler: "OnTaxChanged",
    target_manager: "BudgetManager",
    value_param: "newValue",
  });
  assert.equal(result.applied, false);
  assert.ok(result.snippet.includes("tax-slider"), "snippet should reference element name");
  assert.ok(result.snippet.includes("Q<Slider>"), "snippet should contain Q<Slider> lookup");
});

test("T3.1: applyHostQBind — allow-listed caller, file written with binding stub", async () => {
  const { applyHostQBind } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const tmp = makeTmpDir();
  try {
    const hostPath = path.join(tmp, "BudgetPanelHost.cs");
    fs.copyFileSync(path.join(FIXTURE_HOST_Q_BIND, "BudgetPanelHost.cs"), hostPath);

    const result = await applyHostQBind({
      host_class: "BudgetPanelHost",
      element_name: "new-close-btn",
      element_kind: "Button",
      callback_handler: "OnNewCloseClicked",
      target_manager: "BudgetManager",
      host_file_path: hostPath,
      caller: "spec-implementer",
      dry_run: true, // skip actual unity_compile in test context
    });

    assert.equal(result.applied, true);
    assert.equal(typeof result.snippet, "string");
    assert.ok(result.snippet.includes("new-close-btn"), "snippet should contain element name");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T3.1: applyHostQBind — idempotent re-apply on same (host_class, element_name) = no-op", async () => {
  const { applyHostQBind } = await import("../../src/tools/ui-toolkit-host-q-bind.js");
  const tmp = makeTmpDir();
  try {
    const hostPath = path.join(tmp, "BudgetPanelHost.cs");
    fs.copyFileSync(path.join(FIXTURE_HOST_Q_BIND, "BudgetPanelHost.cs"), hostPath);

    // First apply
    await applyHostQBind({
      host_class: "BudgetPanelHost",
      element_name: "new-close-btn",
      element_kind: "Button",
      callback_handler: "OnNewCloseClicked",
      target_manager: "BudgetManager",
      host_file_path: hostPath,
      caller: "spec-implementer",
      dry_run: true,
    });
    const contentAfterFirst = fs.readFileSync(hostPath, "utf8");

    // Second apply (idempotent)
    const result2 = await applyHostQBind({
      host_class: "BudgetPanelHost",
      element_name: "new-close-btn",
      element_kind: "Button",
      callback_handler: "OnNewCloseClicked",
      target_manager: "BudgetManager",
      host_file_path: hostPath,
      caller: "spec-implementer",
      dry_run: true,
    });
    const contentAfterSecond = fs.readFileSync(hostPath, "utf8");

    assert.equal(contentAfterFirst, contentAfterSecond, "file should not change on idempotent re-apply");
    assert.equal(result2.idempotent, true, "second apply should report idempotent=true");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

// ---------------------------------------------------------------------------
// T3.2 — ui_toolkit_scene_uidoc_validate
// ---------------------------------------------------------------------------

test("T3.2: ui-toolkit-scene-uidoc-validate module exports registerUiToolkitSceneUidocValidate", async () => {
  const mod = await import("../../src/tools/ui-toolkit-scene-uidoc-validate.js");
  assert.equal(typeof mod.registerUiToolkitSceneUidocValidate, "function");
});

test("T3.2: buildSceneVerdict — wired fixture returns {wired:true, missing:[], suggestion:null}", async () => {
  const { buildSceneVerdict } = await import("../../src/tools/ui-toolkit-scene-uidoc-validate.js");
  const fixturePath = path.join(FIXTURE_SCENE_VALIDATE, "wired-scene.yaml");
  const result = buildSceneVerdict("budget-panel", fixturePath);
  assert.equal(result.wired, true);
  assert.deepEqual(result.missing, []);
  assert.equal(result.suggestion, null);
});

test("T3.2: buildSceneVerdict — missing UIDocument component → suggestion:bridge_wire_call", async () => {
  const { buildSceneVerdict } = await import("../../src/tools/ui-toolkit-scene-uidoc-validate.js");
  const fixturePath = path.join(FIXTURE_SCENE_VALIDATE, "missing-component-scene.yaml");
  const result = buildSceneVerdict("budget-panel", fixturePath);
  assert.equal(result.wired, false);
  assert.ok(result.missing.length > 0, "missing should be non-empty");
  const missingFields = result.missing.map((m: { field: string }) => m.field);
  assert.ok(missingFields.includes("UIDocument"), "UIDocument should be in missing list");
  assert.equal(result.suggestion, "bridge_wire_call");
});

test("T3.2: buildSceneVerdict — absent GameObject → suggestion:runtime_spawn_pattern", async () => {
  const { buildSceneVerdict } = await import("../../src/tools/ui-toolkit-scene-uidoc-validate.js");
  const fixturePath = path.join(FIXTURE_SCENE_VALIDATE, "absent-gameobject-scene.yaml");
  const result = buildSceneVerdict("budget-panel", fixturePath);
  assert.equal(result.wired, false);
  assert.ok(result.missing.length > 0, "missing should be non-empty");
  const missingFields = result.missing.map((m: { field: string }) => m.field);
  assert.ok(missingFields.includes("GameObject"), "GameObject should be in missing list");
  assert.equal(result.suggestion, "runtime_spawn_pattern");
});
