# Web Platform — Master Plan (MVP)

> **Status:** Draft — Steps 1–2 Final; Step 3 decomposed (Stage 3.1 Done; Stage 3.2 filed — tasks Draft; Stage 3.3 _pending_)
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

**Status:** Draft (Stage 3.1 Done; Stage 3.2 Done; Stage 3.3 _pending_)

**Backlog state (Step 3):** Stage 3.1 — TECH-200…TECH-203 (archived); Stage 3.2 — TECH-205…TECH-208 (archived)

**Objectives:** Replace the static `docs/progress.html` snapshot with a live React Server Component dashboard at `/dashboard` that reads every master plan under `ia/projects/*-master-plan.md` via a thin wrapper around `tools/progress-tracker/parse.mjs`. Filter chips (per-plan / per-status / per-phase) use the Stage 1.2 `FilterChips` primitive; table uses `DataTable`. Apply the Q14 obscure-URL gate — unlinked route, `robots.txt` disallow, "internal" banner — until Step 4 portal auth lands, at which point dashboard migrates behind auth middleware. Add "Live dashboard" link on the legacy `docs/progress.html`; deprecate the legacy page once dashboard has proven stable for a measurable duration (exact trigger tracked in Step 3 Decision Log).

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

**Status:** In Review — TECH-213 closed 2026-04-15 (archived); TECH-214 filed 2026-04-15 (kicked off).

**Objectives:** Wire the `docs/progress.html` "Live dashboard" link to the Vercel deploy URL, run end-to-end smoke confirming the dashboard works in production, and author the Decision Log entry for the `docs/progress.html` deprecation trigger.

**Exit:**

- `docs/progress.html` has a visible "Live dashboard →" banner at top linking to `https://web-nine-wheat-35.vercel.app/dashboard`.
- End-to-end smoke: `/dashboard` returns 200 on Vercel deploy; filter chips modify URL params + re-render; "internal" banner visible; Vercel-served `robots.txt` disallows the route.
- §Decision Log section added to this master plan below Orchestration guardrails, documenting the `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles after Step 4 portal auth gate lands).

**Phases:**

- [ ] Phase 1 — Legacy link + E2E smoke + deprecation decision log.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T3.3.1 | 1 | **TECH-213** (archived) | Done | Edit `docs/progress.html` — insert "Live dashboard →" banner `<div>` at top of `<body>` linking to `https://web-nine-wheat-35.vercel.app/dashboard`; minimal inline style consistent with existing page aesthetic (no external CSS added). |
| T3.3.2 | 1 | **TECH-214** | In Review | End-to-end smoke + deprecation decision log — manually confirm Vercel `/dashboard` returns 200, filter chips functional, "internal" banner visible, `robots.txt` disallows route; append §Decision Log section to this master plan below Orchestration guardrails documenting `docs/progress.html` deprecation trigger (proposed: ≥2 stable deploy cycles post Step 4 portal-auth gate). |

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

### Step 5 — Playwright E2E harness

**Status:** Draft — decomposition deferred until Step 4 closes.

**Objectives:** Install and configure Playwright as the automated e2e layer for the `web/` workspace; integrate into `npm run validate:all` CI chain; land baseline route coverage for all existing public surfaces; then add dashboard-specific e2e for SSR query-param filter flows. Step 4 portal auth-flow tests extend this harness as a Stage 4.X — the harness ships here so portal work inherits it without bootstrapping from scratch.

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

#### Stage 5.1 — Install + config + CI wiring

**Status:** _pending_

**Objectives:** Install `@playwright/test`; author `web/playwright.config.ts` (baseURL from env, headless Chromium, 1 worker in CI); add `test:e2e` + `test:e2e:ci` scripts to `web/package.json`; wire into root `validate:all` (opt-in flag or separate `validate:e2e` target to avoid mandatory browser install in non-e2e CI contexts); document env var contract in `web/README.md`.

**Exit:**
- `cd web && npm run test:e2e` runs (even with 0 test files) without error.
- Root `npm run validate:e2e` composes `web/` e2e run; existing `validate:all` unchanged (no forced browser install).
- `web/README.md` §E2E section present.

**Phases:**
- [ ] Phase 1 — Install `@playwright/test` + `playwright.config.ts`.
- [ ] Phase 2 — npm scripts + root composition.
- [ ] Phase 3 — README §E2E documentation.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.1.1 | 1 | _pending_ | _pending_ | Install `@playwright/test` into `web/package.json` devDeps; author `web/playwright.config.ts` — `baseURL` from `process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:4000'`; headless Chromium project; `testDir: './tests'`; `outputDir: './playwright-report'`; add `web/tests/` dir stub (`.gitkeep`). |
| T5.1.2 | 2 | _pending_ | _pending_ | Add `"test:e2e": "playwright test"` + `"test:e2e:ci": "playwright test --reporter=github"` to `web/package.json`; add `"validate:e2e": "npm --prefix web run test:e2e:ci"` to root `package.json`; add `web/playwright-report/` to `.gitignore`. `validate:all` unchanged — `validate:e2e` is a separate opt-in target. |
| T5.1.3 | 3 | _pending_ | _pending_ | Extend `web/README.md` §E2E — document: local run (`npm run test:e2e`), `PLAYWRIGHT_BASE_URL` env contract, Vercel preview injection pattern (`PLAYWRIGHT_BASE_URL=https://$VERCEL_URL`), CI bootstrap (`npx playwright install --with-deps chromium`), and convention for adding tests per route under `web/tests/`. |

---

#### Stage 5.2 — Baseline route coverage

**Status:** _pending_

**Objectives:** Author e2e tests for all existing public surfaces. Validates that routes return 200, key content landmarks are present, `robots.txt` disallows `/dashboard`, sitemap enumerates slugs, RSS `Content-Type` correct. No auth-gated routes at this stage.

**Exit:**
- `npm run test:e2e` green against `localhost:4000` (dev server) + headless Chromium.
- Tests cover: landing, `/about`, `/install`, `/history`, `/wiki`, `/devlog` (list + at least one slug), `robots.txt` body, `/sitemap.xml` slug presence, `/feed.xml` Content-Type.

**Phases:**
- [ ] Phase 1 — Static page smoke tests.
- [ ] Phase 2 — robots / sitemap / RSS contract tests.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.2.1 | 1 | _pending_ | _pending_ | Author `web/tests/routes.spec.ts` — assert HTTP 200 + at least one visible heading for: `/`, `/about`, `/install`, `/history`, `/wiki`, `/devlog`; assert first devlog slug link navigates to a 200 page. |
| T5.2.2 | 2 | _pending_ | _pending_ | Author `web/tests/meta.spec.ts` — assert `robots.txt` body contains `Disallow: /dashboard`; assert `/sitemap.xml` contains at least one devlog URL; assert `GET /feed.xml` response `Content-Type` header matches `application/rss+xml`. |

---

#### Stage 5.3 — Dashboard e2e (SSR filter flows)

**Status:** _pending_

**Objectives:** Author e2e tests for the dashboard's SSR query-param filter chip flows. Validates the full round-trip: URL param → server render → active chip state → filtered task rows → clear-filters reset. Covers combinations and empty-state.

**Exit:**
- Dashboard filter chip tests green headless; `?plan=` / `?status=` / `?phase=` each produce active chip + filtered rows; multi-param combination narrows correctly; clear-filters `<a>` resets to unfiltered state; unrecognised param value renders empty-state message.

**Phases:**
- [ ] Phase 1 — Single-param filter round-trip tests.
- [ ] Phase 2 — Multi-param + clear-filters + empty-state tests.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T5.3.1 | 1 | _pending_ | _pending_ | Author `web/tests/dashboard-filters.spec.ts` — for each of `plan`, `status`, `phase` params: navigate to `/dashboard?{param}={value}` with a known value from the unfiltered render; assert chip with matching label has active visual state (class or aria); assert table rows visible count < unfiltered count. |
| T5.3.2 | 2 | _pending_ | _pending_ | Extend `web/tests/dashboard-filters.spec.ts` — multi-param test (`?status=Done&phase=1`): assert rows satisfy both filters; clear-filters link test: assert `<a href="/dashboard">` present when any param active, clicking it returns unfiltered row count; unknown-value test: navigate to `/dashboard?status=nonexistent` and assert empty-state message text present. |

---

## Deferred decomposition

Materialize when the named step opens (per `ia/rules/project-hierarchy.md` lazy-materialization rule). Do NOT pre-decompose — surface area changes once Step {N-1} lands.

- **Step 2 — Public surface + wiki + devlog:** decomposed 2026-04-15. Stages: `MDX pipeline + public pages + SEO`, `Wiki + glossary auto-index + search`, `Devlog + RSS + origin story`.
- **Step 3 — Live dashboard:** decomposed 2026-04-15. Stages: `Plan loader + typed schema`, `Dashboard RSC + filters`, `Legacy handoff + validation`.
- **Step 4 — Portal foundations:** decompose after Step 3 closes. Candidate stages: `Postgres provider + auth library selection`, `Auth API stubs + schema draft`, `Dashboard auth migration`.
- **Step 5 — Playwright E2E harness:** decomposed 2026-04-15. Stages: `Install + config + CI wiring`, `Baseline route coverage`, `Dashboard e2e (SSR filter flows)`. Decompose-after trigger deferred to Step 4 close; stage structure fully authored now.

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
| 2026-04-15 | Playwright chosen as e2e framework (Step 5) over Cypress + Puppeteer | SSR/RSC filter flows require real request cycle — Playwright's browser context hits the server, validating what actually renders; TypeScript-first; built-in test runner; CI-friendly `--with-deps`; multi-browser (Chromium sufficient for CI) | Cypress — client-DOM bias, weaker RSC support, heavier CI image; Puppeteer — Chrome-only, no built-in runner, more glue code |
| 2026-04-15 | `validate:e2e` is a separate root target, not merged into `validate:all` | Browser install (`playwright install`) is heavy; agent CI runs `validate:all` headlessly without browser deps; e2e runs in a dedicated CI step or manually | Merge into `validate:all` — rejected, breaks non-e2e agent shells |
| 2026-04-15 | Deprecate `docs/progress.html` after Step 4 portal-auth gate lands ≥2 stable deploy cycles | Avoid premature removal while portal auth unresolved; live `/dashboard` stays obscure-URL-gated until auth middleware lands; ≥2 deploy cycles gives rollback window if dashboard regresses | Immediate delete — rejected, leaves no fallback if dashboard regresses; link-only banner (archived TECH-213) + no trigger — rejected, leaves legacy indefinitely without closure condition |

---
