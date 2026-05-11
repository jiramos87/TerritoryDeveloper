---
purpose: "large-file-atomization-hub-thinning-sweep — hub-thinning sweep across Assets/Scripts/**."
audience: both
loaded_by: ondemand
slices_via: none
---
# large-file-atomization-hub-thinning-sweep — Hub-thinning sweep

> **Issue:** [large-file-atomization-hub-thinning-sweep](../../BACKLOG.md)
> **Status:** In Progress
> **Created:** 2026-05-11
> **Last updated:** 2026-05-11

## 1. Summary

Hub-thinning sweep across `Assets/Scripts/**` — end state: zero file >500 LOC; every Unity GO-inspector hub trimmed to thin pass-through delegate via `ServiceRegistry`. Builds on predecessor large-file-atomization plans. Uses `ServiceRegistry` pattern (new, Stage 0) as service-locator infra for all domains.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Zero C# file >500 LOC in `Assets/Scripts/` (enforced by `validate:no-hub-fat` + `validate:no-service-fat` when `ATOMIZATION_GATES=1`).
2. Every Unity GO-inspector hub trimmed to thin pass-through delegate resolved via `ServiceRegistry`.
3. `Domains/_Registry/ServiceRegistry` present in every production scene.
4. Three Tier-F validator stubs wired and gated (`validate:no-hub-fat`, `validate:no-service-fat`, `validate:registry-resolve-pattern`).
5. Glossary row `service registry` + rule invariant #12 locked.

### 2.2 Non-Goals (Out of Scope)

1. Web / MCP server file-size changes.
2. Changing gameplay behavior.
3. Activating `ATOMIZATION_GATES=1` in CI before Stage 8.0.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Zero >500 LOC files — any new file over limit is caught before merge | `ATOMIZATION_GATES=1 npm run validate:no-hub-fat` exits 0 |
| 2 | Developer | Service wiring is discoverable via registry pattern | `validate:registry-resolve-pattern` exits 0 |

## 4. Current State

### 4.1 Domain behavior

Multiple `Managers/**/*.cs` files exceed 500–2000+ LOC. Inspector hubs hold direct serialized references to concrete managers. No cross-domain facade-locator pattern exists yet.

### 4.2 Systems map

- `Assets/Scripts/Managers/` — primary source of hub files
- `Assets/Scripts/Domains/` — already partially atomized (Terrain, Roads, Grid, Water, Zones, UI, Bridge, Economy, Geography)
- `Assets/Scripts/Domains/_Registry/` — NEW Stage 0 (ServiceRegistry)

### 4.3 Implementation investigation notes (optional)

Predecessor closed plans: `large-file-atomization-componentization-strategy` + `large-file-atomization-refactor`. Strategy γ (facade interface + facade impl MonoBehaviour + POCO services + per-domain asmdef) is the locked pattern.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible behavior change. Internal restructuring only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

See `docs/post-atomization-architecture.md §Service Registry` for wiring rules. Stages 1–8 execute the sweep per master plan.

### 5.3 Method / algorithm notes (optional)

Strategy γ folder shape: `Domains/{X}/I{X}.cs` + `{X}.cs` + `Services/{Concern}Service.cs` + `{X}.asmdef`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-05-11 | `ServiceRegistry` pattern over direct `FindObjectOfType` per-consumer | Decouples hub-to-service wire; single lookup per scene Awake | Zenject/VContainer (overkill for this game scale) |
| 2026-05-11 | `ATOMIZATION_GATES=1` opt-in until Stage 8.0 | Avoids CI red on pre-existing fat files during transition stages | Immediately enabling (would break CI) |

## 7. Implementation Plan

### Phase 0 — Pre-conditions (Stage 0)

- [x] Confirm validate:no-domain-game-cycle baseline green
- [x] Land `Domains/_Registry/` scaffold (IServiceRegistry + ServiceRegistry + asmdef)
- [x] Add ServiceRegistry GO to CityScene + MainMenu
- [x] Stub three Tier-F validators (env-flag gated OFF)
- [x] Doc: append §Service Registry to post-atomization-architecture
- [x] Doc: add invariant #12 to unity-invariants.md
- [x] Glossary: add `service registry` row
- [x] Tool: land `cs-find-zero-caller-publics.mjs`

### Phase 1–8 — Sweep stages

See master plan `large-file-atomization-hub-thinning-sweep-master-plan.md`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Registry scaffold compiles | Unity compile | `npm run unity:compile-check` | C# touched |
| Validator stubs exit 0 (gate OFF) | Node | `npm run validate:no-hub-fat && npm run validate:no-service-fat && npm run validate:registry-resolve-pattern` | Default OFF |
| No domain→game GUID cycle | Node | `npm run validate:no-domain-game-cycle` | Baseline |
| IA indexes updated | Node | `npm run generate:ia-indexes -- --check` | Glossary + rule edits |

## 8. Acceptance Criteria

- [ ] `npm run unity:compile-check` passes after Stage 0 C# landing
- [ ] Three new validators exit 0 (gate OFF)
- [ ] `validate:no-domain-game-cycle` stays green
- [ ] Glossary row `service registry` queryable via MCP `glossary_lookup`
- [ ] Invariant #12 present in `ia/rules/unity-invariants.md`
- [ ] Project spec file present at `ia/projects/large-file-atomization-hub-thinning-sweep.md`

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | LoadingScene.unity absent in repo | Scene not yet created | Skipped per task spec; wired CityScene + MainMenu only |

## 10. Lessons Learned

- Registry pattern needs scene-host GO wired at Stage 0 or later stages' `Awake` resolve races fire before registry exists.

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._

## §Plan Digest

_pending — populated by `/stage-authoring`._

### §Goal

### §Acceptance

### §Test Blueprint

### §Examples

### §Mechanical Steps

## Open Questions

None — tooling only; see §8 Acceptance criteria.

## parent_plans:

- `docs/explorations/large-file-atomization-refactor.md` (predecessor)
- `docs/large-file-atomization-componentization-strategy.md` (predecessor)
