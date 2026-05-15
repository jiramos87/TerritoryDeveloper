// tests/ui-toolkit-emitter-parity-and-db-reverse-capture/stage4.0-cutover.test.mjs
//
// Stage 4.0 bridge-aware integration test — Pass B verify-loop gate.
// Cutover — atomic single commit: rename .baked → canonical + Host Q-rewrite + adapter delete + Play Mode smoke.

import { describe, it, beforeAll, afterAll, expect } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();
const SCENE_PATH = 'Assets/Scenes/CityScene.unity';

describe('ui-toolkit-emitter-parity-and-db-reverse-capture Stage 4.0 — Cutover', () => {
  beforeAll(async () => {
    if (SCENE_PATH) {
      await bridge.openScene(SCENE_PATH);
    }
  }, 30_000);

  afterAll(async () => {
    await bridge.close();
  });

  it('TECH-842: Rename .baked → canonical — overwrite iter-43 toolbar.uxml/.uss + cream.tss', async () => {
    // TODO(spec-implementer): fs.statSync Assets/UI/Generated/toolbar.uxml exists,
    //   .baked.uxml absent. Cream.tss carries Generated-by banner.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-34685: Host Q-rewrite — ToolbarHost.cs TileSlugs[] + _btns Q strings match DB-emitted slugs', async () => {
    // TODO(spec-implementer): grep Assets/Scripts/UI/Hosts/ToolbarHost.cs for
    //   TileSlugs literal; assert each slug present in DB panel_child rows for
    //   panel_id=100; assert class ToolbarHost preserved + [SerializeField] _doc
    //   + _subtypePicker intact.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-843: ToolbarDataAdapter + ToolbarAdapterService delete — gated on zero references', async () => {
    // TODO(spec-implementer): fs.existsSync Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs
    //   == false; same for ToolbarAdapterService.cs; grep Assets/Scripts/ shows
    //   zero references to either class name.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-844: Play Mode smoke — toolbar renders + clicks route scenario', async () => {
    // TODO(spec-implementer): bridge.enterPlayMode; bridge.uiTreeWalk asserts
    //   toolbar VE present with 9 tile buttons; bridge.dispatchAction or click
    //   each slug; assert ToolbarVM.ActiveTool matches; bridge.exitPlayMode.
    throw new Error('red — not yet implemented');
  }, 30_000);

  it('TECH-845: Idempotence verify — second unity:bake-ui produces zero git diff', async () => {
    // TODO(spec-implementer): exec npm run unity:bake-ui twice via child_process;
    //   exec git diff --exit-code Assets/UI/Generated/toolbar.uxml toolbar.uss
    //   Assets/UI/Themes/cream.tss; assert exit 0.
    throw new Error('red — not yet implemented');
  }, 30_000);
});
