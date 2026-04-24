### Stage 26 — Visual design layer / Asset pipeline + media transport strip


**Status:** Final

**Objectives:** Land the asset pipeline decision (D6 — still open at Stage 8.5 close time) and import the CD bundle's SVG logo suite + pillar scenes + media icon family from `web/design-refs/step-8-console/src/console-assets.jsx`. Recommendation per S-CD3: `public/` SVG for hero / pillar scenes (cacheable + indexable), inline React components for the 13-glyph media icon family (palette-locked via CSS vars). Ship `<MediaTransport>` as a net-new composite component (R16) wrapping CD `TransportStrip` + media icons.

**Exit:**

- `web/lib/design-system.md` §7 appendix: D6 decision documented (asset pipeline strategy picked: `public/` SVG vs inline React vs sprite sheet, per-category rationale).
- `web/public/design/` directory: `logomark.svg`, `wordmark.svg`, `lettermark.svg`, `strapline-lockup.svg`, plus `hero-art.svg`, `pillar-planet.svg`, `pillar-signal.svg`, `pillar-mixer.svg`, `pillar-radar.svg`, `pillar-tape.svg` — cacheable static assets.
- `web/components/console/icons/TIcon.tsx`: inline React component family exporting `TIcon.Play` / `TIcon.Pause` / `TIcon.Stop` / `TIcon.Record` / `TIcon.Rewind` / `TIcon.FastForward` / `TIcon.RewindEnd` / `TIcon.FastForwardEnd` / `TIcon.Eject` / `TIcon.Loop` / `TIcon.Shuffle` / `TIcon.Mute` / `TIcon.Solo` (13 glyphs); `currentColor` fill for `--ds-*` CSS-var theming.
- `web/components/console/MediaTransport.tsx`: composite wrapping `TransportStrip` + `TIcon` family; props `state: 'stopped' | 'playing' | 'paused' | 'recording'` + `actions: Partial<Record<Action, () => void>>`; `'use client'` for interaction dispatch.
- `web/app/(dev)/design-system/page.tsx`: appended media-icon matrix + `<MediaTransport>` demo row.
- `npm run validate:web` green.
- Phase 1 — Asset pipeline decision + logo suite + pillar scenes (`public/design/` SVG imports).
- Phase 2 — Icon family + MediaTransport composite + showcase update.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T26.1 | **TECH-646** | Done | Document D6 asset pipeline decision in `web/lib/design-system.md` §7 appendix: rationale for `public/` SVG (hero + pillar scenes — cacheable via Vercel CDN + indexable + palette-locked via inline `style` attrs) vs inline React (icon family — CSS-var theming, palette-locked via `currentColor`) vs sprite sheet (rejected — no build-time bundler affordance in App Router default). Create `web/public/design/` directory with `.gitkeep`; document per-asset category path convention. |
| T26.2 | **TECH-647** | Done | Extract logo suite + hero + pillar SVGs from CD `web/design-refs/step-8-console/src/console-assets.jsx` inline React SVG components into standalone `.svg` files under `web/public/design/`: `logomark.svg`, `wordmark.svg`, `lettermark.svg`, `strapline-lockup.svg`, `hero-art.svg`, `pillar-planet.svg`, `pillar-signal.svg`, `pillar-mixer.svg`, `pillar-radar.svg`, `pillar-tape.svg`; replace inline `fill` props with `style` attrs using `--ds-*` CSS vars so theme tracks palette; cite CD bundle source in `design-system.md` §7 appendix per-asset row. |
| T26.3 | **TECH-648** | Done | Author `web/components/console/icons/TIcon.tsx` — inline React component family exporting `TIcon.Play` / `Pause` / `Stop` / `Record` / `Rewind` / `FastForward` / `RewindEnd` / `FastForwardEnd` / `Eject` / `Loop` / `Shuffle` / `Mute` / `Solo` (13 glyphs); port SVG paths from CD `console-assets.jsx`; all use `fill="currentColor"` for `--ds-*` CSS-var theming; props `{ size?: number, className?: string, 'aria-label'?: string }`; pure RSC; barrel-append to `web/components/console/index.ts`. |
| T26.4 | **TECH-649** | Done | Author `web/components/console/MediaTransport.tsx` — composite wrapping `TransportStrip` (Stage 8.4 T25.5) + `TIcon` family (T26.3); props `state: 'stopped' | 'playing' | 'paused' | 'recording'` + `actions: Partial<Record<'play' | 'pause' | 'stop' | 'rewind' | 'ff' | 'eject', () => void>>`; `'use client'` for dispatch; `aria-label` on each button; reduced-motion audit (no animation by default). Extend `web/app/(dev)/design-system/page.tsx` with media-icon matrix + `<MediaTransport>` demo row (all state values); `npm run validate:web` green. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived backlog rows **TECH-646**…**TECH-649** to `ia/backlog-archive/` (`status: closed`, `completed: "2026-04-22"`); removed temporary project specs for those ids; flipped Stage 26 task table to **Done** and Stage **Status** to **Final**; ran `materialize-backlog.sh` + `validate:all`.

---
