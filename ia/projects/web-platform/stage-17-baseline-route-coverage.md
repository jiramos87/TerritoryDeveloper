### Stage 17 — Playwright E2E harness / Baseline route coverage


**Status:** Done (closed 2026-04-17 — TECH-277 archived)

**Objectives:** Author e2e tests for all existing public surfaces. Validates that routes return 200, key content landmarks are present, `robots.txt` disallows `/dashboard`, sitemap enumerates slugs, RSS `Content-Type` correct. No auth-gated routes at this stage.

**Exit:**
- `npm run test:e2e` green against `localhost:4000` (dev server) + headless Chromium.
- Tests cover: landing, `/about`, `/install`, `/history`, `/wiki`, `/devlog` (list + at least one slug), `robots.txt` body, `/sitemap.xml` slug presence, `/feed.xml` Content-Type.

- [x] Phase 1 — Both specs authored + e2e green (TECH-277).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T17.1 | **TECH-277** | Done (archived) | Author `web/tests/routes.spec.ts` — assert HTTP 200 + at least one visible heading for: `/`, `/about`, `/install`, `/history`, `/wiki`, `/devlog`; assert first devlog slug link navigates to a 200 page. |
| T17.2 | **TECH-277** | Done (archived) | Author `web/tests/meta.spec.ts` — assert `robots.txt` body contains `Disallow: /dashboard`; assert `/sitemap.xml` contains at least one devlog URL; assert `GET /feed.xml` response `Content-Type` header matches `application/rss+xml`. |

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
