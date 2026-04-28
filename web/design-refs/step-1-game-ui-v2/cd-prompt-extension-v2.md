# CD prompt v2 — Territory Game UI MVP bundle EXTENSION

**Same CD session as v1.** Drop ONLY the new folder `web/design-refs/step-1-game-ui-v2/` (this prompt + v2 fixtures file) into the existing v1 session. Do NOT re-drop the v1 folder — CD already has v1 grounding loaded (`cd-prompt-kicker.md`, `cd-context-bundle.md`, `cd-bundle/`, `ir.json`, `screenshots/*.png`).

This is an **extension** of the v1 bundle (already shipped + transcribed → `ir.json`). Goal: close 3 gaps blocking Stages 4 / 6 / 7 of the master plan.

New v2 file (attached via this folder drop):
- `cd-context-bundle-v2-fixtures.md` — v2 game-content corpus (real building names, alert text, overlay legends, date format, city stats list). Use these strings verbatim in Package 2 panel mockups + Package 3 icon labels.

Already in session (from v1 drop — reference, do not re-attach):
- `cd-prompt-kicker.md` — v1 prompt (already executed; do not regenerate v1 outputs)
- `cd-context-bundle.md` — original direction brief (mood, out-of-scope, web grammar)
- `cd-bundle/` — v1 deliverables: `tokens.css`, `panels.json`, `interactives.json`, `archetypes.jsx`, `panels.jsx`, `app.jsx`
- `ir.json` — transcribed IR (round-tripped from v1; visual ground truth)
- `screenshots/*.png` — 10 current Unity build captures (`screenshots/README.md` maps each PNG to its surface)

---

# ONE-SHOT EXTENSION REQUEST — 3 deliverables, single response

Extend the existing CD bundle with three additive packages. Do NOT re-author v1 outputs (current `tokens.css` flat block + 5 archetypes + 10 panels stay). Append-only across all deliverables. Studio-rack mood throughout.

## Package 1 — Missing 3 StudioControl archetypes

Current bundle ships 5 archetypes. The locked IR enum has 8. Close the gap.

### 1A — `oscilloscope`

**Concept:** real-time signal trace, console-instrument flavor. Used in city sim for time-series previews (pollution curve, citizen mood trace, treasury flow).

**Required visual:** rectangular bezel frame (chassis-graphite), inset CRT-style well (faceplate-bronze deep tier), graticule grid (8×4 division lines, low-alpha silkscreen), waveform path (illumination-tone driven stroke + halo glow), corner anchor screws.

**States (5):** default / hover / focus / pressed / disabled.

**Sizes (3):**
- `sm` — 96×56 px (tooltip / inline)
- `md` — 192×112 px (info-panel default)
- `lg` — 320×180 px (city-stats / settings)

**Tones (3):** primary (cyan trace), neutral (cream trace), alert (ruby trace).

**Trace samples (one per size):**
- `sm` — sine-like 4 cycles, mid amplitude
- `md` — pollution-curve shape (slow rise + spike + decay), 1 cycle
- `lg` — multi-segment economic flow (3 distinct phases)

**Detail JSON shape:**
```json
{
  "states": ["default","hover","focus","pressed","disabled"],
  "sizes": ["sm","md","lg"],
  "tones": ["primary","neutral","alert"],
  "trace_samples": {
    "sm": "M0,28 Q12,0 24,28 T48,28 T72,28 T96,28",
    "md": "M0,90 L40,80 L80,30 L100,15 L130,40 L192,75",
    "lg": "M0,140 L60,120 L100,40 L160,60 L220,150 L320,90"
  },
  "graticule_divisions": { "x": 8, "y": 4 },
  "default_size": "md",
  "default_tone": "primary"
}
```

### 1B — `detent-ring`

**Concept:** discrete-step rotary selector (think rack-mount selector switch). 12-position by default. Used for tool selection, zone-type cycling, time-of-day quick-jump.

**Required visual:** circular bezel ring, position dots (12 around perimeter, low-tier silkscreen for unlit, illumination-tone for lit), pointer wedge from center to current detent, knob-cream cap with central indicator notch.

**States (5):** default / hover / focus / pressed / disabled.

**Sizes (3):**
- `sm` — 56 px diameter (toolbar inline)
- `md` — 88 px (info-panel)
- `lg` — 128 px (settings)

**Tones (3):** primary / neutral / alert.

**Detents (samples):** sm=8 / md=12 / lg=16. Lit position rotates with current value.

**Detail JSON shape:**
```json
{
  "states": ["default","hover","focus","pressed","disabled"],
  "sizes": ["sm","md","lg"],
  "tones": ["primary","neutral","alert"],
  "detent_samples": {
    "sm": { "detents": 8, "current": 3 },
    "md": { "detents": 12, "current": 7 },
    "lg": { "detents": 16, "current": 11 }
  },
  "default_size": "md",
  "default_tone": "primary"
}
```

### 1C — `led`

**Concept:** standalone status pip — no label, no body, just the lamp + halo. Used for compact status indicators (alert active, autosave pending, network sync) where a full illuminated-button is too heavy.

**Required visual:** circular lit dot, halo bloom under the illumination token, thin chassis-graphite ring around the lamp. NO label slot. NO press affordance.

**States (5):** default / hover / focus / pressed / disabled. (Even though it's not interactive in most uses, the bridge-side prefab still wants the full state ring for consistency.)

**Sizes (3):**
- `sm` — 8 px (inline status)
- `md` — 12 px (panel header)
- `lg` — 18 px (alert badge)

**Tones (4):** primary (cyan), neutral (cream), alert (ruby), success (grass). NOTE: led adds `success` tone (grass) — first archetype to use the existing `led-grass` palette + matching `illumination-grass` block.

**Lit modes (2):** unlit / lit. Default = lit.

**Detail JSON shape:**
```json
{
  "states": ["default","hover","focus","pressed","disabled"],
  "sizes": ["sm","md","lg"],
  "tones": ["primary","neutral","alert","success"],
  "lit_modes": ["unlit","lit"],
  "default_size": "md",
  "default_tone": "primary",
  "default_lit": true
}
```

## Package 2 — 5 game-domain panels

Current 10 panels are studio-rack flavored generic shells. City sim needs surfaces that carry actual game data. Same archetype shell aesthetic — only slot composition + content language is game-domain.

> Game context (pulled from `ia/specs/game-overview.md`): Territory Developer = 2D isometric city builder, multi-scale (city / region / country MVP). Player as mayor places streets / zones / buildings, manages economy. Real-time tick.

### 2A — `building-info`

**Trigger:** player clicks a placed building or unit on the map.

**Screenshot reference:** `screenshots/info-panel-subtype.png` (current BuildingSelector hover state with `Medium` / `Heavy` subtype tiles — closest analog), `screenshots/budget.png` (Growth Budget side-rail, related economy).

**Sample content (from `cd-context-bundle-v2-fixtures.md`):** building name `Power Plant`, density tier `Heavy`, alert pip = success (grass led).

**Slots:**
- `header` — building name + icon + tone-led status pip. Accepts: `led`, `segmented-readout`, `illuminated-button`.
- `vitals` — 3-row meter strip (capacity / efficiency / upkeep). Accepts: `vu-meter`, `segmented-readout`.
- `controls` — operate (toggle on/off), upgrade, demolish. Accepts: `illuminated-button`, `detent-ring`.
- `trend` — last-N-ticks throughput trace. Accepts: `oscilloscope`, `segmented-readout`.

### 2B — `zone-overlay`

**Trigger:** player switches to a zone-paint or overlay-view mode (heat / desirability / pollution / land-value heatmap).

**Screenshot reference:** no current overlay UI in build — closest analog is `screenshots/minimap.png` (5-toggle layer pill row `St / Zn / Fr / De / Ct`). Treat as new surface; honor minimap pill aesthetic for `overlay-select`.

**Sample content:** overlay names `Desirability` / `Pollution` / `Land Value` / `Heat` / `Power` / `Water`; legend anchors `low → high` / `clean → toxic` / etc. (see fixtures).

**Slots:**
- `overlay-select` — choose which heatmap layer is active. Accepts: `detent-ring`, `illuminated-button`.
- `legend` — value-to-color ramp + min/max readouts. Accepts: `segmented-readout`, `vu-meter`.
- `tools` — paint / erase / sample. Accepts: `illuminated-button`.
- `stats` — current-cell readout (hover sample). Accepts: `segmented-readout`, `led`.

### 2C — `time-controls`

**Trigger:** always present — top-right HUD region.

**Screenshot reference:** `screenshots/hud-top-right.png` (current AUTO toggle + zoom + graph + Mini Map button + `$20,000 (+$0)` treasury + play-speed transport row).

**Sample content:** clock `2024-03-15` (`yyyy-MM-dd`), speed dial positions `1× / 2× / 4× / 8×`, transport icons `pause / play / fast-forward / step` (Package 3).

**Slots:**
- `transport` — pause / play / fast-forward / step-once. Accepts: `illuminated-button`.
- `speed-select` — 1× / 2× / 4× / 8× speed dial. Accepts: `detent-ring`.
- `clock` — current in-game date/time. Accepts: `segmented-readout`.
- `status` — autosave / pause-when-unfocused indicators. Accepts: `led`.

### 2D — `alerts-panel`

**Trigger:** floating bottom-left or summoned via top-bar button. Holds active notifications (resource-depleted, building-destroyed, treasury-floor-breached, etc.).

**Screenshot reference:** no current persistent panel — current build only shows transient toasts via `GameNotificationManager.PostNotification`. Treat as new surface; reuse `info-panel` chassis aesthetic for the feed list.

**Sample content (verbatim from fixtures):** info row `Built Whitmore Heights`, success row `Bond issued.`, warning row `Connect a road to the Interstate Highway before building.`, error row `Cannot place zone here.` Severity pips use `led` archetype with cyan / grass / amber-ruby / ruby tones.

**Slots:**
- `summary` — count of active alerts by severity (info / warn / critical). Accepts: `led`, `segmented-readout`.
- `feed` — scrollable list of last N alerts (5–8 visible). Each row has a tone-led pip + label + timestamp readout. Accepts: `led`, `illuminated-button`, `segmented-readout`.
- `filters` — show/hide by severity. Accepts: `illuminated-button`.

### 2E — `mini-map`

**Trigger:** persistent — bottom-right corner.

**Screenshot reference:** `screenshots/minimap.png` (current standalone minimap with 5-toggle layer pill row `St / Zn / Fr / De / Ct`). `screenshots/hud-top-right.png` shows the `Mini Map` button that summons / focuses it.

**Sample content:** layer pills `St / Zn / Fr / De / Ct`, scale dial positions `City / Region / Country`, coords readout `X 67 Y 127`.

**Slots:**
- `viewport` — the actual mini-map render area (just a framed well; no interactives go here, but it's still a slot for prefab discoverability). Accepts: empty (frame-only). Children: `[]`.
- `controls` — zoom in / zoom out / center-on-camera. Accepts: `illuminated-button`.
- `scale-select` — switch between city / region / country mini-map view (multi-scale game). Accepts: `detent-ring`, `illuminated-button`.
- `coords` — camera position readout (X, Y). Accepts: `segmented-readout`.

## Package 3 — Icon ring

Current bundle has zero icons. Stage 7 toolbar = 9-button tool grid + zone-paint selector + overlay selectors + time-control transports — all currently text-labeled. Need a coherent SVG icon set.

### Coverage (~22 icons)

**Tool / build mode (6):**
- `select` (cursor / arrow)
- `road` (paving brush)
- `zone-residential` (house silhouette)
- `zone-commercial` (storefront)
- `zone-industrial` (smokestack)
- `bulldoze` (dozer blade)

**Building category (4):**
- `power` (lightning bolt)
- `water` (droplet)
- `services` (gear)
- `landmark` (column / monument)

**Overlay (4):**
- `desirability` (radial gradient mark)
- `pollution` (smoke wisp)
- `land-value` (stacked coins)
- `heat` (flame)

**Transport (4):**
- `pause` (two bars)
- `play` (triangle)
- `fast-forward` (double triangle)
- `step` (single triangle + bar)

**Status (4):**
- `alert` (exclamation triangle)
- `info` (i in circle)
- `success` (check)
- `autosave` (disk + arrow)

### Style

- Stroke-based, 1.5 px nominal at 24×24 viewBox.
- Single-color via `currentColor` (token-themable through chassis-graphite / faceplate-bronze / illumination tones).
- Optical alignment baseline matches silkscreen line-height.
- Round line-caps + round line-joins (matches studio-rack chassis radius language).
- NO drop shadows / NO gradients / NO embedded fills beyond `currentColor`.

### Delivery

Single SVG sprite sheet using `<symbol>` per icon, plus a JSON manifest mapping slug → viewBox + brief.

# Hard requirements (apply to all 3 packages)

- **Append-only.** Do NOT rewrite v1 `tokens.css` flat `:root { ... }` block, v1 `panels.json`, or v1 `interactives.json`. Output extension blocks designed to be merged manually.
- **Token discipline.** Reuse v1 palette / frame_style / font_face / motion_curve / illumination tokens. NEW token additions only when strictly required (e.g., `palette-led-grass` already exists from v1; if grass illumination missing, add `illumination-grass`).
- **IR JSON shape locked.** `panels[]` items need `{ slug, archetype, slots: [{ name, accepts, children }] }`. `interactives[]` items need `{ slug, kind, detail }` with `kind ∈ { oscilloscope, detent-ring, led }`. Slot `accepts` must reference real interactive slugs (the 5 v1 + the 3 new); `children` must be subsets of `accepts`.
- **Slot accept-rule.** Every child entry must appear in the slot's `accepts` array. Empty `children: []` is allowed (e.g. mini-map.viewport).
- **Studio-rack mood.** NO flat dashboard / Material / Tailwind defaults. Bezel + chassis + silkscreen + illumination throughout.
- **Self-contained.** System font stack only, no external CDNs, no JS, no animations beyond CSS keyframes named to motion_curve slugs.

# Canvas output

Show on canvas:
- Full hi-fi mockups of the 3 new archetypes (oscilloscope / detent-ring / led) in all states × sizes × tones.
- Hi-fi mockups of the 5 new game-domain panels with realistic city-sim content (real building name, real overlay legend, real game-clock string, real alert messages).
- Full icon sprite preview at 24 / 16 / 32 px sizes.

# Deliverable code blocks (end of response — exact 6 blocks, named exactly as below)

1. **CSS block titled `tokens-extension.css`** — only NEW `:root.{group}-{slug}` indexed blocks (palette / frame_style / font_face / motion_curve / illumination) added by this extension. Plus any new `.iled--success`, `.led`, `.osc`, `.detent-ring` style rules required to render the new archetypes (parser ignores style rules; humans need them for visual review).

2. **JSON block titled `panels-extension.json`** — array of 5 new panel objects (building-info / zone-overlay / time-controls / alerts-panel / mini-map). Top-level shape = same as v1 `panels.json` — array, not wrapped in object. Will be appended to v1 `panels.json` array.

3. **JSON block titled `interactives-extension.json`** — array of 3 new interactive objects (oscilloscope / detent-ring / led). Top-level = array. Will be appended to v1 `interactives.json` array.

4. **HTML / JSX block titled `archetypes-extension.jsx`** — React component sketches for the 3 new archetypes (`Osc`, `DetentRing`, `Led`), matching the function shape of v1 `archetypes.jsx` (props: `size`, `tone`, `state`, plus archetype-specific kwargs). Used for visual review only; not consumed by transcribe.

5. **HTML / JSX block titled `panels-extension.jsx`** — React component sketches for the 5 new game-domain panels with realistic content fixtures, matching the function shape of v1 `panels.jsx`. Used for visual review only.

6. **SVG block titled `icons.svg`** — single SVG sprite using `<symbol id="icon-{slug}" viewBox="0 0 24 24">…</symbol>` per icon. All 22 icons inside one root `<svg>` with `style="display:none"` so it renders as a sprite source. Plus an inline `<!-- icons.json -->` HTML comment block at the top containing the JSON manifest:
   ```json
   { "icons": [ { "slug": "select", "viewBox": "0 0 24 24", "brief": "cursor arrow" }, ... ] }
   ```

# Acceptance — what success looks like

After this CD response lands, manual merge into `cd-bundle/` should yield:
- `tokens.css` = v1 contents + appended `tokens-extension.css` blocks (parser-readable indexed blocks only — flat `:root` block does not need new entries).
- `panels.json` = v1 array + 5 new panel objects (15 total).
- `interactives.json` = v1 array + 3 new interactive objects (8 total — matches StudioControlKind enum).
- `archetypes.jsx`, `panels.jsx` = v1 contents + new component definitions (visual review parity).
- `icons.svg` + `icons.json` = new files (separate from IR — wired in a later stage).

Then `npm run transcribe:cd-game-ui` must exit 0 and produce an extended `ir.json` with:
- 8 archetypes in the `interactives[]` array
- 15 panels in the `panels[]` array
- All slot accept-rule guards green

Coverage gaps in any of the 6 deliverable blocks = unusable output.
