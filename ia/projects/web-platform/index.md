# Web Platform — Master Plan (MVP)

> **Last updated:** 2026-04-22 (Stage 24 Done — TECH-630…TECH-633 closeout)
>
> **Status:** In Progress — MVP Done 2026-04-17 (Steps 1–4 + 6 Final; Step 5 Done but architecture outputs retired 2026-04-22 per `docs/architecture-audit-change-list-2026-04-22.md`; Postgres driver swapped `@neondatabase/serverless` → `postgres`-js). Post-MVP extensions tracked in `docs/web-platform-post-mvp-extensions.md` — ready for `/design-explore` + `/master-plan-extend` Step 7+.
>
> **Scope:** Unified Next.js 14+ app at `web/` (monorepo workspace) serving three audiences from one codebase — public game site (landing / wiki / devlog / about / install / history), live DevOps progress dashboard, and future user portal. Static-first hybrid on Vercel free tier; Postgres + auth deferred to portal step. Post-MVP extensions companion doc: `docs/web-platform-post-mvp-extensions.md` (seeded §1 rollout completion view + §§2–7 deferred stubs).
>
> **Exploration source:**
> - `docs/web-platform-exploration.md` (§Design Expansion → Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples) — MVP Steps 1..6.
> - `docs/web-platform-post-mvp-extensions.md` (§Design Expansion — §1 Release-scoped progress view + §8 Visual Design Layer + §CD Pilot Bundle 2026-04-18 + §Design Expansion — Master Plan Alignment CD Pilot Bundle) — extension source for Steps 7..8 (Stages 8.5..8.8 appended 2026-04-18 from CD pilot gap-analysis).
>
> **Locked decisions (do not reopen in this plan):**
> - Stack: Next.js 14+ App Router, TypeScript, React Server Components, Tailwind CSS. MCP server (`territory-ia`) stays stdio dev-only; NOT consumed by web app.
> - Repo layout: monorepo; Next.js app at `web/`; root `package.json` declares npm workspaces.
> - Hosting: Vercel free tier. Build root `web/`. Vercel preview deploys optional; MVP critical path is localhost build (2026-04-22 audit — localhost-only MVP lock).
> - Auth (W7): deferred entirely per 2026-04-22 audit — no `/api/auth/*` and no auth UI surface in MVP. If/when portal re-enters scope, roll-own JWT + sessions remains the locked preference (not re-decide); no third-party auth provider.
> - Free-tier constraint: every service (Vercel, Postgres when selected, etc.) must be zero recurring cost until revenue exists.
> - Design language: FUTBIN-style data density + NYT-style dark choropleth palette. Tokens exported as JSON so future Unity UI/UX plan reuses the same palette.
> - Dashboard access: obscure-URL gate at MVP (Q14), auth gate once portal lands. `robots.txt` disallow + unlinked route.
> - Public copy style: full English marketing prose (caveman exception — per `agent-output-caveman.md` §exceptions). Agent-authored IA prose stays caveman.
> - **D5 (2026-04-18):** Console-rack aesthetic adoption = SITE-WIDE. Console chrome library (Rack / Bezel / Screen / LED / TapeReel / VuStrip / TransportStrip) mandatory production primitive set — not landing-only, not optional.
> - **D4 (2026-04-18):** Screen-port scope = FULL FLOW. All 4 production routes (`/`, `/dashboard`, `/dashboard/releases`, `/dashboard/releases/:releaseId/progress`) + 1 dev-only (`/design-system` — `web/app/(dev)/design-system/page.tsx`) ported from CD bundle; half-themed app rejected.
> - **CD bundle immutability (2026-04-18):** `web/design-refs/step-8-console/` treated as read-only ingestion source. Extraction + transcription emit new files under `web/app/globals.css` / `web/lib/design-tokens.ts` / `web/components/console/` — never edit the bundle in place.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
> - `ia/projects/blip-master-plan.md` — runtime C# audio; disjoint surface. No collision.
> - `ia/projects/multi-scale-master-plan.md` — runtime C# + save schema; disjoint surface. No collision.
> - `ia/projects/sprite-gen-master-plan.md` — Python tool; disjoint surface. No collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
> - `docs/web-platform-exploration.md` — full design + architecture mermaid + 3 examples. `### Design Expansion` block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase, ≤6 tasks per phase).
> - `ia/rules/agent-output-caveman.md` §exceptions — public-facing marketing / wiki / devlog copy is end-user surface; caveman rule does NOT apply to `web/content/**`.
> - `tools/progress-tracker/parse.mjs` + `render.mjs` — plan data source for dashboard; `parse.mjs` is authoritative and stays unchanged at MVP.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.
>
> **Invariants:** `ia/rules/invariants.md` #1–#12 NOT implicated — web platform is tooling / docs-only surface with zero runtime C# / Unity coupling. Any future Unity WebGL export OR in-game UI coordination will re-trigger invariants review; out of scope here.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Scaffold + design system foundation / Scaffold + deploy + CI](stage-1-scaffold-deploy-ci.md) — _Final — TECH-136 done (archived); Vercel project linked (`territory-developer-web`), production deploy confirmed 2026-04-15 (`https://web-nine-wheat-35.vercel.app`); validate:all green. Stage closed 2026-04-15._
- [Stage 2 — Scaffold + design system foundation / Design system foundation](stage-2-design-system-foundation.md) — _Done — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips + HeatmapCell + AnnotatedMap + `/design` review route + README §Tokens all archived (see BACKLOG-ARCHIVE.md). Stage closed 2026-04-14._
- [Stage 3 — Public surface + wiki + devlog / MDX pipeline + public pages + SEO](stage-3-mdx-pipeline-public-pages-seo.md) — _Done 2026-04-15 — all tasks archived (TECH-163 … TECH-168)._
- [Stage 4 — Public surface + wiki + devlog / Wiki + glossary auto-index + search](stage-4-wiki-glossary-auto-index-search.md) — _Done (closed 2026-04-15 — TECH-184…TECH-187 all archived)_
- [Stage 5 — Public surface + wiki + devlog / Devlog + RSS + origin story](stage-5-devlog-rss-origin-story.md) — _Done (closed 2026-04-15 — TECH-192…TECH-195 all archived)_
- [Stage 6 — Live dashboard / Plan loader + typed schema](stage-6-plan-loader-typed-schema.md) — _Done (archived 2026-04-15 — TECH-200 / TECH-201 / TECH-202 / TECH-203 closed; loader + types + RSC stub + README §Dashboard + JSDoc all landed)_
- [Stage 7 — Live dashboard / Dashboard RSC + filters](stage-7-dashboard-rsc-filters.md) — _Done (closed 2026-04-15 — TECH-205…TECH-208 archived)_
- [Stage 8 — Live dashboard / Legacy handoff + validation](stage-8-legacy-handoff-validation.md) — _Done — TECH-213 closed 2026-04-15 (archived); TECH-214 closed 2026-04-15 (archived). Stage 3.3 exit criteria met; Step 3 closed._
- [Stage 9 — Dashboard improvements + UI polish / Navigation sidebar + icon system](stage-9-navigation-sidebar-icon-system.md) — _Done (TECH-223…TECH-226 all closed 2026-04-16)_
- [Stage 10 — Dashboard improvements + UI polish / UI primitives polish + dashboard percentages](stage-10-ui-primitives-polish-dashboard-percentages.md) — _Done (TECH-231 + TECH-232 + TECH-233 + TECH-234 archived 2026-04-16)_
- [Stage 11 — Dashboard improvements + UI polish / D3.js data visualization](stage-11-d3-js-data-visualization.md) — _Done — TECH-239 + TECH-240 + TECH-241 + TECH-242 all closed 2026-04-16 (archived)_
- [Stage 12 — Dashboard improvements + UI polish / Multi-select dashboard filtering](stage-12-multi-select-dashboard-filtering.md) — _Done (TECH-247 + TECH-248 + TECH-249 + TECH-250 archived 2026-04-16)_
- [Stage 13 — Portal foundations (architecture-only at this tier) / Postgres provider + auth library selection](stage-13-postgres-provider-auth-library-selection.md) — _Done (TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived 2026-04-16)_
- [Stage 14 — Portal foundations (architecture-only at this tier) / Auth API stubs + schema draft](stage-14-auth-api-stubs-schema-draft.md) — _Done — TECH-261 + TECH-262 + TECH-263 + TECH-264 all archived 2026-04-17._
- [Stage 15 — Portal foundations (architecture-only at this tier) / Dashboard auth middleware migration](stage-15-dashboard-auth-middleware-migration.md) — _Done — Stage 5.3 closed 2026-04-17. Phase 0 (TECH-269), Phase 1 (TECH-265 + TECH-266), Phase 2 (TECH-267 + TECH-268) all archived. Next.js 16 migration note: `web/middleware.ts` → `web/proxy.ts` (rename surfaced during TECH-268 smoke; see Issues Found)._
- [Stage 16 — Playwright E2E harness / Install + config + CI wiring](stage-16-install-config-ci-wiring.md) — _Done (closed 2026-04-17 — TECH-276 archived)_
- [Stage 17 — Playwright E2E harness / Baseline route coverage](stage-17-baseline-route-coverage.md) — _Done (closed 2026-04-17 — TECH-277 archived)_
- [Stage 18 — Playwright E2E harness / Dashboard e2e (SSR filter flows)](stage-18-dashboard-e2e-ssr-filter-flows.md) — _Done (closed 2026-04-17 — TECH-284 archived)_
- [Stage 19 — Release-scoped progress view / Registry + pure shapers](stage-19-registry-pure-shapers.md) — _Final (4 tasks filed 2026-04-17 — TECH-339..TECH-342; all archived 2026-04-18)_
- [Stage 20 — Release-scoped progress view / Routes + progress tree surface](stage-20-routes-progress-tree-surface.md) — _Final — TECH-351, TECH-352, TECH-353, TECH-354 archived 2026-04-18_
- [Stage 21 — Release-scoped progress view / Auth wiring, nav link + docs](stage-21-auth-wiring-nav-link-docs.md) — _Final — TECH-358..TECH-361 archived 2026-04-18_
- [Stage 22 — Visual design layer / Design system spec + token pipeline](stage-22-design-system-spec-token-pipeline.md) — _Done — 4 / 4 tasks closed (TECH-618..TECH-621)._
- [Stage 23 — Visual design layer / Prose + surface primitives](stage-23-prose-surface-primitives.md) — _Done — TECH-622…TECH-625 closed 2026-04-22 (archived). Heading, Prose, Surface + motion CSS, dev `/design-system` page (`app/(dev)/design-system/`)._
- [Stage 24 — Visual design layer / CD bundle extraction + transcription pipeline](stage-24-cd-bundle-extraction-transcription-pipeline.md) — _Done — TECH-630…TECH-633 shipped 2026-04-22_
- [Stage 25 — Visual design layer / Console chrome primitive library](stage-25-console-chrome-primitive-library.md) — _Final_
- [Stage 26 — Visual design layer / Asset pipeline + media transport strip](stage-26-asset-pipeline-media-transport-strip.md) — _Final_
- [Stage 27 — Visual design layer / Full-flow screen port + port harness](stage-27-full-flow-screen-port-port-harness.md) — _Final_
- [Stage 28 — Visual design layer / Broad component token migration](stage-28-broad-component-token-migration.md) — _Done (closed 2026-04-22 — TECH-667, TECH-668 archived; `materialize-backlog` + `validate:all` green)_
- [Stage 29 — Visual design layer / Docs + validation](stage-29-docs-validation.md) — _Done — Stage 29 (shipped 2026-04-22)_
- [Stage 30 — Catalog admin CRUD views / List + detail surface](stage-30-list-detail-surface.md) — _Draft (tasks _pending_ — not yet filed; Step 9 opens only when Step 8 Final + grid-asset-visual-registry Step 1.3 shipped)_
- [Stage 31 — Catalog admin CRUD views / Edit + create forms + retire action](stage-31-edit-create-forms-retire-action.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 32 — Catalog admin CRUD views / Pool management surface](stage-32-pool-management-surface.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 33 — Catalog admin CRUD views / Docs + nav polish + E2E](stage-33-docs-nav-polish-e2e.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 34 — Catalog composite authoring / Pools tree CRUD (supersedes Stage 32)](stage-34-pools-tree-crud-supersedes-stage-32.md) — _Draft (tasks _pending_ — not yet filed; Step 10 opens only when grid-asset-visual-registry Step 5 Stage 5.1 Final — pools self-ref tree + composite_type schemas shipped)_
- [Stage 35 — Catalog composite authoring / Composite-type schema admin + panels CRUD](stage-35-composite-type-schema-admin-panels-crud.md) — _Draft (tasks _pending_ — not yet filed; opens only when Stage 34 Final + grid-asset-visual-registry Step 5 Stage 5.2 Final — panels + buttons tables shipped)_
- [Stage 36 — Catalog composite authoring / Buttons + prefabs CRUD](stage-36-buttons-prefabs-crud.md) — _Draft (tasks _pending_ — not yet filed; opens only when Stage 35 Final + grid-asset-visual-registry Step 5 Stage 5.3 Final — prefabs table shipped)_
- [Stage 37 — Catalog composite authoring / Snapshot management (list changes · diff · publish)](stage-37-snapshot-management-list-changes-diff-publish.md) — _Draft (tasks _pending_ — not yet filed; opens only when Stage 36 Final + grid-asset-visual-registry Step 6 Stage 6.3 Final — publish + diff + reload broadcast shipped)_
- [Stage 38 — Catalog composite authoring / Docs + nav polish + E2E](stage-38-docs-nav-polish-e2e.md) — _Draft (tasks _pending_ — not yet filed; opens only when Stages 34..37 Final)_

## Deferred decomposition

Materialize when the named step opens (per `ia/rules/project-hierarchy.md` lazy-materialization rule). Do NOT pre-decompose — surface area changes once Step {N-1} lands.

- **Step 2 — Public surface + wiki + devlog:** decomposed 2026-04-15. Stages: `MDX pipeline + public pages + SEO`, `Wiki + glossary auto-index + search`, `Devlog + RSS + origin story`.
- **Step 3 — Live dashboard:** decomposed 2026-04-15. Stages: `Plan loader + typed schema`, `Dashboard RSC + filters`, `Legacy handoff + validation`.
- **Step 4 — Dashboard improvements + UI polish:** decomposed 2026-04-16. Stages: `Navigation sidebar + icon system`, `UI primitives polish + dashboard percentages`, `D3.js data visualization`, `Multi-select dashboard filtering`.
- **Step 5 — Portal foundations:** decomposed 2026-04-15. Stages: `Postgres provider + auth library selection`, `Auth API stubs + schema draft`, `Dashboard auth middleware migration`. Paused until future instruction.
- **Step 6 — Playwright E2E harness:** decomposed 2026-04-15. Stages: `Install + config + CI wiring`, `Baseline route coverage`, `Dashboard e2e (SSR filter flows)`. Decompose-after trigger deferred to Step 5 close; paused until future instruction.

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `/stage-file {this-doc} Stage {N}.{M}` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/web-platform-exploration.md`.
- Keep public-facing copy under `web/content/**` + `web/app/**` user-surface routes in full English (caveman exception — `agent-output-caveman.md` §exceptions). Agent-authored IA prose (specs, skills, handoffs) stays caveman.
- Pin `tools/progress-tracker/parse.mjs` as authoritative — `web/lib/plan-loader.ts` (Step 3) is a read-only wrapper; do NOT fork parser logic.
- When Step 5 portal stage opens, raise recommendation to create `docs/web-platform-post-mvp-extensions.md` scope-boundary doc; exploration doc's Deferred / out of scope list currently carries post-MVP items inline but no companion doc exists yet.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (payment gateway, cloud saves, community wiki edits, i18n, Unity WebGL export) into MVP stages — they belong in the post-MVP extensions doc once created.
- Pre-decompose Steps 2+ before Step 1 closes — surface area changes.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` green, Vercel deploy green).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Couple web platform work to game ship date (Q3 locked decision) — web investment proceeds independently.
- Consume the MCP server (`territory-ia`) from the Next.js app — MCP stays stdio dev-only (Q7 lock).

---

## Orchestrator Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-14 | Compress Stage 1.1 from 6 tasks (TECH-129..TECH-134) to 1 consolidated issue (TECH-136) | Each original task was ≤1 file or docs-only; no task had been kicked off; single orchestration unit reduces step overhead and handoff friction | Keep 6-task split — rejected, over-granular for units this small |
| 2026-04-14 | `npm --prefix web` composition for `validate:all` (not `cd web && …`) | Cleaner exit-code propagation; no subshell state quirks | `cd web && …` — rejected |
| 2026-04-14 | Caveman-exception scope narrowed to user-facing rendered text (`web/content/**` + page-body JSX strings in `web/app/**/page.tsx`) | Prevents drift in app shell code, identifiers, commits, comments, IA prose | Broader `web/app/**` scope — rejected, invites non-rendered prose to go full-English |
| 2026-04-14 | Vercel link + first deploy flagged `[HUMAN ACTION]` upfront in future stage specs | Dashboard-only; no CLI auth in agent env; discovered mid-Phase-2 on TECH-136 | Attempt CLI automation — rejected, no creds surface |
| 2026-04-14 | Stage 1.2: merge T1.2.1 + T1.2.2 → single tokens + Tailwind wiring task (archived) | Tokens + wiring ship together; smoke verify (`bg-canvas text-accent-critical`) needs both halves; each side ≤2 files | Keep split 6-task stage — rejected per task sizing heuristic (two ≤2-file tasks) |
| 2026-04-15 | Playwright chosen as e2e framework (Step 6) over Cypress + Puppeteer | SSR/RSC filter flows require real request cycle — Playwright's browser context hits the server, validating what actually renders; TypeScript-first; built-in test runner; CI-friendly `--with-deps`; multi-browser (Chromium sufficient for CI) | Cypress — client-DOM bias, weaker RSC support, heavier CI image; Puppeteer — Chrome-only, no built-in runner, more glue code |
| 2026-04-15 | `validate:e2e` is a separate root target, not merged into `validate:all` | Browser install (`playwright install`) is heavy; agent CI runs `validate:all` headlessly without browser deps; e2e runs in a dedicated CI step or manually | Merge into `validate:all` — rejected, breaks non-e2e agent shells |
| 2026-04-15 | Deprecate `docs/progress.html` after Step 5 portal-auth gate lands ≥2 stable deploy cycles | Avoid premature removal while portal auth unresolved; live `/dashboard` stays obscure-URL-gated until auth middleware lands; ≥2 deploy cycles gives rollback window if dashboard regresses | Immediate delete — rejected, leaves no fallback if dashboard regresses; link-only banner (archived TECH-213) + no trigger — rejected, leaves legacy indefinitely without closure condition |
| 2026-04-15 | Insert Step 4 (Dashboard improvements + UI polish) before portal/E2E; shift former Steps 4→5, 5→6 | Portal auth (now Step 5) and Playwright E2E (now Step 6) paused until future instruction; dashboard UI improvements (sidebar, icons, D3 charts, multi-select filters) prioritized as next active work; no task filings affected — all deferred tasks were _pending_ | Append as Step 7 — rejected, sequential numbering should reflect implementation order; keeping old numbering — rejected, misleads about active next step |
| 2026-04-16 | Free-tier Postgres provider: **Neon free (Launch tier)** | Pooled connections: 100 > expected ≤ 20 concurrent serverless functions; storage: 0.5 GB vs ≤ 0.1 GB at Stage 5.2 stub (flag monitoring at 0.4 GB); egress: 5 GB/month >> dev traffic; region us-east-1 matches Vercel project default; `@neondatabase/serverless` HTTP driver avoids TCP socket leak on serverless cold-start — no persistent connection held across Next.js function invocations; branch preview-DB feature (up to 10 branches) enables per-PR isolated DBs at TECH-254+ stage; auto-suspend threshold 5 min acceptable for dev workload | **Supabase free** — rejected: 7-day inactivity pause risks portal dashboard latency on low-traffic days; bundled auth/storage/edge surface adds unneeded scope (auth owned by TECH-253); **Vercel Postgres Hobby** — rejected: tightest caps (storage 256 MB, egress 1 GB/month) already near Stage 5.2 stub ceiling; single-region lock at project creation inflexible; Neon-backed underneath so no reliability differentiation vs. Neon direct — no net advantage to justify tighter caps |
| 2026-04-17 | Stage 6.1: merge T6.1.1 + T6.1.2 + T6.1.3 → single TECH-276 | Pure setup boilerplate — install + config + scripts + README docs ship together; ≤5 files total (`web/package.json`, `web/playwright.config.ts`, `web/tests/.gitkeep`, `web/README.md`, root `package.json`, `.gitignore`); smoke verify (`cd web && npm run test:e2e` exit 0 w/ empty `tests/`) needs all halves; single orchestration unit reduces handoff friction. Precedent: 2026-04-14 Stage 1.1 + Stage 1.2 merges. | Keep 3-task split — rejected, each phase ≤2 files w/ no independent verify gate. |
| 2026-04-17 | Stage 6.3: collapse T6.3.1 + T6.3.2 → single TECH-284 | Test-only, single file (`web/tests/dashboard-filters.spec.ts`), single verify gate (`cd web && npm run test:e2e` green); single-param + multi-param + clear-filters + empty-state scenarios share one spec file + fixtures — splitting forces redundant imports + duplicated setup. Precedent: 2026-04-17 Stage 6.2 pattern (TECH-277 authored routes.spec.ts + meta.spec.ts under one issue). | Keep 2-task split — rejected, no independent verify gate per phase; phases only differ in test case coverage w/in same file. |
| 2026-04-16 | Auth library: **roll-own JWT + sessions** (Q11 confirmed). Constants: `SESSION_COOKIE_NAME=portal_session`, `SESSION_LIFETIME_DAYS=30`, password hash lib `@node-rs/argon2` (argon2id, Node runtime only — route handlers only, not middleware). API surface: `jose` (`SignJWT` / `jwtVerify`, Edge-safe Web Crypto) for token sign/verify; stateful `session` DB row (`id UUID PK, user_id UUID FK, expires_at TIMESTAMPTZ, token TEXT`) for revocation; cookie set via `cookies()` from `next/headers` in server actions, read via `request.cookies.get(SESSION_COOKIE_NAME)` in Edge middleware. | Q11 exactly matches this pattern (stateful row, no third-party provider); `jose` covers middleware JWT verify on Edge runtime without Node-only deps; argon2id hash ops confined to Node-runtime route handlers — clean runtime split; zero external auth framework lock-in; drizzle types map directly to session row columns. | **Lucia Auth v3** — rejected: officially sunsetted/archived by author (pilcrow) in late 2025; no active maintainers; maintenance risk unacceptable for a session-first library that owns cookie + session lifecycle. **Auth.js v5 (NextAuth)** — rejected: full OAuth/PKCE/CSRF machinery ships even with Credentials-only config (~50 kB server bundle overhead); Credentials provider + DB session requires Node runtime split anyway (same as roll-own); overkill for email+password MVP with no social login planned. |
