---
purpose: "Unity C# runtime invariants + guardrails — never violate when touching Assets/Scripts/**/*.cs"
audience: agent
loaded_by: on-demand
slices_via: none
description: Unity runtime invariants and guardrails — Unity C# / MonoBehaviour / grid / roads / water / cliffs
alwaysApply: false
---

# System Invariants (NEVER violate)

1. `HeightMap[x,y]` == `Cell.height` — always in sync; update both on every write
2. After road modification → call `InvalidateRoadCache()`
3. No `FindObjectOfType` in `Update` or per-frame loops — cache in `Awake`/`Start`
4. No new singletons — use Inspector + `FindObjectOfType` pattern
5. No direct `gridArray`/`cellArray` access outside `GridManager` — use `GetCell(x, y)`. Carve-out: helper services under `Assets/Scripts/Managers/GameManagers/*Service.cs` extracted from `GridManager` per invariant #6 (hold a `GridManager grid` composition reference) share the owning class's trust boundary and may touch `grid.cellArray` / `grid.gridArray` directly; document the rationale at the touch site.
6. Do not add responsibilities to `GridManager` — extract to helper classes
7. Shore band: land Moore-adjacent to water must have `height ≤ min(S)` of neighbor water cells
8. Rivers: `H_bed` monotonically non-increasing toward exit
9. Cliff visible faces: south + east only — N/W not instantiated
10. Road placement: always through the **road preparation family** ending in `PathTerraformPlan` + Phase-1 + `Apply` — never `ComputePathPlan` alone
11. `UrbanizationProposal`: NEVER re-enable — obsolete (see **glossary** **Urbanization proposal**)

# Guardrails (IF → THEN)

- IF adding a manager reference → THEN `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`
- IF modifying roads → THEN call `InvalidateRoadCache()` after changes
- IF placing a road → THEN use the preparation family, NOT `ComputePathPlan` alone
- IF touching `GridManager` → THEN extract new logic to a helper class
- IF creating a new manager → THEN MonoBehaviour scene component, never `new`
- IF modifying `HeightMap` → THEN also write `Cell.height` (and vice versa)
- IF placing or removing water → THEN call `RefreshShoreTerrainAfterWaterUpdate`

# Bridge + tooling patterns

- `git mv` a `.cs` file: also `git mv` the adjacent `.meta` separately — preserves GUID, keeps prefab/scene refs intact.
- `Bridge get_compilation_status`: reliable compile gate when Unity Editor holds project lock and batchmode is blocked.
- `AgentBridgeCommandRunner.Mutations.cs` pattern: bridge kind expansions go in sibling partial class — isolates mutation dispatch, keeps diff reviewable. Reuse for future bridge additions.
- `JsonUtility.FromJson` + `value_kind` string + flat `string value` = polymorphic DTOs without Newtonsoft. DTOs must be `[Serializable]` + public fields; interpret at runtime in the switch.
- `AppDomain.CurrentDomain.GetAssemblies()` resolves component type names across Territory + third-party assemblies. Return `type_ambiguous:<name>;candidates=<csv>` on multi-hit, not silent first-match.
- `GameNotificationManager` omits from EditMode fixtures — `Awake` NPEs on null `notificationPanel`. Assert return value + state unchanged instead of queue-count delta.
- Manager-init race: gate consumer's tick block (not whole `Update`) on producer's `IsInitialized` — keeps UI responsive during load while blocking sim-state reads.

# How loaded

Not `@`-imported. Fetched on-demand:

- Router row in `ia/rules/agent-router.md` — "Unity C# work → fetch this file via `rule_content unity-invariants` or read directly".
- MCP `invariants_summary` — merges this file + `ia/rules/invariants.md` (universal) so single call still returns all 13 cardinal invariants.
