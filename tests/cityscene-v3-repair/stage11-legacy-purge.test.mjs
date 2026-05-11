/**
 * Stage 11.0 — Legacy-GO purge test suite (TECH-29755 → TECH-29758).
 * Red state at T11.0.1 creation; green on stage close.
 * Node --test runner.
 *
 * Red-Stage Proof anchor: LegacyGO_AbsentFromScene
 * Asserts SubtypePickerRoot, GrowthBudgetPanelRoot, city-stats-handoff
 * are absent from CityScene.unity YAML. B.7e sweep = zero live legacy GOs.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const SCENE_PATH = resolve(
  new URL('.', import.meta.url).pathname,
  '../../Assets/Scenes/CityScene.unity'
);

function loadScene() {
  return readFileSync(SCENE_PATH, 'utf-8');
}

// ── T11.0.1 / T11.0.2 / T11.0.3 — GOs absent from scene ────────────────────

test('LegacyGO_AbsentFromScene — SubtypePickerRoot not present in CityScene.unity', () => {
  const scene = loadScene();
  assert.ok(
    !scene.includes('SubtypePickerRoot'),
    'SubtypePickerRoot found in CityScene.unity — legacy GO not purged'
  );
});

test('LegacyGO_AbsentFromScene — GrowthBudgetPanelRoot not present in CityScene.unity', () => {
  const scene = loadScene();
  assert.ok(
    !scene.includes('GrowthBudgetPanelRoot'),
    'GrowthBudgetPanelRoot found in CityScene.unity — legacy GO not purged'
  );
});

test('LegacyGO_AbsentFromScene — city-stats-handoff not present in CityScene.unity', () => {
  const scene = loadScene();
  assert.ok(
    !scene.includes('city-stats-handoff'),
    'city-stats-handoff found in CityScene.unity — legacy GO not purged'
  );
});

// ── T11.0.4 — B.7e sweep: zero live legacy GO prefab instances ───────────────

const LEGACY_GUIDS = [
  // city-stats-handoff.prefab
  'a153ec8cf629842019a03b5263e0f11a',
];

test('LegacyGO_AbsentFromScene — B.7e sweep: no legacy-GO prefab instances in CityScene.unity', () => {
  const scene = loadScene();
  const live = LEGACY_GUIDS.filter(guid => scene.includes(guid));
  assert.equal(
    live.length,
    0,
    `Live legacy GO prefab GUID(s) still present in CityScene: ${live.join(', ')}`
  );
});
