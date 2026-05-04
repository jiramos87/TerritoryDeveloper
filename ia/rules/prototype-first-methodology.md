---
purpose: "Force-loaded canonical rule for prototype-first methodology — Stage 1.0 of every master plan ships a tracer slice (one real player/agent verb, hardcoded data OK, peripherals stubbed); Stages 2+ each declare a unique §Visibility Delta. Locked decisions D1/D7/D8/D9/D10 from `docs/prototype-first-methodology-design.md` §6."
audience: agent
loaded_by: force-loaded
alwaysApply: true
---

# Prototype-first methodology

## Driving intent

Every future master plan delivers a playable thinnest slice in Stage 1.0; subsequent stages iterate up to MVP. Plumbing-only stages stop existing as standalone units — plumbing arrives **inside** a vertical slice the player/agent can run end-to-end.

## Binding methodology — hybrid tracer slice (D1)

> **Stage 1.0 of every master plan = tracer slice.** End-to-end real code, one real player/agent verb, hardcoded data OK, peripheral systems explicitly stubbed. No master plan ships with Stage 1 = pure scaffolding.

Adopted whole-system per `docs/prototype-first-methodology-design.md` §6 D1. Combines vertical-slice (one real verb), tracer-bullet (real code throughout, no stubs in the verb's reachable path), and prototype-pillar discipline (explicit deferrals).

## §Tracer Slice — 5-field contract (D9)

Mandatory `§Tracer Slice` subsection on Stage 1.0 of every master plan. All 5 fields non-empty.

| Field | One-line semantics |
|---|---|
| `verb` | What the player/agent can do at end of Stage 1.0. Free-form, single sentence, non-empty. |
| `hardcoded_scope` | List of hardcoded data / scenes / config admitted as throwaway. |
| `stubbed_systems` | List of stub methods returning constants (per D4 — no `NotImplementedException` / dead-end TODO blockers). |
| `throwaway` | Visible-layer items acceptable for Stage 2+ rewrite (per D7). |
| `forward_living` | Structural-layer items locked forward (API shapes, schemas, signatures). Stages 2+ fatten without redesign. |

## §Visibility Delta — Stages 2+ contract (D9)

Mandatory single-line `§Visibility Delta` on every Stage with id ≥ 2. Free-form prose, non-empty, **unique across stages** within a plan. States "what does the player/agent see/feel that they didn't before this stage?"

## Stage 1.0 plumbing-only ban + visibility-ordered fattening (D8)

Phase 4 Stage-ordering heuristic (binding rewrite of `master-plan-new` skill):

1. **Stage 1.0 — Tracer slice (mandatory).** End-to-end real code, one real player/agent verb, hardcoded data OK, peripheral systems explicitly stubbed (per D4). Throwaway/forward-living split declared (per D7).
2. **Stages 2+ — Visibility-ordered fattening.** Each subsequent stage adds the next slice the player/agent will see/feel soonest. Replace stubs with real behavior, prioritized by player visibility. Hidden plumbing (perf, refactor, infra hardening) lands inside the visible slice that needs it, not as standalone plumbing-only stages.
3. **Late stages — Production hardening + polish.** Save/load completeness, multi-config support, edge cases, post-MVP extensions. Land only after every visible slice is real.

Plumbing-only stages forbidden post Stage 1.0 — every Stage must declare its player-visible delta or be merged into one that does.

## Throwaway vs forward-living split (D7)

Stage 1.0 visible layer (scenes, hardcoded prefab placement, stub UI panels, hardcoded number tables) = **throwaway-acceptable**. Stage 1.0 structural layer (manager class shapes, method signatures, data structs, MCP tool surfaces, save schema fields) = **forward-living** — Stages 2+ fatten/extend, do not redesign. §Tracer Slice block makes the split explicit via `throwaway:` + `forward_living:` fields.

## Mechanical mapping from `design-explore` (D10)

`/design-explore` output ships two new mandatory sections:

- `§Core Prototype` — minimum-scope playable/runnable core. Maps 1:1 to Stage 1.0 §Tracer Slice block (5 fields).
- `§Iteration Roadmap` — ordered list of incremental improvements. Each iteration = one player-visible delta. Maps 1:1 to Stages 2+ §Visibility Delta lines.

`/master-plan-new` reads exploration → §Core Prototype seeds Stage 1.0 §Tracer Slice fields → §Iteration Roadmap seeds Stages 2+ titles + §Visibility Delta lines. **Authoring becomes mechanical mapping, not invention.**

## Validator gate

`npm run validate:plan-prototype-first` — CI red blocks merge on missing/empty §Tracer Slice 5-field block (Stage 1.0) or missing/empty/non-unique §Visibility Delta (Stages 2+). Shipped Stage 1.3.

## Cross-links

- `docs/MASTER-PLAN-STRUCTURE.md` — schema sections for Stage 1.0 §Tracer Slice + Stage 2+ §Visibility Delta (Stage 1.2 ship surfaces).
- `docs/prototype-first-methodology-design.md` §6 — verbatim D1/D7/D8/D9/D10 source decisions.
- `ia/rules/agent-principles.md` — "Spec authoring + validators" section links here.
- DEC-A22 — `arch_surface_resolve({surface_id: "rules/prototype-first-methodology"})` returns this file.
