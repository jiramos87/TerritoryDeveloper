---
purpose: "UI bake pipeline contract — production dispatch path, stale-DLL gate, dead-code map, sub-view slot reset pattern. Force-loaded only when touching bake surfaces."
audience: agent
loaded_by: on-demand
slices_via: none
description: UI bake pipeline rules — `panels.json` → `NormalizeChildKind` → `BakeChildByKind` switch is the ONLY production dispatch path. Stale-DLL gate before `bake_ui_from_ir`. KindRendererMatrix + KindRenderers/ + RowBakeHandler are non-production. Sub-view slot LayoutElement restore-on-unmount.
alwaysApply: false
---

# UI Bake Pipeline Contract

**Principle:** UI bakes (`panels.json` → prefab) flow through a single dispatch path. Source edits to `Assets/Scripts/Editor/UiBake/**` or `Assets/Scripts/Editor/Bridge/UiBakeHandler.*.cs` do NOT apply until Unity recompiles the Editor DLL. Skip the recompile and `bake_ui_from_ir` bakes against stale code — false-green stage, broken prefab.

Battle-tested on `cityscene-mainmenu-panel-rollout` Stage 13 settings widget loop (slider-row / toggle-row / dropdown-row / section-header). Hours lost to "why is the prefab still wrong?" → answer: stale DLL + dead-code red herring.

---

## §1 — Production dispatch path (the ONLY path)

For each child of a panel in `Assets/UI/Snapshots/panels.json`:

```
panel.children[i].kind          ← outer kind (often "panel" for composite widgets)
panel.children[i].params_json.kind ← inner kind (e.g. "slider-row")

innerKind = NormalizeChildKind(outerKind, innerKind)
BakeChildByKind(childGo, innerKind, ...)   ← switch in UiBakeHandler.cs:486
```

Authoritative source:

- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` line ~447 — `NormalizeChildKind(outerKind, innerKind)`
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` line ~486 — `BakeChildByKind` switch (handles `slider-row`, `toggle-row`, `dropdown-row`, `section-header`, `label`, `button`, etc.)
- `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs` line ~2208 — call site inside `BakePanelSnapshotChildren`

**Rule:** New widget kind → add `case "{kind}":` branch inside `BakeChildByKind`. Add `panels.json` IR shape under `params_json.kind = "{kind}"`. That is the contract — nothing else.

**Do NOT** add a new file under `Assets/Scripts/Editor/UiBake/KindRenderers/` and expect production to pick it up. KindRenderer code is **not on the bake path** (see §3).

---

## §2 — Stale-DLL gate (force recompile before bake)

When `Assets/Scripts/Editor/**/*.cs` is touched in current stage, **MUST** run `unity_compile` before any `bake_ui_from_ir` mutation. Otherwise the bake executes against the prior compiled Editor DLL — source edits silently invisible.

```
1. Edit Assets/Scripts/Editor/Bridge/UiBakeHandler.*.cs
2. unity_compile (or unity:compile-check)         ← MANDATORY
3. unity_bridge_command(kind="bake_ui_from_ir")   ← now uses fresh code
```

**Symptom of skip:** prefab `m_Name` list still shows old child names (Track / Thumb / Fill) instead of new ones (SliderHost / Background / Fill Area / Handle). Grep `m_Name:` in the generated prefab to verify post-bake.

**Stage-level gate.** `ship-cycle` Pass A already aggregates `Assets/**/*.cs` and runs one `unity:compile-check` per stage. The gate above applies inside a stage *between* a source edit and the bake invocation that should pick it up — typically during iterative bake-then-inspect loops within Pass A.

---

## §3 — Dead-code map (do NOT extend)

The following surfaces look like dispatch infrastructure but are **non-production** as of `cityscene-mainmenu-panel-rollout` Stage 13:

| Path | Status | Notes |
|---|---|---|
| `Assets/Scripts/Editor/UiBake/KindRendererMatrix.cs` | dead | Maps kind → IKindRenderer impl. Not referenced from production `BakeChildByKind`. |
| `Assets/Scripts/Editor/UiBake/KindRenderers/*.cs` (SliderRowRenderer, ToggleRowRenderer, DropdownRowRenderer, SectionHeaderRenderer, ListRowRenderer, ExpenseRowRenderer, ReadoutBlockRenderer, IKindRenderer) | dead | Only referenced by test fixtures + KindRendererMatrix. Production bake never enters this code. |
| `Assets/Scripts/Editor/UiBake/Plugins/RowBakeHandler.cs` | dead stub | Logs `[RowBakeHandler] Bake dispatched for kind='{child.kind}'` — never writes content. |

**Why kept:** test fixtures + cross-reference. Retiring requires unwinding the asmdef `UiBake.KindRenderers.Editor` and confirming no test references break. Defer to a dedicated TECH issue (`retire-kindrenderer-deadcode`).

**Until retired:** treat the KindRenderer surface as read-only archeology. Adding a renderer there is dead code that ships nothing.

---

## §4 — Sub-view slot mount/unmount (LayoutElement restore pattern)

When a navigated-view UX mounts a sub-view prefab into a content slot governed by a VerticalLayoutGroup parent:

1. **On mount** — capture current `LayoutElement.flexibleHeight` + `preferredHeight` from the slot, then force `flexibleHeight = 1f` + `preferredHeight = -1f` so the slot expands to full available height. Without this the sub-view bakes inside a 0-height rect and the nav-header / scroll content ends up off-screen.
2. **On unmount** — restore the captured `flexibleHeight` + `preferredHeight` BEFORE deactivating the slot, so the root button column re-layouts cleanly when shown again.

Canonical impl: `Assets/Scripts/UI/Modals/PauseMenuDataAdapter.cs` — `MountSubView` / `UnmountSubView` (search for `_slotSavedFlexHeight` + `_slotSavedPrefHeight`).

**When to apply:** any new navigated-view adapter that swaps prefabs into a content slot under a `VerticalLayoutGroup` (settings, save, load, options sub-screens). Always pair save+restore — capturing without restore breaks the parent layout on second mount.

**Future refactor:** lift the save/restore pair into a reusable helper (e.g. `SubViewSlotMount` static class with `Mount(Transform slot, GameObject prefab, out SavedLayout)` + `Unmount(Transform slot, SavedLayout)`). Not blocking; multiple call sites are needed first.

---

## §5 — Verification checklist (after a bake-pipeline edit)

- [ ] Edited file under `Assets/Scripts/Editor/Bridge/UiBakeHandler.*.cs` OR `Assets/Scripts/Editor/UiBake/**` (only the live surfaces — see §3 dead-code list).
- [ ] `unity_compile` run AFTER the edit, BEFORE any `bake_ui_from_ir` invocation in the same iteration loop.
- [ ] Re-bake target prefab via `unity_bridge_command(kind="bake_ui_from_ir", ir_path="Assets/UI/Snapshots/panels.json", panels=[...])`.
- [ ] Post-bake `grep m_Name: {prefab.path}` returns the expected child-name shape (not stale shape from the prior DLL).
- [ ] If new widget kind added → `panels.json` IR has `params_json.kind = "{new-kind}"` for at least one child; not just the C# case branch.

---

## Cross-links

- Skill that owns bake invocations: `ia/skills/ship-cycle/SKILL.md` §Guardrails (force-recompile clause).
- Scene wiring (companion contract — same agent-owns-Unity principle): `ia/rules/unity-scene-wiring.md`.
- Master plan that produced this rule: `cityscene-mainmenu-panel-rollout` Stage 13 (navigated-view UX hotfix QA).

---

## Changelog

### 2026-05-11 — Rule introduced (Stage 13 hardening)

**Symptom (Stage 13 navigated-view UX loop):** Settings widgets (slider-row / toggle-row / dropdown-row) baked with wrong child-name shape (Track / Thumb / Fill instead of SliderHost / Background / Fill Area / Handle) despite repeated source edits to `UiBakeHandler.cs`. Hours lost chasing a dispatch mystery — agent inspected `KindRendererMatrix` + `KindRenderers/*.cs` impls believing they were the production path because the prefab shape matched KindRenderer's `Track/Thumb/Fill` template.

**Root cause:** stale Editor DLL. `bake_ui_from_ir` ran against the prior compiled assembly; source edits in `UiBakeHandler.cs:616` (slider-row case) were silently invisible. `KindRendererMatrix` + `IKindRenderer` impls are **dead code** — only referenced by test fixtures + a stub `RowBakeHandler.cs` that logs but never writes. The actual dispatch is `NormalizeChildKind(outerKind, innerKind) → BakeChildByKind(switch)` in `UiBakeHandler.cs`.

**Fix:** This rule. Four guardrails locked in:
1. Production dispatch path documented (§1) — `BakeChildByKind` switch is the ONLY path.
2. Stale-DLL gate (§2) — `unity_compile` MUST precede `bake_ui_from_ir` after Editor C# edits.
3. Dead-code map (§3) — KindRendererMatrix + KindRenderers/ + RowBakeHandler are non-production; do not extend.
4. Sub-view slot LayoutElement reset pattern (§4) — capture-on-mount + restore-on-unmount; canonical impl in `PauseMenuDataAdapter`.

**Cross-link:** `ship-cycle` SKILL.md gained Guardrails reference to §2 (force-recompile before bake within iterative loops).
