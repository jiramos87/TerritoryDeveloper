# Web Platform — Master Plan (MVP)

> **Last updated:** 2026-04-17
>
> **Status:** MVP Done 2026-04-17 — Steps 1–6 all Final (Step 5 portal stages 5.1 + 5.2 + 5.3 all Done 2026-04-17; Step 6 all three stages Done 2026-04-17). Post-MVP extensions now tracked in companion doc `docs/web-platform-post-mvp-extensions.md` — ready for `/design-explore` poll-based expansion + `/master-plan-extend` Step 7+.
>
> **Scope:** Unified Next.js 14+ app at `web/` (monorepo workspace) serving three audiences from one codebase — public game site (landing / wiki / devlog / about / install / history), live DevOps progress dashboard, and future user portal. Static-first hybrid on Vercel free tier; Postgres + auth deferred to portal step. Post-MVP extensions companion doc: `docs/web-platform-post-mvp-extensions.md` (seeded §1 rollout completion view + §§2–7 deferred stubs).
>
> **Exploration source:**
> - `docs/web-platform-exploration.md` (§Design Expansion → Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples) — MVP Steps 1..6.
> - `docs/web-platform-post-mvp-extensions.md` (§Design Expansion — §1 Release-scoped progress view + §8 Visual Design Layer) — extension source for Steps 7..8.
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

**Status:** Final

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

**Status:** Final — TECH-136 done (archived); Vercel project linked (`territory-developer-web`), production deploy confirmed 2026-04-15 (`https://web-nine-wheat-35.vercel.app`); validate:all green. Stage closed 2026-04-15.

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
- [x] Phase 2 — Deploy + CI integration (Vercel link + `validate:all` entry).
- [x] Phase 3 — Documentation (web README + repo-level docs append).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | 1 | **TECH-136** | Done (archived) | Scaffold `web/` Next.js 14+ workspace — root workspaces entry, App Router scaffold w/ TS strict + Tailwind + ESLint, Vercel deploy link (prod branch `main`, capture `*.vercel.app` URL), extend root `validate:all` CI chain, author `web/README.md`, append `§Web` to `CLAUDE.md` + `AGENTS.md`. Supersedes T1.1.1..T1.1.6 (TECH-129..TECH-134) — stage compress 2026-04-14. |

#### Stage 1.2 — Design system foundation

**Status:** Done — tokens + Tailwind wiring task + DataTable/BadgeChip + StatBar/FilterChips + HeatmapCell + AnnotatedMap + `/design` review route + README §Tokens all archived (see BACKLOG-ARCHIVE.md). Stage closed 2026-04-14.

**Objectives:** Land the token layer (NYT dark palette, type scale, spacing) and the six core primitives (`DataTable`, `StatBar`, `BadgeChip`, `FilterChips`, `HeatmapCell`, `AnnotatedMap`) that every downstream public page / wiki / dashboard will consume. Export the palette as JSON under `web/lib/tokens/palette.json` so future Unity UI/UX master plan can consume the same design language (per Q15 cross-cutting note). Ship a `/design` route as a live visual-review surface covering all primitives against fixture data.

**Exit:**

- `web/lib/tokens/` exports `palette.json`, `type-scale.json`, `spacing.json`; Tailwind config (`web/tailwind.config.ts`) imports these as its color / spacing / font-family source of truth.
- `web/components/DataTable.tsx`, `StatBar.tsx`, `BadgeChip.tsx`, `FilterChips.tsx`, `HeatmapCell.tsx`, `AnnotatedMap.tsx` each render against fixture props and have unit-style snapshot smoke (render + assert no throw, if test infra lands at this stage; else manual visual verify at `/design` route).
- `web/app/design/page.tsx` renders every primitive in isolation with 2–3 fixture variants; served under `/design` on dev + deploy; dashboard access gate (Q14 obscure-URL) NOT applied yet since `/design` is internal review only (document that follow-up in Step 3 surfaces).
- `web/README.md` §Tokens documents the JSON export contract (keys, semantic naming, consumption pattern for Unity UI/UX follow-up plan).
- New glossary row candidate: "Web design token set" — canonical name for the palette + type-scale + spacing bundle. Deferred to glossary authoring in Stage 1.2 close (add once tokens stabilize).

**Phases:**

- [x] Phase 1 — Token layer (palette + type + spacing JSON + Tailwind wiring).
- [x] Phase 2 — Data-dense primitives (the six components).
- [x] Phase 3 — Review surface (`/design` route + docs).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | 1 | _(archived)_ | Done (archived) | Tokens + Tailwind wiring — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/lib/tokens/{palette,type-scale,spacing}.json` + semantic aliases; wired via Tailwind v4 `@theme` CSS custom properties in `globals.css`. |
| T1.2.2 | 1 | _(archived)_ | Done (archived) | _(merged into T1.2.1 — see archive)_ |
| T1.2.3 | 2 | _(archived)_ | Done (archived) | DataTable + BadgeChip SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/DataTable.tsx` (typed generic `<T,>` + `Column<T>` + `statusCell` slot + `aria-sort`-only sortable indicator) + `BadgeChip.tsx` (4-status enum → `bg-status-*` + `text-status-*-fg` semantic aliases). Phase 1 extended palette JSON + `@theme` w/ new `raw.green` + 8 status aliases before component authoring. |
| T1.2.4 | 2 | _(archived)_ | Done (archived) | StatBar + FilterChips SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/StatBar.tsx` (`TIER_FILL` dispatch → semantic `bg-[var(--color-text-accent-warn\|critical)]` arbitrary utilities; raw-value tier resolution; `pct` clamp guards `max ≤ 0`) + `FilterChips.tsx` (`chips[]` row, no `onClick`, `active` → `bg-panel`/`text-primary`). No new `bg-accent-*` palette aliases added (deferred until ≥2 bar-style consumers). |
| T1.2.5 | 2 | _(archived)_ | Done (archived) | HeatmapCell + AnnotatedMap SSR-only primitives — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/components/HeatmapCell.tsx` (5-bucket `color-mix()` ramp anchored on existing semantic aliases) + `AnnotatedMap.tsx` (SVG wrapper w/ `regions` + `annotations` props; NYT-style spaced-caps geo labels via `letterSpacing: 0.15em` + `textTransform: uppercase`). Both SSR-only. |
| T1.2.6 | 3 | _(archived)_ | Done (archived) | `/design` review route + README §Tokens — see BACKLOG-ARCHIVE.md 2026-04-14. Authored `web/app/design/page.tsx` SSR-only rendering all six primitives w/ 2–3 fixture variants each; inline fixtures at module scope; internal-review banner (caveman prose, internal-facing). `web/README.md` §Tokens documents palette JSON file layout + `{raw.<key>}` indirection via `resolveAlias` in `web/lib/tokens/index.ts` + Unity UI/UX consumption stub. Glossary row "Web design token set" deferred per Exit bullet 5. |

---

### Step 2 — Public surface + wiki + devlog

**Status:** Done (closed 2026-04-15 — Stages 2.1, 2.2, 2.3 all closed)

**Backlog state (Step 2):** 0 filed

**Objectives:** Land the public-facing Next.js routes — landing page (`/`), about (`/about`), install (`/install`), project history (`/history`) — plus the MDX-driven wiki (`/wiki/[...slug]`) with auto-indexed glossary-derived term pages, and the devlog (`/devlog/[slug]`) with origin-story static page + living post list + RSS feed. All surfaces consume the Stage 1.2 design system + tokens; content authored as MDX under `web/content/**`. Wiki filters internal spec-ref columns from glossary imports; devlog is manual MDX at launch (no auto-pull from BACKLOG-ARCHIVE). SEO basics (sitemap, `robots.txt`, OpenGraph images) ship as part of the public surface.

**Exit criteria:**

- `/`, `/about`, `/install`, `/history` render from MDX under `web/content/pages/*.mdx`; design system tokens used exclusively — no ad-hoc colors.
- `/wiki/[...slug]` resolves MDX pages under `web/content/wiki/**.mdx`; auto-index route lists glossary-derived terms from `ia/specs/glossary.md` with `Spec reference` column filtered out (Term + Definition only).
- `/devlog` lists posts from `web/content/devlog/YYYY-MM-DD-slug.mdx`; origin-story static page present; `/devlog/[slug]` renders single post with cover image, tags, read time.
- `/feed.xml` RSS feed exposes devlog posts.
- `sitemap.xml` + `robots.txt` live; OpenGraph default image present.
- Client-side search (`fuse.js` over prebuilt index) works on `/wiki`.

**Art:** None. OpenGraph default image = token-palette-driven SVG or flat PNG (design-system derived); no illustrator assets at this tier.

**Relevant surfaces (load when step opens):**
- Step 1 outputs: `web/lib/tokens/*.json`, `web/tailwind.config.ts`, `web/components/{DataTable,StatBar,BadgeChip,FilterChips,HeatmapCell,AnnotatedMap}.tsx`, `web/app/layout.tsx`, `web/app/page.tsx`, `web/README.md` §Tokens — all consumed, not modified.
- `docs/web-platform-exploration.md` §Implementation Points W3 (public pages + SEO), W4 (wiki + glossary auto-index + search), W5 (devlog + RSS).
- `ia/specs/glossary.md` — authoritative source for wiki auto-index; NOT modified. Parsed at build time; `Spec reference` column filtered.
- `ia/rules/agent-output-caveman.md` §exceptions — caveman-exception applies to `web/content/**` MDX + user-facing page-body JSX strings; app shell code stays caveman.
- `web/content/pages/*.mdx` (new), `web/content/wiki/**.mdx` (new), `web/content/devlog/YYYY-MM-DD-*.mdx` (new).
- `web/app/about/page.tsx` (new), `web/app/install/page.tsx` (new), `web/app/history/page.tsx` (new).
- `web/app/wiki/[...slug]/page.tsx` (new), `web/app/wiki/page.tsx` (new auto-index).
- `web/app/devlog/page.tsx` (new list), `web/app/devlog/[slug]/page.tsx` (new single).
- `web/app/feed.xml/route.ts` (new), `web/app/sitemap.ts` (new), `web/app/robots.ts` (new), `web/app/opengraph-image.tsx` (new).
- `web/lib/mdx/` (new) — MDX loader + frontmatter parser + reading-time calc.
- `web/lib/glossary/import.ts` (new) — parses `ia/specs/glossary.md`, strips `Spec reference` column, emits typed `GlossaryTerm[]`.
- `web/lib/search/build-index.ts` (new) — builds fuse.js JSON index at build time; consumed by client-side search component.
- `web/next.config.ts` — extended with MDX plugin (`@next/mdx` + remark/rehype chain).
- Invariants: `ia/rules/invariants.md` #1–#12 NOT implicated (no runtime C# / Unity coupling).

#### Stage 2.1 — MDX pipeline + public pages + SEO

**Status:** Done 2026-04-15 — all tasks archived (TECH-163 … TECH-168).

**Objectives:** Wire the MDX content pipeline (`@next/mdx`, remark/rehype, typed frontmatter) so `web/content/**` compiles into RSC routes. Ship the four static public pages (`/`, `/about`, `/install`, `/history`) consuming Stage 1.2 primitives + tokens. Ship SEO bedrock (`sitemap.ts`, `robots.ts`, `opengraph-image.tsx`, per-route `generateMetadata`). Landing page replaces the Next.js boilerplate in current `web/app/page.tsx`.

**Exit:**

- `web/next.config.ts` extended with `@next/mdx` + `remark-frontmatter` + `remark-gfm` + `rehype-slug` + `rehype-autolink-headings`; `.mdx` pages compile under `web/content/`.
- `web/lib/mdx/loader.ts` exports `loadMdxPage(slug: string): Promise<{ source: MDXRemoteSerializeResult, frontmatter: PageFrontmatter }>` + typed `PageFrontmatter` interface (title, description, updated, hero?).
- `web/content/pages/{landing,about,install,history}.mdx` authored in full English (caveman-exception per `agent-output-caveman.md` §exceptions); each carries frontmatter.
- `web/app/page.tsx` (landing replacement), `web/app/about/page.tsx`, `web/app/install/page.tsx`, `web/app/history/page.tsx` — each RSC reads matching MDX via `loadMdxPage`; design tokens exclusively (no inline hex); `DataTable` + `StatBar` used where data-density content warrants.
- `web/app/sitemap.ts` enumerates static routes + MDX-derived slugs; `web/app/robots.ts` allows `/` + disallows `/design` (internal review route); `web/app/opengraph-image.tsx` generates token-palette OG card via `next/og`.
- Per-route `generateMetadata` sets title + description + OG image from frontmatter.
- `npm run validate:all` (web lint + typecheck + build) green.

**Phases:**

- [x] Phase 1 — MDX pipeline wiring (`next.config.ts`, loader, typed frontmatter).
- [x] Phase 2 — Public pages (landing / about / install / history + MDX content).
- [x] Phase 3 — SEO bedrock (sitemap, robots, OG image, metadata).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T2.1.1 | 1 | **TECH-163** | Done (archived) | Install + wire MDX pipeline — add `@next/mdx`, `@mdx-js/loader`, `@mdx-js/react`, `remark-frontmatter`, `remark-gfm`, `rehype-slug`, `rehype-autolink-headings` to `web/package.json`; extend `web/next.config.ts` with `withMDX` + plugin chain; configure `pageExtensions` to include `mdx`. |
| T2.1.2 | 1 | **TECH-164** | Done (archived) | Author `web/lib/mdx/loader.ts` + `web/lib/mdx/types.ts` — `loadMdxPage(slug)` reads from `web/content/pages/{slug}.mdx`, parses frontmatter via `gray-matter`, returns `{ source, frontmatter }`; typed `PageFrontmatter` interface (title, description, updated ISO date, hero optional). Companion `loadMdxContent(dir, slug)` generic helper for reuse by wiki + devlog stages. |
| T2.1.3 | 2 | **TECH-165** | Done (archived) | Replace boilerplate `web/app/page.tsx` w/ landing RSC consuming `web/content/pages/landing.mdx`; author full-English landing MDX (hero + what-this-is + CTA to `/install` + `/history`). Tokens exclusive — no inline hex. |
| T2.1.4 | 2 | TECH-166 | Done (archived) | Author `web/app/about/page.tsx`, `web/app/install/page.tsx`, `web/app/history/page.tsx` RSCs + matching `web/content/pages/{about,install,history}.mdx`. `/history` uses `DataTable` to render timeline rows from MDX-embedded data; `/install` uses `BadgeChip` for platform tags. |
| T2.1.5 | 3 | TECH-167 | Done (archived) | Author `web/app/sitemap.ts` + `web/app/robots.ts` — sitemap enumerates static public routes + MDX slugs (landing, about, install, history); robots allows `/`, disallows `/design` + `/dashboard` (reserved for Step 3). |
| T2.1.6 | 3 | TECH-168 | Done (archived) | Author `web/app/opengraph-image.tsx` via `next/og` — token-palette-driven OG card (title + subtitle from site-level metadata); per-route `generateMetadata` in each public page returns title + description + OG image url derived from frontmatter. |

#### Stage 2.2 — Wiki + glossary auto-index + search

**Status:** Done (closed 2026-04-15 — TECH-184…TECH-187 all archived)

**Objectives:** Ship the MDX-driven wiki at `/wiki/[...slug]` + auto-index landing at `/wiki` that merges hand-authored MDX pages w/ glossary-derived term rows imported from `ia/specs/glossary.md` (filtered to Term + Definition; `Spec reference` column stripped). Build-time fuse.js index feeds client-side search. No authoring of wiki content yet — scaffolding + 1 seed MDX page + full glossary import + search.

**Exit:**

- `web/lib/glossary/import.ts` parses `ia/specs/glossary.md` at build time — extracts term rows from markdown tables, strips `Spec reference` column, returns typed `GlossaryTerm[]` = `{ term, definition, slug, category? }`. Unit-coverable (pure string → struct).
- `web/app/wiki/[...slug]/page.tsx` RSC renders MDX from `web/content/wiki/**.mdx` via `loadMdxContent`; `generateStaticParams` enumerates all MDX slugs + glossary slugs.
- `web/app/wiki/page.tsx` — auto-index RSC lists glossary terms (from `import.ts`) + hand-authored wiki pages (from frontmatter scan); grouped by category; uses `DataTable` primitive.
- `web/content/wiki/README.mdx` seed page exists (sanity of loader).
- `web/lib/search/build-index.ts` — build-time script emits `web/public/search-index.json` (fuse.js-shaped records of `{ slug, title, body, tags }`) covering wiki + glossary entries.
- `web/components/WikiSearch.tsx` (client component) — `fuse.js` in-memory search against prebuilt index; rendered in `/wiki` header.
- `web/next.config.ts` or `web/package.json` `prebuild` script invokes `build-index.ts` before `next build`; `validate:all` remains green.

**Phases:**

- [x] Phase 1 — Glossary import + wiki routing scaffold.
- [x] Phase 2 — Search index build + client search component.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T2.2.1 | 1 | **TECH-184** | Done | Author `web/lib/glossary/import.ts` — reads `ia/specs/glossary.md` from repo root (relative path via `path.resolve(process.cwd(), '../ia/specs/glossary.md')` or equivalent build-safe mechanism); parses markdown tables via `remark-parse` or regex split; emits `GlossaryTerm[]` w/ `Spec reference` column filtered out; includes slug derivation (kebab-case term). Typed export consumed by wiki routes. |
| T2.2.2 | 1 | **TECH-185** | Done (archived) | Author `web/app/wiki/[...slug]/page.tsx` + `web/app/wiki/page.tsx` — catch-all route renders hand-authored MDX from `web/content/wiki/**.mdx` via `loadMdxContent('wiki', slug)` OR glossary-derived page (renders `GlossaryTerm.definition` in MDX-styled shell when slug matches imported term); `/wiki` index uses `DataTable` + groups by category; `generateStaticParams` unions MDX slugs + glossary slugs. Seed `web/content/wiki/README.mdx` with frontmatter + 1 paragraph. |
| T2.2.3 | 2 | **TECH-186** | Done (archived) | Author `web/lib/search/build-index.ts` + `web/package.json` `prebuild` entry — script consumes `GlossaryTerm[]` + scans `web/content/wiki/**.mdx` frontmatter/body, emits `web/public/search-index.json` (fuse.js records: `{ slug, title, body, category, type: 'glossary' | 'wiki' }`). Deterministic output for CI repeatability. |
| T2.2.4 | 2 | **TECH-187** | Done (archived) | Author `web/components/WikiSearch.tsx` client component — fetches `/search-index.json` on mount, constructs `Fuse` instance w/ `keys: ['title', 'body', 'category']`, threshold tuned for fuzzy match; renders input + result list linking to `/wiki/{slug}`. Embedded in `web/app/wiki/page.tsx` header. Install `fuse.js` into `web/package.json` deps. |

#### Stage 2.3 — Devlog + RSS + origin story

**Status:** Done (closed 2026-04-15 — TECH-192…TECH-195 all archived)

**Objectives:** Ship devlog list at `/devlog`, single-post route `/devlog/[slug]`, origin-story static page, and RSS feed at `/feed.xml`. All posts are manual MDX under `web/content/devlog/YYYY-MM-DD-slug.mdx` — no auto-pull from BACKLOG-ARCHIVE at MVP. Sitemap (Stage 2.1) regenerated to include devlog slugs.

**Exit:**

- `web/app/devlog/page.tsx` — RSC lists all MDX files in `web/content/devlog/` sorted by frontmatter `date` desc; each row: date + title + tag `BadgeChip`s + read-time + excerpt.
- `web/app/devlog/[slug]/page.tsx` — RSC renders single post via `loadMdxContent('devlog', slug)`; shows cover image (frontmatter `cover` field, optional), tags, computed read time, `generateMetadata` for OG.
- `web/content/devlog/2026-MM-DD-origin-story.mdx` — origin-story seed post authored (caveman-exception: full English).
- `web/app/feed.xml/route.ts` — Next.js route handler returning RSS 2.0 XML covering latest 20 devlog posts; `Content-Type: application/rss+xml`.
- `web/lib/mdx/reading-time.ts` — computes minutes from MDX word count; consumed by list + single views.
- `web/app/sitemap.ts` (from Stage 2.1) extended to enumerate devlog slugs; linked from landing or footer nav.
- `/feed.xml` validates against a public RSS validator (manual check captured in task spec).

**Phases:**

- [x] Phase 1 — Devlog routes + MDX content.
- [x] Phase 2 — RSS feed + sitemap integration.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T2.3.1 | 1 | **TECH-192** | Done (archived) | Author `web/app/devlog/page.tsx` + `web/lib/mdx/reading-time.ts` — list RSC scans `web/content/devlog/*.mdx` via filesystem read, parses frontmatter (`title`, `date`, `tags[]`, `cover?`, `excerpt`), sorts desc by `date`, renders card list w/ `BadgeChip` tags + read-time computed from MDX body. Extend `PageFrontmatter` or add `DevlogFrontmatter` type in `web/lib/mdx/types.ts`. |
| T2.3.2 | 1 | **TECH-193** | Done (archived) | Author `web/app/devlog/[slug]/page.tsx` + `web/content/devlog/2026-MM-DD-origin-story.mdx` — single-post RSC renders via `loadMdxContent('devlog', slug)`; cover image (frontmatter `cover` optional), tags row, read-time, `generateMetadata` returns OG image derived from cover or falling back to site default. Origin-story MDX seed authored in full English per caveman-exception. |
| T2.3.3 | 2 | **TECH-194** | Done (archived) | Author `web/app/feed.xml/route.ts` — Next.js route handler (`GET`) returns RSS 2.0 XML (`<rss version="2.0"><channel>…</channel></rss>`) enumerating latest 20 devlog posts w/ `<item>` per post (`title`, `link`, `description` from excerpt, `pubDate` RFC-822, `guid`); `Content-Type: application/rss+xml; charset=utf-8`. |
| T2.3.4 | 2 | **TECH-195** | Done (archived) | Extend `web/app/sitemap.ts` (from Stage 2.1) to enumerate devlog slugs via filesystem scan of `web/content/devlog/`; add footer nav link to `/feed.xml` + `/devlog` in `web/app/layout.tsx`. `validate:all` green. |

---

### Step 3 — Live dashboard

**Status:** Final

**Backlog state (Step 3):** Stage 3.1 — TECH-200…TECH-203 (archived); Stage 3.2 — TECH-205…TECH-208 (archived)

**Objectives:** Replace the static `docs/progress.html` snapshot with a live React Server Component dashboard at `/dashboard` that reads every master plan under `ia/projects/*-master-plan.md` via a thin wrapper around `tools/progress-tracker/parse.mjs`. Filter chips (per-plan / per-status / per-phase) use the Stage 1.2 `FilterChips` primitive; table uses `DataTable`. Apply the Q14 obscure-URL gate — unlinked route, `robots.txt` disallow, "internal" banner — until Step 5 portal auth lands, at which point dashboard migrates behind auth middleware. Add "Live dashboard" link on the legacy `docs/progress.html`; deprecate the legacy page once dashboard has proven stable for a measurable duration (exact trigger tracked in Step 3 Decision Log).

**Exit criteria:**

- `web/lib/plan-loader.ts` wraps `tools/progress-tracker/parse.mjs`; exports typed `PlanData` + `TaskRow` consumed by RSC.
- `/dashboard` renders every master plan; filter chips function (active state reflected via query params; SSR only for MVP, client-interactive hydration as optional enhancement).
- `/dashboard` unlinked from any public nav; `robots.txt` disallows; "internal" banner visible.
- `docs/progress.html` gains a "Live dashboard" link at top with the deploy URL.
- `parse.mjs` remains unchanged — wrapper only; output schema pinned via JSDoc + TS types in `web/lib/`.

**Art:** None. Dashboard is code/data surface — no illustrator assets.

**Relevant surfaces (load when step opens):**
- Step 2 outputs: `web/app/sitemap.ts`, `web/app/robots.ts`, `web/app/layout.tsx` — all consumed; `robots.ts` extended in Stage 3.2.
- Step 1 outputs: `web/components/{DataTable,FilterChips,BadgeChip}.tsx`, `web/lib/tokens/*.json`, `web/tailwind.config.ts` — consumed, not modified.
- `tools/progress-tracker/parse.mjs` — authoritative parser; exports `parseMasterPlan(markdown, filename)`. NOT modified.
- `docs/web-platform-exploration.md` §Implementation Points W6 (dashboard).
- `docs/progress.html` — legacy snapshot; amended in Stage 3.3 only.
- `web/lib/plan-loader.ts` (new), `web/lib/plan-loader-types.ts` (new).
- `web/app/dashboard/page.tsx` (new).
- Invariants: `ia/rules/invariants.md` #1–#12 NOT implicated — web platform only.

#### Stage 3.1 — Plan loader + typed schema

**Status:** Done (archived 2026-04-15 — TECH-200 / TECH-201 / TECH-202 / TECH-203 closed; loader + types + RSC stub + README §Dashboard + JSDoc all landed)

**Objectives:** Author `web/lib/plan-loader.ts` as a read-only wrapper around `tools/progress-tracker/parse.mjs`, exporting `loadAllPlans(): Promise<PlanData[]>` for RSC consumption. Pin the parse.mjs output schema as TypeScript interfaces so downstream consumers are type-safe and `parse.mjs` itself stays untouched.

**Exit:**

- `web/lib/plan-loader-types.ts` exports `TaskStatus`, `HierarchyStatus`, `TaskRow`, `PhaseEntry`, `Stage`, `Step`, `PlanData` TypeScript interfaces mirroring the parse.mjs JSDoc output schema exactly.
- `web/lib/plan-loader.ts` exports `loadAllPlans(): Promise<PlanData[]>` — globs `ia/projects/*-master-plan.md` from repo root, reads each file, calls `parseMasterPlan(content, filename)` via dynamic ESM import, returns typed array.
- `parse.mjs` has zero modifications — wrapper-only contract upheld.
- `validate:all` green; `loadAllPlans()` resolves with ≥1 plan against current repo state (confirmed in T3.1.3).
- `web/README.md` §Dashboard documents loader contract, `PlanData` shape, and "parse.mjs is authoritative" invariant.

**Phases:**

- [ ] Phase 1 — Types + loader implementation.
- [ ] Phase 2 — Build integration + smoke + docs.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.1.1 | 1 | **TECH-200** | Done (archived) | Author `web/lib/plan-loader-types.ts` — TypeScript interfaces: `TaskStatus` (union literal), `HierarchyStatus` (union literal), `TaskRow { id, phase, issue, status, intent }`, `PhaseEntry { checked, label }`, `Stage { id, title, status, statusDetail, phases, tasks }`, `Step { id, title, status, statusDetail, stages }`, `PlanData { title, overallStatus, overallStatusDetail, siblingWarnings, steps, allTasks }` — mirroring parse.mjs JSDoc schema exactly. |
| T3.1.2 | 1 | **TECH-201** | Done (archived) | Author `web/lib/plan-loader.ts` — `loadAllPlans(): Promise<PlanData[]>`: globs `ia/projects/*-master-plan.md` from repo root via `fs.promises` + path resolution; reads each file; calls `parseMasterPlan(content, filename)` via dynamic `import()` of `../../tools/progress-tracker/parse.mjs`; returns typed `PlanData[]`. `parse.mjs` untouched. |
| T3.1.3 | 2 | **TECH-202** | Done (archived) | Verify Next.js RSC can call `loadAllPlans()` at build time without bundler errors — confirm dynamic `import()` of `parse.mjs` resolves in Node 20+ ESM context (server component, no `"use client"`); stub `web/app/dashboard/page.tsx` (bare RSC calling `loadAllPlans()` + logging plan count); `validate:all` green. |
| T3.1.4 | 2 | **TECH-203** | Done (archived) | Extend `web/README.md` with §Dashboard section — documents `loadAllPlans()` contract, `PlanData` shape key fields, "parse.mjs is authoritative — plan-loader is read-only wrapper" invariant, and consumption pattern for RSC callers; add inline JSDoc to `plan-loader.ts` with glob-path note + invariant comment. |

#### Stage 3.2 — Dashboard RSC + filters

**Status:** Done (closed 2026-04-15 — TECH-205…TECH-208 archived)

**Objectives:** Ship `/dashboard` RSC consuming `loadAllPlans()`, rendering per-plan task tables via `DataTable`, and wiring `FilterChips` for per-plan / per-status / per-phase filter via URL query params (SSR-only). Apply Q14 obscure-URL gate: route unlinked from public nav, `robots.txt` disallows, "internal" banner displayed.

**Exit:**

- `web/app/dashboard/page.tsx` RSC renders all plans from `loadAllPlans()`; each plan section: title + overall-status `BadgeChip` + `DataTable` with columns `id | phase | issue | status | intent` consuming `plan.allTasks`.
- Step/stage grouping visible via plan heading + `statusDetail`; step heading rows show `HierarchyStatus` badge.
- `FilterChips` for plan / status / phase wired; active state read from `searchParams`; filtering applied server-side before passing rows to `DataTable`.
- "Internal" banner at top of `/dashboard` (full-English user-facing text per caveman-exception).
- `web/app/robots.ts` disallow list extended to include `/dashboard`; route not linked from `web/app/layout.tsx` or any nav component; absent from `web/app/sitemap.ts`.
- `validate:all` green.

**Phases:**

- [x] Phase 1 — RSC core (page + DataTable + plan-loader wiring).
- [x] Phase 2 — Filter chips + access gate.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.2.1 | 1 | **TECH-205** | Done (archived) | Build out `web/app/dashboard/page.tsx` RSC — import `loadAllPlans()`, render per-plan sections; each section: plan title heading + `BadgeChip` for `overallStatus`; `DataTable` consuming `plan.allTasks` w/ typed columns `id | phase | issue | status | intent`; "internal" banner paragraph at page top (full-English caveman-exception text). |
| T3.2.2 | 1 | **TECH-206** | Done (archived) | Add plan-grouped visual hierarchy — step heading rows (`Step {id} — {title}` + `HierarchyStatus` badge via `BadgeChip`) above per-stage task groups; `statusDetail` in muted text; task rows prefixed by `stage.id` so stage breakdown is scannable within each plan's `DataTable`. |
| T3.2.3 | 2 | **TECH-207** | Done (archived) | Wire `FilterChips` for per-plan / per-status / per-phase — read `searchParams: { plan?, status?, phase? }` in RSC; filter `PlanData[]` + task rows server-side before render; chip `<a href>` links emit query-param URLs; active chip state derived from `searchParams` match against chip value; uses existing `FilterChips` `active` prop from Stage 1.2. |
| T3.2.4 | 2 | **TECH-208** | Done (archived) | Apply Q14 access gate — extend `web/app/robots.ts` disallow array to include `/dashboard`; confirm `/dashboard` absent from `web/app/layout.tsx` nav and `web/app/sitemap.ts`; `validate:all` green. |

#### Stage 3.3 — Legacy handoff + validation

**Status:** Done — TECH-213 closed 2026-04-15 (archived); TECH-214 closed 2026-04-15 (archived). Stage 3.3 exit criteria met; Step 3 closed.

**Objectives:** Wire the `docs/progress.html` "Live dashboard" link to the Vercel deploy URL, run end-to-end smoke confirming the dashboard works in production, and author the Decision Log entry for the `docs/progress.html` deprecation trigger.

**Exit:**

- `docs/progress.html` has a visible "Live dashboard →" banner at top linking to `https://web-nine-wheat-35.vercel.app/dashboard`.
- End-to-end smoke: `/dashboard` returns 200 on Vercel deploy; filter chips modify URL params + re-render; "internal" banner visible; Vercel-served `robots.txt` disallows the route.
- §Decision Log section added to this master plan below Orchestration guardrails, documenting the `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles after Step 5 portal auth gate lands).

**Phases:**

- [x] Phase 1 — Legacy link + E2E smoke + deprecation decision log.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.3.1 | 1 | **TECH-213** (archived) | Done | Edit `docs/progress.html` — insert "Live dashboard →" banner `<div>` at top of `<body>` linking to `https://web-nine-wheat-35.vercel.app/dashboard`; minimal inline style consistent with existing page aesthetic (no external CSS added). |
| T3.3.2 | 1 | **TECH-214** (archived) | Done | End-to-end smoke + deprecation decision log — manually confirm Vercel `/dashboard` returns 200, filter chips functional, "internal" banner visible, `robots.txt` disallows route; append §Decision Log section to this master plan below Orchestration guardrails documenting `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles post Step 5 portal-auth gate). |

---

### Step 4 — Dashboard improvements + UI polish

**Status:** Done (Stages 4.1 + 4.2 + 4.3 + 4.4 all closed 2026-04-16)

**Backlog state (Step 4):** 16 filed + archived (TECH-223…TECH-226, TECH-231…TECH-234, TECH-239…TECH-242, TECH-247…TECH-250)

**Objectives:** Improve `/dashboard` and the overall web app experience with richer navigation, standardized UI primitives, data visualization, and multi-select filtering. Ship an app-wide collapsible sidebar wired into the root layout; integrate an icon library; add a `Button` primitive aligned to design tokens; extend `DataTable` with percentage-column support and show per-plan completion stats on the dashboard; land D3.js-driven charts (status breakdown) as client components; upgrade `FilterChips` + dashboard filter logic to support multiple choices per dimension simultaneously. No production deploy during development — each stage closeout triggers deploy.

**Exit criteria:**

- `web/components/Sidebar.tsx` (new) — collapsible sidebar with icon + label links to `/ | /wiki | /devlog | /dashboard`; wired into `web/app/layout.tsx`; responsive (collapsed on mobile, expanded on ≥md); uses icon library per route.
- Icon library installed (`lucide-react` preferred — tree-shakeable, MIT); icons used across sidebar, button, badge; no raw emoji icons in components.
- `web/components/Button.tsx` (new) — `variant: 'primary' | 'secondary' | 'ghost'`; `size: 'sm' | 'md' | 'lg'`; design tokens (`bg-accent-*`, `text-*`) — no inline hex; replaces ad-hoc `<button>` elements in existing pages where present.
- Dashboard shows per-plan completion percentage (tasks with `status === 'Done (archived)'` / total tasks) and per-step completion percentage; both derived from `PlanData` — `parse.mjs` + `plan-loader.ts` untouched.
- `web/components/PlanChart.tsx` (new) — D3.js client component; renders at minimum a status-breakdown bar chart per plan (grouped bars: pending / in-progress / done counts by step or stage); design tokens for fills; exported via `dynamic()` with `{ ssr: false }` to avoid hydration errors.
- Dashboard filter chips support multi-select per dimension: query string accepts repeated params (`?status=Draft&status=In+Progress`) or comma-delimited (`?status=Draft,In+Progress`); active state per-value; de-select one value → only that value removed; "clear all filters" control resets to bare `/dashboard`.
- `validate:all` green at each stage close; production deploy on stage closeout (not mid-stage).

**Art:** None. Charts use design-token palette; no illustrator assets required.

**Relevant surfaces (load when step opens):**
- Step 3 outputs: `web/app/dashboard/page.tsx`, `web/lib/plan-loader.ts`, `web/lib/plan-loader-types.ts` — modified or extended in Stages 4.2, 4.3, 4.4.
- Step 1 outputs: `web/components/{DataTable,FilterChips,BadgeChip,StatBar}.tsx`, `web/lib/tokens/*.json`, `web/tailwind.config.ts` — `DataTable` + `FilterChips` extended; tokens consumed.
- `web/app/layout.tsx` — `Sidebar` wired here (Stage 4.1).
- `web/package.json` — `lucide-react`, `d3`, `@types/d3` added.
- `docs/web-platform-exploration.md` §Implementation Points — UI density, navigation context.
- D3.js pattern in Next.js: `'use client'` component + `dynamic()` with `{ ssr: false }` wrapper; no SSR DOM manipulation.
- Invariants: `ia/rules/invariants.md` #1–#12 NOT implicated — web platform only.

#### Stage 4.1 — Navigation sidebar + icon system

**Status:** Done (TECH-223…TECH-226 all closed 2026-04-16)

**Objectives:** Install `lucide-react`; author `Sidebar` client component with icon + label links to all top-level routes; add active route highlighting via `usePathname`; implement responsive behavior (collapsed on mobile via slide/overlay, always expanded on ≥md); wire into root layout.

**Exit:**

- `lucide-react` added to `web/package.json` deps; `web/components/Sidebar.tsx` (new) renders vertical nav list with icon + label per route (`Home`, `BookOpen`, `Newspaper`, `LayoutDashboard` icons); design token classes only (no inline hex).
- Active route link styled with `text-accent`/`bg-panel` via `usePathname()`; mobile hamburger toggle (`Menu`/`X`) collapses/expands sidebar via `useState`; `'use client'` component.
- `web/app/layout.tsx` restructured as `flex min-h-screen` row; `<Sidebar />` in left slot; `<main className="flex-1 min-w-0">` wraps `{children}`; sidebar `hidden md:flex` on desktop, overlay on mobile.
- `validate:all` green; `web/README.md §Components` Sidebar entry added (lucide dep, `'use client'` rationale, active state pattern).

**Phases:**

- [x] Phase 1 — Sidebar component (markup + icons + active state + mobile toggle).
- [x] Phase 2 — Layout integration + docs + validation.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T4.1.1 | 1 | **TECH-223** | Done (archived) | Install `lucide-react` into `web/package.json`; author `web/components/Sidebar.tsx` base — `<nav>` with vertical `<Link>` list per route (`/` → `Home`, `/wiki` → `BookOpen`, `/devlog` → `Newspaper`, `/dashboard` → `LayoutDashboard`); each link: icon (24px) + label text; design token classes (`bg-canvas`, `text-primary`, `text-muted`, hover `text-primary`); SSR-compatible static markup; no active state yet. |
| T4.1.2 | 1 | **TECH-224** | Done (archived) | Convert `web/components/Sidebar.tsx` to `'use client'`; add `usePathname()` active highlight (`text-accent-warn bg-panel rounded` on matching href — token corrected during kickoff, no plain `text-accent` alias exists in palette); add mobile collapse toggle — `useState(false)` `open` bool; `Menu`/`X` icon button visible on `<md`; collapsed: sidebar `-translate-x-full` (kept in DOM for transition); expanded: fixed overlay with `z-50 bg-canvas` on mobile; transitions via Tailwind `transition-transform`. |
| T4.1.3 | 2 | **TECH-225** | Done (archived) | Wire `<Sidebar />` into `web/app/layout.tsx` — preserve existing `<html>` Geist font vars + `h-full antialiased`, `<body className="min-h-full flex flex-col">`, footer (Devlog + RSS), metadata export; insert inner horizontal row `<div className="flex flex-1 min-h-0"><Sidebar /><main className="flex-1 min-w-0 overflow-auto">{children}</main></div>` as first `<body>` child; render `<Sidebar />` directly (no `hidden md:flex` wrapper — Sidebar root `<nav>` already owns `fixed ... md:static md:translate-x-0 w-48`); smoke `/`, `/wiki`, `/devlog`, `/dashboard`, `/design`, `/about`, `/install`, `/history` for no horizontal scroll, footer pinned below row, TECH-224 mobile overlay intact. |
| T4.1.4 | 2 | **TECH-226** | Done (archived) | Add `web/README.md §Components` Sidebar entry — documents lucide-react dependency, `'use client'` rationale (`usePathname` + `useState`), mobile overlay pattern, and desktop `md:static md:translate-x-0` strategy (same-element, not `hidden md:flex` wrapper); active-route token = `text-accent-warn`; token consumption via inline `style` + `tokens.colors[...]` JS map (not Tailwind utility classes); `validate:all` green; confirm no TypeScript errors from lucide-react imports. |

#### Stage 4.2 — UI primitives polish + dashboard percentages

**Status:** Done (TECH-231 + TECH-232 + TECH-233 + TECH-234 archived 2026-04-16)

**Objectives:** Author `Button` primitive with variant + size props consuming design tokens; extend `DataTable` with optional `pctColumn` prop rendering `StatBar` inline; compute and display per-plan and per-step completion percentages on the dashboard derived from `PlanData`; `plan-loader.ts` + `plan-loader-types.ts` untouched.

**Exit:**

- `web/components/Button.tsx` (new) — `variant: 'primary' | 'secondary' | 'ghost'`; `size: 'sm' | 'md' | 'lg'`; polymorphic (`<button>` default, `<a>` when `href` present); design token classes; `disabled` state; exports `ButtonProps`.
- `web/components/DataTable.tsx` extended — optional `pctColumn?: { dataKey: keyof T; label?: string; max?: number }` prop; renders `StatBar` inline for named key; existing column contract unchanged.
- Dashboard renders per-plan `StatBar` (done / total tasks) in plan section heading and per-step compact `StatBar` rows below each step heading; both computed from `PlanData.allTasks` — `plan-loader.ts` untouched.
- `web/README.md §Components` Button + DataTable `pctColumn` entries added; `validate:all` green.

**Phases:**

- [x] Phase 1 — Button + DataTable pctColumn primitives.
- [x] Phase 2 — Dashboard percentage rendering + docs.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T4.2.1 | 1 | **TECH-231** | Done (archived) | Author `web/components/Button.tsx` (new) — polymorphic: renders `<button>` (default) or `<a>` when `href` prop present; `variant: 'primary' \| 'secondary' \| 'ghost'` mapped to corrected token utility classes (`bg-bg-status-progress text-text-status-progress-fg` / `bg-bg-panel text-text-primary border border-text-muted/40` / `bg-transparent text-text-muted hover:text-text-primary` — phantom `accent-info` / `border-border` names from spec draft replaced during kickoff with real `globals.css @theme` aliases); `size: 'sm' \| 'md' \| 'lg'` mapped to `px-/py-/text-` scale; `disabled` → `opacity-50 cursor-not-allowed pointer-events-none`; named-exports `Button` + `ButtonProps`. |
| T4.2.2 | 1 | **TECH-232** | Done (archived) | Extend `web/components/DataTable.tsx` — add optional `pctColumn?: { dataKey: keyof T; label?: string; max?: number }` prop; when provided, appends an extra column rendering `<StatBar value={(row[dataKey] as number) / (pctColumn.max ?? 100) * 100} />` with `label ?? 'Progress'` header; all existing column definitions, generic types, and sort contract unchanged; import `StatBar` from `./StatBar`. |
| T4.2.3 | 2 | **TECH-233** | Done (archived) | Add per-plan completion `StatBar` to `web/app/dashboard/page.tsx` — for each plan, compute `completedCount` (`allTasks.filter(t => DONE_STATUSES.has(t.status)).length`, `DONE_STATUSES = {'Done (archived)', 'Done'}`) + `totalCount`; render `<StatBar label="{completedCount} / {totalCount} done" value={completedCount} max={totalCount} />` in plan section heading row next to `BadgeChip`; `plan-loader.ts` + `plan-loader-types.ts` untouched. |
| T4.2.4 | 2 | **TECH-234** | Done (archived) | Add per-step completion stats to dashboard — for each `step` in `plan.steps`, derive step tasks from `plan.allTasks.filter(t => t.id.startsWith('T' + step.id + '.'))` (done / total); render compact `<StatBar>` row below each step heading; add `web/README.md §Components` Button + DataTable `pctColumn` entries; `validate:all` green. |

#### Stage 4.3 — D3.js data visualization

**Status:** Done — TECH-239 + TECH-240 + TECH-241 + TECH-242 all closed 2026-04-16 (archived)

**Objectives:** Install `d3` + `@types/d3`; author `PlanChart` `'use client'` component with grouped-bar status-breakdown chart per plan; wire `dynamic()` with `{ ssr: false }` to avoid hydration errors; integrate into dashboard page with data aggregation from `PlanData`; validate no SSR build errors.

**Exit:**

- `d3` + `@types/d3` added to `web/package.json`.
- `web/components/PlanChart.tsx` (new) — `'use client'` SVG chart; D3 `scaleBand` + `scaleLinear` + `axisBottom` + `axisLeft`; grouped bars (pending / in-progress / done per step); fills via `var(--color-*)` CSS vars; axis labels + color legend; empty-state `<p>` when 0 tasks.
- Dashboard page imports `PlanChart` via `next/dynamic({ ssr: false })`; one chart per plan with loading skeleton; no SSR / hydration errors in `next build` output.
- `web/README.md §Components` PlanChart entry added; `validate:all` green.

**Phases:**

- [x] Phase 1 — D3 install + PlanChart component (chart + axes + legend).
- [x] Phase 2 — Dashboard integration + ssr-bypass + validation.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T4.3.1 | 1 | **TECH-239** | Done (archived) | Install `d3` + `@types/d3` into `web/package.json`; author `web/components/PlanChart.tsx` (new) — `'use client'`; props `{ data: { label: string; pending: number; inProgress: number; done: number }[] }`; `useRef<SVGSVGElement>` + `useEffect` for D3 draw; `scaleBand` (step labels) + `scaleLinear` (count); 3 grouped bars per step using nested `scaleBand`; fills via `var(--color-bg-status-pending)` / `var(--color-bg-status-progress)` / `var(--color-bg-status-done)` real `@theme` aliases; static `480×220` SVG; empty-state `<p>` when `data.length === 0`. |
| T4.3.2 | 1 | **TECH-240** | Done (archived) | Extend `PlanChart.tsx` — add `axisBottom` (step label ticks, truncated at 12 chars via `.text(d => d.length > 12 ? d.slice(0,11) + '…' : d)`); `axisLeft` (count integer ticks, `tickFormat(d3.format('d'))`); inline SVG `<text>` legend (3 color swatches + "Pending / In Progress / Done" labels); handle 0-task plan (data array empty → render placeholder `<p className="text-text-muted text-sm">No tasks</p>` instead of SVG). |
| T4.3.3 | 2 | **TECH-241** | Done (archived) | Integrate `PlanChart` into `web/app/dashboard/page.tsx` — `const PlanChart = dynamic(() => import('@/components/PlanChart'), { ssr: false, loading: () => <div className="h-[220px] bg-bg-panel animate-pulse rounded" /> })`; for each plan derive chart data: `plan.steps.map(step => ({ label: step.title, pending: plan.allTasks.filter(t => t.id.startsWith('T' + step.id + '.') && t.status === '_pending_').length, inProgress: …'In Progress'…, done: …'Done (archived)'… }))`; render one `<PlanChart data={chartData} />` per plan below its `DataTable`. |
| T4.3.4 | 2 | **TECH-242** | Done (archived) | Smoke chart in dev + build — run `cd web && npm run build`; confirm zero `ReferenceError: window is not defined` or `document` SSR errors in build output; `validate:all` green; add `web/README.md §Components` PlanChart entry (dynamic import pattern, `ssr: false` rationale, data aggregation shape, fill CSS var names). |

#### Stage 4.4 — Multi-select dashboard filtering

**Status:** Done (TECH-247 + TECH-248 + TECH-249 + TECH-250 archived 2026-04-16)

**Objectives:** Upgrade `FilterChips` with per-chip `href` override for multi-select callers; author `web/lib/dashboard/filter-params.ts` URL helpers (`toggleFilterParam`, `parseFilterValues`); update dashboard `searchParams` parsing to multi-value arrays (OR within dimension, AND across); add per-value de-select and "clear all filters" ghost `Button` control.

**Exit:**

- `web/components/FilterChips.tsx` extended — `Chip` interface gains optional `href?: string` (explicit URL override); `active?: boolean` per-chip; backward-compatible (chips without `href` unchanged).
- `web/lib/dashboard/filter-params.ts` (new) — exports `toggleFilterParam(search, key, value): string`; `parseFilterValues(params, key): string[]` (handles comma-delimited + repeated params); `clearFiltersHref` constant `'/dashboard'`.
- Dashboard `searchParams` parsed via `parseFilterValues`; `PlanData[]` + `TaskRow[]` filtered server-side (OR within dimension, AND across); each chip `href` from `toggleFilterParam`.
- "Clear filters" ghost `Button` visible when `searchParams` non-empty; full-English "Clear filters" text (caveman-exception); `validate:all` green.

**Phases:**

- [x] Phase 1 — FilterChips extension + URL helper module.
- [x] Phase 2 — Dashboard wiring + clear-filters control + validation.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T4.4.1 | 1 | **TECH-247** | Done (archived) | Extend `web/components/FilterChips.tsx` — update `Chip` interface: add `href?: string` (when present, chip renders `<a href={href}>` directly instead of computing href internally); `active?: boolean` stays per-chip; remove any assumption of exactly one active chip; existing single-select callers (no `href` in chips) fall back to `href="#"` — backward-compatible; no `'use client'` conversion needed (chips are purely declarative). |
| T4.4.2 | 1 | **TECH-248** | Done (archived) | Author `web/lib/dashboard/filter-params.ts` (new) — exports: `parseFilterValues(params: URLSearchParams \| ReadonlyURLSearchParams, key: string): string[]` — splits comma-delimited value + collects repeated params, deduplicates, returns sorted array; `toggleFilterParam(currentSearch: string, key: string, value: string): string` — parses `currentSearch` into `URLSearchParams`, adds `value` if absent or removes if present (comma-delimited representation), returns new query string; `clearFiltersHref = '/dashboard'` constant. |
| T4.4.3 | 2 | **TECH-249** | Done (archived) | Update `web/app/dashboard/page.tsx` `searchParams` parsing — replace single-value reads with `parseFilterValues(new URLSearchParams(searchParams as Record<string, string>), 'plan')` etc. for each dimension; filter `PlanData[]` (OR within dimension, AND across): `plan` filter on `plan.title`, `status` filter on `task.status`, `phase` filter on `task.phase`; pass per-chip `href` from `toggleFilterParam(new URLSearchParams(searchParams as Record<string, string>).toString(), key, chipValue)` to `FilterChips` chips array. |
| T4.4.4 | 2 | **TECH-250** | Done (archived) | Add "clear all filters" control to dashboard page — conditionally render `<Button variant="ghost" href="/dashboard">Clear filters</Button>` (full-English caveman-exception) when `Object.values(searchParams ?? {}).some(Boolean)`; smoke multi-select: `?status=Draft,In+Progress` narrows rows; each chip individually de-selectable; single-chip round-trip `toggleFilterParam` adds then removes cleanly; `validate:all` green. |

---

### Step 5 — Portal foundations (architecture-only at this tier)

**Status:** Final

**Backlog state (Step 5):** Stage 5.1 closed 2026-04-16 — TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived; Stage 5.2 closed 2026-04-17 — TECH-261 + TECH-262 + TECH-263 + TECH-264 all archived; Stage 5.3 closed 2026-04-17 — TECH-269 + TECH-265 + TECH-266 + TECH-267 + TECH-268 all archived

**Objectives:** Land the user-portal foundations — free-tier Postgres provider selected (Neon / Supabase free / Vercel Postgres Hobby — evaluate limits against expected volume); auth stack picked (roll-own JWT + sessions per Q11; confirm vs. Lucia-Auth-style minimal library before committing); stub `app/api/auth/*` route handlers with no user-facing flow; schema drafted for `user` / `session` / `save` / `entitlement` tables but NOT yet migrated. Dashboard migrates from obscure-URL gate to auth middleware once session handling works end-to-end. Payment gateway remains deferred (Q10 undecided) — architecture slot reserved, no provider wiring at this tier. This step intentionally stays architecture-only; user-facing portal UX ships in a follow-up master plan after this step's foundations lock.

**Exit criteria:**

- Free-tier Postgres provider selected; `web/lib/db/` wraps a single connection pool; `DATABASE_URL` env wired into Vercel project env vars.
- Auth library decision locked in Decision Log; `web/app/api/auth/login`, `register`, `session`, `logout` route handlers present (stub bodies, return 501 Not Implemented until follow-up plan).
- Schema draft under `web/lib/db/schema.ts` covers `user`, `session`, `save`, `entitlement`; migration tool chosen (drizzle-kit or prisma migrate) but migrations NOT yet run.
- Dashboard `/dashboard` now behind an auth middleware check (obscure-URL gate removed); unauthenticated users get redirect to a stub login page; stub login returns a canned error at this tier.
- Payment gateway architecture slot documented in `web/README.md` §Portal as a placeholder, no provider chosen.

**Art:** None. Architecture-only step — no illustrator assets.

**Relevant surfaces (load when step opens):**
- Step 3 outputs: `web/app/dashboard/page.tsx` (internal banner + obscure-URL gate — both removed in Stage 5.3), `web/app/robots.ts` (disallow extended — modified in Stage 5.3), `web/lib/plan-loader.ts`, `web/app/sitemap.ts` — consumed, not modified except where noted.
- `docs/web-platform-exploration.md` §Implementation Points W7 (portal auth + DB).
- `web/lib/db/client.ts` (new), `web/lib/db/schema.ts` (new), `drizzle.config.ts` (new).
- `web/app/api/auth/{login,register,session,logout}/route.ts` (new).
- `web/app/auth/login/page.tsx` (new), `web/middleware.ts` (new).
- Invariants: `ia/rules/invariants.md` #1–#12 NOT implicated — web platform only.

#### Stage 5.1 — Postgres provider + auth library selection

**Status:** Done (TECH-252 + TECH-253 + TECH-254 + TECH-255 all archived 2026-04-16)

**Objectives:** Evaluate and select free-tier Postgres provider (Neon / Supabase free / Vercel Postgres Hobby) against MVP volume; lock auth library decision (Lucia Auth v3 vs. roll-own JWT vs. Auth.js — confirm Q11). Lock both decisions in Decision Log. Scaffold `web/lib/db/client.ts` connection pool wrapper + wire `DATABASE_URL` into Vercel env vars. Document in `web/README.md §Portal`.

**Exit:**

- Free-tier Postgres provider locked in Decision Log: provider name, connection/storage limits, region, rationale vs. alternatives.
- Auth library locked in Decision Log: confirm or update Q11 "roll-own JWT + sessions"; Lucia Auth v3 evaluated as minimal alternative before committing to pure roll-own.
- `web/lib/db/client.ts` (new) exports connection pool via `DATABASE_URL`; lazy-connects (no open at build time).
- `DATABASE_URL` env var wired into Vercel project (production + preview + development environments).
- `web/README.md §Portal` documents provider choice, connection pool pattern, `DATABASE_URL` contract, payment gateway placeholder.

**Phases:**

- [x] Phase 1 — Provider + auth library evaluation + Decision Log entries.
- [x] Phase 2 — Connection pool scaffold + env wiring + README §Portal.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.1.1 | 1 | **TECH-252** | Done (archived) | Evaluate Neon free / Supabase free / Vercel Postgres Hobby — compare connection limits, storage caps, regions, Next.js/Node driver compatibility; lock chosen provider in Decision Log with limits table + rationale vs. alternatives. No code — Decision Log entry only. |
| T5.1.2 | 1 | **TECH-253** | Done (archived) | Evaluate + lock auth library — compare Lucia Auth v3 (minimal, session-first) / pure roll-own JWT / Auth.js (heavy); confirm or update Q11 "roll-own JWT + sessions" decision; lock in Decision Log with API surface note + rationale. No code — Decision Log entry only. |
| T5.1.3 | 2 | **TECH-254** | Done (archived) | Install chosen Postgres driver into `web/package.json`; author `web/lib/db/client.ts` (new) — exports `db` or `sql` connection pool via `DATABASE_URL` (lazy-connect, no open at build time); wire `DATABASE_URL` into Vercel project env vars (production + preview + development) via Vercel dashboard or `vercel env add`. |
| T5.1.4 | 2 | **TECH-255** | Done (archived) | Extend `web/README.md` with `§Portal` section — documents provider choice, connection pool pattern, `DATABASE_URL` env contract, payment gateway architecture placeholder (no provider chosen), and "Step 5 is architecture-only — no migrations run" boundary note; `validate:all` green. |

#### Stage 5.2 — Auth API stubs + schema draft

**Status:** Done — TECH-261 + TECH-262 + TECH-263 + TECH-264 all archived 2026-04-17.

**Objectives:** Draft `web/lib/db/schema.ts` covering `user`, `session`, `save`, `entitlement` tables using drizzle-kit (preferred). Install + configure migration tooling; confirm `db:generate` script works. Author stub `web/app/api/auth/{login,register,session,logout}/route.ts` handlers returning 501 Not Implemented. No migrations run.

**Exit:**

- `web/lib/db/schema.ts` (new) defines typed drizzle `pgTable` definitions for `user`, `session`, `save`, `entitlement` tables with column types matching auth library data contract from Stage 5.1.
- `drizzle.config.ts` (new) at `web/` root; `web/package.json` has `db:generate` script; `npm run db:generate` produces artifacts in `web/drizzle/`; migrations NOT run.
- `web/app/api/auth/login/route.ts`, `register/route.ts` (new) — `POST` handlers each return `Response.json({ error: 'Not Implemented' }, { status: 501 })`.
- `web/app/api/auth/session/route.ts` (`GET`), `logout/route.ts` (`POST`) (new) — each returns 501; all four routes absent from `web/app/sitemap.ts`.
- `validate:all` green; no TypeScript errors from schema imports.

**Phases:**

- [ ] Phase 1 — Schema + migration tool setup.
- [ ] Phase 2 — Auth API stub handlers.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.2.1 | 1 | **TECH-261** | Done (archived) | Install `drizzle-orm` + `drizzle-kit` into `web/package.json`; author `web/lib/db/schema.ts` (new) — drizzle `pgTable` for: `user` (id uuid PK, email text unique, passwordHash text, createdAt timestamp), `session` (id uuid PK, userId uuid FK→user.id, expiresAt timestamp, token text), `save` (id uuid PK, userId uuid FK→user.id, data jsonb, updatedAt timestamp), `entitlement` (id uuid PK, userId uuid FK→user.id, tier text, grantedAt timestamp). |
| T5.2.2 | 1 | **TECH-262** | Done (archived) | Author `web/drizzle.config.ts` (new) — `schema: './lib/db/schema.ts'`, `out: './drizzle/'`, driver from `DATABASE_URL`; add `"db:generate": "drizzle-kit generate"` to `web/package.json` scripts; confirm `npm run db:generate` produces SQL artifacts in `web/drizzle/` without live DB; decide + document whether `web/drizzle/` is gitignored or committed; `validate:all` green. |
| T5.2.3 | 2 | **TECH-263** | Done (archived) | Author `web/app/api/auth/login/route.ts` + `web/app/api/auth/register/route.ts` (new) — each exports `export async function POST(_req: Request)` returning `Response.json({ error: 'Not Implemented' }, { status: 501 })`; TypeScript typed; no DB imports yet. |
| T5.2.4 | 2 | **TECH-264** | Done (archived) | Author `web/app/api/auth/session/route.ts` (`GET`) + `web/app/api/auth/logout/route.ts` (`POST`) (new) — each returns 501 Not Implemented; confirm all four `/api/auth/*` routes absent from `web/app/sitemap.ts` (API routes not enumerated); `validate:all` green. |

#### Stage 5.3 — Dashboard auth middleware migration

**Status:** Done — Stage 5.3 closed 2026-04-17. Phase 0 (TECH-269), Phase 1 (TECH-265 + TECH-266), Phase 2 (TECH-267 + TECH-268) all archived. Next.js 16 migration note: `web/middleware.ts` → `web/proxy.ts` (rename surfaced during TECH-268 smoke; see Issues Found).

**Objectives:** Replace obscure-URL gate on `/dashboard` with Next.js Middleware auth check. Unauthenticated requests → redirect to stub `/auth/login`. Author stub login page (full-English UI, caveman-exception). Remove "internal" banner from dashboard. Update `robots.ts`.

**Exit:**

- `web/middleware.ts` (new) — matcher `['/dashboard']`; reads session cookie; absent/invalid → `NextResponse.redirect(new URL('/auth/login', request.url))`; present → `NextResponse.next()`.
- `web/app/auth/login/page.tsx` (new) — stub RSC; full-English copy (caveman-exception): "Sign in" heading, email + password placeholder inputs, disabled submit, canned banner "Authentication not yet available — coming soon."; design token classes (no inline hex).
- `web/app/robots.ts` updated — `/dashboard` removed from disallow; `/auth` added to disallow.
- "Internal" banner removed from `web/app/dashboard/page.tsx`; manual smoke: `/dashboard` without session cookie → 302 to `/auth/login`.
- `validate:all` green.

**Phases:**

- [x] Phase 0 — Dev-bypass env scaffolding (prerequisite to Phase 1 middleware gate).
- [x] Phase 1 — Middleware + stub login page.
- [x] Phase 2 — robots.ts update + banner removal + smoke.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.3.0 | 0 | **TECH-269** | Done (archived) | Prerequisite to **TECH-265**. Create `web/.env.local` (gitignored) containing `DASHBOARD_AUTH_SKIP=1` + `web/.env.local.example` (committed) w/ comment documenting bypass knob + prod-warning. Amend `web/README.md` — new `## Local development auth bypass` section. Amend TECH-265 spec (archived) §2.1 Goals + §5.3 algorithm notes — middleware reads `process.env.DASHBOARD_AUTH_SKIP` before cookie; `=== '1'` → `NextResponse.next()` immediately. Ensures local devs not locked out of `/dashboard` once cookie gate lands. Vercel env vars MUST NOT set `DASHBOARD_AUTH_SKIP` — prod stays gated. |
| T5.3.1 | 1 | **TECH-265** | Done (archived) | Author `web/middleware.ts` (new) — `config = { matcher: ['/dashboard'] }`; reads session cookie by name from `request.cookies.get(SESSION_COOKIE_NAME)`; if missing/empty → `NextResponse.redirect(new URL('/auth/login', request.url))`; if present → `NextResponse.next()`. Cookie name constant matches auth library decision from Stage 5.1. No DB lookup at this tier. Middleware now also short-circuits on `process.env.DASHBOARD_AUTH_SKIP === '1'` (**TECH-269** bypass knob) — local dev only. |
| T5.3.2 | 1 | **TECH-266** | Done (archived) | Author `web/app/auth/login/page.tsx` (new) — RSC stub login page; full-English user-facing copy (caveman-exception): "Sign in" heading, email + password `<input>` placeholders, disabled `<button>` submit, canned error `<p>` "Authentication not yet available — coming soon."; consumes design token classes (`bg-canvas`, `text-primary`, etc. — no inline hex). |
| T5.3.3 | 2 | **TECH-267** | Done (archived) | Update `web/app/robots.ts` — remove `/dashboard` from disallow array; add `/auth` to disallow (login page not publicly indexed); confirm `/auth/login` absent from `web/app/sitemap.ts`; `validate:all` green. |
| T5.3.4 | 2 | **TECH-268** | Done (archived) | Remove "Internal" banner `<p>` from `web/app/dashboard/page.tsx`; smoke note: `localhost:4000/dashboard` without session cookie → middleware should 302 to `/auth/login`; confirm middleware matcher fires in Next.js dev server; `validate:all` green. |

---

### Step 6 — Playwright E2E harness

**Status:** Final

**Objectives:** Install and configure Playwright as the automated e2e layer for the `web/` workspace; integrate into `npm run validate:all` CI chain; land baseline route coverage for all existing public surfaces; then add dashboard-specific e2e for SSR query-param filter flows. Step 5 portal auth-flow tests extend this harness as a Stage 5.X — the harness ships here so portal work inherits it without bootstrapping from scratch.

**Exit criteria:**

- `web/playwright.config.ts` present; `npm run test:e2e` (in `web/`) runs the full suite headless; exit code propagates to root `validate:all` via `npm --prefix web run test:e2e` composition.
- CI-ready: `PLAYWRIGHT_BASE_URL` env var injected per environment (local `localhost:4000`, Vercel preview URL via `VERCEL_URL`); `npx playwright install --with-deps chromium` in CI bootstrap step.
- Baseline suite passes green on `main`: public routes (landing, `/about`, `/install`, `/history`, `/devlog`, `/wiki`) return 200; `robots.txt` disallows `/dashboard`; sitemap enumerates at least one devlog slug; RSS `Content-Type` header correct.
- Dashboard e2e: filter chip `?plan=` / `?status=` / `?phase=` round-trip — URL param → server render → visible chip active state; multi-param combination; clear-filters link resets to unfiltered; empty-result message renders when no tasks match.
- All tests authored in TypeScript via `@playwright/test`; no Puppeteer or Cypress deps added.
- `web/README.md` §E2E documents local run, CI env var wiring, and how to add tests for new routes.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `web/playwright.config.ts` (new)
- `web/tests/` (new — e2e suite directory)
- `web/package.json` — add `test:e2e` + `test:e2e:ci` scripts
- `package.json` (root) — extend `validate:all` chain
- `web/app/dashboard/page.tsx` — filter param contract under test
- `web/components/FilterChips.tsx` — active prop + href contract under test
- `docs/agent-led-verification-policy.md` — determine whether `test:e2e` slots into Path A or remains a separate gate

#### Stage 6.1 — Install + config + CI wiring

**Status:** Done (closed 2026-04-17 — TECH-276 archived)

**Objectives:** Install `@playwright/test`; author `web/playwright.config.ts` (baseURL from env, headless Chromium, 1 worker in CI); add `test:e2e` + `test:e2e:ci` scripts to `web/package.json`; wire into root `validate:all` (opt-in flag or separate `validate:e2e` target to avoid mandatory browser install in non-e2e CI contexts); document env var contract in `web/README.md`.

**Exit:**
- `cd web && npm run test:e2e` runs (even with 0 test files) without error.
- Root `npm run validate:e2e` composes `web/` e2e run; existing `validate:all` unchanged (no forced browser install).
- `web/README.md` §E2E section present.

**Phases:** Merged into single task per 2026-04-17 Decision Log (pure setup boilerplate, ≤5 files, single verify gate).
- [x] Phase 1 — Install + config + scripts + README §E2E (TECH-276).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T6.1.1 | 1 | **TECH-276** | Done (archived) | Install `@playwright/test` + author `web/playwright.config.ts` (baseURL from env, headless Chromium, `testDir: './tests'`, `outputDir: './playwright-report'`); stub `web/tests/.gitkeep`; add `test:e2e` + `test:e2e:ci` scripts to `web/package.json`; add `validate:e2e` to root `package.json`; add `web/playwright-report/` to `.gitignore`; author `web/README.md` §E2E (local run, `PLAYWRIGHT_BASE_URL` contract, Vercel preview injection, CI bootstrap `npx playwright install --with-deps chromium`, per-route convention). `validate:all` unchanged. |

---

#### Stage 6.2 — Baseline route coverage

**Status:** Done (closed 2026-04-17 — TECH-277 archived)

**Objectives:** Author e2e tests for all existing public surfaces. Validates that routes return 200, key content landmarks are present, `robots.txt` disallows `/dashboard`, sitemap enumerates slugs, RSS `Content-Type` correct. No auth-gated routes at this stage.

**Exit:**
- `npm run test:e2e` green against `localhost:4000` (dev server) + headless Chromium.
- Tests cover: landing, `/about`, `/install`, `/history`, `/wiki`, `/devlog` (list + at least one slug), `robots.txt` body, `/sitemap.xml` slug presence, `/feed.xml` Content-Type.

**Phases:** Merged into single task per 2026-04-17 Decision Log (2 test-only spec files, no prod code changes, single verify gate — Stage 6.1 merge precedent).
- [x] Phase 1 — Both specs authored + e2e green (TECH-277).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T6.2.1 | 1 | **TECH-277** | Done (archived) | Author `web/tests/routes.spec.ts` — assert HTTP 200 + at least one visible heading for: `/`, `/about`, `/install`, `/history`, `/wiki`, `/devlog`; assert first devlog slug link navigates to a 200 page. |
| T6.2.2 | 1 | **TECH-277** | Done (archived) | Author `web/tests/meta.spec.ts` — assert `robots.txt` body contains `Disallow: /dashboard`; assert `/sitemap.xml` contains at least one devlog URL; assert `GET /feed.xml` response `Content-Type` header matches `application/rss+xml`. |

---

#### Stage 6.3 — Dashboard e2e (SSR filter flows)

**Status:** Done (closed 2026-04-17 — TECH-284 archived)

**Objectives:** Author e2e tests for the dashboard's SSR query-param filter chip flows. Validates the full round-trip: URL param → server render → active chip state → filtered task rows → clear-filters reset. Covers combinations and empty-state.

**Exit:**
- Dashboard filter chip tests green headless; `?plan=` / `?status=` / `?phase=` each produce active chip + filtered rows; multi-param combination narrows correctly; clear-filters `<a>` resets to unfiltered state; unrecognised param value renders empty-state message.

**Phases:**
- [x] Phase 1 — Full dashboard filter spec (single-param + multi-param + clear-filters + empty-state).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T6.3.1 | 1 | **TECH-284** | Done (archived) | Author `web/tests/dashboard-filters.spec.ts` — (a) for each of `plan`, `status`, `phase` params: navigate to `/dashboard?{param}={value}` w/ known value from unfiltered render; assert chip w/ matching label has active visual state (class or aria); assert visible row count < unfiltered. (b) multi-param (`?status=Done&phase=1`): assert rows satisfy both filters. (c) clear-filters: assert `<a href="/dashboard">` present when any param active; following it returns unfiltered row count. (d) unknown-value (`?status=nonexistent`): assert empty-state message text present. |

---

### Step 7 — Release-scoped progress view

**Status:** Final — Stage 7.1 Final (TECH-339..TECH-342 archived 2026-04-18); Stage 7.2 Final (TECH-351..TECH-354 archived 2026-04-18); Stage 7.3 Final (TECH-358..TECH-361 archived 2026-04-18)

**Backlog state (Step 7):** 0 filed, 8 closed

**Objectives:** Ship `/dashboard/releases` release picker and `/dashboard/releases/:releaseId/progress` SSR expandable plan tree with chevron-toggle expand/collapse; hand-maintained release registry (`web/lib/releases.ts`) seeded with `full-game-mvp` row; backend-derived default-expand predicate (first non-done step by task counts); `PlanTree` Client component as the only hydration island on this surface. Extends `web/proxy.ts` auth matcher to cover `/dashboard/:path*` and adds a "Releases" nav link to `Sidebar.tsx`. Reuses existing `PlanData` / `PlanMetrics` types, `loadAllPlans`, `computePlanMetrics`, and `BadgeChip` status tokens — zero new parser logic, zero new paid services.

**Exit criteria:**

- `web/lib/releases.ts` exports `Release` interface + `resolveRelease(id)` + seeded `full-game-mvp` row; header comment cites `ia/projects/full-game-mvp-rollout-tracker.md` as source of truth for `children[]` drift.
- `web/lib/releases/resolve.ts`: `getReleasePlans(release, allPlans)` pure filter; silently drops missing-on-disk children.
- `web/lib/releases/default-expand.ts`: `deriveDefaultExpandedStepId(plan, metrics)` returns first non-done step id or `null` if all done; ignores step.status prose (tasks are ground truth); JSDoc documents this.
- `web/lib/plan-tree.ts`: `buildPlanTree(plan, metrics)` synthesizes `TreeNodeData` tree; phase nodes derived from `task.phase` groupBy (NOT conflated with `Stage.phases` checklist); JSDoc NB1.
- Unit tests for all four pure shapers pass; `npm run validate:web` green.
- `/dashboard/releases` RSC picker renders release list with links to progress pages; auth-gated via middleware.
- `/dashboard/releases/:releaseId/progress` RSC renders `Breadcrumb` + `<PlanTree>` per plan; unknown `releaseId` → `notFound()`; default-expanded step derived from metrics.
- `web/components/PlanTree.tsx` is the ONLY `'use client'` island on this surface.
- `web/proxy.ts` matcher: `['/dashboard', '/dashboard/:path*']` — both entries present (B2: single `:path*` alone breaks bare `/dashboard` coverage).
- `web/components/Sidebar.tsx` gains "Releases" link after Dashboard entry.
- `web/README.md` + `CLAUDE.md §6` route table updated.
- `npm run validate:web` green.

**Art:** None.

**Relevant surfaces (load when step opens):**

- `docs/web-platform-post-mvp-extensions.md` `## Design Expansion — §1 Release-scoped progress view` — Chosen Approach + Architecture mermaid + Subsystem Impact + Implementation Points + Examples (canonical source for this step).
- `web/lib/plan-loader.ts` + `web/lib/plan-loader-types.ts` (existing) — `PlanData`, `PlanMetrics`, `Step`, `Stage`, `TaskRow` types; read-only, no parser changes.
- `web/proxy.ts` (existing) — matcher widening target; retains `SESSION_COOKIE_NAME` / `portal_session` cookie gate from Stage 5.3.
- `web/components/Sidebar.tsx` (existing) — `LINKS` array append target.
- `web/components/BadgeChip.tsx` (existing) — status token classes (`bg-bg-status-done` etc.) reused in `TreeNode`.
- `web/components/Breadcrumb.tsx` (existing) — used in picker + progress RSC pages.
- `web/components/DataTable.tsx` (existing) — optional reuse in picker page.
- `web/app/dashboard/page.tsx` (existing) — zero edit; verify unaffected by proxy matcher change.
- Prior step outputs: Steps 3–5 shipped `loadAllPlans`, `computePlanMetrics`, `PlanMetrics`, auth middleware — consumed as-is.
- Glossary: **Rollout tracker** + **Rollout lifecycle** (canonical terms; `ia/specs/glossary.md`).

#### Stage 7.1 — Registry + pure shapers

**Status:** Final (4 tasks filed 2026-04-17 — TECH-339..TECH-342; all archived 2026-04-18)

**Backlog state (2026-04-18):** 4 / 4 tasks filed; all closed.

**Objectives:** Author the hand-maintained release registry, pure filtering shaper, default-expand predicate, and plan-tree builder. No routes, no UI, no auth changes. Self-contained data layer consumed by Stage 7.2 pages.

**Exit:**

- `web/lib/releases.ts`: `Release` interface + `resolveRelease()` + seeded `full-game-mvp` row; header comment cites **Rollout tracker** doc as source of truth.
- `web/lib/releases/resolve.ts`: `getReleasePlans()` pure filter; silently drops missing-on-disk children; imports `PlanData` from `web/lib/plan-loader-types.ts`.
- `web/lib/releases/default-expand.ts`: `deriveDefaultExpandedStepId()` predicate; JSDoc "tasks are ground truth; stale step-header Status prose ignored" + `'blocked'` unreachable note.
- `web/lib/plan-tree.ts`: `buildPlanTree()` + `TreeNodeData` union; phase nodes from `task.phase` groupBy, NOT `Stage.phases` checklist; JSDoc NB1.
- Unit tests for all four modules under `web/lib/**/__tests__/`; `npm run validate:web` green.

**Phases:**

- [ ] Phase 1 — Registry + resolve shaper (`releases.ts` + `releases/resolve.ts` + tests).
- [ ] Phase 2 — Default-expand + plan-tree shapers (`releases/default-expand.ts` + `plan-tree.ts` + tests).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T7.1.1 | 1 | **TECH-339** | Done (archived) | Author `web/lib/releases.ts` — `Release` interface (`id`, `label`, `umbrellaMasterPlan`, `children: string[]`) + `resolveRelease(id: string): Release | null` + seeded `releases` const array with `full-game-mvp` row (9 children from extensions doc Examples block); header comment cites `ia/projects/full-game-mvp-rollout-tracker.md` as source of truth for `children[]` drift warning. |
| T7.1.2 | 1 | **TECH-340** | Done (archived) | Author `web/lib/releases/resolve.ts` — `getReleasePlans(release: Release, allPlans: PlanData[]): PlanData[]` pure filter; matches `plan.filename` basename against `release.children`; silently drops missing-on-disk entries. Author `web/lib/__tests__/releases.test.ts` — unit tests: `resolveRelease` found/not-found, `getReleasePlans` filter + missing-child drop + umbrella self-inclusion edge case. |
| T7.1.3 | 2 | **TECH-341** | Done (archived) | Author `web/lib/releases/default-expand.ts` — `deriveDefaultExpandedStepId(plan: PlanData, metrics: PlanMetrics): string | null`; iterates `plan.steps` in order; returns first step id where `metrics.stepCounts[step.id]?.done < metrics.stepCounts[step.id]?.total`; returns `null` if all done or steps empty; JSDoc: "tasks are ground truth; stale step-header Status prose ignored" + `'blocked'` unreachable note. Author `web/lib/__tests__/default-expand.test.ts` — unit tests: first-non-done, all-done null, all-pending returns first, stale-header ignored, empty-steps null. |
| T7.1.4 | 2 | **TECH-342** | Done (archived) | Author `web/lib/plan-tree.ts` — `TreeNodeData` discriminated union (kind: `step | stage | phase | task`; id, label, status, counts, children); `buildPlanTree(plan: PlanData, metrics: PlanMetrics): TreeNodeData[]`; synthesizes phase nodes by `groupBy(task.phase)` within each stage (NOT conflated with `Stage.phases` checklist; JSDoc NB1); per-node status from `BadgeChip` Status union (`done | in-progress | pending | blocked`). Author `web/lib/__tests__/plan-tree.test.ts` — unit tests: stage-node counts, phase synthesis from tasks, status derivation, all-done propagation. |

---

#### Stage 7.2 — Routes + progress tree surface

**Status:** Final — TECH-351, TECH-352, TECH-353, TECH-354 archived 2026-04-18

**Objectives:** Author `TreeNode` + `PlanTree` Client components; ship the release picker RSC page (`/dashboard/releases`) and progress tree RSC page (`/dashboard/releases/[releaseId]/progress`). Relies on Stage 7.1 shapers.

**Exit:**

- `web/components/TreeNode.tsx`: recursive render; status glyph + label + count summary; `<button aria-expanded aria-controls>` for non-leaf (a11y); leaf tasks show Issue id when not `_pending_`.
- `web/components/PlanTree.tsx` (`'use client'`): `useState<Set<string>>` expanded ids seeded from `props.initialExpanded`; chevron toggle; ONLY Client island on this surface.
- `web/app/dashboard/releases/page.tsx` RSC: registry list with links; `Breadcrumb`; existing primitives only.
- `web/app/dashboard/releases/[releaseId]/progress/page.tsx` RSC: `resolveRelease` → `notFound()` on null; calls `loadAllPlans` + `getReleasePlans` + per-plan `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId`; renders `<PlanTree>` per plan; reserved comment for future `/rollout` sibling (no filesystem stub per B1).
- `npm run validate:web` green.

**Phases:**

- [x] Phase 1 — Client components (`TreeNode.tsx` + `PlanTree.tsx`).
- [x] Phase 2 — RSC pages (picker + progress page).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T7.2.1 | 1 | **TECH-351** | Done (archived) | Author `web/components/TreeNode.tsx` — recursive render of `TreeNodeData`; status-colored glyph (chevron for branches, `●` for task leaves); label + `{done}/{total}` count; `<button aria-expanded={isExpanded} aria-controls={childListId}>` for non-leaf toggles (a11y); leaf tasks show Issue id when present (not `_pending_`); consumes existing `BadgeChip` status token CSS classes; props: `node: TreeNodeData, expanded: Set<string>, onToggle: (id: string) => void`. |
| T7.2.2 | 1 | **TECH-352** | Done (archived) | Author `web/components/PlanTree.tsx` — `'use client'`; `useState<Set<string>>(new Set(props.initialExpanded))`; renders root `TreeNodeData[]` list; `onToggle = id => setExpanded(prev => { const next = new Set(prev); next.has(id) ? next.delete(id) : next.add(id); return next; })`; passes `expanded` + `onToggle` to each `<TreeNode>`; props: `{ nodes: TreeNodeData[], initialExpanded: Set<string> }`. ONLY Client island on this surface — progress `page.tsx` stays RSC. |
| T7.2.3 | 2 | **TECH-353** | Done (archived) | Author `web/app/dashboard/releases/page.tsx` (RSC) — imports `releases` registry from `web/lib/releases.ts`; renders `Breadcrumb` (Dashboard › Releases) + list/`DataTable` of release rows, each linking to `/dashboard/releases/{release.id}/progress`; full-English user-facing labels (caveman exception — CLAUDE.md §6); `npm run validate:web` green. |
| T7.2.4 | 2 | **TECH-354** | Done (archived) | Author `web/app/dashboard/releases/[releaseId]/progress/page.tsx` (RSC) — `resolveRelease(params.releaseId)` → `notFound()` on null; `loadAllPlans()` + `getReleasePlans` + per-plan `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId`; render `Breadcrumb` (Dashboard › Releases › {release.label} › Progress) + `<PlanTree nodes={tree} initialExpanded={new Set(defaultId ? [defaultId] : [])} />` per plan; reserved comment `// /dashboard/releases/:releaseId/rollout — reserved; URL 404s by default; no filesystem stub (B1)`; full-English headings. |

---

#### Stage 7.3 — Auth wiring, nav link + docs

**Status:** Final — TECH-358..TECH-361 archived 2026-04-18

**Backlog state (2026-04-18):** 4 / 4 tasks filed; all closed.

**Objectives:** Widen `web/proxy.ts` matcher to cover `/dashboard/:path*`; add "Releases" nav link to `Sidebar.tsx`; update route docs in `web/README.md` + `CLAUDE.md §6`. Final green gate for Step 7.

**Exit:**

- `web/proxy.ts` matcher: `['/dashboard', '/dashboard/:path*']`; both entries present (B2 guard); `/api/*` unaffected; unauthenticated request to `/dashboard/releases/**` → 302 to `/auth/login`.
- `web/components/Sidebar.tsx` `LINKS` array: `{ href: '/dashboard/releases', label: 'Releases', Icon: Layers3 }` after Dashboard entry.
- `web/README.md` route-list rows added for `/dashboard/releases` + `/dashboard/releases/:releaseId/progress`.
- `CLAUDE.md §6` route table row added.
- `npm run validate:web` green; bare `/dashboard` without session cookie still → 302 to `/auth/login` (regression guard).

**Phases:**

- [ ] Phase 1 — Auth matcher + nav link (`proxy.ts` + `Sidebar.tsx`).
- [ ] Phase 2 — Docs + validation (`web/README.md` + `CLAUDE.md §6` + `validate:web`).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T7.3.1 | 1 | **TECH-358** | Done (archived) | Edit `web/proxy.ts` — update `matcher` config to `['/dashboard', '/dashboard/:path*']`; both entries required (B2: single `:path*` breaks bare `/dashboard`); confirm no `/api/dashboard` path inadvertently gated; add reserved comment: `// /dashboard/releases/:releaseId/rollout — reserved; no filesystem stub`. |
| T7.3.2 | 1 | **TECH-359** | Done (archived) | Edit `web/components/Sidebar.tsx` — append `{ href: '/dashboard/releases', label: 'Releases', Icon: Layers3 }` to `LINKS` array after Dashboard entry; add `import { Layers3 } from 'lucide-react'` (or `ListTree` per S4 — pick by visual fit at implementation time); confirm mobile-collapsed behavior unaffected; `npm run validate:web` green. |
| T7.3.3 | 2 | **TECH-360** | Done (archived) | Update `web/README.md` — add route-list rows for `/dashboard/releases` (Release picker, auth-gated, RSC) + `/dashboard/releases/:releaseId/progress` (Release progress tree, auth-gated, RSC + `PlanTree` Client island); note auth gate inherits from Stage 7.3 proxy matcher widen. |
| T7.3.4 | 2 | **TECH-361** | Done (archived) | Update `CLAUDE.md §6` route table — add rows for `/dashboard/releases` + `/dashboard/releases/:releaseId/progress`; run `npm run validate:web` (lint + typecheck + build); confirm exit 0; confirm `DASHBOARD_AUTH_SKIP=1` dev bypass still functions (no regression on Stage 5.3 bypass knob). |

---

### Step 8 — Visual design layer

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 8):** 0 filed

**Objectives:** Author `web/lib/design-system.md` spec (type scale, spacing, motion vocab, semantic aliases, component map, a11y); derive `web/lib/design-tokens.ts` (TS const exports); extend `web/app/globals.css` `@theme` block with `ds-*` CSS custom properties (Tailwind v4 CSS-based config — no `tailwind.config.ts` exists in this project); ship `Heading` + `Prose` + `Surface` primitives; re-skin landing hero + dashboard (priority surfaces per Q1 interview); broad `tokens.*` → `ds-*` alias migration across components + wiki/devlog. Locked `palette.json` unchanged; game-accent additive only.

**Exit criteria:**

- `web/lib/design-system.md`: §1 type scale (10 levels, 1.25 minor-third ratio) + §2 spacing (4px grid, 9 stops) + §3 motion vocab (4 durations, reduced-motion first) + §4 semantic aliases (`text.*`/`surface.*`/`accent.*`) + §5 component map + §6 a11y notes; cites Dribbble + Shopify references; ≤ ~10 pages.
- `web/lib/design-tokens.ts`: exports `typeScale`, `spacing`, `motion`, `text`, `surface`, `accent`; imports `palette.json`; zero palette mutation; game-accent subset promoted (`terrainGreen` + `waterBlue` + one warm → `accent.*`).
- `web/app/globals.css` `@theme` block: new `--ds-*` CSS custom properties appended (type/spacing/motion/semantic-alias layers); all prefixed `ds-*`; existing `--color-*` / `--spacing-*` / `--text-*` entries untouched (B1 guard).
- `web/components/type/Heading.tsx` + `Prose.tsx` + `web/components/surface/Surface.tsx` ship; `Surface` default `motion="none"` is RSC-compatible; non-none triggers `'use client'` island (B2 guard).
- `web/app/_design-system/page.tsx`: dev-only showcase; `noindex`; `NODE_ENV !== 'production'` guard; unlinked from `Sidebar.tsx` (NB2).
- Landing hero + dashboard re-skinned; `tokens.*` migration complete on `Breadcrumb`, `Sidebar`, `BadgeChip`, `DataTable`, `FilterChips`; wiki + devlog MDX wrapped in `<Prose>`.
- Lighthouse baseline captured BEFORE Phase D re-skin; post-skin LCP ≤ baseline × 1.1, CLS < 0.1.
- `npm run validate:web` green.

**Art:** None.

**Relevant surfaces (load when step opens):**

- `docs/web-platform-post-mvp-extensions.md` `## Design Expansion — Section 8: Visual Design Layer` — Chosen Approach + Architecture mermaid + Subsystem Impact + Implementation Points A–F + Examples 1–3 (canonical source for this step).
- `web/app/globals.css` (existing) — Tailwind v4 `@theme` block; new `ds-*` CSS custom properties appended here. No `tailwind.config.ts` in this project — Tailwind v4 CSS-based config only.
- `web/lib/tokens/index.ts` + `web/lib/tokens/palette.json` + `web/lib/tokens/type-scale.json` + `web/lib/tokens/spacing.json` (existing) — locked palette + existing scale definitions; `design-tokens.ts` derives from / is consistent with these.
- `web/components/BadgeChip.tsx` + `Breadcrumb.tsx` + `Sidebar.tsx` + `DataTable.tsx` + `FilterChips.tsx` (existing) — token-alias migration targets in Stage 8.3.
- `web/app/page.tsx` (existing) — landing hero re-skin target.
- `web/app/dashboard/page.tsx` (existing) — dashboard re-skin target; verify `/dashboard/releases/**` (Step 7) unaffected.
- `web/app/wiki/**` + `web/app/devlog/**` (existing) — `<Prose>` wrapper targets; no layout rework.
- Prior step output: Step 7 ships `/dashboard/releases/**`; Stage 8.3 Phase 1 re-skin must not regress those routes.

#### Stage 8.1 — Design system spec + token pipeline

**Status:** Draft (tasks _pending_ — reset via TECH-411 Phase 1)

**Objectives:** Author `web/lib/design-system.md` spec; derive `web/lib/design-tokens.ts` (TS const); extend `globals.css` `@theme` with `ds-*` CSS custom properties; unit-test scale monotonicity + alias resolution + reduced-motion.

**Exit:**

- `web/lib/design-system.md`: §1–§6 complete; cites Dribbble + Shopify refs from extensions doc §8; game-accent subset identified from `palette.json` with WCAG AA verification; ≤ ~10 pages.
- `web/lib/design-tokens.ts`: `typeScale` (10 levels) + `spacing` (9 stops) + `motion` (4 durations + `reducedMotion: { duration: 0 }`) + `text` + `surface` + `accent` exports; imports `palette.json`; zero mutation.
- `web/app/globals.css` `@theme` block: `--ds-font-size-*`, `--ds-spacing-*`, `--ds-duration-*`, `--ds-text-*`, `--ds-surface-*`, `--ds-accent-*` CSS custom properties appended; existing entries untouched.
- `web/lib/__tests__/design-tokens.test.ts`: typeScale monotonically decreasing rem values, 9 spacing stops, motion keys complete, `reducedMotion.duration === 0`, alias hex values match palette.
- `npm run validate:web` green.

**Phases:**

- [ ] Phase 1 — Spec authorship + game-accent derivation (`design-system.md` only; no code).
- [ ] Phase 2 — Token pipeline (`design-tokens.ts` + `globals.css` `@theme` extension + tests).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T8.1.1 | 1 | _pending_ | _pending_ | Author `web/lib/design-system.md` — §1 type scale (10 levels, 1.25 minor-third ratio: `display` 3.815rem → `mono-meta`; weight + letter-spacing per level per extensions doc Example 1) + §2 spacing (4px grid, 9 stops: `2xs` 4px → `layout` 128px) + §3 motion vocab (4 durations: `instant` 0ms / `subtle` 120ms / `gentle` 200ms / `deliberate` 320ms; `prefers-reduced-motion: reduce` collapses all to `instant`; CSS transitions only) + §4 semantic aliases (`text.primary/secondary/meta/disabled`, `surface.canvas/raised/sunken/inset`, `accent.terrain/water/warm`) + §5 component map (per-component scale + spacing + motion bindings) + §6 a11y (WCAG AA on all aliases, `focus-visible` ring spec, keyboard nav); cites Dribbble + Shopify design references (extensions doc §8 source screenshots; NB5); cap ~10 pages. |
| T8.1.2 | 1 | _pending_ | _pending_ | Read `web/lib/tokens/palette.json` raw values; identify `terrainGreen` + `waterBlue` + one warm candidate (amber or closest warm hue); verify WCAG AA contrast ratio on `surface.canvas` (#0a0a0a) for each candidate (NB1 — designer taste call at implementation time); document selection + contrast ratios in `design-system.md` §4 `accent.*` subsection. |
| T8.1.3 | 2 | _pending_ | _pending_ | Author `web/lib/design-tokens.ts` — nested TS `const as const`: `typeScale` (10 entries), `spacing` (9 entries), `motion` (4 durations + `reducedMotion: { duration: 0 }`), `text` + `surface` + `accent` semantic alias maps; imports `./tokens/palette.json`; zero palette mutation; JSDoc on `motion.reducedMotion`: "`prefers-reduced-motion: reduce` collapses all durations to 0 via CSS media query in `globals.css`". Author `web/lib/__tests__/design-tokens.test.ts` — assert typeScale monotonically decreasing rem, 9 spacing stops, motion keys complete, `reducedMotion.duration === 0`, alias hex values resolve to palette raw entries. |
| T8.1.4 | 2 | _pending_ | _pending_ | Extend `web/app/globals.css` `@theme` block — append `--ds-*` CSS custom properties: `--ds-font-size-display` … `--ds-font-size-mono-meta` (type scale), `--ds-spacing-2xs` … `--ds-spacing-layout` (spacing), `--ds-duration-instant` … `--ds-duration-deliberate` + `--ds-duration-reduced-motion: 0ms` (motion), `--ds-text-*` / `--ds-surface-*` / `--ds-accent-*` semantic aliases; all prefixed `ds-*` (B1 guard — no collision with existing `--color-*` / `--spacing-*` / `--text-*`); add `@media (prefers-reduced-motion: reduce)` rule setting all `--ds-duration-*` to `0ms`; `npm run validate:web` green. |

---

#### Stage 8.2 — Prose + surface primitives

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Author `Heading` + `Prose` (type primitives) + `Surface` (panel with optional motion Client island) + dev-only `_design-system/page.tsx` showcase. Additive — no page adoption yet; zero existing component changes.

**Exit:**

- `web/components/type/Heading.tsx`: `level` prop (10 levels); maps to `--ds-font-size-{level}` via Tailwind v4 arbitrary value; HTML element derived from level; pure RSC.
- `web/components/type/Prose.tsx`: RSC wrapper; vertical rhythm via `[&>*+*]:mt-[var(--ds-spacing-md)]`; pure RSC; accepts `className?`.
- `web/components/surface/Surface.tsx`: `tone` + `padding` + `motion` props; default `motion="none"` → RSC-compat div; non-none → `'use client'` island + `useEffect` `data-mounted` + CSS transition rules in `globals.css` per extensions-doc Example 2 (including `prefers-reduced-motion` collapse); B2 guard: default `motion="none"`.
- `web/app/_design-system/page.tsx`: `notFound()` in production; renders all primitives + alias swatches + motion demo; `noindex` meta; unlinked from Sidebar (NB2).
- `npm run validate:web` green.

**Phases:**

- [ ] Phase 1 — Type primitives (`Heading.tsx` + `Prose.tsx`).
- [ ] Phase 2 — Surface primitive + showcase (`Surface.tsx` + `_design-system/page.tsx`).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T8.2.1 | 1 | _pending_ | _pending_ | Author `web/components/type/Heading.tsx` — `level: 'display' | 'h1' | 'h2' | 'h3' | 'body-lg' | 'body' | 'body-sm' | 'caption' | 'mono-code' | 'mono-meta'`; maps level → HTML element (`display/h1` → `<h1>`, `h2` → `<h2>`, `h3` → `<h3>`, `body-*` → `<p>`, `caption/mono-*` → `<span>`); applies `text-[var(--ds-font-size-{level})]` Tailwind v4 arbitrary value; optional `weight?` override class; optional `className?` passthrough; pure RSC. |
| T8.2.2 | 1 | _pending_ | _pending_ | Author `web/components/type/Prose.tsx` — RSC wrapper; accepts `children` + optional `className`; applies Tailwind v4 CSS vertical rhythm: `[&>*+*]:mt-[var(--ds-spacing-md)]`; cite `design-system.md` §5 component map in JSDoc; zero inline styles; pure RSC. |
| T8.2.3 | 2 | _pending_ | _pending_ | Author `web/components/surface/Surface.tsx` — `tone: 'raised' | 'sunken' | 'inset'` → `bg-[var(--ds-surface-{tone})]`; `padding: 'sm' | 'md' | 'lg' | 'section'` → `p-[var(--ds-spacing-{padding})]`; `motion?: 'none' | 'subtle' | 'gentle' | 'deliberate'` default `'none'`; `motion="none"` → pure RSC div; non-none → `'use client'` + `useEffect(() => setMounted(true), [])` + `data-mounted="true"`; append CSS transition rules + `prefers-reduced-motion: reduce` collapse to `globals.css` per extensions-doc Example 2; B2 guard enforced via prop default. |
| T8.2.4 | 2 | _pending_ | _pending_ | Author `web/app/_design-system/page.tsx` — `if (process.env.NODE_ENV === 'production') { notFound() }` guard (NB2); renders: all 10 `Heading` levels, `Prose` block with sample body text, `Surface` matrix (all tones × paddings), motion demo per duration, `BadgeChip` status token swatches, `--ds-*` CSS var reference table; `export const metadata = { robots: { index: false } }`; NOT added to `Sidebar.tsx` `LINKS`. |

---

#### Stage 8.3 — Priority surfaces adoption + broad token migration

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Re-skin landing hero + dashboard with new primitives (priority surfaces per Q1); broad `tokens.*` → `ds-*` alias migration across remaining components + wiki/devlog Prose wrap. Lighthouse baseline captured before Phase 1 re-skin.

**Exit:**

- Lighthouse baseline (LCP / CLS / TBT) captured on `localhost:4000` + `/dashboard` BEFORE any re-skin (NB3).
- `web/app/page.tsx` landing hero: `<Heading level="display">` + `<Surface tone="raised" motion="subtle">` + `--ds-accent-terrain` on CTA; full-English user-facing copy unchanged (B3 / CLAUDE.md §6).
- `web/app/dashboard/page.tsx` re-skinned; stat blocks in `<Surface>`; headings via `<Heading>`; `BadgeChip` uses `ds-*` aliases; `/dashboard/releases/**` (Step 7) unaffected (regression guard).
- `grep "tokens\."` surfaces enumerated; `Breadcrumb`, `Sidebar`, `BadgeChip`, `DataTable`, `FilterChips` migrated to `ds-*` CSS var classes; alias-neutral (palette unchanged — NB4 / Example 3 from extensions doc).
- `web/app/wiki/**` + `web/app/devlog/**` MDX output wrapped in `<Prose>`; no layout rework.
- `npm run validate:web` green; manual visual diff noted in PR body.

**Phases:**

- [ ] Phase 1 — Priority surfaces adoption (landing hero + dashboard re-skin).
- [ ] Phase 2 — Broad token-alias migration (components + wiki/devlog Prose wrap).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T8.3.1 | 1 | _pending_ | _pending_ | Capture Lighthouse baseline (LCP / CLS / TBT) on `localhost:4000` BEFORE any edit; store scores in PR body (NB3). Re-skin `web/app/page.tsx` landing hero: `<Heading level="display">` on main title; `<Surface tone="raised" motion="subtle" padding="section">` on hero panel; `bg-[var(--ds-accent-terrain)]` on CTA button; full-English user-facing copy unchanged (CLAUDE.md §6 / B3). `npm run validate:web` green. |
| T8.3.2 | 1 | _pending_ | _pending_ | Re-skin `web/app/dashboard/page.tsx`: wrap stat blocks in `<Surface tone="raised" padding="md">`; replace raw `<h1>`/`<h2>` with `<Heading level="h1">` / `<Heading level="h2">`; update `BadgeChip` usages to `ds-*` alias classes; verify `/dashboard/releases/**` (Stage 7.2) still renders correctly; `npm run validate:web` green. |
| T8.3.3 | 2 | _pending_ | _pending_ | Grep `tokens\.` across `web/app/**/*.tsx` + `web/components/**/*.tsx`; enumerate surfaces; migrate `web/components/Breadcrumb.tsx` + `web/components/Sidebar.tsx` inline `tokens.*` → `bg-[var(--ds-*)]` / `text-[var(--ds-*)]` Tailwind v4 arbitrary value classes; confirm alias-neutral (zero visual diff — same hex values per Example 3); `npm run validate:web` green. |
| T8.3.4 | 2 | _pending_ | _pending_ | Migrate `web/components/BadgeChip.tsx` + `web/components/DataTable.tsx` + `web/components/FilterChips.tsx` inline `tokens.*` → `ds-*` CSS var classes; wrap MDX output in `web/app/wiki/**` + `web/app/devlog/**` pages in `<Prose>` component (vertical rhythm only; no layout rework); `npm run validate:web` green; manual visual diff on `localhost:4000/wiki` + `/devlog` noted in PR body. |

---

#### Stage 8.4 — Docs + validation

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Update `web/README.md` (Design System section) + `CLAUDE.md §6` (spec path row); final `validate:web` green gate; post-skin Lighthouse check against Stage 8.3 baseline (NB3 regression guard).

**Exit:**

- `web/README.md` has `## Design System` section: spec path, primitive one-liners, showcase route note, `ds-*` class convention (Tailwind v4 CSS custom properties, not `tailwind.config.ts`).
- `CLAUDE.md §6` has row for `web/lib/design-system.md`.
- `npm run validate:web` green.
- Lighthouse post-check on `/`: LCP ≤ Stage 8.3 T8.3.1 baseline × 1.1; CLS < 0.1; if CLS regressed → set all `Surface motion="none"` in landing + dashboard as fallback.

**Phases:**

- [ ] Phase 1 — Docs (`web/README.md` + `CLAUDE.md §6`).
- [ ] Phase 2 — Final validation (`validate:web` + Lighthouse post-check).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T8.4.1 | 1 | _pending_ | _pending_ | Update `web/README.md` — add `## Design System` section: cite `web/lib/design-system.md` as authoritative spec; one-liner per primitive (`Heading` — level-bound RSC typography; `Prose` — MDX vertical-rhythm wrapper; `Surface` — tone/padding/motion panel); showcase route (`web/app/_design-system/page.tsx`, dev-only, unlinked); `ds-*` class convention note (Tailwind v4 CSS vars via `--ds-*` in `globals.css`, not `tailwind.config.ts`). |
| T8.4.2 | 1 | _pending_ | _pending_ | Update `CLAUDE.md §6` web workspace section — add row for design-system spec: `web/lib/design-system.md — Design system spec: type/spacing/motion/alias tables; derivation source for web/lib/design-tokens.ts + globals.css @theme ds-* block`; add caveman carve-out reminder: page-body JSX strings in `web/app/**/page.tsx` stay full English (CLAUDE.md §6 authority). |
| T8.4.3 | 2 | _pending_ | _pending_ | Run `npm run validate:web` (lint + typecheck + build) from repo root; fix any type or lint regressions introduced in Stages 8.1–8.3; confirm exit 0; report exit code + any fixes in PR body. |
| T8.4.4 | 2 | _pending_ | _pending_ | Run Lighthouse on `localhost:4000` (landing); record LCP / CLS / TBT; compare against Stage 8.3 T8.3.1 baseline (cap: LCP ≤ baseline × 1.1, CLS < 0.1); if CLS regressed → set `Surface motion="none"` in landing + dashboard and re-run Lighthouse; document result + any remediation in PR body (NB3). |

---

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

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
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
