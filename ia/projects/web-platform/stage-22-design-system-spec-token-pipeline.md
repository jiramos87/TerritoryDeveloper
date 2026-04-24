### Stage 22 — Visual design layer / Design system spec + token pipeline


**Status:** Done — 4 / 4 tasks closed (TECH-618..TECH-621).

**Objectives:** Author `web/lib/design-system.md` spec; derive `web/lib/design-tokens.ts` (TS const); extend `globals.css` `@theme` with `ds-*` CSS custom properties; unit-test scale monotonicity + alias resolution + reduced-motion.

**Exit:**

- `web/lib/design-system.md`: §1–§6 complete; cites Dribbble + Shopify refs from extensions doc §8; game-accent subset identified from `palette.json` with WCAG AA verification; ≤ ~10 pages.
- `web/lib/design-tokens.ts`: `typeScale` (10 levels) + `spacing` (9 stops) + `motion` (4 durations + `reducedMotion: { duration: 0 }`) + `text` + `surface` + `accent` exports; imports `palette.json`; zero mutation.
- `web/app/globals.css` `@theme` block: `--ds-font-size-*`, `--ds-spacing-*`, `--ds-duration-*`, `--ds-text-*`, `--ds-surface-*`, `--ds-accent-*` CSS custom properties appended; existing entries untouched.
- `web/lib/__tests__/design-tokens.test.ts`: typeScale monotonically decreasing rem values, 9 spacing stops, motion keys complete, `reducedMotion.duration === 0`, alias hex values match palette.
- `npm run validate:web` green.
- Phase 1 — Spec authorship + game-accent derivation (`design-system.md` only; no code).
- Phase 2 — Token pipeline (`design-tokens.ts` + `globals.css` `@theme` extension + tests).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T22.1 | **TECH-618** | Done (archived) | Author `web/lib/design-system.md` — §1 type scale (10 levels, 1.25 minor-third ratio: `display` 3.815rem → `mono-meta`; weight + letter-spacing per level per extensions doc Example 1) + §2 spacing (4px grid, 9 stops: `2xs` 4px → `layout` 128px) + §3 motion vocab (4 durations: `instant` 0ms / `subtle` 120ms / `gentle` 200ms / `deliberate` 320ms; `prefers-reduced-motion: reduce` collapses all to `instant`; CSS transitions only) + §4 semantic aliases (`text.primary/secondary/meta/disabled`, `surface.canvas/raised/sunken/inset`, `accent.terrain/water/warm`) + §5 component map (per-component scale + spacing + motion bindings) + §6 a11y (WCAG AA on all aliases, `focus-visible` ring spec, keyboard nav); cites Dribbble + Shopify design references (extensions doc §8 source screenshots; NB5); cap ~10 pages. |
| T22.2 | **TECH-619** | Done (archived) | Read `web/lib/tokens/palette.json` raw values; identify `terrainGreen` + `waterBlue` + one warm candidate (amber or closest warm hue); verify WCAG AA contrast ratio on `surface.canvas` (#0a0a0a) for each candidate (NB1 — designer taste call at implementation time); document selection + contrast ratios in `design-system.md` §4 `accent.*` subsection. |
| T22.3 | **TECH-620** | Done (archived) | Author `web/lib/design-tokens.ts` — nested TS `const as const`: `typeScale` (10 entries), `spacing` (9 entries), `motion` (4 durations + `reducedMotion: { duration: 0 }`), `text` + `surface` + `accent` semantic alias maps; imports `./tokens/palette.json`; zero palette mutation; JSDoc on `motion.reducedMotion`: "`prefers-reduced-motion: reduce` collapses all durations to 0 via CSS media query in `globals.css`". Author `web/lib/__tests__/design-tokens.test.ts` — assert typeScale monotonically decreasing rem, 9 spacing stops, motion keys complete, `reducedMotion.duration === 0`, alias hex values resolve to palette raw entries. |
| T22.4 | **TECH-621** | Done (archived) | Extend `web/app/globals.css` `@theme` block — append `--ds-*` CSS custom properties: `--ds-font-size-display` … `--ds-font-size-mono-meta` (type scale), `--ds-spacing-2xs` … `--ds-spacing-layout` (spacing), `--ds-duration-instant` … `--ds-duration-deliberate` + `--ds-duration-reduced-motion: 0ms` (motion), `--ds-text-*` / `--ds-surface-*` / `--ds-accent-*` semantic aliases; all prefixed `ds-*` (B1 guard — no collision with existing `--color-*` / `--spacing-*` / `--text-*`); add `@media (prefers-reduced-motion: reduce)` rule setting all `--ds-duration-*` to `0ms`; `npm run validate:web` green. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: |
    Author `web/lib/design-system.md` — §1 type scale (10 levels, 1.25 minor-third ratio: `display` 3.815rem → `mono-meta`; weight + letter-spacing per level per extensions doc Example 1) + §2 spacing (4px grid, 9 stops: `2xs` 4px → `layout` 128px) + §3 motion vocab (4 durations: `instant` 0ms / `subtle` 120ms / `gentle` 200ms / `deliberate` 320ms; `prefers-reduced-motion: reduce` collapses all to `instant`; CSS transitions only) + §4 semantic aliases (`text.primary/secondary/meta/disabled`, `surface.canvas/raised/sunken/inset`, `accent.terrain/water/warm`) + §5 component map (per-component scale + spacing + motion bindings) + §6 a11y (WCAG AA on all aliases, `focus-visible` ring spec, keyboard nav); cites Dribbble + Shopify design references (extensions doc §8 source screenshots; NB5); cap ~10 pages.
  priority: medium
  notes: |
    Spec-only Phase 1 deliverable. Cites `docs/web-platform-post-mvp-extensions.md` §8 + NB5; caps ~10 pages. No `design-tokens.ts` or `globals.css` edits in this task — sibling T22.3/T22.4 own pipeline.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Author canonical `web/lib/design-system.md` (§1 type scale 10 levels minor-third, §2 spacing 9 stops, §3 motion 4 durations + reduced-motion policy, §4 semantic text/surface/accent aliases, §5 component map, §6 a11y). Names Dribbble + Shopify refs. Establishes narrative before TS + CSS token work.
    goals: |
      1. `web/lib/design-system.md` present with §1–§6 per Stage 22 Intent.
      2. Dribbble + Shopify design references cited (extensions §8, NB5).
      3. `design-system.md` length ~≤10 pages; game-accent scope identified for T22.2.
    systems_map: |
      - `web/lib/design-system.md` (new)
      - `docs/web-platform-post-mvp-extensions.md` (§8 refs)
      - `web/lib/tokens/palette.json` (read-only cite for T22.2 handoff)
    impl_plan_sketch: |
      ### Phase 1 — Author design-system.md
      - [ ] Draft §1–§6 per master-plan Intent; cite refs; pass `npm run validate:web` if markdown tooling complains.
- reserved_id: ""
  title: |
    Read `web/lib/tokens/palette.json` raw values; identify `terrainGreen` + `waterBlue` + one warm candidate (amber or closest warm hue); verify WCAG AA contrast ratio on `surface.canvas` (#0a0a0a) for each candidate (NB1 — designer taste call at implementation time); document selection + contrast ratios in `design-system.md` §4 `accent.*` subsection.
  priority: medium
  notes: |
    Reads `web/lib/tokens/palette.json`; picks terrain, water, warm accent; documents contrast on `surface.canvas` (#0a0a0a) per NB1; designer taste at implementation time.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Read palette raws; select `terrainGreen`, `waterBlue`, warm (amber or nearest); verify WCAG AA on canvas #0a0a0a; document ratios + choices in `design-system.md` §4 `accent.*`.
    goals: |
      1. Three accent families justified + contrast table in §4.
      2. Selection tied to `palette.json` keys; no ad-hoc hex off-palette.
      3. Notes NB1 — designer call documented where ambiguous.
    systems_map: |
      - `web/lib/tokens/palette.json` (read)
      - `web/lib/design-system.md` (edit §4)
    impl_plan_sketch: |
      ### Phase 1 — Accent derivation
      - [ ] Measure contrast; write §4 subsection; `npm run validate:web`.
- reserved_id: ""
  title: |
    Author `web/lib/design-tokens.ts` — nested TS `const as const`: `typeScale` (10 entries), `spacing` (9 entries), `motion` (4 durations + `reducedMotion: { duration: 0 }`), `text` + `surface` + `accent` semantic alias maps; imports `./tokens/palette.json`; zero palette mutation; JSDoc on `motion.reducedMotion`: "`prefers-reduced-motion: reduce` collapses all durations to 0 via CSS media query in `globals.css`". Author `web/lib/__tests__/design-tokens.test.ts` — assert typeScale monotonically decreasing rem, 9 spacing stops, motion keys complete, `reducedMotion.duration === 0`, alias hex values resolve to palette raw entries.
  priority: medium
  notes: |
    TS const tree: typeScale, spacing, motion+reducedMotion, text, surface, accent; import palette; tests in `web/lib/__tests__/design-tokens.test.ts`.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Implement `web/lib/design-tokens.ts` per Stage Exit: nested `as const`, no palette mutation, JSDoc on `motion.reducedMotion`. Add unit tests per Exit bullets.
    goals: |
      1. Exports: typeScale (10), spacing (9), motion (4 + reduced), text, surface, accent.
      2. Tests cover monotonic rem, stop count, motion keys, reduced duration 0, alias→palette resolution.
      3. `npm run validate:web` green.
    systems_map: |
      - `web/lib/design-tokens.ts` (new)
      - `web/lib/tokens/palette.json` (import)
      - `web/lib/__tests__/design-tokens.test.ts` (new)
    impl_plan_sketch: |
      ### Phase 1 — Token module + tests
      - [ ] Author TS + tests; run `npm run validate:web`.
- reserved_id: ""
  title: |
    Extend `web/app/globals.css` `@theme` block — append `--ds-*` CSS custom properties: `--ds-font-size-display` … `--ds-font-size-mono-meta` (type scale), `--ds-spacing-2xs` … `--ds-spacing-layout` (spacing), `--ds-duration-instant` … `--ds-duration-deliberate` + `--ds-duration-reduced-motion: 0ms` (motion), `--ds-text-*` / `--ds-surface-*` / `--ds-accent-*` semantic aliases; all prefixed `ds-*` (B1 guard — no collision with existing `--color-*` / `--spacing-*` / `--text-*`); add `@media (prefers-reduced-motion: reduce)` rule setting all `--ds-duration-*` to `0ms`; `npm run validate:web` green.
  priority: medium
  notes: |
    Append DS CSS custom properties; B1 `ds-*` prefix; `@media (prefers-reduced-motion: reduce)` sets durations to 0ms; do not clobber existing `--color-*` / legacy tokens.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Mirror `design-tokens.ts` into `@theme` `--ds-font-size-*`, `--ds-spacing-*`, `--ds-duration-*`, semantic text/surface/accent; add reduced-motion media block; keep prior `@theme` lines intact.
    goals: |
      1. All `--ds-*` names per master-plan Intent; no prefix collision (B1).
      2. Reduced-motion block forces duration vars to 0ms.
      3. `npm run validate:web` green.
    systems_map: |
      - `web/app/globals.css` (`@theme` append)
      - `web/lib/design-tokens.ts` (align names — optional cross-check)
    impl_plan_sketch: |
      ### Phase 1 — CSS custom properties
      - [ ] Edit `globals.css`; run `npm run validate:web`.
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
