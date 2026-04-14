---
purpose: "Reference spec for Territory Developer web platform UI/UX design system — tokens, components, and patterns for the Next.js web layer."
audience: agent
loaded_by: router
slices_via: spec_section
---
# Web UI Design System — Spec

## Overview

Defines the **visual language**, **design tokens**, **components**, and **layout patterns** for the Territory Developer web platform (`web/` monorepo directory, Next.js + React + TypeScript + Tailwind CSS, deployed to Vercel).

**Scope:** Web layer only — public site, DevOps dashboard, and future user portal. In-game Unity uGUI design targets live in `ia/specs/ui-design-system.md §7`. Both specs share the same underlying visual language — dark-first, data-dense, systems-serious — but use different implementation stacks.

**Status:** Draft — seeded from visual reference analysis (April 2026). Sections marked *(TBD)* await `/design-explore` + `/master-plan-new` pass before backlog issues open.

**Lifecycle note:** This spec is permanent domain documentation (under `ia/specs/`). Web-platform implementation work goes in `ia/projects/{ISSUE_ID}.md` issue specs; lessons migrate back here on closeout.

---

## 1. Design principles

| Principle | Description |
|-----------|-------------|
| **Data-first** | Information density over decorative whitespace. Numbers, labels, and metrics are the hero. |
| **Dark-only** | No light-mode variant planned. Dark base is the canonical experience. |
| **Semantic color** | Color encodes meaning (positive/negative/neutral), never decoration. |
| **Systems-serious** | Tone is precise, restrained, technical — not playful or casual. Matches the game's simulation depth. |
| **Progressive disclosure** | Summary first; drill into detail on demand (tabs, expandable rows, modal panels). |
| **Free-tier safe** | No client-side tracking pixels or bundled analytics that could incur cost. Privacy-respecting by default. |

---

## 2. Color tokens

Design tokens defined as CSS custom properties (`--token-name`). Implemented via Tailwind CSS `theme.extend.colors` or a `globals.css` variable sheet.

### 2.1 Base palette

| Token | Value | Role |
|-------|-------|------|
| `--color-base` | `#0a0a0a` | Page background |
| `--color-surface-1` | `#111111` | Card / panel |
| `--color-surface-2` | `#1a1a1a` | Elevated card |
| `--color-surface-3` | `#222222` | Hovered row / selected chip |
| `--color-border-subtle` | `rgba(255,255,255,0.07)` | Card borders, dividers |
| `--color-border-muted` | `rgba(255,255,255,0.13)` | Active borders, focused inputs |

### 2.2 Text

| Token | Value | Role |
|-------|-------|------|
| `--color-text-primary` | `#e8e8e8` | Body, headings |
| `--color-text-secondary` | `rgba(232,232,232,0.65)` | Labels, captions |
| `--color-text-muted` | `rgba(232,232,232,0.38)` | De-emphasized, placeholders |
| `--color-text-disabled` | `rgba(232,232,232,0.22)` | Disabled state |

### 2.3 Semantic accents

| Token | Value | Meaning |
|-------|-------|---------|
| `--color-positive` | `#40bf72` | Growth, surplus, good metric |
| `--color-positive-dim` | `#2d8a4e` | Positive bg chip / bar fill |
| `--color-negative` | `#e05555` | Deficit, damage, alert |
| `--color-negative-dim` | `#8a2d2d` | Negative bg chip / bar fill |
| `--color-neutral` | `#70a0e0` | Interactive chrome, selected |
| `--color-neutral-dim` | `#1a2a40` | Neutral bg chip |
| `--color-warning` | `#c0b060` | Caution, draft state |
| `--color-warning-dim` | `#2a2a1a` | Warning bg chip |

### 2.4 Map / geographic layer colors

| Token | Value | Role |
|-------|-------|------|
| `--color-map-base` | `#141414` | Base tile fill |
| `--color-map-border` | `#2a2a2a` | Country/region outline |
| `--color-heat-low` | `rgba(64,191,114,0.3)` | Heat overlay — low intensity |
| `--color-heat-high` | `rgba(224,85,85,0.8)` | Heat overlay — high intensity |
| `--color-bubble-primary` | `rgba(112,160,224,0.5)` | Proportional bubble fill |
| `--color-bubble-border` | `rgba(112,160,224,0.8)` | Proportional bubble stroke |

---

## 3. Typography

**Stack:** System UI sans-serif as default body (`system-ui, -apple-system, 'Segoe UI', sans-serif`). Monospace for code/data cells (`'JetBrains Mono', 'Fira Code', ui-monospace, monospace`). No custom font loading until a dedicated typography issue scopes it (avoids FOUT and free-tier bandwidth cost).

| Style token | Size | Weight | Letter spacing | Usage |
|-------------|------|--------|----------------|-------|
| `display` | `2rem` | 700 | `−0.02em` | Hero headings, page titles |
| `heading-1` | `1.4rem` | 600 | `−0.01em` | Section headings |
| `heading-2` | `1.1rem` | 600 | `0` | Card titles |
| `label-caps` | `0.72rem` | 500 | `0.12em` | Metric keys, column headers (uppercase) |
| `body` | `0.875rem` | 400 | `0` | Prose, descriptions |
| `caption` | `0.78rem` | 400 | `0` | Meta, timestamps, footnotes |
| `data` | `0.84rem` | 400 | `0` | Table cells, stat values (monospace) |
| `data-large` | `1.5rem` | 700 | `−0.02em` | Large numeric readouts |

**Tabular nums:** All numeric data fields use `font-variant-numeric: tabular-nums` for column alignment.

---

## 4. Components

### 4.1 StatBadge

Rounded-rectangle chip encoding a numeric value with semantic color.

```
┌─────────┐
│  96.0   │  ← green bg (#2d8a4e), text (#40bf72)
└─────────┘
```

- Size: `0.74rem` font, `0.15rem 0.45rem` padding, `4px` radius
- Color driven by threshold config (positive / negative / neutral / warning)
- Optional delta sub-label (`▼ 8.22%`) below or inline

### 4.2 StatBar

Thin horizontal bar encoding proportion of max.

```
████████░░░░░░░░  68%
```

- Height: `6px` (inline HUD) / `10px` (detail panel)
- Track: `--color-surface-3`
- Fill: gradient from `--color-positive-dim` → `--color-positive` (or negative equivalent)
- Border-radius: `3px` / `5px`
- Accessible: `role="progressbar"`, `aria-valuenow`, `aria-valuemax`

### 4.3 DataTable

Dense multi-column sortable table.

- Row height: `44px`
- Alternating row bg: base / `rgba(255,255,255,0.02)`
- Hover row: `--color-surface-3`
- Header: `label-caps` style, `1px solid --color-border-subtle` bottom border, sticky
- Sort indicator: small caret icon inline with header label
- Stat columns: right-aligned, monospace, StatBadge or plain number
- Entity column: left-aligned, icon + name pattern

### 4.4 FilterChip

Horizontal scrollable pill row above DataTable.

```
[ Version ]  [ Positions ▾ ]  [ Rating ]  [ + ]
```

- Height: `32px`, `0.8rem` font
- Inactive: `--color-surface-2` bg, `--color-border-subtle` border
- Active: `--color-neutral-dim` bg, `--color-neutral` border + text
- Overflow: horizontal scroll with fade mask, no wrap

### 4.5 EntityCard

Used for city stats, plan cards, player-style entity views.

```
┌─────────────────────────────────────────┐
│ [icon/avatar]  Title          [badge]   │
│                Subtitle                  │
│ ─────────────────────────────────────── │
│ Stat key   ████████░░  value            │
│ Stat key   ████░░░░░░  value            │
│ ─────────────────────────────────────── │
│ [Tab A]  [Tab B]  [Tab C]               │
└─────────────────────────────────────────┘
```

- Background: `--color-surface-1`, `1px solid --color-border-subtle`, `8px` radius
- Tabs: text + underline active indicator, not pill-style

### 4.6 PlanProgressCard

Variant of EntityCard specific to master-plan progress (replaces static `progress.html`).

- Plan title + status badge
- Combined progress bar (overall %)
- Task count breakdown (Done / In Progress / Pending badges)
- Active step/stage/task summary table
- Expandable phase checklist
- Sibling coordination warning box (yellow-dim border)

### 4.7 MapOverlay *(TBD)*

Geographic data visualization. Two modes:
- **Heat mode:** Colored fill per cell, graduated by value (red scale or green scale)
- **Bubble mode:** Proportional circles positioned at city/region nodes

Implementation deferred — scoped when a wiki map or city-stats map page is planned.

### 4.8 NavBar

Top navigation bar for the public site.

```
[ Territory Developer ]   Wiki   Devlog   About   Install   [ Dashboard ↗ ]
```

- Background: `--color-base` + `1px solid --color-border-subtle` bottom
- Logo: game title in `heading-2` weight
- Links: `body` size, `--color-text-secondary`, hover `--color-text-primary`
- Dashboard link: obscure — not in primary nav until auth is built
- Mobile: hamburger collapse

---

## 5. Layout system

### 5.1 Grid and spacing

- Base unit: `4px`
- Content max-width: `1280px` centered
- Page padding: `1rem` (mobile) → `2rem` (tablet) → `3rem` (desktop)
- Card gap: `1.25rem`
- Section gap: `2.5rem`

### 5.2 Page templates

| Template | Sections | Used by |
|----------|---------|---------|
| **Landing** | Hero, Feature grid, Devlog preview, Install CTA | `/` |
| **Wiki article** | Breadcrumb, Article body, Sidebar (TOC + related), Image gallery | `/wiki/[slug]` |
| **Wiki index** | Filter chips, Article card grid | `/wiki` |
| **Devlog** | Post list with dates, tag filter | `/devlog` |
| **Devlog post** | Article body, prev/next, tags | `/devlog/[slug]` |
| **About** | Two-col (bio left, links right), project history timeline | `/about` |
| **Dashboard** | Plan progress cards, overall bar, BACKLOG summary | `/dashboard` (obscure) |
| **User portal** *(future)* | Auth gate, city stats, save list | `/portal` |

---

## 6. Motion

Minimal — data readability over animation. Rules:
- Transition duration ≤ `150ms` for hover/focus state changes
- No entrance animations on data tables (jarring on dense rows)
- Progress bar fill: `transition: width 300ms ease-out` only on initial mount
- Page transitions: none by default (Next.js default behavior)

---

## 7. Responsive strategy

- **Mobile (< 640px):** Single column. DataTable scrolls horizontally. FilterChips scroll horizontally. EntityCard stacks vertically.
- **Tablet (640–1024px):** Two-column card grid. Nav collapses.
- **Desktop (> 1024px):** Full multi-column layouts. Sticky sidebar in wiki articles.

Dashboard and portal surfaces are **desktop-primary** — layout degrade is acceptable on mobile for those pages.

---

## 8. Accessibility baseline

- Color is never the only semantic signal — icons or text labels accompany all color-coded badges.
- All interactive elements: `focus-visible` outline (`2px solid --color-neutral`, `2px` offset).
- Contrast: text-primary on surface-1 ≥ 7:1. text-secondary ≥ 4.5:1.
- StatBars and MapOverlays: `role="img"` with descriptive `aria-label`.

---

## 9. Tech stack (implementation)

| Layer | Choice |
|-------|--------|
| Framework | Next.js 14+ (App Router) |
| Language | TypeScript (strict) |
| Styling | Tailwind CSS v3 + CSS custom properties for semantic tokens |
| Component library | None — bespoke components per this spec |
| Charts / data viz | Recharts or Visx *(TBD — pick when first chart page is scoped)* |
| Icons | Lucide React (free, tree-shakeable) |
| Map | Leaflet.js or D3-geo *(TBD — scoped with MapOverlay page)* |
| Markdown (wiki/devlog) | MDX (`@next/mdx` or `contentlayer`) |
| Fonts | System UI stack — no custom font loading in v1 |

---

## 10. Cross-surface alignment with game UI

Both `web-ui-design-system.md` (this spec) and `ui-design-system.md §7` share the same visual language. When a game UX/UI master plan is authored:

1. Map `--color-positive` → `UiTheme.accentPositive`
2. Map `--color-negative` → `UiTheme.accentNegative`
3. Map `--color-neutral` → `UiTheme.accentPrimary`
4. StatBar pattern → in-game demand gauge (`RCI` bars)
5. EntityCard pattern → city details popup, building selector
6. DataTable pattern → future in-game stats overlay / economy table

The web spec has more implementation freedom (CSS, arbitrary component shapes). The game spec is constrained by Unity uGUI. Prefer keeping the **semantic intent** identical; let implementation differ.

---

## 11. Revision history

| Date | Change |
|------|--------|
| 2026-04-14 | Initial draft — seeded from visual reference analysis (4 images) + web platform brainstorm interview |
