// Stage 1 — Tracer slice: pause-menu pilot + migration infra — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: tracer-verb-test:tests/ui-toolkit-migration/stage1-pause-menu-pilot.test.mjs::pauseMenuRendersViaUiToolkit
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   1.0.1  pre-condition sweep — Unity 6 + visual + perf baseline + atomization dep confirm
//   1.0.2  Add UxmlBakeHandler sidecar (implements IPanelEmitter)
//   1.0.3  Add TssEmitter — generates TSS file from DB token rows
//   1.0.4  Extend ui_def_drift_scan MCP slice to UXML drift
//   1.0.5  Build pixel-diff visual regression harness
//   1.0.6  Emit pause-menu uxml/uss + author VM + Host + scene wire + adapter delete

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-32902: Pre-condition sweep ──────────────────────────────────────────

describe("pre-condition sweep", () => {
  it("visualBaselineManifestExists [task 1.0.1] — tools/visual-baseline/golden/.checksum-manifest.json committed", () => {
    const manifestPath = join(repoRoot, "tools/visual-baseline/golden/.checksum-manifest.json");
    assert.ok(existsSync(manifestPath), `checksum manifest must exist: ${manifestPath}`);
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    assert.ok(manifest.version === 1, "manifest version must be 1");
    assert.ok(typeof manifest.entries === "object", "manifest must have entries object");
  });

  it("perfBaselineExists [task 1.0.1] — tools/perf-baseline/hud-baseline.json committed", () => {
    const baselinePath = join(repoRoot, "tools/perf-baseline/hud-baseline.json");
    assert.ok(existsSync(baselinePath), `perf baseline must exist: ${baselinePath}`);
    const baseline = JSON.parse(readFileSync(baselinePath, "utf8"));
    assert.ok("hud_fps_at_full_city_load" in baseline, "baseline must have hud_fps_at_full_city_load");
    assert.ok("modal_open_ms" in baseline, "baseline must have modal_open_ms");
    assert.ok("modal_close_ms" in baseline, "baseline must have modal_close_ms");
  });

  it("captureBaselineCliExists [task 1.0.1] — tools/visual-baseline/cli/capture-baseline.mjs present", () => {
    const cliPath = join(repoRoot, "tools/visual-baseline/cli/capture-baseline.mjs");
    assert.ok(existsSync(cliPath), `capture-baseline CLI must exist: ${cliPath}`);
    const src = readFileSync(cliPath, "utf8");
    assert.ok(src.includes("checksum-manifest"), "CLI must reference checksum-manifest");
    assert.ok(src.includes("sha256"), "CLI must compute sha256 checksums");
  });
});

// ── TECH-32903: UxmlBakeHandler sidecar ──────────────────────────────────────

describe("UxmlBakeHandler sidecar", () => {
  it("uxmlBakeHandlerExists [task 1.0.2] — Assets/Scripts/Editor/Bridge/UxmlBakeHandler.cs implements IPanelEmitter", () => {
    const csPath = join(repoRoot, "Assets/Scripts/Editor/Bridge/UxmlBakeHandler.cs");
    assert.ok(existsSync(csPath), `UxmlBakeHandler.cs must exist: ${csPath}`);
    const src = readFileSync(csPath, "utf8");
    assert.ok(src.includes("IPanelEmitter"), "UxmlBakeHandler must implement IPanelEmitter");
    assert.ok(src.includes("IUxmlEmissionService"), "UxmlBakeHandler must use IUxmlEmissionService facade");
    assert.ok(src.includes("Emit"), "UxmlBakeHandler must expose Emit method");
  });

  it("uxmlEmissionServiceExists [task 1.0.2] — UxmlEmissionService.cs + IUxmlEmissionService.cs present", () => {
    const svcPath = join(repoRoot, "Assets/Scripts/Editor/Bridge/UxmlEmissionService.cs");
    const ifacePath = join(repoRoot, "Assets/Scripts/Editor/Bridge/IUxmlEmissionService.cs");
    assert.ok(existsSync(svcPath), `UxmlEmissionService.cs must exist: ${svcPath}`);
    assert.ok(existsSync(ifacePath), `IUxmlEmissionService.cs must exist: ${ifacePath}`);
    const ifaceSrc = readFileSync(ifacePath, "utf8");
    assert.ok(ifaceSrc.includes("interface IUxmlEmissionService"), "IUxmlEmissionService must be an interface");
    assert.ok(ifaceSrc.includes("EmitTo"), "IUxmlEmissionService must declare EmitTo");
  });

  it("ipanelEmitterExists [task 1.0.2] — IPanelEmitter interface present in codebase", () => {
    const ifacePath = join(repoRoot, "Assets/Scripts/Editor/Bridge/IPanelEmitter.cs");
    assert.ok(existsSync(ifacePath), `IPanelEmitter.cs must exist: ${ifacePath}`);
    const src = readFileSync(ifacePath, "utf8");
    assert.ok(src.includes("interface IPanelEmitter"), "IPanelEmitter must be an interface");
    assert.ok(src.includes("Emit"), "IPanelEmitter must declare Emit method");
  });
});

// ── TECH-32904: TssEmitter ────────────────────────────────────────────────────

describe("TssEmitter", () => {
  it("tssEmitterExists [task 1.0.3] — Assets/Scripts/Editor/Bridge/TssEmitter.cs present", () => {
    const csPath = join(repoRoot, "Assets/Scripts/Editor/Bridge/TssEmitter.cs");
    assert.ok(existsSync(csPath), `TssEmitter.cs must exist: ${csPath}`);
    const src = readFileSync(csPath, "utf8");
    assert.ok(src.includes("TssEmitter"), "TssEmitter class must be present");
    assert.ok(src.includes("ITssEmissionService"), "TssEmitter must use ITssEmissionService facade");
    assert.ok(src.includes("Emit"), "TssEmitter must expose Emit method");
  });

  it("tssEmissionServiceExists [task 1.0.3] — TssEmissionService.cs + ITssEmissionService.cs present", () => {
    const svcPath = join(repoRoot, "Assets/Scripts/Editor/Bridge/TssEmissionService.cs");
    const ifacePath = join(repoRoot, "Assets/Scripts/Editor/Bridge/ITssEmissionService.cs");
    assert.ok(existsSync(svcPath), `TssEmissionService.cs must exist: ${svcPath}`);
    assert.ok(existsSync(ifacePath), `ITssEmissionService.cs must exist: ${ifacePath}`);
    const ifaceSrc = readFileSync(ifacePath, "utf8");
    assert.ok(ifaceSrc.includes("interface ITssEmissionService"), "ITssEmissionService must be an interface");
    assert.ok(ifaceSrc.includes("EmitTo"), "ITssEmissionService must declare EmitTo");
  });

  it("darkTssExists [task 1.0.3] — Assets/UI/Themes/dark.tss generated artifact present", () => {
    const tssPath = join(repoRoot, "Assets/UI/Themes/dark.tss");
    assert.ok(existsSync(tssPath), `dark.tss must exist: ${tssPath}`);
    const src = readFileSync(tssPath, "utf8");
    assert.ok(src.includes(":root"), "dark.tss must have :root block");
    assert.ok(src.includes("--ds-"), "dark.tss must have --ds-* CSS custom properties");
  });
});

// ── TECH-32905: ui_def_drift_scan UXML extension ──────────────────────────────

describe("ui_def_drift_scan UXML extension", () => {
  it("driftScanHasUxmlSupport [task 1.0.4] — ui-def-drift-scan.ts scans Assets/UI/Generated/*.uxml", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    assert.ok(existsSync(scanPath), `ui-def-drift-scan.ts must exist: ${scanPath}`);
    const src = readFileSync(scanPath, "utf8");
    assert.ok(src.includes("Assets/UI/Generated"), "drift scan must reference Assets/UI/Generated path");
    assert.ok(
      src.includes(".uxml") || src.includes("uxml"),
      "drift scan must handle .uxml files",
    );
    assert.ok(src.includes("uxml"), "drift scan must have uxml branch");
  });

  it("driftScanTestExists [task 1.0.4] — ui-def-drift-scan-uxml.test.mjs present", () => {
    const testPath = join(repoRoot, "tools/mcp-ia-server/test/ui-def-drift-scan-uxml.test.mjs");
    assert.ok(existsSync(testPath), `UXML drift scan test must exist: ${testPath}`);
    const src = readFileSync(testPath, "utf8");
    assert.ok(src.includes("uxml"), "test must cover UXML finding shape");
    assert.ok(src.includes("kind"), "test must assert kind discriminator");
  });
});

// ── TECH-32906: pixel-diff visual regression harness ─────────────────────────

describe("pixel-diff harness", () => {
  it("visualDiffScriptExists [task 1.0.5] — tools/scripts/unity-visual-diff.mjs present", () => {
    const scriptPath = join(repoRoot, "tools/scripts/unity-visual-diff.mjs");
    assert.ok(existsSync(scriptPath), `unity-visual-diff.mjs must exist: ${scriptPath}`);
    const src = readFileSync(scriptPath, "utf8");
    assert.ok(src.includes("tolerance"), "visual-diff must use tolerance threshold");
    assert.ok(src.includes("golden"), "visual-diff must reference golden directory");
    assert.ok(
      src.includes("process.exit(1)") || src.includes("exit(1)"),
      "visual-diff must exit 1 on diff exceeding tolerance",
    );
  });

  it("packageJsonHasVisualDiffScript [task 1.0.5] — package.json has unity:visual-diff script entry", () => {
    const pkgPath = join(repoRoot, "package.json");
    assert.ok(existsSync(pkgPath), "package.json must exist");
    const pkg = JSON.parse(readFileSync(pkgPath, "utf8"));
    assert.ok(
      pkg.scripts && pkg.scripts["unity:visual-diff"],
      "package.json must have unity:visual-diff script",
    );
    assert.ok(
      pkg.scripts["unity:visual-diff"].includes("unity-visual-diff.mjs"),
      "unity:visual-diff script must invoke unity-visual-diff.mjs",
    );
  });
});

// ── TECH-32907: Emit pause-menu + VM + Host + scene wire ─────────────────────

describe("pauseMenuRendersViaUiToolkit", () => {
  it("pauseMenuUxmlExists [task 1.0.6] — Assets/UI/Generated/pause-menu.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/pause-menu.uxml");
    assert.ok(existsSync(uxmlPath), `pause-menu.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("UXML") || src.includes("<ui:") || src.includes("xmlns"), "pause-menu.uxml must be valid UXML");
    assert.ok(src.includes("pause-menu") || src.includes("PauseMenu"), "pause-menu.uxml must reference pause-menu");
  });

  it("pauseMenuUssExists [task 1.0.6] — Assets/UI/Generated/pause-menu.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/pause-menu.uss");
    assert.ok(existsSync(ussPath), `pause-menu.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "pause-menu.uss must reference --ds-* vars or import dark.tss");
  });

  it("pauseMenuVMExists [task 1.0.6] — PauseMenuVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/PauseMenuVM.cs");
    assert.ok(existsSync(vmPath), `PauseMenuVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("PauseMenuVM"), "PauseMenuVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "PauseMenuVM must implement INotifyPropertyChanged",
    );
  });

  it("pauseMenuHostExists [task 1.0.6] — PauseMenuHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/PauseMenuHost.cs");
    assert.ok(existsSync(hostPath), `PauseMenuHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("PauseMenuHost"), "PauseMenuHost class must be present");
    assert.ok(src.includes("UIDocument"), "PauseMenuHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "PauseMenuHost must set rootVisualElement.dataSource");
  });

  it("panelSettingsAssetExists [task 1.0.6] — Assets/Settings/UI/PanelSettings.asset present", () => {
    const assetPath = join(repoRoot, "Assets/Settings/UI/PanelSettings.asset");
    assert.ok(existsSync(assetPath), `PanelSettings.asset must exist: ${assetPath}`);
  });
});
