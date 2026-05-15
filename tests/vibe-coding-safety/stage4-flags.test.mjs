// Stage 5.0 — Wave D (feature flag DB table + Unity runtime + interchange JSON + bridge) — TDD red→green.
//
// Stage anchor: visibility-delta-test:tests/vibe-coding-safety/stage4-flags.test.mjs::BootHydrationFromInterchangeJson
//
// Scene hint: Assets/Scenes/MainScene.unity (bridge play-mode load target).
//
// Tasks:
//   5.0.1  Migration — ia_feature_flags table
//   5.0.2  Migration — ia_stages.flag_slug column
//   5.0.3  Author FeatureFlags.cs static class
//   5.0.4  Boot hook — bootstrap MonoBehaviour Awake invokes hydration
//   5.0.5  Register interchange JSON artifact schema
//   5.0.6  MCP tool / web export — ia_feature_flags → snapshot artifact
//   5.0.7  Bridge command kind flag_flip
//   5.0.8  Web dashboard read-only flag panel
//   5.0.9  Stage test — flag table + boot hydration + bridge flip (this file)
//   5.0.10 Glossary row — Feature flag (Stage-scoped)

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");

const SCENE_PATH = "Assets/Scenes/MainScene.unity";

describe("Stage 5.0 — feature flag DB → snapshot → boot hydration → bridge flip", () => {
  // ── 5.0.5 / 5.0.6 — interchange JSON artifact shape ────────────────────
  it("featureFlagsSnapshotArtifactShape [task 5.0.9]", () => {
    const snapshotPath = path.join(REPO_ROOT, "tools", "interchange", "feature-flags-snapshot.json");
    assert.ok(fs.existsSync(snapshotPath), `Snapshot not found at ${snapshotPath}`);

    const raw = fs.readFileSync(snapshotPath, "utf-8");
    const obj = JSON.parse(raw);

    assert.strictEqual(obj.artifact, "feature-flags-snapshot", "artifact field must be 'feature-flags-snapshot'");
    assert.strictEqual(obj.schema_version, 1, "schema_version must be 1");
    assert.ok(Array.isArray(obj.flags), "flags must be an array");

    // Each entry (if present) must have required fields.
    for (const flag of obj.flags) {
      assert.ok(typeof flag.slug === "string" && flag.slug.length > 0, "each flag must have a non-empty slug");
      assert.ok(typeof flag.enabled === "boolean", `flag '${flag.slug}' must have boolean enabled`);
      assert.ok(typeof flag.default_value === "boolean", `flag '${flag.slug}' must have boolean default_value`);
    }
  });

  // ── 5.0.3 / 5.0.4 — FeatureFlags.cs + bootstrap Awake hook ────────────
  it(`bootHydrationPopulatesCacheFromSnapshot [scene=${SCENE_PATH}] [task 5.0.9]`, () => {
    // Structural assertion: FeatureFlags.cs must exist at the declared path.
    const featureFlagsCs = path.join(REPO_ROOT, "Assets", "Scripts", "Core", "Core", "FeatureFlags.cs");
    assert.ok(fs.existsSync(featureFlagsCs), `FeatureFlags.cs not found at ${featureFlagsCs}`);

    const src = fs.readFileSync(featureFlagsCs, "utf-8");
    assert.ok(src.includes("public static bool IsEnabled"), "FeatureFlags must expose IsEnabled(string slug)");
    assert.ok(src.includes("public static void HydrateFromJson"), "FeatureFlags must expose HydrateFromJson(string path)");
    assert.ok(src.includes("public static void InvalidateCache"), "FeatureFlags must expose InvalidateCache()");

    // Bootstrap MonoBehaviour must exist.
    const bootstrapCs = path.join(REPO_ROOT, "Assets", "Scripts", "Managers", "GameManagers", "FeatureFlagsBootstrap.cs");
    assert.ok(fs.existsSync(bootstrapCs), `FeatureFlagsBootstrap.cs not found at ${bootstrapCs}`);

    const bsrc = fs.readFileSync(bootstrapCs, "utf-8");
    assert.ok(bsrc.includes("HydrateFromJson"), "FeatureFlagsBootstrap must invoke HydrateFromJson in Awake");
    assert.ok(bsrc.includes("void Awake"), "FeatureFlagsBootstrap must have Awake()");
  });

  // ── 5.0.7 — bridge flag_flip kind ───────────────────────────────────────
  it(`bridgeFlagFlipInvalidatesCacheAndReHydrates [scene=${SCENE_PATH}] [task 5.0.9]`, () => {
    // Structural: FlagFlip partial class must exist and register the kind.
    const flagFlipCs = path.join(REPO_ROOT, "Assets", "Scripts", "Editor", "AgentBridgeCommandRunner.FlagFlip.cs");
    assert.ok(fs.existsSync(flagFlipCs), `AgentBridgeCommandRunner.FlagFlip.cs not found at ${flagFlipCs}`);

    const src = fs.readFileSync(flagFlipCs, "utf-8");
    assert.ok(src.includes("RunFlagFlip"), "FlagFlip partial must contain RunFlagFlip method");
    assert.ok(src.includes("InvalidateCache"), "FlagFlip handler must call FeatureFlags.InvalidateCache()");
    assert.ok(src.includes("HydrateFromJson"), "FlagFlip handler must call FeatureFlags.HydrateFromJson(...)");

    // Main switch must include the case.
    const runnerCs = path.join(REPO_ROOT, "Assets", "Scripts", "Editor", "AgentBridgeCommandRunner.cs");
    const rsrc = fs.readFileSync(runnerCs, "utf-8");
    assert.ok(rsrc.includes('"flag_flip"'), 'AgentBridgeCommandRunner.cs must register case "flag_flip"');
  });

  // ── 5.0.1 — migration file for ia_feature_flags ─────────────────────────
  it("iaFeatureFlagsTableExistsWithExpectedColumns [task 5.0.9]", () => {
    // Structural: migration SQL file must exist and contain expected DDL.
    const migrationsDir = path.join(REPO_ROOT, "db", "migrations");
    const files = fs.readdirSync(migrationsDir).sort();
    const migFile = files.find(f => f.includes("ia_feature_flags"));
    assert.ok(migFile, "A migration file containing 'ia_feature_flags' must exist under db/migrations/");

    const sql = fs.readFileSync(path.join(migrationsDir, migFile), "utf-8");
    for (const col of ["slug", "enabled", "default_value", "owner", "created_at"]) {
      assert.ok(sql.includes(col), `Migration must define column '${col}'`);
    }
  });
});
