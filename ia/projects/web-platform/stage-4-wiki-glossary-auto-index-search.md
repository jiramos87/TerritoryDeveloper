### Stage 4 — Public surface + wiki + devlog / Wiki + glossary auto-index + search


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
- Phase 1 — Glossary import + wiki routing scaffold.
- Phase 2 — Search index build + client search component.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T4.1 | **TECH-184** | Done | Author `web/lib/glossary/import.ts` — reads `ia/specs/glossary.md` from repo root (relative path via `path.resolve(process.cwd(), '../ia/specs/glossary.md')` or equivalent build-safe mechanism); parses markdown tables via `remark-parse` or regex split; emits `GlossaryTerm[]` w/ `Spec reference` column filtered out; includes slug derivation (kebab-case term). Typed export consumed by wiki routes. |
| T4.2 | **TECH-185** | Done (archived) | Author `web/app/wiki/[...slug]/page.tsx` + `web/app/wiki/page.tsx` — catch-all route renders hand-authored MDX from `web/content/wiki/**.mdx` via `loadMdxContent('wiki', slug)` OR glossary-derived page (renders `GlossaryTerm.definition` in MDX-styled shell when slug matches imported term); `/wiki` index uses `DataTable` + groups by category; `generateStaticParams` unions MDX slugs + glossary slugs. Seed `web/content/wiki/README.mdx` with frontmatter + 1 paragraph. |
| T4.3 | **TECH-186** | Done (archived) | Author `web/lib/search/build-index.ts` + `web/package.json` `prebuild` entry — script consumes `GlossaryTerm[]` + scans `web/content/wiki/**.mdx` frontmatter/body, emits `web/public/search-index.json` (fuse.js records: `{ slug, title, body, category, type: 'glossary' | 'wiki' }`). Deterministic output for CI repeatability. |
| T4.4 | **TECH-187** | Done (archived) | Author `web/components/WikiSearch.tsx` client component — fetches `/search-index.json` on mount, constructs `Fuse` instance w/ `keys: ['title', 'body', 'category']`, threshold tuned for fuzzy match; renders input + result list linking to `/wiki/{slug}`. Embedded in `web/app/wiki/page.tsx` header. Install `fuse.js` into `web/package.json` deps. |

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
