# Backlog Archive — Territory Developer

> Completed issues archived from `BACKLOG.md`. A **2026-04-04** batch holds the former **Completed** slice from `BACKLOG.md`; the **Recent archive** block holds items moved on **2026-04-10**. Older completions follow under **Pre-2026-03-22 archive**.

- [x] **TECH-330** — Remediate **critical** + **major** findings from `feature/master-plans-1` self-review (2026-04-17)
  - Type: code health / IA remediation
  - Files: `docs/backlog-yaml-mcp-alignment-exploration.md`, `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md`, `docs/ship-stage-exploration.md`, `docs/web-platform-post-mvp-extensions.md`, `docs/release-rollout-model-audit.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md`, `ia/skills/release-rollout-enumerate/SKILL.md`, `ia/skills/release-rollout-track/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `tools/scripts/materialize-backlog.sh`, `.claude/agents/stage-decompose.md`, `ia/projects/full-game-mvp-rollout-tracker.md`
  - Spec: (removed after closure)
  - Notes: Closed 5 critical (C1–C5) + 10 major (M1–M10) + Q1. C1 lifecycle-tools-audit owns MCP mutation surface, alignment doc = appendix. C2 `stage-file` batch-reserves + forwards `--reserved-id {ID}` to `project-new` (invariant #13 preserved). C3 (f)-signal collapsed — both `release-rollout-enumerate` + `release-rollout-track` use paired-record check. C4 yaml-loader logs stderr + surfaces `parseErrorCount` metadata. C5 closeout lock scope separated — `.closeout.lock` + `in-flight-closeouts.schema.json` + 24h TTL purge. M5 dead post-migration fallbacks swept. M6 `materialize-backlog.sh` flocked. Q1 `validate-backlog-yaml.ts` canonical, `.mjs` deleted. Sibling TECH filed for Sonnet promotion of release-rollout helpers (M9 decision). Decision Log persisted to `ia_project_spec_journal`.
  - Acceptance: criticals + majors landed per §6; Q1 executed; `validate:all` green; lessons migrated (MEMORY.md + invariants guardrail).

- [x] **TECH-323** — Extract shared lint core `backlog-record-schema.ts` (Stage 1.2 Phase 1) (2026-04-17)
  - Type: infrastructure / MCP tooling
  - Files: `tools/mcp-ia-server/src/parser/backlog-record-schema.ts`, `tools/validate-backlog-yaml.ts`
  - Spec: (removed after closure)
  - Notes: Extracted pure `validateBacklogRecord(yamlBody) → { ok, errors, warnings }` + rule-id constants (`E_MISSING_FIELD`, `E_BAD_ID_FORMAT`, `E_BAD_STATUS`, `E_EMPTY_DEPENDS_ON_RAW`). Single canonical yaml parser (`parseYamlScalars`). Validator renamed `.mjs` → `.ts` + run via `npx tsx` (zero-config — `validate:backlog-yaml` runs before `compute-lib:build` in `validate:all`). Unblocks TECH-324.
  - Acceptance: shared core pure; validator delegates; lint byte-stable vs baseline; `npm run validate:all` green.
  - Related: TECH-324, TECH-325

- [x] **TECH-301** — Round-trip soft-dep marker integration test (Stage 1.1 Phase 3) (2026-04-17)
  - Type: test / regression
  - Files: `tools/mcp-ia-server/tests/tools/backlog-issue.test.ts`
  - Spec: (removed at closeout — test-regression issue; no Lessons Learned; Decision Log in git history)
  - Notes: Integration test at MCP-tool layer — tmp-root yaml fixtures (`soft: FEAT-12` + plain), asserts `soft_only` classifies correctly. `[optional]` deferred. Regression guard for TECH-297. `npm run validate:all` green.
  - Acceptance: Two tests cover soft + plain; guards lossy `array.join` revert; validate green.
  - Depends on: TECH-297

- [x] **TECH-300** — Surface new fields in `backlog_issue` + `backlog_search` payloads (Stage 1.1 Phase 3) (2026-04-17)
  - Type: infrastructure / MCP tooling
  - Files: `tools/mcp-ia-server/src/tools/backlog-issue.ts`, `tools/mcp-ia-server/src/tools/backlog-search.ts`, `tools/mcp-ia-server/tests/tools/backlog-issue.test.ts`, `tools/mcp-ia-server/tests/tools/backlog-search.test.ts`
  - Spec: (removed at closeout — tooling-only issue; Decision Log in git history; no Lessons Learned)
  - Notes: Extended `backlog_search` results projection w/ `priority`/`related`/`created`; `backlog_issue` already spread fields via `...parsed`. New `backlog-issue.test.ts` + extended `backlog-search.test.ts` + `build-registry.test.ts` snapshot. `npm run validate:all` green.
  - Acceptance: Payloads expose three new fields; snapshot tests updated; `npm run validate:all` green.
  - Depends on: TECH-295, TECH-296

- [x] **TECH-299** — Execute `proposed_solution` decision (Stage 1.1 Phase 2) (2026-04-17)
  - Type: infrastructure / MCP tooling
  - Files: `tools/mcp-ia-server/src/parser/backlog-parser.ts`, `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `tools/scripts/migrate-backlog-to-yaml.mjs`, `tools/validate-backlog-yaml.mjs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; tooling-only issue; full prose in git history only)
  - Notes: Executed Option A (drop) per TECH-298 decision. Removed `proposed_solution?: string` from `ParsedBacklogIssue` + `scrapeIssueFields`, dropped `"proposed solution"` header remap, removed field assignment. No yaml schema / loader / validator / fixture change. Regression test added.
  - Acceptance: `proposed_solution` removed end-to-end; regression test lands; `npm run validate:all` green.
  - Depends on: TECH-298

- [x] **TECH-298** — Grep-audit `proposed_solution` consumers (Stage 1.1 Phase 2) (2026-04-17)
  - Type: audit / decision
  - Files: `tools/mcp-ia-server/src/parser/backlog-parser.ts`, `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; audit-only issue; full prose in git history only)
  - Notes: Audit confirmed 4 producer-only hits in `backlog-parser.ts` (lines 41, 140, 151, 163); zero downstream readers across tools/, ia/backlog*/, tests, migrate script. Decision locked Option A (drop field). Unblocks TECH-299 execution scope = remove 4 parser lines + `"proposed solution"` header remap; no yaml schema change.
  - Acceptance: Consumer list enumerated; decision (Option A) + rationale recorded; TECH-299 unblocked; no code change.
  - Related: TECH-299

- [x] **TECH-297** — Fix `depends_on_raw` soft-marker fallback (Stage 1.1 Phase 1) (2026-04-17)
  - Type: infrastructure / MCP tooling (correctness fix)
  - Files: `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `tools/mcp-ia-server/tests/parser/backlog-yaml-loader.test.ts`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; tooling-only regression fixture + dead-sentinel cleanup; full prose in git history only)
  - Notes: Loader precedence already raw-first — delivery was (a) regression fixtures D/E/F in `backlog-yaml-loader.test.ts` locking soft-marker survival (`"FEAT-12 (soft)"` preserved verbatim), fallback synthesis when raw absent (lossy, documented), empty-raw string falls back to array; (b) dropped dead `rec.depends_on_raw !== '""'` sentinel (unreachable post-`unquote`); (c) chain-through assertion on `resolveDependsOnStatus` classifying `(soft)` kind. Upstream emitter always writes `depends_on_raw` — lossy path only hit on malformed hand-edits. Validator schema enforcement deferred to Stage 2.2 / IP8.
  - Acceptance: Fallback prefers yaml source; soft markers survive loader; fixtures D/E/F green; `npm run validate:all` green.
  - Related: TECH-295, TECH-296, TECH-301

- [x] **TECH-296** — Map new fields in yaml loader (Stage 1.1 Phase 1) (2026-04-17)
  - Type: infrastructure / MCP tooling
  - Files: `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `tools/mcp-ia-server/tests/parser/backlog-yaml-loader.test.ts`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; tooling-local loader mapping; full prose in git history only)
  - Notes: `yamlToIssue` sets `priority`, `related`, `created` from yaml record. Markdown-path callers default to `null` / `[]` when yaml absent. Cover existing fixtures + at least one new fixture with all three fields populated. Depends on TECH-295 type shape.
  - Acceptance: Loader maps all three fields; markdown fallback defaults safe; new fixture covers populated + absent cases; `npm run validate:all` green.
  - Related: TECH-295, TECH-297, TECH-300

- [x] **TECH-295** — Extend `ParsedBacklogIssue` shape (Stage 1.1 Phase 1) (2026-04-17)
  - Type: infrastructure / MCP tooling
  - Files: `tools/mcp-ia-server/src/parser/backlog-parser.ts`, `tools/mcp-ia-server/src/parser/types.ts`
  - Spec: (removed at closeout — tooling-only type extension; Decision Log in git history only)
  - Notes: Add `priority: string | null`, `related: string[]`, `created: string | null` to `ParsedBacklogIssue`. Update dependent type exports. No behavior change — loader mapping lands in TECH-296. Stage-shared context: IP1 field extension + soft-dep marker preservation bug fix; MCP tool surface must expose priority/related/created downstream.
  - Acceptance: Type gains three new fields; all dependent exports compile; no loader / MCP payload change yet; `npm run validate:all` green.
  - Related: TECH-296, TECH-297, TECH-300

- [x] **TECH-294** — Web workspace — audit + refactor components to move business logic into backend loaders/parsers (2026-04-17)
  - Type: tech (web / refactor)
  - Files: `web/app/**`, `web/components/**`, `web/lib/**`, `web/README.md`, `ia/rules/web-backend-logic.md`, `ia/projects/web-platform-master-plan.md`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to `ia_project_spec_journal`; canonical rule `ia/rules/web-backend-logic.md` already authoritative; full prose in git history only)
  - Notes: Audit + migration of business logic out of `web/app/**` + `web/components/**` into `web/lib/**` per rule `ia/rules/web-backend-logic.md`. Phase 2 audit surfaced 3 status sets + 4 inline derivations in `dashboard/page.tsx` (`DONE_STATUSES`, `PENDING_STATUSES`, `IN_PROGRESS_STATUSES`, `completedCount`, `statBarLabel`, `chartData`, `stepDone/stepTotal`). Phase 3 moved all hits to `computePlanMetrics()` in `plan-parser.ts`; new types `PlanMetrics`, `StepChartBar`, `StepTaskCounts` in `plan-loader-types.ts`; page destructures pre-computed fields. `new Date()` hits in `feed.xml/route.ts` + `sitemap.ts` classified server-route utility (not a violation). Boundary note landed in `web/README.md`. `npm run validate:web` green.
  - Acceptance: audit report landed (7 hits); each migrated to `web/lib/**` w/ typed return extended; no component embeds status inference / aggregation / markdown re-parsing / business-rule rollups; `npm run validate:web` exit 0; dashboard + feed + wiki visual parity confirmed; boundary note landed in `web/README.md`; rule `ia/rules/web-backend-logic.md` cited.
  - Related: `ia/rules/web-backend-logic.md`; `ia/projects/web-platform-master-plan.md`; `web/lib/plan-parser.ts` (`deriveHierarchyStatus`, `computePlanMetrics`)

- [x] **TECH-289** — Split BACKLOG into per-issue YAML — parallel-safe stage-file / closeout (2026-04-17)
  - Type: tech (infra / agent orchestration concurrency)
  - Files: `BACKLOG.md`, `BACKLOG-ARCHIVE.md`, `ia/backlog/**`, `ia/backlog-archive/**`, `ia/state/id-counter.json`, `tools/scripts/reserve-id.sh`, `tools/scripts/materialize-backlog.sh`, `tools/scripts/migrate-backlog-to-yaml.mjs`, `tools/mcp-ia-server/src/parser/backlog-parser.ts`, `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `tools/validate-backlog-yaml.mjs`, `ia/skills/{project-new,stage-file,project-spec-close,project-stage-close,master-plan-new,master-plan-extend,stage-decompose,release-rollout-enumerate,release-rollout-track,release-rollout}/SKILL.md`, `CLAUDE.md`, `AGENTS.md`, `docs/agent-lifecycle.md`, `ia/rules/invariants.md`, `ia/rules/terminology-consistency.md`, `ia/specs/glossary.md`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to `ia_project_spec_journal`; yaml backlog model + materializer + reserve-id lock documented in `CLAUDE.md` §3, `AGENTS.md`, `docs/agent-lifecycle.md`, `ia/rules/invariants.md`, `ia/rules/terminology-consistency.md`, `ia/specs/glossary.md` (Backlog record / Backlog view rows); tooling lessons in `MEMORY.md`; full prose in git history only)
  - Notes: Shipped per-issue yaml backlog (`ia/backlog/*.yaml` open, `ia/backlog-archive/*.yaml` closed) w/ monotonic id counter `ia/state/id-counter.json` under `flock` via `tools/scripts/reserve-id.sh` (env-var overrides `IA_COUNTER_FILE` / `IA_COUNTER_LOCK` for test isolation). `tools/scripts/materialize-backlog.sh` regenerates `BACKLOG.md` + `BACKLOG-ARCHIVE.md` as read-only **backlog view** artifact (round-trip diff gate). All writer skills (`project-new`, `stage-file`, `project-spec-close`, `project-stage-close`, `master-plan-new`, `master-plan-extend`, `stage-decompose`, `release-rollout-enumerate`, `release-rollout-track`) + MCP readers migrated; public response shapes (`backlog_issue`, `backlog_search`) unchanged. Glossary rows **Backlog record** + **Backlog view** added. Skipped `flock` on read-only validators (no corruption risk). Dashboard `/dashboard` renders via ISR.
  - Acceptance: all writer skills + MCP readers migrated; `npm run validate:all` green; round-trip `migrate → materialize → diff` = empty; reserve-id 8-way concurrency smoke 8 distinct ids; glossary rows + indexes regen; `npm run validate:backlog-yaml` exists + green; public MCP response shapes unchanged.
  - Related: `ia/specs/glossary.md` **Backlog record** / **Backlog view**

- [x] **TECH-284** — Dashboard filter e2e spec (Stage 6.3) (2026-04-17)
  - Type: tooling / e2e tests
  - Files: `web/tests/dashboard-filters.spec.ts` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; full prose in git history only)
  - Notes: Authored `web/tests/dashboard-filters.spec.ts` covering single-param (`plan` / `status` / `phase`), multi-param intersection (`?status={v}&phase={n}`), clear-filters link reset, empty-state (`?status=nonexistent`). Baseline fixture extracts live values from unfiltered `/dashboard` render. Active-chip hook = class token `bg-panel text-primary`. Clear-filters locator = `role=link` named `Clear filters`. Row count = `page.locator('tbody tr').count()` summed across all `DataTable` instances.
  - Acceptance: `cd web && npm run test:e2e` exit 0 headless; `npm run validate:all` green; Stage 6.3 Exit criteria satisfied.
  - Related: **TECH-277** (baseline Playwright harness), `ia/projects/web-platform-master-plan.md` Stage 6.3

- [x] **BUG-17** — `cachedCamera` is null when creating `ChunkCullingSystem` (2026-04-17)
  - Type: fix
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; canonical init-race guard pattern covered in `ia/specs/unity-development-context.md` §6; full prose in git history only)
  - Notes: Promoted `cachedCamera` → `[SerializeField] private Camera cachedCamera;` + added minimal `Awake()` resolving via `Camera.main` fallback so `ChunkCullingSystem` constructor (called from `InitializeGrid`) receives non-null reference. Removed redundant lazy null-checks at `GridManager.cs:366` + `:1294` (Awake now guarantees resolution). `ChunkCullingSystem.UpdateVisibility` self-heal retained as belt-and-braces (non-MonoBehaviour). Compile clean; no visual regression on chunk culling. Matches canonical init-race guard pattern in `unity-development-context §6` + guardrail "IF adding a manager reference → `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`".
  - Acceptance: `cachedCamera` non-null at `InitializeGrid` entry; zero NRE from culling init on New Game / Load; chunk visibility unchanged.
  - Related: **BUG-16** (init ordering sibling), `ia/specs/unity-development-context.md` §6

- [x] **BUG-16** — Possible race condition in GeographyManager vs TimeManager initialization (**geography initialization**) (2026-04-17)
  - Type: fix
  - Files: `Assets/Scripts/Managers/GameManagers/GeographyManager.cs`, `Assets/Scripts/Managers/GameManagers/TimeManager.cs`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to `ia_project_spec_journal`; canonical init-race guard pattern captured in `ia/specs/unity-development-context.md` §6; full prose in git history only)
  - Notes: `GeographyManager.IsInitialized` flips true at tail of `InitializeGeography()` (post desirability/sorting). `TimeManager` caches ref via `[SerializeField]` + `FindObjectOfType` fallback in new `Awake` (invariant #3); daily-tick block (`if timeElapsed >= 1f`) early-returns when flag false — `HandleOnKeyInput` + accumulator stay unguarded so pause/speed UI responsive during load. Bridge `get_console_logs` fresh-scene smoke: 0 NRE tagged `TimeManager`; compile clean. Grep sweep: `GridManager.Update`/`UIManager.Update` already self-guard — fix self-contained. Edit-mode `testmode-batch` skipped (Editor project lock); bridge compile gate covers.
  - Acceptance: `IsInitialized` flips post-pipeline; `TimeManager.Update` tick early-returns pre-init; no fresh-scene NRE; compile clean.
  - Related: **BUG-17** (init ordering sibling), `ia/specs/unity-development-context.md` §6

- [x] **BUG-14** — `FindObjectOfType` in Update/per-frame degrades performance (2026-04-17)
  - Type: fix (performance)
  - Files: `Assets/Scripts/Managers/GameManagers/UIManager.cs`, `Assets/Scripts/Managers/GameManagers/UIManager.Hud.cs`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to `ia_project_spec_journal`; full prose in git history only)
  - Notes: `UIManager.UpdateUI()` caches `EmploymentManager` + `DemandManager` in `Start` via `[SerializeField] private` + null-safe `FindObjectOfType` fallback (sibling pattern to existing `cityStats` / `waterManager` resolution); dead `StatisticsManager` per-frame lookup removed (was assigned, never read). `UpdateGridCoordinatesDebugText` (`LateUpdate` path) drops lazy `FindObjectOfType<GameDebugInfoBuilder>` + `FindObjectOfType<WaterManager>` branches — both fields resolved once in `Start`. Zero per-frame `FindObjectOfType` in `UIManager.Hud.cs` (verified). `CursorManager.Update` already cached (unchanged). Bridge smoke 2026-04-17: compile clean, HUD + debug coord text render, zero console errors. Invariant #3 satisfied. **Prevention:** **TECH-26** CI scanner.
  - Acceptance: zero `FindObjectOfType` matches in `UIManager.Hud.cs`; Unity compile clean; Play Mode HUD smoke clean.
  - Related: **TECH-26**, **TECH-05**

- [x] **BUG-55** — Codebase audit: critical simulation, data integrity, and controller bugs (10 fixes) (2026-04-17)
  - Type: fix (crasher + data corruption + simulation logic + memory leak)
  - Files: `EmploymentManager.cs`, `AutoZoningManager.cs`, `CellData.cs`, `GrowthBudgetManager.cs`, `AutoRoadBuilder.cs`, `DemandManager.cs`, `CityCell.cs`, `RoadStrokeTerrainRules.cs`, `GridPathfinder.cs`, `SimulateGrowthToggle.cs`, `GrowthBudgetSlidersController.cs`, `CityStatsUIController.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons captured in `MEMORY.md`; full prose in git history only)
  - Notes: All 10 audit fixes landed across 6 phases. **Crashers:** EmploymentManager div/0 already guarded (`if totalRatio > 0`); `CityCell` ctor `Enum.Parse` → `TryParse` w/ fallback. **Data corruption:** AutoZoningManager placement-first ordering (`PlaceZoneAt` then `TrySpend`); `CellData.ValidateData()` height floor `Mathf.Max(1,…)` → `Mathf.Max(0,…)`. **Sim logic:** `GrowthBudgetManager.ReturnAvailable` returns `minAvailablePerCategory` (not `Mathf.Min`); `AutoRoadBuilder` adds `InvalidateRoadCache()` + edges/roadSet re-fetch post street-project loop (satisfies invariant #2); `DemandManager` subtracts building counts from zone counts w/ `Mathf.Max(0,…)`. **Terrain / balance:** water height `<= 0` → `< 0` strict in `RoadStrokeTerrainRules` + `GridPathfinder`; `unemploymentResidentialPenalty` 1.5 → 1.2 (symmetric w/ jobBoost). **Memory leaks:** `OnDestroy()` listener cleanup in `SimulateGrowthToggle`, `GrowthBudgetSlidersController`, `CityStatsUIController`. Kickoff caught Fix 7 spec-vs-code path drift (`Cell.cs` → `CityCell.cs`) — lesson archived.
  - Related: **BUG-14**, **TECH-05**, **TECH-16**

- [x] **TECH-277** — Playwright e2e baseline route + meta contract tests (Stage 6.2) (2026-04-17)
  - Type: tooling / e2e tests
  - Files: `web/tests/routes.spec.ts` (new), `web/tests/meta.spec.ts` (new), `web/tests/.gitkeep` (removed), `web/app/robots.ts` (minimal prod fix — added `/dashboard` to disallow)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons captured in `MEMORY.md`; full prose in git history only)
  - Notes: Authored 2 Playwright spec files per Stage 6.2 Exit. `routes.spec.ts` array-driven smoke for `/`, `/about`, `/install`, `/history`, `/wiki`, `/devlog` (200 + visible heading) + first devlog slug nav via `page.waitForURL(/\/devlog\/.+/)`. `meta.spec.ts` robots `Disallow: /dashboard`, sitemap `/devlog/` substring, RSS `application/rss+xml` `Content-Type`. Issues Found §9 surfaced 3 runtime pitfalls (missing `/dashboard` in robots.ts → 1-line prod fix; `npx playwright install chromium` needed; `waitForResponse` unreliable for SPA nav → `waitForURL`). 10/10 tests green against `localhost:4000` headless Chromium.
  - Acceptance: `cd web && npm run test:e2e` exit 0; `npm run validate:all` green; Stage 6.2 Exit criteria satisfied.
  - Depends on: TECH-276 (Done)
  - Related: [`ia/projects/web-platform-master-plan.md`](projects/web-platform-master-plan.md) Stage 6.2

- [x] **TECH-276** — Playwright e2e harness — install + config + scripts + README docs (Stage 6.1) (2026-04-17)
  - Type: tooling / scaffold
  - Files: `web/package.json`, `web/playwright.config.ts` (new), `web/tests/.gitkeep` (new), `web/README.md` (§E2E append), `package.json` (root — `validate:e2e`), `.gitignore`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to `ia_project_spec_journal`; full prose in git history only)
  - Notes: Stage 6.1 single-pass scaffold. Installed `@playwright/test`; authored `web/playwright.config.ts` (baseURL from `PLAYWRIGHT_BASE_URL` env w/ `http://localhost:4000` fallback, headless Chromium, `testDir: './tests'`, `outputDir: './playwright-report'`, `workers: CI ? 1 : undefined`); stubbed `web/tests/.gitkeep`; added `test:e2e` + `test:e2e:ci` scripts (both pass `--pass-with-no-tests` so empty dir does not fail CI before first spec); root `validate:e2e` composes `npm --prefix web run test:e2e:ci` (NOT chained into `validate:all` — browser binary install opt-in). README §E2E documents local run, env contract, Vercel preview injection, CI bootstrap. Merges orig Phase 1+2+3 per 2026-04-17 Decision Log.
  - Acceptance: `cd web && npm run test:e2e` exits 0 w/ empty `tests/`; `npm run validate:all` green; README §E2E present; `validate:e2e` composes web workspace run.

- [x] **BUG-56** — **Blip** **comb-filter** feedback path broken — `BlipFxChainTests.Comb_FeedbackAttenuation` failing (2026-04-17)
  - Type: fix (audio DSP regression)
  - Files: `Assets/Scripts/Audio/Blip/BlipFxChain.cs`, `Assets/Tests/EditMode/Audio/BlipFxChainTests.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Comb kernel wrote dry `x` into delay buffer — degraded to single-tap delay. One-line fix in `BlipFxChain.ProcessFx` comb case: `delayBuf[writePos] = y` (wet write). Invariant `out[2D]/out[D] ≈ g` restored; distinguishes feedback comb from chorus/flanger (wet-from-dry taps) and Schroeder allpass (writes `x + g·v`). Captured in `MEMORY.md`.
  - Depends on: none (Stage 5.2 Done)
  - Related: [`ia/projects/blip-master-plan.md`](projects/blip-master-plan.md) Stage 5.2

- [x] **TECH-264** — `/api/auth/session` GET + `/api/auth/logout` POST stub handlers (501) + sitemap audit (Stage 5.2 Phase 2) (2026-04-17)
  - Type: web platform / API stub
  - Files: `web/app/api/auth/session/route.ts` (new), `web/app/api/auth/logout/route.ts` (new), `web/app/sitemap.ts` (audit only — no edit)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: `session` route exports `GET` (idempotent session probe per REST convention); `logout` route exports `POST` (state-mutating). Both return 501 + `{"error":"Not Implemented"}` JSON body matching sibling login + register stubs. Sitemap audit: `web/app/sitemap.ts` enumerates MDX pages + devlog posts only — all 4 `/api/auth/*` routes absent (Next.js sitemap convention + repo precedent). `Promise<Response>` return annotation matches sibling stubs verbatim.
  - Acceptance: Both routes return HTTP 501 + JSON body; sitemap audit confirms 4 `/api/auth/*` URLs absent; `cd web && npm run typecheck` green; `npm run validate:all` exit 0.
- [x] **TECH-263** — `/api/auth/login` + `/api/auth/register` POST stub handlers (501) (Stage 5.2 Phase 2) (2026-04-17)
  - Type: web platform / API stub
  - Files: `web/app/api/auth/login/route.ts` (new), `web/app/api/auth/register/route.ts` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Each handler exports `export async function POST(_req: Request): Promise<Response>` returning `Response.json({ error: 'Not Implemented' }, { status: 501 })`. TS-typed; zero non-Web imports; no DB / drizzle / auth-lib deps. `Response.json()` (Web standard) over `NextResponse.json()` — no Next-specific cookie / header handling at stub stage. HTTP 501 status semantically signals "endpoint exists but unimplemented" — distinguishes from 404 / 405 for middleware smoke.
  - Acceptance: Both routes return HTTP 501 + `{"error":"Not Implemented"}` JSON; `cd web && npm run typecheck` green; `npm run validate:all` exit 0.
- [x] **TECH-275** — NoAlloc delay-FX test + `Render` overload verification (Stage 5.2 Phase 3) (2026-04-17)
  - Type: audio / test gate
  - Files: `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`, `Assets/Tests/EditMode/Audio/BlipBakerTests.cs` (regression — no edit), `Assets/Tests/EditMode/Audio/BlipDeterminismTests.cs` (regression — no edit)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Stage 5.2 closing gate. New EditMode test `BlipNoAllocTests.Render_WithChorus_ZeroManagedAlloc`: chorus patch (rateHz=1, depthMs=5, mix=0.4); pre-lease 1 delay buf via `BlipDelayPool.Lease(48000, 50f)` OUTSIDE `GC.GetAllocatedBytesForCurrentThread` window; 3 warm-up + 10 measured renders; assert delta/call ≤ 0. Delay-aware `BlipVoice.Render` overload (base 8 + 4 buf + 4 len + 4 ref writePos) + back-compat 8-param shim both compile. `BlipBakerTests` + `BlipDeterminismTests` suites green. Stage 5.2 Exit satisfied.
  - Acceptance: `Render_WithChorus_ZeroManagedAlloc` green (delta/call ≤ 0); delay-aware + base `Render` overloads compile; regression suites green; `npm run unity:compile-check` + `npm run validate:all` exit 0.
- [x] **TECH-268** — Remove "Internal" banner from `/dashboard` + middleware redirect smoke (Stage 5.3 Phase 2) (2026-04-17)
- [x] **TECH-274** — Chorus + flanger kernels in `BlipFxChain.ProcessFx` (Stage 5.2 Phase 3) (2026-04-17)
  - Type: audio / DSP kernel
  - Files: `Assets/Scripts/Audio/Blip/BlipFxChain.cs`, `Assets/Scripts/Audio/Blip/BlipPatch.cs`, `Assets/Scripts/Audio/Blip/BlipVoice.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Replace Stage 5.1 Chorus + Flanger passthrough stubs w/ 2-tap LFO-modulated delay kernels. Chorus rate `p0` Hz, depth `p1` ms, mix `p2`; symmetric taps `(writePos - center ± lfoOffset) mod bufLen`; `x = (1 - p2) * x + p2 * 0.5 * (tap0 + tap1)`. Flanger identical body, depth clamped `[1f, 10f]` ms via `OnValidate`. Per-slot LFO phase reuses `ringModPhase_N` (slot-private — no cross-slot conflict; warn loop dropped per DL). `ProcessFx` signature gained `float p2`; 8 call sites in `BlipVoice.Render` (4 deterministic + 4 live) updated to forward `param2`. Nearest-neighbour taps; linear interp deferred to Stage 5.3+.
  - Acceptance: Chorus + Flanger kernels per spec §5.2; Flanger depth clamp 1..10 ms; `BlipFxChainTests.Chorus_WetMixNonZero` + `Flanger_DepthClampedTo10ms` green; `npm run unity:compile-check` + `npm run validate:all` exit 0.
- [x] **TECH-273** — Allpass filter kernel in `BlipFxChain.ProcessFx` (Stage 5.2 Phase 2) (2026-04-17)
  - Type: audio / DSP kernel
  - Files: `Assets/Scripts/Audio/Blip/BlipFxChain.cs`, `Assets/Tests/EditMode/Audio/BlipFxChainTests.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Replace Stage 5.1 Allpass-case passthrough w/ Schroeder form `float v = delayBuf[(writePos - D + bufLen) % bufLen]; delayBuf[writePos] = x + p1 * v; float y = v - p1 * delayBuf[writePos]; writePos = (writePos + 1) % bufLen; x = y`. No `p1` clamp — Schroeder stable for `|g| < 1` (unlike Comb). EditMode test `BlipFxChainTests.Allpass_FlatMagnitude`: 1024 samples pink noise through allpass, assert `RMS_out ≈ RMS_in ± 15%` (ideal allpass = flat magnitude response; phase-only modification). Zero managed alloc.
  - Acceptance: Allpass kernel present; RMS-flat test passes within ±15%; `npm run unity:compile-check` + `npm run validate:all` exit 0.
  - Depends on: **TECH-271**.

- [x] **TECH-272** — Comb filter kernel in `BlipFxChain.ProcessFx` (Stage 5.2 Phase 2) (2026-04-17)
  - Type: audio / DSP kernel
  - Files: `Assets/Scripts/Audio/Blip/BlipFxChain.cs`, `Assets/Scripts/Audio/Blip/BlipPatch.cs`, `Assets/Tests/EditMode/Audio/BlipFxChainTests.cs` (new or append)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Replace Stage 5.1 Comb-case passthrough stub w/ feedback comb `int D = (int)(p0 / 1000f * sampleRate); float delayed = delayBuf[(writePos - D + bufLen) % bufLen]; float y = x + p1 * delayed; delayBuf[writePos] = x; writePos = (writePos + 1) % bufLen; x = y`. Guard `D >= 1 && D < bufLen` else break (passthrough). `BlipPatch.OnValidate` clamps `p1` (feedback gain) to `[0f, 0.97f]` for Comb FX slots — BIBO stability margin. New EditMode test `BlipFxChainTests.Comb_FeedbackAttenuation`: impulse, 10 ms delay, g=0.5 → second-echo amplitude ≈ 0.5 ± 0.05 relative to first echo. Zero managed alloc; no Unity API.
  - Acceptance: Comb kernel present per §5.2; `p1` clamp in `OnValidate`; impulse-response test green (0.5 ± 0.05); `npm run unity:compile-check` + `npm run validate:all` exit 0.
  - Depends on: **TECH-271**.

- [x] **TECH-271** — `BlipVoice.Render` delay-buffer overload + `BlipBaker` pre-lease (Stage 5.2 Phase 1) (2026-04-17)
  - Type: audio / DSP plumbing
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`, `Assets/Scripts/Audio/Blip/BlipFxChain.cs`, `Assets/Scripts/Audio/Blip/BlipBaker.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: `BlipVoice.Render` gains 11-param overload — existing 7-param signature + `float[]? d0, float[]? d1, float[]? d2, float[]? d3` appended. Existing 7-param delegates w/ all-null (back-compat shim; `BlipNoAllocTests` + `BlipDeterminismTests` untouched callers stay green). `BlipFxChain.ProcessFx` signature extended w/ `float[]? delayBuf, int bufLen, ref int writePos` per-call params; null-guard in every FX case (memoryless kinds ignore). `BlipBaker.BakeOrGet` pre-leases up to 4 buffers from `_catalog._delayPool.Lease(sampleRate, 50f)` (50 ms ceiling covers comb+chorus+flanger range); passes to Render; `finally { pool.Return(buf) }` unconditionally. Delay-line kernel bodies still passthrough — behavior change lands in **TECH-272..274**. Zero managed alloc inside Render.
  - Acceptance: 11-param Render overload present; 7-param shim delegates; `ProcessFx` signature extended w/ null-guards; `BlipBaker.BakeOrGet` lease+return via `finally`; `BlipNoAllocTests` + `BlipDeterminismTests` + `BlipBakerTests` green; `npm run unity:compile-check` + `npm run validate:all` exit 0.
  - Depends on: **TECH-270**.

- [x] **TECH-267** — `web/app/robots.ts` update — remove `/dashboard`, add `/auth` to disallow (Stage 5.3 Phase 2) (2026-04-17)
  - Type: web platform / SEO
  - Files: `web/app/robots.ts`, `web/app/sitemap.ts` (audit only)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Update `disallow` array in `web/app/robots.ts` — drop `/dashboard` (auth middleware from TECH-265 now gates access; SEO crawlers hitting `/dashboard` unauthenticated get 302 → `/auth/login` which is itself disallowed); add `/auth` (covers `/auth/login` + future `/auth/register`). Final disallow: `['/design', '/auth']`. Sitemap audit: `web/app/sitemap.ts` emits only MDX pages + devlog — `/auth/login` + `/dashboard` absent (neither enumerated). No sitemap edit. Decision Log: drop `/dashboard` (middleware provides structural 302 gate; robots duplicate signal); prefix `/auth` not `/auth/login` (post-Step-5 portal `/auth/register` inherits w/o robots churn); no `X-Robots-Tag` header (robots.txt sufficient at stub tier).
  - Acceptance: `web/app/robots.ts` disallow array = `['/design', '/auth']`; `/auth/login` + `/dashboard` absent from `web/app/sitemap.ts` output (audit only); `cd web && npm run typecheck` green; `cd web && npm run build` green; `npm run validate:all` exit 0.
  - Depends on: **TECH-265**, **TECH-266**

- [x] **TECH-266** — `web/app/auth/login/page.tsx` stub login RSC (Stage 5.3 Phase 1) (2026-04-16)
  - Type: web platform / UI stub
  - Files: `web/app/auth/login/page.tsx` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Author `web/app/auth/login/page.tsx` (new) — RSC (no `'use client'`); full-English user-facing copy per caveman-exception (`ia/rules/agent-output-caveman.md` §exceptions — user-facing rendered text under `web/app/**/page.tsx`): "Sign in" `<h1>`, email + password placeholder `<input>` pair, disabled `<button>` submit, canned banner `<p>` "Authentication not yet available — coming soon.". Consumes design token classes (`bg-canvas`, `text-text-primary`, `border-border-subtle`, etc. — NO inline hex, NO raw tailwind colors). No form action — inputs are placeholders only; TECH-263 archived `/api/auth/login` stub returns 501 anyway. No `<Link>` to `/auth/register` at this tier — register UI deferred to post-Step-5 portal plan. Decision Log: RSC over client component (zero interactivity at stub tier; minimal bundle); disabled inputs + submit (honest UX signal matching canned banner; users still see expected form affordance); design-token classes only, no inline hex (Stage 1.2 TECH-116 convention; post-Step-5 portal plan can restyle via token updates w/o page edits).
  - Acceptance: `web/app/auth/login/page.tsx` renders at `http://localhost:4000/auth/login`; heading + canned error banner + disabled submit present; only design-token CSS classes used (grep confirms no inline hex); `cd web && npm run typecheck` green; `cd web && npm run build` green; `npm run validate:all` exit 0.
  - Depends on: none

- [x] **TECH-270** — `BlipDelayPool` + `BlipCatalog` wiring + `BlipVoiceState` write-heads (Stage 5.2 Phase 1) (2026-04-16)
  - Type: audio / infrastructure
  - Files: `Assets/Scripts/Audio/Blip/BlipDelayPool.cs` (new), `Assets/Scripts/Audio/Blip/BlipCatalog.cs`, `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: New `internal sealed class BlipDelayPool` — `float[] Lease(int sampleRate, float maxDelayMs)` sized to `(int)Math.Ceiling(maxDelayMs/1000f*sampleRate)+1` via `ArrayPool<float>.Shared.Rent`; `void Return(float[])` w/ `clearArray: true` (prevents stale-sample leak). `BlipCatalog` gains `private BlipDelayPool _delayPool = new BlipDelayPool()` (plain-ref field-init; invariant #4 compliant — no new singleton). `BlipVoiceState` appends 4 blittable ints `delayWritePos_0..3` (circular write-head per FX slot). Zero kernel logic — foundation for Stage 5.2 Phase 2/3 comb/allpass/chorus/flanger kernels + Render overload. Decision Log: plain `internal sealed class` (not MonoBehaviour) — data-only infra, owner Catalog already MB; `clearArray: true` on Return prevents stale-sample bleed across leases; buffer len `+1` guard avoids wrap-boundary click transient; pool owned by Catalog (not Baker) — single pool survives bake cycles, Catalog `DontDestroyOnLoad` lifetime matches Blip bootstrap.
  - Acceptance: `BlipDelayPool` class present w/ `Lease`/`Return`; `BlipCatalog._delayPool` field initialized; 4 write-head ints added; `BlipVoiceState` stays blittable + `default = 0`; `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: none (Stage 5.1 closed).

- [x] **TECH-265** — `web/middleware.ts` session-cookie gate on `/dashboard` (Stage 5.3 Phase 1) (2026-04-16)
  - Type: web platform / middleware
  - Files: `web/middleware.ts` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Author `web/middleware.ts` (new) — `export const config = { matcher: ['/dashboard'] }`; `export function middleware(request: NextRequest)` reads `request.cookies.get('portal_session')` (Stage 5.1 archived constant — `SESSION_COOKIE_NAME=portal_session`); missing / empty value → `NextResponse.redirect(new URL('/auth/login', request.url))`; present → `NextResponse.next()`. Presence-only check at stub tier; cookie-signature verification deferred to post-Step-5 portal-launch master plan. Edge-runtime compatible (no `@node-rs/argon2`, no `jose` verify). `DASHBOARD_AUTH_SKIP=1` local-dev bypass short-circuits before cookie read. Decision Log: presence-only (no JWT verify — no tokens exist yet); matcher exact `['/dashboard']` (no sub-path wildcard); inline `SESSION_COOKIE_NAME` const (no shared constants file yet at Stage 5.3).
  - Acceptance: `web/middleware.ts` exports `middleware` + `config` w/ matcher `['/dashboard']`; `/dashboard` without `portal_session` cookie → 302 to `/auth/login`; `/dashboard` with any non-empty cookie value → 200 (stub-tier presence-only check); `cd web && npm run typecheck` green; `npm run validate:all` exit 0.
  - Depends on: **TECH-266** (redirect target `/auth/login` must exist to avoid 404 loop).

- [x] **TECH-269** — `web/.env.local` dev bypass — `DASHBOARD_AUTH_SKIP=1` + middleware env-var check (Stage 5.3 Phase 0) (2026-04-16)
  - Type: web platform / dev ergonomics
  - Files: `web/.env.local` (new, gitignored), `web/.env.local.example` (new, committed), `web/README.md`, `ia/projects/TECH-265.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Prerequisite to **TECH-265**. `web/.env.local` holds `DASHBOARD_AUTH_SKIP=1` (local-only); `web/.env.local.example` committed w/ inline prod-warning comment; `web/README.md` gains "Local development auth bypass" section; **TECH-265** §2.1 Goals + §5.3 pseudo-code amended — middleware short-circuits on `process.env.DASHBOARD_AUTH_SKIP === '1'` before cookie check. Vercel env vars never set the knob — prod stays gated. Decision Log: single-knob bypass over per-dev cookie stub; committed `.env.local.example` for discoverability; bypass check BEFORE cookie read; never set on Vercel.
  - Acceptance: `web/.env.local` gitignored + populated; `.env.local.example` committed; README section landed; TECH-265 spec amended; `npm run validate:all` exit 0.
  - Depends on: none (unblocked TECH-265).

- [x] **TECH-260** — `BlipVoice.Render` FX loop + `BlipNoAllocTests` FX variant (Stage 5.1 Phase 2) (2026-04-16)
  - Type: audio / DSP
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`, `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`
  - Spec: (removed at closeout — Decision Log empty per journal parser; full prose in git history only)
  - Notes: Post-envelope FX dispatch in `BlipVoice.Render` — unrolled 4-slot `if (patch.fxSlotCount >= N) BlipFxChain.ProcessFx(ref x, patch.fxN.kind, patch.fxN.param0, patch.fxN.param1, ref state.dcZ1_N, ref state.dcY1_N, ref state.ringModPhase_N, sampleRate)` for N in 1..4. Mirror block wired into both deterministic (lines ~162) and live (lines ~254) per-sample loops. Empty chain (`fxSlotCount == 0`) fast-exits through all four `if` guards — MVP golden fixtures stay bit-exact (no fixture regeneration). `BlipNoAllocTests.Render_WithFxChain_ZeroManagedAlloc` — 2-slot BitCrush+DcBlocker patch; 3 warm-up + 10 measure w/ `GC.GetAllocatedBytesForCurrentThread`; delta/call ≤ 0. Decision Log: POST-envelope PRE-filter placement (matches Stage 5.1 Exit; alt POST-filter rejected — would bypass LP for DcBlocker's HP action); unrolled `if` cascade vs `for` loop (blittable + zero-alloc matches oscillator inline-triplet precedent); mirror into both branches vs shared helper (existing per-sample cores already duplicated — extract outside Stage 5.1 scope); new test lives alongside `Render_SteadyState_ZeroManagedAlloc` (tight isolation on failure). Closes Stage 5.1 T5.1.5 Exit bullet "BlipVoice.Render FX loop + BlipNoAllocTests still green" + Stage 5.1 as a whole.
  - Acceptance: FX loop wired post-envelope in both deterministic + live branches; empty chain passthrough (MVP goldens green); new NoAlloc test green; existing `BlipNoAllocTests` + `BlipGoldenFixtureTests` + `BlipDeterminismTests` still green; `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: **TECH-257** (archived), **TECH-259** (archived).

- [x] **TECH-259** — `BlipFxChain.cs` memoryless cores — BitCrush / RingMod / SoftClip / DcBlocker (Stage 5.1 Phase 2) (2026-04-16)
  - Type: audio / DSP
  - Files: `Assets/Scripts/Audio/Blip/BlipFxChain.cs` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: New `internal static class BlipFxChain` with `static void ProcessFx(ref float x, BlipFxKind kind, float p0, float p1, ref float dcZ1, ref float dcY1, ref float ringPhase, int sampleRate)`. Cores: BitCrush `x = Mathf.Round(x*steps)/steps, steps = 1<<(int)p0`; RingMod `ringPhase += 2π*p0/sampleRate; x *= Mathf.Sin(ringPhase)` w/ guard `sampleRate > 0` + wrap `if (ringPhase > TwoPi) ringPhase -= TwoPi`; SoftClip `x = x/(1f + Mathf.Abs(x))`; DcBlocker `float y = x - dcZ1 + 0.9995f*dcY1; dcZ1 = x; dcY1 = y; x = y`. Comb/Allpass/Chorus/Flanger cases → passthrough stubs (kernels land Stage 5.2). Zero allocs; only `Mathf.Round`/`Mathf.Sin`/`Mathf.Abs`/`Mathf.PI` Unity API. Decision Log: BitCrush clamp at caller not core (hot-path branch); single `if`-subtract phase wrap (no `fmod`); `0.9995f` pole pinned (not param); `Mathf.Sin` direct (LUT → Stage 5.3); `None` + delay-line kinds share `default` arm. Closes Stage 5.1 T5.1.4 Exit bullet "BitCrush/RingMod/SoftClip/DcBlocker implemented; Comb/Allpass/Chorus/Flanger return passthrough". Consumer is TECH-260 (`BlipVoice.Render` unrolled 4-slot dispatch).
  - Acceptance: `BlipFxChain.cs` present; 4 memoryless cores implemented; delay-line kinds return input unchanged; zero managed allocs (verified via NoAlloc test in TECH-260); `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: **TECH-256** (archived), **TECH-258** (archived).

- [x] **TECH-258** — `BlipVoiceState` FX state fields (Stage 5.1 Phase 1) (2026-04-16)
  - Type: audio / data model
  - Files: `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Appended 12 blittable `float` fields at struct tail of `BlipVoiceState` — slot-grouped triplets `(dcZ1_N, dcY1_N, ringModPhase_N)` for slots 0..3 (DC blocker input/output z-1 + ring-mod carrier phase). Grouped by slot (not by kind) for cache locality — TECH-260 unrolled dispatch reads all three per-slot fields per `BlipFxChain.ProcessFx` call. Flat-field pattern (not nested `BlipFxVoiceSlot` struct) matches existing `phaseA..D` precedent + avoids `ref state.fxSlot0.dcZ1` indirection against TECH-259 `ref float` kernel params. No `readonly` modifier — `ref`-writable required by TECH-259. No explicit `ResetFxState()` — relies on caller zero-init + `default(BlipVoiceState) = 0f` (per `ia/specs/audio-blip.md §3.2`). Each field carries `<summary>` XML pointing to TECH-259 consumer; inline comment block documents slot-triplet layout. Zero behavior change — `BlipVoice.Render` untouched; MVP golden fixtures stay bit-exact (FX wire lands in TECH-260). Closes Stage 5.1 Phase 1 Exit bullet "`BlipVoiceState` extended w/ FX state". Feeds **TECH-259** + **TECH-260**.
  - Acceptance: 12 new float fields present w/ exact names; `BlipVoiceState` still blittable (unmanaged-struct compile); `default(BlipVoiceState) = 0f` contract preserved; `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: none.

- [x] **TECH-257** — `BlipPatch.fxChain` + `BlipPatchFlat` FX inline fields + ctor extension (Stage 5.1 Phase 1) (2026-04-16)
  - Type: audio / data model
  - Files: `Assets/Scripts/Audio/Blip/BlipPatch.cs`, `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs`, `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: `BlipPatch` gained `[SerializeField] private BlipFxSlot[] fxChain = new BlipFxSlot[0]` + `BlipFxSlot[] FxChain => fxChain` getter mirroring oscillator public surface. `OnValidate` truncates `fxChain` to max 4 entries via `Array.Resize(ref fxChain, 4)` after existing oscillator cap-at-3 resize (silent truncate precedent). `BlipPatchFlat` gained 4 inline `readonly BlipFxSlotFlat fx0, fx1, fx2, fx3` + `readonly int fxSlotCount` between oscillator triplet and envelope — blittable discipline preserved (no array allocations). Ctor extended after oscillator flatten: `fxSlotCount = fx != null ? Mathf.Min(fx.Length, 4) : 0`; unused slots default. `BlipPatchHash.Compute` append-only section 9 — feeds `fxSlotCount` + per-active-slot `kind`/`param0`/`param1`/`param2`; sections 1–8 unchanged. Decision Log: FX cap=4 vs oscillator cap=3 (composition depth); silent truncate (author ergonomics precedent); `patchHash` feeds FX unconditionally (one-time shift at land — fixtures gate on PCM tolerance per `ia/specs/audio-blip.md §7.2`, not hash equality; LRU re-bakes on miss); append-only section order frozen (FX delay state + LFO state append as sections 10+ in Stage 5.2/5.3). MVP golden fixtures stayed bit-exact (empty-chain passthrough). Closes Stage 5.1 Phase 1 Exit bullets on `BlipPatch` + `BlipPatchFlat` extension. Feeds **TECH-258** (`BlipVoiceState` FX state), **TECH-259** (`BlipFxChain.ProcessFx` kernel), **TECH-260** (`BlipVoice.Render` FX dispatch).
  - Acceptance: `BlipPatch.fxChain` + `OnValidate` truncation present; `BlipPatchFlat` has 4 inline fx slots + `fxSlotCount`; existing MVP golden fixtures pass (empty chain = passthrough); `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: **TECH-256** (archived — `BlipFxKind` / `BlipFxSlot` / `BlipFxSlotFlat` types).

- [x] **TECH-255** — `web/README.md` §Portal documentation (Stage 5.1 Phase 2) (2026-04-16)
  - Type: web / docs
  - Files: `web/README.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Inserted `## Portal` section between `## Dashboard` and `## Tokens` in `web/README.md` — four subsections: Database provider (Neon free Launch tier cited per TECH-252 lock, orchestrator Decision Log cross-link for full limits table, no duplication), Connection pool pattern (lazy singleton via `getSql()` + `sql` tagged-template Proxy per TECH-254 `web/lib/db/client.ts` shape, build-time safety note — `next build` green w/o `DATABASE_URL`), `DATABASE_URL` env contract (Vercel tri-scope production + preview + development flagged `[HUMAN ACTION]` pending dashboard wiring, contributor `.env.local` guidance — not shipped), Payment gateway placeholder (architecture slot reserved, no provider, Q10 deferred). Closing boundary paragraph — "Step 5 architecture-only — no migrations, no live queries, no auth flow; Stage 5.2 files schema, Stage 5.3 files middleware." `§Links` block unchanged — orchestrator cross-link already present pre-TECH-255. Caveman prose throughout — caveman-exception boundary does NOT apply to contributor-facing README per `agent-output-caveman.md` §exceptions (exception surface is `web/content/**` + `web/app/**/page.tsx` only). Closes Stage 5.1 Phase 2 Exit bullet. Zero code edits — pure markdown surface. Invariants #1–#12 not implicated (web platform scaffold only).
  - Acceptance: `## Portal` section present between `## Dashboard` and `## Tokens`; four subsections + boundary paragraph; `npm run validate:all` exit 0.
  - Depends on: **TECH-252** (archived — Neon provider lock), **TECH-254** (archived — `web/lib/db/client.ts` lazy driver wiring).

- [x] **TECH-256** — `BlipFxKind` + `BlipFxSlot` + `BlipFxSlotFlat` in `BlipPatchTypes.cs` (Stage 5.1 Phase 1) (2026-04-16)
  - Type: audio / types
  - Files: `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs`
  - Spec: (removed at closeout — pure data-type scaffolding; Decision Log + Lessons sections empty; full prose in git history only)
  - Notes: Landed 3 new types in `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs` — `BlipFxKind` enum (None=0/BitCrush=1/RingMod=2/SoftClip=3/DcBlocker=4/Comb=5/Allpass=6/Chorus=7/Flanger=8, explicit ints pinned for stable `switch`-dispatch in TECH-259 `BlipFxChain.ProcessFx`), `BlipFxSlot [Serializable] struct` (authoring row: `BlipFxKind kind; float param0, param1, param2`), `BlipFxSlotFlat readonly struct` (blittable runtime mirror, copy ctor from `BlipFxSlot`, scalar-only fields so unmanaged-struct compile verifies blittability — mirrors `BlipPatchFlat` discipline per **Blip patch flat** glossary row). Full 9-value enum up front even though Comb/Allpass/Chorus/Flanger kernels land Stage 5.2 — prevents enum-value churn mid-step. No glossary row added (per Step 1 precedent, terms land at Stage 5.1 close, not per-task). Feeds TECH-257 (`BlipPatch.fxChain` + `BlipPatchFlat` inline flatten), TECH-258 (`BlipVoiceState` FX fields), TECH-259 (`BlipFxChain.ProcessFx` kernel dispatch), TECH-260 (`BlipVoice.Render` FX loop).
  - Acceptance: 3 new types present; `BlipFxSlotFlat` passes blittable check (unmanaged-struct compile); `npm run unity:compile-check` green; `npm run validate:all` exit 0.
  - Depends on: none

- [x] **TECH-254** — Postgres driver install + `web/lib/db/client.ts` + Vercel `DATABASE_URL` wiring (Stage 5.1 Phase 2) (2026-04-16)
  - Type: web / scaffold
  - Files: `web/package.json`, `web/package-lock.json`, `web/lib/db/client.ts` (new), Vercel project env (production + preview + development — `[HUMAN ACTION]` pending dashboard wiring)
  - Spec: (removed at closeout — Decision Log + Lessons Learned persisted to `ia_project_spec_journal`; full prose in git history only)
  - Notes: Installed `@neondatabase/serverless@^1.0.2` (resolved `1.0.2` — spec draft cited `^0.9.x`, v1 reached stable; Decision Log updated w/ actual pin). Authored `web/lib/db/client.ts` — lazy singleton via `getSql()` getter + `new Proxy({} as NeonQueryFunction<false, false>, { get, apply })` tagged-template handle; first `sql` invocation reads `process.env.DATABASE_URL` and throws clear error if missing; repeat imports return same singleton (no per-request reconnection); `next build` w/o env set stays green (build-time safe). Shape satisfies Stage 5.2 `drizzle-orm/neon-http` adapter — one-line wrap `drizzle(getSql(), { schema })`. Vercel `DATABASE_URL` wiring across production + preview + development scopes flagged `[HUMAN ACTION]` — agent shell has no Vercel CLI auth per Stage 1.2 Decision Log precedent (2026-04-14); human completes via dashboard. No migrations, no live queries, no auth handlers at this tier (deferred to Stage 5.2 TECH-5.2.x). No local `.env` shipped — contributors wire own value post-TECH-255 README §Portal. Invariants #1–#12 not implicated (web platform scaffold only). Feeds TECH-255 (README §Portal doc) + Stage 5.2 schema/auth tasks.
  - Acceptance: `@neondatabase/serverless` under `dependencies` (not `devDependencies`); `web/lib/db/client.ts` exports `sql` + `getSql`; `npm --prefix web run typecheck` exit 0; `npm --prefix web run build` exit 0 in shell w/o `DATABASE_URL`; `npm run validate:all` exit 0; `DATABASE_URL` present in Vercel env for all three scopes (`[HUMAN ACTION]` — human-verified).
  - Depends on: **TECH-252** (archived — Neon provider lock).

- [x] **TECH-253** — Auth library evaluation + Decision Log entry (Stage 5.1 Phase 1) (2026-04-16)
  - Type: web / decision log
  - Files: `ia/projects/web-platform-master-plan.md` (Decision Log row appended), `docs/web-platform-exploration.md` (§Phase W7 locked constants migrated)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Locked **roll-own JWT + sessions** (Q11 confirmed) — row appended to orchestrator `§Orchestrator Decision Log` (2026-04-16). Stack: `jose` (`SignJWT` / `jwtVerify`, Edge-safe Web Crypto) for token sign/verify; `@node-rs/argon2` (argon2id) password hash confined to Node-runtime route handlers — never Edge middleware; stateful `session` row (`id UUID PK, user_id UUID FK, expires_at TIMESTAMPTZ, token TEXT`) enables revocation per Q11 "no third-party auth provider". Three locked downstream constants: `SESSION_COOKIE_NAME=portal_session` (consumed TECH-5.3.1 middleware), `SESSION_LIFETIME_DAYS=30` (consumed TECH-5.2.1 schema), password hash lib `@node-rs/argon2` (consumed TECH-5.2.3 stub `/api/auth/register`). Lucia Auth v3 rejected — officially sunsetted/archived by author (pilcrow) late 2025; maintenance risk unacceptable for a session-first library owning cookie + session lifecycle. Auth.js v5 (NextAuth) rejected — full OAuth/PKCE/CSRF machinery ships even with Credentials-only config (~50 kB server bundle overhead); Credentials provider + DB session requires Node runtime split anyway (same as roll-own); overkill for email+password MVP with no social login planned. Durable rationale + constants migrated to `docs/web-platform-exploration.md §Phase W7` so TECH-254 / TECH-5.2.x / TECH-5.3.x readers find them without re-reading deleted spec. No code — Decision Log authoring only.
  - Acceptance: Decision Log row appended w/ locked library + API surface note + rationale; Q11 confirm/update entry visible in orchestrator header; `npm run validate:all` exit 0.
  - Depends on: none

- [x] **TECH-252** — Postgres free-tier provider evaluation + Decision Log entry (Stage 5.1 Phase 1) (2026-04-16)
  - Type: web / decision log
  - Files: `ia/projects/web-platform-master-plan.md` (Decision Log row appended)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Locked **Neon free (Launch tier)** as web platform Postgres provider — row appended to orchestrator `§Orchestrator Decision Log` (2026-04-16). Rationale quoted in row: pooled connections 100 >> expected ≤ 20 serverless; storage 0.5 GB vs ≤ 0.1 GB Stage 5.2 stub (monitor at 0.4 GB); egress 5 GB/month; us-east-1 matches Vercel default; `@neondatabase/serverless` HTTP driver sidesteps TCP leak on serverless cold-start; branch preview-DB enables per-PR isolated DBs at TECH-254+. Supabase free rejected — 7-day inactivity pause risks dashboard latency + bundled auth/storage/edge adds scope (auth owned by TECH-253). Vercel Postgres Hobby rejected — 256 MB storage + 1 GB/mo egress near Stage 5.2 ceiling; single-region lock; Neon-backed so no reliability diff vs. Neon direct. No code — Decision Log authoring only per Stage 5.1 design (web platform orchestrator §34 disjoint from Unity runtime invariants #1–#12). Feeds TECH-254 (driver install + pool wiring) + TECH-255 (README §Portal doc).
  - Acceptance: Decision Log row appended w/ provider name + limits table + rationale + two alternatives rejected per-alt reason; `npm run validate:all` exit 0.
  - Depends on: none

- [x] **TECH-246** — Glossary `Blip bootstrap` row update — visible-UI path + `SfxMutedKey` (Stage 4.2 Phase 2) (2026-04-16)
  - Type: doc / glossary
  - Files: `ia/specs/glossary.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: **Blip bootstrap** row (line 208) definition extended — append "Boot-time: also reads `SfxMutedKey` (`PlayerPrefs.GetInt`) and clamps dB to −80 if muted, ahead of mixer apply. Visible-volume-UI path: `BlipVolumeController` (mounted on `OptionsPanel`) primes slider/toggle from `PlayerPrefs` on `OnEnable` and writes back on change." Spec-ref column unchanged (`ia/specs/audio-blip.md §5.1`, `§5.2` — bootstrap runtime sections only; settings-UI lifecycle lives in `blip-master-plan.md` Step 4 not authoritative spec). Index row (line 32) unchanged — term name stable. No new rows for `SfxMutedKey` / `BlipVolumeController` — impl-detail identifiers, `glossary_discover` returned no hits. Closes Stage 4.2 Exit bullet "`ia/specs/glossary.md` **Blip bootstrap** row updated with `SfxMutedKey` boot-time restore + `BlipVolumeController` visible-UI path".
  - Acceptance: **Blip bootstrap** row reflects visible-UI path + `SfxMutedKey` semantics; spec-ref + Index rows byte-identical; `npm run validate:all` exit 0 (dead-spec-refs + frontmatter + IA indexes).
  - Depends on: **TECH-243**, **TECH-244**, **TECH-245** (all archived — the three behaviors the extended glossary row describes).

- [x] **TECH-245** — `BlipBootstrap.SfxMutedKey` + boot-time mute restore (Stage 4.2 Phase 2) (2026-04-16)
  - Type: audio settings / persistence
  - Files: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: `public const string SfxMutedKey = "BlipSfxMuted";` already landed at `BlipBootstrap.cs` line 33 alongside TECH-243 consumer — Phase 1 verify-only (no re-declare to avoid double-definition + compile break). Inserted 2-line mute-restore block into `BlipBootstrap.Awake` after `float db = PlayerPrefs.GetFloat(SfxVolumeDbKey, SfxVolumeDbDefault)` (current line 58) and before `if (blipMixer == null)` null guard — `int muted = PlayerPrefs.GetInt(SfxMutedKey, 0); if (muted != 0) db = -80f;` clamps to -80 dB ahead of `blipMixer.SetFloat(SfxVolumeParam, db)` apply. Cold-start guarantee: player who muted in prior session hears silence from first Blip play; no unmuted click burst before Options opens + `BlipVolumeController.OnEnable` primes toggle. Existing `Debug.Log($"[Blip] SfxVolume bound headless: {db} dB")` naturally reflects -80 on mute — no extra log. Invariants #3 (`Awake`-only, no per-frame read) + #4 (no new singleton — static const on existing MonoBehaviour) preserved. Satisfies Stage 4.2 Exit bullet "`BlipBootstrap.cs` — new `public const string SfxMutedKey` constant; `Awake` reads `PlayerPrefs.GetInt(SfxMutedKey, 0)` after volume read; if muted, overrides `db = -80f` before `blipMixer.SetFloat`".
  - Acceptance: `SfxMutedKey` constant single-declaration; `Awake` reads mute key after dB read; muted path clamps db = -80f before mixer apply; `npm run unity:compile-check` exit 0; `npm run validate:all` exit 0.
  - Depends on: **TECH-243** (archived — consumer reads `SfxMutedKey` via `BlipBootstrap` constant).

- [x] **TECH-250** — Clear-filters `Button` control + multi-select smoke + README §Components update (Stage 4.4 Phase 2) (2026-04-16)
  - Type: web / dashboard / docs
  - Files: `web/app/dashboard/page.tsx`, `web/README.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Swapped underline `<a>Clear filters</a>` for `<Button variant="ghost" size="sm" href="/dashboard">Clear filters</Button>` (TECH-231 polymorphic `href` path) at `web/app/dashboard/page.tsx`. Visibility gated by existing `anyFilter = multi.plan.length + multi.status.length + multi.phase.length > 0` predicate (landed in TECH-249; no logic change). Full-English "Clear filters" label (caveman-exception — user-facing rendered UI per `agent-output-caveman.md`). Smoke 4-scenario matrix confirmed on dev server (port 4000): two-status multi + combined status+phase + toggle-off round-trip + Clear → bare URL. Appended `web/README.md §Components` Dashboard multi-select paragraph (helpers location `web/lib/dashboard/filter-params.ts`, canonical comma-delimited URL form, Clear control, `anyFilter` predicate shape). Satisfies Stage 4.4 Exit bullet 4.
  - Acceptance: Button replaces `<a>`; ghost variant + sm size; visible iff `anyFilter`; smoke matrix manually confirmed; README §Components updated; `npm run validate:all` exit 0.
  - Depends on: **TECH-247** (archived), **TECH-248** (archived), **TECH-249** (archived)

- [x] **TECH-249** — Dashboard page multi-select wiring (Stage 4.4 Phase 2) (2026-04-16)
  - Type: web / dashboard
  - Files: `web/app/dashboard/page.tsx`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Rewired `web/app/dashboard/page.tsx` to consume `parseFilterValues` + `toggleFilterParam` from `@/lib/dashboard/filter-params` (TECH-248). Replaced `firstParam` single-value coercion + local `buildHref` w/ `MultiParams = { plan: string[]; status: string[]; phase: string[] }` shape. `filterPlans` now uses `.includes` predicate w/ empty-array = no-filter semantics — OR within dimension (status=Draft,In+Progress matches either) + AND across dimensions (status AND phase must hold). Hierarchical prune on tasks → stages → steps → plans preserved. Per-chip `href` via local `chipHref(key, value)` — single `toggleFilterParam(currentSearch, key, value)` call per chip, prefixed `/dashboard?${qs}` or bare `/dashboard` when empty. `currentSearch` built from `rawParams` preserving sibling dimensions. Chip `active = multi[key].includes(chipValue)` (dropped single-value equality). `anyFilter = multi.plan.length + multi.status.length + multi.phase.length > 0`. Deleted unused `firstParam` + local `buildHref` helpers. Satisfies Stage 4.4 Exit bullet 3.
  - Acceptance: page imports both helpers; filter logic OR-within / AND-across; chip hrefs via toggle helper; multi-select smoke `?status=Draft,In+Progress` narrows rows; `npm run validate:all` exit 0.
  - Depends on: **TECH-247** (archived), **TECH-248** (archived)

- [x] **TECH-248** — `web/lib/dashboard/filter-params.ts` URL helpers (Stage 4.4 Phase 1) (2026-04-16)
  - Type: web / utility module
  - Files: `web/lib/dashboard/filter-params.ts`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Authored `web/lib/dashboard/filter-params.ts` — three named exports: `parseFilterValues(params, key): string[]` (accepts comma-delimited + repeated params, dedupes, returns sorted array; duck-typed `{ getAll }` second arm avoids `ReadonlyURLSearchParams` import to preserve RSC purity), `toggleFilterParam(currentSearch, key, value): string` (add/remove value, re-emits canonical comma-delimited single-param form, returns new query string w/o leading `?`), `clearFiltersHref = '/dashboard'` constant. Empty / whitespace tokens stripped during parse. Zero `fs` / `React` / `fetch` imports — RSC + client safe. Verified via throwaway tsx smoke (not committed — `web/` has no test runner) + `cd web && npm run lint && npm run typecheck && npm run build` green + `npm run validate:all` exit 0.
  - Acceptance: three named exports present; helpers handle both comma + repeated forms; canonical output comma-delimited; module pure (zero disallowed imports); `npm run validate:all` exit 0.
  - Depends on: none

- [x] **TECH-247** — `FilterChips` `Chip` interface confirmation for multi-select (Stage 4.4 Phase 1) (2026-04-16)
  - Type: web / UI primitive
  - Files: `web/components/FilterChips.tsx`, `web/README.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Audited `web/components/FilterChips.tsx` — `Chip = { label: string; active: boolean; href?: string }` already per-chip independent (`chips.map` + `chipClass(c.active)`; zero shared / derived single-active state). Added JSDoc block above `Chip` export documenting multi-select semantics + `href?` fallback + RSC compatibility. Added `### FilterChips` subsection under `web/README.md §Components` (alphabetical between `DataTable` and later primitives). Doc-only PR — zero runtime change. Satisfies Stage 4.4 Exit bullet 1.
  - Acceptance: `Chip` shape confirmed; render path per-chip `active` independent; JSDoc + README note landed; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-244** — `OnSliderChanged` + `OnToggleChanged` bodies (Stage 4.2 Phase 1) (2026-04-16)
  - Type: audio settings
  - Files: `Assets/Scripts/Audio/Blip/BlipVolumeController.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Filled `BlipVolumeController.OnSliderChanged(float v)` — `db = v > 0.0001f ? 20f * Mathf.Log10(v) : -80f`; `PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, db)`; guarded `_mixer.SetFloat(BlipBootstrap.SfxVolumeParam, db)` only when `!_sfxToggle.isOn && _mixer != null` (mute dominates). Filled `OnToggleChanged(bool mute)` — `PlayerPrefs.SetInt(BlipBootstrap.SfxMutedKey, mute ? 1 : 0)`; `_mixer == null` early-return guard; mute → `_mixer.SetFloat(SfxVolumeParam, -80f)`; unmute → re-read `PlayerPrefs.GetFloat(SfxVolumeDbKey, 0f)` + apply. `0.0001f` threshold guards `Log10(0)` singularity. Single-source-of-truth on `PlayerPrefs` (no cached `_lastDb` field — drift-safe). Consumes `BlipBootstrap.SfxMutedKey` constant from sibling TECH-245 (landed same commit / ahead to avoid `CS0117`).
  - Acceptance: slider callback applies `20·log10` w/ `-80` floor + writes `SfxVolumeDbKey` + guards mixer write on mute/null; toggle callback writes `SfxMutedKey` + clamps `-80f` on mute + restores stored dB on unmute + null-guards `_mixer`; `npm run unity:compile-check` exit 0; `npm run validate:all` exit 0.
  - Depends on: **TECH-243** (archived)

- [x] **TECH-242** — SSR build smoke + README §Components PlanChart entry (Stage 4.3 Phase 2) (2026-04-16)
  - Type: web / validation / docs
  - Files: `web/README.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Ran `cd web && npm run build` — exit 0, zero `ReferenceError` / `window is not defined` / `document is not defined` / `navigator is not defined` matches. `npm run validate:all` green (full chain: dead-spec + compute-lib + mcp-ia tests + fixtures + index check + web lint/typecheck/build, 176 pages prerendered). Authored `web/README.md §Components` `### PlanChart` subsection between `### DataTable` and `### Sidebar` — documents two-file split (`PlanChart.tsx` D3 client + `PlanChartClient.tsx` `next/dynamic({ ssr: false })` wrapper), Next 16 App Router RSC restriction + D3 DOM mutation rationale, `PlanChartDatum` shape (typed), props table, fill CSS var names (`--color-bg-status-pending` / `--color-bg-status-progress` / `--color-bg-status-done` real `@theme` aliases + `--color-text-muted` axis/legend), loading skeleton, empty-state behavior, dashboard aggregation example. No phantom tokens. No code changes — docs + smoke only. Closes Stage 4.3 Exit.
  - Acceptance: `cd web && npm run build` exit 0 zero SSR ref errors; `npm run validate:all` green; README PlanChart subsection present w/ all 5 doc bullets; Stage 4.3 Exit satisfied.
  - Depends on: **TECH-239** (archived), **TECH-240** (archived), **TECH-241** (archived)

- [x] **TECH-243** — `BlipVolumeController` Awake mixer cache + OnEnable prime (Stage 4.2 Phase 1) (2026-04-16)
  - Type: audio settings
  - Files: `Assets/Scripts/Audio/Blip/BlipVolumeController.cs`, `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Filled `BlipVolumeController.Awake` — caches `_mixer = BlipBootstrap.Instance?.BlipMixer`; null-guard logs warning + `enabled = false` + early `return` (invariant #3 one-time lookup). Added `OnEnable` override — reads `BlipBootstrap.SfxVolumeDbKey` via `PlayerPrefs.GetFloat(..., 0f)`, converts dB → linear (`Mathf.Pow(10f, db/20f)` clamped `0..1`, floor 0 at `db ≤ -79f`), calls `_sfxSlider.SetValueWithoutNotify(linear)`; reads `BlipBootstrap.SfxMutedKey` via `PlayerPrefs.GetInt(..., 0)`, calls `_sfxToggle.SetValueWithoutNotify(muted)` — `SetValueWithoutNotify` blocks callback loop during prime. Removed `public void OnPanelOpen() { }` stub from `BlipVolumeController.cs`; removed matching `_volumeController?.OnPanelOpen();` call from `MainMenuController.OnOptionsClicked` (Unity `OnEnable` fires on `SetActive(true)` — redundant hook). Cross-phase compile dep: TECH-245 `SfxMutedKey` constant landed same-commit or ahead per Decision Log row 2 to avoid `CS0117`.
  - Acceptance: `Awake` mixer cache present w/ null-guard; `OnEnable` primes slider (linear) + toggle (muted) from `PlayerPrefs`; `OnPanelOpen` stub + call site deleted; `npm run unity:compile-check` green.
  - Depends on: **TECH-235** (archived), **TECH-238** (archived)

- [x] **TECH-238** — `OnOptionsClicked` pre-open hook `_volumeController?.OnPanelOpen()` (Stage 4.1 Phase 2) (2026-04-16)
  - Type: audio / UI lifecycle
  - Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — Decision Log + Lessons sections skipped_empty by journal persist; full prose in git history only)
  - Notes: `OnOptionsClicked` (line 569) — inserted `_volumeController?.OnPanelOpen();` immediately before `optionsPanel.SetActive(true)` (line 573), inside the existing `if (optionsPanel != null)` guard (single-statement `if` converted to block). Null-conditional `?.` covers fallback / first-frame edge cases (invariant #4 / manager-wiring posture). `CloseOptionsPanel` (line 576) unchanged — Unity `OnDisable` on the `BlipVolumeController` (mounted on `OptionsPanel`, deactivates w/ parent) covers cleanup. Stub fires no-op until Stage 4.2 T4.2.1 swaps to `OnEnable`-based prime + deletes both call site + stub. Ordering inside block: blip → prime → activate. Closes Stage 4.1 Exit.
  - Acceptance: Hook line precedes `SetActive(true)`; `CloseOptionsPanel` untouched; Stage 4.1 Exit bullets 1–5 satisfied; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: **TECH-237** (archived)

- [x] **TECH-241** — Dashboard `dynamic({ ssr: false })` PlanChart integration (Stage 4.3 Phase 2) (2026-04-16)
  - Type: web / dashboard / chart wiring
  - Files: `web/app/dashboard/page.tsx`, `web/components/PlanChartClient.tsx` (new)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Wired `PlanChart` into `web/app/dashboard/page.tsx` via `next/dynamic` + `ssr: false`. Next.js 16 App Router RSC forbids `ssr: false` in RSC scope — extracted dynamic call to new `'use client'` wrapper `web/components/PlanChartClient.tsx` and imported wrapper from RSC (canonical pattern per `node_modules/next/dist/docs/01-app/02-guides/lazy-loading.md`). Loading skeleton `<div className="h-[220px] bg-bg-panel animate-pulse rounded" />` — real `@theme` aliases. Status buckets set-based (Option B locked at kickoff, mirrors TECH-233): `PENDING_STATUSES = {'_pending_', 'Draft'}`, `IN_PROGRESS_STATUSES = {'In Progress', 'In Review'}`, reuse existing `DONE_STATUSES = {'Done (archived)', 'Done'}` — covers full `TaskStatus` union. Per-plan aggregate via `plan.allTasks.filter(t => t.id.startsWith('T' + step.id + '.'))`; one `<PlanChart>` per plan below its `<DataTable>`. `plan-loader.ts` + `plan-loader-types.ts` + `parse.mjs` byte-identical. MEMORY tip added for RSC `ssr: false` wrapper pattern.
  - Acceptance: `dynamic(() => import('@/components/PlanChart'), { ssr: false, loading })` wired (via `PlanChartClient.tsx` wrapper); skeleton uses real token classes; per-plan chart renders below `DataTable`; plan-loader byte-identical; `npm run validate:all` green.
  - Depends on: **TECH-239** (archived), **TECH-240** (archived)

- [x] **TECH-240** — PlanChart axes + legend + empty-state refinement (Stage 4.3 Phase 1) (2026-04-16)
  - Type: web / dashboard / chart
  - Files: `web/components/PlanChart.tsx`
  - Spec: (removed at closeout — Decision Log + Lessons sections skipped_empty by journal persist; full prose in git history only)
  - Notes: Extended TECH-239 skeleton. `axisBottom` — step labels w/ `> 12` char ellipsis truncate (`d.slice(0,11) + '…'`). `axisLeft` — integer ticks via `tickFormat(d3.format('d'))` + `.ticks(Math.min(5, Math.max(1, yMax)))` — 1-tick guard for all-pending plans. Inline legend — `<g>` top-right of main chart `<g>` at `translate(innerW - legendWidth, -MARGIN.top + 2)`; 3 swatch rects + text labels `Pending` / `In Progress` / `Done`. Axis + legend text fills via `var(--color-text-muted)` — no inline hex. Empty-state `<p>` switched from inline-style fallback hex to `className="text-text-muted text-sm"` (Tailwind v4 double-prefix per `web/README.md` §189).
  - Acceptance: `axisBottom` + `axisLeft` present w/ truncate + integer ticks; legend renders 3 swatches + labels; axis + legend text via CSS vars; empty-state uses real `@theme` aliases; `npm run validate:all` green.
  - Depends on: **TECH-239** (archived)

- [x] **TECH-239** — D3 install + PlanChart grouped-bar skeleton (Stage 4.3 Phase 1) (2026-04-16)
  - Type: web / dashboard / chart
  - Files: `web/package.json`, `web/components/PlanChart.tsx`, `web/app/globals.css` (token ref only)
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Installed `d3` + `@types/d3` in `web/package.json`. Authored `web/components/PlanChart.tsx` — `'use client'` SVG chart, default export `PlanChart` + named exports `PlanChartProps` + `PlanChartDatum`. `useRef<SVGSVGElement>` + `useEffect(..., [data])` D3 draw. `scaleBand` outer (step labels) + inner (`pending` / `inProgress` / `done`) + `scaleLinear` y. Rect fills via `var(--color-bg-status-pending)` / `--color-bg-status-progress` / `--color-bg-status-done` real `@theme` aliases. Static `480×220` viewport. Empty `data` → early return `<p>` placeholder, no SVG mount. No axes / legend — TECH-240 adds. D3 namespace import (`import * as d3 from 'd3'`) — sibling tiers extend w/ `d3-axis` / `d3-format`.
  - Acceptance: `d3` + `@types/d3` pinned; component exports present; grouped bars render for non-empty data; empty renders `<p>` placeholder w/ no SVG; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-237** — Instantiate `BlipVolumeController` + `Bind` + `InitListeners` in `CreateOptionsPanel` (Stage 4.1 Phase 2) (2026-04-16)
  - Type: audio / UI wiring
  - Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons empty; full prose in git history only)
  - Notes: `CreateOptionsPanel` lines 393–394 placeholder discards `_ = sfxSlider; _ = sfxToggle;` replaced with `var controller = panel.AddComponent<BlipVolumeController>(); controller.Bind(sfxSlider, sfxToggle); controller.InitListeners(); _volumeController = controller;`. Added `private BlipVolumeController _volumeController;` field after `optionsBackButton` decl (line 34) — runtime-only, no `[SerializeField]`. Controller mounts on `OptionsPanel` GameObject (invariant #4 — no new singletons). Call order load-bearing: `AddComponent` → `Bind` → `InitListeners` → field-assign.
  - Acceptance: `_volumeController` field present; chain wired before `SetActive(false)`; back button + `SetActive(false)` unchanged; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: **TECH-235** (archived), **TECH-236** (archived)

- [x] **TECH-236** — OptionsPanel Slider + Toggle UI construction (Stage 4.1 Phase 1) (2026-04-16)
  - Type: audio / UI
  - Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — Decision Log + Lessons sections empty; journal persist skipped both; full prose in git history only)
  - Notes: `CreateOptionsPanel` (line 308) — `contentRect.sizeDelta` `(300,200)` → `(300,260)`. Added `Slider` child `"SfxVolumeSlider"` at `(40,-65)`, `sizeDelta (120,20)`, `min=0 max=1 value=1 wholeNumbers=false`. Label `"SFX Volume"` at `(-55,-65)`, `LegacyRuntime.ttf` size 14 white. Toggle `"SfxMuteToggle"` at `(10,-100)`, `sizeDelta (60,20)`, `isOn=false`. Label `"Mute SFX"` at `(-45,-100)` same style. Back button relocated y=-80 → y=-135 to clear Toggle span (Decision D-1). `sfxSlider` + `sfxToggle` held as method locals (Decision D-2) for TECH-237 chaining. Pure UI construction — zero runtime behavior.
  - Acceptance: Content rect `(300,260)`; Slider + Toggle + labels render; back button at y=-135 unchanged listener; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: none (soft: **TECH-235** (archived) for TECH-237 chaining)

- [x] **TECH-235** — `BlipVolumeController` stub + `BlipBootstrap.BlipMixer` accessor (Stage 4.1 Phase 1) (2026-04-16)
  - Type: audio / UI
  - Files: `Assets/Scripts/Audio/Blip/BlipVolumeController.cs`, `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: New file `BlipVolumeController.cs` — `public sealed class : MonoBehaviour` w/ `Slider _sfxSlider` + `Toggle _sfxToggle` + `AudioMixer _mixer` fields; `Bind(Slider, Toggle)` assigns refs; `InitListeners()` wires `onValueChanged` → empty stubs `OnSliderChanged(float)` / `OnToggleChanged(bool)` + stub `OnPanelOpen()`. `BlipBootstrap.cs` — added `public AudioMixer BlipMixer => blipMixer;` after `SfxVolumeDbDefault` (line ~34). Stub scaffolding — Stage 4.2 fills bodies. Zero runtime behavior.
  - Acceptance: `BlipVolumeController.cs` compiles w/ fields + stubs; `BlipMixer` accessor returns serialized ref; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-234** — Per-step completion stats + README Button/DataTable docs (Stage 4.2 Phase 2) (2026-04-16)
  - Type: web / dashboard + docs
  - Files: `web/app/dashboard/page.tsx`, `web/README.md`
  - Spec: (removed at closeout — Decision Log persisted to `ia_project_spec_journal`; Lessons section empty; full prose in git history only)
  - Notes: Extended `web/app/dashboard/page.tsx` `plan.steps.map((step) => ...)` block — derived `stepTasks = plan.allTasks.filter(t => t.id.startsWith(\`T${step.id}.\`))`, `stepDone = stepTasks.filter(t => DONE_STATUSES.has(t.status)).length`, `stepTotal = stepTasks.length` reusing top-of-file `DONE_STATUSES = new Set(['Done (archived)', 'Done'])` constant (no duplication — drift risk). Rendered `<StatBar label={\`${stepDone} / ${stepTotal} done\`} value={stepDone} max={stepTotal} />` in step heading flex row next to `<BadgeChip>`; guarded `stepTotal === 0` → skip render (no `0/0` placeholder — keeps heading clean, mirrors `filterPlans` empty-prune pattern). Task→step filter stays consumer-side — `plan-loader.ts` + `parse.mjs` byte-identical. Appended `web/README.md §Components` `DataTable` subsection documenting `pctColumn?: { dataKey; label?; max? }` contract + minimal snippet; confirmed existing `Button` subsection matches shipped `web/components/Button.tsx` API (variant / size / href / disabled). Closes Stage 4.2 Exit bullets (Button shipped, DataTable `pctColumn` shipped, per-plan + per-step StatBar rendering, README entries present).
  - Acceptance: Per-step `StatBar` row visible below each step heading on `/dashboard`; `web/README.md §Components` has Button + DataTable `pctColumn` entries; `npm run validate:all` green; Stage 4.2 Exit bullets all satisfied.
  - Depends on: **TECH-231** (archived), **TECH-232** (archived), **TECH-233** (archived — soft)

- [x] **TECH-233** — Per-plan completion `StatBar` on dashboard (Stage 4.2 Phase 2) (2026-04-16)
  - Type: web / dashboard
  - Files: `web/app/dashboard/page.tsx`, `web/components/StatBar.tsx`
  - Spec: (removed at closeout — journal persist skipped both sections empty; Decision Log + Lessons live in git history only)
  - Notes: Extended `web/app/dashboard/page.tsx` plan render loop — added module-local `DONE_STATUSES: ReadonlySet<TaskStatus> = new Set(['Done (archived)', 'Done'])` (both terminal forms — single-string compare under-counts unarchived plans); per plan derived `totalCount = plan.allTasks.length` + `completedCount = plan.allTasks.filter(t => DONE_STATUSES.has(t.status)).length` + `statBarLabel = \`${completedCount} / ${totalCount} done\``; rendered `<StatBar label value max />` in plan `<h2>` heading `<div>` next to `BadgeChip` wrapped in `flex-1 min-w-[12rem] max-w-[24rem]`. Passes raw `value` + `max` (not pre-divided pct) — StatBar owns `[0,100]` clamp + `max ≤ 0 → 0` + `(v/m)*100` per TECH-232 contract. Counts from unfiltered `plan.allTasks` — `filterPlans` prunes `plan.steps[*].stages[*].tasks` only, so status/phase chips do not skew plan-level ratio. `plan-loader.ts` + `plan-loader-types.ts` + `parse.mjs` byte-identical. Feeds Stage 4.2 Phase 2; TECH-234 adds per-step row + README §Components docs.
  - Acceptance: Per-plan `StatBar` visible every plan section; label `"{done} / {total} done"`; raw `value` + `max` passed; `totalCount === 0` renders without NaN; filter chips leave plan ratio unchanged; plan-loader files untouched; `npm run validate:all` green.
  - Depends on: **TECH-232** (archived — soft)

- [x] **TECH-232** — Extend `DataTable` with optional `pctColumn` (StatBar inline) (Stage 4.2 Phase 1) (2026-04-16)
  - Type: web / component primitive
  - Files: `web/components/DataTable.tsx`, `web/components/StatBar.tsx`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Added optional `pctColumn?: PctColumnConfig<T>` prop + exported `PctColumnConfig<T>` type (`{ dataKey: keyof T; label?: string; max?: number }`). Prop set → trailing `<th>` (`label ?? 'Progress'`, no `aria-sort` per §2.2) + trailing `<td>` rendering `<StatBar label value max />` w/ fallbacks `label='Progress'` + `max=100`. Module-local `toFiniteNumber(raw)` coerces non-numeric / `NaN` / `undefined` → `0` at boundary (guards `Math.max/min` `NaN` propagation in StatBar). DataTable passes raw `value` + `max` (not pre-computed pct) — StatBar owns `[0,100]` clamp + `max ≤ 0 → 0` + `(value/max)*100` as single source of truth; backlog snippet (`value={raw/max*100}`) reconciled in Decision Log. Prop absent → zero DOM change; existing `Column<T>` / generic / `statusCell` / `getRowKey` contract preserved. Import `StatBar` from `./StatBar`. Feeds Stage 4.2 dashboard pct rendering (TECH-233 + TECH-234).
  - Acceptance: `pctColumn` typed + optional; `PctColumnConfig<T>` exported; existing call sites compile unchanged; trailing header + StatBar render gated on prop; `NaN` guard holds; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-230** — Blip glossary rows + cross-refs to `ia/specs/audio-blip.md` (Stage 3.4 Phase 2) (2026-04-16)
  - Type: docs / glossary
  - Files: `ia/specs/glossary.md`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Appended 4 new rows alphabetical in Audio block — **Bake-to-clip** (on-demand `BlipPatchFlat` → `AudioClip` via `BlipBaker.BakeOrGet`; LRU 4 MB), **Blip cooldown** (min ms between same-id plays; `BlipCooldownRegistry`), **Blip variant** (per-patch index 0..`variantCount-1`; round-robin or fixed 0 when `deterministic`), **Patch flatten** (`BlipPatch` SO → `BlipPatchFlat` blittable in `BlipCatalog.Awake`; strips managed refs). Rewrote Spec col on 5 existing blip rows (**Blip bootstrap**, **Blip mixer group**, **Blip patch**, **Blip patch flat**, **patch hash**) from `ia/projects/blip-master-plan.md` Stage 1.x → `ia/specs/audio-blip.md §N`. Refreshed Index row line 32 to list all 9 Audio terms + `scene-load suppression`. Kickoff fixed over-claim (5 existing rows, not 13) + 3-col format (was 4-col) + bundled Index refresh. Closes Stage 3.4 + Step 3 Blip lane.
  - Acceptance: 4 new rows present; all existing blip rows cross-ref spec; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-231** — Author `Button` primitive (variant + size + polymorphic) (Stage 4.2 Phase 1) (2026-04-16)
  - Type: web / component primitive
  - Files: `web/components/Button.tsx` (new)
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty; token verification + Tailwind v4 double-prefix + no-clsx conventions migrated to `web/README.md §Components §Button`)
  - Notes: Named-export `Button` + `ButtonProps` (match `BadgeChip` / `FilterChips` / `DataTable` sibling convention — no default export). Polymorphic: `<button>` default, `<a>` when `href` present. `variant` maps to real `@theme` alias classes — `primary` → `bg-bg-status-progress text-text-status-progress-fg` (amber CTA, phantom `accent-info` from spec v1 replaced); `secondary` → `bg-bg-panel text-text-primary border border-text-muted/40`; `ghost` → `bg-transparent text-text-muted hover:text-text-primary`. `size` scale `sm|md|lg` → `px-/py-/text-`. `disabled` → `opacity-50 cursor-not-allowed pointer-events-none` + native attr on `<button>`. No `clsx` dep; template-literal concat. Closes Stage 4.2 Phase 1 Button slot; feeds Stage 4.4 "Clear filters" + future CTAs.
  - Acceptance: Named exports present; variant/size/href/disabled functional on corrected tokens; no inline hex / inline style / new dep; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-229** — Promote blip exploration doc to `ia/specs/audio-blip.md` (Stage 3.4 Phase 2) (2026-04-16)
  - Type: docs / spec promotion
  - Files: `ia/specs/audio-blip.md` (new), `docs/blip-procedural-sfx-exploration.md`, `docs/blip-post-mvp-extensions.md`, `ia/projects/blip-master-plan.md`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to journal; registry-count gate lesson migrated to `ia/specs/REFERENCE-SPEC-STRUCTURE.md` §Conventions #8 + New reference spec checklist #4)
  - Notes: Authored canonical reference spec `ia/specs/audio-blip.md` §1–§10 w/ frontmatter (`purpose` / `audience: agent` / `loaded_by: ondemand` / `slices_via: spec_section`). Exploration doc `docs/blip-procedural-sfx-exploration.md` gained "Superseded by" banner + stays in-tree for recipe tables + post-MVP sketches. Post-MVP extensions doc gained spec cross-ref line. Orchestrator Lessons row for `BlipVoiceState` rewritten to `promoted to ia/specs/audio-blip.md §3 (TECH-229)`; Decision Log row appended. Bumped `build-registry.test.ts` expected entry count 33 → 34. Ran `generate:ia-indexes` + committed `spec-index.json` + `glossary-index.json`.
  - Acceptance: `ia/specs/audio-blip.md` shipped w/ §1–§10; banner on exploration doc; post-MVP extensions cross-refs spec; orchestrator Decision Log row appended; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-228** — Blip golden fixture regression test (EditMode) (Stage 3.4 Phase 1) (2026-04-16)
  - Type: test / regression gate
  - Files: `Assets/Tests/EditMode/Audio/BlipGoldenFixtureTests.cs` (new)
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: New `BlipGoldenFixtureTests.cs` under existing `Blip.Tests.EditMode.asmdef` (Stage 1.4 — no new asmdef; namespace `Territory.Tests.EditMode.Audio`). Parameterized `[TestCase(BlipId.*)]` × 10: parses `tools/fixtures/blip/{id}-v0.json` via `JsonUtility.FromJson<BlipFixtureDto>`, loads SO via `AssetDatabase.LoadAssetAtPath<BlipPatch>("Assets/Audio/Blip/Patches/BlipPatch_{id}.asset")`, re-renders via `BlipTestFixtures.RenderPatch(in flat, fx.sampleRate, seconds=sampleCount/sampleRate, fx.variant)` — reuses Stage 1.4 TECH-137 helpers. Asserts `sumAbsHash` within 1e-6 + zero-crossings within ±2 + `patchHash` exact equality (stale-fixture guard points reviewer at `npx ts-node tools/scripts/blip-bake-fixtures.ts`). Kickoff alignments: sample rate 44100→48000, ns `Blip.*`→`Territory.*`, helper `BlipTestHelpers`→`BlipTestFixtures`, asset path `Assets/Audio/BlipPatches/`→`Assets/Audio/Blip/Patches/`, `RenderPatch` 3rd arg sampleCount→seconds w/ divisibility assert. Closes Stage 3.4 Phase 1.
  - Acceptance: EditMode tests compile green; stale-fixture guard trips on `patchHash` mismatch; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: TECH-227 (archived)

- [x] **TECH-227** — Blip golden fixture bake script + fixture JSONs (Stage 3.4 Phase 1) (2026-04-16)
  - Type: infrastructure / test tooling
  - Files: `tools/scripts/blip-bake-fixtures.ts` (new), `tools/fixtures/blip/` (new dir, 10 JSONs)
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Pure TS port of `BlipVoice.Render` scalar loop (osc bank + AHDSR + one-pole LP; `Math.fround` at arithmetic boundaries keeps float32 rail w/ C# kernel). 10 MVP **Blip patch** recipes hardcoded from `docs/blip-procedural-sfx-exploration.md` §9 — variant 0 per id, `patchHash` FNV-1a 32-bit (Stage 1.2 T1.2.5 field-walk). Writes `tools/fixtures/blip/{id}-v0.json` w/ schema `{ id, variant, patchHash, sampleRate:48000, sampleCount, sumAbsHash, zeroCrossings }`. xorshift seed 0 + guard → `0x9E3779B9` for reproducible bake. Manual run only — `npx ts-node tools/scripts/blip-bake-fixtures.ts`; CI never regens. Dev notes: Node ≥22 TS strip drops `const enum` (→ `as const`); `__dirname` ESM quirk → one `..` from `tools/scripts/`. Satisfies Stage 3.4 exit first half.
  - Acceptance: 10 fixture JSONs emitted; schema complete; bake reproducible; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-223** — Sidebar base markup + icons + static link list (Stage 4.1 Phase 1) (2026-04-16)
  - Type: infrastructure / web workspace / component
  - Files: `web/package.json`, `web/components/Sidebar.tsx` (new)
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Installed `lucide-react` into `web/package.json` deps. Authored `web/components/Sidebar.tsx` as SSR-compatible vertical `<nav>` w/ four `<Link>` entries (`/` → `Home`, `/wiki` → `BookOpen`, `/devlog` → `Newspaper`, `/dashboard` → `LayoutDashboard`). Each link: 24px icon + label text. Design token classes exclusively (`bg-canvas`, `text-primary`, `text-muted`, hover `text-primary`). No active state, no `'use client'`, no `useState` — static markup only (interactive state lands in TECH-224). Named imports preserve tree-shake. Completes Stage 4.1 Phase 1 first half.
  - Acceptance: `lucide-react` installed w/ tree-shake intact; `Sidebar.tsx` renders static list; zero TS errors from lucide imports; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-222** — GridManager cell-select Blip call site (Stage 3.3 Phase 2) (2026-04-16)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Added `using Territory.Audio;` import + `BlipEngine.Play(BlipId.WorldCellSelected)` after each `selectedPoint` assignment in `GridManager.cs` — line 391 (left-click-down) + line 399 (right-click-up non-pan). Invariant #6 carve-out — one-liner side-effect, not new GridManager logic. Invariant #3 — `BlipEngine` static facade self-caches, no per-frame `FindObjectOfType`. 80 ms cooldown enforced by `BlipCooldownRegistry` via patch SO — left-then-right within window collapses to single play. Closes Stage 3.3 Phase 2 + full Stage 3.3 World lane.
  - Acceptance: cell-select fires `WorldCellSelected` SFX; 80 ms cooldown blocks rapid re-selects; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: **TECH-221** (archived, soft — both touch `GridManager.cs`)

- [x] **TECH-221** — BuildingPlacementService place + denied Blip call sites (Stage 3.3 Phase 2) (2026-04-16)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/BuildingPlacementService.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Inserted `using Territory.Audio;` import + `BlipEngine.Play(BlipId.ToolBuildingPlace)` in `PlaceBuilding` success branch (after `PostBuildingConstructed`) + `BlipEngine.Play(BlipId.ToolBuildingDenied)` in `else` branch (after `PostBuildingPlacementError`). Kickoff audit 2026-04-16 relocated denied call from `GridManager` caller — `HandleBuildingPlacement` line 874 is a 4-line delegate with no fail-reason branch. Insufficient-funds early-return stays silent (owned by `ShowInsufficientFundsTooltip`). `GridManager.cs` untouched. Static `BlipEngine` self-caches (invariants #3, #4). Scope narrowed to 1 file.
  - Acceptance: successful placement fires `ToolBuildingPlace`; denied placement fires `ToolBuildingDenied`; insufficient-funds stays silent; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-220** — RoadManager stroke-complete Blip call site (Stage 3.3 Phase 1) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/RoadManager.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Inserted `BlipEngine.Play(BlipId.ToolRoadComplete)` in `TryFinalizeManualRoadPlacement` success tail between `cityStats.AddPowerConsumption(...)` and `return true;`. Static facade (invariants #3, #4). Scenario batch path (`TryCommitStreetStrokeForScenarioBuild`) + interstate path stay silent. Decision Log — Blip fires before any future `InvalidateRoadCache()` placement at success tail per invariant #2 ordering convention. Open Question #1 deferred: success path currently lacks `InvalidateRoadCache()` call (invariant #2 drift vs sibling paths) — belongs to separate road-cache audit issue.
  - Acceptance: stroke completion fires `ToolRoadComplete` SFX once per stroke; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: **TECH-219** (archived, soft — same file)

- [x] **TECH-219** — RoadManager per-tile tick Blip call site (Stage 3.3 Phase 1) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/RoadManager.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Inserted `BlipEngine.Play(BlipId.ToolRoadTick)` inside per-tile loop in `TryFinalizeManualRoadPlacement` after `PlaceRoadTileFromResolved(resolved[i])`. Manual-drag path only — scenario batch (`TryCommitStreetStrokeForScenarioBuild`) stays silent per Decision Log. 30 ms cooldown owned by patch SO via `BlipCooldownRegistry`; no per-call guard. Static `BlipEngine` self-caches (invariants #3, #4). Opens Stage 3.3 Phase 1.
  - Acceptance: per-tile road commit fires `ToolRoadTick` SFX; 30 ms cooldown observed via registry; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-218** — GameSaveManager save-complete Blip call site (Stage 3.2 Phase 2) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Inserted `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText` in `SaveGame` (line ~69) + `TryWriteGameSaveToPath` (line ~91). Patch SO cooldown 2 s via `BlipCooldownRegistry` gates hotkey burst — no additional guard. Failure path (exception) stays silent — Blip call not reached. Closes Stage 3.2 Phase 2 + full Stage 3.2.
  - Acceptance: save-success fires SFX; save failure silent; cooldown prevents burst; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-214** — Dashboard E2E smoke + `progress.html` deprecation decision log (2026-04-15)
  - Type: web (verification + docs)
  - Files: `ia/projects/web-platform-master-plan.md`, `docs/progress.html`
  - Spec: (removed at closeout — Decision Log + Lessons persisted to journal)
  - Notes: Stage 3.3 Phase 1 / T3.3.2. Phase 0 added post-kickoff — confirmed web stack tracked + deployed before smoke. Vercel `/dashboard` → HTTP/2 200; `robots.txt` disallows route; `?plan=` / `?status=` / `?phase=` chips render + compose AND; internal banner in HTML. Appended row to `## Orchestrator Decision Log` documenting `docs/progress.html` deprecation trigger (≥2 stable deploy cycles post Step 4 portal-auth). Closes Stage 3.3 + Step 3.
  - Acceptance: smoke checklist ticked; orchestrator Decision Log row added; `validate:all` green.
  - Depends on: **TECH-213** (archived), **TECH-208** (archived)

- [x] **TECH-215** — MainMenuController UiButtonClick call sites (Stage 3.2 Phase 1) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — Decision Log persisted to journal; Lessons skipped empty)
  - Notes: Added `using Territory.Audio;` + inserted `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in 6 click handlers (`OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`). Static facade — no new fields, no `FindObjectOfType` (invariants #3, #4). First audible Blip call site. Decision Log — per-handler explicit `Play` over EventSystem listener interception (preserves per-button granularity).
  - Acceptance: all 6 handlers fire click SFX; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-212** — BlipCatalog PlayMode smoke (Stage 3.1 Phase 2) (2026-04-15)
  - Type: infrastructure / tests
  - Files: `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs`
  - Spec: (removed at closeout — journal skipped empty; Decision Log preserved in git history)
  - Notes: added `[Test] Catalog_AllMvpIds_Resolve_WithMixerGroup()` to `BlipPlayModeSmokeTests`. Reuses TECH-196 `Blip.Tests.PlayMode.asmdef` + TECH-197 `[UnitySetUp]` (loads `MainMenu.unity`, caches `BlipCatalog`). Phase 1 asserts `_catalog.IsReady` + `MixerRouter != null`. Phase 2 iterates 10 MVP `BlipId` values (`UiButtonHover`, `UiButtonClick`, `ToolRoadTick`, `ToolRoadComplete`, `ToolBuildingPlace`, `ToolBuildingDenied`, `WorldCellSelected` + 3 Eco/Sys ids) and asserts `patch != null`, `patch.patchHash != 0`, `mixerRouter.Get(id) != null` with id-named failure messages. No new asmdef / scene / SetUp. Locks SO → catalog → mixer-router chain before Stage 3.2 / 3.3 call sites land. Closes Stage 3.1.
  - Acceptance: `[Test]` green in PlayMode runner; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: **TECH-211** (archived)

- [x] **TECH-211** — MixerGroup refs + BlipCatalog.entries[] wiring (Stage 3.1 Phase 2) (2026-04-15)
  - Type: infrastructure / audio authoring
  - Files: `Assets/Audio/Blip/Patches/*.asset`, `Assets/Prefabs/Audio/BlipBootstrap.prefab`
  - Spec: (removed at closeout — journal captured Decision Log; Lessons skipped empty)
  - Notes: assigned `mixerGroup` ref on all 10 **Blip patch** SOs per exploration §14 routing (UI pair → `Blip-UI`; Tool + World pair → `Blip-World`; Eco pair → `Blip-Eco`; Sys → `Blip-Sys`). Populated `BlipCatalog.entries[]` MonoBehaviour on `BlipBootstrap.prefab` catalog child — 10 `BlipPatchEntry` rows (`BlipId` + `BlipPatch` SO ref), no null refs, no duplicate ids. Confirmed `catalogSlot` + `playerSlot` `Transform` fields on root `BlipBootstrap` point to child GOs hosting `BlipCatalog` + `BlipPlayer` MonoBehaviours. Decision Log — catalog is MonoBehaviour on prefab child (not standalone SO) so wiring lives on prefab; patch SO canonical path `Assets/Audio/Blip/Patches/` (not `Assets/Audio/BlipPatches/`); slot typing is `Transform` so acceptance requires both Transform assignment + component presence on child GO.
  - Acceptance: 10 SOs carry non-null `mixerGroup`; `entries[]` size == 10 w/ no null refs; prefab slots populated; `npm run unity:compile-check` green.
  - Depends on: **TECH-209** (archived), **TECH-210** (archived)

- [x] **TECH-210** — World BlipPatch SO authoring (Stage 3.1 Phase 1) (2026-04-15)
  - Type: infrastructure / audio authoring
  - Files: `Assets/Audio/Blip/Patches/BlipPatch_ToolRoadTick.asset`, `BlipPatch_ToolRoadComplete.asset`, `BlipPatch_ToolBuildingPlace.asset`, `BlipPatch_ToolBuildingDenied.asset`, `BlipPatch_WorldCellSelected.asset`
  - Spec: (removed at closeout — journal captured Decision Log; Lessons skipped empty)
  - Notes: filled 5 World-lane **Blip patch** SO skeletons (pre-existing from TECH-209 batch) per `docs/blip-procedural-sfx-exploration.md` §9 recipes 5/6/9/10/15. Canonical path `Assets/Audio/Blip/Patches/` w/ `BlipPatch_` filename prefix (not `Assets/Audio/BlipPatches/` as master-plan orig implied). `patchHash` recomputed non-zero via `OnValidate` + differs from skeleton hash `-1679074758`. Decision Log — skeleton reuse over delete-recreate (preserves GUID + `mixerGroup` wiring); multi-note recipes (ex 6 arpeggio, ex 9 two-note) reduced to fundamental + envelope since MVP kernel is single-shot; pitch jitter unit cents (±8 % ≈ ±138 cents). §9 Issue: `HighPass` filter missing from `BlipFilterKind` MVP enum (only `None=0` + `LowPass=1`) — ToolRoadTick noise transient encoded as `kind: 0` + `cutoffHz: 4000` placeholder; post-MVP adds `HighPass=2`.
  - Acceptance: 5 `.asset` files populated; `cooldownMs` targets met (30 / 0 / 0 / 0 / 80); `patchHash` non-zero on each; `npm run unity:compile-check` green.
  - Depends on: none

- [x] **TECH-208** — Dashboard Q14 access gate (`robots.ts` disallow + nav/sitemap audit) (2026-04-15)
  - Type: web (SEO + access gate)
  - Files: `web/app/robots.ts`, `web/app/sitemap.ts`, `web/app/layout.tsx`
  - Spec: (removed at closeout — journal captured Decision Log; Lessons skipped empty)
  - Notes: Stage 3.2 Phase 2 / T3.2.4. Baseline already contained `/dashboard` in `robots.ts` disallow `['/design','/dashboard']` (landed alongside TECH-205); Phase 1 degraded to verification + contract lock. Sitemap audit confirmed no `/dashboard` entry — page lives under `web/app/dashboard/` not `web/content/pages/` so auto-scan skips. Layout footer + `web/content/**` grep clean. Build emitted `Disallow: /dashboard` in `.next/` robots artifact. Decision Log — gate layered as `unlinked + robots disallow + internal banner` until Step 4 portal auth (accepts obscure URL not true access control); `/dashboard` intentionally outside MDX content tree so sitemap scan never lists it (no opt-out flag needed).
  - Acceptance: `robots.txt` build artifact emits `Disallow: /dashboard`; sitemap + nav audited clean; `validate:all` + `validate:web` green.
  - Depends on: **TECH-205** (archived)

- [x] **TECH-207** — Dashboard `FilterChips` wiring (plan / status / phase, SSR query params) (2026-04-15)
  - Type: web (RSC filter)
  - Files: `web/app/dashboard/page.tsx`, `web/components/FilterChips.tsx`, `web/lib/plan-loader-types.ts`
  - Spec: (removed at closeout — journal captured Decision Log; Lessons skipped empty)
  - Notes: Stage 3.2 Phase 2 / T3.2.3. Extended `Chip` type w/ optional `href` → `FilterChips` branches to `<a>` when present, `<span>` otherwise (back-compat preserved). `DashboardPage` signature now `async ({ searchParams: Promise<{ plan?, status?, phase? }> })` per Next 16 async API; `await searchParams` + first-of-array coercion; hierarchical prune (drop empty stages → steps → plans). Chip value sets computed from unfiltered plans so chips stable across filter changes. `buildHref(current, key, value)` preserves sibling params + toggles off on match. Decision Log — extend existing primitive vs. new component (keeps Stage 1.2 `FilterChips` authoritative); hierarchical prune over flat list; single-value params (multi-select deferred).
  - Acceptance: `?plan=`/`?status=`/`?phase=` filter functional; active chip reflects `searchParams`; `validate:all` + `validate:web` green.
  - Depends on: **TECH-205** (archived)

- [x] **TECH-199** — Pool + cooldown assertions (Stage 2.4 Phase 2) (2026-04-15)
  - Type: infrastructure / tests
  - Files: `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs`, `Assets/Scripts/Audio/Blip/BlipPlayer.cs`, `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: `[UnityTest] Play_RapidFire_ExhaustsPoolAndBlocksOnCooldown()` — rapid-fire leg: 16 `BlipEngine.Play(ToolRoadTick)` single frame, no yield; assert no exception + `BlipPlayer.DebugCursor == 0` post-wrap (new `internal int DebugCursor => _cursor` accessor). Cooldown leg: MVP patches all `cooldownMs: 0`, so called `BlipCooldownRegistry.TryConsume` directly w/ 5 000 ms window using identical DSP timestamp — baseline captured, delta asserted == 1 on `BlockedCount` (new `internal int BlockedCount` counter incremented on `TryConsume == false` branch). Single `yield return null` at method end. Decision Log — plain `internal` accessors (no `#if UNITY_EDITOR`): friend-assembly IVT already grants access, conditional compilation fragments XML-doc + trips analyzers, production cost = one int field + getter negligible; `BlockedCount` not reset between tests (tests compute deltas, matches clock-agnostic registry pattern); cooldown leg bypasses `BlipEngine.Play` because no MVP catalog id has `cooldownMs ≥ 100 ms` and test-time catalog mutation out-of-scope. PlayMode test runner pass deferred to manual trigger; CI batch wiring out-of-scope (covered by TECH-204 orthogonal runner).
  - Acceptance: pool wraps w/o exception; `DebugCursor == 0` post-16 plays; cooldown block observed via `BlockedCount` delta == 1; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: **TECH-197** (archived)

- [x] **TECH-204** — Unity **batchmode** **NUnit** runner scripts (**`unity:test-editmode`** / **`unity:test-playmode`**) (2026-04-15)
  - Type: tooling / CI / agent verification
  - Files: `tools/scripts/unity-run-tests.sh` (new), `tools/scripts/parse-nunit-xml.mjs` (new), `package.json` (root), `docs/agent-led-verification-policy.md`
  - Spec: (removed at closeout — journal captured Decision Log; Lessons placeholder)
  - Notes: `unity-run-tests.sh --platform {editmode|playmode}` wraps `Unity -batchmode -runTests`; Node XML parser (`parse-nunit-xml.mjs`) emits `Passed/Failed/Errors/Skipped` + `FAILED: <fullname>` list; exits non-zero on any failure. `--quit-editor-first` guard mirrors `unity:testmode-batch`. npm aliases `unity:test-editmode` / `unity:test-playmode` / `unity:test-all`. Hooked into `verify:local` (not `validate:all` — CI stays Unity-free). Two-tier NUnit strategy decision: Tier A batchmode shipped; Tier B bridge `run_nunit_tests` via `TestRunnerApi` deferred (Editor-open dev-machine path) — deferral note appended to `docs/agent-led-verification-policy.md`.
  - Acceptance: runner script exec-bit + dotenv + editor-helpers wired; stdout contract `Passed: N  Failed: M  Errors: K  Skipped: S` + failed `fullname`s; `verify:local` includes **EditMode** step; `validate:all` unchanged.
  - Depends on: none

- [x] **TECH-205** — Dashboard RSC page skeleton + DataTable wiring (2026-04-15)
  - Type: web (Next.js RSC)
  - Files: `web/app/dashboard/page.tsx`, `web/app/dashboard/_status.ts` (new)
  - Spec: (removed at closeout — journal captured Decision Log; Lessons skipped empty)
  - Notes: Stage 3.2 Phase 1 / T3.2.1. RSC page imports `loadAllPlans()`; per-plan `<section>` renders `<h2>{plan.title}</h2>` + `BadgeChip` for `overallStatus` + `DataTable<TaskRow>` w/ columns `id | phase | issue | status | intent`. Top banner `<p>` flags page as internal / non-public (full-English caveman-exception). Status-mapping helper `_status.ts` strips `" — {detail}"` tail + maps `Done`/`Done (archived)`/`Final` → `'done'`, `In Progress` → `'in-progress'`, `Draft`/`In Review`/`_pending_` → `'pending'`, unknown → `'pending'`. Empty-plans guard emits banner + neutral note, no throw. Decision Log — status mapping at render site (loader stays wrapper-only invariant); overall chip strips trailing detail (detail belongs to later hierarchy stage).
  - Acceptance: `/dashboard` renders every plan; task tables populated; internal banner visible; `validate:all` + `validate:web` lint + typecheck green (build step lock-blocked by running dev server — latest `.next` artifact from 14:15 confirms success).
  - Depends on: **TECH-201** (archived), **TECH-202** (archived)

- [x] **TECH-202** — Plan-loader RSC smoke (Stage 3.1 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/app/dashboard/page.tsx` (new)
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: async default export, no `"use client"`, imports `loadAllPlans` from `@/lib/plan-loader`, awaits + logs `[dashboard] plan count 4` server-side, renders `<main><h1>Dashboard (internal)</h1></main>`. `parse.mjs` inlined cleanly by Next.js server trace — no `next.config.ts` change needed. Decision Log — `serverExternalPackages` rejected (accepts npm package names only, not workspace-relative paths; correct escape hatch for trace misses would be `outputFileTracingIncludes`); RSC over Route Handler (server-rendered page simplest smoke surface); TSX return type inferred (no explicit `Promise<JSX.Element>` — `JSX` namespace dropped in Next 16 / React 19).
  - Acceptance: `cd web && npm run build` green w/ `[dashboard] plan count 4`; `npm run validate:web` + `npm run validate:all` green (141 tests).
  - Depends on: **TECH-201** (archived)

- [x] **TECH-200** — Plan-loader type definitions (Stage 3.1 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/lib/plan-loader-types.ts` (new)
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: TypeScript interface file mirroring `tools/progress-tracker/parse.mjs` JSDoc schema verbatim. Exports `TaskStatus` + `HierarchyStatus` union literals, `TaskRow` + `PhaseEntry` + `Stage` + `Step` + `PlanData` interfaces. Zero runtime code — `export type` / `export interface` only. `parse.mjs` authoritative; file-header JSDoc documents cross-module contract for **RSC** consumers + drift-sync rule. Decision Log — types file separate from loader (TECH-201) per orchestrator Stage 3.1 phase split (allows TECH-202 RSC smoke to import types independently); zero runtime rules out `z.infer` / Zod (duplicating schema as runtime validator forks authority, orchestrator lock violation); include `'Done'` short form in `TaskStatus` union despite parse.mjs canonicalizing to `'Done (archived)'` (JSDoc documents both variants).
  - Acceptance: 7 symbols exported, shapes match parse.mjs JSDoc 1-to-1; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-198** — Resolution + routing assertions (Stage 2.4 Phase 2) (2026-04-15)
  - Type: infrastructure / tests
  - Files: `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs`, `Assets/Scripts/AssemblyInfo.cs`, `Assets/Audio/Blip/Patches/BlipPatch_*.asset` (×10), `Assets/Audio/Blip/BlipBootstrap.prefab`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: `[UnityTest] Play_AllMvpIds_ResolvesAndRoutes()` iterates 10 MVP `BlipId`s (`UiButtonHover`, `UiButtonClick`, `ToolRoadTick`, `ToolRoadComplete`, `ToolBuildingPlace`, `ToolBuildingDenied`, `WorldCellSelected`, `EcoMoneyEarned`, `EcoMoneySpent`, `SysSaveGame`); asserts per-id `BlipCatalog.Resolve(id)` non-throw + `PatchHash(id) != 0`, `MixerRouter.Get(id) != null`, `BlipEngine.Play(id)` `DoesNotThrow`; single `yield return null` post-loop. `AssemblyInfo.cs` grants `InternalsVisibleTo("Blip.Tests.PlayMode")` (mirrors EditMode grant). 10 `BlipPatch_*.asset` authored under `Assets/Audio/Blip/Patches/` w/ pre-computed FNV-1a patchHash; `BlipBootstrap.prefab` patched w/ `BlipCatalog` component wiring all 10 entries. Blip-UI routes: UiButtonHover + UiButtonClick + SysSaveGame; Blip-World: remaining 7. Decision Log — IVT grant over public widening (catalog `MixerRouter` + `PatchHash` stay internal per invariant #4 ownership); single post-loop yield over per-id (drains `AudioSource.Play` side-effects once, avoids tangling w/ TECH-199 pool assertions). Green in Unity Test Runner (screenshot confirmed).
  - Acceptance: all 10 MVP ids resolve patch + mixer group; `Play` does not throw; `npm run unity:compile-check` + `npm run validate:all` green; PlayMode test passes locally.
  - Depends on: **TECH-197** (archived)

- [x] **TECH-197** — Boot-scene fixture SetUp (Stage 2.4 Phase 1) (2026-04-15)
  - Type: infrastructure / tests
  - Files: `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs`
  - Spec: (removed at closeout — journal skipped empty sections)
  - Notes: `[UnitySetUp]` loads `MainMenu` scene (build index 0) + `yield return null` × 2 (Awake cascade + catalog ready); asserts `BlipBootstrap.Instance` + `BlipCatalog.IsReady`; caches `_catalog` + `_player` refs. `[UnityTearDown]` unloads scene.
  - Acceptance: SetUp boots MainMenu + catches ready flag; TearDown unloads clean; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: **TECH-196** (archived)

- [x] **TECH-196** — PlayMode asmdef bootstrap (Stage 2.4 Phase 1) (2026-04-15)
  - Type: infrastructure / tests
  - Files: `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef`, `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: new `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` — name `Blip.Tests.PlayMode`, `rootNamespace` `Territory.Tests.PlayMode.Audio`, `includePlatforms: ["Editor"]`, `optionalUnityReferences: ["TestAssemblies"]`, `autoReferenced: false`, references `["TerritoryDeveloper.Game"]` (Blip runtime lives in root game asmdef — no dedicated `Blip.asmdef` exists). Mirrors sibling `Blip.Tests.EditMode.asmdef` shape. Companion `BlipPlayModeSmokeTests.cs` declares empty `public sealed class BlipPlayModeSmokeTests` under `Territory.Tests.PlayMode.Audio` — anchors asmdef resolution, no test attributes. Decision Log — `optionalUnityReferences: ["TestAssemblies"]` over top-level `"testAssemblies": true` (matches sibling + Unity legacy schema); `autoReferenced: false` (test asmdef isolated from unrelated asmdefs); anchor class empty by design (fixture body lands in TECH-197..TECH-199).
  - Acceptance: asmdef + `.meta` + anchor `.cs` land under `Assets/Tests/PlayMode/Audio/`; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-195** — Extend sitemap w/ devlog slugs + footer nav links (Stage 2.3 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/app/sitemap.ts` (extend), `web/app/layout.tsx` (extend)
  - Spec: (removed at closeout — journal persist `ok`, both sections empty)
  - Notes: Extend `sitemap.ts` — `resolveDevlogDir()` helper mirrors `resolvePagesDir` (cwd = repo root or `web/`); scans `web/content/devlog/*.mdx`, parses `gray-matter` for frontmatter `date`, emits entries `${base}/devlog/${stem}` w/ `lastModified=new Date(date)`, `changeFrequency: 'weekly'`, `priority: 0.6`; `/devlog` index entry `priority: 0.7`, `lastModified=max(date)` across posts (fallback `new Date()`). Pages-section ordering untouched. `web/app/layout.tsx` — new `<footer>` sibling after `{children}` inside `<body>` (root layout `flex flex-col`); two `next/link` anchors `/devlog` ("Devlog") + `/feed.xml` ("RSS"), inline `@/lib/tokens` muted-text + top-border styling, no new component file. Decision Log — RSS autodiscovery `<link rel="alternate">` deferred (explicit Non-Goal per Stage 2.3 scoping); footer inlined in root layout vs separate `Footer.tsx` component (minimal diff, matches existing page-shell pattern); sitemap priorities 1.0 landing > 0.8 pages > 0.7 devlog index > 0.6 individual post (signals crawl weight).
  - Acceptance: `/sitemap.xml` includes `/devlog` + each devlog slug w/ correct `lastModified`; footer renders `/devlog` + `/feed.xml` on every route; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-192** (archived), **TECH-194** (archived)

- [x] **TECH-194** — RSS 2.0 feed route for devlog (Stage 2.3 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/app/feed.xml/route.ts` (new), `web/lib/mdx/loader.ts` (devlog scan helper)
  - Spec: (removed at closeout — journal persist `ok`, decision_log inserted, lessons_learned empty)
  - Notes: `GET` returns RSS 2.0 XML enumerating latest 20 devlog posts w/ `<item>` per post (`title`, `link`, `description` from `excerpt`, `pubDate` RFC-822 via `toUTCString()`, `guid` absolute link w/ `isPermaLink="true"`); `Content-Type: application/rss+xml; charset=utf-8`. Channel metadata: `title`, `link`, `description`, `language=en`, `lastBuildDate`. `export const dynamic = 'force-static'` — Next 16.2.3 prerender at build (fs scan deterministic). Inline 5-char XML-escape helper. Absolute URLs via `getBaseUrl()` (consistent w/ sitemap precedent). Autodiscovery `<link rel="alternate">` deferred to **TECH-195**.
  - Acceptance: `/feed.xml` returns well-formed RSS 2.0 XML ≤20 items desc; correct `Content-Type`; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-192** (archived)

- [x] **TECH-191** — `BlipEngine.StopAll` dispatch body (Stage 2.3 Phase 2) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipEngine.cs`, `Assets/Scripts/Audio/Blip/BlipBaker.cs`, `Assets/Scripts/Audio/Blip/BlipPlayer.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: `BlipEngine.StopAll(BlipId id)` body — `AssertMainThread()` → `ResolveCatalog()` / `ResolvePlayer()` null-silent mirrors `Play` gate; `!cat.IsReady` → silent return; `int patchHash = cat.PatchHash(id)` → `HashSet<AudioClip> hits = new(cat.Baker.EnumerateClipsForPatchHash(patchHash))` → iterate `player.Pool` → `src.Stop()` where `src.isPlaying && hits.Contains(src.clip)`. Added `internal IEnumerable<AudioClip> BlipBaker.EnumerateClipsForPatchHash(int)` — scans `_index` keys, yields `entry.clip` on `key.patchHash` match, no LRU mutation. Added `internal IReadOnlyList<AudioSource> BlipPlayer.Pool => _pool`. Decision Log — `internal Pool` accessor over `StopMatching(Predicate<AudioClip>)` callback (same-namespace trust, scales to future ops); `IEnumerable<AudioClip>` return avoids per-call allocation (caller materializes `HashSet`); hard `AudioSource.Stop()` no fade (master plan Stage 2.3 exit; fade requires voice-state tracker punted post-MVP). Non-destructive — LRU cache order + byte total untouched.
  - Acceptance: `StopAll` halts matching voices via ref-equality on `source.clip`; non-matching voices untouched (isolation via `HashSet.Contains`); baker LRU unchanged; catalog/player null or `!IsReady` → silent no-op; `npm run unity:compile-check` green.
  - Depends on: **TECH-190** (archived)

- [x] **TECH-190** — `BlipEngine.Play` dispatch body (Stage 2.3 Phase 2) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipEngine.cs`, `Assets/Scripts/Audio/Blip/BlipCatalog.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: `BlipEngine.Play(BlipId, float pitchMult, float gainMult)` chain — `AssertMainThread()` → `ResolveCatalog()` null/not-ready → silent return; `ResolvePlayer()` null → silent return; `cat.Resolve(id)` → `ref readonly BlipPatchFlat`; `cat.CooldownRegistry.TryConsume(id, AudioSettings.dspTime, patch.cooldownMs)` block → silent return **before** bake; `variantIndex = patch.deterministic ? 0 : cat.NextVariant(id, patch.variantCount)`; `cat.Baker.BakeOrGet(in patch, cat.PatchHash(id), variantIndex)` → clip; `cat.MixerRouter.Get(id)` → group; `player.PlayOneShot(clip, pitchMult, gainMult, group)`. `BlipCatalog` adds `_baker` field + `_patchHashes` parallel int array + `_rngState` xorshift32 dict + `internal` accessors `Baker` / `MixerRouter` / `CooldownRegistry` / `PatchHash(BlipId)` / `NextVariant(BlipId, int)`. Decision Log — `PatchHash` on catalog not flat (SO owns hash; `BlipPatchFlat` intentionally omits); Baker instantiation lands here (Stage 2.2 omitted); xorshift32 over `System.Random` (allocation-free, deterministic, Knuth-hash seed forced odd); player-null silent return mirrors non-ready catalog (boot race safety).
  - Acceptance: play path lands clip on player pool when catalog ready; cooldown-blocked id returns silently without baking; non-ready catalog returns silently; deterministic patch always picks variant 0; `npm run unity:compile-check` green.
  - Depends on: **TECH-189** (archived)

- [x] **TECH-189** — Bind/Unbind + cached lazy resolution (Stage 2.3 Phase 1) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipEngine.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: `BlipEngine` adds `static BlipCatalog _catalog; static BlipPlayer _player;`. `Bind(BlipCatalog c)` / `Bind(BlipPlayer p)` setters (null-safe overwrite via `if (c != null) _catalog = c;`). `Unbind(BlipCatalog)` / `Unbind(BlipPlayer)` identity-guarded nullers (`if (ReferenceEquals(_catalog, c)) _catalog = null;`) — prevents late `OnDestroy` from stale instance wiping freshly-bound reload. `internal static ResolveCatalog()` / `ResolvePlayer()` — return cached field when non-null, else `FindObjectOfType<T>()` + cache. Invariant #3 — one-shot bootstrap lookup, not per-frame. Invariant #4 — no new singleton; state lives on MonoBehaviour hosts. Decision Log — Unbind guarded by `ReferenceEquals` (additive-scene reload safety); `Bind(null)` = no-op not clear (callers use `Unbind` explicitly; off-path callers never explode); lazy `FindObjectOfType` allowed in `Resolve*` (one-time cached, not per-frame).
  - Acceptance: Bind/Unbind overloads land + null-safe; `Resolve*` caches reference on first call; repeated calls do not re-enter `FindObjectOfType`; `npm run unity:compile-check` green.
  - Depends on: **TECH-188** (archived)

- [x] **TECH-188** — `BlipEngine` facade skeleton + main-thread gate (Stage 2.3 Phase 1) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipEngine.cs`, `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: new `public static class BlipEngine` — declares `Play(BlipId id, float pitchMult = 1f, float gainMult = 1f)` + `StopAll(BlipId id)` w/ empty bodies. Private `AssertMainThread()` compares `Thread.CurrentThread.ManagedThreadId` to cached `BlipBootstrap.MainThreadId` (captured first line of `BlipBootstrap.Awake`, Stage 2.1 prereq). Throws `InvalidOperationException` w/ diagnostic on mismatch. Invoked first line of every entry point. Invariant #4 — stateless facade, no new singleton. Decision Log — Bind/Unbind stubs left untouched (TECH-189 fills bodies, keeps task surface narrow + honors master-plan T2.3.2 boundary); `MainThreadId` capture reused from Stage 2.1 `BlipBaker.AssertMainThread` (no duplicate capture); direct off-thread EditMode test deferred (Stage 2.4 PlayMode smoke gates happy path, Unity main-thread context implicit).
  - Acceptance: facade file compiles, static methods present w/ correct signatures; `BlipBootstrap.MainThreadId` captured in `Awake`; `AssertMainThread` throws when invoked off main thread (EditMode test); `npm run unity:compile-check` green.
  - Depends on: none

- [x] **TECH-187** — Client-side wiki search component (Stage 2.2 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/components/WikiSearch.tsx`, `web/app/wiki/page.tsx` (embed), `web/package.json` (`fuse.js` dep), `web/package-lock.json`
  - Spec: (removed at closeout — journal db_error; Decision Log captured in Notes)
  - Notes: Stage 2.2 Phase 2 closer. Client component `web/components/WikiSearch.tsx` (`'use client'`) fetches `/search-index.json` on mount (unmount-guarded `useEffect`), builds `Fuse` instance in `useMemo` w/ `keys: ['title','body','category']`, `threshold: 0.35`, `includeScore: false`. Controlled input; top 10 results link `/wiki/{slug}` via `next/link` w/ category badge. Imports `SearchRecord` from `@/lib/search/types` (no local redefinition). Token-driven styling via `@/lib/tokens`. Embedded in `web/app/wiki/page.tsx` header below description. `fuse.js` pinned exact version. Decision Log — static JSON + client Fuse (no server infra, 156 records trivially fits memory; alternatives: Route Handler streaming, Algolia — overkill); reuse `SearchRecord` (shape owned by TECH-186 emitter, duplication would drift); threshold `0.35` initial (balance typo tolerance + noise on small record set; `0.3` stricter / `0.4` looser revisit on feedback).
  - Acceptance: `/wiki` header shows search input; fuzzy matches span glossary + wiki records linking `/wiki/{slug}`; `fuse.js` pinned exact in `web/package.json`; `web/package-lock.json` updated; `cd web && npm run lint && npm run typecheck && npm run build` green; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-186** (archived)

- [x] **TECH-186** — Build-time search index emitter (Stage 2.2 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/lib/search/build-index.ts`, `web/lib/search/types.ts`, `web/package.json` (`prebuild` + `build:search-index` scripts, `tsx` devDep), `.gitignore` (`web/public/search-index.json`)
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.2 Phase 2 opener. Node CLI `tsx lib/search/build-index.ts` emits deterministic `web/public/search-index.json` (156 records) — glossary via `loadGlossaryTerms()` (TECH-184) + wiki MDX glob `web/content/wiki/**/*.mdx` parsed with `gray-matter`. Records shape `{ slug, title, body, category, type: 'glossary' | 'wiki' }`. Stable sort by `slug` ascending; `JSON.stringify(records, null, 2)` + trailing `\n`. Cwd-dual resolution mirrors `loader.ts` + `glossary/import.ts` (works under `web/` or repo root). `prebuild` script auto-fires before `next build`. Artifact git-ignored (regenerated each build). Decision Log — cwd-dual resolution (mirrors existing pattern); artifact ignored (build output, not source); `tsx` local devDep in web/ (avoids PATH surprises in prebuild hook); sort by `slug` not `title` (stable primary key, no unicode/casing issues); raw MDX body frontmatter-stripped (shape simple, Fuse.js threshold tunes match in TECH-187).
  - Acceptance: `cd web && npm run build:search-index` → 156 records emitted; two runs byte-identical (sha256 match); `prebuild` auto-invokes before `next build`; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-184** (archived), **TECH-185** (archived)

- [x] **TECH-174** — `BlipPlayer.PlayOneShot` round-robin dispatch (Stage 2.2 Phase 3) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipPlayer.cs`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `PlayOneShot(AudioClip clip, float pitch, float gain, AudioMixerGroup group)` — `var source = _pool[_cursor]; _cursor = (_cursor + 1) % _pool.Length;` advances cursor before `Play()` so next caller lands on next voice even if current `Play()` throws. Stops prior clip if still playing (voice-steal hard overwrite — no crossfade, post-MVP per orchestration guardrails §390; MVP 10 sounds + 16-voice pool makes steal rare). Sets `source.clip`, `source.pitch`, `source.volume`, `source.outputAudioMixerGroup` then `source.Play()`. Decision Log — voice-steal = hard overwrite (Stop + reassign, no crossfade); cursor advances before Play (wrap math off playback path); per-call mixer group assignment (BlipMixerRouter.Get resolves upstream in BlipEngine.Play, voice stays group-agnostic).
  - Acceptance: 16 rapid calls wrap cursor once (wrap point `_cursor == 0`); voice-steal overwrites prior clip on wrap; no exception on mid-playback overwrite; `unity:compile-check` green.
  - Depends on: **TECH-173** (archived)

- [x] **TECH-185** — Wiki catch-all route + auto-index + seed page (Stage 2.2 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/app/wiki/[...slug]/page.tsx`, `web/app/wiki/page.tsx`, `web/content/wiki/README.mdx`, `web/lib/wiki/slugs.ts`, `web/components/GlossaryShell.tsx`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.2 Phase 1 closer. Catch-all `/wiki/[...slug]` RSC resolves MDX via `loadMdxContent('wiki', slug)` first, falls back to glossary-derived `<GlossaryShell>` when slug matches imported `GlossaryTerm`, else `notFound()`. `/wiki` auto-index uses `DataTable` w/ Category column (single-table pattern matches `/history`). `web/lib/wiki/slugs.ts` `listWikiSlugs()` centralizes MDX glob + glossary union (MDX wins on collision). `generateStaticParams` unions both sources — build prerenders 157 wiki routes (4 MDX + 153 glossary slugs) via Next 15 async params idiom. Seed `web/content/wiki/README.mdx` proves loader happy path. Decision Log — single `DataTable` w/ Category column (not table-per-category, matches `/history` TECH-166); MDX wins on slug collision (hand-authored overrides glossary shell, enables editorial enrichment path); extract `listWikiSlugs` helper (avoid double-loading glossary); `notFound()` over custom 404 render (Next.js idiom).
  - Acceptance: `/wiki` index lists glossary + wiki MDX rows grouped by category; `/wiki/{glossary-term-slug}` renders definition shell; `/wiki/readme` renders seed MDX; `generateStaticParams` enumerates both sources; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-184** (archived), **TECH-164** (archived), **TECH-162** (archived — DataTable primitive)

- [x] **TECH-178** — Slope regression tests (17 variants) (Stage 1.4 Phase 2) (2026-04-15)
  - Type: test / regression
  - Files: `tools/sprite-gen/specs/building_residential_small_N.yaml`, `tools/sprite-gen/tests/test_slope_regression.py`, `tools/sprite-gen/src/cli.py`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: N-slope spec fixture clones `building_residential_small.yaml` w/ `terrain: N`, `output.name: building_residential_small_N`, `variants: 1`. Pytest `test_slope_regression.py` — `test_n_slope_canvas_grows` invokes `cli.main(["render","building_residential_small_N"])` via monkeypatched `_OUT_DIR=tmp_path`, asserts `img.height > 64` + `canvas.pivot_uv(img.height) != (0.5, 0.25)`. `test_all_17_slope_ids_render` parametrizes over `cli._VALID_SLOPE_IDS - {"flat"}` via `--terrain` override on flat source spec, asserts `rc==0` + PNG exists. `test_flat_sentinel_byte_stable` pins TECH-177 no-op branch (`h==64`, `pivot_uv==(0.5,0.25)`). Decision Log — slope id source = `cli._VALID_SLOPE_IDS - {"flat"}` (single source of truth, matches glossary **Slope variant naming**); parametrize via `--terrain` override (production code path, not 17 YAML fixtures); dedicated N-slope YAML kept (exercises non-override `spec['terrain']` read); flat sentinel test added beyond original spec (pins TECH-177 byte-stable contract); no golden PNG snapshots (shape-level only per §2.2).
  - Acceptance: 17 slope renders pass; N-slope canvas grown + pivot adjusted; `pytest tools/sprite-gen/tests/test_slope_regression.py` green; `npm run validate:all` green.
  - Depends on: **TECH-176** (archived), **TECH-177** (archived)

- [x] **TECH-172** — `BlipCooldownRegistry` plain class (Stage 2.2 Phase 2) (2026-04-15)
  - Type: infrastructure / audio gating
  - Files: `Assets/Scripts/Audio/Blip/BlipCooldownRegistry.cs`, `Assets/Scripts/Audio/Blip/BlipCatalog.cs`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `public sealed class BlipCooldownRegistry` plain class. `Dictionary<BlipId, double> _lastPlayDspTime`. `TryConsume(BlipId id, double nowDspTime, double cooldownMs) → bool` — if unseen OR `(nowDsp - last) * 1000 >= cooldownMs` → record + return `true`; else `false`. Instantiated in `BlipCatalog.Awake` between `_mixerRouter` alloc and `BlipEngine.Bind`; held as `_cooldownRegistry` instance field (invariant #4 — no singleton). Clock-agnostic — caller passes `nowDspTime` (pure-C# testable). Window anchored on first accepted timestamp (blocked attempts do NOT slide window — starvation-safe under rapid spam). No autosave wiring (MVP). Consumer wiring (`BlipEngine.Play` cooldown query + `internal` catalog accessor) deferred to Stage 2.3 T2.3.3. Decision Log — registry clock-agnostic (pure-C# testable w/o PlayMode harness); no `internal` catalog accessor in this spec (T2.3.3 adds when consumer lands — avoid dangling dead code); window anchors on first accepted timestamp (master-plan T2.2.4 pseudocode match); glossary row `Blip cooldown` deferred to Step 2 close (blip-master-plan §Glossary rows — Step 2 Blip-* terms batch-land on Step close).
  - Acceptance: first call returns `true`; second within window returns `false`; after-window returns `true` + updates timestamp; catalog holds instance; `unity:compile-check` green.
  - Depends on: **TECH-169** (archived)

- [x] **TECH-184** — Glossary import helper (Stage 2.2 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/lib/glossary/import.ts`, `web/lib/glossary/types.ts`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.2 Phase 1 opener. `loadGlossaryTerms()` reads `ia/specs/glossary.md` via `resolveGlossaryPath()` (cwd duality — probe repo-root `ia/specs/glossary.md` via `fs.access`, fallback `../ia/specs/glossary.md` for `web/` cwd; mirrors `web/lib/mdx/loader.ts`). Splits by `^## ` headings, drops `Index (quick skim)` + `Planned terminology`; per section scans `|`-delimited rows, skips header + separator; strips `**` from term cell, keeps definition verbatim, discards Spec column (3rd). Slug derivation — lower-case, drop `[…]` bracketed suffix, replace non-alphanumeric runs w/ `-`, trim; deterministic `-2`/`-3` dedup preserves source order. Emits `GlossaryTerm[] = { term, definition, slug, category }` (category required, not optional — every row has a `## Heading` parent). Decision Log — `category` required in type (TECH-185 auto-index groups by it); skip `## Planned terminology` (glossary header flags non-authoritative); regex parse over `remark-parse` (zero deps, glossary format internally controlled); deterministic `-N` slug dedup preserves source order (avoid alphabetical reorder breaking wiki links on glossary edits).
  - Acceptance: `loadGlossaryTerms()` returns typed `GlossaryTerm[]`; `Spec` column absent from output; slugs kebab-case `^[a-z0-9]+(-[a-z0-9]+)*$`; runs from repo-root + `web/` cwd; `Index (quick skim)` + `Planned terminology` skipped; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-164** (archived — cwd duality guard pattern)

- [x] **TECH-177** — Compose slope auto-insert + canvas auto-grow (Stage 1.4 Phase 2) (2026-04-15)
  - Type: infrastructure / composition wiring
  - Files: `tools/sprite-gen/src/compose.py`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `compose_sprite` reads `spec.get('terrain', 'flat')`; non-`'flat'` → prepends `iso_stepped_foundation(canvas, x0, y0, fx, fy, slope_id, material=spec.get('foundation_material','dirt'), palette)` before composition loop; folds `lip = max(corners.values()) + 2` from `slopes.get_corner_z(slope_id)` into `extra_h` so `canvas_size(fx, fy, extra_h)` grows vertically; registers `iso_stepped_foundation` in `_DISPATCH`; `SlopeKeyError` propagates to CLI exit 1. `spec.terrain` absent / `'flat'` = byte-stable no-op. Decision Log — `slope_id` source = `spec['terrain']` matches master-plan T1.4.3 + sibling TECH-178 CLI `--terrain` flag; `flat` branches explicitly (avoid zero-row foundation alloc + keep flat path byte-stable); lip formula mirrors TECH-176 primitive contract (+2 px above tallest corner); pivot recomputation deferred to TECH-179 `unity_meta.write_meta` (compose grows canvas → meta writer reads new `canvas_h` → pivot shifts naturally); `foundation_material` defaults `'dirt'` (palette-agnostic); reuse `src/slopes.py` single `@lru_cache` read point per TECH-176 (no duplicate loader).
  - Acceptance: non-flat `terrain` spec auto-inserts foundation + grows canvas; `SlopeKeyError` → exit 1; `pytest tools/sprite-gen/tests/test_compose.py` green; `npm run validate:all` green.
  - Depends on: **TECH-175** (archived), **TECH-176** (archived)

- [x] **TECH-176** — `iso_stepped_foundation` primitive (Stage 1.4 Phase 1) (2026-04-15)
  - Type: infrastructure / primitive
  - Files: `tools/sprite-gen/src/primitives/iso_stepped_foundation.py`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `iso_stepped_foundation(canvas, x0, y0, fx, fy, slope_id, material, palette)` reads per-corner Z from `slopes.yaml`; builds stair/wedge bridging sloped ground → flat top at `max(n,e,s,w)+2` px lip; draws via `apply_ramp(material, 'south')` / `apply_ramp(material, 'east')` (invariant #9 — visible faces south + east only). `SlopeKeyError` on missing id. Decision Log — foundation ramp mapping reuses existing `apply_ramp` face slots (`top`/`south`/`east`); new `foundation_*` materials deferred to palette work, primitive accepts any material key. YAML loader → `src/slopes.py` with `@lru_cache`, not inline (TECH-177 compose auto-insert needs same read; single cache point). `_project` helper copied into primitive, not extracted (avoids refactor spillover; revisit if 3rd primitive needs same math).
  - Acceptance: 17 slope ids render without crash; `SlopeKeyError` raised on unknown id; `pytest tools/sprite-gen/tests/test_iso_stepped_foundation.py` green; `npm run validate:all` green.
  - Depends on: **TECH-175** (archived)

- [x] **TECH-168** — OG image + per-route `generateMetadata` (Stage 2.1 Phase 3) (2026-04-15)
  - Type: infrastructure / SEO / web workspace
  - Files: `web/app/opengraph-image.tsx`, `web/app/page.tsx`, `web/app/about/page.tsx`, `web/app/install/page.tsx`, `web/app/history/page.tsx`, `web/app/layout.tsx`, `web/lib/site/metadata.ts`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 3 closer. `web/app/opengraph-image.tsx` via `next/og` `ImageResponse` — 1200x630 PNG, palette-token bg (`bg-canvas`) + accent (`raw.green` 4px rule) + title (`text-primary` mono) + tagline (`text-muted` sans); named exports `alt`/`size`/`contentType`. `web/lib/site/metadata.ts` (new) centralizes `siteTitle` + `siteTagline`. `web/app/layout.tsx` `metadata` extended w/ `metadataBase: new URL(getBaseUrl())` + default `title`/`template` + `description` + `openGraph` base. Each public RSC (`/`, `/about`, `/install`, `/history`) exports async `generateMetadata` via `loadMdxPage(slug)` → `Metadata { title, description, openGraph(title/description/url/type:"article"), twitter(card:"summary_large_image") }`. Canonical URL from `getBaseUrl()` + slug. Decision Log — single site-level OG card (no per-slug dynamic OG at MVP); centralize site strings in `lib/site/metadata.ts` (single source for OG card + layout default); `metadataBase` pinned to `getBaseUrl()` for absolute OG URLs across envs. Open Question #1 resolved w/ proposed tagline `"A city builder where geography shapes every decision."`.
  - Acceptance: `/opengraph-image` returns 1200x630 PNG; each page emits `<meta og:*>` + `<title>`; `npm run validate:web` green.
  - Depends on: **TECH-166** (archived), **TECH-165** (archived)

- [x] **TECH-171** — `BlipMixerRouter` plain class (Stage 2.2 Phase 2) (2026-04-15)
  - Type: infrastructure / audio routing
  - Files: `Assets/Scripts/Audio/Blip/BlipMixerRouter.cs`, `Assets/Scripts/Audio/Blip/BlipCatalog.cs`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `public sealed class BlipMixerRouter` plain class. Ctor takes `BlipPatchEntry[] entries`, builds `Dictionary<BlipId, AudioMixerGroup> _map` reading authoring-only `entry.patch.mixerGroup` ref (NOT in `BlipPatchFlat` — Stage 1.2 T1.2.4 Decision Log). `Get(BlipId) → AudioMixerGroup` (throws on unknown id via `ArgumentOutOfRangeException`). Duplicate-id throws `InvalidOperationException` (defense-in-depth; upstream `BlipCatalog` also traps). Instantiated in `BlipCatalog.Awake` + held as instance field `_mixerRouter` before `BlipEngine.Bind(this)` + ready flag. Invariant #4 — plain class, no singleton. Decision Log — router accepts null `patch.mixerGroup` silently (Stage 2.3 consumer falls back to mixer master); router mirrors `BlipCatalog` duplicate-id throw contract for symmetric API surface.
  - Acceptance: router constructs w/o throw on valid entries; `Get` round-trips authored mixer group ref; catalog holds instance; `unity:compile-check` green.
  - Depends on: **TECH-169** (archived)

- [x] **TECH-170** — Catalog `Resolve` + ready flag + `BlipEngine` bind stubs (Stage 2.2 Phase 1) (2026-04-15)
  - Type: infrastructure / audio authoring
  - Files: `Assets/Scripts/Audio/Blip/BlipCatalog.cs`, `Assets/Scripts/Audio/Blip/BlipEngine.cs`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: `Resolve(BlipId) → ref readonly BlipPatchFlat` via `_indexById` (throws on unknown id). `bool isReady` set `true` as last statement in `Awake` — scene-load suppression per Stage 1.1 T1.1.4. `BlipEngine.Bind(BlipCatalog)` + `Unbind(BlipCatalog)` stub signatures (empty bodies; full bodies land Stage 2.3 T2.3.2). Catalog `Awake` calls `BlipEngine.Bind(this)`; `OnDestroy` calls `Unbind`. Null-safe. Decision Log — Bind stub signatures land this task, bodies Stage 2.3 (decouple catalog lifecycle wiring from facade impl; sibling T171/T172 construct against stable surface); suppression boundary comment in `Awake` tail guards against drive-by edits breaking `_isReady = true` last-statement invariant.
  - Acceptance: `Resolve` returns by ref w/ correct patch data; ready flag flips last; `BlipEngine` stub methods compile + no-op; `unity:compile-check` green.
  - Depends on: **TECH-169** (archived)

- [x] **TECH-175** — `slopes.yaml` per-corner Z table (Stage 1.4 Phase 1) (2026-04-15)
  - Type: infrastructure / data table
  - Files: `tools/sprite-gen/slopes.yaml`, `tools/sprite-gen/tests/test_slopes_yaml.py`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 1.4 Phase 1 foundation data. `tools/sprite-gen/slopes.yaml` — 18 top-level keys (`flat` + 17 land slope variants matching `Assets/Sprites/Slopes/{CODE}-slope.png` stems: N, S, E, W, NE, NW, SE, SW, NE-up, NW-up, SE-up, SW-up, NE-bay, NW-bay, NW-bay-2, SE-bay, SW-bay). Each value = `{n,e,s,w}` int map (pixels, 0 or 16). `flat` included as zero-row so composer lookup is uniform (no special-case branch downstream). Authoritative source for TECH-176 `iso_stepped_foundation` + TECH-177 compose auto-insert. Decision Log — `flat` in yaml (avoid compose branch); land only (water slopes are sprites, not foundation geometry); filename stems as canonical id (lowercase-hyphen follows filesystem + master plan; geo §6.4 CamelCase is Unity-prefab-naming concern).
  - Acceptance: yaml loads via `yaml.safe_load`; 18 keys present; codes match `Assets/Sprites/Slopes/` stems 1:1; `pytest tools/sprite-gen/tests/test_slopes_yaml.py` green; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-169** — `BlipPatchEntry` + catalog flatten (Stage 2.2 Phase 1) (2026-04-15)
  - Type: infrastructure / audio authoring
  - Files: `Assets/Scripts/Audio/Blip/BlipPatchEntry.cs`, `Assets/Scripts/Audio/Blip/BlipCatalog.cs`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.2 Phase 1 entry. `[Serializable] public struct BlipPatchEntry { public BlipId id; public BlipPatch patch; }` under `Territory.Audio.Blip`. `public sealed class BlipCatalog : MonoBehaviour` w/ `[SerializeField] private BlipPatchEntry[] entries = System.Array.Empty<BlipPatchEntry>()`. `Awake` iterates entries, validates non-null `patch`, validates unique `id` via `_indexById.TryAdd`, calls `BlipPatchFlat.FromSO(entry.patch)` into parallel `_flat[i]`. `_indexById` pre-sized `entries.Length`. Null / duplicate → `InvalidOperationException` w/ index + id diagnostic. Invariant #4 — scene MonoBehaviour, no singleton. Decision Log — use existing `BlipPatchFlat.FromSO(BlipPatch)` static factory (not hypothetical `BlipPatch.ToFlat()` from backlog note); mixer index left `-1` (TECH-171 router overrides); empty entries array legal (no-op flatten); no ready flag here (deferred to TECH-170); `sealed` class (no subclass extension point).
  - Acceptance: both files present + compile; `_flat.Length == entries.Length`; `_indexById` maps each id → flat slot; duplicate / null throws at `Awake`; `unity:compile-check` + `validate:all` green.
  - Depends on: Stage 1.2 `BlipPatchFlat.FromSO` (archived), Stage 1.1 `BlipId` (archived)

- [x] **TECH-167** — `sitemap.ts` + `robots.ts` (Stage 2.1 Phase 3) (2026-04-15)
  - Type: infrastructure / SEO / web workspace
  - Files: `web/app/sitemap.ts`, `web/app/robots.ts`, `web/lib/site/base-url.ts`
  - Spec: (removed at closeout — journal skipped empty Lessons; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 3 closer. `web/app/sitemap.ts` default-exports async `sitemap()` → `MetadataRoute.Sitemap`; enumerates `web/content/pages/*.mdx` via `fs.readdir` (Node runtime); maps `landing` → `''`, others → slug; absolute URLs via shared `getBaseUrl()` (trims trailing slash; `NEXT_PUBLIC_SITE_URL` w/ `http://localhost:3000` dev fallback); per-entry `lastModified` from TECH-164 loader frontmatter `updated`. `web/app/robots.ts` default-exports `robots()` → `MetadataRoute.Robots` — `{ userAgent: '*', allow: '/', disallow: ['/design', '/dashboard'] }` + `sitemap: ${getBaseUrl()}/sitemap.xml`. `web/lib/site/base-url.ts` shared helper. Decision Log — App Router file-based convention over static `public/sitemap.xml` or custom route handler (native, build-time MDX scan); absolute URLs from env (SEO requirement + staging swappable); disallow `/design` + `/dashboard` at MVP per master-plan Dashboard obscure-URL gate.
  - Acceptance: `/sitemap.xml` 200 w/ 4 `<url>` entries (landing, about, install, history) absolute; `/robots.txt` 200 w/ allow/disallow/Sitemap lines; `npm run validate:web` green.
  - Depends on: **TECH-164** (archived)

- [x] **TECH-166** — About + install + history pages (Stage 2.1 Phase 2) (2026-04-15)
  - Type: feature / web workspace (public user-facing)
  - Files: `web/app/about/page.tsx`, `web/app/install/page.tsx`, `web/app/history/page.tsx`, `web/content/pages/about.mdx`, `web/content/pages/install.mdx`, `web/content/pages/history.mdx`, `web/content/pages/history-timeline.ts`
  - Spec: (removed at closeout — journal persist skipped; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 2 sibling to TECH-165. Three RSCs mirror landing pattern (`web/app/page.tsx`) — async fn, `loadMdxPage(slug)` → token-styled `<main>` + MDX body. `/history` renders timeline via `DataTable` (date/milestone/notes cols) w/ rows from typed `web/content/pages/history-timeline.ts`. `/install` renders platform availability via `BadgeChip` (Mac/Windows/Linux/Web → existing `Status` union, all `pending` seeded). Tokens-only styling; full-English MDX (caveman-exception). Decision Log — timeline data lives in `.ts` module (typed rows feed `DataTable`; MDX stays prose); reuse `BadgeChip` `Status` union verbatim for platforms (no union extension); RSC pattern mirrors landing (no shared layout refactor this stage). Open Questions deferred: final timeline milestones + platform `Status` mapping (product-owner input).
  - Acceptance: three routes reachable on dev; `DataTable` + `BadgeChip` wired; tokens-only; `npm run validate:web` green.
  - Depends on: **TECH-163** (archived), **TECH-165** (archived)

- [x] **TECH-158** — GPL round-trip: export + import + test (Stage 1.3 Phase 4) (2026-04-15)
  - Type: tooling / editor integration (Tier 1)
  - Files: `tools/sprite-gen/src/palette.py`, `tools/sprite-gen/src/cli.py`, `tools/sprite-gen/tests/test_palette_gpl.py`, `tools/sprite-gen/.gitignore`
  - Spec: (removed at closeout — journal skipped, Decision Log captured in Notes)
  - Notes: Stage 1.3 Phase 4 closer. Merged T1.3.7 + T1.3.8 + T1.3.9 atomic (export/import symmetric; test gates both). **Export** `export_gpl(cls, dest_path=None) -> str` on `src/palette.py` — reads `palettes/{cls}.json`, emits GIMP header (`GIMP Palette\nName: {cls}\nColumns: 3\n#\n`) + per-material × level rows (`R G B\t{material}_{level}`). **Import** `import_gpl(cls, gpl_path) -> dict` — skips header through `#`, whitespace-split RGB+name, `rsplit('_', 1)` suffix, groups into materials dict; raises `GplParseError(ValueError)` w/ row-number context on non-int RGB / bad suffix / missing level triplet. **CLI** `palette export {class}` writes `palettes/{class}.gpl`; `palette import {class} --gpl {path}` diffs vs prior JSON + overwrites. Added `*.gpl` to `tools/sprite-gen/.gitignore`. Decision Log — merge 3 tasks (round-trip symmetry requires both sides); `.gpl` gitignored (JSON = source of truth); import tolerates tab **or** space separator (Aseprite emits `\t`, GIMP emits spaces); `rsplit('_', 1)` for suffix (material names may contain `_`); round-trip byte-exact (no HSV re-derive on import — human edits authoritative). Tests `test_palette_gpl.py` — round-trip `residential.json` deep-equal; 24 body rows (8 materials × 3 levels); negative cases for bad RGB / bad suffix / missing level.
  - Acceptance: `palette export residential` writes loadable `.gpl` (owner verified Aseprite load); round-trip deep-equal; `pytest tools/sprite-gen/tests/test_palette_gpl.py` green; `.gpl` untracked; `npm run validate:all` green.
  - Depends on: **TECH-157** (archived)

- [x] **TECH-162** — Memory budget + eviction loop (Stage 2.1 Phase 2) (2026-04-15)
  - Type: infrastructure / cache
  - Files: `Assets/Scripts/Audio/Blip/BlipBaker.cs`, `Assets/Tests/EditMode/Audio/BlipBakerBudgetTests.cs`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 2 closer. Ctor extended to `BlipBaker(int sampleRate = 0, long budgetBytes = 4L * 1024 * 1024)` — 4 MB default per orchestrator Stage 2.1 Exit; throws `ArgumentOutOfRangeException` on `budgetBytes <= 0`. Folded `_totalBytes` accounting into `TryEvictHead` (single mutation site). Miss-insert loop: `while (_totalBytes + newByteCount > _budgetBytes && TryEvictHead()) { }` then `AddAtTail` + `_totalBytes += newByteCount`. Oversize single entry (`newByteCount > _budgetBytes`) → drains cache + post-loop `Debug.LogWarning` + inserts anyway so `BakeOrGet` never returns null. New `internal long DebugTotalBytes` test hook. Decision Log — fold accounting into `TryEvictHead` (not sibling wrapper) keeps invariant local, TECH-161 tests assert structural pop + `Destroy` only so stay green; ctor param order `(sampleRate, budgetBytes)` preserves TECH-161 default-arg call sites; oversize warn+insert beats throw/drop/null (never silently drops play requests). Tests — `BlipBakerBudgetTests.cs` EditMode coverage (budget ceiling, normal insert, oversize warning+non-null, evicted clip destroyed, invalid ctor throws).
  - Acceptance: `_totalBytes ≤ _budgetBytes` after every normal insert; oversize case warns + still returns clip; evicted `AudioClip` instances destroyed; Stage 2.1 Exit bullets 3 + 4 satisfied; `unity:compile-check` + `validate:all` green.
  - Depends on: **TECH-161** (archived)

- [x] **TECH-165** — Landing page RSC + MDX content (Stage 2.1 Phase 2) (2026-04-15)
  - Type: feature / web workspace (public user-facing)
  - Files: `web/app/page.tsx`, `web/content/pages/landing.mdx`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 2 closer. Replaced Next.js boilerplate in `web/app/page.tsx` w/ async RSC — static-imports `Landing` from `@/content/pages/landing.mdx` (via `@next/mdx`) + awaits `loadMdxPage('landing')` for typed frontmatter. Tokens-only styling via `@/lib/tokens` (zero inline hex, zero hardcoded spacing outside scale). Authored `web/content/pages/landing.mdx` — full-English (caveman exception) w/ hero + tagline + what-this-is + CTA to `/install` + `/history`; frontmatter `title` / `description` / `updated`=`2026-04-15`. Decision Log — Path A (static `.mdx` import via `@next/mdx`) over Path B (`next-mdx-remote` runtime compile): landing slug hardcoded, pipeline already wired, zero new dep; frontmatter surfaced via `loadMdxPage` even though body uses static import (single-source validation + typed access); sibling pages under TECH-166 follow same shape. Pattern documented in `web/README.md` §MDX page pattern for future page authors.
  - Acceptance: landing route reachable; MDX rendered via loader; tokens-only styling; `npm run validate:web` green.
  - Depends on: **TECH-163** (archived)

- [x] **TECH-157** — Bootstrap residential palette JSON (Stage 1.3 Phase 3) (2026-04-15)
  - Type: content / palette data
  - Files: `tools/sprite-gen/palettes/residential.json`
  - Spec: (removed at closeout — journal persist empty; Decision Log captured in Notes)
  - Notes: Stage 1.3 Phase 3 closer. Ran `palette extract residential --sources "Assets/Sprites/Residential/House1-64.png" --names "..."` (TECH-154 CLI); 8 K-means clusters hand-named. Final slot mapping (sorted HSV V bright→dark): 0=`window_glass` (40,63,206), 1=`wall_brick_red` (196,178,162), 2=`roof_tile_brown` (193,75,75), 3=`concrete` (106,190,48), 4=`wall_brick_grey` (132,120,110), 5=`roof_tile_grey` (128,47,47), 6=`trim` (59,108,25), 7=`mortar` (0,0,0). Decision Log — swapped `shadow` / `highlight` slots for grey-family wall + roof variants so `apply_variant` material-family swaps resolve without `PaletteKeyError`; `.gpl` export deferred to TECH-158; 4 rendered variants read as residential (beige+red v01, grey+red v02–v03, dark-grey+dark v04). Owner signoff.
  - Acceptance: `palettes/residential.json` committed w/ 8 materials incl. required 4; `render building_residential_small` PNGs read as residential (owner signoff); `npm run validate:all` green.
  - Depends on: **TECH-154** (archived), **TECH-155** (archived)

- [x] **TECH-164** — MDX loader helper + typed frontmatter (Stage 2.1 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/lib/mdx/loader.ts`, `web/lib/mdx/types.ts`
  - Spec: (removed at closeout — journal skipped empty sections; Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 1 closer. `web/lib/mdx/types.ts` exports `PageFrontmatter` (`title`, `description`, `updated` ISO `YYYY-MM-DD`, optional `hero`) + generic `MdxLoadResult<T>` (`{ source, frontmatter }`). `web/lib/mdx/loader.ts` exports `loadMdxContent<T>(dir, slug)` + thin `loadMdxPage(slug)` wrapper. `fs/promises` read → `matter(raw)` parse → required-field + ISO-date regex check → throw `Error` with `{slug, dir, missingFields}` context on bad input; `ENOENT` rethrown w/ slug/dir context. cwd duality guard — resolves both repo-root + `web/` cwd via `fs.access` probe. Seeded `web/content/pages/.gitkeep`. Decision Log — `gray-matter` over custom parser (already installed via TECH-163-archived, battle-tested); generic `loadMdxContent(dir, slug)` shipped now (wiki Stage 2.2 + devlog Stage 2.3 reuse without refactor); no caching Phase 1 (RSC request-level dedup sufficient; revisit if devlog glob hot); `source` returned raw not compiled (downstream RSCs pick `@next/mdx` route vs. `next-mdx-remote`); cwd duality guard (Next runs from `web/`, root `validate:web` may run from repo root).
  - Acceptance: loader + types exported; required-field + ISO-date validation throws w/ slug context; `npm run validate:web` green; `npm run validate:all` green.
  - Depends on: Stage 2.1 Phase 1 opener (archived)

- [x] **TECH-163** — Install + wire MDX pipeline (Stage 2.1 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/package.json`, `web/next.config.ts`, `web/mdx-components.tsx`
  - Spec: (removed at closeout — journal persist ok, Decision Log captured in Notes)
  - Notes: Stage 2.1 opener. Added `@next/mdx`, `@mdx-js/loader`, `@mdx-js/react`, `gray-matter` deps + `remark-frontmatter`, `remark-gfm`, `rehype-slug`, `rehype-autolink-headings`, `@types/mdx` devDeps to `web/package.json`. Wired `web/next.config.ts` via `createMDX` + plugin chain; `pageExtensions` extended w/ `"md"`, `"mdx"`. Added `web/mdx-components.tsx` at project root (App Router requirement, not mentioned in spec sketch — discovered via `node_modules/next/dist/docs/01-app/02-guides/mdx.md`). Decision Log — Next 16 `@next/mdx` API: `import createMDX from "@next/mdx"` + `options: { remarkPlugins, rehypePlugins }`; `mdx-components.tsx` mandatory at project root for App Router; npm workspaces hoisted `@next/mdx` to root `node_modules/` (resolves at build time).
  - Acceptance: deps installed; `withMDX` wraps config; `npm run validate:web` green; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-160** — Bake key + cache hit dispatch (Stage 2.1 Phase 1) (2026-04-15)
  - Type: infrastructure / cache
  - Files: `Assets/Scripts/Audio/Blip/BlipBakeKey.cs`, `Assets/Scripts/Audio/Blip/BlipBaker.cs`
  - Spec: (removed at closeout — journal persist failed, Decision Log captured in Notes)
  - Notes: Stage 2.1 Phase 1 closer. New file `BlipBakeKey.cs` — `public readonly struct BlipBakeKey(int patchHash, int variantIndex)` w/ `IEquatable<BlipBakeKey>` + deterministic hash combine (`patchHash * 397 ^ variantIndex`). In `BlipBaker`: `Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>> _index` + `LinkedList<BlipBakeEntry> _lru`. `BakeOrGet` probes `_index` first; hit → `_lru.Remove(node); _lru.AddLast(node)` (LRU tail promote) + return cached clip; miss → invokes Stage 2.1 Phase 1 opener render path, then hands to Phase 2 insertion + eviction. Reuses `patch.patchHash` from Stage 1.2. Decision Log — `LinkedList<LinkedListNode<Entry>>` indirection (O(1) access-order reorder + O(1) head pop) over `List<T>` (O(n) removal) / `OrderedDictionary` (boxes values); `patchHash * 397 ^ variantIndex` hash combine over `HashCode.Combine` (avoids per-call alloc on some runtimes); keep 3-arg `BakeOrGet(in patch, int patchHash, int variantIndex)` (`BlipPatchFlat` defers `patchHash` per Stage 1.2 — caller reads `BlipPatch.PatchHash`) over adding hash to flat struct (scope creep + breaks blittable-frozen-field contract); `BlipBakeEntry` ref class with `key` + `clip` only (additive byteCount lands next task) over mutable struct entry (`node.Value` copy traps) / full shape in one task (splits Phase 1 / 2 ownership).
  - Acceptance: cache hit returns same `AudioClip` ref as prior bake (ref-equality); miss path produces fresh clip + inserts at LRU tail; node reordering on hit keeps newest at tail; `unity:compile-check` + `validate:all` green.
  - Depends on: Stage 2.1 Phase 1 opener (archived)

- [x] **TECH-156** — Palette unit tests (Stage 1.3 Phase 3) (2026-04-15)
  - Type: test / palette verification
  - Files: `tools/sprite-gen/tests/test_palette.py`
  - Spec: (removed at closeout — journal persist ok, no Lessons/Decision body captured by heuristic)
  - Notes: Stage 1.3 Phase 3 opener. Extended `test_palette.py` with ramp-math tests (low/mid/high-V centroids, clamp at V=1.0) using single-pixel PNG inputs into `extract_palette(..., n_clusters=1)` — deterministic since 1 cluster = input color. Face routing audit confirmed existing `top/south/east` + `PaletteKeyError` coverage; added unknown-face test locked to real `KeyError` (not spec-row prose `ValueError`). Final test count ≥17. Decision Log — unknown-face error type: test real `KeyError` behavior over patching source (face validation = programmer error, `KeyError` idiomatic; follow-up could tighten to `ValueError` w/ valid-faces list); ramp math via single-pixel PNG over monkey-patch `kmeans2` (deterministic 1-cluster path exercises real HSV pipeline, less brittle).
  - Acceptance: `pytest tools/sprite-gen/tests/test_palette.py` exits 0 with ≥17 tests covering ramp math + `apply_ramp` face routing + error cases; `npm run validate:all` green.
  - Depends on: **TECH-153** (archived), **TECH-155** (archived)

- [x] **TECH-155** — `apply_ramp` API + compose wiring (Stage 1.3 Phase 2) (2026-04-15)
  - Type: infrastructure / composition wiring
  - Files: `tools/sprite-gen/src/palette.py`, `tools/sprite-gen/src/compose.py`, `tools/sprite-gen/src/primitives/iso_cube.py`, `tools/sprite-gen/src/primitives/iso_prism.py`
  - Spec: (removed at closeout — journal persist failed, Decision Log captured in Notes)
  - Notes: Stage 1.3 Phase 2 single task (T1.3.3 + T1.3.4 merged — API + sole consumer must land atomic). `load_palette(cls)` reads `palettes/{cls}.json`; `apply_ramp(palette, material_name, face)` maps face → bright/mid/dark; `PaletteKeyError(KeyError)` on missing material → CLI exit 2 per exploration §10. `compose_sprite` loads palette once per sprite, passes dict + raw material str into every primitive. Primitives `iso_cube` + `iso_prism` switch signature from `material: RGBTuple` → `material: str, palette: dict`; inline `_ramp` helpers dropped (palette stores pre-computed bright/mid/dark). Drops `_MATERIAL_STUB` / `_MATERIAL_FALLBACK` / `_resolve_material` from compose. Missing palette file propagates as `FileNotFoundError` → generic exit 1 (distinct from exit 2 missing-material). Decision Log — merge T1.3.3+T1.3.4 (dead-code hazard if split); `PaletteKeyError(KeyError)` subclass over custom base; programmer-error `KeyError` on bad face slot; CLI `_MATERIAL_FAMILIES` variant swap left in place (orthogonal to ramp).
  - Acceptance: `render building_residential_small` produces PNGs using palette RGBs (no stub reds); missing material → exit 2 + stderr; `pytest tools/sprite-gen/tests/` green; `npm run validate:all` green.
  - Depends on: **TECH-153** (archived), **TECH-154** (archived), **TECH-147** (archived)

- [x] **TECH-159** — BlipBaker core + render path (Stage 2.1 Phase 1) (2026-04-15)
  - Type: infrastructure / audio baking
  - Files: `Assets/Scripts/Audio/Blip/BlipBaker.cs`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 2.1 Phase 1 opener. Plain class (not MonoBehaviour) at `Assets/Scripts/Audio/Blip/BlipBaker.cs`. `BakeOrGet(in BlipPatchFlat patch, int patchHash, int variantIndex) → AudioClip`. `sampleRate` is baker ctor param (default `AudioSettings.outputSampleRate`) — not a flat field. `patchHash` passed per-call (flat struct defers hash per Stage 1.2). Main-thread assert at entry via `BlipBootstrap.MainThreadId`; this task also lands the minimal static prop + `Awake` capture for Stage 2.3 T2.3.1 to reuse. Render path: `lengthSamples = (int)(patch.durationSeconds * _sampleRate)`, `float[]` alloc, default `BlipVoiceState`, `BlipVoice.Render(...)`, wrap via `AudioClip.Create(name, lengthSamples, 1, _sampleRate, stream: false)` + `clip.SetData(buffer, 0)`. Cache hit/miss dispatch deferred to follow-up (bake-key + LRU). Invariants #3 + #4 — no `FindObjectOfType`, no singleton; instance owned by `BlipCatalog` (Stage 2.2). Decision Log — plain class over MonoBehaviour (no scene state); non-streaming clip (<1 s buffer in memory); `sampleRate` ctor param over flat field (Stage 1.2 already archived) + over per-call param (keeps cache key `(patchHash, variantIndex)` only); `patchHash` explicit arg over flat-field read (flat defers hash; SO holds `.PatchHash`); `BlipBootstrap.MainThreadId` landed here vs Stage 2.3 T2.3.1 (baker needs the accessor first).
  - Acceptance: `BlipBaker.BakeOrGet` returns non-null `AudioClip` w/ `.samples == lengthSamples`, `.channels == 1`, `.frequency == sampleRate`; clip name matches `Blip_{patchHash:X8}_v{variantIndex}`; main-thread assert throws `InvalidOperationException` on background-thread invocation; `unity:compile-check` + `validate:all` green.
  - Depends on: Step 1 Stage 1.2 + 1.3 (archived — `BlipPatchFlat`, `BlipVoice.Render`, `BlipVoiceState`)

- [x] **TECH-154** — Palette extract CLI command (Stage 1.3 Phase 1) (2026-04-15)
  - Type: CLI / tooling
  - Files: `tools/sprite-gen/src/cli.py`, `tools/sprite-gen/src/palette.py`
  - Spec: (removed at closeout — journal persist skipped, no Lessons/Decision body captured by heuristic)
  - Notes: Stage 1.3 Phase 1 closer. `palette extract {class} --sources "glob_pattern"` subcommand in existing argparse `cli.py`. Expand glob to `list[Path]`, call `extract_palette` (TECH-153, archived), print each cluster's swatch using ANSI 24-bit true-color block, prompt `stdin` for material name per cluster, write named result to `tools/sprite-gen/palettes/{class}.json`. JSON schema: `{"class": str, "materials": {name: {bright, mid, dark}}}` — `centroid` dropped (consumer needs ramp only). Non-TTY fallback: `--names "a,b,c,..."` comma list. Decision Log — drop `centroid` from persisted JSON (exploration §6 ramp-only contract); out dir under `_TOOL_ROOT/palettes/` (matches `_SPECS_DIR`/`_OUT_DIR` convention); hard error on name/cluster count mismatch (fail fast over silent truncate); non-TTY without `--names` → exit 1 (prevents CI hang on closed stdin).
  - Acceptance: interactive run writes valid `palettes/{class}.json`; non-TTY `--names` path works without stdin; `cli.py palette extract residential --sources "Assets/Sprites/Residential/House1-64.png" --names "wall_brick_red,roof_tile_brown,window_glass,concrete,trim,shadow,highlight,mortar"` produces 8-material JSON; `npm run validate:all` green.
  - Depends on: **TECH-153** (archived)

- [x] **TECH-141** — Blip no-alloc regression test (2026-04-15)
  - Type: test / performance regression
  - Files: `Assets/Tests/EditMode/Audio/BlipNoAllocTests.cs`
  - Spec: (removed at closeout — journal persist failed, db_error)
  - Notes: Stage 1.4 T1.4.5 closeout — locks in Step 1 zero-alloc invariant. `Render_SteadyState_ZeroManagedAlloc` — warm-up 3 renders then measure `GC.GetAllocatedBytesForCurrentThread` delta across 10 steady-state `BlipVoice.Render` calls; assert delta ≤ 0 bytes (tolerates GC reclaim within window). Decision Log — `≤ 0` tolerance over `== 0` (Editor JIT inlining flips delta negative occasionally); warm-up = 3 renders (covers JIT + first-call lazy init + Editor instrumentation one-shots); measure window = 10 renders (amortizes noise, < 1 s runtime); `BuildPatch` helper inlined (extract to `BlipTestFixtures` when third sibling drifts — current three `Determinism/Envelope/NoAlloc` share recipe inline). Reuses `BlipTestFixtures.RenderPatch` (TECH-137). Satisfies Stage 1.4 Exit bullet 7.
  - Acceptance: no-alloc test passes; `unity:compile-check` + `validate:all` green.
  - Depends on: **TECH-137** (archived)

- [x] **TECH-153** — K-means palette extractor library (Stage 1.3 Phase 1) (2026-04-15)
  - Type: infrastructure / palette pipeline
  - Files: `tools/sprite-gen/src/palette.py`, `tools/sprite-gen/requirements.txt`
  - Spec: (removed at closeout — journal persist skipped, db_error)
  - Notes: Stage 1.3 Phase 1 opener. `extract_palette(cls, source_paths, n_clusters=8, alpha_threshold=32, seed=42) -> dict` — Pillow RGBA load, alpha mask, `scipy.cluster.vq.kmeans2(minit='++', seed=seed)`, HSV ramp (V ×1.2 / ×1.0 / ×0.6 clamped [0,255]). Decision Log — sort centroids by HSV V descending for stable `cluster_idx` across runs (kmeans2 native ordering non-deterministic); pass `seed` as int (forward-compat older scipy); ramp math from exploration doc §6; raise `ValueError` on empty stack or `N < n_clusters`. Pure library — no filesystem writes, no stdin. Human naming lives in **TECH-154** CLI.
  - Acceptance: `extract_palette('residential', [House1-64.png], 8)` returns 8 clusters w/ 3-level ramp; ramp clamp preserves 0–255; deterministic across two runs; alpha-0 ignored; `pytest tools/sprite-gen/tests/test_palette.py` green.
  - Depends on: **TECH-124** (archived)

- [x] **TECH-152** — Stage 1.2 integration smoke test (Stage 1.2 Phase 3) (2026-04-15)
  - Type: test / integration
  - Files: `tools/sprite-gen/tests/test_render_integration.py`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.2 Phase 3 closeout. End-to-end smoke — `subprocess.run([sys.executable, "-m", "src", "render", "building_residential_small"], cwd=tool_root)`; asserts `returncode == 0`, 4 variant PNGs `_v01`…`_v04` present under real `out/` dir, PIL opens each, `.size == (64, 64)`. Pre-clean fixture deletes only `building_residential_small_v*.png` glob (leaves neighbor archetype artifacts intact). Decision Log — subprocess over in-process (covers `__main__` + argparse entry; in-process `test_cli.py` misses CLI layer); real `out/` + targeted glob-clean over `tmp_path` (CLI `_OUT_DIR` constant is tool-root-anchored; subprocess cannot see pytest monkeypatch — `--out` flag would need CLI refactor, out of scope); module invoked as `-m src` (matches archived `test_cli.test_module_help` convention; `__main__.py` under `src/`); `sys.executable` over hardcoded `"python"` (venv portability); `pytest.importorskip("PIL")` + missing-spec skip guard (defensive — both deps already archived). Locks Layer 2 contract (CLI → loader → compose → PNG) before Stage 1.3 palette work.
  - Acceptance: `pytest tools/sprite-gen/tests/test_render_integration.py` exits 0; 4 variant PNGs verified at `(64, 64)`; `npm run validate:all` green.
  - Depends on: **TECH-149** (archived), **TECH-151** (archived)

- [x] **TECH-151** — First archetype YAML `building_residential_small.yaml` (Stage 1.2 Phase 3) (2026-04-14)
  - Type: content / spec YAML
  - Files: `tools/sprite-gen/specs/building_residential_small.yaml`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.2 Phase 3 opener. First archetype YAML — `id: building_residential_small_v1`, `class: residential`, `footprint: [1,1]`, `terrain: flat`, `levels: 2`, `seed: 42`, `variants: 4`. Composition: `iso_cube × 2` (wall_brick_red, stacked via `offset_z`) + `iso_prism` (roof_tile_brown, pitch=0.5, axis=ns, `offset_z: 32`). `palette: residential` (stub material names → RGB fallback until Stage 1.3 palette JSON lands). `diffusion.enabled: false`. Canvas `(64, 64)` via `canvas_size(1, 1, extra_h=44)` + min-64 clamp in `compose.py`. Decision Log — 2 stacked cubes over single tall cube (exercises `offset_z` path); `offset_z:` key over `z:` (matches archived `compose.py` signature); `variants:` under `output:` block (matches exploration §8); drop `x0/y0` from composition entries (composer derives SE-corner anchor from footprint); `h` values sized for 64-px canvas (two 16-px half-levels + 12-px roof fits clamp).
  - Acceptance: YAML validates via **TECH-148** loader; `render building_residential_small` produces 4 variant PNGs at `(64, 64)`; `npm run validate:all` green.
  - Depends on: **TECH-147** (archived), **TECH-148** (archived)

- [x] **TECH-140** — Blip determinism test (2026-04-14)
  - Type: test / DSP verification
  - Files: `Assets/Tests/EditMode/Audio/BlipDeterminismTests.cs`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.4 T1.4.4. One `[Test]` `RenderPatch_SameSeedVariant_ProducesDeterministicBuffer` — builds `BlipPatch` SO via `BuildPatch()` helper (sine osc, AHDSR 50/0/100/0.5/50 ms, `deterministic = true`, non-zero jitter params to prove deterministic path bypasses them, tracked in `_createdSo` + `TearDown` `DestroyImmediate`), `ToFlat()`, two `BlipTestFixtures.RenderPatch(in patch, 48000, 1, variantIndex: 0)` calls (fixture allocates fresh `BlipVoiceState` per call). Asserts `Math.Abs(SumAbsHash(bufA) - SumAbsHash(bufB)) < 1e-6` + indexed first-256-sample `Is.EqualTo` loop (no Linq alloc). Decision Log — hybrid sum-of-abs + first-256 sample equality (catches deep drift via hash + early state leak via prefix; avoids JIT-LSB brittleness of full-buffer byte-equal); pin `deterministic = true` path (jitter-free branch exercises canonical reset `rngState = variantIndex + 1` w/o seed-XOR confounders); single `variantIndex = 0` (non-goal §2.2 excludes cross-variant determinism). Satisfies Stage 1.4 Exit bullet 6.
  - Acceptance: determinism test passes; `unity:compile-check` + `validate:all` green.
  - Depends on: **TECH-137** (archived)

- [x] **TECH-150** — `render --all` + `--terrain` CLI flag (Stage 1.2 Phase 2) (2026-04-14)
  - Type: infrastructure / CLI
  - Files: `tools/sprite-gen/src/cli.py`, `tools/sprite-gen/tests/test_cli.py`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.2 Phase 2 second task. Refactored `_cmd_render` body into `_render_one(archetype, terrain_override) → int` reusable helper; `_cmd_render` becomes thin dispatcher on `args.all` xor positional `args.archetype` (argparse mutually-exclusive group, required=True). `--all` globs `sorted(_SPECS_DIR.glob("*.yaml"))` (deterministic CI log order), iterates `_render_one`, collects failed stems, prints `failed: [name1, name2]` to stderr only when non-empty, returns 0 iff list empty else 1. `--terrain {slope_id}` flag w/ argparse `choices=sorted(_VALID_SLOPE_IDS)` (18 entries: `flat` + 17 land variants matching **Slope variant naming** glossary); when `terrain_override is not None` overrides `spec['terrain']` pre-compose. Stage 1.2 compose guard — when `spec['terrain'] != 'flat'` post-override raise `NotImplementedError("slope-aware foundation lands Stage 1.4")` caught in `_render_one` → stderr message → return 1. Decision Log — argparse `choices=` (exit 2 on bad enum) over custom `type=` callable (stdlib idiom); serial loop over `multiprocessing` (15-archetype scope trivial); `NotImplementedError` raise over silent flat fallthrough (hides bug). 5 new pytest cases (`test_render_all`, `test_render_all_aggregate`, `test_terrain_bad_enum`, `test_terrain_flat_override`, `test_terrain_non_flat_not_implemented`).
  - Acceptance: `render --all` iterates all `specs/*.yaml`; aggregate exit code reflects any failures; `--terrain flat` accepted; `npm run validate:all` green.
  - Depends on: **TECH-149** (archived)

- [x] **TECH-139** — Blip envelope shape + silence tests (2026-04-14)
- [x] **TECH-146** — `/design` review route + web README §Tokens (Stage 1.2 Phase 3) (2026-04-14)
  - Type: IA / tooling (web workspace) / docs
  - Files: `web/app/design/page.tsx`, `web/README.md`
  - Spec: (removed at closeout — journal persist attempted, db_error logged)
  - Notes: Closes Stage 1.2. `web/app/design/page.tsx` SSR-only renders all six primitives (DataTable, BadgeChip, StatBar, FilterChips, HeatmapCell, AnnotatedMap) w/ 2–3 fixture variants each; sections keyed `#datatable`/`#badgechip`/`#statbar`/`#filterchips`/`#heatmapcell`/`#annotatedmap`; inline fixtures at module scope (no client fetch). Internal-review banner in `<header>` (caveman prose — internal-facing, exception scope covers only public `web/content/**` + page-body strings). `web/README.md` §Tokens documents file layout (`palette.json` raw + semantic, `type-scale.json`, `spacing.json`), `{raw.<key>}` indirection resolved by `resolveAlias` in `web/lib/tokens/index.ts`, Unity UI/UX consumption stub (read JSON at build → map semantic keys to `UnityEngine.Color` / `Vector2`). Decision Log — SSR-only page (no client variant picker); banner stays caveman; alias contract documented as-is (no schema change); glossary row "Web design token set" deferred per orchestrator Exit bullet 5 until Step 3 dashboard stabilizes tokens.
  - Acceptance: `/design` reachable on dev + deploy; all six primitives rendered; README §Tokens present; internal-review banner visible; `npm run validate:all` green.
  - Depends on: tokens + DataTable + BadgeChip + StatBar + FilterChips + HeatmapCell + AnnotatedMap (all archived)

---

## Completed (moved from BACKLOG.md, 2026-04-15)

- [x] **TECH-217** — EconomyManager money earn/spend Blip call sites (Stage 3.2 Phase 2) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/EconomyManager.cs`
  - Spec: (removed at closeout — journal persisted Decision Log)
  - Notes: `AddMoney` fires `BlipId.EcoMoneyEarned` after `cityStats.AddMoney(amount)` gated on `amount > 0`. `SpendMoney` success branch fires `BlipId.EcoMoneySpent` after `cityStats.RemoveMoney(amount)` gated on existing `notifyInsufficientFunds` flag — `ChargeMonthlyMaintenance` (passes `false`) stays silent. No new fields / no new singletons (invariant #4).
  - Acceptance: interactive earn + spend fire SFX; monthly maintenance silent; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-216** — MainMenuController UiButtonHover call sites (Stage 3.2 Phase 1) (2026-04-15)
  - Type: feature wiring / audio integration
  - Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs`
  - Spec: (removed at closeout — journal persisted Decision Log)
  - Notes: Added `AddHoverBlip(Button)` + `WireHoverBlips()` private helpers; programmatic `EventTrigger` `PointerEnter` entry fires `BlipEngine.Play(BlipId.UiButtonHover)` on each of 6 MainMenu buttons. Single call site in `Start()` post-branch covers both `BuildUI()` + `WireExistingUI()` paths. No new fields; cooldown owned by `BlipCooldownRegistry` via patch SO.
  - Acceptance: 6 buttons wired; `npm run unity:compile-check` green; `npm run validate:all` green.
  - Depends on: **TECH-215** (archived — soft, same file)

- [x] **TECH-213** — Legacy `docs/progress.html` live dashboard banner link (2026-04-15)
  - Type: web (docs / legacy handoff)
  - Files: `tools/progress-tracker/render.mjs`, `docs/progress.html`
  - Spec: (removed at closeout — journal persisted Decision Log; banner template edit in `render.mjs`)
  - Notes: Stage 3.3 Phase 1 / T3.3.1. Inserted inline-styled banner `<div>` in `render.mjs` template immediately after `<body>` before `${header}`; regen via `npm run progress` wrote updated `docs/progress.html`. Href exact `https://web-nine-wheat-35.vercel.app/dashboard`. Decision Log — edited `render.mjs` template (not hand-patched HTML) to survive regen; banner stays passive link (no auto-redirect) pending TECH-214 deprecation trigger.
  - Acceptance: banner visible at top of generated `docs/progress.html`; href exact; inline style only; deterministic regen; `validate:all` green.
  - Depends on: **TECH-208** (archived — dashboard access gate)

- [x] **TECH-206** — Dashboard step/stage visual hierarchy + statusDetail rendering (Stage 3.2 Phase 1 / T3.2.2) (2026-04-15)
  - Type: web (RSC layout)
  - Files: `web/app/dashboard/page.tsx`, `web/app/dashboard/_status.ts`
  - Spec: (removed at closeout — journal persist skipped empty sections; decisions inline in Notes)
  - Notes: Extended `/dashboard` RSC w/ project-hierarchy grouping — each plan section iterates `plan.steps` → step heading (`Step {id} — {title}` + `BadgeChip` via `toBadgeStatus`), then per-stage sub-heading (`Stage {id} — {title}` + badge), then `DataTable<TaskRow>` scoped to `stage.tasks`. `step.statusDetail` + `stage.statusDetail` rendered in `text-text-muted` when non-empty; omitted when empty string. No `"use client"`. Decision Log — per-stage `DataTable` vs single table w/ `groupHeader` slot: kept `DataTable` signature stable; reused `toBadgeStatus` (`HierarchyStatus` already covered); omit empty `statusDetail` span to avoid DOM whitespace.
  - Acceptance: step + stage hierarchy scannable; `HierarchyStatus` badges rendered; `validate:all` + `validate:web` green.
  - Depends on: **TECH-205** (archived)

- [x] **TECH-201** — Plan-loader implementation (Stage 3.1 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/lib/plan-loader.ts` (new)
  - Spec: (removed at closeout — journal persist skipped empty sections; Decision Log captured in Notes)
  - Notes: `loadAllPlans(): Promise<PlanData[]>` — globs `ia/projects/*-master-plan.md` from repo root via `fs.promises` (cwd-aware: repo root vs `web/`, mirror Stage 2.1/2.3 loader `resolveContentPath` idiom); reads files; dynamic `import('../../tools/progress-tracker/parse.mjs')` → `parseMasterPlan(content, filename)` passes basename (matches CLI `index.mjs` line 53); returns sorted `PlanData[]`. `parse.mjs` byte-identical — wrapper-only invariant. Decision Log — filter idiom `includes('master-plan') && endsWith('.md')` mirrors `index.mjs` lines 39–42 verbatim to stay drift-free w/ CLI; filename arg = basename (`PlanData` consumers key off basename for sibling-warning match); empty-dir returns `[]` (diverges from CLI exit-1 — RSC prefers graceful empty render, documented divergence); no caching in v1 (Node ESM module cache dedupes `parse.mjs`; file-content memo deferred until profiling justifies).
  - Acceptance: `loadAllPlans()` exported + typed; `git diff tools/progress-tracker/parse.mjs` empty; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-200** (archived)

- [x] **TECH-193** — Devlog single-post RSC + origin-story MDX seed (Stage 2.3 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace / content
  - Files: `web/app/devlog/[slug]/page.tsx` (new), `web/content/devlog/2026-MM-DD-origin-story.mdx` (new)
  - Spec: (removed at closeout — journal persist `ok`, both sections empty)
  - Notes: Single-post RSC resolves slug via `loadDevlogPost(slug)` (new loader sibling — accepts `DevlogFrontmatter` w/o `PageFrontmatter` validator); renders title + tag chips + read-time + optional cover + compiled MDX body. `generateMetadata` returns `openGraph.images` from `cover` or `/og-default.png` fallback. `generateStaticParams` fs-scans `web/content/devlog/*.mdx`. Decision Log — used `@mdx-js/mdx` `evaluate()` over dynamic `import()` (webpack template-literal constraint + Turbopack SSG compat); `@mdx-js/mdx` hoisted via npm workspace; created 1x1 white PNG placeholder at `web/public/og-default.png` (real OG art deferred).
  - Acceptance: `/devlog/2026-04-15-origin-story` renders cover (or fallback) + tags + read-time + MDX body; OG metadata valid; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: **TECH-192** (archived)

- [x] **TECH-192** — Devlog list route + reading-time helper (Stage 2.3 Phase 1) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/app/devlog/page.tsx` (new), `web/lib/mdx/reading-time.ts` (new), `web/lib/mdx/types.ts` (extend)
  - Spec: (removed at closeout — journal persist `ok`, both sections empty)
  - Notes: RSC scans `web/content/devlog/*.mdx`, parses frontmatter (`title`, `date`, `tags[]`, `cover?`, `excerpt`), sorts desc by `date`, renders card list w/ `BadgeChip` tags + read-time + excerpt. `computeReadingTime(body): number` helper — minutes rounded up from word count (~200 wpm baseline). Seeds devlog surface consumed by **TECH-193**/**TECH-194**/**TECH-195**. Decision Log — direct `gray-matter` over extending `loadMdxContent` (validator hard-codes `PageFrontmatter` fields); 200 wpm baseline + floor-1 minute; rich OG deferred to **TECH-193**/**TECH-195**.
  - Acceptance: `/devlog` renders sorted card list w/ tag chips + read-time + excerpt; `DevlogFrontmatter` type exported; `npm run validate:web` + `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-173** — `BlipPlayer` pool construction (Stage 2.2 Phase 3) (2026-04-15)
  - Type: infrastructure / audio runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipPlayer.cs`, `Assets/Scripts/Audio/Blip/BlipEngine.cs`
  - Spec: (removed at closeout — journal persist `ok`, both sections empty)
  - Notes: New `BlipPlayer : MonoBehaviour` w/ `[SerializeField] private int poolSize = 16`. `Awake` spawns 16 child GameObjects (`BlipVoice_0..BlipVoice_15`) each carrying `AudioSource` (`playOnAwake = false`, `loop = false`). Holds `AudioSource[] _pool` + `int _cursor = 0`. Calls `BlipEngine.Bind(this)` at `Awake` end; `OnDestroy` → `Unbind(this)`. Added `Bind(BlipPlayer)` / `Unbind(BlipPlayer)` no-op stubs on `BlipEngine` (body fills Stage 2.3 T2.3.2). Placed as child of `BlipBootstrap` prefab. Invariant #3 + #4 satisfied. Decision Log — pool size as `[SerializeField]` not const (authoring knob); stubs land here (T2.2.2 only added Catalog pair); `OnDestroy` pairs `Bind`/`Unbind` mirrors Catalog contract.
  - Acceptance: 16 child GameObjects spawn w/ configured `AudioSource`; `_pool` populated + `_cursor = 0`; `Bind` stub called; `unity:compile-check` + `validate:all` green.
  - Depends on: **TECH-170** (archived)

- [x] **TECH-161** — LRU ordering + access tracking (Stage 2.1 Phase 2) (2026-04-15)
  - Type: infrastructure / cache
  - Files: `Assets/Scripts/Audio/Blip/BlipBaker.cs`, `Assets/Tests/EditMode/Audio/BlipBakerCacheTests.cs`
  - Spec: (removed at closeout — journal persist attempted, db_error logged)
  - Notes: Stage 2.1 Phase 2 opener. Extended `BlipBakeEntry` with `long byteCount` (value writes deferred to **TECH-162**). Added private `AddAtTail(BlipBakeEntry) → LinkedListNode<BlipBakeEntry>` DRY wrapper; refactored `BakeOrGet` miss-path insert. Added `internal bool TryEvictHead()` — `RemoveFirst` + `_index.Remove` + `Object.Destroy(clip)` + return `true`; empty → `false`. Consumed by **TECH-162** budget loop. Decision Log — `bool` return (caller guard) over throw; `Object.Destroy` (Play Mode safe) over `DestroyImmediate`; add `byteCount` field here (struct-shape in one commit) over deferring to TECH-162; `AddAtTail` private (no test need) over internal. `InternalsVisibleTo("Blip.Tests.EditMode")` already wired in `Assets/Scripts/AssemblyInfo.cs`.
  - Acceptance: insert / hit / evict-head sequence maintains head-oldest / tail-newest ordering; `TryEvictHead` on empty returns `false`; `unity:compile-check` + `validate:all` green.
  - Depends on: **TECH-160** (archived)

---

## Completed (moved from BACKLOG.md, 2026-04-14)

- [x] **TECH-149** — `render {archetype}` CLI command (Stage 1.2 Phase 2) (2026-04-14)
  - Type: infrastructure / CLI
  - Files: `tools/sprite-gen/src/cli.py`, `tools/sprite-gen/src/__main__.py`, `tools/sprite-gen/tests/test_cli.py`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.2 Phase 2 opener. `python -m sprite_gen render {archetype}` — resolves `specs/{archetype}.yaml` cwd-independent via `Path(__file__).resolve().parent.parent / "specs"`, loads + validates via `load_spec` (**TECH-148** archived), iterates `range(spec['output'].get('variants', 1))`, applies `apply_variant(spec, idx)` deepcopy + seeded `random.Random(spec.get('seed', 0) + idx)` permutation (material swap within inline family map, prism pitch × `rng.uniform(0.8, 1.2)` clamped `[0, 1]`), calls `compose_sprite` (**TECH-147** archived), writes `out/{spec['output']['name']}_v{idx+1:02d}.png`. `main(argv=None) → int` returns exit code; `__main__.py` two-liner wraps `SystemExit(main())` for fast pytest without subprocess. Decision Log — argparse over click (stdlib, no new dep); variant count reads `spec['output']['variants']` (not top-level) matching TECH-148 schema; output name from `spec['output']['name']` (not `id`, which carries `_v1` suffix); inline material-family swap map temporary until Stage 1.3 palette class metadata lands (**TECH-153**); `main()` returns int over `sys.exit` inside — enables direct-call pytest.
  - Acceptance: `python -m sprite_gen render building_residential_small` writes N PNGs to `out/`; exit 0 success, 1 on missing archetype / `yaml.YAMLError` / `SpecValidationError`; deterministic bytes across same-seed runs; `npm run validate:all` green.
  - Depends on: **TECH-147** (archived), **TECH-148** (archived)

- [x] **TECH-138** — Blip oscillator zero-crossing tests (2026-04-14)
  - Type: test / DSP verification
  - Files: `Assets/Tests/EditMode/Audio/BlipOscillatorTests.cs`
  - Spec: (removed at closeout — journal persist attempted, db_error logged)
  - Notes: Stage 1.4 T1.4.2. Four `[Test]` methods — sine / triangle / square / pulse duty=0.5 @ 440 Hz × 1 s @ 48 kHz ≈ 880 crossings (± 2). Patch built via `ScriptableObject.CreateInstance<BlipPatch>()` + reflection on serialized fields → `BlipPatchFlat.FromSO`; envelope `A=1 ms / H=2000 ms / D=0 / S=1 / R=1 ms` keeps render in hold for full 1 s; `BlipFilter.kind = None`; `deterministic = true`, `variantIndex = 0`. Decision Log — exclude noise osc (no deterministic crossing target); reflection route keeps `BlipPatchFlat` blittable surface read-only (no test-only ctor); hold ≫ render duration so 1-ms ramp stays negligible vs ± 2 tolerance. Satisfies Stage 1.4 Exit bullet 3.
  - Acceptance: all four tests pass; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-137** (archived)

- [x] **TECH-148** — YAML spec loader + validator (Stage 1.2 Phase 1) (2026-04-14)
  - Type: infrastructure / YAML schema
  - Files: `tools/sprite-gen/src/spec.py`, `tools/sprite-gen/tests/test_spec.py`, `tools/sprite-gen/tests/fixtures/spec_valid.yaml`, `tools/sprite-gen/tests/fixtures/spec_malformed.yaml`
  - Spec: (removed at closeout — journal persist skipped, db_unconfigured)
  - Notes: Stage 1.2 Phase 1 second task. `load_spec(path) → dict` — loads YAML via `yaml.safe_load`, validates required keys (`id`, `class`, `footprint`, `terrain`, `composition`, `palette`, `output`) via flat `REQUIRED_KEYS` table; `SpecValidationError(field=...)` raised on missing / wrong-typed key; `footprint` 2-int shape check; `composition` non-empty list-of-dicts-with-`type` check; optional fields (`levels`, `seed`, `variants`, `diffusion`) round-trip un-validated; `yaml.YAMLError` bubbles for parse failures (CLI maps both to exit 1). Decision Log — flat required-key table over Pydantic (minimal deps, small schema); pass-through optional fields (keeps loader stable while Stages 1.3 / 1.4 add palette/slope/diffusion semantics); distinct `SpecValidationError` vs `yaml.YAMLError` (preserves parse-line info). 22 pytest cases green.
  - Acceptance: `load_spec(valid)` → dict; missing key → `SpecValidationError` w/ `field`; malformed YAML → `yaml.YAMLError`; `npm run validate:all` green.
  - Depends on: none

- [x] **TECH-144** — Web primitives: StatBar + FilterChips (Stage 1.2 Phase 2) (2026-04-14)
  - Type: IA / tooling (web workspace)
  - Files: `web/components/StatBar.tsx`, `web/components/FilterChips.tsx`
  - Spec: (removed at closeout — journal persist skipped, no output)
  - Notes: SSR-only primitives — no `"use client"`. StatBar: `label` + `value` + `max` + optional `thresholds: { warn, critical }`; `TIER_FILL` dispatch → `bg-panel` (default) / `bg-[var(--color-text-accent-warn)]` (warn) / `bg-[var(--color-text-accent-critical)]` (critical); tier resolves off raw `value` (over-max still flags critical); `pct` clamped [0,100] guards divide-by-zero on `max ≤ 0`. FilterChips: `chips: { label, active }[]` row, no `onClick` (Step 3 wires query-param toggle), `active` → `bg-panel` + `text-primary` vs `bg-canvas` + `text-muted`. Decision Log: reuse `text-accent-*` hex via arbitrary `bg-[var(--color-…)]` utilities (no new `bg-accent-*` palette aliases until ≥2 consumers); SSR-only lock (no premature `"use client"` boundary); raw-value tier semantics (absolute thresholds, not normalized). Second pair of six Stage 1.2 primitives; consumed by Step 3 dashboard.
  - Acceptance: both files present; no `"use client"`; `cd web && npm run build` green; `npm run validate:all` green.
  - Depends on: tokens (archived — see this file Completed 2026-04-14)

- [x] **TECH-147** — Compose layer `compose_sprite(spec)` (Stage 1.2 Phase 1) (2026-04-14)
  - Type: infrastructure / rendering pipeline
  - Files: `tools/sprite-gen/src/compose.py`, `tools/sprite-gen/src/primitives/__init__.py`, `tools/sprite-gen/tests/test_compose.py`
  - Spec: (removed at closeout — journal persist attempted, db_error logged)
  - Notes: Stage 1.2 Phase 1 opener. `compose_sprite(spec: dict) → PIL.Image` — canvas via `canvas_size(fx, fy, extra_h)` clamped min 64 px; iterates `composition:` list; dispatch dict `{'iso_cube','iso_prism'}` resolves `type:` key; `UnknownPrimitiveError` on unknown; `extra_h = max(h + offset_z)` over entries; origin = footprint SE corner (y-down) matching TECH-125/126 `_project` convention; material stays stub RGB until Stage 1.3 palette. Wires **TECH-125** / **TECH-126** into Layer 2 of the 5-layer composer per exploration §3. Decision Log — dispatch dict (extensible for Stage 1.4 foundation); `max(h+offset_z)` not sum (stacks); composer owns min-canvas-h clamp; SE-corner origin; stub material dict. Four pytest contracts in `test_compose.py` (canvas size, composition order, unknown primitive, min canvas clamp).
  - Acceptance: `compose_sprite(sample_spec)` returns PIL.Image w/ canvas size matching `canvas_size(fx, fy, extra_h)`; primitives stacked in order; `npm run validate:all` green.
  - Depends on: **TECH-125**, **TECH-126** (archived)

- [x] **TECH-137** — Blip EditMode test asmdef + fixture helpers bootstrap (2026-04-14)
  - Type: test / infrastructure
  - Files: `Assets/Tests/EditMode/Audio/Blip.Tests.EditMode.asmdef`, `Assets/Tests/EditMode/Audio/BlipTestFixtures.cs`
  - Spec: (removed at closeout — journal persist attempted)
  - Notes: Opened Stage 1.4 Phase 1. Editor-only asmdef refs default `TerritoryDeveloper.Game` asmdef (Blip runtime `Territory.Audio` lives there) + `optionalUnityReferences: ["TestAssemblies"]` (auto-supplies `UnityEngine.TestRunner` + `nunit.framework.dll`). Helpers static class `BlipTestFixtures` — `RenderPatch`, `CountZeroCrossings` (skip-zero), `SampleEnvelopeLevels` (abs-value stride), `SumAbsHash`. Consolidated former T1.4.1 + T1.4.2 per stage compress. Decision Log — reference `TerritoryDeveloper.Game` by name (not GUID, not carve-out `Blip.asmdef`); rectified envelope stride sample for monotonicity; skip-zero crossings to hit deterministic ≈ 880 @ 440 Hz × 1 s × 48 kHz.
  - Acceptance: asmdef present + compiles; four helpers exposed; `npm run unity:compile-check` + `npm run validate:all` green.
  - Depends on: none (Stage 1.3 runtime already closed)

- [x] **TECH-143** — Web primitives: DataTable + BadgeChip (Stage 1.2 Phase 2) (2026-04-14)
  - Type: IA / tooling (web workspace)
  - Files: `web/components/DataTable.tsx`, `web/components/BadgeChip.tsx`, `web/lib/tokens/palette.json`, `web/app/globals.css`
  - Spec: (removed at closeout — journal persist attempted)
  - Notes: SSR-only primitives — no `"use client"`. DataTable typed generic `<T,>` w/ `Column<T>` + `statusCell?: (row: T) => ReactNode` slot; sortable header via `aria-sort` only (no onClick). BadgeChip 4-status enum → `bg-status-*` + `text-status-*-fg` semantic aliases (Phase 1 prereq extended palette JSON + `@theme` w/ new `raw.green`). Decision Log: SSR-only lock, aria-sort-only sortable contract, semantic-alias mandatory (never raw Tailwind colors), `<T,>` trailing-comma generic. First two of six Stage 1.2 primitives; consumed by Step 3 dashboard + Step 2 wiki.
  - Acceptance: both files present; no `"use client"`; palette aliases present; `cd web && npm run build` green; `npm run validate:all` green.
  - Depends on: tokens (archived — see above)

- [x] **TECH-142** — Web design tokens (palette + type + spacing) + Tailwind wiring (Stage 1.2 Phase 1) (2026-04-14)
  - Type: IA / tooling (web workspace)
  - Files: `web/lib/tokens/palette.json`, `web/lib/tokens/type-scale.json`, `web/lib/tokens/spacing.json`, `web/lib/tokens/index.ts`, `web/app/globals.css` (Tailwind v4 `@theme` CSS custom properties replace `tailwind.config.ts`)
  - Spec: (removed at closeout — journal persisted in `ia_project_spec_journal`)
  - Notes: Merged T1.2.1 + T1.2.2 per web master-plan Decision Log 2026-04-14 — tokens + Tailwind wiring shipped together; throwaway `_smoke-tokens` page smoke-verified `bg-canvas` / `text-accent-critical` semantic aliases → expected hex then deleted pre-merge per spec Decision Log. Tailwind v4 realization: `@theme` in `web/app/globals.css` replaces JS config file per v4 migration. NYT-dark-choropleth palette locked; semantic aliases mandatory (consumers never reference raw hex). JSON schema stable for future Unity UI/UX plan. Decision Log migrated via `persist-project-spec-journal` (no Lessons Learned section — tooling-only issue).
  - Acceptance: three JSON files under `web/lib/tokens/`; Tailwind wiring via v4 `@theme`; default create-next-app palette removed; `cd web && npm run build` green + `npm run validate:all` green.
  - Depends on: **TECH-136** (archived — scaffold + validate:all chain)

- [x] **TECH-128** — Primitive smoke tests (pytest + fixture PNGs, iso_cube + iso_prism NS/EW) (2026-04-14)
  - Type: test / infrastructure
  - Files: `tools/sprite-gen/tests/test_primitives.py`, `tools/sprite-gen/tests/fixtures/iso_cube_smoke.png`, `iso_prism_ns_smoke.png`, `iso_prism_ew_smoke.png`
  - Spec: (removed at closeout — journal persisted in `ia_project_spec_journal`)
  - Notes: Closes Stage 1.1. Smoke renders `iso_cube(1,1,32)` + `iso_prism` both axes (pitch=0.5) on `canvas_size(1,1,64)=(64,64)` (canvas-h bumped from 32 → 64 per §9 #1 — top face at h=32 projects above y=0 on 32-tall canvas). Alpha>0 bbox asserts per face; `ValueError` guard locked for bad axis. `iso_prism` re-exported from `src/primitives/__init__.py`. Fixtures tracked in git.
  - Acceptance: `pytest tools/sprite-gen/tests/test_primitives.py` exits 0; 3 fixture PNGs emitted; `npm run validate:all` green.
  - Depends on: **TECH-125**, **TECH-126**

- [x] **TECH-127** — Canvas unit tests (pytest, §4 Examples table) (2026-04-14)
  - Type: test / infrastructure
  - Files: `tools/sprite-gen/tests/test_canvas.py`
  - Spec: (removed at closeout — journal persisted in `ia_project_spec_journal`)
  - Notes: Stage 1.1 Phase 3 opener. Six asserts covering exploration §4 Examples rows — `canvas_size(1,1)=(64,0)`, `canvas_size(1,1,32)=(64,32)`, `canvas_size(3,3,96)=(192,96)`, `pivot_uv(64)=(0.5,0.25)`, `pivot_uv(128)=(0.5,0.125)`, `pivot_uv(192)=(0.5,16/192)`. Plus `pivot_uv(0)` ValueError guard. Manual pytest gate — `npm run validate:all` does NOT yet cover Python (candidate CI fold-in: Stage 1.3 palette tests).
  - Acceptance: `pytest tools/sprite-gen/tests/test_canvas.py` exits 0 (7 passed); all §4 Examples rows covered; `npm run validate:all` green.
  - Depends on: **TECH-124**

- [x] **TECH-126** — `iso_prism` primitive (sloped tops + triangular gables, axis NS/EW) (2026-04-14)
  - Type: infrastructure / rendering primitive
  - Files: `tools/sprite-gen/src/primitives/iso_prism.py`
  - Spec: (removed at closeout — journal persisted in `ia_project_spec_journal`)
  - Notes: Stage 1.1 Phase 2 second task. `iso_prism(canvas, x0, y0, w, d, h, pitch, axis, material)` — two sloped top faces + two triangular end-faces. `axis ∈ {'ns','ew'}` selects ridge direction; `pitch` (0..1) scales ridge height. Same NW-light ramp as **TECH-125**. Enables pitched-roof archetypes in Stage 1.2+ YAML specs.
  - Acceptance: both axes + pitch variants render cleanly; shade ramp matches iso_cube; `npm run validate:all` green
  - Depends on: **TECH-124**

- [x] **TECH-136** — Scaffold `web/` Next.js 14+ workspace (Stage 1.1 consolidated) (2026-04-14)
  - Type: tooling / scaffold / deploy / documentation
  - Files: `package.json` (root — workspaces entry); `web/**` (new subtree — scaffold + README); `web/app/page.tsx`, `web/app/layout.tsx`, `web/tailwind.config.ts`, `web/tsconfig.json`, `web/components/`, `web/lib/`, `web/content/`; `package.json` (root scripts — validate:all extension); `web/package.json` (typecheck script); `.github/workflows/*` (CI verify); `CLAUDE.md` (§Web section); `AGENTS.md` (§Web section)
  - Spec: (removed at closeout — journal persisted in `ia_project_spec_journal`)
  - Notes: Stage 1.1 Phase 1 — whole stage collapses to one landable unit. Supersedes **TECH-129**..**TECH-134** (stage compress, 2026-04-14). Workspaces entry (`"web"` alongside `"tools/*"`); Next.js 14+ App Router w/ TS strict + Tailwind + ESLint via `create-next-app`; placeholder `<h1>Territory Developer</h1>`; stub `components/`, `lib/`, `content/` w/ `.gitkeep`; `npm --prefix web run lint/typecheck/build` folded into `validate:all`; `web/README.md` sections (overview, local dev, build, content conventions, caveman-exception boundary, Vercel URL); `§Web` appended to `CLAUDE.md` + `AGENTS.md`. Vercel link + throwaway-PR CI verify remain as human-action items tracked in `web-platform-master-plan.md` Stage 1.1 Phase 2 (dashboard-only steps; no CLI auth in agent env).
  - Acceptance: `npm install` exits 0; `cd web && npm run build` exits 0; `npm run validate:all` green incl. web/ lint+typecheck+build; `web/README.md` + `CLAUDE.md §Web` + `AGENTS.md §Web` present. Vercel deploy green + URL reachable pending human action.
  - Depends on: none

- [x] **TECH-129** — Root npm **workspaces** add `web/` entry (2026-04-14, superseded)
  - Type: tooling / monorepo wiring
  - Files: `package.json` (root)
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). Over-granular 1-file task folded into consolidated Stage 1.1 unit; scope carried forward.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: none

- [x] **TECH-130** — Next.js 14+ App Router scaffold under `web/` (2026-04-14, superseded)
  - Type: tooling / scaffold
  - Files: `web/**` (new subtree)
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). Scaffold scope carried forward intact into consolidated issue.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: **TECH-129**

- [x] **TECH-131** — Vercel project link + deploy-on-`main` for `web/` (2026-04-14, superseded)
  - Type: tooling / deploy
  - Files: Vercel dashboard; optional `vercel.json`; `web/README.md` (URL capture)
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). Vercel link + URL capture scope folded into consolidated issue.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: **TECH-130**

- [x] **TECH-132** — Fold `web/` lint + typecheck + build into `validate:all` chain (2026-04-14, superseded)
  - Type: tooling / CI
  - Files: `package.json` (root scripts); `web/package.json`; `.github/workflows/*`
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). CI integration scope folded into consolidated issue.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: **TECH-130**

- [x] **TECH-133** — Author `web/README.md` (local dev, content conventions, caveman exception) (2026-04-14, superseded)
  - Type: documentation
  - Files: `web/README.md` (new)
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). README authoring scope folded into consolidated issue.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: **TECH-130**, **TECH-131**

- [x] **TECH-134** — Append `§Web` section to `CLAUDE.md` + `AGENTS.md` (2026-04-14, superseded)
  - Type: documentation / discovery
  - Files: `CLAUDE.md` (root); `AGENTS.md` (root)
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-136** — stage compress (1.1). Repo-docs append scope folded into consolidated issue.
  - Acceptance: superseded — see **TECH-136** Acceptance.
  - Depends on: **TECH-133**

---

## Completed (moved from BACKLOG.md, 2026-04-13)

- [x] **TECH-121** — `BlipVoice.Render` driver (per-sample integrator loop) (2026-04-14, superseded)
  - Type: infrastructure / runtime
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-135** — stage compress (1.3). Merged w/ TECH-122 per-invocation jitter into single consolidated Phase 3 closeout task. Scope folded forward — render driver loop + osc bank + envelope + filter multiply chain. Draft spec never kicked off individually.
  - Acceptance: superseded — see **TECH-135** Acceptance.
  - Depends on: **TECH-116**, **TECH-117**, **TECH-118**, **TECH-119**, **TECH-120**

- [x] **TECH-122** — Per-invocation jitter (pitch cents / gain dB / pan) (2026-04-14, superseded)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`
  - Spec: (removed — superseded)
  - Notes: superseded by **TECH-135** — stage compress (1.3). Merged w/ TECH-121 render driver into single consolidated Phase 3 closeout task. Scope folded forward — pitch cents / gain dB / pan jitter w/ `deterministic` flag + xorshift32 seed from `variantIndex * 0x9E3779B9 ^ voiceId`. Draft spec never kicked off individually.
  - Acceptance: superseded — see **TECH-135** Acceptance.
  - Depends on: **TECH-116**, **TECH-121** (former) → now **TECH-135**

- [x] **TECH-135** — `BlipVoice.Render` driver + per-invocation jitter (consolidated) (2026-04-14)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`, `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`, `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 3 closeout. Consolidates former TECH-121 (render driver loop) + TECH-122 (per-invocation jitter) per stage compress. Lands `BlipVoice.Render` static kernel — per-sample loop (osc × envelope × LP filter → buffer mix-in) + pre-computed per-invocation jitter block (pitch cents → `pow(2, cents/1200)`, gain dB → `pow(10, dB/20)`, pan stashed on state). Honors `deterministic` flag → bypass jitter + fixed seed `(uint)(variantIndex + 1)`. Live path seed mix `(uint)(variantIndex * 0x9E3779B9) ^ state.rngState` w/ `0x9E3779B9` zero-guard (xorshift32 undefined at 0). **Decisions:** extended `BlipVoiceState` w/ `public float panOffset` (caller-scratch rejected — single-source-of-truth DSP state); caller-seeded `state.rngState` as voice-hash input (`patch.patchHash` deferred); pitch-fold Option B — added `BlipOscillatorFlat(in BlipOscillatorFlat src, float detuneCents)` copy constructor so TECH-117 `SampleOsc` signature stays frozen (churn confined to driver); `SampleJitter` helper short-circuits `range == 0f`. Zero managed allocs (all locals stack value types); no Unity API. Shared kernel — `BlipBaker` Step 2 + `BlipLiveHost` post-MVP. Determinism + zero-alloc assertions deferred to Stage 1.4 T1.4.6 / T1.4.7 EditMode tests.
  - Acceptance: signature matches Stage 1.3 Exit; per-sample loop mixes osc × envelope × filter; jitter applied per invocation; no Unity API; `unity:compile-check` + `validate:all` green (141/141).
  - Depends on: **TECH-116**, **TECH-117**, **TECH-118**, **TECH-119**, **TECH-120**

- [x] **TECH-120** — One-pole LP filter inline in `BlipVoice.Render` (2026-04-14)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipVoice.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 3 opener landed inline in `BlipVoice.Render`. α pre-compute outside loop — `kind == LowPass` → `1 - (float)Math.Exp(-2π * cutoffHz / sampleRate)` clamped `[0,1]`; `kind == None` → `1f` literal (no `Math.Exp`). Per-sample recursion `state.filterZ1 += α * (x - state.filterZ1); buffer[i] = state.filterZ1;` — single kernel, branchless, 1 mul + 1 add + 1 store. `ref BlipVoiceState state` threaded via TECH-121 driver; zero per-sample allocs. **Decisions:** α clamp guards `cutoffHz ≥ sampleRate/2` w/o branching on input; passthrough via α = 1 (not `if kind == None`) keeps single kernel matching TECH-121 "no per-sample branches" invariant; narrow `Math.Exp` `double` → `float` once outside loop to avoid repeated widening (state is `float`). Master plan Stage 1.3 T1.3.5 flipped to Done.
  - Acceptance: LP math inline in driver; `None` passthrough branchless; `npm run validate:all` green.
  - Depends on: **TECH-116**

- [x] **BUG-52** — **AUTO** zoning: persistent **grass cells** between **undeveloped light zoning** and new **AUTO** **street** segments (gaps not filled on later **simulation ticks**) (2026-04-14)
  - Type: bug (behavior / regression suspicion)
  - Files: `AutoZoningManager.cs` (`ZoneSegmentStrip`, `ScanRoadFrontierForZoneable`, `SelectZoneTypeForRing`), `ia/specs/simulation-system.md`
  - Spec: (removed after closure)
  - Notes: **Root cause:** segment-driven strip zoning in `AutoZoningManager.ZoneSegmentStrip` skipped endpoints (`k=0` / `k=L-1`); segments popped after single pass; no fallback rescan once **road reservation** (axial corridor + extension cells, geo §13.9 rule 4) relaxed. Ruled out stale **road cache**, `TerrainManager.RestoreTerrainForCell` regression, tick ordering. **Fix A:** extended `k` loop bound to `L-1`, guarded `k=0` for true endpoints (no T-joint double-zone). **Fix B:** added `ScanRoadFrontierForZoneable` post-tick pass iterating `GetRoadEdgePositions()` cardinal neighbors, applying `CanZoneCell` under `MaxZonedCellsPerTickSafetyCap` + **growth budget**. Refactored `SelectZoneTypeForSegment` → `SelectZoneTypeForRing(UrbanRing)` via `urbanCentroidService.GetUrbanRing`. Reservation cells still untouchable per §13.9 invariant. Simulation-system spec updated.
  - Acceptance: endpoint cells covered; historical reservation cells rescanned once freed; `npm run unity:compile-check` exit 0; `npm run validate:all` clean (TECH-119 dead-spec failure pre-existing, unrelated).
  - Depends on: none

- [x] **TECH-125** — `iso_cube` primitive (top + south + east faces, NW-light shade ramp) (2026-04-14)
  - Type: infrastructure / rendering primitive
  - Files: `tools/sprite-gen/src/primitives/iso_cube.py`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 2 opener. `iso_cube(canvas, x0, y0, w, d, h, material)` draws top rhombus (bright) + south parallelogram (mid) + east parallelogram (dark) via Pillow polygons. NW-light hardcoded. Pixel coords from 2:1 iso projection per exploration §5. HSV ramp ×1.2/×1.0/×0.6 per §6.3; origin `(x0, y0)` = footprint SE corner (y-down) to align w/ Pillow + canvas pivot. Material stays stub RGB tuple MVP; palette integration lands Stage 1.3.
  - Acceptance: three faces render w/ distinct bright/mid/dark ramp; signature matches Stage 1.1 Exit; `npm run validate:all` green
  - Depends on: **TECH-124**

- [x] **TECH-124** — `canvas.py` canvas sizing + Unity pivot math (2026-04-14)
  - Type: infrastructure / DSP (geometry)
  - Files: `tools/sprite-gen/src/canvas.py`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 1 second task. `canvas_size(fx, fy, extra_h=0) → (w, h)` per exploration §4 baseline `(fx+fy)*32`; `pivot_uv(canvas_h) → (0.5, 16/canvas_h)`. Pure functions, docstring cites §4. Must match **Tile dimensions** (tileWidth=1, tileHeight=0.5) so emitted PNGs align w/ Unity isometric diamond at PPU=64.
  - Acceptance: both functions match §4 examples; docstrings cite source; `npm run validate:all` green
  - Depends on: **TECH-123**

- [x] **TECH-123** — `tools/sprite-gen/` folder scaffold + `requirements.txt` + README stub (2026-04-14)
  - Type: infrastructure / tooling scaffold
  - Files: `tools/sprite-gen/` (new: `src/__init__.py`, `src/primitives/__init__.py`, `tests/fixtures/`, `out/`, `requirements.txt`, `README.md`), `.gitignore`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 1 opener. Layout per exploration §9: `src/`, `src/primitives/`, `tests/`, `tests/fixtures/`, `specs/`, `palettes/`, `out/` (gitignored). `requirements.txt` pins pillow + numpy + scipy + pyyaml. README stub points at master plan + exploration doc. Python / Unity-isolated — no runtime **C#** touched.
  - Acceptance: folder layout matches §9; `out/` gitignored; `requirements.txt` lists 4 deps; `npm run validate:all` green
  - Depends on: none

- [x] **TECH-118** — AHDSR envelope state machine (Idle → Attack → Hold → Decay → Sustain → Release) (2026-04-14)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipEnvelope.cs` (static class `BlipEnvelopeStepper`)
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 2 opener. Per-sample state-machine step. Converts `attackMs` / `holdMs` / `decayMs` / `releaseMs` → sample counts via `sampleRate * ms / 1000`. Durations already ≥ 1 ms per TECH-113 clamp. `decayMs == 0` → Attack → Hold → Sustain shortcut (sustain-only fallback). MVP release triggered by `samplesElapsed` vs patch `durationSeconds` (one-shot). Stage entry resets `samplesElapsed`. Helper class named `BlipEnvelopeStepper` (not `BlipEnvelope`) to avoid CS0101 collision w/ patch-data struct in `BlipPatchTypes.cs`.
  - Acceptance: six-stage FSM advances correctly; sustain-only case routes cleanly; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-116**

- [x] **TECH-117** — `BlipVoice` oscillator bank (sine / triangle / square / pulse / noise) (2026-04-14)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipOscillatorBank.cs` (or inlined in `BlipVoice.cs` per implementer)
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 1 second task. Phase-accumulator osc family — sine (`Math.Sin` MVP; LUT reserved post-MVP per `docs/blip-post-mvp-extensions.md` §1), triangle (abs-ramp), square, pulse (duty 0..1), noise-white (xorshift on `BlipVoiceState.rngState`). Freq from `BlipOscillatorFlat.frequency * pitchMult`. Pure static per-kind helpers; zero allocs; no Unity API.
  - Acceptance: five osc kinds emit expected shapes (verified Stage 1.4 T1.4.2); `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-116**

- [x] **TECH-116** — `BlipVoiceState` blittable struct (per-voice DSP state) (2026-04-14)
  - Type: infrastructure / runtime data
  - Files: `Assets/Scripts/Audio/Blip/BlipVoiceState.cs`
  - Spec: (removed after closure)
  - Notes: Blip master-plan Stage 1.3 Phase 1 opener (task T1.3.1). `BlipVoiceState` blittable struct in `Territory.Audio` — 9 fields: `phaseA..phaseD` (double phase accumulators, 3 osc slots + LFO reserve), `envLevel` (float 0..1), `envStage` (`BlipEnvStage` reused from TECH-112 / `BlipPatchTypes.cs` — do NOT redeclare), `samplesElapsed` (int since stage entry), `filterZ1` (float one-pole LP memory), `rngState` (uint xorshift32 seed). Public fields, no ctor / properties — kernel mutates via `ref`. Zero managed refs. Default zero-init = Idle / silent. 4th phase slot (phaseD) reserved for LFO / post-MVP modulation (8 bytes padding; avoids struct churn when LFO lands). Caller-owned — lives outside static kernel; feeds TECH-117 (osc bank, writes phaseA..C + rngState) + TECH-118 (AHDSR, writes envStage + samplesElapsed) + TECH-119 (env level, writes envLevel) + TECH-120 (LP, writes filterZ1) + TECH-121 (render driver) + TECH-122 (jitter RNG). Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.3.
  - Acceptance: struct + `BlipEnvStage` enum compile; zero managed refs; `unity:compile-check` + `validate:all` green
  - Depends on: none

- [x] **TECH-115** — `patchHash` content hash on `BlipPatch` + glossary rows (2026-04-14)
  - Type: infrastructure / glossary
  - Files: `Assets/Scripts/Audio/Blip/BlipPatch.cs`, `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Closes Stage 1.2. FNV-1a 32-bit (offset basis `0x811C9DC5`, prime `0x01000193`) digest over serialized scalars (osc freqs, env timings, env shapes, filter cutoff, jitter, cooldown) — xxhash64 rejected (adds runtime dep; FNV-1a stdlib-free + sufficient for `BlipBaker` LRU cache-key scope, ≪1000 patches lifetime). Stable across Unity GUID churn + version bumps. `[SerializeField] private int patchHash` persisted on `OnValidate` (after clamp + oscillator resize). `Awake` / `OnEnable` recompute-and-assert warn-only (no write — keeps SO non-dirty at runtime load; mismatch surfaces as `Debug.LogWarning` w/ `name` + stored hash + recomputed hash). Canonical field order frozen in helper (§5.2) — reorder invalidates `BlipBaker` cache; future fields append at tail + bump `HashVersion` const post-MVP. Hash scope excludes `mixerGroup` (`AudioMixerGroup` managed ref — routed by `BlipMixerRouter` Step 2; not in `BlipPatchFlat`) + `patchHash` self-field (circular). `BlipPatchHash` static helper co-located in `BlipPatch.cs` (small helper; mirrors `BlipPatchFlat.FromSO` colocation style). Glossary rows landed for **Blip patch** / **Blip patch flat** / **patch hash** (Audio category, peers of **Blip bootstrap** / **Blip mixer group**). Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.2.
  - Acceptance: hash stable across sessions (identical scalars → identical int); `OnValidate` write + `Awake` assert wired; 3 glossary rows land; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-111**, **TECH-113**, **TECH-114**

- [x] **TECH-114** — `BlipPatchFlat` blittable readonly struct mirror (2026-04-14)
  - Type: infrastructure / runtime data
  - Files: `Assets/Scripts/Audio/Blip/BlipPatchFlat.cs`
  - Spec: (removed after closure)
  - Notes: Blip master-plan Stage 1.2 Phase 2 opener. `BlipPatchFlat` readonly struct mirrors `BlipPatch` scalars; zero managed refs (no class / string / `AnimationCurve` / `AudioMixerGroup`). `AudioMixerGroup` kept on SO + `BlipMixerRouter` parallel map (Step 2) — preserves blittable contract. Nested `BlipOscillatorFlat` / `BlipEnvelopeFlat` / `BlipFilterFlat` readonly structs under `BlipPatchFlat.cs`. Oscillator slots inline triplet (`osc0/osc1/osc2 + oscillatorCount`) — managed array rejected (heap ref breaks blittable); triplet matches `BlipPatch.OnValidate` cap of 3. `mixerGroupIndex` int sentinel defaults `-1` (router overrides post-flatten; avoids nullable). Flatten via ctor `BlipPatchFlat(BlipPatch so, int mixerGroupIndex = -1)` + static `FromSO(BlipPatch)` helper — runs main-thread only on `BlipCatalog.Awake`. `patchHash` slot deferred to TECH-115 (appended w/o layout churn). Consumed by Stage 1.3 `BlipVoice.Render(in BlipPatchFlat, …)` + Step 2 `BlipBaker.BakeOrGet(in BlipPatchFlat, …)`. Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.2.
  - Acceptance: `BlipPatchFlat` + 3 nested flats compile as readonly structs; zero managed refs; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-111**, **TECH-112**

- [x] **TECH-113** — `OnValidate` clamps on `BlipPatch` (anti-click + range guards) (2026-04-14)
  - Type: infrastructure / authoring guard
  - Files: `Assets/Scripts/Audio/Blip/BlipPatch.cs`
  - Spec: (removed after closure)
  - Notes: Blip master-plan Stage 1.2 Phase 1 third task. `OnValidate` body on `BlipPatch` clamps AHDSR timings + range guards: `attackMs` / `releaseMs` ≥ 1 ms (≈48 samples @ 48 kHz mix rate — kills snap-onset click); `decayMs` ≥ 0 ms (allows instant Attack → Sustain transition — sustain-only patches via A=1 / D=0 / R=1); `sustainLevel` `Mathf.Clamp01`; `variantCount` 1..8; `voiceLimit` 1..16; `cooldownMs` ≥ 0. Oscillator array resize guard — `oscillators[]` length capped at 3 via `Array.Resize` (matches `BlipPatchFlat` MVP budget — TECH-114). Decision: `decayMs` clamp ≥ 0 (not ≥ 1 as Backlog Notes initially said) — contradiction w/ sustain-only fallback clause resolved in favor of fallback. Authoring-only pass; runtime flatten + `BlipVoice.Render` never re-clamp. TECH-115 later appends `patchHash = ComputeHash()` at bottom of same `OnValidate` body. Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.2.
  - Acceptance: six clamp rules + oscillator resize enforced; sustain-only case authors cleanly; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-111**, **TECH-112**

- [x] **TECH-112** — MVP struct + enum definitions for `BlipPatch` (2026-04-14)
  - Type: infrastructure / authoring
  - Files: `Assets/Scripts/Audio/Blip/BlipPatchTypes.cs`
  - Spec: (removed after closure)
  - Notes: Blip master-plan Stage 1.2 Phase 1 — 3 `[Serializable]` structs (`BlipOscillator` no `pitchEnvCurve`; `BlipEnvelope` per-stage `BlipEnvShape` + `sustainLevel`, no top-level `shape` curve; `BlipFilter` `kind` + `cutoffHz`, no `cutoffEnv`) + 5 enums (`BlipId` 11 rows = `None` + 10 MVP matching `docs/blip-procedural-sfx-exploration.md` §11.4; `BlipWaveform` Sine/Triangle/Square/Pulse/NoiseWhite; `BlipFilterKind` None/LowPass; `BlipEnvStage` Idle/Attack/Hold/Decay/Sustain/Release; `BlipEnvShape` Linear/Exponential). All integer-backed w/ explicit values, `None`/`Idle = 0` sentinels, no `[Flags]`. Sibling file `BlipPatchTypes.cs` (not nested in `BlipPatch.cs`) — enums referenced by kernel + flat struct without SO dep. Code landed pre-kickoff under TECH-111; implement phase = audit + validators. No curve fields anywhere under `Assets/Scripts/Audio/Blip/`. Feeds `BlipPatchFlat` flatten (TECH-114) + kernel (Stage 1.3). Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.2.
  - Acceptance: 3 structs + 5 enums compile; no curve fields; `unity:compile-check` + `validate:all` green
  - Depends on: none

- [x] **TECH-111** — `BlipPatch : ScriptableObject` authoring surface (MVP fields) (2026-04-14)
  - Type: infrastructure / authoring
  - Files: `Assets/Scripts/Audio/Blip/BlipPatch.cs`
  - Spec: (removed after closure)
  - Notes: Blip master-plan Stage 1.2 Phase 1 opener. `BlipPatch : ScriptableObject` landed w/ 15 MVP scalar fields (`oscillators[0..3]`, `envelope`, `filter`, `variantCount`, jitter triplet, `voiceLimit`, `priority`, `cooldownMs`, `deterministic`, `mixerGroup` authoring-only ref, `durationSeconds`, `useLutOscillators` reserved, `patchHash` `[SerializeField] private int`). `CreateAssetMenu("Territory/Audio/Blip Patch")` attribute wired. No `AnimationCurve` fields. No `mode` field / `BlipMode` enum (deferred post-MVP per `docs/blip-post-mvp-extensions.md` §1). Decisions: `mixerGroup` stays on SO (authoring-only) — NOT flattened into `BlipPatchFlat` to keep struct blittable; `BlipMixerRouter` parallel to catalog holds `BlipId → AudioMixerGroup` map (Step 2). `patchHash` serialized on SO (persist across Editor reload; computed TECH-115). Feeds flatten (TECH-114) + hash persist (TECH-115) + DSP kernel (Stage 1.3). Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.2.
  - Acceptance: `BlipPatch.cs` compiles + CreateAssetMenu reachable; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-112**

- [x] **TECH-109** — Testmode smoke: stub at border after new-game + binding intact after interstate build (2026-04-14)
  - Type: verification
  - Files: `Assets/Scripts/Editor/AgentTestModeBatchRunner.cs`, `Assets/Scripts/Editor/Testing/NeighborStubSmokeDriver.cs`, `tools/fixtures/scenarios/README.md`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 4 closer — regression gate rolling up stage exit criteria. Added `-testNewGame` (+ optional `-testSeed N`) flag to batch runner; post-`NewGame`, `NeighborStubSmokeDriver` (Editor-only) picks seeded stub's `borderSide`, invokes `InterstateManager.GenerateAndPlaceInterstate()` — canonical single-call entry that internally runs road preparation family + `InvalidateRoadCache()` + `NeighborCityBindingRecorder.RecordExits` (invariants #2 + #10 satisfied). Assertions: `stub_count >= 1`, `binding_count >= 1`, `resolver_matches == binding_count`, zero C# exceptions across ≥1 sim tick. Report JSON carries `neighbor_stub_smoke` block; mismatch reuses `ExitCodeGoldenMismatch` (8) w/ distinct `failure_detail` string for CI triage. `MapGenerationSeed.SetSessionMasterSeed(int)` (TECH-41 infra) pre-existed — `-testSeed` just delegates. Scenario id `neighbor-stub-new-game-smoke-32x32` reuses 32x32 map geometry gated by flag — no new `save.json`. No golden compare (seed GUID non-determinism per **TECH-104**); complement to **TECH-108** (load-path fixture). Closes Stage 1.3 exit. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: testmode batch exit 0; all assertions pass; zero C# exceptions; report attached; `npm run validate:all` + `npm run unity:compile-check` green
  - Depends on: **TECH-104**, **TECH-106**

- [x] **TECH-110** — Master-plan **HTML** progress tracker (`tools/progress-tracker/`) (2026-04-14)
  - Type: tooling / dev-ergonomics (no runtime Unity impact)
  - Files: `tools/progress-tracker/` (`parse.mjs`, `render.mjs`, `index.mjs`, `package.json`, `README.md`, `tests/parse.test.mjs`, `tests/render.test.mjs`); root `package.json` (`progress` script); `docs/progress.html` (generated, committed); `ia/skills/project-stage-close/SKILL.md` + `ia/skills/project-spec-close/SKILL.md` (regen hook)
  - Spec: (removed after closure)
  - Notes: Static **HTML** generator parses **orchestrator document** Markdown (`ia/projects/*master-plan*.md`) → emits single `docs/progress.html` w/ per-plan progress cards (green bar, current step/stage/phase/task, status breakdown, phase checklist, sibling-coordination notes) + overall combined header. Pure fn parser + renderer — same bytes in → same HTML bytes out (no wall-clock, no git-log, no `Date.now`); `git diff docs/progress.html` empty on repeat runs. Inline CSS, zero JS deps. Regen wired into `project-stage-close` + `project-spec-close` skills so lifecycle events auto-refresh output (no CI / watcher / pre-commit). Parsing contract + hook contract documented in `tools/progress-tracker/README.md`. Decisions: drop git-log timestamp (breaks determinism); lifecycle-skill hook over watcher (state flips are discrete lifecycle events); static HTML over SPA (zero deps). Orchestrator doc rules per `ia/rules/orchestrator-vs-spec.md`.
  - Acceptance: `npm run progress` regenerates `docs/progress.html` deterministically; HTML renders w/ no external fetches; green bar % matches manual `Done` / total task count per plan; step/stage/phase/task surfaces across all plan states; sibling-orchestrator warnings visible per card; `npm run validate:all` green
  - Depends on: none

- [x] **TECH-108** — Save/load round-trip test: stubs + bindings preserved (2026-04-14)
  - Type: verification
  - Files: `Assets/Scripts/Editor/AgentTestModeBatchRunner.cs`, `tools/fixtures/scenarios/neighbor-stub-roundtrip-32x32/`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 4 opener. Verification-only — committed schema-3 fixture + sibling golden `agent-testmode-golden-neighbor-stubs.json` prove `GameSaveData.neighborStubs` + `neighborCityBindings` survive **save data** round-trip byte-identical. `AgentTestModeBatchRunner` extended: filename-suffix dispatch (`neighbor-stubs` → neighbor compare branch) post-`LoadGame`; sort-stable JSON compare; diff to `golden_diff`; mismatch → `ExitCodeGoldenMismatch` (8). Rejected live save→reload in batch (no road-build driver — **TECH-109** smoke covers live-build angle). Sibling DTO file (not schema bump) avoids regen ripple. Hand-authored GUIDs — seed determinism covered by **TECH-104**. Inline fix: `NeighborStubSeeder.cs` missing `using Territory.Persistence` added (pre-existing bug; invariants untouched). Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: testmode batch exit 0; projected DTO byte-equal to golden; report under `tools/reports/agent-testmode-batch-*.json`; `unity:compile-check` + `validate:all` green; invariants untouched
  - Depends on: **TECH-103**

- [x] **TECH-107** — Glossary rows: **neighbor-city stub** + **interstate border** (2026-04-14)
  - Type: IA / glossary
  - Files: `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 3 closer (docs-only). Added **neighbor-city stub** row under Multi-scale simulation (cites master plan + `NeighborCityStub.cs`) + **interstate border** row under Roads & Bridges (cites geo §13.5, cross-ref **Interstate** + **Map border**). Terminology consistency — no synonyms; existing rows untouched. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: both rows present + alphabetized within category; canonical cross-refs; `npm run validate:all` green
  - Depends on: **TECH-102**

- [x] **TECH-106** — `GridManager.GetNeighborStub(BorderSide)` inert read contract (2026-04-14)
  - Type: infrastructure / runtime
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Managers/UnitManagers/IGridManager.cs`, `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 3 opener (T1.3.5). Read-only `GetNeighborStub(BorderSide side) → NeighborCityStub?` mirrors **TECH-88** `ParentRegionId` / `ParentCountryId` one-shot hydrate + read pattern. `HydrateNeighborStubs(IEnumerable<NeighborCityStub>)` on concrete `GridManager` (off interface, matches TECH-88); linear scan over cached `IReadOnlyList<NeighborCityStub>` (≤4 at MVP). Hydration wired in `GameSaveManager.NewGame` (post-`SeedInitial`) + `LoadGame` (post-`HydrateParentIds`). Duplicate call → `Debug.LogError` + return. Null on unmatched side is silent (normal condition). Zero consumers yet — inert. Invariant #6 preserved (thin accessor under TECH-88 precedent). Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: accessor present on `GridManager` + `IGridManager`; null on unmatched side; zero city-sim behavior change; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-103**, **TECH-104**

- [x] **TECH-105** — On-road-build: **interstate** exit at **map border** binds to stub by `BorderSide` (2026-04-13)
  - Type: infrastructure / roads
  - Files: `Assets/Scripts/Managers/GameManagers/RoadManager.cs`, `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`, `Assets/Scripts/Managers/UnitManagers/NeighborCityStub.cs`, `Assets/Scripts/Managers/UnitManagers/NeighborCityBindingRecorder.cs` (new)
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 2 closer. Added `NeighborCityBinding` struct under `Territory.Core` + `GameSaveData.neighborCityBindings` list; bumped `CurrentSchemaVersion` 2 → 3 w/ legacy-null → empty migration. Post-`Apply` recorder `NeighborCityBindingRecorder.RecordExits` hooked into `RoadManager` interstate commit after `InvalidateRoadCache` (invariant #2 preserved). Border resolver: `x==0→West`, `x==w-1→East`, `y==0→South`, `y==h-1→North`; corner tie-break via `InterstateManager.ExitBorder`/`EntryBorder`. Dedupe key `(stubId, exitCellX, exitCellY)`. Missing stub → warn + skip. Helper holds `GridManager grid` composition ref only where needed (invariant #6 untouched). Road preparation family (#10) untouched. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: binding recorded post-interstate-`Apply`; survives save/load (schema 3); legacy schema-2 saves load w/ empty list; dedupe prevents duplicates; invariants #2/#6/#10 preserved; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-104**

- [x] **TECH-104** — New-game init: place ≥1 neighbor stub at random **interstate** **map border** (seed-deterministic side) (2026-04-13)
  - Type: infrastructure / new-game
  - Files: `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`, `Assets/Scripts/Managers/UnitManagers/NeighborStubSeeder.cs` (new)
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 2 opener. `NewGame()` post-`ReinitializeGeographyForNewGame` invokes `NeighborStubSeeder.SeedInitial`. Candidate sides drawn from `InterstateManager.EntryBorder` ∪ `ExitBorder` (fallback to all 4 when both unset); pick via `new System.Random(MapGenerationSeed.MasterSeed)`. GUID id accepts non-determinism; display name `Neighbor-{GUID8}`. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: `neighborStubs.Count >= 1` post New Game; same seed → same `borderSide`; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-103**

- [x] **TECH-103** — `GameSaveData.neighborStubs` list + save version bump + legacy migration (2026-04-13)
  - Type: infrastructure / save
  - Files: `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 1 closer. Wired `List<NeighborCityStub>` onto `GameSaveData`, bumped `CurrentSchemaVersion` 1 → 2, legacy-null → empty guard added in `MigrateLoadedSaveData` (mirrors **TECH-87** / **TECH-88** parent-id migration). `BuildCurrentGameSaveData` initializes non-null empty list. Placement deferred to **TECH-104**. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: `neighborStubs` non-null post-load; `CurrentSchemaVersion` = 2; legacy saves migrate w/ empty list + parent ids preserved; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-102**

- [x] **TECH-102** — `NeighborCityStub` struct (id GUID, display name, `BorderSide` enum) + serialize schema (2026-04-13)
  - Type: infrastructure / schema
  - Files: `Assets/Scripts/Managers/UnitManagers/NeighborCityStub.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.3 Phase 1 opener. Plain C# struct under `Territory.Core`; `[Serializable]`; three fields (`id`, `displayName`, `borderSide`) + `BorderSide { North, South, East, West }` enum. Compile-only schema feeding downstream save list / seeder / binding issues. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.3.
  - Acceptance: struct + enum compile under `Territory.Core`; `[Serializable]`; three fields; `unity:compile-check` + `validate:all` green
  - Depends on: none

- [x] **TECH-101** — Scene-load suppression policy doc + glossary rows (Blip mixer group, Blip bootstrap) (2026-04-13)
  - Type: documentation / glossary
  - Files: `ia/specs/glossary.md`, `Assets/Scripts/Audio/Blip/BlipBootstrap.cs` (comment only)
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 2 of Blip audio program. Landed two glossary rows under new `## Audio` H2 — **Blip bootstrap** (persistent prefab, `DontDestroyOnLoad`, scene-load suppression policy summary) + **Blip mixer group** (three routing groups on `BlipMixer.mixer` + `SfxVolume` exposed param). Both cite `ia/projects/blip-master-plan.md` Stage 1.1. Index row added under `## Index (quick skim)`. Scene-load suppression `<remarks>` paragraph added to `BlipBootstrap` class XML doc stating no Blip fires until `BlipCatalog.Awake` sets ready flag (lands Step 2). Satisfies Stage 1.1 final Exit bullet. Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.1 Phase 2.
  - Acceptance: glossary rows + code comment committed; `validate:all` green
  - Depends on: none

- [x] **TECH-100** — `BlipBootstrap` prefab + `DontDestroyOnLoad` + `MainMenu.unity` placement (2026-04-13)
  - Type: infrastructure / prefab + scene
  - Files: `Assets/Prefabs/Audio/BlipBootstrap.prefab`, `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`, `Assets/Scenes/MainMenu.unity`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 2 of Blip audio program. Authored `Assets/Prefabs/Audio/BlipBootstrap.prefab` w/ four empty child slots (`BlipCatalog`, `BlipPlayer`, `BlipMixerRouter`, `BlipCooldownRegistry` — populated Step 2). Added four `[SerializeField] private Transform` slot fields to `BlipBootstrap.cs`. `Awake` calls `DontDestroyOnLoad(transform.root.gameObject)` per `GameNotificationManager.cs` pattern. `BlipMixer.mixer` asset wired to `blipMixer` field. Prefab instance placed at root of `MainMenu.unity` (build index 0 per `MainMenuController.cs`). Honors invariants #3 + #4. Satisfies Stage 1.1 exit criterion "`BlipBootstrap` GameObject prefab at `MainMenu.unity` root". Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.1 Phase 2.
  - Acceptance: prefab + scene instance + `DontDestroyOnLoad` call committed; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-99**

- [x] **TECH-99** — Headless SFX volume binding in `BlipBootstrap.Awake` via `PlayerPrefs` (2026-04-13)
  - Type: infrastructure
  - Files: `Assets/Scripts/Audio/Blip/BlipBootstrap.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 1 of Blip audio program. `BlipBootstrap.Awake` reads `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` + calls `BlipMixer.SetFloat("SfxVolume", db)` with null-guard + branch logs (success + missing-mixer warn + SetFloat-failure warn). Key + param + default exposed as `public const string` / `public const float` on `BlipBootstrap` (`SfxVolumeDbKey`, `SfxVolumeParam`, `SfxVolumeDbDefault = 0f`) so post-MVP Settings UI binds same keys w/o duplication. No Settings UI in MVP (visible slider + mute toggle deferred per `docs/blip-post-mvp-extensions.md` §4). Merged w/ TECH-100 `Awake` body — TECH-99 owns binding block + constants, TECH-100 owns `DontDestroyOnLoad` + slots. Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.1 Phase 1.
  - Acceptance: `BlipBootstrap.cs` committed w/ binding; `unity:compile-check` + `validate:all` green
  - Depends on: **TECH-98**

- [x] **TECH-98** — `BlipMixer.mixer` asset + three groups + exposed `SfxVolume` param (2026-04-13)
  - Type: infrastructure / asset
  - Files: `Assets/Audio/BlipMixer.mixer`
  - Spec: (removed after closure)
  - Notes: Stage 1.1 Phase 1 of Blip audio program. Authored `Assets/Audio/BlipMixer.mixer` via Unity Editor (`Window → Audio → Audio Mixer` — binary YAML asset). Three child groups (`Blip-UI`, `Blip-World`, `Blip-Ambient`) routed through master. Master `SfxVolume` dB param exposed via `Exposed Parameters` panel (default 0 dB). Satisfies first Stage 1.1 exit criterion — mixer asset + routing surface ready for Step 2 player pool + router to consume. Orchestrator: [`projects/blip-master-plan.md`](../ia/projects/blip-master-plan.md) Stage 1.1.
  - Acceptance: asset + three groups + exposed param committed; `validate:all` green
  - Depends on: none

- [x] **TECH-97** — Testmode assertion: `HeightMap` / `CityCell.height` integrity (invariant #1) (2026-04-13)
  - Type: verification
  - Files: testmode batch scenario
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 4 regression gate. Added `HeightIntegritySweep` in `AgentTestModeBatchRunner` — iterates grid post-load + post-tick, compares `HeightMap[x,y]` vs `CityCell.height`; emits `height_integrity` JSON block + new exit code `9` on mismatch. Regression run on `reference-flat-32x32` + `--simulation-ticks 3`: exit 0, 1024 cells checked, zero violations post-load + post-tick. Report: `tools/reports/agent-testmode-batch-20260413-212829.json`. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: `height_integrity.post_load.violations == 0` + `post_tick.violations == 0`; batch exit `0`; exit code `9` documented in `ia/skills/agent-test-mode-verify/SKILL.md`
  - Depends on: **TECH-96**

- [x] **TECH-96** — Testmode smoke: city load + sim tick, no regression (cell-type split) (2026-04-13)
  - Type: verification
  - Files: testmode batch scenario
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 4 regression gate. Reused `reference-flat-32x32` smoke scenario; exit 0, `simulation_ticks_applied: 3`, zero C# exceptions on commit `73fd7e8`. Confirmed cell-type split (TECH-90–95) introduced zero behavior regression. Report: `tools/reports/agent-testmode-batch-20260413-211557.json`. Lessons (stale lockfile recovery, `--simulation-ticks N` flag, `--golden-path` upgrade) migrated to `ia/skills/agent-test-mode-verify/SKILL.md` Gotchas. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: testmode batch exit 0 + zero exceptions; `GameSaveManager.LoadGame` + ≥1 sim tick confirmed; batch log + commit hash recorded
  - Depends on: **TECH-95**

- [x] **TECH-95** — Back-compat `GetCell(x,y)` defaults to `CityCell`; update all callers; invariant #5 preserved (2026-04-13)
  - Type: refactor / infrastructure
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Managers/UnitManagers/IGridManager.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 3 (T1.2.6) closer of cell-type split — audit-only gate. Verified `GridManager.GetCell(int x, int y)` returns `CityCell` (post-TECH-91) + `IGridManager` mirror; zero `Cell`-typed locals across `Assets/Scripts/`. Classified 25 `gridArray`/`cellArray` direct-access hits: 19 helper-service touches (`BuildingPlacementService`, `GridSortingOrderService`) allowed under invariant #6 carve-out (composition reference shares trust boundary with owning class — clarification added to `ia/rules/invariants.md` #5); 6 external-manager touches (`WaterManager` lines 353, 464; `GeographyManager` lines 585, 736, 954, 995) deferred to pre-existing **TECH-04**. No code change. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: return type `CityCell` on both surfaces; zero `Cell`-typed locals; every direct-access site classified; pre-existing violations linked to TECH-04; `npm run unity:compile-check` + `npm run validate:all` green
  - Depends on: **TECH-94**

- [x] **TECH-94** — Generic `GetCell<T>(x,y)` typed accessor on `GridManager` + `IGridManager` (compile gate) (2026-04-13)
  - Type: infrastructure / refactor
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Managers/UnitManagers/IGridManager.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 3 (T1.2.5) of cell-type split. Generic `public T GetCell<T>(int x, int y) where T : CellBase` added to `GridManager` + `IGridManager`; bounds check + `as T` cast; null on out-of-range or type mismatch. Existing untyped `CityCell GetCell(int x, int y)` byte-identical. `RegionCell` / `CountryCell` intentionally unreachable (plain classes outside `CellBase`, not in `cellArray`). Diff ≤ ~10 lines → no helper extracted (invariant #6 untouched). Caller migration = TECH-95. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: generic accessor present on both surfaces; untyped overload unchanged; null on OOB + type mismatch; `npm run unity:compile-check` + `npm run validate:all` green
  - Depends on: **TECH-92**, **TECH-93**

- [x] **TECH-93** — `CountryCell` placeholder type (coord + parent-country-id; no behavior) + complete cell-type glossary (2026-04-13)
  - Type: infrastructure / IA
  - Files: `Assets/Scripts/Managers/UnitManagers/CountryCell.cs` (new), `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 2 (T1.2.4) of cell-type split. Mirrors TECH-92 `RegionCell`. Plain C# class under `Territory.Core` (no MonoBehaviour, no `CellBase` inheritance — `CellBase : MonoBehaviour` is city-grid infra; country scale data-only in MVP). Carries read-only `X`, `Y` (int) + `ParentCountryId` (string GUID matching `GameSaveData.countryId`); single constructor; zero methods. NOT inserted into `GridManager.gridArray` (invariant #5 untouched). No save wiring; country scale dormant. Combined glossary row "City cell / Region cell / Country cell" at `glossary.md:247` covers all three — no split. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: `CountryCell` compiles under `Territory.Core`; plain C# only; not in grid/save paths; city sim + invariants #1/#5 untouched; `npm run unity:compile-check` + `npm run validate:all` green
  - Depends on: **TECH-91**

- [x] **TECH-92** — `RegionCell` placeholder type (coord + parent-region-id; no behavior) + glossary row (2026-04-13)
  - Type: infrastructure / IA
  - Files: `Assets/Scripts/Managers/UnitManagers/RegionCell.cs` (new), `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 2 of cell-type split. Plain C# class under `Territory.Core` (no MonoBehaviour, no `CellBase` inheritance — `CellBase : MonoBehaviour` is city-grid infra; region scale data-only in MVP). Carries read-only `X`, `Y` (int) + `ParentRegionId` (string GUID matching `GameSaveData.regionId`); single constructor; zero methods. NOT inserted into `GridManager.gridArray` (invariant #5 untouched). No save wiring; region scale dormant. Combined glossary row "City cell / Region cell / Country cell" at `glossary.md:247` covers it — no new row added. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: `RegionCell` compiles under `Territory.Core`; plain C# only; not in grid/save paths; city sim + invariants #1/#5 untouched; `npm run unity:compile-check` + `npm run validate:all` green
  - Depends on: **TECH-91**

- [x] **TECH-91** — Rename `Cell` → `CityCell` across all city sim files (2026-04-13)
  - Type: refactor / infrastructure
  - Files: `Assets/Scripts/Managers/UnitManagers/CityCell.cs` (renamed from `Cell.cs`), `Assets/Scripts/Managers/GameManagers/GridManager.cs`, all city sim files referencing `Cell`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 1 of cell-type split. Mechanical rename `Cell` → `CityCell` across 35 files (~300 occurrences); `git mv` preserves `.cs.meta` GUID (prefab / scene refs survive); `HeightMap` ↔ `CityCell.height` dual-write (invariant #1) intact via field inheritance from `CellBase`; `IGridManager.GetCell` returns `CityCell`; `CellBase` kept scale-universal (not renamed). Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: class + file named `CityCell`; zero stray bare `Cell` refs outside `CellBase` / `cellArray` / `GetCell`; `npm run unity:compile-check` green; `npm run validate:all` green
  - Depends on: **TECH-90**

- [x] **TECH-90** — Extract `Cell` abstract base type (coord, height, shared primitives) (2026-04-13)
  - Type: refactor / infrastructure
  - Files: `Assets/Scripts/Managers/UnitManagers/CellBase.cs` (new), `Assets/Scripts/Managers/UnitManagers/Cell.cs`
  - Spec: (removed after closure)
  - Notes: Stage 1.2 Phase 1 of cell-type split. Abstract `CellBase : MonoBehaviour` extracted under `Territory.Core` carrying scale-universal primitives only (`x`, `y`, `height`, `sortingOrder`, `transformPosition`). `Cell : CellBase`; all city-specific fields (roads, buildings, zones, forests, water, cliffs, interstate, desirability) stay on `Cell`. Compile-only; zero caller edits; rename `Cell` → `CityCell` deferred to TECH-91. Invariant #1 (`HeightMap` ↔ `Cell.height`) unaffected — field inheritance preserves dual-write syntax. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Stage 1.2.
  - Acceptance: `CellBase.cs` exists w/ 5 fields only; `Cell : CellBase`; 5 fields removed from `Cell.cs`; `npm run unity:compile-check` green; `npm run validate:all` green; no caller edits outside the two files
  - Depends on: **TECH-89**

- [x] **TECH-89** — Parent-id round-trip + legacy-migration tests (testmode) (2026-04-13)
  - Type: test / verification
  - Files: `Assets/Scripts/Editor/AgentTestModeBatchRunner.cs` (DTO `schema_version` 1 → 2 + `regionId` / `countryId` fields + `IdMatches` sentinel helper), `tools/fixtures/scenarios/parent-id-seeded-32x32/` (save + golden), `tools/fixtures/scenarios/parent-id-legacy-32x32/` (save + golden), `tools/fixtures/scenarios/reference-flat-32x32/agent-testmode-golden-ticks{0,3}.json` (regen)
  - Spec: (removed after closure)
  - Notes: Two testmode scenarios + golden-snapshot extension assert parent region id / parent country id persist through Load pipeline. Seeded modern fixture (schema v1 + committed GUIDs) → load → golden asserts `GridManager.ParentRegionId` / `.ParentCountryId` equal seeded values. Legacy fixture (schema 0, ids absent) → load → `MigrateLoadedSaveData` allocates placeholder GUIDs → `IdMatches(goldenValue, runtimeValue)` accepts `"<guid>"` sentinel iff `Guid.TryParseExact` succeeds. Existing reference-flat-32x32 goldens regenerated for DTO bump. Closes Stage 1.1 verification. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Step 1 / Stage 1.1.
  - Acceptance: testmode scenarios green (seeded + legacy + regenerated reference); fixtures committed; `npm run validate:all` + `unity:compile-check` green
  - Depends on: **TECH-87**

## Completed (moved from BACKLOG.md, 2026-04-12)

- [x] **TECH-87** — Parent-scale identity fields on `GameSaveData` + save migration (2026-04-12)
  - Type: infrastructure / save
  - Files: `Assets/Scripts/SaveSystem/GameSaveData.cs`, `Assets/Scripts/SaveSystem/SaveManager.cs` (version bump + migration path), `ia/specs/save-system.md` (§schema), `ia/specs/glossary.md`
  - Spec: (removed after closure)
  - Notes: Added non-null `region_id` + `country_id` (GUID) to `GameSaveData`. Bumped save version. Legacy saves load w/ placeholder GUIDs. Glossary rows landed for **parent region id** + **parent country id**. No runtime behavior change beyond ids being present. Orchestrator: [`projects/multi-scale-master-plan.md`](../ia/projects/multi-scale-master-plan.md) Step 1 / Stage 1.1.
  - Acceptance: fields serialize + deserialize round-trip; legacy save loads w/ placeholder ids; save version bumped; glossary rows land; `npm run validate:all` green
  - Depends on: none

## Completed (moved from BACKLOG.md, 2026-04-11)

- [x] **TECH-85** — IA migration to neutral `ia/` namespace + native Claude Code layer (2026-04-11)
  - Type: tooling / IA infrastructure / agent enablement
  - Files: `ia/{specs,rules,skills,projects,templates}`; `tools/mcp-ia-server/src/config.ts`; `tools/mcp-ia-server/src/tools/{router-for-task,project-spec-journal,project-spec-closeout-digest,glossary-lookup,unity-callers-of,unity-subscribers-of,csharp-class-summary}.ts`; `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`; `tools/mcp-ia-server/scripts/generate-ia-indexes.ts`; `tools/validate-dead-project-spec-paths.mjs`; `.claude/{settings.json,skills/,agents/,output-styles/,commands/,memory/}`; `tools/scripts/claude-hooks/`; `MEMORY.md`; densification pass over `docs/`, `AGENTS.md`, `CLAUDE.md`, `ARCHITECTURE.md`
  - Spec: (removed after closure — glossary rows **Code intelligence MCP tools**, **Glossary graph**, extended **IA index manifest** to I3)
  - Notes: Five stages shipped by fresh agents against a stage/phase execution model. Stage 1 — bootstrap Claude Code layer (`.claude/settings.json` with `acceptEdits` + `mcp__territory-ia__*` wildcard, 4 hooks, 5 slash command stubs, `MEMORY.md` seed, `project-stage-close` skill). Stage 2 — structural move `.cursor/{specs,rules,skills,projects,templates}` → `ia/...`, cross-extension `.md → .md` symlinks for back-compat, MCP server path constants, validator symlink-awareness. Stage 3 — four-field IA frontmatter on 74 files, `validate:frontmatter` validator, verification policy consolidated to `docs/agent-led-verification-policy.md` (single canonical source), `AGENTS.md` / `BACKLOG.md` / `CLAUDE.md` / `docs/information-architecture-overview.md` densified. Stage 4 — 5 native subagents (`spec-kickoff`, `spec-implementer`, `verifier`, `test-mode-loop`, `closeout`; Opus orchestrators + Sonnet executors), 5 real slash commands, 2 output styles (`verification-report`, `closeout-digest` — JSON header + caveman summary), caveman directive enforced at 4 layers (16 path grep gate). Stage 5 — 3 new code-intelligence MCP tools (`unity_callers_of`, `unity_subscribers_of`, `csharp_class_summary`) + `glossary_lookup` extended to graph shape (`related`, `cited_in`, `appears_in_code`), precomputed `glossary-graph-index.json` (I3 companion to I1 / I2). Cursor remains a first-class consumer throughout via back-compat symlinks. Canonical stances locked: `permissions.defaultMode: "acceptEdits"` (discovered after in-vivo chicken-and-egg friction with default mode), `mcp__territory-ia__*` wildcard (vs per-tool list), 4-layer caveman directive (subagent body + skill preamble + slash command body + stage-close handoff template), subagent `tools` field as explicit per-subagent allow-list (not wildcard).
  - Acceptance: `ia/` populated with frontmatter (76 files); back-compat symlinks resolve via cross-extension `.md → .md`; MCP server reads from `ia/`; `npm run validate:all` + `npm run verify:local` green end-to-end; 5 subagents + 5 slash commands + 4 hooks + 2 output styles operative under `.claude/`; 3 new MCP tools (`unity_callers_of`, `unity_subscribers_of`, `csharp_class_summary`) registered; `glossary_lookup` returns `{term, definition, related, cited_in, appears_in_code}`; verification policy consolidated; caveman directive present on 16 paths
  - Depends on: none

## Completed (moved from BACKLOG.md, 2026-04-09)

- [x] **FEAT-22** — **Tax base** feedback on **demand (R / C / I)** and happiness (2026-04-09)
  - Type: feature
  - Files: `EconomyManager.cs`, `DemandManager.cs`, `CityStats.cs`, `EmploymentManager.cs`, `UIManager.Theme.cs`, `UIManager.Hud.cs`, `UIManager.Toolbar.cs`
  - Spec: (removed after closure — **glossary** **Tax base**, **Demand (R / C / I)**, **Happiness**; **managers-reference** **Demand (R / C / I)**; **simulation-system** daily pass note; this row)
  - Notes: **Hybrid model:** **per-sector** tax scaling on R/C/I **demand** plus **happiness**-**target** multiplier; **highest** (not average) **tax** rate vs comfort band for **happiness**; same-day **demand** refresh after **happiness** in `PerformDailyUpdates`; **tax** UI calls `RefreshHappinessAfterPolicyChange()`. Tunable weights on **`CityStats`** / **`DemandManager`**. Grid debug **HUD** chrome: **ScrollRect** for long copy; square panel aligned between **DataPanelButtons** and **ControlPanel**.
  - Depends on: none (happiness + **monthly maintenance** shipped — **glossary** / archive)

## Completed (moved from BACKLOG.md, 2026-04-08)

- [x] **FEAT-21** — Expenses and maintenance system (2026-04-08)
  - Type: feature
  - Files: `EconomyManager.cs`, `CityStats.cs`, `GrowthBudgetManager.cs`
  - Spec: (removed after closure — **glossary** **Monthly maintenance**; **managers-reference** §Demand; **simulation-system** **Calendar and monthly economy**; this row)
  - Notes: **Monthly maintenance** after **tax base** on calendar day 1; **street** cost from `roadCount`, **power plant** cost from `GetRegisteredPowerPlantCount()`; `SpendMoney` uses `RemoveMoney`; HUD / growth budget use net projected cash flow. Optional **TECH-82** **city events** audit trail still open.
  - Depends on: none (happiness system shipped — see **FEAT-23** below)

## Completed (moved from BACKLOG.md, 2026-04-07)

- [x] **FEAT-23** — Dynamic happiness based on city conditions (2026-04-07)
  - Type: feature
  - Files: `CityStats.cs`, `DemandManager.cs`, `EmploymentManager.cs`, `CityStatsUIController.cs`, `UIManager.Hud.cs`, `AgentBridgeCommandRunner.cs`
  - Notes: Replaced unbounded `int` happiness accumulator with normalized 0–100 float score recalculated each simulation tick from 6 weighted factors (employment, tax burden, service coverage stub, forest bonus, development base, pollution penalty). Convergence rate scales with population. Introduced foundational city-wide **pollution** model (industrial buildings + power plants − forest absorption). Happiness feeds back into **demand (R / C / I)** via multiplier in `DemandManager`. Old saves clamp happiness to 0–100 on load. Migrated: **glossary** (Happiness, Pollution), **mgrs** §Demand + §World, **ARCHITECTURE.md** dependency table.

---

## Completed (moved from BACKLOG.md, 2026-04-04)

- [x] **TECH-36** — **Computational program** (umbrella; charter closed) (2026-04-04)
  - Type: tooling / code health / agent enablement
  - Files: umbrella only — **glossary** **Compute-lib program**; pilot **`tools/compute-lib/`** + **TECH-37**; **TECH-39** **MCP** suite; [`ARCHITECTURE.md`](ARCHITECTURE.md) **Compute** row; `ia/specs/isometric-geography-system.md`, `ia/specs/simulation-system.md`, `ia/specs/managers-reference.md`
  - Spec: (removed after closure — **glossary** **Compute-lib program**; **TECH-37**/**TECH-39** rows below; open **C#** / **research** follow-ups remain on [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program** — **TECH-38**, **TECH-32**, **TECH-35**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella retired from open **BACKLOG**; **TECH-38** no longer gates closure. **Authority** and **tooling** trace: **glossary** **Compute-lib program**, **territory-compute-lib (TECH-37)**, **C# compute utilities (TECH-38)**, **Computational MCP tools (TECH-39)**.
  - Depends on: none

- [x] **TECH-37** — **Computational** infra: **`tools/compute-lib/`** + pilot **MCP** tool (**World ↔ Grid**) (2026-04-04)
  - Type: tooling
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/`; `Assets/Scripts/Utilities/Compute/`; `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`; [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml)
  - Spec: (removed after closure — **glossary** **territory-compute-lib (TECH-37)**; geo §1.3 **Agent tooling** note; [`ARCHITECTURE.md`](ARCHITECTURE.md) **territory-ia** tools + **`tools/compute-lib/`**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **glossary** **Compute-lib program**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-36**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`territory-compute-lib`**, **`isometric_world_to_grid`**, **`IsometricGridMath`**, golden **`world-to-grid.json`**, **IA tools** **CI** builds **compute-lib** before **mcp-ia-server**. **Authority:** **C#** / **Unity** remain **grid** truth; **Node** duplicates **verified** planar **World ↔ Grid** inverse only (**glossary** **World ↔ Grid conversion**).
  - Depends on: none (soft: **TECH-21** **§ Completed**)

- [x] **TECH-39** — **territory-ia** **computational** **MCP** tool suite (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `tools/compute-lib/`; `tools/mcp-ia-server/src/tools/compute/`; `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`; `Assets/Scripts/Utilities/Compute/` (parity surfaces)
  - Spec: (removed after closure — no project spec; **glossary** **Computational MCP tools (TECH-39)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`BACKLOG.md`](BACKLOG.md) **§ Compute-lib program** follow-ups; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`growth_ring_classify`**, **`grid_distance`**, **`pathfinding_cost_preview`** v1, **`geography_init_params_validate`**, **`desirability_top_cells`** (**`NOT_AVAILABLE`** stub until **TECH-66**); shared **`territory-compute-lib`**. **Deferred** work: **TECH-65**, **TECH-66**, **TECH-64**, **TECH-32**, **TECH-15**/**TECH-16** (see open **BACKLOG**).
  - Depends on: none (soft: **TECH-38** for **heavy** tools; pilot milestone in archive)

- [x] **TECH-60** — **Spec pipeline & verification program** (umbrella): agent workflow, MCP, scripts, **test contracts** (2026-04-04)
  - Type: tooling / documentation / agent enablement
  - Files: [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`ia/skills/README.md`](ia/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml); **§ Completed** children **TECH-61**–**TECH-63** (this file)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-61**–**TECH-63**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`ia/skills/README.md`](ia/skills/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); prerequisite rows **TECH-15**, **TECH-16**, **TECH-31**, **TECH-35**, **TECH-30**, **TECH-37**, **TECH-38** — `ia/projects/*.md`; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Phased **TECH-61** (layer A), **TECH-62** (layer B — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**), **TECH-63** (layer C — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**). **Charter:** ids **TECH-60**–**TECH-63**; three layers vs monolithic umbrella. **Related:** **TECH-48** (MCP discovery — **TECH-62** overlap **§ Completed**); **TECH-23**; **TECH-45**–**TECH-47** (**Skills** README).
  - Depends on: none (prerequisites remain separate **BACKLOG** rows)

- [x] **TECH-63** — **Spec pipeline** layer **C**: Cursor **Skills** + **project spec** template (**test contracts**, workflow steps) (2026-04-04)
  - Type: documentation / agent enablement (**Cursor Skill** + template edits)
  - Files: `ia/skills/project-spec-kickoff/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/project-implementation-validation/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/project-new/SKILL.md`; `ia/templates/project-spec-template.md`; `ia/projects/PROJECT-SPEC-STRUCTURE.md`; `ia/skills/README.md`; [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline layer C (TECH-63)**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md) **§7b**; [`ia/skills/README.md`](ia/skills/README.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`## 7b. Test Contracts`** in template; **Skills** — **`depends_on_status`** preflight, **`router_for_task`** **`files`**, **Impact preflight**, **Phase exit** / **rollback**; **`AGENTS.md`** **§7b** pointer. **Does not** extend **`project_spec_closeout_digest`** for **§7b** — follow-up **BACKLOG** row if machine-read **test contracts** is required.
  - Depends on: **TECH-62** **§ Completed** (soft)

- [x] **TECH-62** — **Spec pipeline** layer **B**: **territory-ia** **`backlog_issue`** **`depends_on_status`** + **`router_for_task`** **`files`** / **`file_domain_hints`** (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/` (handlers, parsers); `tools/mcp-ia-server/tests/`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/package.json`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md)
  - Spec: (removed after closure — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`backlog_issue`** returns **`depends_on_status`** per cited **Depends on** id; **`router_for_task`** accepts **`domain`** and/or **`files`**. **`@territory/mcp-ia-server`** **0.4.4**. **Deferred:** **`context_bundle`**, **`spec_section`** **`include_children`**, **`project_spec_status`** — **TECH-48** / follow-ups. **TECH-48** overlap and MVP split recorded in pre-closeout **Decision Log** (migrated to this row + **glossary**).
  - Depends on: **TECH-61** **§ Completed** (soft)

- [x] **TECH-61** — **Spec pipeline** layer **A**: repo **scripts** + validation **infrastructure** (`npm run`, optional `tools/invariant-checks/`) (2026-04-04)
  - Type: tooling / CI / agent enablement
  - Files: root [`package.json`](package.json) (`validate:all`, `description`); [`ia/skills/project-implementation-validation/SKILL.md`](ia/skills/project-implementation-validation/SKILL.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **project-implementation-validation**, **territory-ia spec-pipeline layer B (TECH-62)**, **territory-ia spec-pipeline program (TECH-60)**, **Documentation** row; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-62**; [`projects/spec-pipeline-exploration.md`](projects/spec-pipeline-exploration.md) (reference)
  - Spec: (removed after closure — **glossary** **project-implementation-validation** / **`validate:all`**; **project-implementation-validation** **`SKILL.md`**; **`docs/mcp-ia-server.md`**; root **`package.json`**; **glossary** **territory-ia spec-pipeline program (TECH-60)**; **TECH-62** **§ Completed**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`npm run validate:all`** chains **IA tools** steps 1–4 (**dead project spec**, **`test:ia`**, **`validate:fixtures`**, **`generate:ia-indexes --check`**); triple-source rule with **project-implementation-validation** manifest and [`.github/workflows/ia-tools.yml`](.github/workflows/ia-tools.yml). **Phase 2**/**3** optional scripts (**impact** / **diff** / **backlog-deps**, **`test:invariants`**) deferred per **Decision Log** — pick up under **TECH-30** / follow-up. **Does not** register MCP tools (**TECH-62** layer B **§ Completed** for **territory-ia** extensions — **glossary** **territory-ia spec-pipeline layer B (TECH-62)**).
  - Depends on: none (soft: **TECH-50** **§ Completed**)

- [x] **TECH-21** — **JSON program** (umbrella; charter closed) (2026-04-03)
  - Type: technical / data interchange
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); `ia/specs/glossary.md` — **JSON program (TECH-21)**, **Interchange JSON (artifact)**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); `ia/specs/persistence-system.md`; `docs/planned-domain-ideas.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**, **TECH-41**, **TECH-44a**, **TECH-44**
  - Spec: (removed after closure — **glossary** **JSON program (TECH-21)**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-40**/**TECH-41**/**TECH-44a**/**TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella phases **TECH-40**/**TECH-41**/**TECH-44a** **§ Completed**; **Save data** format unchanged without a migration issue; charter **Decision Log** and **Open Questions** trace live in **glossary** + durable docs. **Ongoing process:** any **Save data** change needs a tracked migration issue; keep brainstorm FAQ aligned when editing interchange docs. **B2** append-only line log → **TECH-43** (open). **Postgres**/**IA** evolution: **TECH-44** **§ Completed**, **TECH-18**.
  - Depends on: none

- [x] **TECH-55b** — **Editor Reports: DB-first document storage + filesystem fallback** (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0005_editor_export_document.sql`; `.gitignore` (`tools/reports/.staging/`); `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; `.env.example`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **Editor export registry**; [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`AGENTS.md`](AGENTS.md)
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup** **Editor export registry** + **Node**/**PATH** troubleshooting; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **DB-first** **`document jsonb`**; **`tools/reports/`** fallback; quiet success **`Debug.Log`** (optional verbose **EditorPrefs**); **`DATABASE_URL`** via **EditorPrefs** / **`.env.local`**; **`node`** resolution for GUI-launched **Unity** (**Volta**/Homebrew/**EditorPrefs**/**`NODE_BINARY`**); optional **`backlog_issue_id`** (**NULL** when unset); no backlog id as **Editor** product branding. **Operational:** run **`npm run db:migrate`** (**`0004`**/**`0005`**) before **`editor_export_*`** exist; **Postgres** user in **`DATABASE_URL`** must match local roles (e.g. Homebrew vs `postgres`).
  - Depends on: **TECH-55** **§ Completed**
  - Related: **TECH-44b**/**c** **§ Completed**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived; absorbed former **TECH-59**)

- [x] **TECH-55** — **Automated Editor report registry** (Postgres, per **Reports** export type) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; `db/migrations/0004_editor_export_tables.sql`; `db/migrations/0005_editor_export_document.sql`; `tools/postgres-ia/register-editor-export.mjs`; root `package.json`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) — **Editor export registry**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**
  - Spec: (removed after closure — glossary **Editor export registry**; **unity-development-context** §10; **postgres-ia-dev-setup**; **postgres-interchange-patterns** **Program extension mapping**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-55b**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Per-export **`editor_export_*`** **B1** tables, **`register-editor-export.mjs`**, **`EditorPostgresExportRegistrar`**; **`normalizeIssueId`** parity with **`backlog-parser.ts`**. **TECH-55b** superseded persistence to **DB-first** full body + filesystem fallback (same closure batch). Does not replace **`dev_repro_bundle`** (**TECH-44c**).
  - Depends on: **TECH-44b** **§ Completed** (soft: **TECH-44c** **§ Completed**)
  - Related: **TECH-55b** **§ Completed**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived)

- [x] **TECH-58** — **Agent closeout efficiency:** **project-spec-close** (**MCP** + **Node**) (2026-04-03)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts`; `tools/mcp-ia-server/src/tools/project-spec-closeout-digest.ts`, `spec-sections.ts`; `tools/mcp-ia-server/src/tools/spec-section.ts` (shared extract); `tools/mcp-ia-server/scripts/project-spec-closeout-report.ts`, `project-spec-dependents.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/closeout-parse.test.ts`, `tests/tools/spec-section-batch.test.ts`; root `package.json` (`closeout:*`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ARCHITECTURE.md`](ARCHITECTURE.md); `AGENTS.md`; `ia/rules/agent-router.md`, `mcp-ia-default.md`; [`ia/skills/project-spec-close/SKILL.md`](ia/skills/project-spec-close/SKILL.md); [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md); [`ia/specs/glossary.md`](ia/specs/glossary.md) — **project-spec-close** / **IA index manifest** / **Reference spec** rows; `tools/mcp-ia-server/src/index.ts` (v0.4.3)
  - Spec: (removed after closure — [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows** + **Tools**; **glossary** **project-spec-close**; **project-spec-close** **`SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Lessons learned (TECH-58 closure)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + `project-implementation-validation`):** **`project_spec_closeout_digest`**, **`spec_sections`**, **`closeout:worksheet`** / **`closeout:dependents`** / **`closeout:verify`**; shared parser for future **TECH-48**. **TECH-51** closeout ordering unchanged. **`npm run verify`** / **`test:ia`** green.
  - Depends on: none (soft: **TECH-48**, **TECH-30**, **TECH-18**)

- [x] **TECH-56** — **Cursor Skill:** **`/project-new`** — new **BACKLOG** row + initial **project spec** + cross-links (**territory-ia** + optional web) (2026-04-06)
  - Type: documentation / agent enablement (**Cursor Skill** + **BACKLOG** / `ia/projects/` hygiene)
  - Files: `ia/skills/project-new/SKILL.md`; [`ia/skills/README.md`](ia/skills/README.md); `AGENTS.md` item 5; `ia/specs/glossary.md` — **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Project spec workflows**
  - Spec: (removed after closure — [`ia/skills/project-new/SKILL.md`](ia/skills/project-new/SKILL.md); **glossary** **project-new**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **create-first** **Tool recipe (territory-ia)**; **`backlog_issue`** resolves **`BACKLOG.md`** then [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) ([`docs/mcp-ia-server.md`](docs/mcp-ia-server.md)); optional **`web_search`** external-only; **`npm run validate:dead-project-specs`** after new **`Spec:`** paths. **Decision Log:** skill folder **`project-new`**; revisit recipe when **TECH-48** ships. Complements **kickoff** / **implement** / **close** / **project-implementation-validation**.
  - Depends on: none (soft: [ia/skills/README.md](ia/skills/README.md); **TECH-49**–**TECH-52** **§ Completed** for sibling patterns)

- [x] **TECH-44** — **Postgres + interchange patterns** (merged program umbrella; charter closed) (2026-04-05)
  - Type: technical / infrastructure + architecture (program umbrella)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (**Program extension mapping (E1–E3)**); **TECH-44a**/**b**/**c** **§ Completed** rows (same section); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; `AGENTS.md` (umbrella programs); `ia/specs/glossary.md` — **Postgres interchange patterns**, **JSON program (TECH-21)**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) **Program extension mapping**; **glossary** **Postgres interchange patterns**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44a**/**b**/**c**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** Charter **§4** satisfied (**TECH-44a**/**b**/**c** **§ Completed**). **E2**/**E3** remain **TECH-53**/**TECH-54** (open); **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**. **Decision Log** entries migrated into [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) and **glossary**. **ID hygiene:** former erroneous **TECH-44** id on **project-spec-kickoff** completion → **TECH-57** (see below).
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-44c** — **Dev repro bundle registry** (**E1**) (2026-04-04)
  - Type: tooling / agent enablement
  - Files: `db/migrations/0003_dev_repro_bundle.sql`; `tools/postgres-ia/register-dev-repro.mjs`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Dev repro bundle registry**); [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md) (Related pointer); repo root `package.json` (`db:register-repro`); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `ia/specs/unity-development-context.md` §10 (**Postgres registry** blurb); `ia/specs/glossary.md` — **Dev repro bundle**
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); glossary **Dev repro bundle**; **unity-development-context** §10; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **`dev_repro_bundle`** **B1** table + **`dev_repro_list_by_issue`**; **`register-dev-repro.mjs`** with **`normalizeIssueId`** parity to **`backlog-parser.ts`** (keep in sync — lesson in glossary). **Save data** / **Load pipeline** unchanged. Per-export **Unity** automation → **TECH-55** **§ Completed** (glossary **Editor export registry**).
  - Depends on: **TECH-44b** **§ Completed**

- [x] **TECH-44b** — Game **PostgreSQL** database; first milestone — **IA** schema + minimal read surface (2026-04-03)
  - Type: infrastructure / tooling
  - Files: `db/migrations/`; `tools/postgres-ia/`; `docs/postgres-ia-dev-setup.md`; `.env.example`; repo root `package.json` (`db:migrate`, `db:seed:glossary`, `db:glossary`); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) (**PostgreSQL IA** subsection for **TECH-18**); `ia/specs/glossary.md` — **Postgres interchange patterns** row (**TECH-44b** milestone); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md); `docs/agent-tooling-verification-priority-tasks.md` (row 11); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; `ia/projects/TECH-18.md` (**Current State**); `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts` (open-issue fixture — e.g. **TECH-75d**)
  - Spec: (removed after closure — [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) **Shipped decisions**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **glossary** **Postgres interchange patterns**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + local migrate/seed/smoke):** Versioned **IA** tables (`glossary`, `spec_sections`, `invariants`, `relationships`); **`ia_glossary_row_by_key`**; **`tools/postgres-ia/`** migrate/seed/read scripts; **`DATABASE_URL`** / **`.env.example`**; **MCP** remains **file-backed** until **TECH-18**. Does **not** replace Markdown authoring or **I1**/**I2** **CI** checks.
  - Depends on: **TECH-44a** **§ Completed**

- [x] **TECH-44a** — **Interchange + PostgreSQL patterns** (**B1**, **B3**, **P5**) (2026-04-03)
  - Type: technical / architecture (documentation)
  - Files: [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); `ia/specs/persistence-system.md` (pointer); `ia/specs/glossary.md` — **Postgres interchange patterns (B1, B3, P5)**, **Interchange JSON** Spec column, **JSON program (TECH-21)**; [`ARCHITECTURE.md`](ARCHITECTURE.md); [`projects/ia-driven-dev-backend-database-value.md`](projects/ia-driven-dev-backend-database-value.md), [`projects/json-use-cases-brainstorm.md`](projects/json-use-cases-brainstorm.md), `docs/mcp-ia-server.md`, `docs/planned-domain-ideas.md`, `docs/cursor-agents-skills-mcp-study.md`, `docs/agent-tooling-verification-priority-tasks.md`; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44** (umbrella — filed after **TECH-44a** closure), **TECH-21**
  - Spec: (removed after closure — [`docs/postgres-interchange-patterns.md`](docs/postgres-interchange-patterns.md); **glossary** **Postgres interchange patterns**, **JSON program (TECH-21)**; **persistence-system** §Save; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-44**/**TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase C** of **TECH-21**. Normative **B1** row+**JSONB**, **B3** idempotent **patch** **envelope**, **P5** streaming, SQL vs **`artifact`** naming; explicit **Save data** / **Load pipeline** separation. **B2** → **TECH-43** only. Former **TECH-42** scope under **TECH-44** program.
  - Depends on: **TECH-41** **§ Completed** (soft: **TECH-40** **§ Completed**)

- [x] **TECH-41** — **JSON** payloads for **current** systems: **geography** params, **cell**/**chunk** interchange, snapshots, DTO layers (2026-04-11)
  - Type: technical / performance enablement
  - Files: `Assets/StreamingAssets/Config/geography-default.json`; `Assets/Scripts/Managers/GameManagers/GeographyInitParamsDto.cs`, `GeographyInitParamsLoader.cs`; `GeographyManager.cs`, `MapGenerationSeed.cs`; `Assets/Scripts/Editor/InterchangeJsonReportsMenu.cs`; `docs/schemas/cell-chunk-interchange.v1.schema.json`, `world-snapshot-dev.v1.schema.json`, `docs/schemas/README.md`; `tools/mcp-ia-server/src/schemas/geography-init-params-zod.ts`, `scripts/validate-fixtures.ts`, `tests/schemas/`; `ia/specs/glossary.md` — **Interchange JSON**, **geography_init_params**; **`ARCHITECTURE.md`** — **Interchange JSON**; **persistence-system** / **unity-development-context** cross-links
  - Spec: (removed after closure — **glossary** + **`ARCHITECTURE.md`** + [`docs/schemas/README.md`](docs/schemas/README.md) + **unity-development-context** §10 + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase B** of **JSON program (TECH-21)**. **G4** optional **`geography_init_params`** load from **StreamingAssets**; **G1**/**G2** Editor exports under **`tools/reports/`**; Zod parity + **`validate:fixtures`**; **E3** layering documented; **Save data** unchanged. **Deferred to FEAT-46:** apply **`water.seaBias`** / **`forest.coverageTarget`** to simulation. **`backlog_issue`** test target: open **Agent** lane row (e.g. **TECH-75d**).
  - Depends on: none (**TECH-40** completed — **§ Completed** **TECH-40**)

- [x] **TECH-40** — **JSON** infra: artifact identity, schemas, **CI** validation, **spec** + **glossary** indexes (2026-04-11)
  - Type: tooling / data interchange
  - Files: `docs/schemas/` (pilot schema + fixtures); repo root `package.json` (`validate:fixtures`, `generate:ia-indexes`, `validate:dead-project-specs`, `test:ia`); `tools/mcp-ia-server/scripts/validate-fixtures.ts`, `generate-ia-indexes.ts`, `src/ia-index/glossary-spec-ref.ts`, `data/spec-index.json`, `data/glossary-index.json`; `.github/workflows/ia-tools.yml`; `projects/json-use-cases-brainstorm.md` (policy §); `docs/mcp-ia-server.md`; `ia/specs/glossary.md` — **Documentation** (**IA index manifest**, **Interchange JSON**); [REFERENCE-SPEC-STRUCTURE.md](ia/specs/REFERENCE-SPEC-STRUCTURE.md) § Conventions item 7
  - Spec: (removed after closure — **glossary** + **REFERENCE-SPEC-STRUCTURE** + [`docs/schemas/README.md`](docs/schemas/README.md) + [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) + **JSON program (TECH-21)**; [`BACKLOG.md`](BACKLOG.md) **§ Completed** **TECH-21**; this row)
  - Notes: **Completed (verified — `/project-spec-close`):** **Phase A** of **JSON program (TECH-21)**. **`artifact`** / **`schema_version`** policy; JSON Schema Draft **2020-12** pilot **`geography_init_params`**; **`npm run validate:fixtures`**; committed **I1**/**I2** with **`generate:ia-indexes -- --check`** in **CI**. **`backlog_issue`** integration test uses an open issue in the **Agent** lane (e.g. **TECH-75d**). **Related:** **TECH-24**, **TECH-30**, **TECH-34**; **TECH-43** **Depends on** updated.
  - Depends on: none (soft: align **TECH-37** **Zod** when touching **compute-lib**)

- [x] **TECH-57** — **Cursor Skills:** **infrastructure** + **kickoff** skill (project **spec** review / IA alignment) (2026-04-11)
  - Type: documentation / agent enablement (**Cursor Skill** + repo docs — no runtime game code)
  - Files: `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/templates/project-spec-review-prompt.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`
  - Spec: (removed after closure — conventions live under **`ia/skills/`** and **§4.4** of [`docs/cursor-agents-skills-mcp-study.md`](docs/cursor-agents-skills-mcp-study.md))
  - Notes: **Completed (verified per user):** Part 1 **README** + authoring rules; Part 2 **project-spec-kickoff** **`SKILL.md`** with **Tool recipe (territory-ia)** (`backlog_issue` → `invariants_summary` → `router_for_task` → …); paste template; **AGENTS.md** item 5 + doc hierarchy pointer; study doc **§4.4**. **Lesson (persisted in README):** **`router_for_task`** `domain` strings should match **`ia/rules/agent-router.md`** task-domain row labels (e.g. `Save / load`), not ad-hoc phrases. **Follow-up:** **TECH-48** (MCP discovery), **TECH-45**–**TECH-47** (domain skills). **Renumbered from erroneous id TECH-44** (collision with Postgres program **TECH-44** — corrected 2026-04-05).
  - Depends on: none

- [x] **TECH-49** — **Cursor Skill:** **implement** a **project spec** (execution workflow after kickoff) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** only)
  - Files: `ia/skills/project-spec-implement/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md` (cross-link); `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md`; `docs/mcp-ia-server.md`; `ia/templates/project-spec-review-prompt.md`
  - Spec: (removed after closure — workflow in **`ia/skills/project-spec-implement/SKILL.md`**; closure record in this row)
  - Notes: **Completed (verified per user request to implement):** **project-spec-implement** **`SKILL.md`** with **Tool recipe (territory-ia)** (per-phase loop, **Branching**, **Seed prompt**, **unity-development-context** §10 pointer); README index row; **AGENTS.md** project-spec bullets + doc hierarchy; study doc **§4.4**; **`docs/mcp-ia-server.md`** “Project spec workflows”; paste template “After review: implement”. **Dry-run:** Meta — authoring followed the recipe while implementing this issue.
  - Depends on: none (soft: **TECH-57**)

- [x] **TECH-50** — **Doc hygiene:** **cascade** references when **project specs** close; **dead links**; **BACKLOG** as durable anchor (2026-04-03)
  - Type: tooling / doc hygiene / agent enablement
  - Files: `tools/validate-dead-project-spec-paths.mjs`; repo root `package.json` (`validate:dead-project-specs`); `.github/workflows/ia-tools.yml`; `ia/projects/PROJECT-SPEC-STRUCTURE.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/agent-tooling-verification-priority-tasks.md`; `tools/mcp-ia-server/README.md` (pointer only)
  - Spec: (removed after closure — **PROJECT-SPEC-STRUCTURE** closeout + **Lessons learned (TECH-50 closure)**; **`docs/mcp-ia-server.md`** **Project spec path hygiene**; this row)
  - Notes: **Completed (verified per user):** `npm run validate:dead-project-specs` + CI gate; **BACKLOG** checks strict **`Spec:`** lines on open rows only; **BACKLOG-ARCHIVE.md** excluded; advisory `--advisory` / `CI_DEAD_SPEC_ADVISORY=1`. **Lessons:** See **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-50 closure)**. **Deferred:** optional **territory-ia** MCP tool; shared **Node** module with **TECH-30**.
  - Depends on: none (soft: **TECH-30** — merge or share implementation)
  - Related: **TECH-51** completed — **`project-spec-close`** documents `npm run validate:dead-project-specs` in the closure workflow

- [x] **TECH-51** — **Cursor Skill:** **`project-spec-close`** — full **issue** / **project spec** closure workflow (IA, lessons, **BACKLOG**, cascade) (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `ia/skills/project-spec-close/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/skills/project-spec-implement/SKILL.md`; `AGENTS.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `docs/mcp-ia-server.md`; `ia/specs/glossary.md` — **Documentation**; `ia/projects/PROJECT-SPEC-STRUCTURE.md`
  - Spec: (removed after closure — **`ia/skills/project-spec-close/SKILL.md`**; **PROJECT-SPEC-STRUCTURE** **Closeout checklist** + **Lessons learned (TECH-51 closure)**; **glossary** **Project spec** / **project-spec-close**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** **IA persistence checklist** + ordered **Tool recipe (territory-ia)**; **persist IA → delete project spec → `validate:dead-project-specs` → BACKLOG Completed** (user-confirmed). **Decisions:** no duplicate **TECH-50** scanner in the skill; composite **closeout_preflight** MCP deferred (**TECH-48** / follow-up). **Related:** **TECH-52** completed — optional **`project-implementation-validation`** before closeout cascade when IA-heavy.
  - Depends on: none (soft: **TECH-50**, **TECH-57**, **TECH-49**)

- [x] **TECH-52** — **Cursor Skill:** **`project-implementation-validation`** — post-implementation tests + available code validations (2026-04-03)
  - Type: documentation / agent enablement (**Cursor Skill** + process)
  - Files: `ia/skills/project-implementation-validation/SKILL.md`; `ia/skills/README.md`; `ia/skills/project-spec-implement/SKILL.md`; `ia/skills/project-spec-kickoff/SKILL.md`; `ia/skills/project-spec-close/SKILL.md`; `AGENTS.md`; `docs/mcp-ia-server.md`; `docs/cursor-agents-skills-mcp-study.md` §4.4; `tools/mcp-ia-server/README.md`
  - Spec: (removed after closure — **`ia/skills/project-implementation-validation/SKILL.md`**; **glossary** **Documentation** — **project-implementation-validation**; **PROJECT-SPEC-STRUCTURE** — **Lessons learned (TECH-52 closure)**; this row)
  - Notes: **Completed (verified per user — `/project-spec-close`):** ordered **validation manifest** (**IA tools** **CI** parity + advisory **`verify`**); **skip** matrix; **failure policy**; cross-links to **implement** / **close** / **kickoff**; **Phase 3** root aggregate **`npm run`** not shipped (optional **BACKLOG** follow-up). **Deferred:** **`run_validations`** MCP (**TECH-48** / follow-up); **Unity** one-liner → **TECH-15** / **TECH-16** / **UTF**.
  - Depends on: none (soft: **TECH-49**, **TECH-50**, **TECH-51**)
  - Related: **TECH-48** — MCP “validation bundle” tool out of scope unless new issue

*(Older batch moved to [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) § **Recent archive** on 2026-04-10. Add new completions here for ~30 days, then archive.)*

> Full history: [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md).

---

---

## Recent archive (moved from BACKLOG.md, 2026-04-10)

- [x] **TECH-262** — `web/drizzle.config.ts` + `db:generate` script (Stage 5.2 Phase 1) (2026-04-17)
  - Type: web platform / tooling
  - Files: `web/drizzle.config.ts` (new), `web/package.json`, `web/README.md`, `web/drizzle/` (new dir)
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `web/drizzle.config.ts` (new) exports `defineConfig({ schema: './lib/db/schema.ts', out: './drizzle', dialect: 'postgresql', dbCredentials: { url: process.env.DATABASE_URL ?? 'postgresql://placeholder' } })` — drizzle-kit v0.20+ `dialect` key; placeholder fallback prevents `db:generate` crash offline (generate is schema-only, no DB hit). `web/package.json` `scripts.db:generate` = `"drizzle-kit generate"` (no `--config` flag — drizzle-kit picks `drizzle.config.ts` at `cwd`). `cd web && npm run db:generate` offline produces `web/drizzle/0000_*.sql` + `web/drizzle/meta/{_journal,0000_snapshot}.json` (CREATE TABLE statements for user/session/save/entitlement w/ FK cascade). `web/README.md §Portal` extended w/ "Migration tooling" subsection — documents `db:generate` purpose + output dir + commit stance + "Step 5 architecture-only — no `db:migrate` script" boundary. Decision Log — commit `web/drizzle/` (not gitignore) per drizzle convention + PR-reviewable schema diffs; `dialect: 'postgresql'` + `DATABASE_URL` placeholder fallback (contributor first-run DX); NO `db:migrate` script in Stage 5.2 (Step 5 architecture-only; migrations in post-Step-5 portal-launch plan). Validate: `npm run validate:all` exit 0. Stage 5.2 Phase 1 second Exit bullet satisfied.
  - Depends on: **TECH-261** (archived — drizzle-orm + schema.ts)

- [x] **TECH-261** — `drizzle-orm` install + `web/lib/db/schema.ts` (user/session/save/entitlement) (Stage 5.2 Phase 1) (2026-04-17)
  - Type: web platform / data model
  - Files: `web/package.json`, `web/lib/db/schema.ts` (new)
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `drizzle-orm` + `drizzle-kit` pinned in `web/package.json` (`dependencies` + `devDependencies`); `web/lib/db/schema.ts` (new) exports 4 typed `pgTable` consts — `user` (uuid PK `defaultRandom()` + `email text unique not null` + `passwordHash text not null` (argon2id digest) + `createdAt timestamptz defaultNow()`), `session` (uuid PK + `userId uuid FK→user.id onDelete cascade` + `expiresAt timestamptz not null` + `token text not null`), `save` (uuid PK + `userId FK cascade` + `data jsonb $type<unknown>() not null` + `updatedAt timestamptz defaultNow()`), `entitlement` (uuid PK + `userId FK cascade` + `tier text not null` + `grantedAt timestamptz defaultNow()`) + 8 inferred types via `$inferSelect` / `$inferInsert`. Column shape matches Stage 5.1 auth-lib lock (TECH-253 Decision Log: roll-own JWT + sessions, `SESSION_COOKIE_NAME=portal_session`, `SESSION_LIFETIME_DAYS=30`, `@node-rs/argon2`). Decision Log — UUID PKs over bigserial (enumeration leak + Stage 5.1 contract); TIMESTAMPTZ over plain timestamp (Vercel UTC vs Neon session-tz drift); `onDelete: 'cascade'` over restrict (GDPR account-delete intent, saves app-level cascade scripts later); `$type<unknown>()` over `Record<string, unknown>` on `save.data` (refined by TECH-263+ when save shape locks). No migrations run; `drizzle` adapter wrap deferred to TECH-262. Validate: `cd web && npm run typecheck` green; `npm run validate:all` green; `npm run validate:web` green. Stage 5.2 Phase 1 first Exit bullet satisfied.
  - Depends on: **TECH-254** (archived — `web/lib/db/client.ts` lazy singleton)

- [x] **TECH-226** — README §Components Sidebar entry + validation closeout (Stage 4.1 Phase 2) (2026-04-16)
  - Type: docs / web workspace
  - Files: `web/README.md`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `web/README.md` gained new `## Components` section (sibling of `## Tokens`) with `### Sidebar` subsection — six bullets: lucide-react named-import dependency (tree-shake via `Home` / `BookOpen` / `Newspaper` / `LayoutDashboard` / `Menu` / `X`, no barrel); `'use client'` rationale (`usePathname()` + `useState` both need browser runtime); active-route styling via inline `style` + `tokens.colors['text-accent-warn']` + `tokens.colors['bg-panel']` (NOT bare `text-accent` — palette only exposes amber-warn + critical-red); mobile overlay pattern (hamburger `md:hidden fixed top-4 left-4 z-50`, nav wrapper `fixed inset-y-0 left-0 w-48 z-40 transform transition-transform`, open/closed `translate-x-0` / `-translate-x-full`); desktop same-element responsive `md:static md:translate-x-0` (NOT `hidden md:flex` wrapper — Sidebar owns own responsive classes, wrapper would break TECH-224 mobile overlay); token-consumption inline-`style` map via `@/lib/tokens` (JSON keys resolved at build, NOT Tailwind utilities). Decision Log — separate `## Components` section over inline under `## Tokens` (components ≠ tokens; future Button / PlanChart share this bucket); `text-accent-warn` over bare `text-accent` (palette audit); inline-`style` over class-string (matches ship source); same-element responsive over wrapper (matches TECH-225 layout). Validate — `npm run validate:all` green (lint + typecheck + next build + IA validators); zero `lucide-react` TS2307 / TS2305 diagnostics. Final Stage 4.1 exit gate satisfied; sibling TECH-223 + TECH-224 + TECH-225 all archived. Master-plan row T4.1.4 already flipped `Done` pre-closeout.
  - Depends on: **TECH-225** (archived)

- [x] **TECH-225** — Root layout integration for Sidebar (Stage 4.1 Phase 2) (2026-04-16)
  - Type: infrastructure / web workspace / layout
  - Files: `web/app/layout.tsx`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `web/app/layout.tsx` restructured to horizontal shell. Outer `<body className="min-h-full flex flex-col">` preserved; inner row `<div className="flex flex-1 min-h-0">` wraps `<Sidebar />` + `<main className="flex-1 min-w-0 overflow-auto">{children}</main>`; existing footer (Devlog + RSS) stays below row. `<html>` classes (`${geistSans.variable} ${geistMono.variable} h-full antialiased`) + metadata export + all lib imports (`getBaseUrl`, `siteTitle`, `siteTagline`, `tokens`) preserved. `<Sidebar />` rendered directly — no `hidden md:flex` wrapper (Sidebar root `<nav>` owns `fixed ... md:static md:translate-x-0 w-48`, wrapping would break TECH-224 mobile overlay). Decision Log — keep outer `<body>` shell + footer (replacing wholesale deletes Devlog/RSS links); render `<Sidebar />` directly (wrapper slot breaks mobile); inner row uses `flex flex-1 min-h-0` not `flex min-h-screen` (min-h-screen double-counts vs outer `min-h-full` → footer pushed off-screen); `min-w-0` on `<main>` prevents flexbox child overflow from long tables / pre blocks. Validate: `cd web && npm run typecheck` + `npm run lint` + `npm run validate:web` + `npm run validate:all` all green. Phase 2 of Stage 4.1; sibling TECH-226 (README §Components) still open.
  - Depends on: **TECH-224** (archived)

- [x] **TECH-224** — Sidebar active-route highlight + mobile overlay toggle (Stage 4.1 Phase 1) (2026-04-16)
  - Type: infrastructure / web workspace / component
  - Files: `web/components/Sidebar.tsx`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `web/components/Sidebar.tsx` flipped to `'use client'`. `usePathname()` drives per-link `active = pathname === href` → `text-accent-warn bg-panel rounded` (token corrected at kickoff — palette has only `text-accent-warn` amber + `text-accent-critical` red, no plain `text-accent`; warn-amber chosen so red stays destructive-only semantics). Mobile overlay: `useState(false)` `open` bool + lucide `Menu` / `X` toggle button (`md:hidden fixed top-4 left-4 z-50`); `<nav>` keeps `fixed inset-y-0 left-0 w-48 z-40 transform transition-transform md:static md:translate-x-0` and toggles `translate-x-0` / `-translate-x-full` on `open` (DOM-resident for slide anim, NOT `hidden`). Each `<Link>` calls `setOpen(false)` → overlay auto-dismisses on mobile nav. Phase 0 preflight confirmed `usePathname` + lucide `Menu` / `X` resolve under `next@16.2.3` + `lucide-react@^1.8.0`. Stack reality: workspace runs Tailwind v4 CSS-first config in `web/app/globals.css` `@theme` (no `tailwind.config.ts`); `--color-text-accent-warn` already declared. Validate: `cd web && npm run lint && npm run typecheck && npm run build` green; `npm run validate:all` green. Decision Log — `fixed inset-y-0` + `md:static` single-component pattern over CSS `@media` + dual components; auto-close overlay on link tap (UX convention); amber over critical-red for active highlight (semantics); `-translate-x-full` over `hidden` (preserve slide). Issues Found — Next 16 `usePathname()` returns non-nullable `string` (Next 13/14 was `string | null`); no null-guard needed (lesson migrated to MEMORY.md). Phase 1 of Stage 4.1; siblings TECH-225 (root layout wiring) + TECH-226 (README §Components) still open.
  - Depends on: **TECH-223** (archived)

- [x] **TECH-209** — UI/Eco/Sys BlipPatch SO authoring (Stage 3.1 Phase 1) (2026-04-15)
  - Type: infrastructure / audio authoring
  - Files: `Assets/Audio/Blip/Patches/UiButtonHover.asset`, `UiButtonClick.asset`, `EcoMoneyEarned.asset`, `EcoMoneySpent.asset`, `SysSaveGame.asset`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** 5 UI/Eco/Sys **Blip patch** SOs authored via `CreateAssetMenu` `Territory/Audio/Blip Patch`. Dir landed as `Assets/Audio/Blip/Patches/` (Stage 1.4 path, not `Assets/Audio/BlipPatches/`). Params frozen to `docs/blip-procedural-sfx-exploration.md` §9 — `UiButtonHover` (ex 1, triangle 2000 Hz, `cooldownMs` 120), `UiButtonClick` (ex 2, square 1000 Hz), `EcoMoneyEarned` (ex 17, sine 1319 Hz), `EcoMoneySpent` (ex 18, triangle 200 Hz + noise), `SysSaveGame` (ex 20, 3× triangle 523/659/784 Hz, `cooldownMs` 2000). Post-MVP FX trimmed (pitch env, ring-mod, delay, BP filter, 4th note, stereo widen) — base carrier only for MVP smoke. `patchHash` non-zero (computed offline; Editor verify deferred to TECH-212). `npm run unity:compile-check` green (bridge `compilation_failed=false`). Decision Log — authoring-only so params stay frozen to exploration §9 (drift → amend doc first); `cooldownMs` defaults to 0 when §9 silent (UI click-rate = user input cadence, no spam); `mixerGroup` left null intentionally (TECH-211 wires all 10 atomic). Half of Stage 3.1 Phase 1 patch-set — sibling TECH-210 covers 5 World patches.
  - Depends on: none

- [x] **TECH-203** — Plan-loader README + JSDoc (Stage 3.1 Phase 2) (2026-04-15)
  - Type: infrastructure / web workspace
  - Files: `web/README.md` (extend), `web/lib/plan-loader.ts` (extend — JSDoc)
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `web/README.md` gained §Dashboard between §MDX page pattern + §Tokens — documents `loadAllPlans(): Promise<PlanData[]>` contract, `PlanData` key fields (`title`, `overallStatus`, `steps[]`, `allTasks[]`), "parse.mjs authoritative — plan-loader read-only wrapper" invariant, glob pattern `ia/projects/*master-plan*.md` (code-accurate, NOT shorthand), RSC consumption snippet, empty-dir `[]` return behavior. `web/lib/plan-loader.ts` file-header JSDoc appended single line — `Requires Node 20+ — dynamic ESM import() of parse.mjs relies on Node ≥ 20 stable ESM resolver.` Additive only; existing header bullets untouched. Decision Log — glob wording code-accurate (not master-plan table shorthand) to prevent doc/runtime drift; JSDoc additive (no rewrite); §Dashboard placement between §MDX page pattern + §Tokens keeps narrative order (page patterns → RSC data → tokens → deploy). Closes Stage 3.1 exit criterion on docs.
  - Depends on: **TECH-200** (archived), **TECH-201** (archived), **TECH-202** (archived)

- [x] **TECH-145** — Web primitives: HeatmapCell + AnnotatedMap (Stage 1.2 Phase 2) (2026-04-14)
  - Type: IA / tooling (web workspace)
  - Files: `web/components/HeatmapCell.tsx`, `web/components/AnnotatedMap.tsx`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** SSR-only primitives under `web/components/`. `HeatmapCell({ intensity })` clamps to `[0,1]` + 5-bucket `color-mix()` ramp anchored on existing semantic aliases (`bg-panel` → `text-accent-warn` → `text-accent-critical`); no new palette rows. `AnnotatedMap({ regions, annotations })` renders `<svg viewBox="0 0 1000 600" role="img">` root w/ per-region `<path>` (bucket helper shared w/ HeatmapCell) + per-annotation `<text>` using `letterSpacing: 0.15em` + `textTransform: uppercase` (NYT-style spaced-caps geo labels). No `"use client"`; no D3-geo / topojson. Last two of six Stage 1.2 primitives — satisfies Stage 1.2 Exit bullet 2. `/design` fixture wiring + visual review deferred to TECH-146.
  - Depends on: tokens (archived)

- [x] **TECH-119** — Envelope level math (Linear + Exponential per-stage shapes) (2026-04-14)
  - Type: infrastructure / DSP math
  - Files: `Assets/Scripts/Audio/Blip/BlipEnvelope.cs` (`BlipEnvelopeStepper.ComputeLevel`)
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** Pure static `ComputeLevel(in BlipEnvelopeFlat, BlipEnvStage, int samplesElapsed, int stageDurationSamples, float releaseStartLevel) → float` on `BlipEnvelopeStepper`. Stage × shape routing: Idle/Hold/Sustain flat constants (0f / 1f / `sustainLevel`); Attack/Decay/Release drive Linear or Exponential per `BlipEnvelopeFlat.{attack,decay,release}Shape`. Linear — `t = samplesElapsed / stageDurationSamples` clamped, `start + (target − start) * t`. Exponential — `τ = stageDurationSamples / 4f`, `target + (start − target) * (float)Math.Exp(−samplesElapsed / τ)` (≈98 % settled at 4 τ). Edge — `stageDurationSamples <= 0` → return `target`. Zero allocs, no Unity API. Exponential ≈98 % settled slope + flat-constant assertions deferred to Stage 1.4 T1.4.3.
  - Depends on: **TECH-116**, **TECH-118**

- [x] **TECH-88** — `GridManager` parent-id surface + new-game placeholder allocation (2026-04-13)
  - Type: infrastructure / runtime
  - Files: `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`
  - Spec: (removed after closure — Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close`).** `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` (PascalCase properties; save fields stay lowercase `regionId` / `countryId` per TECH-87). One-shot `HydrateParentIds(regionId, countryId)` with null/empty guard + `_parentIdsHydrated` duplicate guard (`Debug.LogError` + return, no throw). `GameSaveManager.NewGame()` allocates `Guid.NewGuid()` pair post-`ResetGrid()` + hydrates eagerly (shifts allocation earlier than previous lazy-on-first-save). `LoadGame` hydrates after `MigrateLoadedSaveData` + local id cache, before `RestoreGrid`. `BuildCurrentGameSaveData` keeps fallback as defense-in-depth for scenario-builder paths. No consumers yet — surface only; consumed by ≥1 city system in Step 2. Orchestrator: `multi-scale-master-plan.md` Step 1 / Stage 1.1.
  - Depends on: **TECH-87**

- [x] **BUG-12** — Happiness UI always shows 50% (2026-04-07)
  - Type: fix
  - Files: `CityStatsUIController.cs` (GetHappiness), `GridManager.cs` (HandleBuildingStatsReset), `CityStats.cs` (RemoveMoney Debug.Log)
  - Spec: (removed after closure — no glossary/reference spec changes; Decision Log persisted to Postgres journal)
  - Notes: **Completed (verified — `/project-spec-close` + user):** `GetHappiness()` now reads `cityStats.happiness` instead of returning hardcoded `50.0f`. Format changed from `{F1}%` to `{N0}` (raw integer) for consistency with legacy HUD. Also fixed: bulldoze not reversing stats for developed buildings (`HandleBuildingStatsReset` skipped `HandleBuildingDemolition` when `buildingType != null`); removed noisy `Debug.Log` in `RemoveMoney`. `GetHappinessColor` thresholds kept as-is — revisit in **FEAT-23**.

- [x] **TECH-76** — **Information Architecture** system overview document (2026-04-07)
  - Type: documentation
  - Files: `docs/information-architecture-overview.md` (new); `AGENTS.md` (cross-link); `ARCHITECTURE.md` (cross-link)
  - Spec: (removed after closure — this row)
  - Notes: **Completed (verified).** Single ~220-line document at [`docs/information-architecture-overview.md`](docs/information-architecture-overview.md) describing the IA system as a coherent design: philosophy (slice don't load, one vocabulary, knowledge flows back), layer diagram (ASCII), 6-stage knowledge lifecycle, semantic model axes (vocabulary/routing/invariants), consistency mechanisms table, MCP tool ecosystem, skill system lifecycle table, optional Postgres layer, and 6 extension checklists (reference spec, MCP tool, skill, glossary term, rule, Postgres table). Cross-linked from `AGENTS.md` documentation hierarchy and `ARCHITECTURE.md` § Agent IA. **IA evolution lane** context: [`docs/ia-system-review-and-extensions.md`](docs/ia-system-review-and-extensions.md).
  - Depends on: none

- [x] **TECH-84** — **High-priority MCP diagnostic & discovery tools** (six-tool suite) (2026-04-07)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/tools/backlog-search.ts`, `tools/mcp-ia-server/src/tools/invariant-preflight.ts`, `tools/mcp-ia-server/src/tools/findobjectoftype-scan.ts`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` (extended `kind` enum); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (three new bridge cases + `CreateOk` factory); `tools/mcp-ia-server/src/index.ts`; `docs/mcp-ia-server.md` (28 tools); `tools/mcp-ia-server/README.md` (27 tools)
  - Spec: (removed after closure — **IA project spec journal**; this row)
  - Notes: **Completed (verified).** Six MCP tools shipped in **territory-ia** v0.5.0: **(1) `backlog_search`** — keyword search across backlog issues. **(2) `invariant_preflight`** — composite context bundle (invariants + router + spec sections) for an issue. **(3) `findobjectoftype_scan`** — static C# scan for per-frame `FindObjectOfType` violations. **(4) `economy_balance_snapshot`** — bridge: economy/happiness/demand from Play Mode. **(5) `prefab_manifest`** — bridge: scene MonoBehaviours + missing scripts. **(6) `sorting_order_debug`** — bridge: renderers + sorting order at a cell. 115 tests pass; `npm run verify` green. Also added `parseAllBacklogIssues` to `backlog-parser.ts`, exported `parseInvariantsBody` and `collectRouterData` for internal reuse, C# `AgentBridgeResponseFileDto.CreateOk` factory method. **Migrated content:** [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md).
  - Depends on: none

- [x] **TECH-75** — **Close Dev Loop** orchestration: agent-driven Play Mode verification (2026-04-07)
  - Type: orchestration spec (no umbrella BACKLOG row)
  - Files: (removed after closure — **glossary** **IDE agent bridge**; **`close-dev-loop`** Skill; **`bridge-environment-preflight`** Skill; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); **IA project spec journal**; this row)
  - Spec: (removed after closure)
  - Notes: **Completed (verified):** All sub-issues shipped: **TECH-75a** (Play Mode bridge `kind` values), **TECH-75b** (`debug_context_bundle` + anomaly scanner), **TECH-75c** (`close-dev-loop` Skill + compile gate), **TECH-75d** (dev environment preflight). Agent can enter Play Mode, collect evidence, detect anomalies, verify fixes, and exit — zero human Unity interaction. MVP exit criteria met. Absorbed **TECH-59** (MCP staging superseded by direct Play Mode control). Open follow-ups: `unity_debug_bundle` sugar tool (deferred); Game view auto-focus; multi-seed-cell bundle.
  - Depends on: none
  - Related: **TECH-75a**, **TECH-75b**, **TECH-75c**, **TECH-75d** (all **§ Recent archive**)

- [x] **TECH-75d** — **Close Dev Loop**: dev environment **preflight** (Postgres + **IDE agent bridge** readiness) (2026-04-07)
  - Type: tooling / agent enablement (**scripts** + **Cursor Skill** + docs)
  - Files: `tools/mcp-ia-server/scripts/bridge-preflight.ts`; root `package.json` (`db:bridge-preflight`); `ia/skills/bridge-environment-preflight/SKILL.md`; `ia/skills/README.md`; `ia/skills/close-dev-loop/SKILL.md` (Step 0); `ia/skills/ide-bridge-evidence/SKILL.md`; `AGENTS.md`; `docs/postgres-ia-dev-setup.md`; `docs/mcp-ia-server.md`; `config/README.md`; orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **bridge-environment-preflight** Skill; **close-dev-loop** Step 0; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) **Bridge environment preflight**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; this row)
  - Notes: **Completed (verified):** Node preflight script (`bridge-preflight.ts`) with stable exit codes 0–4; imports `resolveIaDatabaseUrl`; checks Postgres connectivity and `agent_bridge_job` table presence. `npm run db:bridge-preflight` at repo root. **bridge-environment-preflight** Cursor Skill with bounded repair policy (one attempt per failure class). **close-dev-loop** Step 0 upgraded from optional to concrete. All four exit codes verified on dev machine (0/1/2/3 + post-migrate restore).
  - Depends on: none (soft: **`close-dev-loop`** shipped)
  - Related: **TECH-75** orchestration, **TECH-75b** (**§ Recent archive**), **TECH-75c** (**§ Recent archive**), **TECH-75a**

- [x] **TECH-75b** — **Close Dev Loop**: context bundle + anomaly detection (2026-04-09)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/AgentBridgeAnomalyScanner.cs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/scripts/bridge-playmode-smoke.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; **ide-bridge-evidence**; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Bridge **`kind`** **`debug_context_bundle`** — Moore **Agent context** export + deferred **Game view** screenshot + console snapshot + **`AgentBridgeAnomalyScanner`** rules (`missing_border_cliff`, `heightmap_cell_desync`, `redundant_shore_cliff`). CLI **`npm run db:bridge-playmode-smoke`** uses **`runUnityBridgeCommand`** (same path as MCP **`unity_bridge_command`**). Optional **`unity_debug_bundle`** MCP sugar still deferred (open **BACKLOG** follow-up if scoped).
  - Depends on: none (Play Mode bridge **`kind`** values — this file **TECH-75a**)
  - Related: **TECH-75** orchestration, **TECH-75c** (**§ Completed** — this file **Recent archive**), **TECH-75a**

- [x] **TECH-75c** — **Close Dev Loop**: Cursor Skill orchestrating fix → verify → report (2026-04-09)
  - Type: documentation / agent enablement (**Cursor Skill**) + bridge **`kind`**
  - Files: `ia/skills/close-dev-loop/SKILL.md`; `ia/skills/README.md`; [`AGENTS.md`](AGENTS.md); root [`package.json`](package.json) **`unity:compile-check`**; `tools/scripts/unity-compile-check.sh`; `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs` (**`get_compilation_status`**); `tools/mcp-ia-server/src/tools/unity-bridge-command.ts` (**`unity_compile`**); `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md) (**Agent bridge job queue** troubleshooting); [`ARCHITECTURE.md`](ARCHITECTURE.md); orchestration archived (this file **Recent archive**) §7
  - Spec: (removed after closure — **`ia/skills/close-dev-loop/SKILL.md`**; **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); **IA project spec journal**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`close-dev-loop`** Skill (before/after **`debug_context_bundle`**, **compile gate**: **`get_compilation_status`** / **`unity_compile`**, **`npm run unity:compile-check`**, **`get_console_logs`**); **`JsonUtility`** response shape note in **unity-development-context** §10. Optional **`unity_debug_bundle`** MCP sugar still deferred.
  - Depends on: none (soft: **`debug_context_bundle`** — **this file** **TECH-75b**)
  - Related: **TECH-75** orchestration, **TECH-75b**, **TECH-75d** (archived), **TECH-75a**

- [x] **BUG-54** — **Utility building** / **zoning** overlay stripped **brown cliff** stacks on **map border** **cells** (void toward **off-grid** exterior) (2026-04-10)
  - Type: bug (rendering / terrain layering)
  - Files: `GridManager.cs` (`DestroyCellChildren`, `DestroyCellChildrenExceptForest`), `TerrainManager.cs` (`IsCliffStackTerrainObject`), `BuildingPlacementService.cs`, `ZoneManager.cs` (`PlaceZone`, `PlaceZoneAt`, `RestoreZoneTile`); [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §5.7 **Cell child cleanup (overlays)**
  - Spec: (removed after closure — normative **geo** §5.7 bullet **Cell child cleanup (overlays)**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`TerrainManager.IsCliffStackTerrainObject`**; **`GridManager.DestroyCellChildren`** skips **cliff** (and existing **slope**) instances during **`destroyFlatGrass`** **building** cleanup; **`DestroyCellChildrenExceptForest`** applies the same skips so **undeveloped light zoning** brush and restore do not wipe **map border** stacks. **`RestoreTerrainForCell`** early exit on **building**-occupied **cells** prevented relying on post-place **cliff** rebuild alone.
  - Depends on: none
  - Related: **BUG-20**, **BUG-31**; archived **BUG-44** (water × **map border** — different cause)

- [x] **TECH-75a** — **Close Dev Loop**: Play Mode bridge commands + readiness signal (2026-04-08)
  - Type: tooling / agent enablement
  - Files: `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `tools/mcp-ia-server/README.md`; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); [`AGENTS.md`](AGENTS.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) (**IDE agent bridge** bullet)
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md) **Play Mode bridge smoke (MCP, agent-led)**; orchestration archived (this file **Recent archive**) §7 phase 1; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Bridge **`kind`** **`enter_play_mode`**, **`exit_play_mode`**, **`get_play_mode_status`**; readiness via **`GridManager.isInitialized`**; **`UnityEditor.SessionState`** for enter/exit wait across domain reload; **`GameView`** focus via reflection before **`EnterPlaymode`**; concurrent same-type jobs rejected; deferred screenshot pump unified in **`OnEditorUpdate`**. **MCP** smoke + optional **Play Mode** sequence documented in **`AGENTS.md`** / **`docs/mcp-ia-server.md`**. **Subsequent Close Dev Loop:** context bundle **TECH-75b** (this file); **`close-dev-loop`** Skill **TECH-75c** (this file **Recent archive**); **TECH-75d** dev preflight on [`BACKLOG.md`](BACKLOG.md).
  - Depends on: none (extends **TECH-73**/**TECH-74** Phase 1 bridge)
  - Related: **TECH-75** orchestration, **TECH-75b** (archived this file), **TECH-75c**, **TECH-73**, **TECH-74**, **TECH-59** (absorbed)

- [x] **BUG-44** — **Cliff** prefabs: black gaps when a **water body** meets the **east** or **south** **map border** (2026-04-07)
  - Type: bug
  - Files: `TerrainManager.cs` (`PlaceCliffWalls`, `GetCliffWallDropSouth`, `GetCliffWallDropEast`, `ResolveCliffWallDropAfterSuppression`, `PlaceCliffWallStackCore`, `ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary`); [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §5.6.1, §5.7; [`ia/specs/glossary.md`](ia/specs/glossary.md) **Map border**, **Cliff suppression**; [`ARCHITECTURE.md`](ARCHITECTURE.md) **Water** subsection
  - Spec: (removed after closure — normative **geo** §5.7 **Map border (exterior void)** / **Map border × water-shore**; **glossary** **Map border** / **Cliff suppression**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **South**/**east** faces toward **off-grid** void stack brown **cliff** segments to **`MIN_HEIGHT`** (full height drop); **`PlaceCliffWalls`** passes **`MIN_HEIGHT`** as low foot for stack depth. **Water-shore** primary cells suppress duplicate brown **cliff** toward that void. **Water-shore** world-**Y** nudge applies only when the lower neighbor is on-grid. No **water–water cascade** on outermost **map border** cells. **Prior** virtual-foot-from-cardinals approach dropped — see **Decision Log** in **IA project spec journal** if persisted.
  - Depends on: none
  - Related: **BUG-42**, **BUG-45**, **BUG-43**

- [x] **TECH-59** — **territory-ia** MCP: stage **Editor** export registry payload — **absorbed into Close Dev Loop** (2026-04-07)
  - Type: tooling / agent enablement
  - Files: (no implementation shipped — scope absorbed into **Close Dev Loop** (**TECH-75**))
  - Spec: (deleted — `ia/projects/TECH-59.md` removed; concept superseded by **Close Dev Loop** orchestration archived (this file **Recent archive**))
  - Notes: **Absorbed (not implemented):** Original goal was MCP staging for **Editor export registry** payload (**`backlog_issue_id`** + JSON documents) with a Unity menu to apply. Superseded because the **Close Dev Loop** program (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) lets the agent enter Play Mode and collect evidence directly, eliminating the need to pre-stage registry parameters. Registry staging may reappear as a sub-task if needed, but is no longer a standalone issue.
  - Depends on: none
  - Related: **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-55b** **§ Completed**, **TECH-48**

- [x] **TECH-73** — **Unity** ↔ **IDE** **agent bridge** program (**Phase 1** — **Postgres** **`agent_bridge_job`**) (2026-04-06)
  - Type: tooling / agent enablement (program umbrella — Phase 1 shipped)
  - Files: [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) (charter / optional later phases); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); `db/migrations/0008_agent_bridge_job.sql`; `tools/postgres-ia/agent-bridge-dequeue.mjs`; `tools/postgres-ia/agent-bridge-complete.mjs`; `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/scripts/run-unity-bridge-once.ts`; root **`npm run db:bridge-agent-context`**; `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/EditorPostgresBridgeJobs.cs`; `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs` (**ExportAgentContextForAgentBridge**); `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**, **Editor export registry**
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md); **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **territory-ia** **`unity_bridge_command`** / **`unity_bridge_get`** + **Unity** **Node** dequeue/complete; **`TryPersistReport`** **Postgres-only** (no **`tools/reports/`** fallback for registry exports). **Optional later phases** (HTTP): charter doc + open **BACKLOG** when scoped. **Console** / **screenshot** bridge kinds shipped — **TECH-74** **§ Completed** (this file). **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) supersedes **TECH-59** staging concept — agent drives Play Mode directly.
  - Depends on: none (soft: glossary **Editor export registry** — **TECH-55**/**TECH-55b** archived; **unity-development-context** §10 **Reports** menus)
  - Related: **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-48**, **TECH-33**, **TECH-38**, **TECH-18**, **BUG-53**, **TECH-74**

- [x] **TECH-74** — **territory-ia** MCP + **IDE agent bridge**: **`get_console_logs`** and **`capture_screenshot`** (2026-04-07)
  - Type: tooling / agent enablement
  - Files: `tools/mcp-ia-server/src/tools/unity-bridge-command.ts`; `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/src/index.ts`; `tools/mcp-ia-server/package.json`; `tools/mcp-ia-server/README.md`; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); `Assets/Scripts/Editor/AgentBridgeCommandRunner.cs`; `Assets/Scripts/Editor/AgentBridgeConsoleBuffer.cs`; `Assets/Scripts/Editor/AgentBridgeScreenshotCapture.cs`; [`.gitignore`](.gitignore) **`tools/reports/bridge-screenshots/`**; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §4.3; [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10; [`ia/specs/glossary.md`](ia/specs/glossary.md) **IDE agent bridge**; [`ia/skills/ide-bridge-evidence/SKILL.md`](ia/skills/ide-bridge-evidence/SKILL.md); [`AGENTS.md`](AGENTS.md); [`ia/templates/project-spec-template.md`](ia/templates/project-spec-template.md) §7b example
  - Spec: (removed after closure — **glossary** **IDE agent bridge**; **unity-development-context** §10; [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §4.3 **Shipped**; **TECH-73** **§ Completed** **Phase 1** sibling; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`unity_bridge_command`** **`kind`** **`get_console_logs`** / **`capture_screenshot`**; **`response.log_lines`**; **Play Mode** PNG under **`tools/reports/bridge-screenshots/`**; **`params.include_ui`** uses **Game view** **`ScreenCapture`** (**Overlay** UI); **`runUnityBridgeCommand`** **`timeout_ms`** default/clamp; **`@territory/mcp-ia-server`** **0.4.13**. **Node:** **`npm run verify`** / **`npm run test:ia`** green. **Skills:** optional **Play** evidence workflow **`ide-bridge-evidence`**. Charter §5.1 sugar tool names remain aliases only.
  - Depends on: none (soft: **TECH-24** when parser / **Zod** shapes for bridge tools change)
  - Related: **TECH-73**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-48**, **TECH-24**

- [x] **BUG-19** — Mouse scroll wheel in Load Game scrollable menu also triggers camera zoom (2026-04-07)
  - Type: fix (UX)
  - Files: `CameraController.cs` (HandleScrollZoom — `IsPointerOverBlockingUi` guard)
  - Spec: (removed — fix shipped as part of **TECH-69** UI-as-code capstone; normative **`ui-design-system.md`** **§3.5** scroll-zoom checklist)
  - Notes: **Closed (resolved by other issue):** The `IsPointerOverGameObject` guard in `CameraController.HandleScrollZoom` was implemented during **TECH-69**. Scroll over UI panels (Load Game, Building Selector) no longer triggers camera zoom.
  - Depends on: none
  - Related: **TECH-69**, **TECH-67**

- [x] **BUG-53** — **Unity Editor:** **Territory Developer → Reports** menu / **Export Sorting Debug** tooling gap (2026-04-06)
  - Type: bug (tooling / agent workflow)
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`; `tools/reports/` path resolution (`Application.dataPath` parent); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10 (**Editor agent diagnostics**); [`ARCHITECTURE.md`](ARCHITECTURE.md) **Editor agent diagnostics** bullet; [`docs/unity-ide-agent-bridge-analysis.md`](docs/unity-ide-agent-bridge-analysis.md) §2.4 / §7 / §10 (**Agent** bridge next steps)
  - Spec: [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) §10 (authoritative — no project spec)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **Territory Developer → Reports** shows **Export Agent Context** and **Export Sorting Debug (Markdown)** after compile; **Sorting** full breakdown in **Play Mode** with initialized **grid** matches §10; **Edit Mode** stub behavior unchanged. **Original ship:** [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-28**. **Bridge** doc updated so **Reports** is no longer listed as an open prerequisite; **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived) supersedes the staging concept.
  - Depends on: none
  - Related: **TECH-28**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **TECH-64**

- [x] **FEAT-50** — **UI** visual polish: aesthetic refinement (**HUD**, panels, **toolbar**, **MainMenu**) (2026-04-11)
  - Type: feature / UX polish
  - Files: `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/MainScene.unity`; `Assets/UI/Theme/DefaultUiTheme.asset`; `Assets/Scripts/Managers/GameManagers/UiTheme.cs`, `UIManager.cs` + **`UIManager.*.cs`** partials; `CameraController.cs`; `MainMenuController.cs`; **Controllers** under `Assets/Scripts/Controllers/UnitControllers/` as wired; `ia/specs/ui-design-system.md` (**§1**, **§3.5**, **§5.2**, **§5.3**); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/ui-data-dashboard-exploration.md`](docs/ui-data-dashboard-exploration.md) (dashboard charter — renamed from legacy filename)
  - Spec: (removed after closure — normative **`ui-design-system.md`** **as-built** / **Target** + **§5.3** polish patterns; **§3.5** **BUG-19** touch / **WASD** note; exploration doc **`docs/ui-data-dashboard-exploration.md`**; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`UiTheme`**-first **HUD** / **MainMenu** pass; **CanvasGroup** popup fades; **RCI** demand gauge bars; **welcome** briefing (**PlayerPrefs**); **CameraController** **UI** blocking (touch **fingerId** + **WASD**); construction cost / grid debug chrome; **`UiCanvasGroupUtility`**. **Deferred:** optional **`ui_theme_tokens` MCP** — open **BACKLOG** if product wants it. **Dashboard** mechanics: **FEAT-51** + **`docs/ui-data-dashboard-exploration.md`**.
  - Depends on: none (soft: **BUG-19**)
  - Related: **FEAT-51**, **BUG-19**, **BUG-14**, **TECH-67**, **TECH-69**

- [x] **TECH-71** — **IA project spec journal**: Postgres **Decision Log** / **Lessons learned** + MCP tools + **Skills** hooks (2026-04-11)
  - Type: tooling / agent workflow / Postgres dev surface
  - Files: `db/migrations/0007_ia_project_spec_journal.sql`; [`config/postgres-dev.json`](config/postgres-dev.json); [`config/README.md`](config/README.md); `tools/postgres-ia/resolve-database-url.mjs`; `tools/mcp-ia-server/src/ia-db/` (incl. `journal-repo.ts`, `pool.ts`, `resolve-database-url.ts`); `tools/mcp-ia-server/src/tools/project-spec-journal.ts`; `tools/mcp-ia-server/scripts/persist-project-spec-journal.ts`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/ia-db/`; `tools/mcp-ia-server/package.json`; [`ia/projects/PROJECT-SPEC-STRUCTURE.md`](ia/projects/PROJECT-SPEC-STRUCTURE.md); [`.env.example`](.env.example); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`tools/postgres-ia/README.md`](tools/postgres-ia/README.md); [`ia/specs/glossary.md`](ia/specs/glossary.md); [`ia/skills/project-spec-close/SKILL.md`](ia/skills/project-spec-close/SKILL.md); [`ia/skills/project-new/SKILL.md`](ia/skills/project-new/SKILL.md); [`ia/skills/project-spec-kickoff/SKILL.md`](ia/skills/project-spec-kickoff/SKILL.md); [`ia/rules/agent-router.md`](ia/rules/agent-router.md); [`ARCHITECTURE.md`](ARCHITECTURE.md); root [`package.json`](package.json)
  - Spec: (removed after closure — **glossary** **IA project spec journal**; [`config/README.md`](config/README.md); [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md); [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) **territory-ia** tool list + **Postgres** dev surfaces; **this row**)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`ia_project_spec_journal`** + MCP **`project_spec_journal_*`**; **Skills** **J1** + optional **project-new** / **project-spec-kickoff** journal search; committed dev URI **`config/postgres-dev.json`** + **`resolve-database-url`** (**postgres-ia** + **mcp-ia-server**); **`npm run db:persist-project-journal`** at closeout.
  - Depends on: none (soft: **TECH-24** for parser policy when extending closeout parser)
  - Related: **TECH-48**, **TECH-18**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived)

- [x] **TECH-67** — **UI-as-code program** (umbrella) (2026-04-10)
  - Type: tooling / documentation / agent enablement (program closeout)
  - Files: `ia/specs/ui-design-system.md` (**Overview**, **Codebase inventory (uGUI)**, **§5.2**, **§3**); `ia/specs/glossary.md` (**UI-as-code program**, **UI design system (reference spec)**); [`ARCHITECTURE.md`](ARCHITECTURE.md); [`docs/ui-as-built-ui-critique.md`](docs/ui-as-built-ui-critique.md); `docs/reports/ui-inventory-as-built-baseline.json`; `Assets/Scripts/Editor/UiInventoryReportsMenu.cs`; `ia/skills/ui-hud-row-theme/`; **BACKLOG.md** (**§ UI-as-code program** header)
  - Spec: (removed after closure — **`ui-design-system.md`** **Codebase inventory (uGUI)** + **§6** revision history; **glossary** rows above; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-69** capstone row; this row)
  - Notes: **Completed (`/project-spec-close`):** Umbrella charter, **§4.4** inventory, backlog bridge, phased plan, and **§8** acceptance migrated off `ia/projects/TECH-67.md`; **FEAT-50** visual polish completed **2026-04-11** (this file **Recent archive**). Optional **`ui_theme_tokens` MCP** still unscoped.
  - Depends on: none
  - Related: **TECH-69**, **TECH-68**, **TECH-70**, **TECH-07**, **FEAT-50**, **TECH-33**, **BUG-53**, **BUG-19**

- [x] **TECH-69** — **UI improvements using UI-as-code** (**TECH-67** program capstone) (2026-04-04)
  - Type: refactor / tooling / UX (umbrella closeout)
  - Files: `Assets/Scenes/MainMenu.unity`; `MainScene.unity`; `MainMenuController.cs`; `UIManager.cs` + **`UIManager.*.cs` partials**; `CameraController.cs` (**scroll** over **UI** zoom gate); `UiTheme.cs`; `Assets/UI/Theme/`; `Assets/UI/Prefabs/`; `UiThemeValidationMenu.cs`; `UiPrefabLibraryScaffoldMenu.cs`; `ia/specs/ui-design-system.md`; `ia/specs/unity-development-context.md` **§10**; `ia/specs/managers-reference.md`; `ia/skills/ui-hud-row-theme/`; `docs/ui-as-built-ui-critique.md` (planning trace)
  - Spec: (removed after closure — normative **`ui-design-system.md`** **§5.2**, **§3.2**, **§3.5**; **`unity-development-context.md`** **§10**; **`managers-reference`** **UIManager**; **glossary** **UI-as-code program**; **TECH-67** umbrella row (archived same batch); this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **`UiTheme`** + **MainMenu** serialization; **`partial` `UIManager`**; **Editor** **Validate UI Theme** + **Scaffold UI Prefab Library v0**; **`ui-hud-row-theme`** **Skill**; **typography** policy and **Canvas Scaler** matrix in **`ui-design-system.md`**; **modal** **Esc** contract + **§3.5** scroll vs zoom (**BUG-19** code path). **Deferred:** optional **territory-ia** **`ui_theme_tokens`** — file under open **BACKLOG** if product wants it.
  - Depends on: **TECH-67** (umbrella)
  - Related: **TECH-67**, **TECH-33**, **Close Dev Loop** (**TECH-75** — **TECH-75c** **§ Completed** (this file **Recent archive**); **TECH-75d** archived; **TECH-75b** archived), **BUG-19**, **BUG-14**, **BUG-53**, **FEAT-50**

- [x] **TECH-07** — **ControlPanel**: left vertical sidebar layout (category rows) (2026-04-04)
  - Type: refactor (UI/UX)
  - Files: `Assets/Scenes/MainScene.unity` (**`UI/City/Canvas`**, **`ControlPanel`** hierarchy); `UIManager.cs`; `Assets/Scripts/Controllers/UnitControllers/*SelectorButton.cs` (as wired); `ia/specs/ui-design-system.md` **§3.3**, **§1.3**, **§4.3**, **Codebase inventory (uGUI)**
  - Spec: (removed after closure — **`ui-design-system.md`** **§3.3** **toolbar**; **glossary** **UI design system (reference spec)**; [`BACKLOG-ARCHIVE.md`](BACKLOG-ARCHIVE.md) **TECH-08** historical doc bridge; this row)
  - Notes: **Completed (manual scene work + backlog purge):** **Left**-docked **vertical** **toolbar** implemented directly in **`MainScene.unity`**; open **BACKLOG** row retired. **Trace:** prior doc ticket **TECH-08** (archived) linked **§3.3** target copy to this work.
  - Depends on: none (soft: **TECH-67** program context)
  - Related: **TECH-67**

- [x] **TECH-68** — **As-built** **UI** documentation: align **`ui-design-system.md`** with **shipped** **Canvas** / **HUD** / **popups** (2026-04-04)
  - Type: documentation / agent enablement
  - Files: `ia/specs/ui-design-system.md`; `ia/specs/glossary.md` (**UI design system (reference spec)**, **UI-as-code program**); `ia/specs/unity-development-context.md` **§10** (UI inventory baseline row); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/reports/README.md`](docs/reports/README.md); [`ARCHITECTURE.md`](ARCHITECTURE.md) (**UI-as-code** trace); `Assets/Scripts/Editor/UiInventoryReportsMenu.cs`; `Assets/Scripts/Editor/EditorPostgresExportRegistrar.cs`; [`docs/postgres-ia-dev-setup.md`](docs/postgres-ia-dev-setup.md); **TECH-67** umbrella project spec (**Phase 1** — removed after **TECH-67** closure)
  - Spec: (removed after closure — **glossary** **UI design system (reference spec)**; **`ui-design-system.md`** **Machine-readable traceability**; **`unity-development-context.md`** **§10**; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** **As-built** reference spec + committed **UI** inventory baseline; **Editor** export + **Postgres** **`ui_inventory`** kind documented without backlog id branding. **Umbrella:** **TECH-67** **§8** first bullet checked; **TECH-69** **Depends on** no longer cites this row.
  - Depends on: none (soft: **TECH-67** program context)

- [x] **TECH-70** — **UI-as-code** umbrella maintenance & multi-scene **UI** traceability (2026-04-04)
  - Type: documentation / tooling / agent enablement
  - Files: **TECH-67** umbrella project spec (**§4.4**, **§4.6**, **§4.9**, **§7** Phase **0** — removed after **TECH-67** closure); [`ia/specs/ui-design-system.md`](ia/specs/ui-design-system.md); [`Assets/Scripts/Editor/UiInventoryReportsMenu.cs`](Assets/Scripts/Editor/UiInventoryReportsMenu.cs); [`docs/reports/ui-inventory-as-built-baseline.json`](docs/reports/ui-inventory-as-built-baseline.json); [`docs/reports/README.md`](docs/reports/README.md); [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) **§10**; [`db/migrations/0006_editor_export_ui_inventory.sql`](db/migrations/0006_editor_export_ui_inventory.sql) (**Postgres** **`editor_export_ui_inventory`**)
  - Spec: (removed after closure — **`ui-design-system.md`** **Codebase inventory (uGUI)** ongoing hygiene + **Machine-readable traceability**; [`docs/reports/README.md`](docs/reports/README.md) **Postgres vs baseline** note; this row)
  - Notes: **Completed (verified — `/project-spec-close` + user):** Umbrella **§4.9** resolutions + **Decision Log**; **baseline JSON** aligned to **Postgres** **`document`** (export timestamp); **`RegionScene`** / **`CityScene`** rename deferred (**BACKLOG** / **`ui-design-system.md`** hygiene when scenes land); **`validate:all`** green on implementation pass. Ongoing hygiene: **`ui-design-system.md`** + baseline JSON (**no** separate open umbrella row after **TECH-67** closure).
  - Depends on: none (soft: **TECH-67** program context)
  - Related: **TECH-67**, **TECH-33**, **BUG-53**

- [x] **TECH-28** — Unity Editor: **agent diagnostics** (context JSON + sorting debug export) (2026-04-02)
  - Type: tooling / agent workflow
  - Files: `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`, `tools/reports/` (generated output; see `.gitignore`), `.gitignore`
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** **Territory Developer → Reports → Export Agent Context** writes `tools/reports/agent-context-{timestamp}.json` (`schema_version`, `exported_at_utc`, scene, selection, bounded **Cell** / **HeightMap** / **WaterMap** sample via **`GridManager.GetCell`** only). **Export Sorting Debug (Markdown)** writes `sorting-debug-{timestamp}.md` in **Play Mode** using **`TerrainManager`** sorting APIs and capped **`SpriteRenderer`** `sortingOrder` listing. **Agents:** reference `@tools/reports/agent-context-….json` or `@tools/reports/sorting-debug-….md` in Cursor prompts (paths under repo root). `docs/agent-tooling-verification-priority-tasks.md` tasks 2, 23. **Canonical expected behavior** and troubleshooting: `ia/specs/unity-development-context.md` §10; if menus or **Sorting** export regress, file a new **open** row on [`BACKLOG.md`](BACKLOG.md) (attach **Console** output and sample exports per §10 **Verification**).
  - Depends on: none

- [x] **TECH-25** — Incremental authoring milestones for `unity-development-context.md` (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `ia/specs/unity-development-context.md`; `projects/agent-friendly-tasks-with-territory-ia-context.md` (pointer wording); `docs/agent-tooling-verification-priority-tasks.md`; `BACKLOG.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts` (backlog smoke test → **TECH-28**)
  - Spec: (project spec removed after closure)
  - Notes: **Completed (verified per user):** Merged milestone slices **M1**–**M7** into **`unity-development-context.md`** — lifecycle (**`ZoneManager`**, **`WaterManager`**, coroutine/`Invoke` examples), Inspector / **Addressables** guard, **`SerializeField`** scan note + **`DemandManager`**, prefab/**YAML**/**meta** cautions, **`GridManager`** + **`GridSortingOrderService`** sorting entry points (formula still geo §7), **`GeographyManager`** init + **BUG-16** pointer, **`GetComponent`** per-frame row, glossary (**Geography initialization**), §1 roadmap (**TECH-18**, **TECH-26**, **TECH-28**). **`npm run verify`** under **`tools/mcp-ia-server/`**.
  - Depends on: **TECH-20** (umbrella spec)

- [x] **TECH-20** — In-repo Unity development context for agents (spec + concept index) (2026-04-02)
  - Type: documentation / agent tooling
  - Files: `ia/specs/unity-development-context.md`; `AGENTS.md`; `ia/rules/agent-router.md`; `tools/mcp-ia-server/src/config.ts` (`unity` / `unityctx` → `unity-development-context`); `docs/mcp-ia-server.md`; `tools/mcp-ia-server/README.md`; `tools/mcp-ia-server/scripts/verify-mcp.ts`; `tools/mcp-ia-server/tests/parser/backlog-parser.test.ts`; `tools/mcp-ia-server/tests/tools/build-registry.test.ts`; `tools/mcp-ia-server/tests/tools/config-aliases.test.ts`; [`ia/specs/REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) (router authoring note)
  - Spec: [`ia/specs/unity-development-context.md`](ia/specs/unity-development-context.md) (authoritative); project spec removed after closure
  - Notes: **Completed (verified per user):** First-party **Unity** reference for **MonoBehaviour** / **Inspector** / **`FindObjectOfType`** / execution order; **territory-ia** `list_specs` key `unity-development-context`; **agent-router** row avoids **`router_for_task`** token collisions with geography queries (see **REFERENCE-SPEC-STRUCTURE**). Unblocks **TECH-18** `unity_context_section`; follow-up polish shipped in **TECH-25** (completed).
  - Depends on: none

- [x] **BUG-37** — Manual **street** drawing clears **buildings** and **zones** on cells adjacent to the **road stroke** (2026-04-02)
  - Type: bug
  - Files: `TerrainManager.cs` (`RestoreTerrainForCell` — **BUG-37**: skip `PlaceFlatTerrain` / slope rebuild when `GridManager.IsCellOccupiedByBuilding`; sync **HeightMap** / **cell** height + transform first); `RoadManager.cs`, `PathTerraformPlan.cs` (call path unchanged)
  - Spec: `ia/projects/BUG-37.md`; `ia/specs/isometric-geography-system.md` §14 (manual **streets**)
  - Notes: **Completed (verified per user):** Commit/AUTO `PathTerraformPlan.Apply` Phase 2/3 was refreshing **Moore** neighbors and stacking **grass** under **RCI** **buildings** / footprint **cells** (preview skipped **Apply**, so only commit showed the bug). **Fix:** preserve development by returning after height/sync when the **cell** is **building**-occupied. **Follow-up:** **BUG-52** if **AUTO** zoning shows persistent **grass** buffers beside new **streets** (investigate correlation).
  - Depends on: none

- [x] **TECH-22** — Canonical terminology pass on **reference specs** (`ia/specs`) (2026-04-02)
  - Type: documentation / refactor (IA)
  - Files: `ia/specs/glossary.md`, `isometric-geography-system.md`, `roads-system.md`, `water-terrain-system.md`, `simulation-system.md`, `persistence-system.md`, `managers-reference.md`, `ui-design-system.md`, `REFERENCE-SPEC-STRUCTURE.md`; `BACKLOG.md` (one **map border** wording fix); `tools/mcp-ia-server/tests/parser/fuzzy.test.ts` (§13 heading fixture); [`ia/projects/TECH-22.md`](ia/projects/TECH-22.md)
  - Spec: [`ia/specs/glossary.md`](ia/specs/glossary.md); [`ia/specs/REFERENCE-SPEC-STRUCTURE.md`](ia/specs/REFERENCE-SPEC-STRUCTURE.md) (deprecated → canonical table + MCP **`glossary_discover`** hint)
  - Notes: **Completed (verified per user):** Glossary/spec alignment — **map border** vs local **cell** edges; umbrella **street or interstate**; **road validation pipeline** wording; §13 retitled in geo; authoring table in `REFERENCE-SPEC-STRUCTURE.md`. `AGENTS.md` / MCP `config.ts` unchanged (no spec key changes).
  - Depends on: none

- [x] **FEAT-45** — MCP **`glossary_discover`**: keyword-style discovery over **glossary** rows (2026-04-02)
  - Type: feature (IA / tooling)
  - Files: `tools/mcp-ia-server/src/tools/glossary-discover.ts`, `tools/mcp-ia-server/src/tools/glossary-lookup.ts`, `tools/mcp-ia-server/src/parser/glossary-discover-rank.ts`, `tools/mcp-ia-server/src/index.ts`, `tools/mcp-ia-server/package.json`, `tools/mcp-ia-server/tests/parser/glossary-discover-rank.test.ts`, `tools/mcp-ia-server/tests/tools/glossary-discover.test.ts`, `tools/mcp-ia-server/scripts/verify-mcp.ts`, [`docs/mcp-ia-server.md`](docs/mcp-ia-server.md), [`docs/mcp-markdown-ia-pattern.md`](docs/mcp-markdown-ia-pattern.md), [`tools/mcp-ia-server/README.md`](tools/mcp-ia-server/README.md), [`AGENTS.md`](AGENTS.md), [`ia/rules/agent-router.md`](ia/rules/agent-router.md), [`ia/rules/mcp-ia-default.md`](ia/rules/mcp-ia-default.md)
  - Spec: [`ia/projects/FEAT-45.md`](ia/projects/FEAT-45.md)
  - Notes: **Completed (verified per user):** **`glossary_discover`** tool (territory-ia **v0.4.2**): Phase A deterministic ranking over **Term** / **Definition** / **Spec** / category; optional **`spec`** alias + **`registryKey`** from Spec cell; `hint_next_tools`; empty-query branch with fuzzy **term** suggestions. Agents must pass **English** in glossary tools; documented in MCP README, `docs/mcp-ia-server.md`, `AGENTS.md`, and Cursor rules. **`npm test`** / **`npm run verify`** under `tools/mcp-ia-server/`. **Phase B** (scoring linked spec body) deferred.
  - Depends on: **TECH-17** (MCP IA server — baseline)

- [x] **TECH-17** — MCP server for agentic Information Architecture (Markdown sources) (2026-04-02)
  - Type: infrastructure / tooling
  - Files: `tools/mcp-ia-server/`; `.mcp.json`; `ia/specs/*.md`, `ia/rules/*.md`, `AGENTS.md`, `ARCHITECTURE.md` as sources; `docs/mcp-ia-server.md`; docs updates in `AGENTS.md`, `ARCHITECTURE.md`, `ia/rules/project-overview.md`, `agent-router.md` (MCP subsection)
  - Notes: **Shipped:** Node + `@modelcontextprotocol/sdk` stdio server with tools including `list_specs`, `spec_outline`, `spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`, `backlog_issue` (BACKLOG.md by id); spec aliases; fuzzy glossary/section fallbacks; `spec_section` input aliases for LLM mis-keys; parse cache; stderr timing; `node:test` + c8 coverage on `src/parser/**`; `npm run verify`. **Reference:** `docs/mcp-ia-server.md`, `docs/mcp-markdown-ia-pattern.md` (generic pattern), `tools/mcp-ia-server/README.md`. **Retrospective / design history:** `ia/projects/TECH-17a.md`, `TECH-17b.md`, `TECH-17c.md` (§9–11 post-ship; delete when no longer needed).
  - Depends on: none

- [x] **BUG-51** — Diagonal / corner-up land slopes vs roads: design closure (2026-04-01)
  - Type: bug (closed by policy + implementation, not by fixing prefab-on-diagonal art)
  - Files: `RoadStrokeTerrainRules.cs`, `RoadManager.cs` (`TryBuildFilteredPathForRoadPlan`, `TryPrepareRoadPlacementPlanLongestValidPrefix`, `TryPrepareDeckSpanPlanFromAdjacentStroke`), `GridPathfinder.cs`, `InterstateManager.cs` (`IsCellAllowedForInterstate`), `RoadPrefabResolver.cs`, `TerraformingService.cs`, `Cell.cs` (route-first / BUG-51 technical work — see spec)
  - Spec: `ia/specs/roads-system.md` (land slope stroke policy, route-first paragraph), `ia/specs/isometric-geography-system.md` §3.3.3–§3.3.4, §13.10
  - Notes: **Closed (verified):** The original report asked for **correct road prefabs on diagonal and corner-up terrain**. The chosen resolution was **not** to fully support roads on those land slope types. Instead, **road strokes are invalid on land that is not flat and not a cardinal ramp** (`TerrainSlopeType`: `Flat`, `North`, `South`, `East`, `West` only). Pure diagonals (`NorthEast`, …) and corner-up types (`*Up`) are excluded. **Behavior:** silent **prefix truncation** — preview and commit only include cells up to the last allowed cell; cursor may keep moving diagonally without extending preview. **Scope:** manual, AUTO, and interstate. **First cell blocked:** no placement, no notification. **`Road cannot extend further…`** is **not** posted when the only issue is no slope-valid prefix (e.g. stroke starts on diagonal). **Exceptions in stroke truncation / walkability:** path cells at `HeightMap` height ≤ 0 (wet span) and `IsWaterSlopeCell` shore tiles still pass the truncator so FEAT-44 bridges are not cut. **Still in codebase:** BUG-51 **route-first** resolver topology (`pathOnlyNeighbors`), `Cell` path hints, terraform preservation on diagonal wedge when `preferSlopeClimb && dSeg == 0`, `GetWorldPositionForPrefab` anchoring — documented under roads spec **BUG-51 (route-first)**.
  - Depends on: none

- [x] **BUG-47** — AUTO simulation: perpendicular street stubs, reservations, junction prefab refresh (2026-04-01)
  - Type: bug / feature
  - Files: `AutoRoadBuilder.cs` (`FindPath*ForAutoSimulation`, `HasParallelRoadTooClose` + `excludeAlongDir`, batch prefab refresh), `AutoSimulationRoadRules.cs`, `AutoZoningManager.cs`, `RoadCacheService.cs`, `GridPathfinder.cs`, `GridManager.cs`, `IGridManager.cs`, `RoadManager.cs` (`RefreshRoadPrefabsAfterBatchPlacement`, bridge-deck skip); `ia/specs/isometric-geography-system.md` §13.9, `ia/rules/roads.md`, `ia/rules/simulation.md`
  - Spec: `ia/specs/isometric-geography-system.md` §13.9
  - Notes: **Completed (verified in-game):** AUTO can trace perpendicular stubs/connectors and crossings: land = grass/forest/undeveloped light zoning; dedicated AUTO pathfinder; road frontier and extension cells include that class; perpendicular branches pass parent-axis `excludeAlongDir` in `HasParallelRoadTooClose`; auto-zoning skips axial corridor and extension cells. **Visual:** `PlaceRoadTileFromResolved` did not refresh neighbors; added deduplicated per-tick refresh (`RefreshRoadPrefabsAfterBatchPlacement`), skipping bridge deck re-resolve. **Lessons:** any batch `FromResolved` flow must document explicit junction refresh; keep generic `FindPath` separate from AUTO pathfinding.
  - Depends on: none

- [x] **FEAT-44** — High-deck water bridges: cliff banks, uniform deck height, manual + AUTO placement (2026-03-30)
  - Type: feature
  - Files: `RoadManager.cs` (`TryPrepareDeckSpanPlanFromAdjacentStroke`, `TryPrepareLockedDeckSpanBridgePlacement`, `TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord`, `TryExtendCardinalStreetPathWithBridgeChord`, `StrokeHasWaterOrWaterSlopeCells`, `StrokeLastCellIsFirmDryLand`, FEAT-44 validation / chord walk), `TerraformingService.cs` (`TryBuildDeckSpanOnlyWaterBridgePlan`, `TryAssignWaterBridgeDeckDisplayHeight`), `AutoRoadBuilder.cs` (`TryGetStreetPlacementPlan`, `BuildFullSegmentInOneTick` — atomic water-bridge completion), `PathTerraformPlan.cs` (`HasTerraformHeightMutation`, deck display height docs), `RoadPrefabResolver.cs` (bridge deck resolution); rules/spec: `ia/rules/roads.md`, `ia/specs/isometric-geography-system.md` §13
  - Spec: `ia/specs/isometric-geography-system.md` §13 (bridges, shared validation, AUTO behavior)
  - Notes: **Completed (verified per user):** **Manual:** locked lip→chord preview uses a **deck-span-only** plan (`TerraformAction.None`, `TryBuildDeckSpanOnlyWaterBridgePlan`) so valid crossings are not blocked by cut-through / Phase-1 on complex tails; commit matches preview via shared `TryPrepareDeckSpanPlanFromAdjacentStroke`. **AUTO:** extends cardinal strokes with the same `WalkStraightChordFromLipThroughWetToFarDry` when the next step is wet/shore; runs longest-prefix plus programmatic deck-span and **prefers** deck-span when the stroke is wet or yields a longer expanded path. **AUTO water crossings** are **all-or-nothing in one tick**: require a **firm dry exit**, enough remaining tile budget for every new tile, a **single lump** `TrySpend` for the bridge, otherwise **`Revert`** — no half bridges. **Uniform deck:** one `waterBridgeDeckDisplayHeight` for all bridge deck prefabs on the span; assignment **prefers the exit (mesa) dry cell** after the wet run, then entry, then legacy lip fallback. **Description (issue):** Elevated road / bridge crossings across cliff-separated banks and variable terrain with correct clearance, FEAT-44 path rules, and consistent sorting/pathfinding per geography spec.

- [x] **BUG-50** — River–river junction: shore Moore topology, junction post-pass diagonal SlopeWater, upper-brink cliff water stacks + isometric anchor at shore grid (2026-03-28)
  - Type: bug / polish
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `IsOpenWaterForShoreTopology`, `NeighborMatchesShoreOwnerForJunctionTopology`, `ApplyJunctionCascadeShorePostPass`, `ApplyUpperBrinkShoreWaterCascadeCliffStacks`, `TryPlaceWaterCascadeCliffStack` / `waterSurfaceAnchorGrid`, `PlaceCliffWallStackCore` sorting reference), `WaterManager.Membership.cs`, `WaterMap.cs` (`TryFindRiverRiverSurfaceStepBetweenBodiesNear`)
  - Spec: `ia/specs/isometric-geography-system.md` **§12.8.1**
  - Notes: **Completed (verified):** Default shore masks use **`IsOpenWaterForShoreTopology`** (junction-brink dry land not counted). **`RefreshShoreTerrainAfterWaterUpdate`** runs **`ApplyJunctionCascadeShorePostPass`** (extended topology + **`forceJunctionDiagonalSlopeForCascade`**) then **`ApplyUpperBrinkShoreWaterCascadeCliffStacks`** ( **`CliffSouthWater`** / **`CliffEastWater`** on **`UpperBrink`** only). Cascade **Y** anchor and sorting use **`waterSurfaceAnchorGrid`** at the **shore** cell so wide-river banks align with the isometric water plane. **`ARCHITECTURE.md`** Water bullet and **§12.8.1** document pipeline and authority.

- [x] **BUG-45** — Adjacent water bodies at different surface heights: merge, prefab refresh at intersections, straight slope/cliff transitions (2026-03-27)
  - Type: bug / polish
  - Files: `WaterManager.cs` (`UpdateWaterVisuals` — Pass A/B, `ApplyLakeHighToRiverLowContactFallback`), `WaterMap.cs` (`ApplyMultiBodySurfaceBoundaryNormalization`, `ApplyWaterSurfaceJunctionMerge`, `IsLakeSurfaceStepContactForbidden`, lake–river fallback), `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `SelectPerpendicularWaterCornerPrefabs`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`), `ProceduralRiverGenerator.cs` / `TestRiverGenerator.cs` as applicable; `docs/water-junction-merge-implementation-plan.md`
  - Spec: `ia/specs/isometric-geography-system.md` — **§5.6.2**, **§12.7**
  - Notes: **Completed (verified):** Pass A/B multi-body surface handling; lake-at-step exclusions; full-cardinal **`RefreshWaterCascadeCliffs`** (incl. mirror N/W lower pool); perpendicular multi-surface shore corner preference; lake-high vs river-low rim fallback. **Assign** `cliffWaterSouthPrefab` / **`cliffWaterEastPrefab`** on `TerrainManager` for visible cascades (west→east steps use **East**). **Map border** water × brown **cliff** seal: **geo** §5.7 / **Recent archive** **BUG-44**; bridges × cliff-water **BUG-43**; optional N/W cascade art (camera).

- [x] **BUG-42** — Water shores & cliffs: terrain + water (lakes + rivers); water–water cascades; shore coherence — merged **BUG-33** + **BUG-41** (2026-03-26)
  - Type: bug / feature
  - Files: `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `PlaceCliffWalls`, `PlaceCliffWallStackCore`, `RefreshWaterCascadeCliffs`, `RefreshShoreTerrainAfterWaterUpdate`, `ClampShoreLandHeightsToAdjacentWaterSurface`, `IsLandEligibleForWaterShorePrefabs`), `WaterManager.cs` (`PlaceWater`, `UpdateWaterVisuals`), `ProceduralRiverGenerator.cs` (inner-corner shore continuity §13.5), `ProceduralRiverGenerator` / `WaterMap` as applicable; `cliffWaterSouthPrefab` & `cliffWaterEastPrefab` under `Assets/Prefabs/`
  - Spec: `ia/specs/isometric-geography-system.md` (§2.4.1 shore band height coherence, §4.2 gate, §5.6–§5.7, §5.6.2 water–water cascades, §12–§13, §15)
  - Notes: **Completed (verified):** **Shore band height coherence** — `HeightMap` clamp on Moore shore ring vs adjacent logical surface; water-shore prefab gate uses **`V = max(MIN_HEIGHT, S−1)`** vs **land height**. **River** inner-corner promotion + bed assignment guard. **Water–water cascades** — `RefreshWaterCascadeCliffs` after full `UpdateWaterVisuals`; **`PlaceCliffWallStackCore`** shared with brown cliffs; cascade Y anchor matches **water tile** (`GetWorldPositionVector` at `visualSurfaceHeight` + `tileHeight×0.25`). **Out of scope / follow-up:** visible **north/west** cliff meshes (camera); **map border** brown **cliff** seal vs water — **geo** §5.7 / **Recent archive** **BUG-44**; bridges × cliff-water (**BUG-43**); optional **N/S/E/W** “waterfall” art beyond **S/E** stacks — track separately if needed. **Multi-body junctions:** completed **[BUG-45](#bug-45)** (2026-03-27).

- [x] **BUG-33** — Lake shore / edge prefab bugs — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **BUG-41** — River corridors: shore prefabs + cliff stacks — **superseded:** merged into **[BUG-42](#bug-42)** (2026-03-25); closed with **BUG-42** (2026-03-26)
- [x] **FEAT-38** — Procedural rivers during geography / terrain generation (2026-03-24)
  - Type: feature
  - Files: `GeographyManager.cs`, `ProceduralRiverGenerator.cs`, `TerrainManager.cs`, `WaterMap.cs`, `WaterManager.cs`, `WaterBody.cs`, `Cell.cs` / `CellData.cs` (as needed)
  - Spec: `ia/specs/isometric-geography-system.md` §12–§13
  - Notes: **Completed:** `WaterBody` classification + merge (river vs lake/sea); `GenerateProceduralRiversForNewGame()` after `InitializeWaterMap`, before interstate; `ProceduralRiverGenerator` (BFS / forced centerline, border margin, transverse + longitudinal monotonicity, `WaterMap` river bodies). **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (merged **BUG-33** + **BUG-41**, 2026-03-26).

- [x] **BUG-39** — Bay / inner-corner shore prefabs: cliff art alignment vs stacked cliffs (2026-03-24)
  - Type: fix (art vs code)
  - Files: `TerrainManager.cs` (`GetCliffWallSegmentWorldPositionOnSharedEdge`, `PlaceCliffWallStack`), `Assets/Sprites/Cliff/CliffEast.png`, `Assets/Sprites/Cliff/CliffSouth.png`, cliff prefabs under `Assets/Prefabs/Cliff/`
  - Notes: **Resolved:** Inspector-tunable per-face placement (`cliffWallSouthFaceNudgeTileWidthFraction` / `HeightFraction`, `cliffWallEastFaceNudgeTileWidthFraction` / `HeightFraction`) and water-shore Y offset (`cliffWallWaterShoreYOffsetTileHeightFraction`) so cliff sprites align with the south/east diamond faces and water-shore cells after art was moved inside the textures. Further shore/gap / cascade work → completed **[BUG-42](#bug-42)** (2026-03-26) where applicable.

- [x] **BUG-40** — Shore cliff walls draw in front of nearer (foreground) water tiles (2026-03-24)
  - Type: fix (sorting / layers)
  - Files: `TerrainManager.cs` (`PlaceCliffWallStack`, `GetMaxCliffSortingOrderFromForegroundWaterNeighbors`)
  - Notes: **Resolved:** Cliff `sortingOrder` is capped against registered **foreground** water neighbors (`nx+ny < highX+highY`) using their `Cell.sortingOrder`, so brown cliff segments do not draw above nearer water tiles. See `ia/specs/isometric-geography-system.md` §15.2.

- [x] **BUG-36** — Lake generation: seeded RNG (reproducible + varied per New Game) (2026-03-24)
  - Type: fix
  - Files: `WaterMap.cs` (`InitializeLakesFromDepressionFill`, `LakeFillSettings`), `WaterManager.cs`, `MapGenerationSeed.cs` (`GetLakeFillRandomSeed`), `TerrainManager.cs` (`EnsureGuaranteedLakeDepressions` shuffle)
  - Notes: `LakeFillSettings.RandomSeed` comes from map generation seed; depression-fill uses a seeded `System.Random`; bowl shuffle uses a derived seed. Same template no longer forces identical lake bodies across unrelated runs; fixed seed still reproduces. Spec: `ia/specs/isometric-geography-system.md` §12.3. **Related:** **BUG-08**, **FEAT-38**.

- [x] **BUG-35** — Load Game: multi-cell buildings — grass on footprint (non-pivot) could draw above building; 1×1 grass + building under one cell (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs` (`DestroyCellChildren`), `ZoneManager.cs` (`PlaceZoneBuilding`, `PlaceZoneBuildingTile`), `BuildingPlacementService.cs` (`UpdateBuildingTilesAttributes`), `GridSortingOrderService.cs` (`SetZoneBuildingSortingOrder`, `SyncCellTerrainLayersBelowBuilding`)
  - Notes: `DestroyCellChildren(..., destroyFlatGrass: true)` when placing/restoring **RCI and utility** buildings so flat grass prefabs are not kept alongside the building (runtime + load). Multi-cell `SetZoneBuildingSortingOrder` still calls **grass-only** `SyncCellTerrainLayersBelowBuilding` for each footprint cell. **BUG-20** may be re-verified against this. Spec: [`ia/specs/isometric-geography-system.md`](ia/specs/isometric-geography-system.md) §7.4.

- [x] **BUG-34** — Load Game: zone buildings / utilities render under terrain or water edges (`sortingOrder` snapshot vs building layer) (2026-03-22)
  - Type: fix
  - Files: `GridManager.cs`, `ZoneManager.cs`, `TerrainManager.cs`, `BuildingPlacementService.cs`, `GridSortingOrderService.cs`, `Cell.cs`, `CellData.cs`, `GameSaveManager.cs`
  - Notes: Deterministic restore order; open water and shores aligned with runtime sorting; multi-cell RCI passes `buildingSize`; post-load building sort pass; optional grass sync via `SyncCellTerrainLayersBelowBuilding`. **BUG-35** (completed 2026-03-22) adds `destroyFlatGrass` on building placement/restore. Spec summary: `ia/specs/isometric-geography-system.md` §7.4.

- [x] **FEAT-37c** — Persist `WaterMapData` in saves + snapshot load (no terrain/water regen on load) (2026-03-22)
  - Type: feature
  - Files: `GameSaveManager.cs`, `WaterManager.cs`, `TerrainManager.cs`, `GridManager.cs`, `Cell.cs`, `CellData.cs`, `WaterBodyType.cs`
  - Notes: `GameSaveData.waterMapData`; `WaterManager.RestoreWaterMapFromSaveData`; `RestoreGridCellVisuals` applies saved `sortingOrder` and prefabs; legacy saves without `waterMapData` supported. **Follow-up:** building vs terrain sorting on load — **BUG-34** (completed); multi-cell footprint / grass under building — **BUG-35** (completed 2026-03-22).

- [x] **FEAT-37b** — Variable-height water: sorting, roads/bridges, `SEA_LEVEL` removal (no lake shore prefab scope) (2026-03-24)
  - Type: feature + refactor
  - Files: `GridSortingOrderService.cs`, `RoadPrefabResolver.cs`, `RoadManager.cs`, `AutoRoadBuilder.cs`, `ForestManager.cs`, `TerrainManager.cs` (water height queries, bridge/adjacency paths — **exclude** shore placement methods)
  - Notes: Legacy `SEA_LEVEL` / `cell.height == 0` assumptions removed or generalized for sorting, roads, bridges, non-shore water adjacency. Shore tiles **not** in scope (37a + completed **[BUG-42](#bug-42)**). Verified in Unity.

- [x] **BUG-32** — Lakes / `WaterMap` water not shown on minimap (desync with main map) (2026-03-23)
  - Type: fix (UX / consistency)
  - Files: `MiniMapController.cs`, `GeographyManager.cs`, `WaterManager.cs`, `WaterMap.cs`
  - Notes: Minimap water layer aligned with `WaterManager` / `WaterMap` (rebuild timing, `GetCellColor`, layer toggles). Verified in Unity.

- [x] **FEAT-37a** — WaterBody + WaterMap depression-fill (lake data & procedural placement) (2026-03-22)
  - Type: feature + refactor
  - Files: `WaterBody.cs`, `WaterMap.cs`, `WaterManager.cs`, `TerrainManager.cs`, `LakeFeasibility.cs`
  - Notes: `WaterBody` + per-cell body ids; `WaterMap.InitializeLakesFromDepressionFill` + `LakeFillSettings` (depression-fill, bounded pass, artificial fallback, merge); `LakeFeasibility` / `EnsureGuaranteedLakeDepressions` terrain bowls; `WaterMapData` v2 + legacy load; centered 40×40 template + extended terrain. **Shore / cliff / cascade polish:** completed **[BUG-42](#bug-42)** (2026-03-26); **FEAT-37b** / **FEAT-37c** completed; building sort on load **BUG-34** (completed); multi-cell footprint / grass under building **BUG-35** (completed 2026-03-22).

---

## Pre-2026-03-22 archive

- [x] **TECH-12** — Water system refactor: planning pass (objectives, rules, scope, child issues) (2026-03-21)
  - Type: planning / documentation
  - Files: `ia/specs/isometric-geography-system.md` (§12), `BACKLOG.md` (FEAT-37, BUG-08 splits), `ARCHITECTURE.md` (Terrain / Water as needed)
  - Notes: **Goal:** Before implementation of **FEAT-37**, produce a single agreed definition of **objectives**, **rules** (data + gameplay + rendering), **known bugs** to fold in, **non-goals / phases**, and **concrete child issues** (IDs) ordered for development. Link outcomes in this spec and in `FEAT-37`. Overlaps **BUG-08** (generation), **FEAT-15** (ports/sea). **Does not** implement code — only backlog + spec updates and issue breakdown.
  - Depends on: nothing (blocks structured FEAT-37 execution)

- [x] **BUG-30** — Incorrect road prefabs when interstate climbs slopes (2026-03-20)
  - Type: fix
  - Files: `TerraformingService.cs`, `RoadPrefabResolver.cs`, `PathTerraformPlan.cs`, `RoadManager.cs` (shared pipeline)
  - Notes: Segment-based Δh for scale-with-slopes; corner/upslope cells use `GetPostTerraformSlopeTypeAlongExit` (aligned with travel); live-terrain fallback + `RestoreTerrainForCell` force orthogonal ramp when `action == None` and cardinal `postTerraformSlopeType`. Spec: `ia/specs/isometric-geography-system.md` §14.7. Verified in Unity.

- [x] **TECH-09** — Remove obsolete `TerraformNeeded` from TerraformingService (2026-03-20)
  - Type: refactor (dead code removal)
  - Files: `TerraformingService.cs`
  - Notes: Removed `[Obsolete]` `TerraformNeeded` and `GetOrthogonalFromRoadDirection` (only used by it). Path-based terraforming uses `ComputePathPlan` only.

- [x] **TECH-10** — Fix `TerrainManager.DetermineWaterSlopePrefab` north/south sea logic (2026-03-20)
  - Type: fix (code health)
  - Files: `TerrainManager.cs`
  - Notes: Replaced impossible `if (!hasSeaLevelAtNorth)` under `hasSeaLevelAtNorth` with NE/NW corner handling and East-style branch for sea north+south strips (`southEast` / `southEastUpslope`). South-only coast mirrors East; removed unreachable `hasSeaLevelAtSouth` else (handled by North block first).

- [x] **TECH-11** — Namespace `Territory.Terrain` for TerraformingService and PathTerraformPlan (2026-03-20)
  - Type: refactor
  - Files: `TerraformingService.cs`, `PathTerraformPlan.cs`, `ARCHITECTURE.md`, `ia/rules/project-overview.md`
  - Notes: Wrapped both types in `namespace Territory.Terrain`. Dependents already had `using Territory.Terrain`. Docs updated to drop "global namespace" examples for these files.

- [x] **TECH-08** — UI design system docs: TECH-07 (ControlPanel sidebar) ticketed and wired (2026-03-20)
  - Type: documentation
  - Files: `BACKLOG.md` (TECH-07), `docs/ui-design-system-project.md` (Backlog bridge), `docs/ui-design-system-context.md` (Toolbar — ControlPanel), `ia/specs/ui-design-system.md` (§3.3 layout variants), `ARCHITECTURE.md`, `AGENTS.md`, `ia/rules/managers-guide.md`
  - Notes: This issue records the documentation and cross-links only. **TECH-07** (executable **ControlPanel** layout) was later completed manually in **`MainScene.unity`** and archived (**Recent archive**, **2026-04-04**).

- [x] **BUG-25** — Fix bugs in manual street segment drawing (2026-03-19)
  - Type: fix
  - Files: `RoadManager.cs`, `RoadPrefabResolver.cs` (also: `GridManager.cs`, `TerraformingService.cs`, `PathTerraformPlan.cs`, `GridPathfinder.cs` for prior spec work)
  - Notes: Junction/T/cross prefabs: `HashSet` path membership + `SelectFromConnectivity` for 3+ cardinal neighbors in `RoadPrefabResolver`; post-placement `RefreshRoadPrefabAt` pass on placed cells in `TryFinalizeManualRoadPlacement`. Spec: `ia/specs/isometric-geography-system.md` §14. Optional follow-up: `postTerraformSlopeType` on refresh, crossroads prefab audit.
- [x] **BUG-27** — Interstate pathfinding bugs (2026-03-19)
  - Border endpoint scoring (`ComputeInterstateBorderEndpointScore`), sorted candidates, `PickLowerCostInterstateAStarPath` (avoid-high vs not, pick cheaper), `InterstateAwayFromGoalPenalty` and cost tuning in `RoadPathCostConstants`. Spec: `ia/specs/isometric-geography-system.md` §14.5.
- [x] **BUG-29** — Cut-through: high hills cut through disappear leaving crater (2026-03-19)
  - Reject cut-through when `maxHeight - baseHeight > 1`; cliff/corridor context in `TerrainManager` / `PathTerraformPlan`; map-edge margin `cutThroughMinCellsFromMapEdge`; Phase 1 validation ring in `PathTerraformPlan`; interstate uses `forbidCutThrough`. Spec: `ia/specs/isometric-geography-system.md` §14.6.

- [x] **FEAT-24** — Auto-zoning for Medium and Heavy density (2026-03-19)
- [x] **BUG-23** — Interstate route generation is flaky; never created in New Game flow (2026-03-19)
- [x] **BUG-26** — Interstate prefab selection and pathfinding improvements (2026-03-19)
  - Elbow audit, validation, straightness bonus, slope cost, parallel sampling, bridge approach (Rule F), cut-through expansion. Follow-up: BUG-27 / BUG-29 / **BUG-30** completed 2026-03-19–2026-03-20; remaining: BUG-28 (sorting), BUG-31 (prefabs at entry/exit).
- [x] **TECH-06** — Documentation sync: specs aligned with backlog and rules; BUG-26, FEAT-36 added; ARCHITECTURE, file counts, helper services updated; zoning plan translated to English (2026-03-19)
- [x] **FEAT-05** — Streets must be able to climb diagonal slopes using orthogonal prefabs (2026-03-18)
- [x] **FEAT-34** — Zoning and building on slopes (2026-03-16)
- [x] **FEAT-33** — Urban remodeling: expropriations and redevelopment (2026-03-12)
- [x] **FEAT-31** — Auto roads grow toward high desirability areas (2026-03-12)
- [x] **FEAT-30** — Mini map layer toggles + desirability visualization (2026-03-12)
- [x] **BUG-24** — Growth budget not recalculated when income changes (2026-03-12)
- [x] **BUG-06** — Streets should not cost so much energy (2026-03-12)
- [x] **FEAT-32** — More streets and intersections in central and mid-urban areas (AUTO mode) (2026-03-12)
- [x] **BUG-22** — Auto zoning must not block street segment ends (AUTO mode) (2026-03-11)
- [x] **FEAT-25** — Growth budget tied to real income (2026-03-11)
- [x] **BUG-10** — `IndustrialHeavyZoning` never generates buildings (2026-03-11)
- [x] **FEAT-26** — Use desirability for building spawn selection (2026-03-10)
- [x] **BUG-07** — Better zone distribution: less random, more homogeneous by neighbourhoods/sectors (2026-03-10)
- [x] **FEAT-29** — Density gradient around urban centroids (AUTO mode) (2026-03-10)
- [x] **FEAT-17** — Mini-map (2026-03-09)
- [x] **FEAT-01** — Add delta change to total budget (e.g. $25,000 (+$1,200)) (2026-03-09)
- [x] **BUG-03** — Growth % sets amount instead of percentage of total budget (2026-03-09)
- [x] **BUG-02** — Taxes do not work (2026-03-09)
- [x] **BUG-05** — Do not remove cursor preview from buildings when constructing (2026-03-09)
- [x] **BUG-21** — Zoning cost is not charged when placing zones (2026-03-09)
- [x] **FEAT-02** — Add construction cost counter to mouse cursor (2026-03-09)
- [x] **FEAT-28** — Right-click drag-to-pan (grab and drag map) with inertia/fling (2026-03-09)
- [x] **BUG-04** — Pause mode stops camera movement; camera speed tied to simulation speed (2026-03-09)
- [x] **BUG-18** — Road preview and placement draw discontinuous lines instead of continuous paths (2026-03-09)
- [x] **FEAT-27** — Main menu with Continue, New Game, Load City, Options (2026-03-08)
- [x] **BUG-11** — Demand uses `Time.deltaTime` causing framerate dependency (2026-03-11)
- [x] **BUG-21** — Demand fix: unemployment-based RCI, remove environmental from demand, desirability for density (2026-03-11)
- [x] **BUG-01** — Save game, Load game and New game were broken (2026-03-07)
- [x] **BUG-09** — `Cell.GetCellData()` does not serialize cell state (2026-03-07)
- [x] **DONE** — Forest cannot be placed adjacent to water (2026-03)
- [x] **DONE** — Demolish forests at all heights + all building types (2026-03)
- [x] **DONE** — When demolishing forest on slope, correct terrain prefab restored via heightMap read (2026-03)
- [x] **DONE** — Interstate Road (2026-03)
- [x] **DONE** — CityNetwork sim (2026-03)
- [x] **DONE** — Forests on slopes (2026-03)
- [x] **DONE** — Growth simulation — AUTO mode (2026-03)
- [x] **DONE** — Simulation optimization (2026-03)
- [x] **DONE** — Codebase improvement for efficient AI agent contextualization (2026-03)