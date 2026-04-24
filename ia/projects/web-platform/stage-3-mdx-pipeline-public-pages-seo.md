### Stage 3 — Public surface + wiki + devlog / MDX pipeline + public pages + SEO


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
- Phase 1 — MDX pipeline wiring (`next.config.ts`, loader, typed frontmatter).
- Phase 2 — Public pages (landing / about / install / history + MDX content).
- Phase 3 — SEO bedrock (sitemap, robots, OG image, metadata).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T3.1 | **TECH-163** | Done (archived) | Install + wire MDX pipeline — add `@next/mdx`, `@mdx-js/loader`, `@mdx-js/react`, `remark-frontmatter`, `remark-gfm`, `rehype-slug`, `rehype-autolink-headings` to `web/package.json`; extend `web/next.config.ts` with `withMDX` + plugin chain; configure `pageExtensions` to include `mdx`. |
| T3.2 | **TECH-164** | Done (archived) | Author `web/lib/mdx/loader.ts` + `web/lib/mdx/types.ts` — `loadMdxPage(slug)` reads from `web/content/pages/{slug}.mdx`, parses frontmatter via `gray-matter`, returns `{ source, frontmatter }`; typed `PageFrontmatter` interface (title, description, updated ISO date, hero optional). Companion `loadMdxContent(dir, slug)` generic helper for reuse by wiki + devlog stages. |
| T3.3 | **TECH-165** | Done (archived) | Replace boilerplate `web/app/page.tsx` w/ landing RSC consuming `web/content/pages/landing.mdx`; author full-English landing MDX (hero + what-this-is + CTA to `/install` + `/history`). Tokens exclusive — no inline hex. |
| T3.4 | TECH-166 | Done (archived) | Author `web/app/about/page.tsx`, `web/app/install/page.tsx`, `web/app/history/page.tsx` RSCs + matching `web/content/pages/{about,install,history}.mdx`. `/history` uses `DataTable` to render timeline rows from MDX-embedded data; `/install` uses `BadgeChip` for platform tags. |
| T3.5 | TECH-167 | Done (archived) | Author `web/app/sitemap.ts` + `web/app/robots.ts` — sitemap enumerates static public routes + MDX slugs (landing, about, install, history); robots allows `/`, disallows `/design` + `/dashboard` (reserved for Step 3). |
| T3.6 | TECH-168 | Done (archived) | Author `web/app/opengraph-image.tsx` via `next/og` — token-palette-driven OG card (title + subtitle from site-level metadata); per-route `generateMetadata` in each public page returns title + description + OG image url derived from frontmatter. |

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
