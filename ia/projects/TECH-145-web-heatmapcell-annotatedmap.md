---
purpose: "TECH-145 — Web primitives: HeatmapCell + AnnotatedMap."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-145 — Web primitives: HeatmapCell + AnnotatedMap

> **Issue:** [TECH-145](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-14
> **Last updated:** 2026-04-14

## 1. Summary

Authors SSR-only `HeatmapCell` + `AnnotatedMap` primitives under `web/components/`. HeatmapCell: single grid cell w/ `intensity` (0–1) → palette bucket. AnnotatedMap: SVG wrapper w/ `regions` + `annotations` props; renders NYT-style spaced-caps geo labels. Last two of six Stage 1.2 primitives. Satisfies Stage 1.2 Exit bullet 2.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `web/components/HeatmapCell.tsx` — `intensity: number` (0–1) prop; maps to palette bucket (e.g. 5 steps from `bg-panel` → `text-accent-critical`).
2. `web/components/AnnotatedMap.tsx` — `regions: { id: string; path: string; intensity?: number }[]` + `annotations: { x: number; y: number; label: string }[]` props; SVG root.
3. AnnotatedMap renders spaced-caps geo labels (NYT style — e.g. letter-spacing + uppercase).
4. Both SSR-only (no `"use client"`).
5. Render without throw against fixture props.

### 2.2 Non-Goals

1. No choropleth projection library (D3-geo / topojson) — plain SVG path strings only at MVP.
2. No interactive zoom / hover tooltips — Step 3.
3. No live data fetching — fixture-only.
4. No animations.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Compose a 32×32 intensity grid from HeatmapCell. | `<HeatmapCell intensity={0.7}/>` resolves to correct bucket. |
| 2 | Developer | Render regional map w/ spaced-caps labels. | `<AnnotatedMap regions={...} annotations={...}/>` outputs valid SVG. |

## 4. Current State

### 4.1 Domain behavior

Tokens + DataTable/BadgeChip + StatBar/FilterChips (all archived — see BACKLOG-ARCHIVE.md) land first. HeatmapCell + AnnotatedMap cover the densest visualizations — consumed by Step 3 dashboard (per-plan heatmap) + Step 2 wiki (term co-occurrence map).

### 4.2 Systems map

- `web/components/HeatmapCell.tsx` — new.
- `web/components/AnnotatedMap.tsx` — new.
- `web/lib/tokens/*` — consumed via Tailwind aliases.

## 5. Proposed Design

### 5.1 Target behavior

```tsx
// HeatmapCell — 5-bucket palette
const BUCKETS = ['bg-panel', 'bg-grey-700', 'text-accent-warn', 'text-accent-critical-muted', 'text-accent-critical']
export function HeatmapCell({ intensity }: { intensity: number }) {
  const idx = Math.min(4, Math.floor(intensity * 5))
  return <div className={`w-4 h-4 ${BUCKETS[idx]}`}/>
}
```

```tsx
// AnnotatedMap
export function AnnotatedMap({ regions, annotations }: Props) {
  return <svg viewBox="0 0 1000 600">
    {regions.map(r => <path key={r.id} d={r.path} className={bucketForIntensity(r.intensity)}/>)}
    {annotations.map(a => <text key={a.label} x={a.x} y={a.y} style={{letterSpacing:'0.15em',textTransform:'uppercase'}}>{a.label}</text>)}
  </svg>
}
```

### 5.2 Architecture

Pure SSR components. No runtime state.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| YYYY-MM-DD | … | … | … |

## 7. Implementation Plan

### Phase 1 — HeatmapCell

- [ ] Author `HeatmapCell.tsx` w/ 5-bucket intensity → alias map.

### Phase 2 — AnnotatedMap

- [ ] Author `AnnotatedMap.tsx` w/ regions + annotations props; NYT-style labels.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Components compile + SSR render without throw | Node | `cd web && npm run build` | Web workspace build |
| Visual review | Manual | TECH-146 `/design` route | Deferred |

## 8. Acceptance Criteria

- [ ] `HeatmapCell.tsx` + `AnnotatedMap.tsx` present under `web/components/`.
- [ ] Neither file opts into `"use client"`.
- [ ] `npm run validate:all` green.

## Open Questions

None — tooling only; see §8. Exact bucket count (5) + label letter-spacing (`0.15em`) are agent-proposed; user may lock during kickoff.
