# web-platform — Stage 22 Plan Digest

Compiled 2026-04-22 from 4 task spec(s).

---

## §Plan Digest

### §Goal

Create the design-system markdown under `web/lib/` (filename per **Files** + Stage Exit) with §1–§6 (type, spacing, motion, aliases, component map, a11y) and cite Dribbble + Shopify sources from `docs/web-platform-post-mvp-extensions.md` §8.

### §Acceptance

- [ ] File exists on disk at path listed in backlog **Files** with all sections per master-plan T22.1 Intent.
- [ ] Dribbble + Shopify reference lines cite extensions doc §8 (NB5).
- [ ] Prose length stays near ≤10 pages.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | repo after write | exit 0 | node |

### §Examples

| Section | Must include |
|---------|----------------|
| §1 | 10 levels, minor-third, `display` → `mono-meta`rem ranges |
| §3 | `instant` / `subtle` / `gentle` / `deliberate` ms; reduced-motion policy |

### §Mechanical Steps

#### Step 1 — Author design-system markdown

**Goal:** Land markdown file with §1–§6 matching Stage exit bullets.

**Edits:**

- New file under `web/lib/` (markdown; filename in backlog **Files** field) — **before**:
  ```
  (no file on disk)
  ```
  **after**:
  ```
  (full §1–§6 prose; cite post-MVP extensions §8 for Dribbble + Shopify; ≤~10 pages)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Re-open the new spec file under `web/lib/`; fix prose; re-run `npm run validate:web` until exit 0.

**MCP hints:** `glossary_lookup` for UI terms if needed.

---

## §Plan Digest

### §Goal

Read raws from locked palette JSON; write §4 accent + contrast in design-system spec (path per backlog **Files**); target WCAG AA vs `#0a0a0a`.

### §Acceptance

- [ ] §4 lists three accents + contrast ratios vs surface canvas hex from spec.
- [ ] Hex values trace to palette keys; NB1 calls documented.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | after edit | exit 0 | node |

### §Examples

| Accent | Source key |
|--------|----------------|
| Terrain | `terrainGreen` |
| Water | `waterBlue` |
| Warm | warm candidate per Intent |

### §Mechanical Steps

#### Step 1 — Update design-system doc §4

**Goal:** Add contrast table for three accents on dark canvas.

**Edits:**

- Design-system markdown under `web/lib/` (see backlog **Files**) — **before**:
  ```
  (§4 body before this task)
  ```
  **after**:
  ```
  (§4 with terrain, water, warm + measured ratios + NB1 note)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Re-edit §4 if contrast math wrong; re-run gate until exit 0.

**MCP hints:** `backlog_issue` { id: "TECH-619" }.

Read palette from `web/lib/tokens/palette.json` (file exists on HEAD).

---

## §Plan Digest

### §Goal

Ship TS token module + unit test under `web/lib/` (paths per backlog **Files**); import palette JSON raws without mutation.

### §Acceptance

- [ ] Exports: `typeScale` (10), `spacing` (9), `motion` + `reducedMotion`, `text`, `surface`, `accent`.
- [ ] Tests: monotonic rem, 9 stops, motion keys, `reducedMotion.duration === 0`, alias hex matches palette.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| design_tokens_test | `npm --prefix web test` | pass | node |

### §Examples

| Export | Invariant |
|--------|-----------|
| `motion.reducedMotion.duration` | `0` |
| `typeScale[0].rem` | `>` typeScale[9].rem |

### §Mechanical Steps

#### Step 1 — Add token TypeScript module

**Goal:** TS `as const` tree per master-plan; JSDoc on `motion.reducedMotion` points to `globals.css` media query in follow-on task (TECH-621).

**Edits:**

- New TS file under `web/lib/` (name per **Files** list) — **before**:
  ```
  (no file on disk)
  ```
  **after**:
  ```
  (new module — exports per §Acceptance; import JSON raws from `web/lib/tokens/palette.json`)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Fix types or exports; re-run gate until exit 0.

#### Step 2 — Add unit tests

**Goal:** Assert scale, spacing, motion, reduced duration, alias resolution.

**Edits:**

- New test file under `web/lib/__tests__/` (name per **Files**) — **before**:
  ```
  (no file on disk)
  ```
  **after**:
  ```
  (test cases per §Test Blueprint)
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Adjust tests or module exports; re-run until exit 0.

**MCP hints:** `backlog_issue` { id: "TECH-620" }.

---

## §Plan Digest

### §Goal

Append `--ds-*` custom properties to `web/app/globals.css` inside `@theme`; align names with `web/lib/design-tokens.ts`; add `@media (prefers-reduced-motion: reduce)` zeroing duration vars; preserve existing non-`ds-*` entries.

### §Acceptance

- [ ] All `--ds-font-size-*`, `--ds-spacing-*`, `--ds-duration-*`, `--ds-text-*`, `--ds-surface-*`, `--ds-accent-*` present per T22.4 Intent; B1 satisfied.
- [ ] Reduced-motion block sets duration tokens to `0ms`.
- [ ] `npm run validate:web` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | after CSS edit | exit 0 | node |

### §Examples

| Prefix | B1 |
|--------|-----|
| `--ds-` | no collision with `--color-*` |

### §Mechanical Steps

#### Step 1 — Extend `web/app/globals.css` `@theme`

**Goal:** Append `ds` variables + reduced-motion block without deleting prior `@theme` lines.

**Edits:**

- `web/app/globals.css` — **before**:
  ```
  (last lines of @theme block before append — use plan_digest_render_literal for exact anchor)
  ```
  **after**:
  ```
  (same + appended --ds-* block + @media (prefers-reduced-motion: reduce) { --ds-duration-*: 0ms; } )
  ```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Re-open `web/app/globals.css` if CSS parse fails; re-run gate until exit 0.

**MCP hints:** `plan_digest_resolve_anchor` on `@theme` close, `plan_digest_verify_paths` on `web/app/globals.css`.

## Final gate

```bash
npm run validate:all
```
