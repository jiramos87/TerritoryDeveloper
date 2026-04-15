---
purpose: "TECH-187 — Client-side wiki search component (Stage 2.2 Phase 2)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-187 — Client-side wiki search component (Stage 2.2 Phase 2)

> **Issue:** [TECH-187](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-15
> **Last updated:** 2026-04-15

## 1. Summary

Stage 2.2 Phase 2 closer. Client component `web/components/WikiSearch.tsx` fetches `/search-index.json` on mount, constructs `Fuse` instance w/ `keys: ['title', 'body', 'category']` + tuned threshold, renders input + result list linking `/wiki/{slug}`. Embedded in `/wiki` header. Installs `fuse.js`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/components/WikiSearch.tsx` (`'use client'`) — fetches `/search-index.json` once, builds `Fuse` instance, renders search UI.
2. Fuzzy match against `title`, `body`, `category` w/ threshold tuned for reasonable typo tolerance.
3. Result list items link `/wiki/{slug}`.
4. `fuse.js` added to `web/package.json` deps (pinned version).
5. Embedded in `web/app/wiki/page.tsx` header.
6. `npm run validate:web` + `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. No server-side search fallback (static JSON only).
2. No keyboard nav polish beyond basic tab/enter (can follow-up if UX feedback).

## 4. Current State

### 4.2 Systems map

- `web/public/search-index.json` (TECH-186) — static fuse.js-shaped records.
- `web/app/wiki/page.tsx` (TECH-185 archived) — host surface for search input.
- Design tokens (Stage 1.2 archived) — reuse for input + list styling.

## 7. Implementation Plan

### Phase 1 — Component + integration

- [ ] `npm install fuse.js` in `web/` (pin version).
- [ ] Author `web/components/WikiSearch.tsx` — `useEffect` fetch, `useMemo` Fuse, controlled input, result list.
- [ ] Embed in `/wiki` page header.
- [ ] Token-driven styling (no inline hex).

## 8. Acceptance Criteria

- [ ] `/wiki` header renders search input.
- [ ] Typing yields fuzzy matches spanning glossary + wiki records.
- [ ] Each result links `/wiki/{slug}`.
- [ ] `fuse.js` pinned in `web/package.json`.
- [ ] `npm run validate:web` green.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
