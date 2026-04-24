### Stage 14 — Portal foundations (architecture-only at this tier) / Auth API stubs + schema draft


**Status:** Done — TECH-261 + TECH-262 + TECH-263 + TECH-264 all archived 2026-04-17.

**Objectives:** Draft `web/lib/db/schema.ts` covering `user`, `session`, `save`, `entitlement` tables using drizzle-kit (preferred). Install + configure migration tooling; confirm `db:generate` script works. Author stub `web/app/api/auth/{login,register,session,logout}/route.ts` handlers returning 501 Not Implemented. No migrations run.

**Exit:**

- `web/lib/db/schema.ts` (new) defines typed drizzle `pgTable` definitions for `user`, `session`, `save`, `entitlement` tables with column types matching auth library data contract from Stage 5.1.
- `drizzle.config.ts` (new) at `web/` root; `web/package.json` has `db:generate` script; `npm run db:generate` produces artifacts in `web/drizzle/`; migrations NOT run.
- `web/app/api/auth/login/route.ts`, `register/route.ts` (new) — `POST` handlers each return `Response.json({ error: 'Not Implemented' }, { status: 501 })`.
- `web/app/api/auth/session/route.ts` (`GET`), `logout/route.ts` (`POST`) (new) — each returns 501; all four routes absent from `web/app/sitemap.ts`.
- `validate:all` green; no TypeScript errors from schema imports.
- Phase 1 — Schema + migration tool setup.
- Phase 2 — Auth API stub handlers.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T14.1 | **TECH-261** | Done (archived) | Install `drizzle-orm` + `drizzle-kit` into `web/package.json`; author `web/lib/db/schema.ts` (new) — drizzle `pgTable` for: `user` (id uuid PK, email text unique, passwordHash text, createdAt timestamp), `session` (id uuid PK, userId uuid FK→user.id, expiresAt timestamp, token text), `save` (id uuid PK, userId uuid FK→user.id, data jsonb, updatedAt timestamp), `entitlement` (id uuid PK, userId uuid FK→user.id, tier text, grantedAt timestamp). |
| T14.2 | **TECH-262** | Done (archived) | Author `web/drizzle.config.ts` (new) — `schema: './lib/db/schema.ts'`, `out: './drizzle/'`, driver from `DATABASE_URL`; add `"db:generate": "drizzle-kit generate"` to `web/package.json` scripts; confirm `npm run db:generate` produces SQL artifacts in `web/drizzle/` without live DB; decide + document whether `web/drizzle/` is gitignored or committed; `validate:all` green. |
| T14.3 | **TECH-263** | Done (archived) | Author `web/app/api/auth/login/route.ts` + `web/app/api/auth/register/route.ts` (new) — each exports `export async function POST(_req: Request)` returning `Response.json({ error: 'Not Implemented' }, { status: 501 })`; TypeScript typed; no DB imports yet. |
| T14.4 | **TECH-264** | Done (archived) | Author `web/app/api/auth/session/route.ts` (`GET`) + `web/app/api/auth/logout/route.ts` (`POST`) (new) — each returns 501 Not Implemented; confirm all four `/api/auth/*` routes absent from `web/app/sitemap.ts` (API routes not enumerated); `validate:all` green. |

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
