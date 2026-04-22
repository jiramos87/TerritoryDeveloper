# Web design system — Territory Developer

**Scope:** Next.js 14+ App Router (`web/`), Tailwind v4, locked NYT-derived palette (`web/lib/tokens/palette.json`). Informed-by references: **Shopify** developer-portal structure + **Dribbble** breadcrumb/navigation patterns — see `docs/web-platform-post-mvp-extensions.md` **§8** (source observations; NB5). This file is the web-local authority for type, spacing, motion, semantic color aliases, component bindings, and accessibility rules.

---

## §1 — Type scale (10 levels, minor third 1.25)

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

**Reference alignment:** The **Shopify** dev-doc pattern (§8) pairs bold monospace identifiers with sans body — we keep `mono-code` / `mono-meta` on **JetBrains Mono** (see `globals.css` `--font-mono`) and body steps on **Inter** (`--font-sans`). **Dribbble** §8 notes: breadcrumb is **primary orientation** — use at least `body` for crumb text (≥16px effective on base scale); current segment may use `font-medium` (Tailwind) at `body` size.

**Line-height:** Pair each step with `line-height: 1.2` for headings (`display`–`h3`), `1.5` for `body-lg`–`caption`, `1.45` for `mono-code` / `mono-meta` (code blocks may override in MDX).

---

## §2 — Spacing (4px base, 9 stops)

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

**Dribbble** §8: generous gap between breadcrumb segments (`gap-2` = 8px = `xs`) — at least `xs` between crumb tokens; bar row `py-3` (12px = `sm` vertical) for landmark presence.

---

## §3 — Motion vocabulary

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

**Shopify** §8 (two-panel, tab switcher) is a *future* affordance; motion tokens above apply when those components land.

---

## §4 — Semantic color aliases

Palette file: `web/lib/tokens/palette.json`. **Raw** keys are NYT-locked; **game accents** are additive (`terrainGreen`, `waterBlue`, warm uses existing amber family).

### Text

| Alias | Role |
|-------|------|
| `text.primary` | Main body and headings on dark surfaces — maps to `raw.text` |
| `text.secondary` | Supporting labels — `raw.grey-500` |
| `text.meta` | Muted metadata — `raw.grey-500` (may diverge in token file later) |
| `text.disabled` | Non-interactive / placeholder — mix of `grey-500` at reduced opacity in UI; hex in tokens |

### Surface

| Alias | Role |
|-------|------|
| `surface.canvas` | App background — `raw.black` `#0a0a0a` |
| `surface.raised` | Panel/sidebar — `raw.panel` |
| `surface.sunken` | Inset fields, wells |
| `surface.inset` | Code blocks, nested blocks |

### Accent (game + brand)

| Alias | Role | Palette keys |
|-------|------|----------------|
| `accent.terrain` | Growth / map / “done” positive | `raw.terrainGreen` (T22.2 locks hex + contrast) |
| `accent.water` | Info, links, cool highlights | `raw.waterBlue` |
| `accent.warm` | Warnings, progress, energy | `raw.amber` (warm family; NB1 designer confirmation in T22.2) |

**WCAG:** Contrast ratios vs `surface.canvas` (`#0a0a0a`) for the three accent **foreground** uses are in **§4.1** below.

### §4.1 — Measured contrast (T22.2 / TECH-619)

`surface.canvas` = `#0a0a0a` (`raw.black`). Method: **WCAG 2.1** relative luminance; contrast ratio = (L_lighter + 0.05) / (L_darker + 0.05).

| Accent | `palette.json` key | Hex | Contrast vs canvas | ≥ 4.5:1 (AA body) | Notes |
|--------|-------------------|-----|--------------------|-------------------|--------|
| Terrain | `raw.terrainGreen` | `#3a9b4a` | **5.63:1** | Yes | **NB1** — same hue as `raw.green` (status “done”); kept unified for product coherence. |
| Water | `raw.waterBlue` | `#38bdf8` | **9.24:1** | Yes | Chosen sky/water read; high contrast on dark. |
| Warm | `raw.amber` | `#e8a33d` | **9.18:1** | Yes | Warm candidate per Stage Intent; existing NYT-locked family. **NB1** — if future UI needs a redder “alert warm,” re-open palette extension (separate task). |

**Text-on-canvas (accent as foreground):** all three pass **AA** for **normal** text on `surface.canvas` when used as the primary text color. **Fill / chip** patterns that place **text on top of a saturated accent swatch** should use a paired dark fg (e.g. `text-status-*-fg` style rows in `palette.json`); that pairing is not re-listed here.

---

## §5 — Component map (scale · spacing · motion)

Bindings are **defaults**; components may override with design-review.

| Component area | Type scale | Spacing | Motion |
|----------------|------------|---------|--------|
| **Breadcrumb** | `body` + `font-medium` on current segment; `mono-meta` for path-only variant | `xs` between crumbs; `sm` vertical padding for bar | `subtle` on dropdown (future) |
| **Sidebar** | `body-sm` items; `h3` section headers | `md` item padding; `md`–`lg` between groups | `gentle` expand/collapse |
| **Dashboard cards** | `h3` title; `body` metric | `md` card padding; `lg` between cards | `subtle` hover |
| **Badge / chip** | `caption` or `body-sm` | `2xs`–`xs` padding | `instant` color |
| **Data table** | `body-sm` cells; `mono-code` for IDs | `sm` row padding | `subtle` row hover |
| **MDX code block** (future) | `mono-code` | `md` padding; `surface.inset` | `subtle` focus |

**Shopify** — sidebar tree, badges, two-panel: map to this table as those ship (§8).

**Dribbble** — breadcrumb: already matches §1/§2 defaults above.

---

## §6 — Accessibility

- **Contrast:** All **default** `text.*` and `surface.*` pairings in §4 must meet **WCAG 2.1 AA** (4.5:1 for normal text, 3:1 for large/mono-code where specified). **Accent** text-on-canvas verified in **§4.1** (TECH-619).
- **Focus:** `focus-visible` ring 2px `outline-style: solid`, `outline-color` = `accent.water` or dedicated focus token; offset 2px; **never** `outline: none` without replacement.
- **Keyboard:** All interactive Breadcrumb / Sidebar / Dashboard entries remain tab-reachable; skip links and landmark roles unchanged from app layout.
- **Motion:** `prefers-reduced-motion: reduce` forces 0ms duration globally for `ds` motion tokens (see `globals.css`).

---

## §7 — CD Pilot Bundle appendix

> **Maintenance:** This section transcribes the Step 8 console pilot handoff for spec alignment. When the bundle under `web/design-refs/step-8-console/` changes, re-run `npx tsx tools/scripts/transcribe-cd-tokens.ts --from-source` for token/CSS/TS sync, then update this appendix if `HANDOFF.md` narrative drifts.

### Canonical sources

| Source | Role |
|--------|------|
| `web/design-refs/step-8-console/HANDOFF.md` | Primary developer handoff (component inventory, tokens, a11y, asset manifest, known drifts). |
| `docs/web-platform-post-mvp-extensions.md` | **`### CD Pilot Bundle — 2026-04-18`** — pilot capture metadata, bundle layout, token delta vs `palette.json`, fidelity gate, follow-up surfaces. |
| `docs/web-platform-post-mvp-extensions.md` **§8** | Informed-by references: **Shopify** developer-portal patterns (sidebar tree, two-panel code layout, badge chips) and **Dribbble** breadcrumb navigation pattern — see **NB5** in that doc so future work keeps visual provenance. |

### Pilot scope (from handoff)

- **Stack (prototype):** React 18 UMD + Babel standalone in the pilot bundle; production port targets Next.js App Router (`web/`).
- **Tokens:** Locked in `ds/colors_and_type.css` — raw palette, semantic aliases, type scale, spacing (4px base), radii (max 8px + pill), elevation shadows, motion (four duration stops + enter/exit easing), focus ring. Console-specific additions (e.g. metal stops, `--font-lcd`) live in `console.css` in the bundle, not in the extracted `@theme` pipeline.
- **Motion:** CD uses `--dur-fast` (80ms) through `--dur-reveal` (480ms); `prefers-reduced-motion: reduce` collapses durations and disables continuous animations (CRT sweep, reel, pulse, blink, shimmer).
- **Primitives (inventory):** Chrome — `Rack`, `Bezel`, `Screen`, `LED`, `TapeReel`, `VuStrip`, `TransportStrip`. Data — `Button`, `StatusChip`, `IdChip`, `StatBar`, `FilterChip`, `HeatCell`, plus table styling via `.table` in `console.css`. Helpers — `Legend`, `DensityToggle`, empty/loading/error/stale states. Assets — logo suite, `TIcon.*` transport family, hero and pillar art, `Sparkline`. Screens — Landing, Dashboard, Releases, Detail, Design kit.
- **Accessibility (summary):** Real controls (`<button>` / `<a>`), `aria-label` on icon-only controls, `aria-pressed` on filter chips, focus-visible ring using warn/amber accent, documented contrast table in the handoff (including flags for red chip on-blocked and Ultra density row height).

### Token delta vs locked palette (extensions narrative)

The extensions doc records pilot approval of **`--raw-blue` / info role** and semantic additions derived without new raws beyond that delta. Drift vs `web/lib/tokens/palette.json` is enforced in CI via `tools/scripts/extract-cd-tokens.ts` and `web/design-refs/step-8-console/.drift-report.md`.

### Known drifts and flags (from handoff §7)

- Hero and pillar art delivered as stylized SVG (not raster “matte painting”).
- Transport icons delivered as outline set; solid variants optional at port time.
- Red status chip: contrast flag in handoff — evaluate `#c02828` or dark foreground if AA-normal is required on small text.
- Ultra density: row height may fall below WCAG 2.2 target size — document as power-user mode if policy requires 44px minimum.

### D6 — Asset pipeline decision (S-CD3, Stage 26)

| Category | Storage | Rationale |
| --- | --- | --- |
| Brand logos, hero art, pillar scenes | **`public/` static SVG** under `web/public/design/*.svg` | Vercel CDN cache + indexable URLs; theme binds via `style` + `--ds-*` (and fallbacks) so files stay palette-aligned when **inlined** in React/MDX. |
| 13-glyph **TIcon** media family | **Inline React** in `web/components/console/icons/TIcon.tsx` | `currentColor` + app CSS; no separate HTTP fetch; matches App Router colocation. |
| Sprite sheet (single raster/vector bundle) | **Rejected** for default path | No stock sprite pipeline in App Router; would add a build step. Revisit only if a bundler plugin becomes canonical. |

**`web/public/design/` filename convention (Stage 26 Exit):** `logomark.svg`, `wordmark.svg`, `lettermark.svg`, `strapline-lockup.svg`, `hero-art.svg`, `pillar-planet.svg`, `pillar-signal.svg`, `pillar-mixer.svg`, `pillar-radar.svg`, `pillar-tape.svg`. Source traces: `web/design-refs/step-8-console/src/console-assets.jsx` (CD bundle, read-only).

#### §7.1 — Static `public/design` inventory (CD → file)

| File | CD source (approx.) |
| --- | --- |
| `logomark.svg` | `Logomark` (logo suite) |
| `wordmark.svg` | `Wordmark` |
| `lettermark.svg` | `Lettermark` |
| `strapline-lockup.svg` | `StraplineLockup` |
| `hero-art.svg` | `HeroArt` |
| `pillar-planet.svg` | `PillarPlanet` |
| `pillar-signal.svg` | `PillarSignal` |
| `pillar-mixer.svg` | `PillarMixer` |
| `pillar-radar.svg` | `PillarRadar` |
| `pillar-tape.svg` | `PillarTape` |

### Asset manifest (abbrev.)

The handoff §6 maps each bundle file to suggested Next.js destinations (`components/console/*`, `app/*` routes, `public/fonts`, etc.). Prototype-only HTML entries are reference-only.

---

## Document control

| Item | Value |
|------|--------|
| Informed-by | `docs/web-platform-post-mvp-extensions.md` §8 (Shopify + Dribbble observations, NB5) |
| Palette | `web/lib/tokens/palette.json` |
| Code mirror | `web/lib/design-tokens.ts` (Stage 22 T22.3) + `cdBundle` (Stage 24 CD transcription) |
| CSS vars | `web/app/globals.css` `@theme` `ds` prefix (T22.4) + `/* CD-BUNDLE-START */` block (Stage 24) |
| CD pilot | `web/design-refs/step-8-console/HANDOFF.md` + extensions `### CD Pilot Bundle — 2026-04-18` |
