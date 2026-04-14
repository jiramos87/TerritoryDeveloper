# Web Platform — Master Plan (MVP)

> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** Unified Next.js 14+ app at `web/` (monorepo workspace) serving three audiences from one codebase — public game site (landing / wiki / devlog / about / install / history), live DevOps progress dashboard, and future user portal. Static-first hybrid on Vercel free tier; Postgres + auth deferred to portal step. Post-MVP extensions (payment gateway, cloud saves, community wiki edits, i18n, Unity WebGL export) tracked inline in exploration doc `### Implementation Points → Deferred / out of scope`; no separate scope-boundary doc yet.
>
> **Exploration source:** `docs/web-platform-exploration.md` (§Design Expansion → Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples).
>
> **Locked decisions (do not reopen in this plan):**
> - Stack: Next.js 14+ App Router, TypeScript, React Server Components, Tailwind CSS. MCP server (`territory-ia`) stays stdio dev-only; NOT consumed by web app.
> - Repo layout: monorepo; Next.js app at `web/`; root `package.json` declares npm workspaces.
> - Hosting: Vercel free tier. Build root `web/`. Deploy on push to `main`.
> - Auth (W7): roll-own JWT + sessions. No third-party auth provider.
> - Free-tier constraint: every service (Vercel, Postgres when selected, etc.) must be zero recurring cost until revenue exists.
> - Design language: FUTBIN-style data density + NYT-style dark choropleth palette. Tokens exported as JSON so future Unity UI/UX plan reuses the same palette.
> - Dashboard access: obscure-URL gate at MVP (Q14), auth gate once portal lands. `robots.txt` disallow + unlinked route.
> - Public copy style: full English marketing prose (caveman exception — per `agent-output-caveman.md` §exceptions). Agent-authored IA prose stays caveman.
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

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Scaffold + design system foundation

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 1):** 1 closed (Stage 1.1 — **TECH-136** archived 2026-04-14; supersedes **TECH-129**..**TECH-134** — stage compress 2026-04-14)

**Objectives:** Bootstrap the `web/` workspace as a Next.js 14+ App Router app inside the existing monorepo, wire Vercel deploy on push to `main`, and fold the new workspace into `npm run validate:all` so CI catches lint / typecheck / build regressions. Land a design-system token layer (NYT dark palette, FUTBIN data density, JSON-exported palette for future Unity UI/UX reuse) plus the core data-dense primitives (`DataTable`, `StatBar`, `BadgeChip`, `FilterChips`, `HeatmapCell`, `AnnotatedMap`) needed by every downstream step. Scaffold layer must ship before any public page / wiki / dashboard work; design system must ship before public surface authoring starts so content is not later retrofit to tokens.

**Exit criteria:**

- `web/` directory exists with `package.json`, `tsconfig.json`, `app/`, `components/`, `lib/`, `content/` subtrees; `cd web && npm run dev` starts Next.js dev server and `cd web && npm run build` succeeds.
- Root `package.json` declares `"workspaces": ["web", "tools/*"]` without breaking existing `tools/*` installs.
- Vercel project linked to repo; `main` push triggers successful production deploy to a Vercel-assigned `*.vercel.app` URL.
- `npm run validate:all` chain includes `web/` lint + typecheck + build; CI green on a throwaway PR.
- `web/app/design/page.tsx` renders every primitive component (DataTable, StatBar, BadgeChip, FilterChips, HeatmapCell, AnnotatedMap) against representative fixture data; visual output matches NYT/FUTBIN reference aesthetic.
- `web/lib/tokens/palette.json` exports the full color + spacing + type-scale token set as JSON; `web/README.md` documents the export contract for future Unity UI/UX consumption.
- `web/README.md` documents local dev, content conventions, caveman-exception for public copy; `CLAUDE.md` + `AGENTS.md` append a `§Web` entry pointing at the new workspace.

**Art:** None. Design-system tokens + primitives are code-only; any illustrative fixture data inside `/design` route uses placeholder strings, not asset imports.

**Relevant surfaces (load when step opens):**
- `docs/web-platform-exploration.md` §Chosen Approach, §Architecture (entry / exit points), §Subsystem Impact, §Implementation Points W1 + W2.
- `tools/progress-tracker/parse.mjs` — authoritative plan parser; schema will be imported as TS types by `web/lib/` later. NOT modified in Step 1.
- `tools/progress-tracker/render.mjs` — static html generator; coexists untouched.
- `docs/progress.html` — legacy snapshot; untouched in Step 1.
- `package.json` (root) — workspaces entry added additively.
- `ia/rules/agent-output-caveman.md` §exceptions — caveman-exception scope rule referenced in `web/README.md`.
- `web/**` (new) — entire subtree new.
- `web/app/page.tsx` (new), `web/app/design/page.tsx` (new), `web/components/**` (new), `web/lib/tokens/palette.json` (new), `web/README.md` (new).

#### Stage 1.1 — Scaffold + deploy + CI

**Status:** In Progress — TECH-136 done (archived); Phase 2 human-action items (Vercel link + throwaway PR CI verify) pending — 1 consolidated task closed (**TECH-136**; supersedes **TECH-129**..**TECH-134** — stage compress 2026-04-14)

**Objectives:** Land the `web/` Next.js workspace inside the monorepo, wire Vercel deploy on push, and integrate the new workspace into `npm run validate:all` so lint / typecheck / build regressions trip CI. Document the new surface in `web/README.md` + `CLAUDE.md` + `AGENTS.md` so future agents discover it cold.

**Exit:**

- `web/` exists with Next.js 14+ App Router scaffold (`app/`, `components/`, `lib/`, `content/` subdirs stubbed), TypeScript strict, Tailwind configured.
- Root `package.json` workspaces array includes `"web"` alongside `"tools/*"`; root `npm install` succeeds.
- Vercel project linked; `main` push triggers production deploy; deploy URL reachable.
- `npm run validate:all` chain (see `package.json` scripts) runs `web/` lint + typecheck + build; green on a throwaway PR.
- `web/README.md` documents `cd web && npm run dev`, content conventions, and caveman-exception for public copy.
- `CLAUDE.md` + `AGENTS.md` each gain a `§Web` section pointing at `web/` and the new dev commands.

**Phases:**

- [x] Phase 1 — Workspace bootstrap (root workspaces + Next.js scaffold).
- [ ] Phase 2 — Deploy + CI integration (Vercel link + `validate:all` entry). _(validate:all wired + green; Vercel link + throwaway-PR CI verify pending — human action)_
- [x] Phase 3 — Documentation (web README + repo-level docs append).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | 1 | **TECH-136** | Done (archived) | Scaffold `web/` Next.js 14+ workspace — root workspaces entry, App Router scaffold w/ TS strict + Tailwind + ESLint, Vercel deploy link (prod branch `main`, capture `*.vercel.app` URL), extend root `validate:all` CI chain, author `web/README.md`, append `§Web` to `CLAUDE.md` + `AGENTS.md`. Supersedes T1.1.1..T1.1.6 (TECH-129..TECH-134) — stage compress 2026-04-14. |

#### Stage 1.2 — Design system foundation

**Status:** In Progress — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips tasks closed (archived 2026-04-14); TECH-145..TECH-146 Draft (Phase 2 + 3)

**Objectives:** Land the token layer (NYT dark palette, type scale, spacing) and the six core primitives (`DataTable`, `StatBar`, `BadgeChip`, `FilterChips`, `HeatmapCell`, `AnnotatedMap`) that every downstream public page / wiki / dashboard will consume. Export the palette as JSON under `web/lib/tokens/palette.json` so future Unity UI/UX master plan can consume the same design language (per Q15 cross-cutting note). Ship a `/design` route as a live visual-review surface covering all primitives against fixture data.

**Exit:**

- `web/lib/tokens/` exports `palette.json`, `type-scale.json`, `spacing.json`; Tailwind config (`web/tailwind.config.ts`) imports these as its color / spacing / font-family source of truth.
- `web/components/DataTable.tsx`, `StatBar.tsx`, `BadgeChip.tsx`, `FilterChips.tsx`, `HeatmapCell.tsx`, `AnnotatedMap.tsx` each render against fixture props and have unit-style snapshot smoke (render + assert no throw, if test infra lands at this stage; else manual visual verify at `/design` route).
- `web/app/design/page.tsx` renders every primitive in isolation with 2–3 fixture variants; served under `/design` on dev + deploy; dashboard access gate (Q14 obscure-URL) NOT applied yet since `/design` is internal review only (document that follow-up in Step 3 surfaces).
- `web/README.md` §Tokens documents the JSON export contract (keys, semantic naming, consumption pattern for Unity UI/UX follow-up plan).
- New glossary row candidate: "Web design token set" — canonical name for the palette + type-scale + spacing bundle. Deferred to glossary authoring in Stage 1.2 close (add once tokens stabilize).

**Phases:**

- [ ] Phase 1 — Token layer (palette + type + spacing JSON + Tailwind wiring).
- [ ] Phase 2 — Data-dense primitives (the six components).
- [ ] Phase 3 — Review surface (`/design` route + docs).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | 1 | _(archived)_ | Done (archived) | Tokens + Tailwind wiring — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/lib/tokens/{palette,type-scale,spacing}.json` + semantic aliases; wired via Tailwind v4 `@theme` CSS custom properties in `globals.css`. |
| T1.2.2 | 1 | _(archived)_ | Done (archived) | _(merged into T1.2.1 — see archive)_ |
| T1.2.3 | 2 | _(archived)_ | Done (archived) | DataTable + BadgeChip SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/DataTable.tsx` (typed generic `<T,>` + `Column<T>` + `statusCell` slot + `aria-sort`-only sortable indicator) + `BadgeChip.tsx` (4-status enum → `bg-status-*` + `text-status-*-fg` semantic aliases). Phase 1 extended palette JSON + `@theme` w/ new `raw.green` + 8 status aliases before component authoring. |
| T1.2.4 | 2 | _(archived)_ | Done (archived) | StatBar + FilterChips SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/StatBar.tsx` (`TIER_FILL` dispatch → semantic `bg-[var(--color-text-accent-warn\|critical)]` arbitrary utilities; raw-value tier resolution; `pct` clamp guards `max ≤ 0`) + `FilterChips.tsx` (`chips[]` row, no `onClick`, `active` → `bg-panel`/`text-primary`). No new `bg-accent-*` palette aliases added (deferred until ≥2 bar-style consumers). |
| T1.2.5 | 2 | **TECH-145** | Draft | Author `web/components/HeatmapCell.tsx` + `AnnotatedMap.tsx`. HeatmapCell: single grid cell with `intensity` (0–1) → palette bucket. AnnotatedMap: SVG wrapper with `regions` + `annotations` props; renders NYT-style spaced-caps geo labels. Both SSR-only. |
| T1.2.6 | 3 | **TECH-146** | Draft | Author `web/app/design/page.tsx` rendering every primitive from T1.2.3–T1.2.5 against 2–3 fixture variants each. Add a brief header explaining the page is internal-review-only. Update `web/README.md` §Tokens documenting the palette JSON export contract (keys, semantic alias convention, Unity UI/UX consumption pattern stub). |

---

### Step 2 — Public surface + wiki + devlog

**Status:** Draft — decomposition deferred until Step 1 closes.

**Objectives:** Land the public-facing Next.js routes — landing page (`/`), about (`/about`), install (`/install`), project history (`/history`) — plus the MDX-driven wiki (`/wiki/[...slug]`) with auto-indexed glossary-derived term pages, and the devlog (`/devlog/[slug]`) with origin-story static page + living post list + RSS feed. All surfaces consume the Stage 1.2 design system + tokens; content authored as MDX under `web/content/**`. Wiki filters internal spec-ref columns from glossary imports; devlog is manual MDX at launch (no auto-pull from BACKLOG-ARCHIVE). SEO basics (sitemap, `robots.txt`, OpenGraph images) ship as part of the public surface.

**Exit criteria:**

- `/`, `/about`, `/install`, `/history` render from MDX under `web/content/pages/*.mdx`; design system tokens used exclusively — no ad-hoc colors.
- `/wiki/[...slug]` resolves MDX pages under `web/content/wiki/**.mdx`; auto-index route lists glossary-derived terms from `ia/specs/glossary.md` with `Spec reference` column filtered out (Term + Definition only).
- `/devlog` lists posts from `web/content/devlog/YYYY-MM-DD-slug.mdx`; origin-story static page present; `/devlog/[slug]` renders single post with cover image, tags, read time.
- `/feed.xml` RSS feed exposes devlog posts.
- `sitemap.xml` + `robots.txt` live; OpenGraph default image present.
- Client-side search (`fuse.js` over prebuilt index) works on `/wiki`.

**Stages:** _TBD — decompose after Step 1 lands + reveals surface area._

---

### Step 3 — Live dashboard

**Status:** Draft — decomposition deferred until Step 2 closes.

**Objectives:** Replace the static `docs/progress.html` snapshot with a live React Server Component dashboard at `/dashboard` that reads every master plan under `ia/projects/*-master-plan.md` via a thin wrapper around `tools/progress-tracker/parse.mjs`. Filter chips (per-plan / per-status / per-phase) use the Stage 1.2 `FilterChips` primitive; table uses `DataTable`. Apply the Q14 obscure-URL gate — unlinked route, `robots.txt` disallow, "internal" banner — until Step 4 portal auth lands, at which point dashboard migrates behind auth middleware. Add "Live dashboard" link on the legacy `docs/progress.html`; deprecate the legacy page once dashboard has proven stable for a measurable duration (exact trigger tracked in Step 3 Decision Log).

**Exit criteria:**

- `web/lib/plan-loader.ts` wraps `tools/progress-tracker/parse.mjs`; exports typed `PlanData` + `TaskRow` consumed by RSC.
- `/dashboard` renders every master plan; filter chips function (active state reflected via query params; SSR only for MVP, client-interactive hydration as optional enhancement).
- `/dashboard` unlinked from any public nav; `robots.txt` disallows; "internal" banner visible.
- `docs/progress.html` gains a "Live dashboard" link at top with the deploy URL.
- `parse.mjs` remains unchanged — wrapper only; output schema pinned via JSDoc + TS types in `web/lib/`.

**Stages:** _TBD — decompose after Step 2 lands + reveals surface area._

---

### Step 4 — Portal foundations (architecture-only at this tier)

**Status:** Draft — decomposition deferred until Step 3 closes.

**Objectives:** Land the user-portal foundations — free-tier Postgres provider selected (Neon / Supabase free / Vercel Postgres Hobby — evaluate limits against expected volume); auth stack picked (roll-own JWT + sessions per Q11; confirm vs. Lucia-Auth-style minimal library before committing); stub `app/api/auth/*` route handlers with no user-facing flow; schema drafted for `user` / `session` / `save` / `entitlement` tables but NOT yet migrated. Dashboard migrates from obscure-URL gate to auth middleware once session handling works end-to-end. Payment gateway remains deferred (Q10 undecided) — architecture slot reserved, no provider wiring at this tier. This step intentionally stays architecture-only; user-facing portal UX ships in a follow-up master plan after this step's foundations lock.

**Exit criteria:**

- Free-tier Postgres provider selected; `web/lib/db/` wraps a single connection pool; `DATABASE_URL` env wired into Vercel project env vars.
- Auth library decision locked in Decision Log; `web/app/api/auth/login`, `register`, `session`, `logout` route handlers present (stub bodies, return 501 Not Implemented until follow-up plan).
- Schema draft under `web/lib/db/schema.ts` covers `user`, `session`, `save`, `entitlement`; migration tool chosen (drizzle-kit or prisma migrate) but migrations NOT yet run.
- Dashboard `/dashboard` now behind an auth middleware check (obscure-URL gate removed); unauthenticated users get redirect to a stub login page; stub login returns a canned error at this tier.
- Payment gateway architecture slot documented in `web/README.md` §Portal as a placeholder, no provider chosen.

**Stages:** _TBD — decompose after Step 3 lands + reveals surface area._

---

## Deferred decomposition

Materialize when the named step opens (per `ia/rules/project-hierarchy.md` lazy-materialization rule). Do NOT pre-decompose — surface area changes once Step {N-1} lands.

- **Step 2 — Public surface + wiki + devlog:** decompose after Step 1 closes. Candidate stages: `Public pages + SEO` (W3), `Wiki + glossary auto-index + search` (W4), `Devlog + RSS` (W5).
- **Step 3 — Live dashboard:** decompose after Step 2 closes. Candidate stages: `Plan loader + typed schema`, `Dashboard RSC + filters`, `Legacy deprecation + access gate`.
- **Step 4 — Portal foundations:** decompose after Step 3 closes. Candidate stages: `Postgres provider + auth library selection`, `Auth API stubs + schema draft`, `Dashboard auth migration`.

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `/stage-file {this-doc} Stage {N}.{M}` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/web-platform-exploration.md`.
- Keep public-facing copy under `web/content/**` + `web/app/**` user-surface routes in full English (caveman exception — `agent-output-caveman.md` §exceptions). Agent-authored IA prose (specs, skills, handoffs) stays caveman.
- Pin `tools/progress-tracker/parse.mjs` as authoritative — `web/lib/plan-loader.ts` (Step 3) is a read-only wrapper; do NOT fork parser logic.
- When Step 4 portal stage opens, raise recommendation to create `docs/web-platform-post-mvp-extensions.md` scope-boundary doc; exploration doc's Deferred / out of scope list currently carries post-MVP items inline but no companion doc exists yet.

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

---
