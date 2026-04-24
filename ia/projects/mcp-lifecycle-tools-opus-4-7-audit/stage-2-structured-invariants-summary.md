### Stage 2 — Quick Wins / Structured Invariants Summary


**Status:** Final (2026-04-18)

**Backlog state (Stage 1.2):** 4 filed, all Done (archived) — TECH-371 / TECH-372 / TECH-373 / TECH-374

**Objectives:** Extend `invariants-summary.ts` to return a structured per-invariant array with `subsystem_tags` and an optional `domain` filter. Author `invariants-tags.json` sidecar mapping each invariant number to its subsystem tags. Ship as `v0.6.0`.

**Exit:**

- `invariants_summary({ domain: "roads" })` returns only road-tagged invariants in structured form.
- `invariants_summary({})` returns all 13 invariants structured + `markdown` side-channel.
- `tools/mcp-ia-server/data/invariants-tags.json` committed with all 13 invariants + guardrail tags.
- `tools/mcp-ia-server/package.json` at `0.6.0`; `CHANGELOG.md` entry present.
- Tests green.
- Phase 1 — Sidecar + handler extension.
- Phase 2 — Tests + release prep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Invariants-tags sidecar | **TECH-371** | Done (archived) | Author `tools/mcp-ia-server/data/invariants-tags.json` — array of `{ number: N, subsystem_tags: string[] }` for all 13 invariants + Guardrails rows (derive tags from `ia/rules/invariants.md` prose: HeightMap/Cell/roads/water/cliff/urbanization mentions). |
| T2.2 | Structured invariants handler | **TECH-372** | Done (archived) | Extend `tools/mcp-ia-server/src/tools/invariants-summary.ts` to load `invariants-tags.json`; accept `domain?: string` filter param (substring match against `subsystem_tags`); return `{ invariants: [{number, title, body, subsystem_tags, code_touches}], markdown?: string }`. `markdown` preserves existing prose for agents that still prefer text rendering. |
| T2.3 | Invariants tests | **TECH-373** | Done (archived) | Unit tests in `tools/mcp-ia-server/tests/tools/invariants-summary.test.ts`: `domain` filter match; `domain` matches nothing → `{ invariants: [], markdown: "" }` (not error); no `domain` → all 13 returned; `markdown` side-channel populated regardless of filter. |
| T2.4 | Release prep v0.6.0 | **TECH-374** | Done (archived) | Bump `tools/mcp-ia-server/package.json` `version` to `0.6.0`; add `CHANGELOG.md` entry: `v0.6.0 — Quick wins: glossary bulk-terms + structured invariants`. Advisory note: "tag this commit `mcp-pre-envelope-v0.5.0` for P2 rollback target". |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
