### Stage 1 — Quick Wins / Glossary Bulk-Terms Extension


**Status:** Done (2026-04-18)

**Objectives:** Extend `glossary-lookup.ts` to accept a `terms: string[]` array alongside the existing `term: string` param; return per-term `{ results, errors }` partial-result shape. Back-compat: single `term` param still works unchanged.

**Exit:**

- `glossary_lookup({ terms: ["HeightMap", "wet run", "nonexistent"] })` returns `ok: true`, `payload.results` for found terms, `payload.errors` for not-found, `meta.partial` counts.
- `glossary_lookup({ term: "HeightMap" })` (single term) still returns existing shape unwrapped.
- Tests green; `npm run validate:all` passes.
- Phase 1 — Bulk-terms handler + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Bulk terms handler | **TECH-314** | Done | Extend `tools/mcp-ia-server/src/tools/glossary-lookup.ts` to accept `terms?: string[]` alongside `term?: string`. When `terms` present, fan out to per-term lookup, aggregate into `{ results: {[term]: GlossaryEntry}, errors: {[term]: { code, message }} }` + `meta.partial: { succeeded, failed }`. Single-`term` path returns existing shape via backward-compat branch. |
| T1.2 | Bulk terms tests | **TECH-315** | Done | Unit tests in `tools/mcp-ia-server/tests/tools/glossary-lookup.test.ts`: bulk happy path (all found), partial failure (one term not found → in `errors`, rest in `results`), single-`term` back-compat, empty `terms: []` → `{ results: {}, errors: {}, meta.partial: {succeeded:0,failed:0} }`. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
