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

## Routes

Canonical route-list for the `web/` workspace.

| Route | Purpose | Auth | Render |
|---|---|---|---|
| `/` + `/about` + `/install` + `/history` | Public static pages | none | MDX (static import) |
| `/wiki/[...slug]` | Glossary-backed wiki | none | MDX dynamic |
| `/devlog` + `/devlog/[slug]` | Devlog index + post | none | MDX dynamic |
| `/dashboard` | Master-plan progress dashboard | gated (bypass via `DASHBOARD_AUTH_SKIP=1`) | RSC |
| `/dashboard/releases` | Release picker | gated (TECH-358 matcher) | RSC |
| `/dashboard/releases/:releaseId/progress` | Release progress tree | gated (TECH-358 matcher) | RSC + `PlanTree` Client island |

Auth gate for `/dashboard*` inherits from proxy matcher widened in TECH-358.

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

### Diagnostics — dashboard display diverges from master-plan markdown

Recipe for wrong stage status / ghost tasks / off counts. Read linearly; do NOT spawn `Explore` — the files below are the whole surface.

1. **Data source + env gate:** read `web/lib/plan-loader.ts` (data source) + `web/lib/plan-parser.ts` (parse + `deriveHierarchyStatus`).
2. **Parser dump:** run `cd web && npm run plan-parser:verify` to dump per-plan / per-stage `{status, done/total}` straight from the parser.
3. **URL filters:** check the URL for `?status=` / `?plan=` filters — `filterPlans` in `web/app/dashboard/page.tsx` drops tasks per-status before render.
4. **Regression tests:** adversarial markdown lives under `web/lib/__tests__/plan-parser.test.ts` — add a failing case before fixing.

### Live dashboard freshness

`/dashboard` fetches `ia/projects/*master-plan*.md` from GitHub raw via Next.js ISR (5-min revalidate). Push to deployed branch → visible within ~5 min without redeploy. Manual `npm run deploy:web` only when instant refresh or code change required.

## Portal

App-infra surface — Neon DB provider wired at Step 5 (TECH-252 / TECH-254). Architecture-only tier: no migrations, no live queries, no auth flow yet.

### Database provider

Postgres provider: **Neon free (Launch tier)** — pooled connections 100, storage 0.5 GB, region us-east-1 (matches Vercel default). Driver pin: `@neondatabase/serverless@^1.0.2` (HTTP/WebSocket, edge-safe, no native bindings).

Full rationale + alternatives rejected: `ia/projects/web-platform-master-plan.md` §Orchestrator Decision Log (2026-04-16).

### Connection pool pattern

Lazy singleton via `getSql()` — connection not opened at import time; opened on first call. `sql` tagged-template Proxy delegates to `getSql()` on first use.

```ts
import { sql } from '@/lib/db/client'

// Lazy — no connection until this line executes:
const rows = await sql`SELECT 1 AS ping`

// Or explicit accessor (same pool):
import { getSql } from '@/lib/db/client'
const db = getSql()
const rows = await db`SELECT 1 AS ping`
```

Build-time safety: `next build` succeeds without `DATABASE_URL` set — client module imports cleanly; error thrown only on first query invocation at runtime.

Source: `web/lib/db/client.ts`.

### DATABASE_URL env contract

Required format: Neon pooled connection string (includes `?pgbouncer=true&connect_timeout=10` or equivalent pool params).

| Vercel scope | Status | Action |
|---|---|---|
| Production | `[HUMAN ACTION]` | Set in Vercel dashboard → Project Settings → Environment Variables → Production |
| Preview | `[HUMAN ACTION]` | Set for Preview scope in same panel |
| Development | `[HUMAN ACTION]` | Set for Development scope or wire locally |

Local contributors: create `web/.env.local` (not shipped — gitignored) with:

```
DATABASE_URL=<your-neon-pooled-connection-string>
```

`.env.example` not shipped at this tier — contributors source their own Neon free project.

### Payment gateway (deferred)

Architecture slot reserved. No provider chosen. Decision deferred to Q10 (no timeline). No code at this tier — Stage 5.2+ scope.

### Migration tooling

`npm run db:generate` (drizzle-kit) reads `web/lib/db/schema.ts` and writes SQL artifacts to `web/drizzle/`. Offline-safe — no live `DATABASE_URL` required; drizzle-kit generates from schema only.

Output artifacts: timestamped `.sql` migration file + `meta/_journal.json` (diff state) + `meta/0000_snapshot.json` (typed schema snapshot).

**Commit stance:** `web/drizzle/` is committed to the repo (NOT gitignored). Migration files = source-controlled DB state per drizzle convention; PR review needs SQL diffs visible.

Step 5 architecture-only — no `db:migrate` script yet. Migration runner lands in post-Step-5 portal-launch work.

---

**Step 5 boundary:** architecture-only — no migrations run, no live queries in production, no auth flow. Stage 5.2 files schema + migrations; Stage 5.3 files middleware + auth.

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

## Local development auth bypass

- Knob: `DASHBOARD_AUTH_SKIP=1` in `web/.env.local` (gitignored — local only).
- Effect: `web/middleware.ts` short-circuits on env-var truthy → `/dashboard` reachable without a `portal_session` cookie → no cookie ritual each browser session.
- Setup: copy `web/.env.local.example` → `web/.env.local`; set `DASHBOARD_AUTH_SKIP=1`.
- Prod warning: **NEVER** set `DASHBOARD_AUTH_SKIP` on Vercel — prod must stay gated; env var absent on Vercel (`undefined`) → bypass never fires.
- Surface: `web/middleware.ts` reads env var before cookie lookup.

## Backend logic / frontend render boundary

Rule authority: [`ia/rules/web-backend-logic.md`](../ia/rules/web-backend-logic.md).

**Derivation, aggregation, parsing, status-inference, and any non-trivial transformation live in backend modules** — `web/lib/**`, route handlers, server components' data-loading paths. Frontend / client components consume already-shaped props and render only.

Practical contract:

- Status badges, progress counts, rollup percentages, "done / total" figures → computed in loader/parser, passed as props.
- If a component needs data the loader does not yet expose: **add a field to the loader's return type**; do not compute it in-component.
- Client-side reactive state (form inputs, expand/collapse, modal open) is fine in client components — that is UI state, not business logic.

Entry points for pre-computed metrics:

| Lib function | Returns | Used by |
|---|---|---|
| `computePlanMetrics(plan)` | `PlanMetrics` — completedCount, totalCount, statBarLabel, chartData, stepCounts | `web/app/dashboard/page.tsx` |

Source: `web/lib/plan-parser.ts`. Types: `web/lib/plan-loader-types.ts`.

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

## E2E Testing (Playwright)

Playwright scaffold is wired at `web/` — no tests authored yet (Stage 6.2 owns route coverage; Stage 6.3 owns dashboard filter suites).

### Local run

```bash
# 1. Install Chromium binary (one-time, local dev machine only — NOT in validate:all):
npx playwright install chromium

# 2. Start dev server in one terminal:
cd web && npm run dev

# 3. Run tests in another terminal (empty tests/ dir exits 0):
cd web && npm run test:e2e
```

Note: `test:e2e` + `test:e2e:ci` both pass `--pass-with-no-tests` so an empty `tests/` dir does not break CI before Stage 6.2 authors the first spec. Playwright defaults to exit 1 on "no tests found" — keep the flag until at least one route spec lands.

### `PLAYWRIGHT_BASE_URL` contract

`playwright.config.ts` resolves the base URL from the environment:

| Context | Value |
|---|---|
| Local dev (default) | `http://localhost:4000` |
| Vercel preview | `https://$VERCEL_URL` (inject via CI env) |
| Production smoke | `https://web-nine-wheat-35.vercel.app` (or custom domain) |

Set before running:

```bash
PLAYWRIGHT_BASE_URL=https://$VERCEL_URL npm run test:e2e
```

### Vercel preview injection

In a Vercel-integrated CI step, inject the deployment URL before running e2e:

```bash
PLAYWRIGHT_BASE_URL=https://$VERCEL_URL npx playwright test --reporter=github
```

`$VERCEL_URL` is set automatically by Vercel for preview deployments.

### CI bootstrap

Playwright browsers are NOT included in `node_modules`. Run once per CI environment before executing tests:

```bash
npx playwright install --with-deps chromium
```

This downloads Chromium + system deps (~200 MB). Reason it is opt-in and NOT wired into `validate:all` — keeps the default chain fast and Chromium-free for contributors who do not need e2e.

### Per-route test-file convention

- One test file per route under `web/tests/`.
- File naming: `{route-slug}.spec.ts` (e.g., `dashboard.spec.ts`, `home.spec.ts`).
- Import `test`, `expect` from `@playwright/test` — no global setup file needed for scaffold-level tests.
- Use `page.goto('/')` (relative) — `baseURL` from `playwright.config.ts` resolves the full URL.

### Root-level script (repo)

`npm run validate:e2e` at repo root composes `npm --prefix web run test:e2e:ci`. Does NOT run as part of `validate:all` (Chromium opt-in boundary).

## Next.js quirks (version-specific gotchas)

- `usePathname()` from `next/navigation` returns non-nullable `string` in Next 16 (was `string | null` in 13/14) — remove null guards.
- App Router RSC forbids `ssr: false` inside `next/dynamic()`. Wrap in a thin `'use client'` component; import the wrapper from RSC.
- `serverExternalPackages` accepts npm package names only — workspace-relative files outside `node_modules/` need `outputFileTracingIncludes` route→glob mapping instead.
- **Project convention**: proxy file is `web/proxy.ts` + `export function proxy` (not `middleware.ts`) — dev server fails "Cannot find the middleware module" if named the old way.
- Proxy-based lazy neon export: `new Proxy({} as NeonQueryFunction, { get, apply })` wrapping `getSql()` defers `DATABASE_URL` read until first invocation — safe through `next build` with no env set.

## Links

- Repo root `CLAUDE.md` §Web — workspace pointer + dev commands + caveman boundary
- Repo root `AGENTS.md` §Web — agent onboarding for web surface
- `ia/projects/web-platform-master-plan.md` — orchestrator (permanent)
- `ia/rules/agent-output-caveman.md` — caveman rule + exceptions
