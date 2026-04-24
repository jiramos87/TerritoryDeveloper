### Stage 9 — Dashboard improvements + UI polish / Navigation sidebar + icon system


**Status:** Done (TECH-223…TECH-226 all closed 2026-04-16)

**Objectives:** Install `lucide-react`; author `Sidebar` client component with icon + label links to all top-level routes; add active route highlighting via `usePathname`; implement responsive behavior (collapsed on mobile via slide/overlay, always expanded on ≥md); wire into root layout.

**Exit:**

- `lucide-react` added to `web/package.json` deps; `web/components/Sidebar.tsx` (new) renders vertical nav list with icon + label per route (`Home`, `BookOpen`, `Newspaper`, `LayoutDashboard` icons); design token classes only (no inline hex).
- Active route link styled with `text-accent`/`bg-panel` via `usePathname()`; mobile hamburger toggle (`Menu`/`X`) collapses/expands sidebar via `useState`; `'use client'` component.
- `web/app/layout.tsx` restructured as `flex min-h-screen` row; `<Sidebar />` in left slot; `<main className="flex-1 min-w-0">` wraps `{children}`; sidebar `hidden md:flex` on desktop, overlay on mobile.
- `validate:all` green; `web/README.md §Components` Sidebar entry added (lucide dep, `'use client'` rationale, active state pattern).
- Phase 1 — Sidebar component (markup + icons + active state + mobile toggle).
- Phase 2 — Layout integration + docs + validation.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T9.1 | **TECH-223** | Done (archived) | Install `lucide-react` into `web/package.json`; author `web/components/Sidebar.tsx` base — `<nav>` with vertical `<Link>` list per route (`/` → `Home`, `/wiki` → `BookOpen`, `/devlog` → `Newspaper`, `/dashboard` → `LayoutDashboard`); each link: icon (24px) + label text; design token classes (`bg-canvas`, `text-primary`, `text-muted`, hover `text-primary`); SSR-compatible static markup; no active state yet. |
| T9.2 | **TECH-224** | Done (archived) | Convert `web/components/Sidebar.tsx` to `'use client'`; add `usePathname()` active highlight (`text-accent-warn bg-panel rounded` on matching href — token corrected during kickoff, no plain `text-accent` alias exists in palette); add mobile collapse toggle — `useState(false)` `open` bool; `Menu`/`X` icon button visible on `<md`; collapsed: sidebar `-translate-x-full` (kept in DOM for transition); expanded: fixed overlay with `z-50 bg-canvas` on mobile; transitions via Tailwind `transition-transform`. |
| T9.3 | **TECH-225** | Done (archived) | Wire `<Sidebar />` into `web/app/layout.tsx` — preserve existing `<html>` Geist font vars + `h-full antialiased`, `<body className="min-h-full flex flex-col">`, footer (Devlog + RSS), metadata export; insert inner horizontal row `<div className="flex flex-1 min-h-0"><Sidebar /><main className="flex-1 min-w-0 overflow-auto">{children}</main></div>` as first `<body>` child; render `<Sidebar />` directly (no `hidden md:flex` wrapper — Sidebar root `<nav>` already owns `fixed ... md:static md:translate-x-0 w-48`); smoke `/`, `/wiki`, `/devlog`, `/dashboard`, `/design`, `/about`, `/install`, `/history` for no horizontal scroll, footer pinned below row, TECH-224 mobile overlay intact. |
| T9.4 | **TECH-226** | Done (archived) | Add `web/README.md §Components` Sidebar entry — documents lucide-react dependency, `'use client'` rationale (`usePathname` + `useState`), mobile overlay pattern, and desktop `md:static md:translate-x-0` strategy (same-element, not `hidden md:flex` wrapper); active-route token = `text-accent-warn`; token consumption via inline `style` + `tokens.colors[...]` JS map (not Tailwind utility classes); `validate:all` green; confirm no TypeScript errors from lucide-react imports. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
