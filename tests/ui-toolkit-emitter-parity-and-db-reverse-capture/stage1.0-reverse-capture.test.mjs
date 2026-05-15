// tests/ui-toolkit-emitter-parity-and-db-reverse-capture/stage1.0-reverse-capture.test.mjs
//
// Stage 1.0 bridge-aware integration test — Pass B verify-loop gate.
// Reverse capture — read iter-43 toolbar surface → DB migration draft.

import { describe, it, beforeAll, afterAll, expect } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();
const SCENE_PATH = '';

describe('ui-toolkit-emitter-parity-and-db-reverse-capture Stage 1.0 — Reverse capture', () => {
  beforeAll(async () => {
    if (SCENE_PATH) {
      await bridge.openScene(SCENE_PATH);
    }
  }, 30_000);

  afterAll(async () => {
    await bridge.close();
  });

  it('TECH-836: Schema extension migration — node_kind, uss_class[], style_props_json on panel_child', async () => {
    // TODO(spec-implementer): query information_schema.columns for panel_child;
    //   assert node_kind text, uss_class text[], style_props_json jsonb all present.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-837: Reverse-capture parser script — toolbar.uxml + toolbar.uss + ToolbarHost.cs → SQL INSERT plan', async () => {
    // TODO(spec-implementer): exec tools/scripts/ui-toolkit-reverse-capture.mjs;
    //   assert stdout contains valid INSERT INTO panel_child with >=12 rows
    //   + token_detail cream theme rows.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-838: Apply reverse-capture migration — panel_detail.toolbar v616 + 12 panel_child rows', async () => {
    // TODO(spec-implementer): query DB SELECT count from panel_child WHERE panel_id=100;
    //   assert >= 12; assert panel_detail.toolbar.version = 616.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-839: Confirm legacy prefab path untouched post-migration', async () => {
    // TODO(spec-implementer): exec npm run unity:bake-ui; assert legacy prefab
    //   for non-toolbar panel still emitted; toolbar.uxml on disk byte-equal to
    //   pre-bake snapshot (no .baked yet — Stage 2.0).
    throw new Error('red — not yet implemented');
  }, 30_000);
});
