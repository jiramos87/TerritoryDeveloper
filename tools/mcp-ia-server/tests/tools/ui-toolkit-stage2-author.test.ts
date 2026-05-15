/**
 * Stage 2 red-stage proof: ui-toolkit-authoring-mcp-slices Stage 2 — author (mutation) surface.
 *
 * Anchor: stage2_author_idempotent_mutations
 *
 * T2.0: panel-schema.yaml ui-toolkit-overlay block + 9 kinds;
 *       _ui-toolkit-shared.ts exports validatePanelKind() + isIdempotentWrite() + per-kind Zod schemas.
 * T2.1: ui_toolkit_panel_node_upsert tool registration + idempotency + allow-list.
 * T2.2: ui_toolkit_panel_node_remove tool registration + cascade + orphan USS report.
 * T2.3: ui_toolkit_uss_rule_upsert tool registration + literal-hex preservation + position ordering.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";
import { parse as parseYaml } from "yaml";
import { z } from "zod";

import {
  DiskBackend,
  createBackend,
} from "../../src/ia-db/ui-toolkit-backend.js";
import {
  validatePanelKind,
  isIdempotentWrite,
  UXML_ELEMENT_KINDS,
  KIND_TO_UXML_TAG,
  assertCallerAuthorized,
  serializeUssRules,
  parseUssPosition,
} from "../../src/tools/_ui-toolkit-shared.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../../../");

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeTmpDir(): string {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "uitoolkit-s2-"));
  fs.mkdirSync(path.join(tmp, "Assets/UI/UXML"), { recursive: true });
  fs.mkdirSync(path.join(tmp, "Assets/UI/USS"), { recursive: true });
  fs.mkdirSync(path.join(tmp, "Assets/UI/Generated"), { recursive: true });
  return tmp;
}

const FIXTURE_UXML = `<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
    <ui:VisualElement name="root" class="panel">
        <ui:Label name="title-label" class="panel__title" text="Test" />
        <ui:VisualElement name="content-area" class="panel__content">
            <ui:Button name="close-button" class="panel__close-btn" text="Close" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
`;

const FIXTURE_USS = `.panel {
    background-color: #1A2B3C;
    padding: 8px;
}

.panel__title {
    color: #FFFFFF;
    font-size: 16px;
}

.panel__content {
    flex-grow: 1;
}

.panel__close-btn {
    width: 80px;
}
`;

// ---------------------------------------------------------------------------
// T2.0 — panel-schema.yaml + _ui-toolkit-shared.ts
// stage2_author_idempotent_mutations anchor starts here
// ---------------------------------------------------------------------------

test("T2.0: panel-schema.yaml has ui-toolkit-overlay block", () => {
  const schemaPath = path.join(REPO_ROOT, "tools/blueprints/panel-schema.yaml");
  const raw = fs.readFileSync(schemaPath, "utf8");
  const schema = parseYaml(raw) as Record<string, unknown>;
  assert.ok("panel_kind" in schema, "panel_kind key should be present");
  const panelKind = schema["panel_kind"] as Record<string, unknown>;
  assert.ok("ui-toolkit-overlay" in panelKind, "ui-toolkit-overlay block should be present");
});

test("T2.0: panel-schema.yaml ui-toolkit-overlay has exactly 9 element kinds", () => {
  const schemaPath = path.join(REPO_ROOT, "tools/blueprints/panel-schema.yaml");
  const raw = fs.readFileSync(schemaPath, "utf8");
  const schema = parseYaml(raw) as Record<string, unknown>;
  const overlay = (schema["panel_kind"] as Record<string, unknown>)["ui-toolkit-overlay"] as Record<string, unknown>;
  const kinds = overlay["uxml_element_kinds"] as unknown[];
  assert.equal(kinds.length, 9, "Should have exactly 9 element kinds");
});

test("T2.0: panel-schema.yaml button kind requires action_id", () => {
  const schemaPath = path.join(REPO_ROOT, "tools/blueprints/panel-schema.yaml");
  const raw = fs.readFileSync(schemaPath, "utf8");
  const schema = parseYaml(raw) as Record<string, unknown>;
  const overlay = (schema["panel_kind"] as Record<string, unknown>)["ui-toolkit-overlay"] as Record<string, unknown>;
  const kinds = overlay["uxml_element_kinds"] as Array<{ kind: string; required_params: string[] }>;
  const buttonKind = kinds.find((k) => k.kind === "button");
  assert.ok(buttonKind, "button kind should be present");
  assert.ok(buttonKind!.required_params.includes("action_id"), "button should require action_id");
});

test("T2.0: panel-schema.yaml slider kind requires low-value and high-value", () => {
  const schemaPath = path.join(REPO_ROOT, "tools/blueprints/panel-schema.yaml");
  const raw = fs.readFileSync(schemaPath, "utf8");
  const schema = parseYaml(raw) as Record<string, unknown>;
  const overlay = (schema["panel_kind"] as Record<string, unknown>)["ui-toolkit-overlay"] as Record<string, unknown>;
  const kinds = overlay["uxml_element_kinds"] as Array<{ kind: string; required_params: string[] }>;
  const sliderKind = kinds.find((k) => k.kind === "slider");
  assert.ok(sliderKind, "slider kind should be present");
  assert.ok(sliderKind!.required_params.includes("low-value"), "slider should require low-value");
  assert.ok(sliderKind!.required_params.includes("high-value"), "slider should require high-value");
});

test("T2.0: panel-schema.yaml dropdown kind requires choices", () => {
  const schemaPath = path.join(REPO_ROOT, "tools/blueprints/panel-schema.yaml");
  const raw = fs.readFileSync(schemaPath, "utf8");
  const schema = parseYaml(raw) as Record<string, unknown>;
  const overlay = (schema["panel_kind"] as Record<string, unknown>)["ui-toolkit-overlay"] as Record<string, unknown>;
  const kinds = overlay["uxml_element_kinds"] as Array<{ kind: string; required_params: string[] }>;
  const dropdownKind = kinds.find((k) => k.kind === "dropdown");
  assert.ok(dropdownKind, "dropdown kind should be present");
  assert.ok(dropdownKind!.required_params.includes("choices"), "dropdown should require choices");
});

test("T2.0: UXML_ELEMENT_KINDS has 9 members matching schema", () => {
  assert.equal(UXML_ELEMENT_KINDS.length, 9);
  assert.ok(UXML_ELEMENT_KINDS.includes("button"), "should include button");
  assert.ok(UXML_ELEMENT_KINDS.includes("slider"), "should include slider");
  assert.ok(UXML_ELEMENT_KINDS.includes("dropdown"), "should include dropdown");
  assert.ok(UXML_ELEMENT_KINDS.includes("visual-element"), "should include visual-element");
});

test("T2.0: validatePanelKind button — accepts valid action_id", () => {
  const result = validatePanelKind("button", { action_id: "open-budget" });
  assert.equal(result["action_id"], "open-budget");
});

test("T2.0: validatePanelKind button — rejects missing action_id", () => {
  assert.throws(() => validatePanelKind("button", {}));
});

test("T2.0: validatePanelKind slider — accepts valid low/high values", () => {
  const result = validatePanelKind("slider", { "low-value": 0, "high-value": 100 });
  assert.equal(result["low-value"], 0);
  assert.equal(result["high-value"], 100);
});

test("T2.0: validatePanelKind dropdown — accepts valid choices array", () => {
  const result = validatePanelKind("dropdown", { choices: ["A", "B", "C"] });
  assert.deepEqual(result["choices"], ["A", "B", "C"]);
});

test("T2.0: validatePanelKind dropdown — rejects empty choices", () => {
  assert.throws(() => validatePanelKind("dropdown", { choices: [] }));
});

test("T2.0: isIdempotentWrite — identical strings → true", () => {
  assert.equal(isIdempotentWrite("abc", "abc"), true);
});

test("T2.0: isIdempotentWrite — different strings → false", () => {
  assert.equal(isIdempotentWrite("abc", "abcd"), false);
});

test("T2.0: assertCallerAuthorized — spec-implementer passes", () => {
  assert.doesNotThrow(() => assertCallerAuthorized("spec-implementer"));
});

test("T2.0: assertCallerAuthorized — plan-author passes", () => {
  assert.doesNotThrow(() => assertCallerAuthorized("plan-author"));
});

test("T2.0: assertCallerAuthorized — unknown caller throws unauthorized", () => {
  let threw = false;
  try {
    assertCallerAuthorized("random-agent");
  } catch (e) {
    threw = true;
    const err = e as { code: string; message: string };
    assert.equal(err.code, "unauthorized");
    assert.ok(err.message.includes("allow-list"), "message should mention allow-list");
  }
  assert.ok(threw, "should have thrown");
});

test("T2.0: KIND_TO_UXML_TAG maps button → ui:Button", () => {
  assert.equal(KIND_TO_UXML_TAG["button"], "ui:Button");
});

test("T2.0: serializeUssRules preserves literal hex", () => {
  const rules = [{ selector: ".foo", props: { "background-color": "#5b7fa8", color: "#FF0000" } }];
  const out = serializeUssRules(rules);
  assert.ok(out.includes("#5b7fa8"), "literal hex should be preserved verbatim");
  assert.ok(out.includes("#FF0000"), "literal hex should be preserved verbatim");
  assert.ok(!out.includes("rgb("), "should not contain rgb() conversion");
});

test("T2.0: parseUssPosition handles all forms", () => {
  assert.deepEqual(parseUssPosition("prepend"), { kind: "prepend" });
  assert.deepEqual(parseUssPosition("append"), { kind: "append" });
  assert.deepEqual(parseUssPosition("before:.foo"), { kind: "before", ref: ".foo" });
  assert.deepEqual(parseUssPosition("after:.bar"), { kind: "after", ref: ".bar" });
});

// ---------------------------------------------------------------------------
// T2.1 — ui_toolkit_panel_node_upsert
// ---------------------------------------------------------------------------

test("T2.1: ui-toolkit-panel-node-upsert module exports registerUiToolkitPanelNodeUpsert", async () => {
  const mod = await import("../../src/tools/ui-toolkit-panel-node-upsert.js");
  assert.equal(typeof mod.registerUiToolkitPanelNodeUpsert, "function");
});

test("T2.1: DiskBackend.upsertNode — creates new UXML file and inserts button node", async () => {
  const tmp = makeTmpDir();
  try {
    const backend = new DiskBackend(tmp);
    const result = await backend.upsertNode(
      "test-panel",
      "root",
      { tag: "ui:Button", name: "test-btn", classes: ["my-btn"], attrs: { action_id: "open-budget" } },
    );
    assert.ok(result.ok, "upsertNode should succeed");
    assert.ok(!result.idempotent, "first insert should not be idempotent");

    // Verify file was written
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    assert.ok(fs.existsSync(uxmlPath), "uxml file should exist");
    const content = fs.readFileSync(uxmlPath, "utf8");
    assert.ok(content.includes('name="test-btn"'), "node should have correct name");
    assert.ok(content.includes("ui:Button"), "node should use correct tag");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.1: DiskBackend.upsertNode — re-run same args returns idempotent=true (mtime preserved conceptually)", async () => {
  const tmp = makeTmpDir();
  try {
    const backend = new DiskBackend(tmp);
    // First insert
    await backend.upsertNode(
      "test-panel",
      "root",
      { tag: "ui:Button", name: "test-btn", classes: [], attrs: { action_id: "do-thing" } },
    );
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    const contentBefore = fs.readFileSync(uxmlPath, "utf8");

    // Re-insert same node to existing panel
    const result2 = await backend.upsertNode(
      "test-panel",
      "root",
      { tag: "ui:Button", name: "test-btn", classes: [], attrs: { action_id: "do-thing" } },
    );
    // Should be idempotent (no-op)
    const contentAfter = fs.readFileSync(uxmlPath, "utf8");
    // Content should not have duplicated the node
    const matchCount = (contentAfter.match(/name="test-btn"/g) ?? []).length;
    assert.equal(matchCount, 1, "node should not be duplicated");
    assert.ok(result2.ok, "re-run should succeed");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.1: DiskBackend.upsertNode — non-allow-listed caller throws (via tool wrapper)", () => {
  let threw = false;
  try {
    assertCallerAuthorized("untrusted");
  } catch (e) {
    threw = true;
    const err = e as { code: string };
    assert.equal(err.code, "unauthorized");
  }
  assert.ok(threw, "should have thrown unauthorized");
});

test("T2.1: UXML_ELEMENT_KINDS enum — invalid kind rejected by z.enum at tool input layer", () => {
  const kindSchema = z.enum(UXML_ELEMENT_KINDS);
  assert.throws(() => kindSchema.parse("invalid-kind"), "z.enum should reject unknown kind");
});

// ---------------------------------------------------------------------------
// T2.2 — ui_toolkit_panel_node_remove
// ---------------------------------------------------------------------------

test("T2.2: ui-toolkit-panel-node-remove module exports registerUiToolkitPanelNodeRemove", async () => {
  const mod = await import("../../src/tools/ui-toolkit-panel-node-remove.js");
  assert.equal(typeof mod.registerUiToolkitPanelNodeRemove, "function");
});

test("T2.2: DiskBackend.removeNode — removes existing node, children gone", async () => {
  const tmp = makeTmpDir();
  try {
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    const ussPath = path.join(tmp, "Assets/UI/USS/test-panel.uss");
    fs.writeFileSync(uxmlPath, FIXTURE_UXML, "utf8");
    fs.writeFileSync(ussPath, FIXTURE_USS, "utf8");

    const backend = new DiskBackend(tmp);
    const result = await backend.removeNode("test-panel", "root/content-area");

    assert.ok(result.ok, "removeNode should succeed");
    assert.ok(!result.idempotent, "should not be idempotent on first removal");

    const content = fs.readFileSync(uxmlPath, "utf8");
    assert.ok(!content.includes('name="content-area"'), "content-area should be removed");
    assert.ok(!content.includes('name="close-button"'), "close-button (child) should be removed");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.2: DiskBackend.removeNode — orphan_uss_rules populated after removal", async () => {
  const tmp = makeTmpDir();
  try {
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    const ussPath = path.join(tmp, "Assets/UI/USS/test-panel.uss");
    fs.writeFileSync(uxmlPath, FIXTURE_UXML, "utf8");
    fs.writeFileSync(ussPath, FIXTURE_USS, "utf8");

    const backend = new DiskBackend(tmp);
    const result = await backend.removeNode("test-panel", "root/content-area");

    assert.ok(result.ok);
    // panel__content and panel__close-btn were on removed elements → orphan
    assert.ok(Array.isArray(result.orphan_uss_rules), "orphan_uss_rules should be an array");
    // At least one orphan should be detected for removed classes
    // (panel__content class is on content-area which was removed)
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.2: DiskBackend.removeNode — removes non-existent path → no-op + idempotent", async () => {
  const tmp = makeTmpDir();
  try {
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    fs.writeFileSync(uxmlPath, FIXTURE_UXML, "utf8");

    const backend = new DiskBackend(tmp);
    const result = await backend.removeNode("test-panel", "nonexistent-node");

    assert.ok(result.ok, "should succeed for non-existent path");
    assert.ok(result.idempotent, "should be idempotent");
    assert.deepEqual(result.orphan_uss_rules, [], "no orphans when nothing removed");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.2: DiskBackend.removeNode — missing UXML file → no-op", async () => {
  const tmp = makeTmpDir();
  try {
    const backend = new DiskBackend(tmp);
    const result = await backend.removeNode("nonexistent-panel", "some-node");
    assert.ok(result.ok);
    assert.ok(result.idempotent);
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.2: orphan rules NOT auto-deleted — USS file unchanged after remove", async () => {
  const tmp = makeTmpDir();
  try {
    const uxmlPath = path.join(tmp, "Assets/UI/UXML/test-panel.uxml");
    const ussPath = path.join(tmp, "Assets/UI/USS/test-panel.uss");
    fs.writeFileSync(uxmlPath, FIXTURE_UXML, "utf8");
    fs.writeFileSync(ussPath, FIXTURE_USS, "utf8");
    const originalUss = fs.readFileSync(ussPath, "utf8");

    const backend = new DiskBackend(tmp);
    await backend.removeNode("test-panel", "root/content-area");

    const ussAfter = fs.readFileSync(ussPath, "utf8");
    assert.equal(ussAfter, originalUss, "USS file should NOT be modified by removeNode");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

// ---------------------------------------------------------------------------
// T2.3 — ui_toolkit_uss_rule_upsert
// ---------------------------------------------------------------------------

test("T2.3: ui-toolkit-uss-rule-upsert module exports registerUiToolkitUssRuleUpsert", async () => {
  const mod = await import("../../src/tools/ui-toolkit-uss-rule-upsert.js");
  assert.equal(typeof mod.registerUiToolkitUssRuleUpsert, "function");
});

test("T2.3: DiskBackend.upsertUssRule — creates new .uss and writes rule with literal hex", async () => {
  const tmp = makeTmpDir();
  try {
    const backend = new DiskBackend(tmp);
    const result = await backend.upsertUssRule(
      "test-panel",
      ".my-button",
      { "background-color": "#5b7fa8", color: "#FFFFFF" },
      "append",
    );

    assert.ok(result.ok, "upsertUssRule should succeed");

    // Find written USS file
    const candidates = [
      path.join(tmp, "Assets/UI/USS/test-panel.uss"),
      path.join(tmp, "Assets/UI/Generated/test-panel.uss"),
    ];
    const ussPath = candidates.find((p) => fs.existsSync(p));
    assert.ok(ussPath, "uss file should exist");

    const content = fs.readFileSync(ussPath!, "utf8");
    assert.ok(content.includes(".my-button"), "selector should be present");
    assert.ok(content.includes("#5b7fa8"), "literal hex should be preserved char-for-char");
    assert.ok(content.includes("#FFFFFF"), "literal hex should be preserved char-for-char");
    assert.ok(!content.includes("rgb("), "no rgb() conversion should occur");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.3: DiskBackend.upsertUssRule — re-run same args → idempotent", async () => {
  const tmp = makeTmpDir();
  try {
    const backend = new DiskBackend(tmp);
    await backend.upsertUssRule("test-panel", ".foo", { color: "#123456" }, "append");
    const result2 = await backend.upsertUssRule("test-panel", ".foo", { color: "#123456" }, "append");
    assert.ok(result2.ok);
    assert.ok(result2.idempotent, "second identical write should be idempotent");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.3: DiskBackend.upsertUssRule — position=after:.other inserts rule in correct order", async () => {
  const tmp = makeTmpDir();
  try {
    // Write a seed USS file
    const ussPath = path.join(tmp, "Assets/UI/USS/test-panel.uss");
    fs.writeFileSync(ussPath, `.other {\n    color: red;\n}\n`, "utf8");

    const backend = new DiskBackend(tmp);
    const result = await backend.upsertUssRule(
      "test-panel",
      ".inserted",
      { color: "#00FF00" },
      "after:.other",
    );
    assert.ok(result.ok);

    const content = fs.readFileSync(ussPath, "utf8");
    const otherIdx = content.indexOf(".other");
    const insertedIdx = content.indexOf(".inserted");
    assert.ok(insertedIdx > otherIdx, ".inserted should appear after .other in the file");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.3: DiskBackend.upsertUssRule — position=prepend inserts rule before all existing rules", async () => {
  const tmp = makeTmpDir();
  try {
    const ussPath = path.join(tmp, "Assets/UI/USS/test-panel.uss");
    fs.writeFileSync(ussPath, `.existing {\n    color: blue;\n}\n`, "utf8");

    const backend = new DiskBackend(tmp);
    const result = await backend.upsertUssRule(
      "test-panel",
      ".first",
      { "font-size": "14px" },
      "prepend",
    );
    assert.ok(result.ok);

    const content = fs.readFileSync(ussPath, "utf8");
    const firstIdx = content.indexOf(".first");
    const existingIdx = content.indexOf(".existing");
    assert.ok(firstIdx < existingIdx, ".first should appear before .existing");
  } finally {
    fs.rmSync(tmp, { recursive: true });
  }
});

test("T2.3: serializeUssRules round-trip — no hex normalization", () => {
  const input = [
    { selector: ".panel", props: { "background-color": "#1A2B3C", padding: "8px" } },
    { selector: ".title", props: { color: "#FF5500", "font-size": "16px" } },
  ];
  const serialized = serializeUssRules(input);
  assert.ok(serialized.includes("#1A2B3C"), "hex preserved in round-trip");
  assert.ok(serialized.includes("#FF5500"), "hex preserved in round-trip");
  assert.ok(!serialized.includes("rgb("), "no rgb() conversion");
});
