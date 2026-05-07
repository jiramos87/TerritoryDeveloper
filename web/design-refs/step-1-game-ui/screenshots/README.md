# Step 1 — Game UI capture index

Build captures only — no mockups, no Figma, no synthetic compositions. Annotations land only when user supplies overlay siblings (`{name}-annotated.png`).

## Captures

- `main-menu.png` — MainMenu scene, vertical button stack (Continue / New Game / Load City / Options) on flat navy panel — surface: new-game
- `settings.png` — Options modal: title + SFX volume slider + Mute SFX toggle + Back button on rounded dark panel — surface: settings
- `city-scene-overview.png` — Full CityScene HUD: top-bar tabs left + city name center + AUTO/zoom/graph/MiniMap/money/speed-controls right, BuildingSelector floating left, City Stats tabs floating bottom-right — surface: hud
- `toolbar-building-selector.png` — BuildingSelector full grid: 3×3 + 1 spillover icon tiles (residential / commercial / industrial / blank / road / forest / power / water / construction) with green outline + dark navy background — surface: toolbar
- `hud-top-right.png` — Top-right HUD detail at funded state: AUTO red toggle / +/− zoom / graph icon / Mini Map / $20,000 (+$0) money readout / play-speed control row — surface: hud
- `minimap.png` — Minimap panel: row of layer toggles (St / Zn / Fr / De / Ct) over scaled isometric grid with colored cell overlays — surface: hud
- `city-stats.png` — CITY STATISTICS panel: vertical row list (Population / Money / Happiness / Power output / Power consumption / Unemployment / Total jobs / Residential demand / Commercial demand / Industrial demand / Demand feedback / Total jobs created / Available jobs / Jobs taken / Water output / Water consumption) — green silhouette icon + label + right-aligned value, demand rows show inline horizontal bar — surface: city-stats
- `budget.png` — Budget panel: Growth Budget % slider + sub-allocation sliders (Road / Energy / Water / Zoning) all with disc thumb + right-aligned % value, then tax rows (Residential / Commercial / Industrial) with ◀ ▶ steppers — surface: info-panel
- `info-panel-subtype.png` — BuildingSelector hover/selected state — same icon grid plus bottom info row showing two subtype tiles (Medium / Heavy) with diamond chips — surface: info-panel
- `full-scene.png` — Full game window in editor view: top HUD bar (selected city "Whitmore" with red X), cursor cell readout text panel (Cursor: x: 67 y: 127 chunk:(4,7) S: n/a body: n/a CityCell: h:4 Grass), BuildingSelector left, CityStats right, Minimap floating mid-right, isometric world hidden behind grid — surface: hud

## Surface coverage vs scope

| Surface tag | PNG | Notes |
|---|---|---|
| HUD | `city-scene-overview.png`, `hud-top-right.png`, `minimap.png`, `full-scene.png` | top-bar + minimap covered |
| info-panel | `budget.png`, `info-panel-subtype.png` | budget + subtype-detail row |
| pause | _(not present in build)_ | no pause modal currently shipped |
| settings | `settings.png` | Options modal stands in |
| save-load | _(not present in build)_ | save/load surfaces routed through MainMenu Continue/Load City + top-bar floppy/folder tabs (see `city-scene-overview.png`); no dedicated picker modal yet |
| new-game | `main-menu.png` | MainMenu owns new-game entry point |
| tooltip | _(not present in build)_ | no tooltip primitive currently shipped |
| toolbar | `toolbar-building-selector.png` | BuildingSelector floating panel = toolbar |
| city-stats | `city-stats.png` | row list panel |
| onboarding | _(not present in build)_ | onboarding flow not yet authored |

Total PNGs: 10. Range gate (5–10): pass.
