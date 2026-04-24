### Stage 5 — Public surface + wiki + devlog / Devlog + RSS + origin story


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
- Phase 1 — Devlog routes + MDX content.
- Phase 2 — RSS feed + sitemap integration.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T5.1 | **TECH-192** | Done (archived) | Author `web/app/devlog/page.tsx` + `web/lib/mdx/reading-time.ts` — list RSC scans `web/content/devlog/*.mdx` via filesystem read, parses frontmatter (`title`, `date`, `tags[]`, `cover?`, `excerpt`), sorts desc by `date`, renders card list w/ `BadgeChip` tags + read-time computed from MDX body. Extend `PageFrontmatter` or add `DevlogFrontmatter` type in `web/lib/mdx/types.ts`. |
| T5.2 | **TECH-193** | Done (archived) | Author `web/app/devlog/[slug]/page.tsx` + `web/content/devlog/2026-MM-DD-origin-story.mdx` — single-post RSC renders via `loadMdxContent('devlog', slug)`; cover image (frontmatter `cover` optional), tags row, read-time, `generateMetadata` returns OG image derived from cover or falling back to site default. Origin-story MDX seed authored in full English per caveman-exception. |
| T5.3 | **TECH-194** | Done (archived) | Author `web/app/feed.xml/route.ts` — Next.js route handler (`GET`) returns RSS 2.0 XML (`<rss version="2.0"><channel>…</channel></rss>`) enumerating latest 20 devlog posts w/ `<item>` per post (`title`, `link`, `description` from excerpt, `pubDate` RFC-822, `guid`); `Content-Type: application/rss+xml; charset=utf-8`. |
| T5.4 | **TECH-195** | Done (archived) | Extend `web/app/sitemap.ts` (from Stage 2.1) to enumerate devlog slugs via filesystem scan of `web/content/devlog/`; add footer nav link to `/feed.xml` + `/devlog` in `web/app/layout.tsx`. `validate:all` green. |

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

---
