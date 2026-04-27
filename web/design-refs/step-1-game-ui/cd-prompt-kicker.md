# CD prompt kicker — Territory Game UI MVP one-shot

Paste the contents of this file (everything below the `---` separator) into the Claude Design chat input AFTER attaching the `web/design-refs/step-1-game-ui/` folder via DROP FILES HERE. One-shot request — no follow-ups expected.

Sibling file: `cd-context-bundle.md` (full direction brief — auto-attached via folder drop).

---

ONE-SHOT REQUEST. Produce the full MVP game UI design system in this single response. No iteration, no follow-up sessions. Output must cover every surface and state described in the attached `cd-context-bundle.md`. Missing coverage = unusable bundle.

The attached folder `web/design-refs/step-1-game-ui/` is your full grounding:

- `cd-context-bundle.md` — direction brief, IR JSON locked shape, out-of-scope list, web grammar excerpt.
- `current-state.md` — 27 per-element rows tagged keep / evolve / drop (locked: 25 evolve / 0 keep / 2 drop; audio-rack direction).
- `screenshots/*.png` — 10 build captures showing the surfaces you must evolve.

## Canvas output

Show on canvas: full hi-fi mockups of every surface in studio-rack mood — HUD bar, info-panel, pause, settings, save-load, new-game, tooltip, toolbar, city-stats, onboarding. Every state of every StudioControl archetype (knob, fader, vu-meter, illuminated-button, segmented-readout) visible across the boards.

## Deliverable code blocks (end of response)

Output exactly 3 deliverable code blocks I can save directly:

1. **CSS block titled `tokens.css`** — full token system: palette tiers, frame_style, font_face stack, motion_curve slug names, illumination tones. Slug-keyed CSS custom properties only; no values that drift across panels.

2. **HTML block titled `panels.html`** — semantic markup, one `<section data-panel-slug="...">` per surface, with chrome class + `<div data-slot="..." data-accepts="knob fader vu-meter illuminated-button segmented-readout">` slot list. ALL 10 panels.

3. **JSON block titled `interactives.json`** — top-level `{ "interactives": [...] }` with every archetype × every state (default / hover / focus / disabled / pressed) × key variant axes (size sm/md/lg, tone primary/neutral/alert).

## Hard requirements

- Studio-rack / audio-console mood throughout. NO flat dashboard surfaces.
- Reuse token-kind grammar + scale ratios from the web grammar excerpt; do NOT reuse palette / typography / motion curves.
- Self-contained: system font stack only, no external CDNs, no JS, no animations beyond CSS keyframes named to motion_curve slugs.
- Slot accept-rules locked to 5 archetypes only (knob, fader, vu-meter, illuminated-button, segmented-readout).
- Every panel + every archetype + every state present in the 3 code blocks.
