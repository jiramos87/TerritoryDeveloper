/**
 * Stage 1 red-stage proof: ui-toolkit-authoring-mcp-slices Stage 1 — inspect surface.
 *
 * Anchor: stage1_inspect_surface_complete
 *
 * T1.0: IUIToolkitPanelBackend factory + DiskBackend + DbBackend stub
 * T1.1: ui_toolkit_panel_get tool registration + shape
 * T1.2: ui_toolkit_host_inspect tool registration + csharp-host-parser
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";

import { createBackend, DiskBackend, DbBackend } from "../../src/ia-db/ui-toolkit-backend.js";
import { parseUssFile } from "../../src/ia-db/uss-parser.js";
import { scanHostClass } from "../../src/ia-db/csharp-host-parser.js";

// Fixture paths
const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../../../");
const FIXTURE_PANEL_DIR = path.join(
  REPO_ROOT,
  "tools/scripts/test-fixtures/ui-toolkit-panel-get",
);
const FIXTURE_HOST_DIR = path.join(
  REPO_ROOT,
  "tools/scripts/test-fixtures/ui-toolkit-host-inspect",
);

// ---------------------------------------------------------------------------
// T1.0 — Factory + DiskBackend + DbBackend stub
// stage1_inspect_surface_complete anchor starts here
// ---------------------------------------------------------------------------

test("T1.0: createBackend({kind:'disk'}).kind === 'disk'", () => {
  const backend = createBackend({ kind: "disk" });
  assert.equal(backend.kind, "disk");
});

test("T1.0: createBackend({kind:'db'}).kind === 'db'", () => {
  const backend = createBackend({ kind: "db" });
  assert.equal(backend.kind, "db");
});

test("T1.0: DiskBackend is default when no kind specified", () => {
  const orig = process.env.UI_TOOLKIT_BACKEND;
  delete process.env.UI_TOOLKIT_BACKEND;
  const backend = createBackend();
  assert.equal(backend.kind, "disk");
  if (orig !== undefined) process.env.UI_TOOLKIT_BACKEND = orig;
});

test("T1.0: DbBackend.getPanel returns parked error (throws with ok:false)", async () => {
  const backend = createBackend({ kind: "db" });
  let threw = false;
  try {
    await backend.getPanel("any-slug");
  } catch (e) {
    threw = true;
    const err = e as { ok: false; error: string };
    assert.equal(err.ok, false);
    assert.equal(err.error, "db_backend_not_implemented");
  }
  assert.ok(threw, "DbBackend.getPanel should throw parked error");
});

test("T1.0: DiskBackend.getPanel returns exists:false for unknown slug", async () => {
  const backend = new DiskBackend(REPO_ROOT);
  const result = await backend.getPanel("nonexistent-panel-xyz");
  assert.equal(result.exists, false);
  assert.equal(result.uxml_content, null);
  assert.equal(result.uxml_tree, null);
});

test("T1.0: DiskBackend.getPanel finds fixture panel from search roots", async () => {
  // Set up a temp dir mimicking the UXML search root structure
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-test-"));
  const uxmlRoot = path.join(tmpDir, "Assets/UI/UXML");
  const ussRoot = path.join(tmpDir, "Assets/UI/USS");
  fs.mkdirSync(uxmlRoot, { recursive: true });
  fs.mkdirSync(ussRoot, { recursive: true });

  // Copy fixture files
  fs.copyFileSync(
    path.join(FIXTURE_PANEL_DIR, "budget-panel.uxml"),
    path.join(uxmlRoot, "budget-panel.uxml"),
  );
  fs.copyFileSync(
    path.join(FIXTURE_PANEL_DIR, "budget-panel.uss"),
    path.join(ussRoot, "budget-panel.uss"),
  );

  try {
    const backend = new DiskBackend(tmpDir);
    const result = await backend.getPanel("budget-panel");

    assert.equal(result.slug, "budget-panel");
    assert.equal(result.exists, true);
    assert.ok(result.uxml_content !== null, "uxml_content should be non-null");
    assert.ok(result.uxml_tree !== null, "uxml_tree should be non-null");
    assert.ok(Array.isArray(result.uxml_tree), "uxml_tree should be array");
    // UXML tree should have root VisualElement
    assert.ok(result.uxml_tree!.length > 0, "uxml_tree should have nodes");
    const root = result.uxml_tree![0]!;
    assert.equal(root.name, "budget-panel");
    assert.ok(root.classes.includes("panel"), "root should have 'panel' class");
    // USS rules
    assert.ok(result.uss_rules.length > 0, "uss_rules should be non-empty");
    const panelRule = result.uss_rules.find((r) => r.selector === ".panel");
    assert.ok(panelRule, ".panel rule should be present");
    assert.equal(panelRule!.props["background-color"], "#1A2B3C");
  } finally {
    fs.rmSync(tmpDir, { recursive: true });
  }
});

// ---------------------------------------------------------------------------
// T1.0 USS parser tests
// ---------------------------------------------------------------------------

test("T1.0: parseUssFile extracts selectors and props", () => {
  const uss = `
.foo {
  color: red;
  font-size: 14px;
}

.bar { background-color: #123456; }
`;
  const rules = parseUssFile(uss);
  assert.equal(rules.length, 2);
  assert.equal(rules[0]!.selector, ".foo");
  assert.equal(rules[0]!.props["color"], "red");
  assert.equal(rules[0]!.props["font-size"], "14px");
  assert.equal(rules[1]!.selector, ".bar");
  assert.equal(rules[1]!.props["background-color"], "#123456");
});

test("T1.0: parseUssFile preserves hex literal verbatim", () => {
  const uss = `.test { color: #1A2B3C; background-color: #FF0000; }`;
  const rules = parseUssFile(uss);
  assert.equal(rules[0]!.props["color"], "#1A2B3C");
  assert.equal(rules[0]!.props["background-color"], "#FF0000");
});

test("T1.0: parseUssFile handles block comments", () => {
  const uss = `
/* This is a comment */
.foo {
  /* inline comment */
  color: blue;
}
`;
  const rules = parseUssFile(uss);
  assert.equal(rules.length, 1);
  assert.equal(rules[0]!.selector, ".foo");
  assert.equal(rules[0]!.props["color"], "blue");
});

test("T1.0: parseUssFile handles comma-separated selectors", () => {
  const uss = `.foo, .bar { color: green; }`;
  const rules = parseUssFile(uss);
  assert.equal(rules.length, 2);
  assert.equal(rules[0]!.selector, ".foo");
  assert.equal(rules[1]!.selector, ".bar");
});

// ---------------------------------------------------------------------------
// T1.1 — ui_toolkit_panel_get tool registration + shape
// ---------------------------------------------------------------------------

test("T1.1: ui_toolkit_panel_get tool module exports registerUiToolkitPanelGet", async () => {
  const mod = await import("../../src/tools/ui-toolkit-panel-get.js");
  assert.equal(typeof mod.registerUiToolkitPanelGet, "function");
});

test("T1.1: ui_toolkit_panel_get returns shape with required keys for missing panel", async () => {
  // Call directly through backend to verify shape contract
  const backend = new DiskBackend(REPO_ROOT);
  const result = await backend.getPanel("missing-panel-slug-xyz");
  // Shape must include all required keys — no throw on missing
  assert.ok("slug" in result);
  assert.ok("exists" in result);
  assert.ok("uxml_content" in result);
  assert.ok("uxml_tree" in result);
  assert.ok("uss_rules" in result);
  assert.ok("uss_paths" in result);
  assert.ok("scene_uidoc" in result);
  assert.ok("golden_manifest" in result);
  assert.equal(result.exists, false);
});

test("T1.1: ui_toolkit_panel_get shape includes uxml_tree/uss_rules/scene_uidoc/golden_manifest keys", async () => {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-t11-"));
  const uxmlRoot = path.join(tmpDir, "Assets/UI/UXML");
  fs.mkdirSync(uxmlRoot, { recursive: true });
  fs.copyFileSync(
    path.join(FIXTURE_PANEL_DIR, "budget-panel.uxml"),
    path.join(uxmlRoot, "budget-panel.uxml"),
  );
  try {
    const backend = new DiskBackend(tmpDir);
    const result = await backend.getPanel("budget-panel");
    // All required shape keys present
    assert.ok("uxml_tree" in result);
    assert.ok("uss_rules" in result);
    assert.ok("scene_uidoc" in result);
    assert.ok("golden_manifest" in result);
    // uxml_tree is parsed array
    assert.ok(Array.isArray(result.uxml_tree));
    // uss_rules is array (may be empty if no USS in this tmpDir)
    assert.ok(Array.isArray(result.uss_rules));
  } finally {
    fs.rmSync(tmpDir, { recursive: true });
  }
});

// ---------------------------------------------------------------------------
// T1.2 — ui_toolkit_host_inspect + csharp-host-parser
// ---------------------------------------------------------------------------

test("T1.2: ui_toolkit_host_inspect tool module exports registerUiToolkitHostInspect", async () => {
  const mod = await import("../../src/tools/ui-toolkit-host-inspect.js");
  assert.equal(typeof mod.registerUiToolkitHostInspect, "function");
});

test("T1.2: scanHostClass returns empty shape for unknown class", () => {
  const result = scanHostClass("NonExistentHostXyz", REPO_ROOT);
  assert.ok(result !== null);
  assert.equal(result!.host_class, "NonExistentHostXyz");
  assert.equal(result!.file, null);
  assert.equal(result!.declaration_line, null);
  assert.deepEqual(result!.serialized_fields, []);
  assert.deepEqual(result!.q_lookups, {});
  assert.deepEqual(result!.click_bindings, []);
  assert.deepEqual(result!.find_object_of_type_chain, []);
  assert.equal(result!.modal_slug, null);
  assert.deepEqual(result!.blip_bindings, []);
  assert.deepEqual(result!.runtime_ve_constructions, []);
});

test("T1.2: scanHostClass finds BudgetPanelHost fixture and extracts Q-lookups grouped by kind", () => {
  // Copy fixture into a temp dir under Assets/Scripts/ so the scanner finds it
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-host-"));
  const scriptRoot = path.join(tmpDir, "Assets/Scripts");
  fs.mkdirSync(scriptRoot, { recursive: true });
  fs.copyFileSync(
    path.join(FIXTURE_HOST_DIR, "BudgetPanelHost.cs"),
    path.join(scriptRoot, "BudgetPanelHost.cs"),
  );

  try {
    const result = scanHostClass("BudgetPanelHost", tmpDir);
    assert.ok(result !== null);
    assert.equal(result!.host_class, "BudgetPanelHost");
    assert.ok(result!.file !== null);
    assert.ok(result!.declaration_line !== null);

    // Q-lookups grouped by kind
    const ql = result!.q_lookups;
    assert.ok("Label" in ql, "should have Label Q-lookups");
    assert.ok("Button" in ql, "should have Button Q-lookups");
    assert.ok("VisualElement" in ql, "should have VisualElement Q-lookups");

    // balance-label and title-label should be in Label lookups
    const labelLookups = ql["Label"]!;
    const names = labelLookups.map((l) => l.name);
    assert.ok(names.includes("balance-label"), "should find balance-label Q");
    assert.ok(names.includes("title-label"), "should find title-label Q");

    // close-button in Button lookups
    const buttonLookups = ql["Button"]!;
    const btnNames = buttonLookups.map((b) => b.name);
    assert.ok(btnNames.includes("close-button"), "should find close-button Q");

  } finally {
    fs.rmSync(tmpDir, { recursive: true });
  }
});

test("T1.2: scanHostClass extracts serialized fields, click bindings, FindObjectOfType, modal_slug, blip_bindings, runtime constructions", () => {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-host2-"));
  const scriptRoot = path.join(tmpDir, "Assets/Scripts");
  fs.mkdirSync(scriptRoot, { recursive: true });
  fs.copyFileSync(
    path.join(FIXTURE_HOST_DIR, "BudgetPanelHost.cs"),
    path.join(scriptRoot, "BudgetPanelHost.cs"),
  );

  try {
    const result = scanHostClass("BudgetPanelHost", tmpDir);
    assert.ok(result !== null);

    // Serialized fields
    const sfNames = result!.serialized_fields.map((f) => f.name);
    assert.ok(sfNames.includes("uiDocument"), "should find uiDocument serialized field");
    assert.ok(sfNames.includes("budgetData"), "should find budgetData serialized field");

    // Click bindings
    assert.ok(result!.click_bindings.length > 0, "should have click bindings");

    // FindObjectOfType
    const fotTypes = result!.find_object_of_type_chain.map((f) => f.type_name);
    assert.ok(fotTypes.includes("EconomyManager"), "should find EconomyManager");
    assert.ok(fotTypes.includes("CityStatsManager"), "should find CityStatsManager");

    // Modal slug
    assert.equal(result!.modal_slug, "confirm-close");

    // Blip bindings
    const blipNames = result!.blip_bindings.map((b) => b.event_name);
    assert.ok(blipNames.includes("BudgetChangedEvent"), "should find BudgetChangedEvent subscription");
    assert.ok(blipNames.includes("OnTaxRateChanged"), "should find OnTaxRateChanged EventBus subscription");

    // Runtime VE constructions
    const veTypes = result!.runtime_ve_constructions.map((v) => v.type_name);
    assert.ok(veTypes.includes("Label"), "should find Label construction");
    assert.ok(veTypes.includes("VisualElement"), "should find VisualElement construction");

  } finally {
    fs.rmSync(tmpDir, { recursive: true });
  }
});
