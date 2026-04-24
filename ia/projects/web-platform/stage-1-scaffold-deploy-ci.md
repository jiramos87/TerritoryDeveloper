### Stage 1 — Scaffold + design system foundation / Scaffold + deploy + CI


**Status:** Final — TECH-136 done (archived); Vercel project linked (`territory-developer-web`), production deploy confirmed 2026-04-15 (`https://web-nine-wheat-35.vercel.app`); validate:all green. Stage closed 2026-04-15.

**Objectives:** Land the `web/` Next.js workspace inside the monorepo, wire Vercel deploy on push, and integrate the new workspace into `npm run validate:all` so lint / typecheck / build regressions trip CI. Document the new surface in `web/README.md` + `CLAUDE.md` + `AGENTS.md` so future agents discover it cold.

**Exit:**

- `web/` exists with Next.js 14+ App Router scaffold (`app/`, `components/`, `lib/`, `content/` subdirs stubbed), TypeScript strict, Tailwind configured.
- Root `package.json` workspaces array includes `"web"` alongside `"tools/*"`; root `npm install` succeeds.
- Vercel project linked; `main` push triggers production deploy; deploy URL reachable.
- `npm run validate:all` chain (see `package.json` scripts) runs `web/` lint + typecheck + build; green on a throwaway PR.
- `web/README.md` documents `cd web && npm run dev`, content conventions, and caveman-exception for public copy.
- `CLAUDE.md` + `AGENTS.md` each gain a `§Web` section pointing at `web/` and the new dev commands.
- Phase 1 — Workspace bootstrap (root workspaces + Next.js scaffold).
- Phase 2 — Deploy + CI integration (Vercel link + `validate:all` entry).
- Phase 3 — Documentation (web README + repo-level docs append).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T1.1 | **TECH-136** | Done (archived) | Scaffold `web/` Next.js 14+ workspace — root workspaces entry, App Router scaffold w/ TS strict + Tailwind + ESLint, Vercel deploy link (prod branch `main`, capture `*.vercel.app` URL), extend root `validate:all` CI chain, author `web/README.md`, append `§Web` to `CLAUDE.md` + `AGENTS.md`. Supersedes T1.1.1..T1.1.6 (TECH-129..TECH-134) — stage compress 2026-04-14. |

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
