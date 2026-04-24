### Stage 23 — Visual design layer / Prose + surface primitives


**Status:** Done — TECH-622…TECH-625 closed 2026-04-22 (archived). Heading, Prose, Surface + motion CSS, dev `/design-system` page (`app/(dev)/design-system/`).

**Objectives:** Author `Heading` + `Prose` (type primitives) + `Surface` (panel with optional motion Client island) + dev-only `app/(dev)/design-system/page.tsx` showcase (URL `/design-system`). Additive — no page adoption yet; zero existing component changes.

**Exit:**

- `web/components/type/Heading.tsx`: `level` prop (10 levels); maps to `--ds-font-size-{level}` via Tailwind v4 arbitrary value; HTML element derived from level; pure RSC.
- `web/components/type/Prose.tsx`: RSC wrapper; vertical rhythm via `[&>*+*]:mt-[var(--ds-spacing-md)]`; pure RSC; accepts `className?`.
- `web/components/surface/Surface.tsx`: `tone` + `padding` + `motion` props; default `motion="none"` → RSC-compat div; non-none → `'use client'` island + `useEffect` `data-mounted` + CSS transition rules in `globals.css` per extensions-doc Example 2 (including `prefers-reduced-motion` collapse); B2 guard: default `motion="none"`.
- `web/app/(dev)/design-system/page.tsx`: `notFound()` in production; renders all primitives + alias swatches + motion demo; `noindex` meta; unlinked from Sidebar (NB2). Route `/design-system` (underscore-prefixed `app/_…` segments are private in App Router and do not create URLs).
- `npm run validate:web` green.
- Phase 1 — Type primitives (`Heading.tsx` + `Prose.tsx`).
- Phase 2 — Surface primitive + showcase (`Surface.tsx` + `(dev)/design-system/page.tsx`).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T23.1 | **TECH-622** | Done | Author `web/components/type/Heading.tsx` — `level: 'display' | 'h1' | 'h2' | 'h3' | 'body-lg' | 'body' | 'body-sm' | 'caption' | 'mono-code' | 'mono-meta'`; maps level → HTML element (`display/h1` → `<h1>`, `h2` → `<h2>`, `h3` → `<h3>`, `body-*` → `<p>`, `caption/mono-*` → `<span>`); applies `text-[var(--ds-font-size-{level})]` Tailwind v4 arbitrary value; optional `weight?` override class; optional `className?` passthrough; pure RSC. |
| T23.2 | **TECH-623** | Done | Author `web/components/type/Prose.tsx` — RSC wrapper; accepts `children` + optional `className`; applies Tailwind v4 CSS vertical rhythm: `[&>*+*]:mt-[var(--ds-spacing-md)]`; cite `design-system.md` §5 component map in JSDoc; zero inline styles; pure RSC. |
| T23.3 | **TECH-624** | Done | Author `web/components/surface/Surface.tsx` — `tone: 'raised' | 'sunken' | 'inset'` → `bg-[var(--ds-surface-{tone})]`; `padding: 'sm' | 'md' | 'lg' | 'section'` → `p-[var(--ds-spacing-{padding})]`; `motion?: 'none' | 'subtle' | 'gentle' | 'deliberate'` default `'none'`; `motion="none"` → pure RSC div; non-none → `'use client'` + `useEffect(() => setMounted(true), [])` + `data-mounted="true"`; append CSS transition rules + `prefers-reduced-motion: reduce` collapse to `globals.css` per extensions-doc Example 2; B2 guard enforced via prop default. |
| T23.4 | **TECH-625** | Done | Author `web/app/(dev)/design-system/page.tsx` — `if (process.env.NODE_ENV === 'production') { notFound() }` guard (NB2); renders: all 10 `Heading` levels, `Prose` block with sample body text, `Surface` matrix (all tones × paddings), motion demo per duration, `BadgeChip` status token swatches, `--ds-*` CSS var reference table; `export const metadata = { robots: { index: false } }`; NOT added to `Sidebar.tsx` `LINKS`. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-622"
  title: |
    Author `web/components/type/Heading.tsx` — `level: 'display' | 'h1' | 'h2' | 'h3' | 'body-lg' | 'body' | 'body-sm' | 'caption' | 'mono-code' | 'mono-meta'`; maps level → HTML element (`display/h1` → `<h1>`, `h2` → `<h2>`, `h3` → `<h3>`, `body-*` → `<p>`, `caption/mono-*` → `<span>`); applies `text-[var(--ds-font-size-{level})]` Tailwind v4 arbitrary value; optional `weight?` override class; optional `className?` passthrough; pure RSC.
  priority: medium
  notes: |
    RSC type primitive; uses `--ds-font-size-*` from Stage 22 token pipeline. New file `web/components/type/Heading.tsx`.
  depends_on: []
  related:
    - "TECH-623"
    - "TECH-624"
    - "TECH-625"
  stub_body:
    summary: |
      Add `Heading` with ten-level scale; semantic HTML from level; `text-[var(--ds-font-size-{level})]`; no client hooks.
    goals: |
      1. Export typed `level` + optional `weight` + `className` passthrough.
      2. Correct tag map per master-plan (display + h1 share `<h1>`, body levels `<p>`, etc.).
      3. `npm run validate:web` green.
    systems_map: |
      - `web/components/type/Heading.tsx` (new)
      - `web/lib/design-system.md` (JSDoc cite §5)
      - `web/app/globals.css` (read-only; `--ds-font-size-*` already in `@theme`)
    impl_plan_sketch: |
      ### Phase 1 — Heading
      - [ ] Add component + export; run `npm run validate:web`.
- reserved_id: "TECH-623"
  title: |
    Author `web/components/type/Prose.tsx` — RSC wrapper; accepts `children` + optional `className`; applies Tailwind v4 CSS vertical rhythm: `[&>*+*]:mt-[var(--ds-spacing-md)]`; cite `design-system.md` §5 component map in JSDoc; zero inline styles; pure RSC.
  priority: medium
  notes: |
    Stack spacing between children; JSDoc pointer to `design-system.md` §5. New file `web/components/type/Prose.tsx`.
  depends_on: []
  related:
    - "TECH-622"
    - "TECH-624"
    - "TECH-625"
  stub_body:
    summary: |
      Add `Prose` RSC wrapper with sibling vertical spacing via `mt-[var(--ds-spacing-md)]` between direct children; no inline styles.
    goals: |
      1. Children + optional `className` API.
      2. JSDoc cites `web/lib/design-system.md` §5.
      3. `npm run validate:web` green.
    systems_map: |
      - `web/components/type/Prose.tsx` (new)
      - `web/lib/design-system.md` (cite)
    impl_plan_sketch: |
      ### Phase 1 — Prose
      - [ ] Add component; run `npm run validate:web`.
- reserved_id: "TECH-624"
  title: |
    Author `web/components/surface/Surface.tsx` — `tone: 'raised' | 'sunken' | 'inset'` → `bg-[var(--ds-surface-{tone})]`; `padding: 'sm' | 'md' | 'lg' | 'section'` → `p-[var(--ds-spacing-{padding})]`; `motion?: 'none' | 'subtle' | 'gentle' | 'deliberate'` default `'none'`; `motion="none"` → pure RSC div; non-none → `'use client'` + `useEffect(() => setMounted(true), [])` + `data-mounted="true"`; append CSS transition rules + `prefers-reduced-motion: reduce` collapse to `globals.css` per extensions-doc Example 2; B2 guard enforced via prop default.
  priority: medium
  notes: |
    Default motion none (RSC); non-none = client island + `globals.css` motion CSS; B2. New file + `globals.css` append.
  depends_on: []
  related:
    - "TECH-622"
    - "TECH-623"
    - "TECH-625"
  stub_body:
    summary: |
      Surface container with `tone` and `padding`; optional motion; client split only when motion not `none`; CSS transitions in `globals.css` with reduced-motion collapse.
    goals: |
      1. RSC default path when `motion="none"`.
      2. Client path sets `data-mounted` after `useEffect` for animation hooks.
      3. `globals.css` rules match extensions doc Example 2; `validate:web` green.
    systems_map: |
      - `web/components/surface/Surface.tsx` (new)
      - `web/app/globals.css` (edit — motion block)
      - `docs/web-platform-post-mvp-extensions.md` (Example 2 ref)
    impl_plan_sketch: |
      ### Phase 1 — Surface + CSS
      - [ ] Add component; extend `globals.css`; run `npm run validate:web`.
- reserved_id: "TECH-625"
  title: |
    Author `web/app/(dev)/design-system/page.tsx` — `if (process.env.NODE_ENV === 'production') { notFound() }` guard (NB2); renders: all 10 `Heading` levels, `Prose` block with sample body text, `Surface` matrix (all tones × paddings), motion demo per duration, `BadgeChip` status token swatches, `--ds-*` CSS var reference table; `export const metadata = { robots: { index: false } }`; NOT added to `Sidebar.tsx` `LINKS`.
  priority: medium
  notes: |
    Dev-only review page; imports Heading, Prose, Surface, BadgeChip; noindex; NB2 production guard. Not linked in `Sidebar`.
  depends_on: []
  related:
    - "TECH-622"
    - "TECH-623"
    - "TECH-624"
  stub_body:
    summary: |
      `/design-system` RSC page: production `notFound()`, matrix demos, metadata `robots.index=false`, unlinked in nav.
    goals: |
      1. Full primitive showcase per Stage Exit.
      2. `BadgeChip` + CSS var table included.
      3. `npm run validate:web` green.
    systems_map: |
      - `web/app/(dev)/design-system/page.tsx` (new)
      - `web/components/Sidebar.tsx` (no edit — do not add link)
      - `web/components/BadgeChip.tsx` (import)
    impl_plan_sketch: |
      ### Phase 1 — Dev showcase
      - [ ] Add page + metadata; run `npm run validate:web`.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
