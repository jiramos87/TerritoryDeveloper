### Stage 38 — Catalog composite authoring / Docs + nav polish + E2E


**Status:** Draft (tasks _pending_ — not yet filed; opens only when Stages 34..37 Final)

**Objectives:** Document the admin composite-authoring surface, finalize the collapsible `Admin` sidebar group with all Step-10 routes, extend the Playwright suite to cover cross-stage flows (create pool → add panel → publish snapshot → observe reload), and run the full validate gate.

**Exit:**

- `web/README.md` has `## Catalog composite authoring` section: route list (pools / types / panels / buttons / prefabs / snapshot), auth expectations, consumer contract vs `/api/catalog/*`, callout to `docs/asset-snapshot-mvp-exploration.md` §9 for Postgres schema.
- `CLAUDE.md §6` route table extended with all `/admin/catalog/**` rows from Stages 34..37 (pools tree, types diagnostic, panels, buttons, prefabs, snapshot).
- Sidebar groups the full Step-10 admin routes under the collapsible `Admin` section alongside Stage-30..33 routes; ordering: Assets · Pools · Types · Panels · Buttons · Prefabs · Snapshot; group collapse state persists via `localStorage` (matching existing sidebar-group behavior).
- Cross-stage Playwright spec: full happy-path flow (seed → create pool → create panel referencing pool → create button under panel → snapshot diff shows 3 changes → publish → reload banner fires in second tab).
- `npm run validate:web` + `npm run validate:e2e` green on preview deploy.
- Phase 1 — Docs + CLAUDE.md route table extension.
- Phase 2 — Sidebar grouping + collapse persistence.
- Phase 3 — Cross-stage e2e + final validate gate.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T38.1 | _pending_ | _pending_ | Update `web/README.md` — add `## Catalog composite authoring` section: route list with one-line intent per route (pools tree · types diagnostic · panels · buttons · prefabs · snapshot); auth expectations (matcher `/admin/:path*`); consumer contract summary vs `/api/catalog/*` + MCP `catalog_*` parity note; callout to `docs/asset-snapshot-mvp-exploration.md` §9 for Postgres schema + §7.5 locked decisions L1..L15. |
| T38.2 | _pending_ | _pending_ | Update `CLAUDE.md §6` web workspace route table — add rows for `/admin/catalog/pools`, `/admin/catalog/types`, `/admin/catalog/panels`, `/admin/catalog/buttons`, `/admin/catalog/prefabs`, `/admin/catalog/snapshot`; each row cites the Stage that ships it (Stage 34..37). |
| T38.3 | _pending_ | _pending_ | Refactor `web/components/Sidebar.tsx` — group `Admin` routes (from Stages 30..33 + 34..37) under a collapsible section header; ordering: Assets · Pools · Types · Panels · Buttons · Prefabs · Snapshot; group collapse state persists via `localStorage` key `sidebar.admin.collapsed` (matching existing group behavior); ensure Dashboard + Public routes untouched (regression guard). |
| T38.4 | _pending_ | _pending_ | Author cross-stage Playwright spec `web/tests/e2e/admin-catalog-crossflow.spec.ts` — seeds a known-clean snapshot state then: (a) create child pool under a root (b) create panel referencing composite type `panel.weapon` + pool (c) add 2 inline buttons with `button.fire` type (d) open snapshot page → assert 3 pending changes surfaced (e) open diff → assert added entries (f) publish → assert success toast + new label (g) open second tab → assert reload banner fires within 30s. |
| T38.5 | _pending_ | _pending_ | Run `npm run validate:web` + `npm run validate:e2e` from repo root against preview deploy; fix any regressions introduced in Stages 34..37; report final exit codes in PR body + archive task rows via `/closeout`. |

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
