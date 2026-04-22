# web-platform — Stage 28 Plan Digest

Compiled 2026-04-22 from 2 task spec(s). Orchestrator: `ia/projects/web-platform-master-plan.md`.

| Issue   | Task |
| ------- | ---- |
| TECH-667 | T28.1 Breadcrumb + Sidebar token migration |
| TECH-668 | T28.2 Primitives + wiki/devlog Prose |

---

## §Plan Digest

### §Goal

Deliver Stage 28 first table task: inventory `tokens.` usage, migrate `web/components/Breadcrumb.tsx` and
`web/components/Sidebar.tsx` to `ds-*` Tailwind v4 class patterns without visual drift (NB4
alias table).

### §Acceptance

- [ ] Rg (or `rg "tokens\." web/app web/components`) output attached to PR or pasted in PR
  description.
- [ ] `web/components/Breadcrumb.tsx` and `web/components/Sidebar.tsx` carry no old `tokens.`-driven
  class or inline style that still maps to a `ds-*` key in `web/lib/design-system.md`.
- [ ] `npm run validate:web` exits 0 from repository root.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | tree after Breadcrumb + Sidebar token edits | exit 0 | `npm run validate:web` (repo root) |
| tsc_rollup | same | Next build phase passes | part of validate:web |

### §Examples

| Surface | Check |
|---------|--------|
| Breadcrumb | Last crumb and links keep contrast; nav landmark unchanged. |
| Sidebar | `LINKS` order and active state coloring preserved after token swap. |

### §Mechanical Steps

#### Step 1 — Inventory

**Goal:** capture grep hits before code edits.

**Edits:** none (command-only).

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && rg "tokens\." web/app web/components 2>/dev/null | head -n 200
```

**STOP:** if rg missing, use `npm exec -- npx -y ripgrep` or project-approved grep; do not delete
files when inventory empty — still open `web/components/Breadcrumb.tsx` and
`web/components/Sidebar.tsx` for the Step 2 edit.

**MCP hints:** `plan_digest_verify_paths` for `web/components/Breadcrumb.tsx`,
`web/components/Sidebar.tsx`.

#### Step 2 — Migrate Breadcrumb classes

**Goal:** move class strings in `Breadcrumb` to `var(--ds-*)` arbitrary classes per
`web/lib/design-system.md` alias table.

**Edits:**

- `web/components/Breadcrumb.tsx` — **before**:
  ```
  <nav aria-label="Breadcrumb" className="flex items-center flex-wrap gap-2 text-base py-3 mb-6">
  ```
  **after:** same line shape; replace `text-text-muted`, `text-text-primary`, and any `tokens-`
  fragments with the matching `text-[var(--ds-…)]` / `text-ds-…` class strings chosen from the
  design-system table (one edit pass; preserve `aria` and `Link` structure).

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** exit non-zero → read TypeScript error; fix classnames; re-run Gate. Do not hand-edit
this open Backlog spec to hide TypeScript errors.

**MCP hints:** `backlog_issue` (TECH-667) for issue row refresh after scope change.

#### Step 3 — Migrate Sidebar tokens

**Goal:** remove `import { tokens } from '@/lib/tokens'` style-driven colors from mobile toggle +
nav column; use `ds-*` classes or `className` + CSS vars instead, preserving layout and active
link styling.

**Edits:**

- `web/components/Sidebar.tsx` — **before**:
  ```
  import { tokens } from '@/lib/tokens';
  ```
  **after:** remove direct `tokens.colors[...]` style props; replace with `className` strings using
  `bg-[var(--ds-…)]`, `text-[var(--ds-…)]` per `web/lib/design-system.md` (retain
  `usePathname` / `LINKS` / mobile button behavior).

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** if `tokens` import still required for an edge case, document in `## 6. Decision Log` with
rationale; otherwise zero runtime `tokens` import remains.

**MCP hints:** `plan_digest_resolve_anchor` to confirm a single `import { tokens` match before
edit.

---

## §Plan Digest

### §Goal

Stage 28 second table task: migrate `BadgeChip`, `DataTable`, and `FilterChips` to `ds-*` Tailwind classes; wrap
wiki and devlog page bodies in `Prose` from `web/components/type/Prose.tsx` without layout reflow.

### §Acceptance

- [ ] `web/components/BadgeChip.tsx`, `web/components/DataTable.tsx`, `web/components/FilterChips.tsx`
  contain no `tokens.` class or style path that has a `ds-*` replacement in
  `web/lib/design-system.md`.
- [ ] `web/app/wiki/page.tsx`, `web/app/wiki/[...slug]/page.tsx`, `web/app/devlog/page.tsx`,
  `web/app/devlog/[slug]/page.tsx` render article content inside `<Prose>…</Prose>`.
- [ ] `npm run validate:web` exit 0; PR text records a localhost pass over `/wiki` and `/devlog`.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | post-edit tree | exit 0 | `npm run validate:web` at repo root |
| prose_routes | four page files | single `Prose` import each | tsc (via validate:web) |

### §Examples

| File | Prose target |
|------|--------------|
| `web/app/wiki/page.tsx` | Main column MDX or body output |
| `web/app/wiki/[...slug]/page.tsx` | Article column after same pattern as list page |
| `web/app/devlog/page.tsx` / `[slug]/page.tsx` | Post list / detail body region |

### §Mechanical Steps

#### Step 1 — Primitives token migration

**Goal:** apply `ds-*` classes to the three shared primitives; preserve props + behavior.

**Edits:**

- `web/components/BadgeChip.tsx` — **before:** first line with `className` or `tokens` usage tied to
  `tokens.`. **after:** `bg-[var(--ds-…)]` / `text-[var(--ds-…)]` per alias rows in
  `web/lib/design-system.md` (one cohesive edit; keep named exports).
- `web/components/DataTable.tsx` — same pattern for table shell + header cells.
- `web/components/FilterChips.tsx` — same pattern for chip selected/hover/focus.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** on TS error, fix typings first; re-run Gate; do not drop sort/filter state hooks.

**MCP hints:** `plan_digest_verify_paths` on `web/components/BadgeChip.tsx`, `web/components/DataTable.tsx`, and
`web/components/FilterChips.tsx`; `glossary_lookup` when adding a new wrapper name.

#### Step 2 — Wiki Prose wrap

**Goal:** add `import { Prose } from '@/components/type/Prose'` (or existing alias) and wrap the
MDX or HTML column for both wiki routes.

**Edits:**

- `web/app/wiki/page.tsx` — **before:** outer layout JSX that currently renders article body
  without `Prose`. **after:** wrap the body fragment with `<Prose>...</Prose>`; keep RSC + data
  fetching intact.
- `web/app/wiki/[...slug]/page.tsx` — same transformation for the detail article column.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** if MDX already nested inside a parent `Prose`, skip double-wrap — log short note in
`## 6. Decision Log` instead of stacking wrappers.

**MCP hints:** `plan_digest_render_literal` for 12-line read of each page before/after if anchor
unstable.

#### Step 3 — Devlog Prose wrap + PR note

**Goal:** same Prose policy for `web/app/devlog/page.tsx` and `web/app/devlog/[slug]/page.tsx`.
Close with PR text stating `/wiki` and `/devlog` were opened locally after Gate passes.

**Edits:**

- `web/app/devlog/page.tsx` — wrap the list/post excerpt column with `Prose` when it renders
  long-form text.
- `web/app/devlog/[slug]/page.tsx` — wrap the post body subtree with `Prose`.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:web
```

**STOP:** non-zero build → read first Next error; fix import path or RSC/Client boundary; re-run.

**MCP hints:** `backlog_issue` (TECH-668) to confirm scope; `plan_digest_lint` on the finished spec
after any digest text edit in the same pass.

---

## Final gate (stage boundary)

**Edits:** none in this aggregate file; all code edits live in the task digests above.

**Gate:**

```bash
cd /Users/javier/bacayo-studio/territory-developer && npm run validate:all
```

**STOP:** run again after any edit to the two `ia/projects/TECH-667-*.md` or `ia/projects/TECH-668-*.md`
specs, the TECH-667 / TECH-668 backlog rows, or the Stage 28 table in
`ia/projects/web-platform-master-plan.md`.
