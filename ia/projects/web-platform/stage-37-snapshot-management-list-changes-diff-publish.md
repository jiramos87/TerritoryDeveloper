### Stage 37 — Catalog composite authoring / Snapshot management (list changes · diff · publish)


**Status:** Draft (tasks _pending_ — not yet filed; opens only when Stage 36 Final + grid-asset-visual-registry Step 6 Stage 6.3 Final — publish + diff + reload broadcast shipped)

**Objectives:** Ship the snapshot-operations surface at `/admin/catalog/snapshot` that visualizes pending changes since last publish, renders a human-readable diff, and exposes the publish action (label + timestamp bump only per L1). Consumes `/api/catalog/snapshot/meta` + `/api/catalog/snapshot/diff` + `/api/catalog/snapshot/publish` endpoints.

**Exit:**

- `web/app/admin/catalog/snapshot/page.tsx` RSC: snapshot-meta header (current label, last-publish timestamp, pending-changes count); `<DataTable>` of pending changes (entity kind · entity id · mutation kind · changed_at · author); empty state "snapshot is clean — no pending changes" when no diff.
- `web/app/admin/catalog/snapshot/diff/page.tsx` RSC: full diff view — per-entity before/after panels (JSON tree diff with added/removed/modified highlighting); download-as-JSON action for debugging.
- Publish modal: confirm dialog with label input (user-facing release label, default `auto-YYYY-MM-DD-HHmm`); POST `/api/catalog/snapshot/publish` with `updated_at` optimistic-lock (L13); success toast + page refresh; 409 conflict UI if another publisher raced.
- Reload-broadcast banner: when `/api/catalog/snapshot/meta` returns a newer `updated_at` than the RSC-rendered one (polled via Client-side SSE or 30s poll), banner appears "snapshot published by {author} — reload".
- Sidebar `LINKS` entry added: `Snapshot` under Admin group.
- `npm run validate:web` green; Playwright spec covers pending-changes list + diff view + publish happy path + 409 race.
- Phase 1 — Snapshot-meta RSC + pending-changes table + sidebar link.
- Phase 2 — Diff view RSC + JSON tree diff component.
- Phase 3 — Publish modal + reload broadcast + playwright.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T37.1 | _pending_ | _pending_ | Author `web/app/admin/catalog/snapshot/page.tsx` RSC — fetch `GET /api/catalog/snapshot/meta` (current label, timestamps, pending-changes count) + `GET /api/catalog/snapshot/changes` (paginated list of pending mutations); `<DataTable>` rendering: entity kind (asset/pool/panel/button/prefab) · entity id/slug · mutation (create/update/retire) · changed_at · author; empty state "snapshot clean — no pending changes since {lastPublishLabel}"; `<Rack>` + `<Breadcrumb>` Dashboard › Admin › Catalog › Snapshot. Add sidebar link. |
| T37.2 | _pending_ | _pending_ | Author `web/app/admin/catalog/snapshot/diff/page.tsx` RSC + `web/components/catalog/JsonTreeDiff.tsx` Client component — RSC fetches `GET /api/catalog/snapshot/diff` (returns before/after trees per entity); `<JsonTreeDiff>` renders collapsible per-entity panels with line-level added (green) / removed (red) / modified (yellow) highlighting; download-as-JSON action exports the raw diff. |
| T37.3 | _pending_ | _pending_ | Author `web/components/catalog/PublishModal.tsx` Client component — confirm dialog with label input (placeholder `auto-2026-04-22-1530`; default filled on open); POST `/api/catalog/snapshot/publish` with `{ label, updated_at }` (L13 optimistic-lock); success toast + `router.refresh()`; 409 conflict → render conflict UI with "another publish raced ({otherAuthor} at {timestamp}) — reload before retrying". Wire into Stage 37 snapshot page header. |
| T37.4 | _pending_ | _pending_ | Author `web/components/catalog/SnapshotReloadBanner.tsx` Client component — polls `GET /api/catalog/snapshot/meta` every 30s (or subscribes to SSE `/api/catalog/snapshot/events` if Stage 6.3 ships the reload-broadcast channel); when observed `updated_at` exceeds page-rendered `updated_at` → renders sticky banner "Snapshot published by {author} — reload". Mounted in the admin-catalog layout so it appears on every `/admin/catalog/*` route. |
| T37.5 | _pending_ | _pending_ | Author Playwright spec `web/tests/e2e/admin-catalog-snapshot.spec.ts` — covers: (a) pending-changes list renders after a catalog edit in an earlier step (b) diff view highlights added/removed/modified (c) publish flow: confirm modal → success toast → meta refreshed with new label (d) 409 race: publisher B publishes while publisher A has stale `updated_at` → 409 surfaced in modal (e) reload banner appears in second tab after publish. `npm run validate:web` + `npm run validate:e2e` green. |

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
