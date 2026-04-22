# web-platform — Stage 26 Plan Digest

Compiled 2026-04-22 from 4 task spec(s): TECH-646, TECH-647, TECH-648, TECH-649.

Source orchestrator: `ia/projects/web-platform-master-plan.md` — Stage 26.

---

## §Plan Digest

### §Goal

Lock **D6** in `web/lib/design-system.md` §7 and keep `web/public/design/` tracked for **TECH-647** SVG drops.

### §Acceptance

- [ ] §7 states: hero + pillar art → `public/` SVG; 13-glyph family → inline React + `currentColor`; sprite sheet rejected with App Router rationale.
- [ ] `web/public/design/.gitkeep` on branch; §7 lists ten basenames from Stage 26 Exit.
- [ ] `npm run validate:web` exit 0 when markdown under `web/` changes.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| d6_doc | read §7 | D6 prose + basename list | manual |
| validate_web | — | exit 0 | `npm run validate:web` |

### §Examples

| Category | Storage | Theme |
|----------|---------|-------|
| Brand + pillars | `web/public/design/` | `style` + `var(--ds-*)` |
| Transport glyphs | `TIcon` module | `currentColor` |

### §Mechanical Steps

#### Step 1 — §7 D6 subsection

**Goal:** Append scoped D6 decision + filename convention under existing §7 / CD appendix flow.

**Edits:** In `web/lib/design-system.md`, insert subsection after the nearest §7 / CD pilot anchor: prose table or bullet block naming `logomark`, `wordmark`, `lettermark`, `strapline-lockup`, `hero-art`, five `pillar-*` files; cite **S-CD3**; reject sprite sheet with bundler rationale.

**Gate:**

```bash
npm run validate:web
```

**STOP:** If MDX/build chokes on markdown — fix heading level / fence pairing; re-run gate.

**MCP hints:** `plan_digest_resolve_anchor` on `## §7` in `web/lib/design-system.md`; `backlog_issue` **TECH-646**.

#### Step 2 — Directory anchor

**Goal:** Confirm `web/public/design/` tracked.

**Edits:** Ensure `web/public/design/.gitkeep` exists (may be empty); no binary assets in this task.

**Gate:**

```bash
test -f web/public/design/.gitkeep
```

**STOP:** Missing file — add gitkeep; re-run gate.

**MCP hints:** `plan_digest_verify_paths` for `web/public/design/.gitkeep`.
---
## §Plan Digest

### §Goal

Emit ten standalone SVGs under `web/public/design/` from CD `console-assets.jsx`, palette-bound via `--ds-*`, and extend §7 inventory.

### §Acceptance

- [ ] Exit basename set complete (logos, hero, five pillars).
- [ ] No literal hex fills — `style` uses `var(--ds-…)` or `currentColor` where appropriate.
- [ ] §7 appendix row per asset citing CD path.
- [ ] `npm run validate:web` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| svg_count | directory listing | ten `.svg` entries | shell |
| validate_web | — | exit 0 | `npm run validate:web` |

### §Examples

| Output | CD source |
|--------|-----------|
| Logo marks | `console-assets.jsx` logo JSX |
| Pillar scenes | pillar inline components |

### §Mechanical Steps

#### Step 1 — Extract SVG bodies

**Goal:** Map each Exit basename to an `<svg>` fragment; write one file per basename under `web/public/design/`.

**Edits:** Read `web/design-refs/step-8-console/src/console-assets.jsx`; for each required basename from Stage 26 Exit, write matching `.svg` beside `web/public/design/.gitkeep`; replace hard-coded fills with `style` attributes referencing `--ds-*` tokens (no `#rrggbb`).

**Gate:**

```bash
npm run validate:web
```

**STOP:** If CD structure drift blocks copy — open `console-assets.jsx` and re-anchor; do not edit under `web/design-refs/`.

**MCP hints:** `plan_digest_render_literal` on `web/design-refs/step-8-console/src/console-assets.jsx` (bounded lines).

#### Step 2 — §7 inventory

**Goal:** Table/list in `web/lib/design-system.md` §7: filename, CD reference, palette notes.

**Edits:** Append rows to §7 near D6 / CD appendix per **TECH-646** convention.

**Gate:**

```bash
npm run validate:web
```

**STOP:** Markdown parse error — fix table fences; re-run.

**MCP hints:** `plan_digest_resolve_anchor` on latest §7 heading in `web/lib/design-system.md`.
---
## §Plan Digest

### §Goal

Ship `TIcon` namespace with thirteen RSC SVG glyphs from CD `console-assets.jsx`, exported through `web/components/console/index.ts`.

### §Acceptance

- [ ] `TIcon.Play` … `TIcon.Solo` present on single namespace export.
- [ ] Shared props: `size`, `className`, `aria-label`.
- [ ] `fill="currentColor"` on vector markup.
- [ ] `npm run validate:web` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| typecheck | `validate:web` | exit 0 | node |

### §Examples

| Glyph | Notes |
|-------|-------|
| `TIcon.Eject` | transport chrome |
| `TIcon.Loop` | repeat affordance |

### §Mechanical Steps

#### Step 1 — Author icon module

**Goal:** New `icons/TIcon.tsx` beside existing console components; thirteen inner SVGs.

**Edits:** Add `TIcon.tsx` under `web/components/console/icons/` (new folder) copying paths from `web/design-refs/step-8-console/src/console-assets.jsx`; export `TIcon` object with stable display names; no hooks.

**Gate:**

```bash
npm run validate:web
```

**STOP:** Type errors on SVG props — align with `web/components/console/Screen.tsx` patterns; re-run gate.

**MCP hints:** `plan_digest_render_literal` on `web/design-refs/step-8-console/src/console-assets.jsx`.

#### Step 2 — Barrel

**Goal:** Consumers import from `web/components/console/index.ts`.

**Edits:** Re-export `TIcon` from `web/components/console/index.ts` without breaking existing primitive exports.

**Gate:**

```bash
npm run validate:web
```

**STOP:** Circular import — split type-only import or inline re-export; re-run gate.

**MCP hints:** `plan_digest_resolve_anchor` on `export` lines in `web/components/console/index.ts`.
---
## §Plan Digest

### §Goal

Deliver client `MediaTransport` composing `TransportStrip` + `TIcon`, and extend dev design-system page with icon matrix + state demos.

### §Acceptance

- [ ] Props: `state` union includes `recording`; `actions` partial map matches Intent keys.
- [ ] Buttons expose `aria-label`; **NB-CD3** note in file header comment.
- [ ] Page renders all thirteen glyphs plus four `MediaTransport` states.
- [ ] `npm run validate:web` green.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | — | exit 0 | `npm run validate:web` |

### §Examples

| state | Expected chrome |
|-------|-------------------|
| `stopped` | idle transport |
| `recording` | distinct from `playing` |

### §Mechanical Steps

#### Step 1 — Composite component

**Goal:** `MediaTransport.tsx` next to `TransportStrip.tsx`; maps `actions` into `TransportStrip` `onAction` callback; decorates or labels with `TIcon` exports.

**Edits:** New `MediaTransport.tsx` under `web/components/console/`; import `TransportStrip` from `web/components/console/TransportStrip.tsx` and `TIcon` from `web/components/console/icons/TIcon.tsx`; `'use client'` top.

**Gate:**

```bash
npm run validate:web
```

**STOP:** Prop mismatch vs `TransportStrip` — read `web/components/console/TransportStrip.tsx` and adjust map; re-run gate.

**MCP hints:** `backlog_issue` **TECH-638** for archived acceptance text.

#### Step 2 — Showcase

**Goal:** Append sections to `web/app/(dev)/design-system/page.tsx`: grid of `TIcon.*` + four `MediaTransport` instances.

**Edits:** Extend page file with new `<section>` blocks; reuse existing NODE_ENV guard; no new routes.

**Gate:**

```bash
npm run validate:web
```

**STOP:** RSC/client boundary error — mark only interactive demo subtree client; re-run gate.

**MCP hints:** `plan_digest_resolve_anchor` on last showcase section in `web/app/(dev)/design-system/page.tsx`.

#### Step 3 — Barrel export

**Goal:** Export `MediaTransport` via `web/components/console/index.ts`.

**Edits:** Append named export consistent with other console primitives.

**Gate:**

```bash
npm run validate:web
```

**STOP:** Duplicate identifier — rename local helper; re-run gate.

## Final gate

```bash
npm run validate:all
```
