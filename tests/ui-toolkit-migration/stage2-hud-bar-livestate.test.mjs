// Stage 2 — hud-bar live-state binding (real-world stress) — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: visibility-delta-test:tests/ui-toolkit-migration/stage2-hud-bar-livestate.test.mjs::hudBarLiveStateUpdates
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   2.0.1  Bake hud-bar.uxml + hud-bar.uss + author HudBarVM
//   2.0.2  Author HudBarHost + CityScene wire + delete HudBarDataAdapter + refactor EconomyHudBindPublisher
//   2.0.3  Performance check + automated visual-diff acceptance

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-32908: hud-bar UXML + USS + HudBarVM ────────────────────────────────

describe("hudBarUxmlBake", () => {
  it("hudBarUxmlExists [task 2.0.1] — Assets/UI/Generated/hud-bar.uxml emitted", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/hud-bar.uxml");
    assert.ok(existsSync(uxmlPath), `hud-bar.uxml must exist: ${uxmlPath}`);
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes("hud-bar"), "uxml must reference hud-bar");
    assert.ok(src.includes("xmlns") || src.includes("<ui:"), "must be valid UXML");
  });

  it("hudBarUxmlHasBindingPaths [task 2.0.1] — UXML has binding-path for Money, Date, Weather", () => {
    const uxmlPath = join(repoRoot, "Assets/UI/Generated/hud-bar.uxml");
    assert.ok(existsSync(uxmlPath), "hud-bar.uxml must exist");
    const src = readFileSync(uxmlPath, "utf8");
    assert.ok(src.includes('binding-path="Money"'), "must have Money binding-path");
    assert.ok(src.includes('binding-path="Date"'), "must have Date binding-path");
    assert.ok(src.includes('binding-path="Weather"'), "must have Weather binding-path");
  });

  it("hudBarUssExists [task 2.0.1] — Assets/UI/Generated/hud-bar.uss emitted", () => {
    const ussPath = join(repoRoot, "Assets/UI/Generated/hud-bar.uss");
    assert.ok(existsSync(ussPath), `hud-bar.uss must exist: ${ussPath}`);
    const src = readFileSync(ussPath, "utf8");
    assert.ok(src.includes("--ds-") || src.includes("@import"), "must reference --ds-* tokens or import dark.tss");
  });

  it("hudBarVMExists [task 2.0.1] — HudBarVM.cs POCO ViewModel present", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/HudBarVM.cs");
    assert.ok(existsSync(vmPath), `HudBarVM.cs must exist: ${vmPath}`);
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("HudBarVM"), "HudBarVM class must be present");
    assert.ok(
      src.includes("INotifyPropertyChanged") || src.includes("PropertyChanged"),
      "HudBarVM must implement INotifyPropertyChanged",
    );
  });

  it("hudBarVMHasMoneyDateWeather [task 2.0.1] — HudBarVM exposes Money, Date, Weather properties", () => {
    const vmPath = join(repoRoot, "Assets/Scripts/UI/ViewModels/HudBarVM.cs");
    assert.ok(existsSync(vmPath), "HudBarVM.cs must exist");
    const src = readFileSync(vmPath, "utf8");
    assert.ok(src.includes("Money"), "VM must have Money property");
    assert.ok(src.includes("Date"), "VM must have Date property");
    assert.ok(src.includes("Weather"), "VM must have Weather property");
  });
});

// ── TECH-32909: HudBarHost + HudBarDataAdapter delete + EconomyHudBindPublisher refactor ─────────

describe("hudBarLiveStateUpdates", () => {
  it("hudBarHostExists [task 2.0.2] — HudBarHost.cs MonoBehaviour Host present", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/HudBarHost.cs");
    assert.ok(existsSync(hostPath), `HudBarHost.cs must exist: ${hostPath}`);
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("HudBarHost"), "HudBarHost class must be present");
    assert.ok(src.includes("UIDocument"), "HudBarHost must reference UIDocument");
    assert.ok(src.includes("dataSource"), "HudBarHost must set rootVisualElement.dataSource");
  });

  it("hudBarHostWiresVM [task 2.0.2] — HudBarHost wires HudBarVM as dataSource", () => {
    const hostPath = join(repoRoot, "Assets/Scripts/UI/Hosts/HudBarHost.cs");
    assert.ok(existsSync(hostPath), "HudBarHost.cs must exist");
    const src = readFileSync(hostPath, "utf8");
    assert.ok(src.includes("HudBarVM"), "HudBarHost must instantiate HudBarVM");
    assert.ok(src.includes("dataSource"), "HudBarHost must assign dataSource");
  });

  it("hudBarDataAdapterArchived [task 2.0.2] — HudBarDataAdapter.cs removed from active source (moved to .archive)", () => {
    const activePath = join(repoRoot, "Assets/Scripts/UI/HUD/HudBarDataAdapter.cs");
    assert.ok(
      !existsSync(activePath),
      `HudBarDataAdapter.cs must be archived (not in active HUD dir): ${activePath}`,
    );
  });

  it("economyHudBindPublisherRefactored [task 2.0.2] — EconomyHudBindPublisher no longer uses UiBindRegistry", () => {
    const publisherPath = join(repoRoot, "Assets/Scripts/UI/HUD/EconomyHudBindPublisher.cs");
    assert.ok(existsSync(publisherPath), "EconomyHudBindPublisher.cs must exist");
    const src = readFileSync(publisherPath, "utf8");
    // Must NOT call UiBindRegistry.Set (legacy path removed)
    assert.ok(
      !src.includes("_bindRegistry.Set") && !src.includes("bindRegistry.Set"),
      "EconomyHudBindPublisher must not call UiBindRegistry.Set (legacy path removed)",
    );
    // Must reference HudBarHost (new direct path)
    assert.ok(src.includes("HudBarHost"), "EconomyHudBindPublisher must reference HudBarHost");
  });
});

// ── TECH-32910: Performance check + visual-diff acceptance ───────────────────

describe("hudBarPerfAndVisualDiff", () => {
  it("perfStage2SnapshotExists [task 2.0.3] — tools/perf-baseline/hud-stage2.json present", () => {
    const perfPath = join(repoRoot, "tools/perf-baseline/hud-stage2.json");
    assert.ok(existsSync(perfPath), `hud-stage2.json must exist: ${perfPath}`);
    const snap = JSON.parse(readFileSync(perfPath, "utf8"));
    assert.ok("stage" in snap, "snapshot must have stage field");
    assert.ok("hud_fps_at_full_city_load" in snap, "snapshot must have hud_fps_at_full_city_load");
    assert.ok(snap.stage === "stage-2-0", "snapshot stage must be stage-2-0");
  });

  it("checksumManifestHasHudBarEntry [task 2.0.3] — .checksum-manifest.json has hud-bar.png entry", () => {
    const manifestPath = join(repoRoot, "tools/visual-baseline/golden/.checksum-manifest.json");
    assert.ok(existsSync(manifestPath), "checksum manifest must exist");
    const manifest = JSON.parse(readFileSync(manifestPath, "utf8"));
    assert.ok(manifest.version === 1, "manifest version must be 1");
    assert.ok(
      "hud-bar.png" in manifest.entries,
      "manifest must have hud-bar.png entry",
    );
  });

  it("visualDiffScriptExists [task 2.0.3] — tools/scripts/unity-visual-diff.mjs present (harness wired)", () => {
    const scriptPath = join(repoRoot, "tools/scripts/unity-visual-diff.mjs");
    assert.ok(existsSync(scriptPath), `unity-visual-diff.mjs must exist: ${scriptPath}`);
    const src = readFileSync(scriptPath, "utf8");
    assert.ok(src.includes("tolerance"), "visual-diff must use tolerance threshold");
    assert.ok(src.includes("golden"), "visual-diff must reference golden directory");
  });
});
