// Stage 1.0 — Inspector DI + profiler markers — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: GridInitDependencyBinder.cs::Validate
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   T1.0.1  TECH-33426 — Remove FindObjectOfType from GameBootstrap + GameManager
//   T1.0.2  TECH-33427 — GridInitDependencyBinder — validate all 13 GridManager inspector refs
//   T1.0.3  TECH-33428 — Profiler markers on 6 hot init paths in GeographyInitService

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── TECH-33426: FindObjectOfType removed from GameBootstrap + GameManager ─────

describe("TECH-33426 — Inspector DI: FindObjectOfType removed", () => {
  it("gameBootstrapNoFindObjectOfType [T1.0.1] — GameBootstrap.cs has zero FindObjectOfType literals", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GameBootstrap.cs"
    );
    assert.ok(existsSync(path), `GameBootstrap.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(
      !src.includes("FindObjectOfType"),
      "GameBootstrap.cs must not contain FindObjectOfType"
    );
  });

  it("gameBootstrapHasSerializeFieldGameManager [T1.0.1] — GameBootstrap.cs has [SerializeField] private GameManager gameManager", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GameBootstrap.cs"
    );
    const src = readFileSync(path, "utf8");
    assert.ok(
      src.includes("[SerializeField]") && src.includes("GameManager gameManager"),
      "GameBootstrap.cs must declare [SerializeField] private GameManager gameManager"
    );
  });

  it("gameManagerNoFindObjectOfType [T1.0.1] — GameManager.cs has zero FindObjectOfType literals", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GameManager.cs"
    );
    assert.ok(existsSync(path), `GameManager.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(
      !src.includes("FindObjectOfType"),
      "GameManager.cs must not contain FindObjectOfType"
    );
  });

  it("gameManagerHasSerializeFieldRefs [T1.0.1] — GameManager.cs has [SerializeField] for gridManager + saveManager", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GameManager.cs"
    );
    const src = readFileSync(path, "utf8");
    assert.ok(
      src.includes("GridManager gridManager") && src.includes("GameSaveManager saveManager"),
      "GameManager.cs must declare [SerializeField] for gridManager and saveManager"
    );
  });
});

// ── TECH-33427: GridInitDependencyBinder exists + validates 13 refs ───────────

describe("TECH-33427 — GridInitDependencyBinder: 13-field Validate", () => {
  it("binderFileExists [T1.0.2] — GridInitDependencyBinder.cs exists at Domains/Grid/Services/", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Domains/Grid/Services/GridInitDependencyBinder.cs"
    );
    assert.ok(existsSync(path), `GridInitDependencyBinder.cs must exist: ${path}`);
  });

  it("binderValidateMethod [T1.0.2] — GridInitDependencyBinder.cs has public static Validate method returning ValidationResult", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Domains/Grid/Services/GridInitDependencyBinder.cs"
    );
    const src = readFileSync(path, "utf8");
    assert.ok(
      src.includes("public static ValidationResult Validate("),
      "GridInitDependencyBinder.cs must expose public static ValidationResult Validate(...)"
    );
  });

  it("binderChecks13Fields [T1.0.2] — GridInitDependencyBinder.cs checks all 13 named inspector fields", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Domains/Grid/Services/GridInitDependencyBinder.cs"
    );
    const src = readFileSync(path, "utf8");
    const requiredFields = [
      "zoneManager", "uiManager", "cityStats", "cursorManager", "terrainManager",
      "demandManager", "waterManager", "GameNotificationManager", "forestManager",
      "cameraController", "roadManager", "interstateManager", "buildingSelectorMenuController"
    ];
    for (const field of requiredFields) {
      assert.ok(src.includes(field), `GridInitDependencyBinder must check field: ${field}`);
    }
  });

  it("gridManagerCallsBinder [T1.0.2] — GridManager.Impl.cs calls GridInitDependencyBinder.Validate as first op in InitializeGrid", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GridManager.Impl.cs"
    );
    assert.ok(existsSync(path), `GridManager.Impl.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(
      src.includes("GridInitDependencyBinder.Validate(this)"),
      "GridManager.Impl.cs must call GridInitDependencyBinder.Validate(this) in InitializeGrid"
    );
  });
});

// ── TECH-33428: Profiler markers on 6 hot init paths ─────────────────────────

describe("TECH-33428 — Profiler markers: 6 hot init paths in GeographyInitService", () => {
  it("geographyInitServiceHasProfilerUsing [T1.0.3] — GeographyInitService.cs imports UnityEngine.Profiling", () => {
    const path = join(
      repoRoot,
      "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
    );
    assert.ok(existsSync(path), `GeographyInitService.cs must exist: ${path}`);
    const src = readFileSync(path, "utf8");
    assert.ok(
      src.includes("using UnityEngine.Profiling;"),
      "GeographyInitService.cs must have 'using UnityEngine.Profiling;'"
    );
  });

  const expectedSamples = [
    "GeographyInitService.CreateGrid",
    "GeographyInitService.RestoreGrid",
    "GeographyInitService.RunWaterPipeline",
    "GeographyInitService.RunInterstatePipeline",
    "GeographyInitService.InitializeForestMap",
    "GeographyInitService.ReCalculateSortingOrderBasedOnHeight",
  ];

  for (const sampleName of expectedSamples) {
    it(`profilerSample_${sampleName.split(".")[1]} [T1.0.3] — BeginSample("${sampleName}") present in GeographyInitService.cs`, () => {
      const path = join(
        repoRoot,
        "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
      );
      const src = readFileSync(path, "utf8");
      assert.ok(
        src.includes(`BeginSample("${sampleName}")`),
        `GeographyInitService.cs must contain Profiler.BeginSample("${sampleName}")`
      );
    });
  }
});
