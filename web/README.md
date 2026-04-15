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
npm run dev          # dev server at http://localhost:3000
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
