### Stage 33 — Catalog admin CRUD views / Docs + nav polish + E2E


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Document the admin surface, add admin-section parent in Sidebar (collapsible group), run full Playwright suite against the admin flows, final validate gate.

**Exit:**

- `web/README.md` has `## Catalog admin` section: route list, auth expectations, consumer contract vs `/api/catalog/*`.
- `CLAUDE.md §6` route table extended with `/admin/catalog/**` rows.
- Sidebar groups admin routes under a collapsible `Admin` section; Dashboard routes untouched (regression guard).
- Full Playwright e2e suite green headless on preview deploy (route coverage + filter + form + 409 conflict + retire + pool flows).
- `npm run validate:web` + `npm run validate:e2e` green.
- Phase 1 — Docs + Sidebar grouping.
- Phase 2 — E2E suite consolidation + final validate gate.

**Tasks:** _pending_ — materialize via `/stage-decompose` when Step 9 opens.

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
