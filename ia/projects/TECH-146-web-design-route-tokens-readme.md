---
purpose: "TECH-146 — /design review route + web README §Tokens."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-146 — /design review route + web README §Tokens

> **Issue:** [TECH-146](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Authors `web/app/design/page.tsx` rendering every Stage 1.2 primitive (DataTable, BadgeChip, StatBar, FilterChips, HeatmapCell, AnnotatedMap) against 2–3 fixture variants each. Adds brief header marking page internal-review-only. Updates `web/README.md` §Tokens documenting palette JSON export contract (keys, semantic alias convention, Unity UI/UX consumption pattern stub). Closes Stage 1.2 — satisfies Exit bullets 3 + 5 + 6 (review surface + README §Tokens + glossary row candidate flagged).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/app/design/page.tsx` renders every primitive from Phase 2 tasks (DataTable + BadgeChip + StatBar + FilterChips archived; **TECH-145**) w/ 2–3 fixture variants each.
2. Page header: brief paragraph — "Internal review only. Not linked from public nav. Will migrate behind auth once portal lands (Step 4)."
3. `web/README.md` §Tokens documents:
   - Token file layout (`palette.json`, `type-scale.json`, `spacing.json`).
   - Semantic alias convention (raw → semantic indirection; `bg-canvas`, `text-accent-critical` etc.).
   - Unity UI/UX consumption pattern stub (read JSON at build; map to Unity `Color` / `Vector2` equivalents).
4. Route reachable on dev + deploy; no auth gate yet (flagged in page header + Step 3 follow-up).

### 2.2 Non-Goals

1. No obscure-URL gate — Step 3 surface when dashboard lands.
2. No auth middleware — Step 4.
3. No glossary row author — flagged in master plan Exit bullet 5 as post-stage close; add once tokens stabilize.
4. No primitive unit tests.
5. No `robots.txt` disallow entry for `/design` at this stage — add in Step 3 alongside `/dashboard` gate.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Visit `/design` on dev; eyeball every primitive. | Route renders all six primitives w/ fixture variants. |
| 2 | Agent | Read `web/README.md` §Tokens to understand palette schema for Unity UI/UX plan. | Section documents keys + semantic alias rule + Unity mapping stub. |

## 4. Current State

### 4.1 Domain behavior

Tokens + DataTable/BadgeChip + StatBar/FilterChips (all archived — see BACKLOG-ARCHIVE.md) + **TECH-145** ship tokens + six primitives. No review surface. No README §Tokens.

### 4.2 Systems map

- `web/app/design/page.tsx` — new.
- `web/README.md` — append §Tokens.
- `web/components/*` — consumed (DataTable + BadgeChip + StatBar + FilterChips archived; **TECH-145**).
- `web/lib/tokens/*` — documented (archived tokens task — see BACKLOG-ARCHIVE.md).

## 5. Proposed Design

### 5.1 Target behavior

Page structure (sketch):

```tsx
// web/app/design/page.tsx
export default function DesignPage() {
  return <main>
    <header>... internal-review banner ...</header>
    <section id="datatable">
      <DataTable rows={fixtureRows} columns={fixtureCols} statusCell={r => <BadgeChip status={r.status}/>}/>
    </section>
    <section id="statbar">
      <StatBar label="CPU" value={20} max={100}/>
      <StatBar label="Memory" value={75} max={100} thresholds={{warn:70,critical:90}}/>
      <StatBar label="Disk" value={95} max={100} thresholds={{warn:70,critical:90}}/>
    </section>
    {/* FilterChips, HeatmapCell grid, AnnotatedMap fixture */}
  </main>
}
```

### 5.2 Architecture

Single SSR page. Fixture data inlined at module scope.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| YYYY-MM-DD | … | … | … |

## 7. Implementation Plan

### Phase 1 — `/design` route

- [ ] Author `web/app/design/page.tsx` w/ all six primitives + 2–3 fixture variants each.
- [ ] Add internal-review header.

### Phase 2 — README §Tokens

- [ ] Append §Tokens to `web/README.md`: file layout, semantic alias rule, Unity mapping stub.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Route builds + renders | Node | `cd web && npm run build` | Web workspace build |
| Visual review of all six primitives | Manual | `cd web && npm run dev` + visit `/design` | Stage-close gate |
| README §Tokens documented | Manual | `web/README.md` diff review | Unblocks Unity UI/UX future plan |

## 8. Acceptance Criteria

- [ ] `/design` route reachable on dev + deploy.
- [ ] All six primitives rendered w/ 2–3 fixture variants each.
- [ ] `web/README.md` §Tokens documents palette JSON contract.
- [ ] Internal-review banner present.
- [ ] `npm run validate:all` green.

## Open Questions

None — tooling only; see §8. `web/content/**` + page-body JSX strings may contain public-facing English prose (caveman exception per `ia/rules/agent-output-caveman.md`); the internal-review banner is internal-facing so caveman applies.
