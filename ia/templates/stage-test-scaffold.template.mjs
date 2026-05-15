// {{TEST_FILE_PATH}}
//
// Stage {{STAGE_ID}} bridge-aware integration test — Pass B verify-loop gate.
// Authored by ship-plan at plan-file time (skeleton); spec-implementer fills
// each `it()` body during ship-cycle Pass A, one per task. Red on first task,
// fully green after last task — ship-cycle stage closeout requires green.
//
// Slots filled by ship-plan from handoff YAML:
//   {{SLUG}}              — master-plan slug (e.g. region-scene-prototype)
//   {{STAGE_ID}}          — stage id (e.g. 1.0)
//   {{STAGE_TITLE}}       — stage title from handoff YAML stages[].title
//   {{SCENE_PATH}}        — primary scene path for this stage (e.g. Assets/Scenes/RegionScene.unity)
//   {{TASK_ASSERTION_BLOCKS}} — one `it('TASK-id: title', ...)` stub per task,
//                                with `// TODO(spec-implementer): assert ...` body
//
// Run gate:
//   - validate:fast → does NOT run this file (would require live Editor)
//   - ship-cycle Pass B verify-loop step `run_stage_test`:
//       node --test {{TEST_FILE_PATH}}  (or vitest equivalent)
//     Hard-fails Pass B if any assertion red. Worktree stays dirty; no commit.

import { describe, it, beforeAll, afterAll, expect } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();
const SCENE_PATH = '{{SCENE_PATH}}';

describe('{{SLUG}} Stage {{STAGE_ID}} — {{STAGE_TITLE}}', () => {
  beforeAll(async () => {
    // Asserts active scene matches SCENE_PATH. Editor must be open with that scene.
    // No scene-load mutation kind exists yet; tests rely on pre-arranged Editor state.
    if (SCENE_PATH) {
      await bridge.openScene(SCENE_PATH);
    }
  }, 30_000);

  afterAll(async () => {
    await bridge.close();
  });

{{TASK_ASSERTION_BLOCKS}}
});
