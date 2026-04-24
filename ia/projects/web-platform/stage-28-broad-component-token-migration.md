### Stage 28 — Visual design layer / Broad component token migration


**Status:** Done (closed 2026-04-22 — TECH-667, TECH-668 archived; `materialize-backlog` + `validate:all` green)

**Objectives:** Broad `tokens.*` → `ds-*` alias migration across shared components not covered by screen-port stages + wrap wiki/devlog MDX output in `<Prose>`. Alias-neutral (palette unchanged — NB4 / Example 3 from extensions doc). Landing + dashboard re-skin scope is **absorbed into Stage 8.6** (full-flow screen port) and no longer lives here.

**Exit:**

- `grep "tokens\."` surfaces enumerated; `Breadcrumb`, `Sidebar`, `BadgeChip`, `DataTable`, `FilterChips` migrated to `ds-*` CSS var classes; alias-neutral (zero visual diff — same hex values per Example 3).
- `web/app/wiki/**` + `web/app/devlog/**` MDX output wrapped in `<Prose>`; no layout rework.
- `npm run validate:web` green; manual visual diff on `localhost:4000/wiki` + `/devlog` noted in PR body.
- Phase 1 — Broad `tokens.*` → `ds-*` across nav + shared primitives; MDX `<Prose>` wrap (wiki + devlog). Cardinality: two tasks (T28.1 / T28.2) both under this single phase; two atomic commits.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T28.1 | **TECH-667** | Done | Grep `tokens\.` across `web/app/**/*.tsx` + `web/components/**/*.tsx`; enumerate surfaces; migrate `web/components/Breadcrumb.tsx` + `web/components/Sidebar.tsx` inline `tokens.*` → `bg-[var(--ds-*)]` / `text-[var(--ds-*)]` Tailwind v4 arbitrary value classes; confirm alias-neutral (zero visual diff — same hex values per Example 3); `npm run validate:web` green. |
| T28.2 | **TECH-668** | Done | Migrate `web/components/BadgeChip.tsx` + `web/components/DataTable.tsx` + `web/components/FilterChips.tsx` inline `tokens.*` → `ds-*` CSS var classes; wrap MDX output in `web/app/wiki/**` + `web/app/devlog/**` pages in `<Prose>` component (vertical rhythm only; no layout rework); `npm run validate:web` green; manual visual diff on `localhost:4000/wiki` + `/devlog` noted in PR body. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: |-
    Grep `tokens\.` across `web/app/**/*.tsx` + `web/components/**/*.tsx`; enumerate surfaces; migrate `web/components/Breadcrumb.tsx` + `web/components/Sidebar.tsx` inline `tokens.*` → `bg-[var(--ds-*)]` / `text-[var(--ds-*)]` Tailwind v4 arbitrary value classes; confirm alias-neutral (zero visual diff — same hex values per Example 3); `npm run validate:web` green.
  priority: medium
  notes: |
    Nav chrome token migration. web/components/Breadcrumb.tsx, web/components/Sidebar.tsx; grep inventory before edits; validate:web from repo root.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Stage 28 T28.1: Inventory `tokens.*` usage across app + components; migrate Breadcrumb and Sidebar to Tailwind v4 `bg-[var(--ds-*)]` / `text-[var(--ds-*)]` classes. Zero visual change — alias resolves to same hex (NB4).
    goals: |
      1. Published grep inventory attached or noted in PR.
      2. Breadcrumb + Sidebar have no inline `tokens.*` class strings where ds-* replacement exists in design-tokens.
      3. `npm run validate:web` exit 0.
    systems_map: |
      web/components/Breadcrumb.tsx, web/components/Sidebar.tsx, web/lib/design-tokens.ts, web/app/globals.css @theme `ds-*`.
    impl_plan_sketch: |
      ### Phase 1 — Enumerate

      - [ ] `rg "tokens\." web/app web/components` (or project grep) — save list for PR.
      ### Phase 2 — Migrate Breadcrumb + Sidebar

      - [ ] Replace per Intent; spot-check classnames against design-system / NB4 alias table.
      - [ ] `npm run validate:web`.
- reserved_id: ""
  title: |-
    Migrate `web/components/BadgeChip.tsx` + `web/components/DataTable.tsx` + `web/components/FilterChips.tsx` inline `tokens.*` → `ds-*` CSS var classes; wrap MDX output in `web/app/wiki/**` + `web/app/devlog/**` pages in `<Prose>` component (vertical rhythm only; no layout rework); `npm run validate:web` green; manual visual diff on `localhost:4000/wiki` + `/devlog` noted in PR body.
  priority: medium
  notes: |
    Primitives + MDX Prose. BadgeChip, DataTable, FilterChips; wiki/devlog page shells — import Prose; no grid/layout rewrites; validate:web; PR notes manual check wiki + devlog.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Stage 28 T28.2: Migrate BadgeChip, DataTable, FilterChips off `tokens.*` to `ds-*` arbitrary classes; wrap wiki and devlog MDX-driven pages in `<Prose>` for vertical rhythm. validate:web + manual pass on /wiki and /devlog.
    goals: |
      1. Three primitives carry ds-* token classes; no `tokens.` strings remain in those files.
      2. All wiki + devlog route pages wrap article body in `<Prose>`.
      3. `npm run validate:web` green; PR lists manual visual check.
    systems_map: |
      web/components/BadgeChip.tsx, DataTable.tsx, FilterChips.tsx, web/components/Prose.tsx (or path from design system), web/app/wiki/**, web/app/devlog/**.
    impl_plan_sketch: |
      ### Phase 1 — Primitives

      - [ ] Token migration per file; keep semantics (badge colors, table zebra, filter chip states).
      ### Phase 2 — Prose + validate

      - [ ] Add Prose wrapper to MDX page roots; run validate:web; note manual verification in PR.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived **TECH-667**, **TECH-668** to `ia/backlog-archive/`; removed temporary T28.1 + T28.2 project specs under `ia/projects/`; set Stage 28 table + Stage **Status** to **Done**; `materialize-backlog.sh` + `validate:all` + `validate:dead-project-specs`.

---
