# web-platform — Stage 23 Plan Digest

Compiled 2026-04-22 from 4 task spec(s): TECH-622, TECH-623, TECH-624, TECH-625.

Source orchestrator: `ia/projects/web-platform-master-plan.md` — Stage 23.

---

## §Plan Digest — TECH-622

### §Goal

Ship `Heading` RSC (ten `level` values) with semantic tag map and `text-[var(--ds-font-size-*)]` classes aligned to `web/lib/design-system.md` + `@theme` tokens (Stage 22).

### §Acceptance

- [ ] Type exports cover all ten `level` values; tag map matches `ia/projects/web-platform-master-plan.md` Stage 23 T23.1 Intent.
- [ ] JSDoc points readers at `web/lib/design-system.md` §5.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | repo | exit 0 | node |

### §Examples

| level | element |
|-------|---------|
| `h2` | `h2` |
| `body-sm` | `p` |
| `mono-code` | `span` |

### §Mechanical Steps

#### Step 1 — Add Heading module

**Goal:** Create the RSC with tag map + `var()`-based `text-[]` classes per level.

**Edits:**

- First path in backlog `files[0]` for this issue (see `ia/backlog/TECH-622.yaml`) — **before**:
  ```
  (file absent on disk)
  ```
  **after**:
  ```
  (named export `Heading` + props: level, children, optional weight, optional className;
   build className with `text-[var(--ds-font-size-${level})]`; map level → h1|h2|h3|p|span)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Re-open the new module; fix type or Tailwind class errors; re-run the Gate.

**MCP hints:** `backlog_issue` `{"id": "TECH-622"}`, `glossary_lookup` (UI terms if naming conflicts).

---

## §Plan Digest — TECH-623

### §Goal

Add `Prose` RSC wrapper with `[&>*+*]:mt-[var(--ds-spacing-md)]` between direct children; JSDoc cites `web/lib/design-system.md` §5; no inline `style` props.

### §Acceptance

- [ ] RSC: optional `className` merged with stack-spacing utility.
- [ ] JSDoc references `web/lib/design-system.md` §5.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | repo | exit 0 | node |

### §Examples

| children layout | effect |
|-----------------|--------|
| two block elements | second gets top margin from selector |

### §Mechanical Steps

#### Step 1 — Prose component

**Goal:** Land wrapper `div` (or semantically neutral element) with the spacing utility on the root `className`.

**Edits:**

- First path in backlog `files[0]` (`ia/backlog/TECH-623.yaml`) — **before**:
  ```
  (file absent on disk)
  ```
  **after**:
  ```
  (export `Prose` with children + className; include `[&>*+*]:mt-[var(--ds-spacing-md)]`; JSDoc block cites design-system §5)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** If Tailwind class rejected, adjust to valid v4 arbitrary form; re-run Gate.

**MCP hints:** `backlog_issue` `{"id": "TECH-623"}`.

---

## §Plan Digest — TECH-624

### §Goal

Ship `Surface` with `tone` + `padding` + optional `motion`; RSC when `motion="none"`; client sub-module + `globals.css` motion rules (extensions doc Example 2) for animated modes; B2 default.

### §Acceptance

- [ ] `motion` default `none` keeps server-safe entry path.
- [ ] `globals.css` includes transition + `@media (prefers-reduced-motion: reduce)` collapse aligned to Stage 23 Exit.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | repo | exit 0 | node |

### §Examples

| motion | render path |
|--------|----------------|
| `none` | RSC `div` |
| `subtle` | client branch sets `data-mounted` after mount |

### §Mechanical Steps

#### Step 1 — Component files

**Goal:** Add `Surface` + optional small client module if split; wire `bg-[var(--ds-surface-{tone})]`, `p-[var(--ds-spacing-{padding})]`.

**Edits:**

- Paths listed in `ia/backlog/TECH-624.yaml` `files` (component first) — **before**:
  ```
  (component file absent; globals.css exists)
  ```
  **after**:
  ```
  (Surface exports; conditional client wrapper for motion not `none`; data-mounted in client path)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** If `'use client'` accidentally wraps default export used with `motion="none"`, refactor split; re-run Gate.

**MCP hints:** `backlog_issue` `{"id": "TECH-624"}`.

#### Step 2 — globals.css motion block

**Goal:** Append transition CSS per `docs/web-platform-post-mvp-extensions.md` Example 2 (durations from `--ds-duration-*`); add reduced-motion collapse.

**Edits:**

- `web/app/globals.css` — **before**:
  ```
  (tail of @theme + prior rules; exact snapshot from working tree)
  ```
  **after**:
  ```
  (new block: selectors for surface motion + prefers-reduced-motion media query; no removal of prior DS token lines)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** If `validate:web` flags CSS parse or Tailwind conflict, fix ordering; re-run Gate.

**MCP hints:** `spec_section` for extensions doc if Example 2 anchor needed.

---

## §Plan Digest — TECH-625

### §Goal

Add dev-only `web/app/(dev)/design-system/page.tsx` (URL `/design-system`; do not use `app/_…` — private folder) with NB2 `notFound()` guard in production, full primitive matrix, `BadgeChip` swatches, CSS var table, and `metadata.robots.index` false; leave `web/components/Sidebar.tsx` unmodified.

### §Acceptance

- [ ] First statements: `process.env.NODE_ENV === 'production'` → `notFound()`.
- [ ] Renders: all `Heading` levels, `Prose` sample, `Surface` matrix, motion demos, `BadgeChip` row, var table; metadata noindex.
- [ ] `Sidebar` `LINKS` unchanged.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | repo | exit 0 | node |

### §Examples

| check | expect |
|-------|--------|
| prod guard | `notFound()` on prod build path |
| nav | `grep Sidebar` has no `/design-system` link added |

### §Mechanical Steps

#### Step 1 — /design-system page

**Goal:** Create the RSC page file per backlog `files[0]`.

**Edits:**

- New route module per `ia/backlog/TECH-625.yaml` `files[0]` — **before**:
  ```
  (file absent on disk)
  ```
  **after**:
  ```
  (default export: NODE_ENV check + showcase layout; import Heading, Prose, Surface, BadgeChip; metadata export robots.index false; English section labels)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Re-open the route module; fix import graph; re-run Gate.

**MCP hints:** `backlog_issue` `{"id": "TECH-625"}`.
