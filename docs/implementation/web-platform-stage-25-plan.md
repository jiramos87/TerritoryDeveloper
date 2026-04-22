# web-platform — Stage 25 Plan Digest

Compiled 2026-04-22 from 6 task spec(s): TECH-634, TECH-635, TECH-636, TECH-637, TECH-638, TECH-639.

Source orchestrator: `ia/projects/web-platform-master-plan.md` — Stage 25 (console chrome primitive library).

---

## §Plan Digest — TECH-634

### §Goal

Land `Rack` + `Bezel` as RSC console frame primitives under `web/components/console/` with `--ds-*` token styling and a new barrel export, tracing to CD `console-primitives.jsx`.

### §Acceptance

- [ ] `Rack.tsx` + `Bezel.tsx` added; typed `tone` / `padding` (and Bezel props per CD port).
- [ ] `web/components/console/index.ts` re-exports Rack + Bezel.
- [ ] JSDoc cites `web/design-refs/step-8-console/src/console-primitives.jsx`.
- [ ] `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Add `Rack.tsx`, `Bezel.tsx`, `index.ts`; gate: `npm run validate:web`.

---

## §Plan Digest — TECH-635

### §Goal

Add `Screen` + `LED` RSC primitives: readout surface + status LED with prop maps from master-plan T25.2 Intent.

### §Acceptance

- [ ] Screen: `tone` (dark/readout), `inset`; LED: `state`, `color` → `--ds-*` classes.
- [ ] `index.ts` re-exports.
- [ ] `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Add `Screen.tsx`, `LED.tsx`; update barrel; gate: `npm run validate:web`.

---

## §Plan Digest — TECH-636

### §Goal

Add RTL smoke tests for the static console quartet (Rack, Bezel, Screen, LED) with fixture props and `--ds-*` presence checks.

### §Acceptance

- [ ] `web/components/console/__tests__/chrome-frame.test.tsx` exists; imports four primitives.
- [ ] Tests assert render + root element / token substring per conventions.
- [ ] `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Add `chrome-frame.test.tsx`; gate: `npm run validate:web`.

---

## §Plan Digest — TECH-637

### §Goal

Port `TapeReel` as a client component with rotation animation, `globals.css` reduced-motion override, and NB-CD3 JSDoc.

### §Acceptance

- [ ] `TapeReel.tsx` is `'use client'`; `spinning` + `size` props.
- [ ] `@media (prefers-reduced-motion: reduce)` disables tape animation in `web/app/globals.css` if not already present.
- [ ] JSDoc documents NB-CD3; barrel export; `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Add `TapeReel.tsx`; edit `globals.css` + barrel; gate: `npm run validate:web`.

---

## §Plan Digest — TECH-638

### §Goal

Add `VuStrip` (level meter) and `TransportStrip` (action row) as client components; VuStrip respects reduced motion; Transport uses shared `Button` or chrome-styled buttons with `onAction` dispatch.

### §Acceptance

- [ ] `VuStrip` + `TransportStrip` per master-plan T25.5; barrel; `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Add `VuStrip.tsx`, `TransportStrip.tsx`; wire buttons; gate: `npm run validate:web`.

---

## §Plan Digest — TECH-639

### §Goal

Append `## Console chrome` to the dev-only design-system page with a demo of all seven primitives; preserve dev guards.

### §Acceptance

- [ ] Page section renders 7 components; `NODE_ENV` / noindex unchanged; `npm run validate:web` exit 0.

### §Mechanical Steps (summary)

- Edit `web/app/(dev)/design-system/page.tsx`; gate: `npm run validate:web`.

---

## Chain notes

- `plan-digest` aggregate reference only; per-spec bodies were under `ia/projects/{id}.md` until Stage 25 closeout (2026-04-22) — see **BACKLOG-ARCHIVE** rows **TECH-634**…**TECH-639** and this compiled digest.
- `plan-review`: PASS — no fix tuples (2026-04-22).
