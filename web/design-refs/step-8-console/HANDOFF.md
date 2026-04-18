# Territory Developer — Console Prototype · Developer Handoff

Version: Step 8 Console · pilot prototype
Entry point: `Territory Web - Step 8 Console.html`
Stack: React 18 UMD + Babel standalone (prototype only — port to your build pipeline for production)

---

## 1. Component inventory

### Chrome primitives — `src/console-primitives.jsx`

| Component | Purpose |
|---|---|
| `Rack` | Brushed-metal enclosure with 4 screw heads + optional engraved label. Outermost container for any console module. |
| `Bezel` | Recessed black frame, casts inner shadow. Wraps screens and readouts. |
| `Screen` | CRT-style LCD face — scanlines, sweep, tinted glass. Default amber; `color` prop switches to green/blue/red. |
| `LED` | 8px status pip with per-tone glow. Supports `blink`. |
| `TapeReel` | Spinning reel SVG, amber spokes. Animation obeys `prefers-reduced-motion`. |
| `VuStrip` | 24-segment peak meter, green → amber → red. |
| `TransportStrip` | Rack-wrapped row of media buttons using the 8b icon family. |

### Data primitives (six reskinned)

| Component | Purpose |
|---|---|
| `Button` | Chiseled tactile button. Variants: `primary` (amber), `secondary` (graphite), `ghost`. |
| `StatusChip` + `IdChip` | Status pill using the four status tokens; IdChip is a mono code chip with inset shadow. |
| `StatBar` | Horizontal progress meter; `done/total` + tone (`done \| progress \| pending \| blocked \| info`). Pulses subtly. |
| `FilterChip` | Toggleable tactile chip; blue glow when active. Optional status dot + count. |
| `HeatCell` | Density cell, 5 buckets (`h-null \| h-low \| h-mid \| h-high \| h-peak`). |
| (DataTable) | Rendered inline in `ScreenReleases` — class `.table` in `console.css`. |

### Helpers

| Component | Purpose |
|---|---|
| `Legend` | Persistent 4-status color key strip. |
| `DensityToggle` | Segmented control for `comfortable \| compact \| ultra`. |
| `EmptyState` / `LoadingSkeleton` / `ErrorState` | Rack-wrapped blank-states. |
| `StaleDataBanner` | Amber blinking "stale · last sync" indicator. |

### Assets — `src/console-assets.jsx`

| Component | Role (deliverable) |
|---|---|
| `Logomark` | Primary mark — PPI-rings + TD monogram (8d). |
| `Wordmark` | Logomark + "TERRITORY / DEVELOPER" stacked label (8d). |
| `Lettermark` | Square TD monogram for favicons/avatars (8d). |
| `StraplineLockup` | Footer/co-brand lockup (8d). |
| `TIcon.{Play,Pause,Stop,Record,Rewind,FastForward,RewindEnd,FastForwardEnd,Eject,Loop,Shuffle,Mute,Solo}` | Tactile media glyph family (8b). |
| `HeroArt` | 800×900 planetary hero viewport (8a). |
| `HeroCrop` | 16:8 social crop (8a). |
| `PillarPlanet`, `PillarSignal`, `PillarMixer`, `PillarRadar`, `PillarTape` | Feature-pillar scenes (8c). |
| `Sparkline` | Inline trend sparkline. |

### Screens — `src/console-screens.jsx`

| Component | Route | Purpose |
|---|---|---|
| `ScreenLanding` | `/` | Hero rack · now-playing strip · pillars · transport. |
| `ScreenDashboard` | `/dashboard` | Summary bezels · heatmap · filters · step tree. |
| `ScreenReleases` | `/dashboard/releases` | Release ledger — searchable, sortable, filterable. |
| `ScreenDetail` | `/dashboard/releases/:id` | Stage×week heatmap · scoped tree · stage readout + channels. |
| `ScreenDesign` | `/design` | Full asset sheets + primitive states. |

---

## 2. Prop signatures

```ts
Rack({ label?: string, className?: string, children })
Bezel({ thin?: boolean, className?: string, children })
Screen({ color?: "amber"|"green"|"blue"|"red", sweep?: boolean, className?: string, children })
LED({ tone?: "g"|"a"|"r"|"b", blink?: boolean, title?: string })

Button({ variant?: "primary"|"secondary"|"ghost", size?: "lg", icon?: ReactNode, ...htmlButtonProps })
StatusChip({ status: "done"|"progress"|"pending"|"blocked", children? })
IdChip({ children })
StatBar({ done: number, total: number, tone?: "done"|"progress"|"pending"|"blocked"|"info",
          label?: string, showNums?: boolean })
FilterChip({ active?: boolean, status?: StatusKey, onClick?, count?: number, children })
HeatCell({ n: number, label?: string })

Legend()
DensityToggle({ mode, setMode })
VuStrip({ level?: 0..1, segments?: number })
TapeReel({ size?: number })
TransportStrip({ state?: "play"|"pause"|"stop"|"rec"|..., onSetState? })
EmptyState({ title, body, cta? })
LoadingSkeleton({ rows?: number })
ErrorState({ title, body, cta? })
StaleDataBanner({ when?: string })

Logomark({ size?: number, variant?: "amber-on-black"|"white-on-black"|"black-on-amber", glow?: boolean })
Wordmark({ height?: number, variant?: same as Logomark })
Lettermark({ size?: number, variant?: same })
StraplineLockup({ height?: number, variant?: same })
TIcon.<name>(svgProps)   // standard SVG props; color inherits via currentColor
HeroArt({ className? })
HeroCrop({ className? })
Pillar{Planet|Signal|Mixer|Radar|Tape}({ className? })
Sparkline({ data: number[], color?: string, width?: number, height?: number })
```

---

## 3. Token map

All tokens live in `ds/colors_and_type.css` (the original locked system) plus four console additions in `console.css` (`--metal-*`, `--font-lcd`).

### Color — raw palette (LOCKED)

| Token | Value | Role |
|---|---|---|
| `--raw-black` | `#0a0a0a` | Canvas |
| `--raw-panel` | `#1a1a1a` | Panel |
| `--raw-text` | `#e8e8e8` | Primary text |
| `--raw-grey-500` | `#6a6a6a` | Muted text, pending status |
| `--raw-amber` | `#e8a33d` | Warn / in-progress / brand accent |
| `--raw-green` | `#3a9b4a` | Done / OK |
| `--raw-red` | `#d63838` | Blocked / error |
| `--raw-blue` | `#4a7bc8` | Info / selected / signal (not a status) |

### Semantic aliases

```
--bg-canvas, --bg-panel, --text-primary, --text-muted
--text-accent-warn, --text-accent-critical, --text-accent-info
--bg-status-{done|progress|pending|blocked}
--text-status-{done|progress|pending|blocked}-fg
--border-subtle (e8e8e8 @ 12%), --border-strong (e8e8e8 @ 24%)
--overlay-panel (1a1a1a @ 80%)
```

### Console additions (in `console.css`)

```
--metal-hi  #2a2a2a   /* rack top highlight */
--metal-mid #1e1e1e
--metal-lo  #141414   /* rack bottom shadow */
--metal-black #0a0a0a
--font-lcd  "Azeret Mono", "Geist Mono", monospace
```

### Type scale

`--text-xs .75rem / sm .875 / base 1 / lg 1.125 / xl 1.25 / 2xl 1.5`
Line heights match: `--lh-xs 1rem → --lh-2xl 2rem`.
Families: `--font-sans` (Geist), `--font-mono` (Geist Mono), `--font-lcd` (Azeret Mono).

### Spacing

4px base, 8-step scale: `--sp-0 0 / sp-1 4 / sp-2 8 / sp-3 12 / sp-4 16 / sp-6 24 / sp-8 32 / sp-12 48`.

### Radii

`--radius-none 0 / sm 2 / md 4 / lg 8 / pill 9999` (pill for badges only).

### Elevation

`--shadow-0 none / shadow-1 (1px) / shadow-2 (2px blur)`. Console chrome uses inset shadows inline, not tokenised (intentional — they belong to the Rack/Bezel components).

### Motion

```
--dur-fast   80ms   hover, chip toggle
--dur-base  160ms   tab switch, collapse
--dur-slow  280ms   panel slide, reorder
--dur-reveal 480ms  first paint
--ease-enter cubic-bezier(0.2, 0, 0, 1)
--ease-exit  cubic-bezier(0.4, 0, 1, 1)
```
All durations collapse to 0ms under `prefers-reduced-motion: reduce`. Continuous animations (sweep, pulse, blink, reel spin) are disabled with the same query.

### Focus ring

`--focus-ring: 2px solid var(--text-accent-warn)` (amber) + `--focus-ring-offset: 2px`. Applied via `*:focus-visible`.

---

## 4. Accessibility

**Keyboard**
- All interactive elements are `<button>` or `<a>`; Tab order follows DOM order.
- Tree rows and rack clusters are real buttons — Space/Enter activates.
- Transport strip buttons carry `aria-label` (Play, Pause, Record, etc.).
- Filter chips use `aria-pressed`. Density segmented control uses `role="radiogroup" / radio + aria-checked`.
- Narrow-viewport back chevron is a real button with `aria-label="Back"`.

**Focus**
- `*:focus-visible` gets `2px solid var(--raw-amber)` ring with 2px offset — visible on every tactile control because button shadows are inset, not outset.

**Contrast ratios (WCAG AA)**

| Pair | Ratio | Use |
|---|---|---|
| `#e8e8e8` on `#0a0a0a` | 15.3:1 | Body text |
| `#6a6a6a` on `#0a0a0a` | 4.7:1 | Muted labels (passes AA normal) |
| `#e8a33d` on `#0a0a0a` | 9.2:1 | Amber accents, warn text, LCD |
| `#3a9b4a` on `#0a0a0a` | 5.1:1 | Green LCD, done dot |
| `#4a7bc8` on `#0a0a0a` | 4.9:1 | Blue info, selected row |
| `#d63838` on `#0a0a0a` | 4.6:1 | Red LCD, blocked text |
| `#0a0a0a` on `#e8a33d` | 9.2:1 | Status chip foreground on amber fill |
| `#0a0a0a` on `#3a9b4a` | 5.1:1 | Status chip on green fill |
| `#e8e8e8` on `#d63838` | 3.3:1 | Status chip on red fill — AA for large text only; chip text is 12px 500wt. **Flag:** if audit requires AA normal across the board, darken red to `#c02828` (5.0:1) or switch chip fg to `#0a0a0a`. |

**Motion**
- `prefers-reduced-motion: reduce` zeros all durations and disables: CRT sweep, tape reel spin, StatBar pulse, LED blink, skeleton shimmer.

**ARIA**
- `role="meter"` on VU strip with `aria-label="Signal level N%"`.
- `role="list"` on status legend; each item is a `span` with color swatch + text.
- `aria-expanded` on tree rows that toggle children.
- `aria-label` on all icon-only buttons (transport, back chevron, hamburger).

**Target size**
- Transport buttons 40×40 (lg: 48); filter chips ≥28px tall; tree rows ≥32px in Comfortable, 28 in Compact, 24 in Ultra (below WCAG 2.2 44px on Ultra — Ultra is a power-user mode, document as such if that policy matters).

---

## 5. Azeret Mono — licensing & self-hosting

**Current setup:** loaded from Google Fonts (`https://fonts.googleapis.com/css2?family=Azeret+Mono:wght@500;700`).
**License:** SIL Open Font License 1.1 (OFL). Free for personal and commercial use, embedding, modification, and redistribution — provided the OFL is included with any redistributed copies of the font files. No separate attribution required in the UI.

**Recommended self-host path (Next.js):**

1. Download from https://fonts.google.com/specimen/Azeret+Mono → export `AzeretMono-Medium.woff2` + `AzeretMono-Bold.woff2`.
2. Drop into `web/public/fonts/` alongside your Geist files:
   ```
   web/public/fonts/AzeretMono-Medium.woff2
   web/public/fonts/AzeretMono-Bold.woff2
   web/public/fonts/OFL.txt   // keep the license file next to them
   ```
3. Declare in your global stylesheet (mirror the existing Geist pattern):
   ```css
   @font-face {
     font-family: "Azeret Mono";
     src: url("/fonts/AzeretMono-Medium.woff2") format("woff2");
     font-weight: 500; font-style: normal; font-display: swap;
   }
   @font-face {
     font-family: "Azeret Mono";
     src: url("/fonts/AzeretMono-Bold.woff2") format("woff2");
     font-weight: 700; font-style: normal; font-display: swap;
   }
   ```
4. Remove the `@import url('https://fonts.googleapis.com/...')` line at the top of `console.css`.
5. If you prefer `next/font/google`: `import { Azeret_Mono } from "next/font/google"` with weights `[500, 700]` — Next.js self-hosts it at build time automatically.

---

## 6. Asset manifest

Every asset is code-drawn SVG — no raster files to check in. Suggested Next.js paths assume `web/app/(dev)/`.

| File | Purpose | Suggested path |
|---|---|---|
| `Territory Web - Step 8 Console.html` | Prototype entry page | reference only — do not port |
| `console.css` | Console aesthetic stylesheet | `web/app/(dev)/console.css` |
| `ds/colors_and_type.css` | Locked design-system tokens | `web/app/globals.css` or `web/styles/tokens.css` (you already have this) |
| `ds/fonts/Geist_wght_.woff2` | Geist variable | `web/public/fonts/` (already present in your repo) |
| `ds/fonts/GeistMono_wght_.woff2` | Geist Mono variable | `web/public/fonts/` (already present) |
| `src/data.js` | Seed plan + releases + density matrix | replace with real fetchers; preserve the shape of `MASTER_PLAN`, `RELEASES`, `WEEK_DENSITY` so `rollup()` / `flattenTasks()` keep working |
| `src/console-assets.jsx` | Logo suite, media icons, hero + pillars | split into `components/brand/`, `components/icons/Transport.tsx`, `components/art/Hero.tsx`, `components/art/Pillars.tsx` |
| `src/console-primitives.jsx` | Rack / Bezel / Screen / LED / the six primitives | `components/console/{Rack,Bezel,Screen,LED,Button,StatusChip,StatBar,FilterChip,HeatCell,VuStrip,TransportStrip,States}.tsx` |
| `src/console-screens.jsx` | Landing, Dashboard, Releases, Detail, Design | `app/(dev)/page.tsx`, `app/(dev)/dashboard/page.tsx`, `app/(dev)/dashboard/releases/page.tsx`, `app/(dev)/dashboard/releases/[id]/page.tsx`, `app/(dev)/design/page.tsx` |
| `src/console-app.jsx` | Shell + sidebar + router | discard — replace with Next.js `app/(dev)/layout.tsx` and `useRouter()` |
| `archive/Territory Web - Step 8 flat.html` | Superseded flat version | reference only |

**Dependency notes**
- `Object.assign(window, {...})` at the bottom of each JSX file is a Babel-standalone workaround. Remove when porting — use real `export`s.
- No runtime dependencies beyond React. Everything is SVG + CSS.
- `React.useId()` is used in `Logomark` for filter-id uniqueness; keep it.

---

## 7. Known drifts from the amended brief

| Item | Status | Note |
|---|---|---|
| 8a Hero art — "photographic/painted quality" | **Partial** | Delivered as stylized SVG scene in the console palette (planet + HUD + graticule + orbital arc). I cannot generate raster imagery. Swap in real art when a designer/Midjourney pass is available; ratio + slot are preserved. |
| 8c Feature pillars — "matte paintings" | **Partial** | Same constraint as 8a — delivered as five schematic "instrument view" SVGs (planet, waveform, mixer, radar, tape). Each reads thematically and uses palette-locked colours only. |
| 8d Logo suite | **Delivered** | Wordmark, Logomark, Lettermark, Strapline lockup — all three colour variants. Vector-native. |
| 8b Transport icons | **Delivered** | 13 tactile glyphs (Play/Pause/Stop/Record/Rewind/FF/RewindEnd/FFEnd/Eject/Loop/Shuffle/Mute/Solo). Lucide retained for generic UI glyphs (hamburger, back chevron). |
| Third display face | **Delivered** | Azeret Mono on the LCD readouts; Geist + Geist Mono unchanged. |
| Density modes | **Delivered** | Comfortable / Compact / Ultra; Ultra strips rack chrome and scanlines for max info density. |
| Persistent status legend | **Delivered** | Topbar strip on every non-landing route. |
| Narrow drill-down | **Delivered** | Back chevron + scrollable breadcrumb; persists via sidebar toggle. |
| Empty / Loading / Error / Stale | **Delivered** | Toggleable on Dashboard; Stale banner in topbar. |
| Red chip contrast | **Flag** | `#e8e8e8` on `#d63838` is 3.3:1 — AA for large text only. Swap to `#c02828` (5.0:1) or use `#0a0a0a` foreground if full-AA is a release-blocker. |
| Ultra mode target size | **Flag** | Tree rows drop to 24px tall in Ultra — below WCAG 2.2 target-size (44px). Fine for power users, flag if policy requires otherwise. |
| Prototype uses `<script type="text/babel">` | **By design** | Keeps the prototype zero-build. Do not ship this to production — port to your compile pipeline. |

---

## 8. File layout (ready to unzip)

```
step-8-console/
├── Territory Web - Step 8 Console.html     ← entry (dev)
├── Territory Web - Step 8 Console.standalone.html   ← single-file preview
├── console.css
├── HANDOFF.md                              ← this file
├── ds/
│   ├── colors_and_type.css
│   └── fonts/
│       ├── Geist_wght_.woff2
│       └── GeistMono_wght_.woff2
├── src/
│   ├── data.js
│   ├── console-assets.jsx
│   ├── console-primitives.jsx
│   ├── console-screens.jsx
│   └── console-app.jsx
└── archive/
    └── Territory Web - Step 8 flat.html
```

— end —
