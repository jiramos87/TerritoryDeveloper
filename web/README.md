# Territory Developer — Web

Next.js 14+ App Router workspace inside the Territory Developer monorepo. Serves public game site, DevOps progress dashboard, and future user portal — static-first hybrid on Vercel free tier.

## Overview

- **Stack:** Next.js 16+ (App Router), TypeScript strict, Tailwind CSS, ESLint.
- **Monorepo:** `web/` declared as npm workspace in root `package.json` alongside `tools/*`.
- **Hosting:** Vercel free tier. Build root `web/`. Deploy on push to `main`.
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
- Stage 1.2 will introduce `web/lib/tokens/palette.json` — design token export contract documented there.

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
