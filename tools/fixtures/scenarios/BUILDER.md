# Scenario builder (descriptor → save)

Structured **`scenario_descriptor_v1`** JSON (see [`docs/schemas/scenario-descriptor.v1.schema.json`](../../../docs/schemas/scenario-descriptor.v1.schema.json)) is validated in CI via **`npm run validate:fixtures`**. It is **interchange** JSON (not player **Save data**); keys use **camelCase** so Unity **`JsonUtility`** can parse the same files as Node.

## Strict vs repair

Product policy for invalid terrain / **Water map** / **road stroke** combinations is owned by [`.cursor/projects/TECH-31.md`](../../../.cursor/projects/TECH-31.md) **Open Questions**. This builder is **strict**: invalid descriptors fail with stable messages (Node or Unity). There is no silent repair outside the declared edit region.

## Declarative (Node) — terrain only

Use when the descriptor has **`layoutKind`:** **`declarative`**, no **`roadStrokes`**, and no **`waterMapData`**. Emits **`GameSaveData`**-compatible JSON the same way as [`generate-reference-flat-32x32.mjs`](./generate-reference-flat-32x32.mjs) (sorting order and planar positions aligned with terrain rules).

```bash
node tools/fixtures/scenarios/build-scenario-from-descriptor.mjs \
  --descriptor tools/fixtures/scenarios/descriptor-declarative-default-32x32/descriptor.json \
  --output tools/fixtures/scenarios/descriptor-declarative-default-32x32/save.json
```

From repo root (shortcut):

```bash
npm run scenario:build-from-descriptor -- \
  --descriptor tools/fixtures/scenarios/descriptor-declarative-default-32x32/descriptor.json \
  --output tools/fixtures/scenarios/descriptor-declarative-default-32x32/save.json
```

## Roads / water / full apply (Unity batch)

**Road stroke** placement must use the gameplay **road preparation** path (**`TryPrepareRoadPlacementPlan`** / interstate equivalent) plus **`PathTerraformPlan.Apply`** — never persisting from raw **`ComputePathPlan`** alone (see **geo** §13.1 and **roads-system**).

Use **`tools/scripts/unity-build-scenario-from-descriptor.sh`**: loads a base **Save data** (default **`reference-flat-32x32`**), applies the descriptor in **Play Mode**, then writes the output JSON.

```bash
npm run unity:build-scenario-from-descriptor -- \
  --descriptor tools/fixtures/scenarios/descriptor-street-row-32x32/descriptor.json \
  --output tools/fixtures/scenarios/descriptor-street-row-32x32/save.json
```

If the Unity Editor already holds the repo lock, pass **`--quit-editor-first`** (see [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md)).

## AUTO-adjacent pattern

**`layoutKind`:** **`autoAdjacent`** marks intent for **simulation-driven** layout (bounded ticks, then **Save data** export). The descriptor itself must not include **`roadStrokes`**; the workflow is: run **Play Mode** / **Agent test mode batch** with your harness, export **`GameSaveData`** via **`GameSaveManager`**, then commit or diff. This is documentation-only until a dedicated AUTO harness lands.

## Regeneration when `GameSaveData` changes

After C# serialization changes, re-run the Node command for declarative fixtures and re-run the Unity batch for scenarios that include **road stroke** rows. Then **`npm run validate:all`** and **`npm run unity:compile-check`** (see [`README.md`](./README.md)).
