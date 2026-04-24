### Stage 18 — Playwright E2E harness / Dashboard e2e (SSR filter flows)


**Status:** Done (closed 2026-04-17 — TECH-284 archived)

**Objectives:** Author e2e tests for the dashboard's SSR query-param filter chip flows. Validates the full round-trip: URL param → server render → active chip state → filtered task rows → clear-filters reset. Covers combinations and empty-state.

**Exit:**
- Dashboard filter chip tests green headless; `?plan=` / `?status=` / `?phase=` each produce active chip + filtered rows; multi-param combination narrows correctly; clear-filters `<a>` resets to unfiltered state; unrecognised param value renders empty-state message.
- Phase 1 — Full dashboard filter spec (single-param + multi-param + clear-filters + empty-state).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T18.1 | **TECH-284** | Done (archived) | Author `web/tests/dashboard-filters.spec.ts` — (a) for each of `plan`, `status`, `phase` params: navigate to `/dashboard?{param}={value}` w/ known value from unfiltered render; assert chip w/ matching label has active visual state (class or aria); assert visible row count < unfiltered. (b) multi-param (`?status=Done&phase=1`): assert rows satisfy both filters. (c) clear-filters: assert `<a href="/dashboard">` present when any param active; following it returns unfiltered row count. (d) unknown-value (`?status=nonexistent`): assert empty-state message text present. |

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
