### Stage 16 — Playwright E2E harness / Install + config + CI wiring


**Status:** Done (closed 2026-04-17 — TECH-276 archived)

**Objectives:** Install `@playwright/test`; author `web/playwright.config.ts` (baseURL from env, headless Chromium, 1 worker in CI); add `test:e2e` + `test:e2e:ci` scripts to `web/package.json`; wire into root `validate:all` (opt-in flag or separate `validate:e2e` target to avoid mandatory browser install in non-e2e CI contexts); document env var contract in `web/README.md`.

**Exit:**
- `cd web && npm run test:e2e` runs (even with 0 test files) without error.
- Root `npm run validate:e2e` composes `web/` e2e run; existing `validate:all` unchanged (no forced browser install).
- `web/README.md` §E2E section present.

- [x] Phase 1 — Install + config + scripts + README §E2E (TECH-276).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T16.1 | **TECH-276** | Done (archived) | Install `@playwright/test` + author `web/playwright.config.ts` (baseURL from env, headless Chromium, `testDir: './tests'`, `outputDir: './playwright-report'`); stub `web/tests/.gitkeep`; add `test:e2e` + `test:e2e:ci` scripts to `web/package.json`; add `validate:e2e` to root `package.json`; add `web/playwright-report/` to `.gitignore`; author `web/README.md` §E2E (local run, `PLAYWRIGHT_BASE_URL` contract, Vercel preview injection, CI bootstrap `npx playwright install --with-deps chromium`, per-route convention). `validate:all` unchanged. |

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
