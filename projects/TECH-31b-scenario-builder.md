# TECH-31b — Scenario builder (descriptor → save)

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31b**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a** (load path and artifact layout exist).

## Summary

Provide **Editor** / **Node** / **Unity** helpers to assemble **`GameSaveData`-compatible** outputs from structured descriptors (YAML/JSON): **terrain** / **water**, **road stroke** (via **road preparation** family ending in **PathTerraformPlan**—never **`ComputePathPlan`** alone), **zoning**, **buildings**, **in-game time**, treasury, **CityStats** / demand preconditions—without violating **invariants** (**HeightMap** / **Cell.height**, **`InvalidateRoadCache()`**, **shore** refresh, no new **GridManager** responsibilities—extract helpers).

## Goals

- **Builder** APIs apply domain rules through the same code paths as gameplay where possible.
- Validation: reject or repair per product rules (see [`.cursor/projects/TECH-31.md`](../.cursor/projects/TECH-31.md) **Open Questions**—strict vs **best-effort repair**).
- One **reference** descriptor + generated artifact (**AUTO**-adjacent acceptable, e.g. **BUG-52**-class).
- Glossary-aligned error messages for invalid descriptors.

## Non-goals

- Full **property-based** generation (**TECH-35**). **MCP** exposure (31e). **TECH-82** (31d).

## Risks

| Risk | Mitigation |
|------|------------|
| **Invariant violations** | Fail the build; do not emit invalid saves. |
| **Missing **`GameSaveData`** fields** | Scoped **persistence** extension per **persistence-system** or document v1 gaps. |

## Implementation checklist

- [ ] **Builder** pipeline: descriptor → valid save.
- [ ] Validation pass + errors.
- [ ] Reference descriptor + artifact under agreed `tools/fixtures/` (or **`Assets/`**) path.
- [ ] Document how to add **AUTO** and one non-**AUTO** scenario pattern.

## Test contracts (stage)

| Goal | Check | Notes |
|------|--------|--------|
| Generated save loads in **test mode** | Uses **31a** load path | Round-trip |
| Descriptor schema | Extend **`validate:fixtures`** if JSON Schema added | Optional |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
|  |  |  |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
