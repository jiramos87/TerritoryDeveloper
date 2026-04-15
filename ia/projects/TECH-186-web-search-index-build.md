---
purpose: "TECH-186 — Build-time search index emitter (Stage 2.2 Phase 2)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-186 — Build-time search index emitter (Stage 2.2 Phase 2)

> **Issue:** [TECH-186](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Stage 2.2 Phase 2 opener. Node script `web/lib/search/build-index.ts` emits `web/public/search-index.json` (fuse.js-shaped records) covering glossary terms + hand-authored wiki MDX. Wired via `web/package.json` `prebuild` so `next build` auto-regenerates. Deterministic output for CI repeatability.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/lib/search/build-index.ts` — callable as Node CLI; reads `GlossaryTerm[]` (via TECH-184) + globs `web/content/wiki/**.mdx`.
2. Emit `web/public/search-index.json` w/ records `{ slug, title, body, category, type: 'glossary' | 'wiki' }`.
3. Deterministic sort (by `slug` ascending) — two successive runs produce identical bytes.
4. `web/package.json` `prebuild` script invokes emitter before `next build`.
5. `npm run validate:web` + `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. No client-side search UI (deferred to TECH-187).
2. No incremental rebuild — full re-emit each build (small record set).

## 4. Current State

### 4.2 Systems map

- `web/lib/glossary/import.ts` (TECH-184) — glossary source.
- `web/lib/mdx/loader.ts` (TECH-164 archived) — reusable for wiki MDX body/frontmatter read (or direct `gray-matter` glob).
- `web/public/` — static asset dir served at `/search-index.json`.
- Downstream: TECH-187 (`WikiSearch.tsx` client component).

## 7. Implementation Plan

### Phase 1 — Emitter + wiring

- [ ] Author `web/lib/search/build-index.ts` w/ Node shebang or `tsx` runner.
- [ ] Assemble glossary records via `loadGlossaryTerms()` + wiki records via MDX glob + frontmatter parse.
- [ ] Stable sort by slug; `JSON.stringify` w/ 2-space indent (determinism + diffability).
- [ ] Add `prebuild` script in `web/package.json` invoking emitter via `tsx`.

## 8. Acceptance Criteria

- [ ] Running `prebuild` emits `web/public/search-index.json`.
- [ ] Records cover all glossary terms + all wiki MDX files.
- [ ] Two successive runs produce byte-identical output.
- [ ] `npm run validate:web` green.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
