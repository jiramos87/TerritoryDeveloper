### Stage 13 — Portal foundations (architecture-only at this tier) / Postgres provider + auth library selection


**Status:** Done (TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived 2026-04-16)

**Objectives:** Evaluate and select free-tier Postgres provider (Neon / Supabase free / Vercel Postgres Hobby) against MVP volume; lock auth library decision (Lucia Auth v3 vs. roll-own JWT vs. Auth.js — confirm Q11). Lock both decisions in Decision Log. Scaffold `web/lib/db/client.ts` connection pool wrapper + wire `DATABASE_URL` into Vercel env vars. Document in `web/README.md §Portal`.

**Exit:**

- Free-tier Postgres provider locked in Decision Log: provider name, connection/storage limits, region, rationale vs. alternatives.
- Auth library locked in Decision Log: confirm or update Q11 "roll-own JWT + sessions"; Lucia Auth v3 evaluated as minimal alternative before committing to pure roll-own.
- `web/lib/db/client.ts` (new) exports connection pool via `DATABASE_URL`; lazy-connects (no open at build time).
- `DATABASE_URL` env var wired into Vercel project (production + preview + development environments).
- `web/README.md §Portal` documents provider choice, connection pool pattern, `DATABASE_URL` contract, payment gateway placeholder.
- Phase 1 — Provider + auth library evaluation + Decision Log entries.
- Phase 2 — Connection pool scaffold + env wiring + README §Portal.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T13.1 | **TECH-252** | Done (archived) | Evaluate Neon free / Supabase free / Vercel Postgres Hobby — compare connection limits, storage caps, regions, Next.js/Node driver compatibility; lock chosen provider in Decision Log with limits table + rationale vs. alternatives. No code — Decision Log entry only. |
| T13.2 | **TECH-253** | Done (archived) | Evaluate + lock auth library — compare Lucia Auth v3 (minimal, session-first) / pure roll-own JWT / Auth.js (heavy); confirm or update Q11 "roll-own JWT + sessions" decision; lock in Decision Log with API surface note + rationale. No code — Decision Log entry only. |
| T13.3 | **TECH-254** | Done (archived) | Install chosen Postgres driver into `web/package.json`; author `web/lib/db/client.ts` (new) — exports `db` or `sql` connection pool via `DATABASE_URL` (lazy-connect, no open at build time); wire `DATABASE_URL` into Vercel project env vars (production + preview + development) via Vercel dashboard or `vercel env add`. |
| T13.4 | **TECH-255** | Done (archived) | Extend `web/README.md` with `§Portal` section — documents provider choice, connection pool pattern, `DATABASE_URL` env contract, payment gateway architecture placeholder (no provider chosen), and "Step 5 is architecture-only — no migrations run" boundary note; `validate:all` green. |

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
