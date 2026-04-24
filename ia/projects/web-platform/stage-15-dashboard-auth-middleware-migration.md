### Stage 15 — Portal foundations (architecture-only at this tier) / Dashboard auth middleware migration


**Status:** Done — Stage 5.3 closed 2026-04-17. Phase 0 (TECH-269), Phase 1 (TECH-265 + TECH-266), Phase 2 (TECH-267 + TECH-268) all archived. Next.js 16 migration note: `web/middleware.ts` → `web/proxy.ts` (rename surfaced during TECH-268 smoke; see Issues Found).

**Objectives:** Replace obscure-URL gate on `/dashboard` with Next.js Middleware auth check. Unauthenticated requests → redirect to stub `/auth/login`. Author stub login page (full-English UI, caveman-exception). Remove "internal" banner from dashboard. Update `robots.ts`.

**Exit:**

- `web/middleware.ts` (new) — matcher `['/dashboard']`; reads session cookie; absent/invalid → `NextResponse.redirect(new URL('/auth/login', request.url))`; present → `NextResponse.next()`.
- `web/app/auth/login/page.tsx` (new) — stub RSC; full-English copy (caveman-exception): "Sign in" heading, email + password placeholder inputs, disabled submit, canned banner "Authentication not yet available — coming soon."; design token classes (no inline hex).
- `web/app/robots.ts` updated — `/dashboard` removed from disallow; `/auth` added to disallow.
- "Internal" banner removed from `web/app/dashboard/page.tsx`; manual smoke: `/dashboard` without session cookie → 302 to `/auth/login`.
- `validate:all` green.
- Phase 0 — Dev-bypass env scaffolding (prerequisite to Phase 1 middleware gate).
- Phase 1 — Middleware + stub login page.
- Phase 2 — robots.ts update + banner removal + smoke.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T15.0 | **TECH-269** | Done (archived) | Prerequisite to **TECH-265**. Create `web/.env.local` (gitignored) containing `DASHBOARD_AUTH_SKIP=1` + `web/.env.local.example` (committed) w/ comment documenting bypass knob + prod-warning. Amend `web/README.md` — new `## Local development auth bypass` section. Amend TECH-265 spec (archived) §2.1 Goals + §5.3 algorithm notes — middleware reads `process.env.DASHBOARD_AUTH_SKIP` before cookie; `=== '1'` → `NextResponse.next()` immediately. Ensures local devs not locked out of `/dashboard` once cookie gate lands. Vercel env vars MUST NOT set `DASHBOARD_AUTH_SKIP` — prod stays gated. |
| T15.1 | **TECH-265** | Done (archived) | Author `web/middleware.ts` (new) — `config = { matcher: ['/dashboard'] }`; reads session cookie by name from `request.cookies.get(SESSION_COOKIE_NAME)`; if missing/empty → `NextResponse.redirect(new URL('/auth/login', request.url))`; if present → `NextResponse.next()`. Cookie name constant matches auth library decision from Stage 5.1. No DB lookup at this tier. Middleware now also short-circuits on `process.env.DASHBOARD_AUTH_SKIP === '1'` (**TECH-269** bypass knob) — local dev only. |
| T15.2 | **TECH-266** | Done (archived) | Author `web/app/auth/login/page.tsx` (new) — RSC stub login page; full-English user-facing copy (caveman-exception): "Sign in" heading, email + password `<input>` placeholders, disabled `<button>` submit, canned error `<p>` "Authentication not yet available — coming soon."; consumes design token classes (`bg-canvas`, `text-primary`, etc. — no inline hex). |
| T15.3 | **TECH-267** | Done (archived) | Update `web/app/robots.ts` — remove `/dashboard` from disallow array; add `/auth` to disallow (login page not publicly indexed); confirm `/auth/login` absent from `web/app/sitemap.ts`; `validate:all` green. |
| T15.4 | **TECH-268** | Done (archived) | Remove "Internal" banner `<p>` from `web/app/dashboard/page.tsx`; smoke note: `localhost:4000/dashboard` without session cookie → middleware should 302 to `/auth/login`; confirm middleware matcher fires in Next.js dev server; `validate:all` green. |

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
