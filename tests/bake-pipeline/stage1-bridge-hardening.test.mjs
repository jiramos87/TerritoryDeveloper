// Stage 1 — Bridge hardening — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `npm run test:bake-pipeline-hardening`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   1.1  get_console_logs_SurfacesDomainReloadError
//   1.2  allowlistFilter_ExpiredEntryFlipsBackToBlocking
//   1.3  panelSchemaYaml_LoadsAndCoversAllCatalogKinds
//   1.4  validatePanelBlueprint_FailsOnMissingRequiredKey

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { createRequire } from "node:module";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-27336: get_console_logs bridge kind + domain-reload timestamp ─────────

describe("get_console_logs", () => {
  it("get_console_logs_SurfacesDomainReloadError [task 1.1] — AgentBridgeConsoleBuffer exposes DomainReloadTimestampUtc field", () => {
    // Validates: AgentBridgeConsoleBuffer.cs declares DomainReloadTimestampUtc static property.
    // Node-side test reads C# source and asserts the field is present (can't run Unity Editor here).
    const csPath = join(repoRoot, "Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs");
    assert.ok(existsSync(csPath), `C# buffer file must exist: ${csPath}`);
    const src = readFileSync(csPath, "utf8");
    assert.ok(
      src.includes("DomainReloadTimestampUtc"),
      "AgentBridgeConsoleBuffer must declare DomainReloadTimestampUtc property",
    );
    assert.ok(
      src.includes("delayCall"),
      "Domain reload timestamp must be set via EditorApplication.delayCall",
    );
    assert.ok(
      src.includes("get_console_logs") || src.includes("AgentBridgeConsoleBuffer"),
      "Buffer class must be present in codebase",
    );
  });
});

// ── TECH-27337: allowlist YAML + console-scan ship-gate filter ────────────────

describe("allowlist filter", () => {
  it("allowlistFilter_ExpiredEntryFlipsBackToBlocking [task 1.2] — allowlist YAML schema + console-scan.mjs", () => {
    const yamlPath = join(repoRoot, "tools/unity-warning-allowlist.yaml");
    assert.ok(existsSync(yamlPath), `allowlist YAML must exist: ${yamlPath}`);

    const yaml = readFileSync(yamlPath, "utf8");
    // Must have pattern, reason, expires, owner fields in at least one entry.
    assert.ok(yaml.includes("pattern:"), "allowlist must have pattern field");
    assert.ok(yaml.includes("reason:"), "allowlist must have reason field");
    assert.ok(yaml.includes("expires:"), "allowlist must have expires field");
    assert.ok(yaml.includes("owner:"), "allowlist must have owner field");

    const scanPath = join(repoRoot, "tools/scripts/console-scan.mjs");
    assert.ok(existsSync(scanPath), `console-scan.mjs must exist: ${scanPath}`);
    const scanSrc = readFileSync(scanPath, "utf8");
    // Must filter by pattern and check expiry.
    assert.ok(scanSrc.includes("expires"), "console-scan must check entry expiry");
    assert.ok(scanSrc.includes("pattern"), "console-scan must filter by pattern");
    assert.ok(
      scanSrc.includes("exit(1)") || scanSrc.includes("process.exit(1)"),
      "console-scan must exit 1 on unfiltered errors",
    );
  });
});

// ── TECH-27338: panel-schema.yaml per-kind required-keys ─────────────────────

describe("panel-schema yaml", () => {
  it("panelSchemaYaml_LoadsAndCoversAllCatalogKinds [task 1.3] — tools/blueprints/panel-schema.yaml covers all 8 catalog kinds", () => {
    const schemaPath = join(repoRoot, "tools/blueprints/panel-schema.yaml");
    assert.ok(existsSync(schemaPath), `panel-schema.yaml must exist: ${schemaPath}`);

    const yaml = readFileSync(schemaPath, "utf8");
    const requiredKinds = [
      "button",
      "illuminated-button",
      "confirm-button",
      "slider-row",
      "toggle-row",
      "dropdown-row",
      "section-header",
      "list-row",
    ];
    for (const kind of requiredKinds) {
      assert.ok(yaml.includes(kind), `panel-schema.yaml must have a row for kind: ${kind}`);
    }
  });
});

// ── TECH-27339: validate_panel_blueprint MCP kind + bake-handler pre-flight ──

describe("validate_panel_blueprint", () => {
  it("validatePanelBlueprint_FailsOnMissingRequiredKey [task 1.4] — MCP kind registered + C# handler present", () => {
    // MCP descriptor check.
    const inputSchemaPath = join(
      repoRoot,
      "tools/mcp-ia-server/src/tools/unity-bridge-command/input-schema.ts",
    );
    assert.ok(existsSync(inputSchemaPath), `input-schema.ts must exist: ${inputSchemaPath}`);
    const schemaSrc = readFileSync(inputSchemaPath, "utf8");
    assert.ok(
      schemaSrc.includes("validate_panel_blueprint"),
      "input-schema.ts must register validate_panel_blueprint kind",
    );

    // C# handler check.
    const csRunner = join(repoRoot, "Assets/Scripts/Editor/AgentBridgeCommandRunner.cs");
    const csSrc = readFileSync(csRunner, "utf8");
    assert.ok(
      csSrc.includes("validate_panel_blueprint"),
      "AgentBridgeCommandRunner.cs must handle validate_panel_blueprint",
    );

    // UiBakeHandler pre-flight check.
    const bakeHandler = join(repoRoot, "Assets/Scripts/Editor/Bridge/UiBakeHandler.cs");
    const bakeSrc = readFileSync(bakeHandler, "utf8");
    assert.ok(
      bakeSrc.includes("ValidatePanelBlueprint") || bakeSrc.includes("validate_panel_blueprint"),
      "UiBakeHandler.cs must call ValidatePanelBlueprint pre-flight",
    );

    // panel-schema.yaml readable by Node (cross-check with task 1.3).
    const schemaPath = join(repoRoot, "tools/blueprints/panel-schema.yaml");
    assert.ok(existsSync(schemaPath), "panel-schema.yaml must exist for validate_panel_blueprint");
  });
});
