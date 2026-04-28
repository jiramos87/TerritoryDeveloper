# CD context bundle v2 — game content fixtures

Auto-attached via folder drop. Use these strings as realistic content when rendering Package 2 panels and Package 3 icon labels. Do NOT invent new building names, alert text, or overlay legends — pull from this corpus. Studio-rack mood applies to chrome only; copy stays game-domain.

---

## Buildings (real names from runtime)

- `Power Plant` — power-generation building (auto-built by AutoResourcePlanner)
- `Water Plant` — water-supply building (auto-built by AutoResourcePlanner)
- `Interstate Highway` — protected road segment, cannot be demolished
- `Residential` — zone, not a building (low / medium / heavy density subtypes)
- `Commercial` — zone (low / medium / heavy density subtypes)
- `Industrial` — zone (low / medium / heavy density subtypes)
- `Forest` — natural / decorative zone
- `Road` — paved tile

Density subtype tiles (see `info-panel-subtype.png`): `Low` / `Medium` / `Heavy`.

Sample placed building name in HUD: `"Whitmore"` (city name, see `city-scene-overview.png`).

## Alert message corpus (from GameNotificationManager call sites)

Real strings posted by managers — use verbatim or paraphrased shape in alerts-panel feed rows.

**Info (cyan led):**
- `Built {proposalName}`
- `Auto-built Power Plant at (12, 34)`
- `Auto-built Water Plant at (12, 34)`
- `Residential tax raised to 8%`
- `Commercial tax lowered to 6%`

**Success (grass led):**
- `Bond issued.`
- `{buildingName} constructed successfully`

**Warning (amber/ruby led):**
- `Connect a road to the Interstate Highway before building.`
- `Connect a road to the Interstate Highway before zoning.`
- `The Interstate Highway cannot be demolished.`
- `An active bond already exists for this city tier.`
- `Principal must be at least $50,000.`

**Error (ruby led):**
- `Cannot place zone here.`
- `Could not issue bond.`

## Overlay legend strings

Heatmap overlay names + min/max anchor labels:

- `Desirability` — low → high
- `Pollution` — clean → toxic
- `Land Value` — depressed → premium
- `Heat` — cold → hot
- `Power` — unpowered → overflow
- `Water` — dry → flood

## City stats list (16 rows — see `city-stats.png`)

Use as `city-stats.row-list` panel children content (already in v1 panel; reference for v2 game-domain context):

1. Population
2. Money
3. Happiness
4. Power output
5. Power consumption
6. Unemployment
7. Total jobs
8. Residential demand
9. Commercial demand
10. Industrial demand
11. Demand feedback
12. Total jobs created
13. Available jobs
14. Jobs taken
15. Water output
16. Water consumption

## Mini-map layer toggles (see `minimap.png`)

5 abbreviated layer pills at top of minimap viewport: `St` (Streets) / `Zn` (Zones) / `Fr` (Forest) / `De` (Desirability) / `Ct` (Centers / Commerce).

## Number / format examples (verbatim from build)

- **Money:** `$20,000 (+$0)` — current treasury + delta-per-tick
- **Game date:** `2024-03-15` — `yyyy-MM-dd` format, epoch 2024 (TimeManager)
- **Cursor readout:** `Cursor: x: 67 y: 127 chunk: (4,7) S: n/a body: n/a CityCell: h:4 Grass`
- **City name:** `Whitmore`
- **Tax rates:** `Residential 7%` / `Commercial 7%` / `Industrial 7%`
- **Zone subtype tiles:** `Low` / `Medium` / `Heavy`
- **Bond minimum:** `$50,000`

## Time-control transports (see `hud-top-right.png`)

Top-right HUD row: `AUTO` red toggle / zoom buttons / graph button / `Mini Map` button / treasury readout `$20,000 (+$0)` / play-speed transport row (`pause` / `play` / `2×` / `4×`).

## Tool grid (see `toolbar-building-selector.png`)

9-cell tool-grid panel + 2-cell subtype row. Building category icons: `residential` / `commercial` / `industrial` / `road` / `forest` / `power` / `water` / `construction` / `select`.

---

## How to use this file

When rendering Package 2 panels in `panels-extension.jsx`:
- Drop real building names in `building-info.header` (e.g. `Power Plant` not `Building`).
- Use real alert text in `alerts-panel.feed` rows.
- Use real overlay names in `zone-overlay.overlay-select`.
- Use the `yyyy-MM-dd` date format in `time-controls.clock`.
- Use the city stats list verbatim if showing a `city-stats` deep-cut variant.
- Use `Whitmore` as the example city name where one is needed.

When rendering Package 3 icons:
- `zone-residential` / `zone-commercial` / `zone-industrial` icons must read at 16 px next to the subtype tiles (`Low` / `Medium` / `Heavy`).
- `power` / `water` icons must read next to `Power Plant` / `Water Plant` building names.
- `pause` / `play` / `fast-forward` / `step` transport icons go in the time-controls.transport slot.
