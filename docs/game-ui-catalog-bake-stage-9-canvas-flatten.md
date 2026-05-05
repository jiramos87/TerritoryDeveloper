# Stage 9.1 — Canvas wrapper flatten audit + re-parent map

TECH-14446 deliverable. Audits descendant Canvas components under root `UI Canvas` after Stage 8 D9 root-only check passed but `ui_tree_walk` reported `canvas_count: 4`. Anchor for T9.1.2 (bridge mutation) + T9.1.3 (test tighten).

## §Method

Inventory sources:

- **Bridge audit:** `unity_bridge_command kind=ui_tree_walk root_path="UI Canvas"` on `feature/asset-pipeline` MainScene (2026-05-05). Two wrapper Canvases identified — `UI Canvas/Canvas` (800×600 vestigial) + `UI Canvas/Canvas (Game UI)` (empty leaf) — plus one nested Canvas component on `DateText` leaf.
- **Code path-rewrite scan:** `Grep` for literal substrings `UI Canvas/Canvas` and `Canvas (Game UI)` across `Assets/Scripts/**/*.cs` + `Assets/Tests/**/*.cs`. Result: zero hits in C# sources; only `Assets/Scenes/MainScene.unity:9253` carries the wrapper name. No path-rewrite needed.
- **Catalog-baked siblings (no-touch):** root `UI Canvas` already hosts 15 catalog-baked direct children (`toolbar`, `hud-bar`, `info-panel`, `pause-menu`, `settings-screen`, `save-load-screen`, `new-game-screen`, `splash`, `onboarding-overlay`, `glossary-panel`, `city-stats-handoff`, `overlay-toggle-strip`, `BudgetPanel`, `HudEstimatedSurplusHint`, `ConstructionCostText`). T9.1.2 must NOT touch these.

## §Re-parent map

T9.1.2 bridge calls. Each entry = one `set_gameobject_parent` invocation; `world_position_stays: true` per Stage 9 locked decision.

```json
[
  {
    "child_name": "ControlPanel",
    "current_path": "UI Canvas/Canvas/ControlPanel",
    "new_path": "UI Canvas/ControlPanel",
    "world_position_stays": true
  },
  {
    "child_name": "DebugPanel",
    "current_path": "UI Canvas/Canvas/DebugPanel",
    "new_path": "UI Canvas/DebugPanel",
    "world_position_stays": true
  },
  {
    "child_name": "ProposalUI",
    "current_path": "UI Canvas/Canvas/ProposalUI",
    "new_path": "UI Canvas/ProposalUI",
    "world_position_stays": true
  },
  {
    "child_name": "MiniMapPanel",
    "current_path": "UI Canvas/Canvas/MiniMapPanel",
    "new_path": "UI Canvas/MiniMapPanel",
    "world_position_stays": true
  }
]
```

Wrapper deletes (post-re-parent — must be empty):

```json
[
  { "kind": "delete_gameobject", "target_path": "UI Canvas/Canvas" },
  { "kind": "delete_gameobject", "target_path": "UI Canvas/Canvas (Game UI)" }
]
```

`UI Canvas/Canvas (Game UI)` carries zero children at audit time → safe to delete directly without prior re-parent.

## §Path-rewrite log

Zero hits in C# sources. No code edits required.

| File | Line | Pattern matched | Rewrite |
|---|---|---|---|
| _none_ | — | — | — |

`MainScene.unity:9253` is the wrapper's own `m_Name` field — not a code reference; deleted as part of T9.1.2 wrapper-delete step.

## §Component-removal log

**T9.1.2 finding (2026-05-05):** audit prediction superseded by post-mutation `ui_tree_walk` evidence.

Audit predicted a deep Canvas component pair on `DateText` requiring `remove_component` (CanvasScaler then Canvas). T9.1.2 execution found:

1. `remove_component` failed `target_not_found` for `UI Canvas/ControlPanel/DataPanelButtons/DatePanel/DateText` — that path does not exist in the scene. ControlPanel actual children list (per ui_tree_walk): `BuildingSelectorPopupPanel`, `BulldozerButton`, `RoadsSelectorButton`, `PowerBuildingSelectorButton`, `EnviromentalSelectorButton`, `WaterBuildingSelectorButton`, `R/C/I/StateServiceZoningSelectorButton`. No `DataPanelButtons` child.
2. Post-mutation scene-wide `ui_tree_walk` returned `canvas_count: 1` with no descendant Canvas components. Wrapper deletion alone (`UI Canvas/Canvas` + `UI Canvas/Canvas (Game UI)`) closed the descendant-Canvas loophole — the original Stage 8 audit's `canvas_count: 4` reading was the 2 wrappers + 2 Canvas-bearing leaves under the wrappers (deleted with the wrappers), not a separate component on a leaf under `ControlPanel`.

**No component-removal step required.** Scene-wide `Canvas count == 1` invariant (T9.1.3 target) achieved by re-parent + wrapper-delete steps alone.

| Target path | Components to remove | Order |
|---|---|---|
| _none required_ | — | — |

## §Findings

- **4 legacy children** under wrapper `UI Canvas/Canvas` — all hand-authored predecessors of catalog-baked siblings already on root. Re-parent (not rebuild) preserves Inspector field bindings on `ControlPanel.DataPanelButtons` (T8.5 CellDataPanel binding test depends on these refs).
- **1 empty wrapper** `UI Canvas/Canvas (Game UI)` — pure delete; no re-parent traffic.
- **1 nested Canvas component** on `DateText` leaf — vestigial WorldSpace micro-opt; `remove_component` (CanvasScaler then Canvas) closes the descendant-Canvas loophole.
- **Zero code path-rewrites** — no C# source references the wrapper paths. T9.1.2 is pure scene-graph mutation + component drops.
- **Anchor rebake risk:** `world_position_stays: true` keeps each re-parented child visually stable mid-mutation; child `RectTransform` `anchoredPosition` recomputes against new parent rect (root 1920×1080 instead of wrapper 800×600). Visible game-view shift is the deliverable, not a regression — children gain correct full-viewport anchors per Stage 9 locked decision.

## §Follow-ups

None for T9.1.2. Bridge mutation list is complete + ordered; component-removal target identified; zero code edits required.

## Cross-references

- Anchor: `docs/game-ui-catalog-bake-post-mvp-extensions.md` §Stage 9 (Findings log row to be appended on T9.1.2 + T9.1.3 closure).
- Bridge mutation pin: T9.1.2 (TECH-14447) consumes this re-parent map verbatim.
- Test-tighten pin: T9.1.3 (TECH-14448) rewrites `Assets/Tests/EditMode/UI/SingleRootCanvasTest.cs` to assert scene-wide `Canvas` count == 1 + adds descendant-ban probe.
- Stage 8 anchor: `docs/game-ui-catalog-bake-stage-8-legacy-parity-audit.md` (root-only check that this stage tightens).
