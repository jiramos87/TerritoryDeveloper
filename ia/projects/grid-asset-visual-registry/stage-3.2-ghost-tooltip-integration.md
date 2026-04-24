### Stage 3.2 — Ghost + tooltip integration

**Status:** Done — 2026-04-24 (5 tasks closed: **TECH-757**..**TECH-761**)

**Objectives:** Preview flows set **green/red** tint from validator; tooltips show **reason** string; **`GridManager`** hit-test contract unchanged.

**Exit:**

- Play Mode manual smoke documented; no **`Collider2D`** added to world tiles.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.2.1 | Wire ghost preview to validator | **TECH-757** | Done | `CursorManager` or dedicated preview helper calls **`CanPlace`** each move; throttle if needed (no per-frame `FindObjectOfType`). |
| T3.2.2 | Valid tint path | **TECH-758** | Done | Reuse existing sprite tint utilities; ensure **sortingOrder** unaffected. |
| T3.2.3 | Invalid tint path | **TECH-759** | Done | Red tint + reason propagation to UI layer. |
| T3.2.4 | Tooltip reason string | **TECH-760** | Done | `UIManager` or local tooltip controller shows **human-readable** mapping from enum. |
| T3.2.5 | Play Mode smoke checklist | **TECH-761** | Done | Document scenario steps for verify-loop; no automated Play test required if policy says manual — state explicitly. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
mechanicalization_score:
  anchors: ok
  picks: ok
  invariants: ok
  validators: ok
  escalation_enum: ok
  overall: fully_mechanical
```

```yaml
- operation: file_task
  reserved_id: "TECH-757"
  target_anchor: "task_key:T3.2.1"
  issue_type: "tech"
  title: "Wire ghost preview to validator"
  priority: "high"
  notes: |
    `CursorManager` (or dedicated preview helper) calls `PlacementValidator.CanPlace`
    on cursor move; throttle per-move not per-frame; no `FindObjectOfType` in hot path.
    Consumes TECH-688/689 API. Guardrail #3 — extract helper if `GridManager` surface grows.
  depends_on:
    - "TECH-688"
    - "TECH-689"
  related:
    - "TECH-758"
    - "TECH-759"
    - "TECH-760"
    - "TECH-761"
  stub_body:
    summary: |
      Cursor preview hook calls `PlacementValidator.CanPlace(assetId, cell, rotation)` each
      cell-change event; result drives tint path (TECH-758/759) and tooltip (TECH-760).
      Throttle to cell-change, not per-frame.
    goals: |
      - Cursor move event resolves `PlacementValidator` reference once (cached, not
        `FindObjectOfType` per-frame).
      - `CanPlace` called on cell-change only; no per-frame allocation in hit-test path.
      - Result tuple (bool + reason) forwarded to tint + tooltip consumers via event or
        direct call.
      - `GridManager.GetCell(x,y)` only; no `cellArray` touch (invariant #5).
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/CursorManager.cs` — preview hook entry.
      - `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` (TECH-688) — consumer of
        `CanPlace`.
      - `Assets/Scripts/Managers/GameManagers/GridManager.cs` — hit-test contract (unchanged).
      - Possible new: `CursorPreviewController.cs` helper if extraction warranted (guardrail #3).
    impl_plan_sketch: |
      - Phase 1 — Hook + throttle: cache validator ref on Awake; subscribe to cursor
        cell-change event; call `CanPlace`; route result to tint + tooltip consumers.

- operation: file_task
  reserved_id: "TECH-758"
  target_anchor: "task_key:T3.2.2"
  issue_type: "tech"
  title: "Valid tint path"
  priority: "medium"
  notes: |
    Reuse existing sprite tint utilities for green/valid state; preserve `sortingOrder`;
    no new `Collider2D` on world tiles (Stage Exit criteria).
  depends_on:
    - "TECH-688"
  related:
    - "TECH-757"
    - "TECH-759"
    - "TECH-760"
    - "TECH-761"
  stub_body:
    summary: |
      Valid placement path: green tint applied to ghost sprite via existing tint utility;
      `sortingOrder` preserved; no new world-tile collider. Consumed by TECH-757 hook.
    goals: |
      - Green tint uses existing sprite tint utility pattern; no new `SpriteRenderer` writes.
      - `sortingOrder` and sorting group preserved across tint transitions.
      - Tint state reverts on cursor leave / placement commit.
      - No new `Collider2D` on world tiles (Stage Exit).
    systems_map: |
      - Existing sprite tint utility (locate via `SpriteRenderer.color` touch sites in
        Managers/GameManagers).
      - `CursorManager` / preview hook (TECH-757) — caller.
      - `PlacementValidator` result (`PlacementFailReason.None`) — trigger.
    impl_plan_sketch: |
      - Phase 1 — Tint apply/revert path: wire valid result → tint utility; revert on
        cursor leave / commit.

- operation: file_task
  reserved_id: "TECH-759"
  target_anchor: "task_key:T3.2.3"
  issue_type: "tech"
  title: "Invalid tint path"
  priority: "medium"
  notes: |
    Red tint + reason enum propagation to UI layer (TECH-760 consumer). Same tint utility
    reuse as TECH-758; preserve `sortingOrder`; no new `Collider2D`.
  depends_on:
    - "TECH-688"
    - "TECH-689"
  related:
    - "TECH-757"
    - "TECH-758"
    - "TECH-760"
    - "TECH-761"
  stub_body:
    summary: |
      Invalid placement path: red tint on ghost + `PlacementFailReason` enum propagated to
      UI layer for tooltip consumption (TECH-760). Reuses tint utility (TECH-758).
    goals: |
      - Red tint on invalid result using same utility as valid path.
      - `PlacementFailReason` enum value forwarded to UI / tooltip consumer.
      - `sortingOrder` preserved (invariant with TECH-758).
      - No new `Collider2D` on world tiles.
    systems_map: |
      - Same sprite tint utility as TECH-758.
      - `PlacementValidator` result tuple carries reason (TECH-689).
      - UI tooltip consumer (TECH-760) — downstream of enum forward.
    impl_plan_sketch: |
      - Phase 1 — Invalid branch + reason forward: apply red tint; emit reason event /
        direct call to tooltip layer.

- operation: file_task
  reserved_id: "TECH-760"
  target_anchor: "task_key:T3.2.4"
  issue_type: "tech"
  title: "Tooltip reason string"
  priority: "medium"
  notes: |
    `UIManager` or local tooltip controller maps `PlacementFailReason` enum → human-readable
    string. Table-driven map; no switch sprawl. Consumer of TECH-759 enum forward.
  depends_on:
    - "TECH-689"
  related:
    - "TECH-757"
    - "TECH-758"
    - "TECH-759"
    - "TECH-761"
  stub_body:
    summary: |
      Tooltip controller receives `PlacementFailReason` enum from TECH-759; maps to
      human-readable string via table; renders in existing tooltip surface.
    goals: |
      - Table-driven enum → string map (one entry per `PlacementFailReason`); no nested switch.
      - Renders via existing `UIManager` tooltip surface pattern (no new tooltip prefab if avoidable).
      - Empty / None reason → tooltip hidden.
      - Localization hook-point reserved (string keys not hard-coded user strings if
        localization pattern exists).
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/UIManager.cs` — tooltip host.
      - `PlacementFailReason` enum (TECH-689) — input.
      - Preview hook (TECH-757) — upstream caller.
    impl_plan_sketch: |
      - Phase 1 — Enum→string table + render: define table; wire from invalid path;
        hide on valid / cursor leave.

- operation: file_task
  reserved_id: "TECH-761"
  target_anchor: "task_key:T3.2.5"
  issue_type: "tech"
  title: "Play Mode smoke checklist"
  priority: "medium"
  notes: |
    Document manual Play Mode scenario steps for verify-loop (per Stage Exit: manual
    smoke documented). Covers valid tint, invalid tint per reason, tooltip render, no
    new collider introduced. No automated Play test — state explicitly in doc.
  depends_on:
    - "TECH-757"
    - "TECH-758"
    - "TECH-759"
    - "TECH-760"
  related:
    - "TECH-757"
    - "TECH-758"
    - "TECH-759"
    - "TECH-760"
  stub_body:
    summary: |
      Manual Play Mode smoke checklist doc (no automated Play test). Covers ghost tint
      valid/invalid per reason, tooltip render, `sortingOrder` intact, no world-tile
      collider regression.
    goals: |
      - Checklist enumerates scenarios: valid placement tint, each `PlacementFailReason`
        branch, tooltip text correctness, cursor-leave revert.
      - States explicitly: manual per policy; no automated Play test.
      - Lives in `docs/` (likely under `docs/implementation/` or verify-loop scenario folder).
      - Verify-loop operator can follow end-to-end in one session.
    systems_map: |
      - New doc file under `docs/implementation/` (filename TBD at implement time).
      - Scenarios reference TECH-757..760 surfaces.
      - No code changes.
    impl_plan_sketch: |
      - Phase 1 — Doc author + verify: draft scenarios; walk through once in Play Mode;
        amend as needed.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
