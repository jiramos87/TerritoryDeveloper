# Territory Developer — Web

Next.js 14+ App Router workspace inside the Territory Developer monorepo. Serves public game site, DevOps progress dashboard, and future user portal — static-first hybrid on Vercel free tier.

## Overview

- **Stack:** Next.js 16+ (App Router), TypeScript strict, Tailwind CSS, ESLint.
- **Monorepo:** `web/` declared as npm workspace in root `package.json` alongside `tools/*`.
- **Hosting:** Vercel free tier. Build root `web/`. Deploy on push to `main`. Production URL: `https://web-nine-wheat-35.vercel.app`.
- **MCP server** (`territory-ia`): stdio dev-only — NOT consumed by this web app.

## Stack

| Layer | Tool |
|---|---|
| Framework | Next.js 16+ App Router |
| Language | TypeScript (strict) |
| Styling | Tailwind CSS v4 |
| Linting | ESLint (eslint-config-next) |
| Deploy | Vercel (free tier) |

## Local dev

```bash
cd web
npm run dev          # dev server at http://localhost:4000
```

Dev server starts Next.js with Turbopack. Page auto-updates on edit.

## Build

```bash
cd web
npm run build        # production build; exits 0 on success
```

Or from repo root using workspace composition:

```bash
npm run validate:web   # lint + typecheck + build
```

## Scripts

| Script | Command | Purpose |
|---|---|---|
| `dev` | `next dev` | Start dev server |
| `build` | `next build` | Production build |
| `start` | `next start` | Serve production build locally |
| `lint` | `eslint` | Lint all files |
| `typecheck` | `tsc --noEmit` | TypeScript strict check (no emit) |

Root-level alias: `npm run validate:web` runs lint + typecheck + build via `npm --prefix web` composition.

## Content conventions (stub)

- Public page copy lives under `web/content/**` and page-body JSX strings in `web/app/**/page.tsx`.
- Component logic, identifiers, comments, and commits follow normal (caveman) repo style.

## MDX page pattern

Static-page routes (landing, about, install, history, etc.) render MDX via **static import** + **frontmatter loader** (dual-source). Chosen over `next-mdx-remote` runtime compile — page slugs hardcoded, `@next/mdx` pipeline already wired, zero extra dep.

Pattern:

```tsx
// web/app/page.tsx
import Landing from '@/content/pages/landing.mdx';
import { loadMdxPage } from '@/lib/mdx/loader';

export default async function Home() {
  const { frontmatter } = await loadMdxPage('landing');
  return <main><h1>{frontmatter.title}</h1><Landing /></main>;
}
```

- Body rendered via `@next/mdx` typed-component import — compiled at build, zero runtime cost.
- Frontmatter (title/description/updated) read via `loadMdxPage(slug)` for validation + typed access — single-source.
- Sibling pages (about/install/history) follow identical shape.

## Dashboard

The progress dashboard (`web/app/dashboard/`) reads all master-plan Markdown files from the IA projects directory and renders them as structured plan data.

### Data loader

```ts
import { loadAllPlans } from '@/lib/plan-loader';

// Returns PlanData[] — one entry per master-plan file found.
const plans = await loadAllPlans();
```

**Contract:** `loadAllPlans(): Promise<PlanData[]>` — exported from `web/lib/plan-loader.ts`. Reads all `ia/projects/*master-plan*.md` files from repo root (substring match: `f.includes('master-plan') && f.endsWith('.md')`). Returns `[]` when no files match (does not throw).

### PlanData shape

Key fields (full schema: `web/lib/plan-loader-types.ts`):

| Field | Type | Description |
|---|---|---|
| `title` | `string` | Plan title extracted from the Markdown `# H1`. |
| `overallStatus` | `string` | Aggregate status label derived from stage statuses. |
| `steps[]` | `Step[]` | Ordered list of high-level steps, each with stages and tasks. |
| `allTasks[]` | `Task[]` | Flat list of every task across all steps — convenience for filtering. |

### Wrapper invariant

`tools/progress-tracker/parse.mjs` is the **authoritative parser**. `plan-loader.ts` is a read-only wrapper — it calls `parseMasterPlan()` via dynamic ESM `import()` but never modifies it. Schema drift between `plan-loader-types.ts` and `parse.mjs` JSDoc is a defect; fix `parse.mjs` first, then update `plan-loader-types.ts`.

### Glob pattern

Files matched: `ia/projects/*master-plan*.md` from repo root. This is a substring match (`includes('master-plan')`) so both `web-platform-master-plan.md` and `blip-master-plan.md` are included.

### RSC consumption pattern

`loadAllPlans()` is async and safe to call in React Server Components:

```tsx
// web/app/dashboard/page.tsx (server component — no 'use client' needed)
import { loadAllPlans } from '@/lib/plan-loader';

export default async function DashboardPage() {
  const plans = await loadAllPlans();

  if (plans.length === 0) {
    return <p>No plans found.</p>;
  }

  return (
    <main>
      {plans.map(plan => (
        <section key={plan.title}>
          <h2>{plan.title}</h2>
          <p>Status: {plan.overallStatus}</p>
        </section>
      ))}
    </main>
  );
}
```

### Empty-dir behavior

When `ia/projects/` contains no `*master-plan*.md` files, `loadAllPlans()` returns `[]`. This diverges intentionally from the CLI (`tools/progress-tracker/index.mjs` exits non-zero on empty); RSC callers should render an empty state rather than error.

## Tokens

Design token files live under `web/lib/tokens/`. All three files are consumed by `web/lib/tokens/index.ts` which exports the resolved `tokens` map.

### File layout

| File | Contents |
|---|---|
| `palette.json` | Color tokens — `raw` hex map + `semantic` alias map. |
| `type-scale.json` | `fontFamily` (mono + sans stacks) + `fontSize` scale (xs → 2xl, `[size, lineHeight]` tuples). |
| `spacing.json` | Spacing scale keyed by integer step (`"0"` → `"12"`), values in `rem`. |

### Semantic alias convention

`palette.json` has two top-level keys:

- **`raw`** — named hex values (`black`, `panel`, `text`, `red`, `amber`, `grey-500`, `green`).
- **`semantic`** — named intent keys (`bg-canvas`, `bg-panel`, `text-primary`, `text-accent-critical`, `text-accent-warn`, `text-muted`, `bg-status-*`, `text-status-*-fg`). Values use the alias pattern `{raw.<key>}` for indirection.

`resolveAlias(value, raw)` in `index.ts` matches `{raw.<key>}` and substitutes the hex. `resolveSemantic` maps the full semantic object. Consumers import `tokens.colors` (resolved `Record<string, string>`) — never the raw JSON directly.

**Example:**

```json
"bg-canvas": "{raw.black}"   // resolves → "#0a0a0a"
"text-accent-critical": "{raw.red}"  // resolves → "#d63838"
```

### Unity UI/UX consumption (stub — not yet shipped)

Future integration target: at Unity build time, read `palette.json` → iterate `semantic` keys → call `resolveAlias` (port to C# or pre-bake a flat JSON) → map hex strings to `UnityEngine.Color` via `ColorUtility.TryParseHtmlString`. Spacing scale maps to `Vector2` padding/margin values in Canvas layout groups. No Unity-side consumer exists yet; this section is the contract anchor for that work.

## Components

### Button

Polymorphic primitive at `web/components/Button.tsx` — renders `<button>` default, `<a>` when `href` present. Named export `Button` + `ButtonProps` (match `BadgeChip` / `FilterChips` / `DataTable` convention — no default export).

- **Variant map:** `primary` → `bg-bg-status-progress text-text-status-progress-fg` (amber CTA, reuses existing status-progress pair; palette has no `accent-info` alias). `secondary` → `bg-bg-panel text-text-primary border border-text-muted/40`. `ghost` → `bg-transparent text-text-muted hover:text-text-primary`.
- **Size map:** `sm` → `px-2 py-1 text-xs`; `md` → `px-3 py-1.5 text-sm`; `lg` → `px-4 py-2 text-base`. Uses existing `@theme` spacing + type steps.
- **Token verification rule:** before authoring token-consuming components, grep `web/app/globals.css @theme` block for real alias names. Master-plan / spec prose historically cites phantom names (`accent-info`, `border-border`, `text-canvas`) that do not exist. Fix spec, don't invent tokens.
- **Tailwind v4 double-prefix:** utility classes like `bg-bg-panel` / `text-text-primary` are intentional — when the semantic CSS var name already begins with `bg-` / `text-`, Tailwind prepends its own utility prefix. Not a typo.
- **No `clsx` dep:** `web/package.json` does not include `clsx`; sibling primitives (`FilterChips`, `BadgeChip`) concat via template literals. Author new primitives with template-literal concat + optional caller `className` tail — no bundle-size / lockfile churn.
- **Next 16 `<Link>` wrapping:** Button primitive stays tag-agnostic. In-component `<a>` is for raw external `href` only. For in-app routes, caller wraps: `<Link href="…"><Button>…</Button></Link>`.

### DataTable

Generic table primitive at `web/components/DataTable.tsx`. Named exports `DataTable<T>` + `Column<T>` + `PctColumnConfig<T>`.

**Props:**

| Prop | Type | Required | Description |
|---|---|---|---|
| `columns` | `Column<T>[]` | yes | Column definitions — `key`, `header`, optional `render` callback, optional `sortable`/`sortDirection`. |
| `rows` | `T[]` | yes | Data rows. |
| `getRowKey` | `(row: T, index: number) => string \| number` | no | Row key extractor; falls back to index. |
| `statusCell` | `(row: T) => ReactNode` | no | Optional leading status cell rendered before `columns`. |
| `pctColumn` | `PctColumnConfig<T>` | no | When present, appends a trailing `StatBar` column derived from a numeric field. |

**`pctColumn` shape:**

```ts
type PctColumnConfig<T> = {
  dataKey: keyof T   // numeric field on T used as StatBar value
  label?:  string    // column header + StatBar label (default: 'Progress')
  max?:    number    // StatBar max (default: 100)
}
```

**Minimal usage with `pctColumn`:**

```tsx
import { DataTable } from '@/components/DataTable'
import type { Column } from '@/components/DataTable'

type Row = { id: string; name: string; pct: number }

const COLS: Column<Row>[] = [
  { key: 'id',   header: 'ID' },
  { key: 'name', header: 'Name' },
]

<DataTable<Row>
  columns={COLS}
  rows={rows}
  getRowKey={(r) => r.id}
  pctColumn={{ dataKey: 'pct', label: 'Progress', max: 100 }}
/>
```

Non-finite / missing numeric values coerce to `0` (via internal `toFiniteNumber`).

### FilterChips

Filter chip row primitive at `web/components/FilterChips.tsx`. Named exports `FilterChips` + `Chip` + `FilterChipsProps`.

**Multi-select semantics:** each chip's `active` state is evaluated independently — any number of chips may be active simultaneously. No single-active invariant; callers pass a plain `Chip[]` array with each element's `active` computed upstream (e.g., from URL search params).

**Props:**

| Prop | Type | Required | Description |
|---|---|---|---|
| `chips` | `Chip[]` | yes | Array of chip descriptors; order preserved in render output. |

**`Chip` shape:**

```ts
export type Chip = { label: string; active: boolean; href?: string }
```

- `active` — independent per chip; `true` → active visual style (`bg-panel text-primary`); `false` → muted (`bg-canvas text-muted`).
- `href?` — optional. When present: chip renders as `<a href={href}>` (navigable link, e.g., filter URL). When absent: chip renders as `<span>` (non-navigable static state indicator).

**SSR/RSC-compatible:** no `'use client'` directive; no browser hooks. Safe to use directly inside Server Components.

**Minimal usage:**

```tsx
import { FilterChips } from '@/components/FilterChips'
import type { Chip } from '@/components/FilterChips'

const chips: Chip[] = [
  { label: 'All',     active: true,  href: '/dashboard' },
  { label: 'In Progress', active: false, href: '/dashboard?status=in-progress' },
  { label: 'Done',   active: false, href: '/dashboard?status=done' },
]

<FilterChips chips={chips} />
```

### Dashboard multi-select filtering

Filter URL helpers at `web/lib/dashboard/filter-params.ts`:

- **`parseFilterValues(search: URLSearchParams, key: string): string[]`** — reads a single query param, splits on commas, deduplicates, trims. Canonical URL form: `?status=Draft,In+Progress` (comma-delimited single param per dimension).
- **`toggleFilterParam(currentSearch: string, key: string, value: string): string`** — adds `value` to dimension `key` if absent; removes it if present; returns updated query string (empty string when no filters remain).

**URL convention:** each filter dimension occupies one query parameter with comma-delimited values. Example: `?status=Draft,In+Progress&phase=1`. OR within dimension, AND across dimensions. `parseFilterValues` produces the `string[]` arrays consumed by `filterPlans`.

**`anyFilter` predicate (dashboard page):** `multi.plan.length + multi.status.length + multi.phase.length > 0`. Drives conditional render of the "Clear filters" control.

**Clear filters control:** `<Button variant="ghost" size="sm" href="/dashboard">Clear filters</Button>` — rendered only when `anyFilter === true`; navigates to bare `/dashboard` (strips all params). Label "Clear filters" is user-facing rendered UI (caveman-exception per `agent-output-caveman.md`).

### PlanChart

D3-driven grouped-bar chart — per-step status breakdown (pending / in-progress / done). Two-file split:

- `web/components/PlanChart.tsx` — `'use client'`; D3 SVG draw via `scaleBand` (outer + inner) + `scaleLinear` + `axisBottom` + `axisLeft`. Static `480×220` viewport.
- `web/components/PlanChartClient.tsx` — `'use client'` wrapper holding `dynamic(() => import('./PlanChart'), { ssr: false, loading: <skeleton /> })`. RSC dashboard imports this wrapper.

**Why the wrapper:** Next 16 App Router forbids `next/dynamic({ ssr: false })` from server components — D3 mutates the DOM (`d3.select(svgRef.current)`), not SSR-safe. Direct `dynamic()` call from `page.tsx` errors at build time.

**Props:**

| Prop | Type | Required | Description |
|---|---|---|---|
| `data` | `PlanChartDatum[]` | yes | One entry per step; counts per status. Empty array → `<p>No tasks</p>` placeholder. |

**`PlanChartDatum` shape:**

```ts
export interface PlanChartDatum {
  label:      string
  pending:    number
  inProgress: number
  done:       number
}
```

**Fill tokens:** `var(--color-bg-status-pending)` / `var(--color-bg-status-progress)` / `var(--color-bg-status-done)` — real `@theme` aliases in `web/app/globals.css`. Axis + legend text use `var(--color-text-muted)`. No inline hex.

**Loading skeleton:** `<div className="h-[220px] bg-bg-panel animate-pulse rounded" />` — matches chart height; prevents layout shift on hydration.

**Empty state:** `data.length === 0` → `<p className="text-text-muted text-sm">No tasks</p>` (early return, no SVG mount).

### Sidebar

App-wide nav at `web/components/Sidebar.tsx`. Wired into `web/app/layout.tsx` inner flex row (`<div className="flex flex-1 min-h-0"><Sidebar /><main .../></div>`).

- **Dependency:** `lucide-react` — named imports only (`Home`, `BookOpen`, `Newspaper`, `LayoutDashboard`, `Menu`, `X`); tree-shake friendly, no barrel import.
- **Client directive:** `'use client'` required — `usePathname()` (active link) + `useState` (mobile overlay toggle) both need browser runtime.
- **Active route:** matches `usePathname()` against link `href`; matching link styled inline via `{ color: tokens.colors['text-accent-warn'], backgroundColor: tokens.colors['bg-panel'] }`. No plain `text-accent` alias exists — palette exposes `text-accent-warn` (amber) + `text-accent-critical` (red) only.
- **Mobile overlay (<md):** hamburger button `md:hidden fixed top-4 left-4 z-50` toggles `open` state. Nav wrapper holds `fixed inset-y-0 left-0 w-48 z-40 transform transition-transform`; closed = `-translate-x-full`, open = `translate-x-0`. Link `onClick` closes overlay.
- **Desktop (≥md):** same `<nav>` element — responsive overrides `md:static md:translate-x-0` promote it out of fixed positioning. No `hidden md:flex` wrapper in `layout.tsx` — Sidebar owns its own responsive classes.
- **Token consumption:** inline `style={{ backgroundColor: tokens.colors['bg-canvas'], color: tokens.colors['text-primary'] }}` via `@/lib/tokens` map. Keys like `bg-canvas` / `bg-panel` / `text-primary` / `text-muted` / `text-accent-warn` are JSON semantic aliases resolved at build, NOT Tailwind utility classes. No inline hex.

## Caveman exception boundary

Full English (marketing-style prose) applies **only** to:

- User-facing rendered text under `web/content/**`
- Page-body JSX strings in `web/app/**/page.tsx`

Everything else — app shell code, component identifiers, TypeScript comments, commits, IA-authored docs — stays caveman style.

Authority: `ia/rules/agent-output-caveman.md` §exceptions (authoring surface rule).

## Deploy

Vercel project linked via dashboard. Build root: `web/`. Framework preset: Next.js. Production branch: `main`.

> **Note:** Vercel URL to be embedded here after first production deploy.

CI: `npm run validate:all` at repo root includes `validate:web` (lint + typecheck + build). Push to `main` triggers Vercel production deploy.

## Links

- Repo root `CLAUDE.md` §Web — workspace pointer + dev commands + caveman boundary
- Repo root `AGENTS.md` §Web — agent onboarding for web surface
- `ia/projects/web-platform-master-plan.md` — orchestrator (permanent)
- `ia/rules/agent-output-caveman.md` — caveman rule + exceptions
