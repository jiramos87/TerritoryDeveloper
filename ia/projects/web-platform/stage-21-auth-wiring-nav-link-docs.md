### Stage 21 — Release-scoped progress view / Auth wiring, nav link + docs


**Status:** Final — TECH-358..TECH-361 archived 2026-04-18

**Backlog state (2026-04-18):** 4 / 4 tasks filed; all closed.

**Objectives:** Widen `web/proxy.ts` matcher to cover `/dashboard/:path*`; add "Releases" nav link to `Sidebar.tsx`; update route docs in `web/README.md` + `CLAUDE.md §6`. Final green gate for Step 7.

**Exit:**

- `web/proxy.ts` matcher: `['/dashboard', '/dashboard/:path*']`; both entries present (B2 guard); `/api/*` unaffected; unauthenticated request to `/dashboard/releases/**` → 302 to `/auth/login`.
- `web/components/Sidebar.tsx` `LINKS` array: `{ href: '/dashboard/releases', label: 'Releases', Icon: Layers3 }` after Dashboard entry.
- `web/README.md` route-list rows added for `/dashboard/releases` + `/dashboard/releases/:releaseId/progress`.
- `CLAUDE.md §6` route table row added.
- `npm run validate:web` green; bare `/dashboard` without session cookie still → 302 to `/auth/login` (regression guard).
- Phase 1 — Auth matcher + nav link (`proxy.ts` + `Sidebar.tsx`).
- Phase 2 — Docs + validation (`web/README.md` + `CLAUDE.md §6` + `validate:web`).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T21.1 | **TECH-358** | Done (archived) | Edit `web/proxy.ts` — update `matcher` config to `['/dashboard', '/dashboard/:path*']`; both entries required (B2: single `:path*` breaks bare `/dashboard`); confirm no `/api/dashboard` path inadvertently gated; add reserved comment: `// /dashboard/releases/:releaseId/rollout — reserved; no filesystem stub`. |
| T21.2 | **TECH-359** | Done (archived) | Edit `web/components/Sidebar.tsx` — append `{ href: '/dashboard/releases', label: 'Releases', Icon: Layers3 }` to `LINKS` array after Dashboard entry; add `import { Layers3 } from 'lucide-react'` (or `ListTree` per S4 — pick by visual fit at implementation time); confirm mobile-collapsed behavior unaffected; `npm run validate:web` green. |
| T21.3 | **TECH-360** | Done (archived) | Update `web/README.md` — add route-list rows for `/dashboard/releases` (Release picker, auth-gated, RSC) + `/dashboard/releases/:releaseId/progress` (Release progress tree, auth-gated, RSC + `PlanTree` Client island); note auth gate inherits from Stage 7.3 proxy matcher widen. |
| T21.4 | **TECH-361** | Done (archived) | Update `CLAUDE.md §6` route table — add rows for `/dashboard/releases` + `/dashboard/releases/:releaseId/progress`; run `npm run validate:web` (lint + typecheck + build); confirm exit 0; confirm `DASHBOARD_AUTH_SKIP=1` dev bypass still functions (no regression on Stage 5.3 bypass knob). |

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
