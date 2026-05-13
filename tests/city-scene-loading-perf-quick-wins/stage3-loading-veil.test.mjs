/**
 * Stage 3 — Loading veil + lazy forest
 * Tests: LoadingVeilController canvas state + DeferredForestInit wiring
 * Anchor: Assets/Scripts/Domains/Geography/Services/LoadingVeilController.cs::OnGeographyInitialized
 */

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import path from "node:path";

const REPO = new URL("../../", import.meta.url).pathname;

// ---- File existence checks ----

describe("Stage 3 — LoadingVeilController file exists", () => {
  it("LoadingVeilController.cs present", () => {
    const p = path.join(
      REPO,
      "Assets/Scripts/Managers/GameManagers/LoadingVeilController.cs"
    );
    assert.ok(existsSync(p), `Expected file at ${p}`);
  });

  it("LoadingVeil prefab present", () => {
    const p = path.join(REPO, "Assets/UI/Prefabs/LoadingVeil.prefab");
    assert.ok(existsSync(p), `Expected prefab at ${p}`);
  });
});

// ---- Structural checks on LoadingVeilController.cs ----

describe("Stage 3 — LoadingVeilController structure", () => {
  const src = readFileSync(
    path.join(
      REPO,
      "Assets/Scripts/Managers/GameManagers/LoadingVeilController.cs"
    ),
    "utf8"
  );

  it("declares MonoBehaviour subclass", () => {
    assert.match(src, /class LoadingVeilController\s*:\s*MonoBehaviour/);
  });

  it("Awake activates canvas", () => {
    assert.match(src, /\.SetActive\(true\)/);
  });

  it("OnGeographyInitialized deactivates canvas", () => {
    assert.match(src, /OnGeographyInitialized/);
    assert.match(src, /\.SetActive\(false\)/);
  });

  it("subscribes in OnEnable, unsubscribes in OnDisable", () => {
    assert.match(src, /OnEnable/);
    assert.match(src, /OnDisable/);
    assert.match(src, /\+= OnGeographyInitialized/);
    assert.match(src, /-= OnGeographyInitialized/);
  });

  it("exposes Progress property", () => {
    assert.match(src, /public float Progress/);
  });
});

// ---- Structural checks on GeographyManager.cs ----

describe("Stage 3 — GeographyManager event", () => {
  const src = readFileSync(
    path.join(
      REPO,
      "Assets/Scripts/Managers/GameManagers/GeographyManager.cs"
    ),
    "utf8"
  );

  it("declares OnGeographyInitialized event", () => {
    assert.match(src, /public event System\.Action OnGeographyInitialized/);
  });

  it("IsInitialized setter fires event on false→true", () => {
    assert.match(src, /OnGeographyInitialized\?\.Invoke\(\)/);
  });
});

// ---- Structural checks on GeographyInitService.cs ----

describe("Stage 3 — GeographyInitService deferred forest", () => {
  const src = readFileSync(
    path.join(
      REPO,
      "Assets/Scripts/Managers/GameManagers/GeographyInitService.cs"
    ),
    "utf8"
  );

  it("DeferredForestInit coroutine declared", () => {
    assert.match(src, /private IEnumerator DeferredForestInit/);
  });

  it("coroutine yields twice before InitializeForestMap", () => {
    assert.match(src, /yield return null;\s*yield return null;\s*_hub\.forestManager\.InitializeForestMap/s);
  });

  it("StartCoroutine called when initializeForestsOnStart is false", () => {
    assert.match(src, /!_hub\.initializeForestsOnStart/);
    assert.match(src, /StartCoroutine\(DeferredForestInit\(\)\)/);
  });
});
