# CD context bundle — Step 1 Game UI

Review-ready payload for CD design partner (claude.ai design tool). User signed off mood brief + out-of-scope per peer-loop §Phase 6 step 5. Pastes verbatim into CD session to produce first bundle drop under `web/design-refs/step-1-game-ui/cd-bundle/`.

Five sections per TECH-2288 contract:

1. §Web grammar excerpt — sibling aesthetic grammar (verbatim from `web/lib/design-system.md`).
2. §Game UI direction brief — studio-rack mood + relationship statement + 5 control archetypes.
3. §IR JSON shape spec — locked deterministic output contract (verbatim from `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 3).
4. §Out-of-scope — explicit boundary; what CD partner does NOT deliver.
5. §Current-state distillation handoff — link to T1.3 doc + T1.2 capture set.

---

## §Web grammar excerpt

> Game UI is **sibling aesthetic** to web admin: reuse token kinds + naming convention + scale ratios. Do NOT reuse palette / typography / motion curves. Quoted verbatim from `web/lib/design-system.md` so CD partner aligns naming + grammar without inheriting visual language.

### Type scale (10 levels, minor third 1.25)

**Ratio:** Each step down multiplies `rem` by `1/1.25` (minor third). **Display** anchors the scale at **3.815rem**; **mono-meta** is the smallest step (developer-metadata line).

| Step | Token | `rem` | Font | Weight | Letter-spacing | Typical use |
|------|--------|--------|------|--------|----------------|---------------|
| 0 | `display` | 3.815 | sans | 600 | -0.02em | Marketing hero, release titles |
| 1 | `h1` | 3.052 | sans | 600 | -0.02em | Page title |
| 2 | `h2` | 2.442 | sans | 600 | -0.015em | Section heading |
| 3 | `h3` | 1.953 | sans | 550 | -0.01em | Subsection, card title |
| 4 | `body-lg` | 1.563 | sans | 400 | 0 | Lead paragraph, intro |
| 5 | `body` | 1.25 | sans | 400 | 0 | Default UI copy |
| 6 | `body-sm` | 1.0 | sans | 400 | 0.01em | Dense tables, secondary labels |
| 7 | `caption` | 0.8 | sans | 400 | 0.02em | Captions, footnotes |
| 8 | `mono-code` | 0.64 | mono | 400 | 0 | Inline code, API identifiers |
| 9 | `mono-meta` | 0.512 | mono | 400 | 0.02em | Timestamps, breadcrumbs, file paths |

**Line-height:** Pair each step with `line-height: 1.2` for headings (`display`–`h3`), `1.5` for `body-lg`–`caption`, `1.45` for `mono-code` / `mono-meta` (code blocks may override in MDX).

### Spacing (4px base, 9 stops)

Grid: **4px** (`0.25rem` at 16px root). No odd px values except where legacy components require; new work uses this ladder only.

| Token | px | `rem` | Use |
|-------|----|----|-----|
| `2xs` | 4 | 0.25 | Icon-text gaps, inline chip padding |
| `xs` | 8 | 0.5 | Tight stacks, list item vertical rhythm |
| `sm` | 12 | 0.75 | Form field padding (vertical) |
| `md` | 16 | 1 | Default block gap, card padding |
| `lg` | 24 | 1.5 | Section separation inside a card |
| `xl` | 32 | 2 | Between form groups |
| `2xl` | 48 | 3 | Major section break inside a page |
| `3xl` | 64 | 4 | Panel gutters |
| `layout` | 128 | 8 | Max hero/dashboard vertical breathing room, full-bleed section padding |

### Motion vocabulary

| Token | Duration | Use |
|-------|-----------|-----|
| `instant` | 0ms | State toggles, no motion |
| `subtle` | 120ms | Hover, focus ring fade |
| `gentle` | 200ms | Dropdown, disclosure |
| `deliberate` | 320ms | Sheet, large panel |

**Rules:**

- **CSS transitions only** — no keyframe animation library for defaults.
- **`prefers-reduced-motion: reduce`:** all durations collapse to `instant` (0ms) via global CSS (see `globals.css` `ds` duration tokens + media query). Token object exposes `reducedMotion: { duration: 0 }` in TS for parity.
- **Easing default:** `cubic-bezier(0.4, 0, 0.2, 1)` for enter; optional exit `cubic-bezier(0.4, 0, 1, 1)`.

> **Note for CD partner:** the motion table above is grammar reference (token names + duration tier convention). Game UI motion is **out of scope for this bundle** — see §Out-of-scope. CD partner emits motion-curve token slugs only; durations land later in a separate task.

---

## §Game UI direction brief

**Mood:** studio-rack / instrument-panel. Hardware synth front panels, mixing console fader bays, patchbay modules, segmented LED displays, VU meters. Tone: mechanical, tactile, deliberate — not skeuomorphic clutter.

**Relationship statement:** Game UI is sibling aesthetic to web admin; reuse token kinds + naming convention + scale ratios; do NOT reuse palette / typography / motion curves — game = studio-rack, web = clean dashboard.

**Reference imagery:** vintage/modern audio-rack hardware (synthesizer front panels, mixing-desk channel strips, patchbay modules, transport-control bays, VU meter ladders, segmented-display readouts, illuminated rack buttons, latching toggle switches with lens covers).

**Control archetypes** (5 — locked by Stage 4 StudioControl ring):

1. **Knob** — rotary control with detent feel; uses for trim / fine-step adjustment.
2. **Fader** — linear slider, vertical or horizontal; uses for budget / volume / continuous range.
3. **VU meter** — segmented LED ladder (horizontal or vertical); uses for live demand / output / utilization readout.
4. **Illuminated button** — latching or momentary; lit-LED state ring; uses for toggles / mode select / action.
5. **Segmented readout** — LCD/LED-style numeric display; uses for money / counters / metric values.

**Surface scope** (10 surfaces — locked by exploration §Phase 6 step 2):

| Surface | In build? | Captured |
|---|---|---|
| HUD | yes | `city-scene-overview.png`, `hud-top-right.png`, `minimap.png`, `full-scene.png` |
| info-panel | yes | `budget.png`, `info-panel-subtype.png` |
| settings | yes (Options modal stand-in) | `settings.png` |
| new-game | yes (MainMenu) | `main-menu.png` |
| toolbar | yes (BuildingSelector) | `toolbar-building-selector.png` |
| city-stats | yes | `city-stats.png` |
| pause | not in build | — |
| save-load | not in build (routes through MainMenu) | — |
| tooltip | not in build | — |
| onboarding | not in build | — |

**Locked direction signal** (from TECH-2287 §Per-element rows signoff): 25 evolve / 0 keep / 2 drop. Every chrome panel + every control + every readout reads as a piece of audio-rack hardware. Cursor-cell debug overlay drops (gate behind dev flag).

---

## §IR JSON shape spec

> Verbatim copy from `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 3 "IR JSON shape (locked)" block. Deterministic output contract for `tools/scripts/transcribe-cd-game-ui.ts`. CD bundle output transcribes to this shape; Unity bridge handler `bake_ui_from_ir` consumes this shape verbatim.

```json
{
  "tokens": {
    "palette": [{ "slug": "...", "ramp": [...] }],
    "frame_style": [],
    "font_face": [],
    "motion_curve": [],
    "illumination": []
  },
  "panels": [
    {
      "slug": "hud-bar",
      "archetype": "themed-panel",
      "slots": [
        { "name": "left", "accepts": ["money-readout", "pop-readout"], "children": ["money-readout", "pop-readout"] },
        { "name": "right", "accepts": ["happiness-vu", "speed-buttons"], "children": ["happiness-vu"] }
      ]
    }
  ],
  "interactives": [
    { "slug": "happiness-vu", "kind": "vu-meter", "detail": { "attackMs": 80, "releaseMs": 240, "range": [0, 100] } },
    { "slug": "money-readout", "kind": "segmented-readout", "detail": { "digits": 7, "fontSlug": "console-mono", "segmentColor": "amber" } }
  ]
}
```

**Token kinds** (5):

- `palette` — color ramps keyed by slug (e.g. `panel-base`, `accent-positive`, `accent-warning`); each ramp = ordered color stops.
- `frame_style` — chrome/bezel definitions: rivet/screw placement, edge thickness, corner radius, faceplate texture marker.
- `font_face` — typography assets (font slug + weight + style); paired with web grammar scale tier.
- `motion_curve` — easing curves (slug → cubic-bezier or named curve); durations land in separate task per §Out-of-scope.
- `illumination` — lit-state palettes (LED ring color + intensity tiers; segment-display lit/dim).

**Panel shape:**

- `slug` — kebab-case unique handle.
- `archetype` — chrome type (`themed-panel` / `modal-shell` / `toolbar-rack` / `channel-strip` etc.).
- `slots[]` — named cells with `accepts[]` (interactive kinds allowed) + `children[]` (initial bound interactive slugs).

**Interactive shape:**

- `slug` — kebab-case unique handle.
- `kind` — one of 5 archetypes (`knob` / `fader` / `vu-meter` / `illuminated-button` / `segmented-readout`).
- `detail` — kind-specific config row (knob: range/detents/curve; fader: orientation/range/tick; vu-meter: attack/release/range; illuminated-button: latching/lit-color; segmented-readout: digits/font/segment-color).

**Slot accept-rule contract:** bridge handler rejects IR where `children[]` violates `accepts[]` with structured error + does NOT write prefabs. Forces deterministic CD output.

---

## §Out-of-scope

Locked boundaries — CD partner does NOT deliver these in Step 1 bundle. Each row carries rationale + future task pointer.

### From exploration §Phase 3 Non-scope (post-MVP roadmap)

| Out-of-scope | Rationale | Future home |
|---|---|---|
| Web admin schema-driven editors (DEC-A45) | Editor surfaces post-MVP | DEC-A45 plan |
| Catalog DDL drops for token kinds (DEC-A14), `panel_child` (DEC-A27), per-control detail tables (Q12) | MVP reads IR JSON directly; catalog migration deferred | DEC-A14 / DEC-A27 / Q12 |
| Publish ripple (DEC-A44) → ephemeral preview lane (DEC-A28 reuse) | Preview/publish flow lands after catalog migration | DEC-A44 / DEC-A28 |
| Snapshot pipeline → runtime hydration from snapshot | MVP bakes from IR at editor time; snapshot hydration deferred | post-MVP |
| Token migration script (UiTheme SO field cache → catalog rows) | Migration runs after catalog DDL drops | post-MVP |
| Designer-driven CD loop (designer authors directly without agent) | MVP = peer-loop with main-session agent; designer-direct loop deferred | post-MVP |
| Asset-pipeline catalog integration | Generated prefabs land in `Assets/UI/Prefabs/Generated/` per file pattern; catalog wiring deferred | post-MVP |

### From TECH-2288 user gate (peer-loop session, locked answer)

| Out-of-scope | User answer | Rationale |
|---|---|---|
| Animation / motion curves (per-component durations + timings) | B — defer | Token slugs (`subtle`/`gentle`/`deliberate` etc.) included in IR; concrete duration values land in a later task |
| UI feedback sound design (click/clack, fader ticks, LED-on hum) | B — defer | Audio task lives in separate stage outside game-ui-design-system master plan |
| Accessibility mandate (color-blind safe palette, min-contrast ratios, font-size floors) | B — defer | A11y audit lands as a discrete pass on baked output post-MVP |
| Localization (text length tolerance for non-English) | B — defer | English-only for MVP; localization sweep is a separate discipline |

### From TECH-2288 user gate (in-scope, called out for CD partner)

| In-scope | User answer | Direction |
|---|---|---|
| Hover / focus / disabled / pressed states (per-state visuals) | A — in scope | CD partner delivers per-state visuals on every interactive (default + hover + focus + pressed + disabled). Reflected in `interactives[].detail` or via `illumination` tokens — CD chooses representation. |

### From exploration §Phase 6 step 4 (peer-loop locked)

- Slot accept-rules locked by DEC-A27 panel-graph plan — CD partner does NOT redesign slot semantics; consumes locked accept-rule shape.
- Control kinds locked by Q12 detail tables — CD partner stays inside the 5 archetypes (knob / fader / VU meter / illuminated button / segmented readout); no inventing new kinds.

---

## §Current-state distillation handoff

CD partner reads the following before generating bundle output:

- **[`current-state.md`](current-state.md)** — TECH-2287 distillation: §Inventory (`UiTheme.cs` SO field table + UI prefabs in scope) + §Per-element rows (27 rows: 0 keep / 25 evolve / 2 drop) + §Tag summary. Locked direction: audio-rack / studio-console aesthetic.
- **[`screenshots/README.md`](screenshots/README.md)** — TECH-2286 capture index: 10 PNGs across 6 in-build surfaces + 4 not-present surface flags. Build captures only — no mockups.

Cross-link map for CD partner:

| Doc | Purpose |
|---|---|
| `current-state.md` §Per-element rows | Per-row `evolve-toward` prose = direct vocabulary brief for each surface element |
| `current-state.md` §Inventory | Existing structural data (token names, prefab roles, scene-baked vs prefab-instantiated split) |
| `screenshots/*.png` | Visual reference set per surface (referenced by row in §Per-element rows) |
| `tools/scripts/ir-schema.ts` | TS types CD output transcribes against (round-trip reference for TECH-2290 bundle drop) |

**Round-trip contract:** CD partner output drops under `web/design-refs/step-1-game-ui/cd-bundle/` (TECH-2290). `npm run transcribe:cd-game-ui` ingests bundle → emits `web/design-refs/step-1-game-ui/ir.json` (TECH-2289 IR schema gate). Bridge handler `bake_ui_from_ir` (Stage 2) consumes IR + writes `Assets/UI/Prefabs/Generated/*` + populates `UiTheme.asset` SO.

---

## Document control

| Item | Value |
|---|---|
| Authored | TECH-2288 (Stage 1 game-ui-design-system) |
| Peer-loop step | §Phase 6 step 4 (CD context bundle prep) |
| User signoff | Mood brief: locked (audio-rack / studio-console). Out-of-scope: locked (1B 2B 3A 4B 5B). |
| Source — web grammar | `web/lib/design-system.md` §1 / §2 / §3 |
| Source — IR shape | `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 3 |
| Source — non-scope | `docs/game-ui-mvp-authoring-approach-exploration.md` §Phase 3 + TECH-2288 user gate |
| Distillation source | `current-state.md` (TECH-2287) + `screenshots/README.md` (TECH-2286) |
| IR schema | `tools/scripts/ir-schema.ts` (TECH-2289) |
| Bundle drop target | `web/design-refs/step-1-game-ui/cd-bundle/` (TECH-2290) |
