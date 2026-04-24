### Stage 25 — Visual design layer / Console chrome primitive library


**Status:** Final

**Objectives:** Port the CD bundle's console chrome primitive set from `web/design-refs/step-8-console/src/console-primitives.jsx` into production `.tsx` components under `web/components/console/`. Mandatory per D5 (console-rack aesthetic site-wide lock 2026-04-18). Split ports into static chrome frame primitives (Rack / Bezel / Screen / LED) and animated primitives (TapeReel / VuStrip / TransportStrip) — animated set gets explicit `prefers-reduced-motion: reduce` audit per NB-CD3. All primitives consume `--ds-*` CSS variables; default RSC-compatible (client island only when animation needs `useEffect`).

**Exit:**

- `web/components/console/Rack.tsx` + `Bezel.tsx` + `Screen.tsx` + `LED.tsx`: pure RSC; `--ds-*` vars; `tone` / `state` props matching CD bundle; JSDoc cites `web/design-refs/step-8-console/src/console-primitives.jsx` as source.
- `web/components/console/TapeReel.tsx` + `VuStrip.tsx` + `TransportStrip.tsx`: `'use client'` when animation props active; `prefers-reduced-motion: reduce` media query collapses animation to static frame; NB-CD3 audit documented in each component JSDoc.
- `web/components/console/index.ts`: barrel export of all 7 primitives.
- `web/components/console/__tests__/*`: smoke-render tests for all 7 (render without throw against fixture props).
- `web/app/(dev)/design-system/page.tsx` (Stage 8.2) appended: console chrome showcase row rendering all 7 primitives against fixture props.
- `npm run validate:web` green.
- Phase 1 — Static chrome frame (Rack / Bezel / Screen / LED + tests).
- Phase 2 — Animated primitives + showcase (TapeReel / VuStrip / TransportStrip + reduced-motion audit + showcase update).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T25.1 | **TECH-634** | Done | Author `web/components/console/Rack.tsx` + `web/components/console/Bezel.tsx` — port from CD `web/design-refs/step-8-console/src/console-primitives.jsx` `Rack` + `Bezel` components; convert JSX → TSX with typed props (`tone?: 'default' | 'muted'`, `padding?: 'sm' | 'md' | 'lg'`); map CD className references to `--ds-surface-*` + `--ds-spacing-*` via Tailwind v4 arbitrary values (`bg-[var(--ds-surface-raised)]`); pure RSC (no hooks); barrel-exported via `web/components/console/index.ts` (create alongside). |
| T25.2 | **TECH-635** | Done | Author `web/components/console/Screen.tsx` + `web/components/console/LED.tsx` — port CD `Screen` + `LED` components; `Screen` props: `tone?: 'dark' | 'readout'` + `inset?: boolean`; `LED` props: `state?: 'off' | 'on' | 'blink' | 'error'`, `color?: 'green' | 'amber' | 'red' | 'info'` mapped to `--ds-accent-*` aliases (or `--ds-status-*`); pure RSC; append to `web/components/console/index.ts`. |
| T25.3 | **TECH-636** | Done | Author `web/components/console/__tests__/chrome-frame.test.tsx` — smoke-render tests for Rack / Bezel / Screen / LED against fixture props (all tone/padding/state combos); assert no throw + expected root tag + `--ds-*` var presence in style/className; jest + React Testing Library per existing `web/lib/__tests__/` conventions. |
| T25.4 | **TECH-637** | Done | Author `web/components/console/TapeReel.tsx` — port CD `TapeReel`; `'use client'` + `useEffect` for rotation animation; props: `spinning?: boolean`, `size?: 'sm' | 'md' | 'lg'`; CSS animation via `--ds-duration-*` vars with `@media (prefers-reduced-motion: reduce) { animation: none }` rule authored in `web/app/globals.css`; NB-CD3 reduced-motion audit documented in component JSDoc; append to console barrel. |
| T25.5 | **TECH-638** | Done | Author `web/components/console/VuStrip.tsx` + `web/components/console/TransportStrip.tsx` — port CD `VuStrip` (level meter strip; props: `level: number 0..1`, `peak?: boolean`) + `TransportStrip` (Rewind/Play/Pause/Stop/FastForward/Eject button row; props: `state: 'stopped' | 'playing' | 'paused'`, `onAction: (action) => void`); `'use client'` for interaction; `prefers-reduced-motion: reduce` media-query collapses VuStrip smoothing transitions; TransportStrip buttons consume `Button` primitive (Stage 8.2 or inline); append to console barrel. |
| T25.6 | **TECH-639** | Done | Extend `web/app/(dev)/design-system/page.tsx` (Stage 8.2 T23.4 output) — append `## Console chrome` section rendering all 7 primitives against fixture props (Rack-wrapped demo of Bezel + Screen + LED matrix + TapeReel spin demo + VuStrip level bars + TransportStrip interactive row); NODE_ENV guard already applied at page top (Stage 8.2); noindex already applied; `npm run validate:web` green. |

#### §Stage File Plan

<!-- applied 2026-04-22 — stage-file-apply: **TECH-634** … **TECH-639** (6 tuples). `ia/backlog/*.yaml` + `ia/projects/*.md` materialized. -->

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> stage-closeout-plan — 6 Tasks. **Applied 2026-04-22 (session):** archived backlog rows **TECH-634**…**TECH-639** to `ia/backlog-archive/` (`status: closed`, `completed: "2026-04-22"`); removed temporary project specs for those ids; flipped Stage 25 task table to **Done** and Stage **Status** to **Final**; ran `materialize-backlog.sh` + `validate:all`.

---
