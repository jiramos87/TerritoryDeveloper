### Stage 29 — Visual design layer / Docs + validation


**Status:** Done — Stage 29 (shipped 2026-04-22)

**Objectives:** Update `web/README.md` (Design System section) + `CLAUDE.md §6` (spec path row); final `validate:web` green gate; post-port Lighthouse check against Stage 8.6 baseline (NB3 regression guard).

**Exit:**

- `web/README.md` has `## Design System` section: spec path, primitive one-liners, showcase route note, `ds-*` class convention (Tailwind v4 CSS custom properties, not `tailwind.config.ts`).
- `CLAUDE.md §6` has row for `web/lib/design-system.md`.
- `npm run validate:web` green.
- Lighthouse post-check on `/`: LCP ≤ Stage 8.6 T27.7 baseline × 1.1; CLS < 0.1; if CLS regressed → set all `Surface motion="none"` in landing + dashboard as fallback.
- Phase 1 — Docs (`web/README.md` + `CLAUDE.md §6`).
- Phase 2 — Final validation (`validate:web` + Lighthouse post-check).

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T29.1 | **TECH-680** | Done | Update `web/README.md` — add `## Design System` section: cite `web/lib/design-system.md` as authoritative spec; one-liner per primitive (`Heading` — level-bound RSC typography; `Prose` — MDX vertical-rhythm wrapper; `Surface` — tone/padding/motion panel); showcase route (`web/app/(dev)/design-system/page.tsx`, dev-only, unlinked); `ds-*` class convention note (Tailwind v4 CSS vars via `--ds-*` in `globals.css`, not `tailwind.config.ts`). |
| T29.2 | **TECH-681** | Done | Update `CLAUDE.md §6` web workspace section — add row for design-system spec: `web/lib/design-system.md — Design system spec: type/spacing/motion/alias tables; derivation source for web/lib/design-tokens.ts + globals.css @theme ds-* block`; add caveman carve-out reminder: page-body JSX strings in `web/app/**/page.tsx` stay full English (CLAUDE.md §6 authority). |
| T29.3 | **TECH-682** | Done | Run `npm run validate:web` (lint + typecheck + build) from repo root; fix any type or lint regressions introduced in Stages 8.1–8.7; confirm exit 0; report exit code + any fixes in PR body. |
| T29.4 | **TECH-683** | Done | Run Lighthouse on `localhost:4000` (landing); record LCP / CLS / TBT; compare against Stage 8.6 T27.7 baseline (cap: LCP ≤ baseline × 1.1, CLS < 0.1); if CLS regressed → set `Surface motion="none"` in landing + dashboard and re-run Lighthouse; document result + any remediation in PR body (NB3). |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-680
  title: "web/README — Design System section (ds-* + primitives + dev showcase path)"
  priority: medium
  issue_type: TECH
  notes: |
    Doc-only. Add `## Design System` to web/README: cite `web/lib/design-system.md`, one line each for Heading, Prose, Surface;
    note `web/app/(dev)/design-system/page.tsx` dev showcase; `ds-*` + globals.css (Tailwind v4), not `tailwind.config.ts`.
  depends_on: []
  related: [TECH-681, TECH-682, TECH-683]
  stub_body:
    summary: |
      Authoritative README section so contributors find design spec + primitive roles + `ds-*` contract before editing `web/`.
    goals: |
      1. `web/README.md` contains `## Design System` with spec path + primitive one-liners + showcase route + `ds-*` note.
      2. Wording references `web/lib/design-system.md` and `web/app/(dev)/design-system/page.tsx` accurately.
    systems_map: |
      web/README.md, web/lib/design-system.md, web/app/(dev)/design-system/page.tsx, web/app/globals.css
    impl_plan_sketch: |
      ### Phase 1 — README

      - [ ] Add section: bullets for spec file, primitives, dev-only design-system page, `ds-*` + `@theme` in globals.
- reserved_id: TECH-681
  title: "CLAUDE.md — §6 row for `web/lib/design-system.md` + page-body English carve-out"
  priority: medium
  issue_type: TECH
  notes: |
    IA line in root CLAUDE.md table: path `web/lib/design-system.md` + design-tokens + globals `ds-*` block;
    restate that `web/app/**/page.tsx` user-facing copy stays full English (caveman exception by path).
  depends_on: []
  related: [TECH-680, TECH-682, TECH-683]
  stub_body:
    summary: |
      Align dev harness doc with same spec pointer as README so agents land on one authority for tokens + type scale.
    goals: |
      1. `CLAUDE.md` §6 (or web workspace list) includes a row for `web/lib/design-system.md` with role + `design-tokens.ts` + `globals.css` `ds-*` link.
      2. Carve-out line for `web/app/**/page.tsx` body strings (full English) present per agent-output-caveman exception.
    systems_map: |
      CLAUDE.md, web/lib/design-system.md, web/lib/design-tokens.ts, web/app/globals.css
    impl_plan_sketch: |
      ### Phase 1 — CLAUDE row

      - [ ] Insert spec row + short carve-out blurb; keep existing §6 table shape.
- reserved_id: TECH-682
  title: "validate:web green — fix regressions from Stages 8.1–8.7"
  priority: medium
  issue_type: TECH
  notes: |
    Run `npm run validate:web` at repo root after doc tasks; fix lint/type/build breaks introduced by design-layer stages.
  depends_on: []
  related: [TECH-680, TECH-681, TECH-683]
  stub_body:
    summary: |
      Full web pipeline (lint, tsc, `next build`) must exit 0 before Lighthouse gate.
    goals: |
      1. `npm run validate:web` returns 0; any new errors from prior stages are fixed in-repo.
      2. PR body notes command + exit + files touched.
    systems_map: |
      web/ (as surfaced by validate:web), root package.json scripts
    impl_plan_sketch: |
      ### Phase 1 — validate

      - [ ] `npm run validate:web`; resolve failures; record summary for PR.
- reserved_id: TECH-683
  title: "Lighthouse landing vs Stage 8.6 T27.7 baseline; optional Surface motion=\"none\" remediation"
  priority: medium
  issue_type: TECH
  notes: |
    Local `localhost:4000` Lighthouse on `/`; cap LCP vs baseline×1.1, CLS<0.1; if CLS bad, set Surface motion=none on landing+dashboard
    and re-run; document in PR (NB3).
  depends_on: [TECH-682]
  related: [TECH-680, TECH-681, TECH-682]
  stub_body:
    summary: |
      NB3 post-check: confirm perf budget after visual layer; remediate motion-driven CLS on marketing surfaces if required.
    goals: |
      1. Lighthouse metrics recorded for landing; compared to T27.7 baseline with stated caps.
      2. If CLS over threshold, apply `motion="none"` to `Surface` usages on landing + dashboard routes and re-measure.
      3. PR documents numbers + any code change.
    systems_map: |
      web (localhost dev server), `web/components/landing/`, `web/app/dashboard/`, `web/components/surface/`
    impl_plan_sketch: |
      ### Phase 1 — Measure

      - [ ] `npm run dev` (or agreed port 4000); Lighthouse; log LCP/CLS/TBT.
      ### Phase 2 — Remediate (if needed)

      - [ ] If CLS regression: set `Surface` motion to `none` on landing + dashboard; re-run Lighthouse.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived **TECH-680**, **TECH-681**, **TECH-682**, **TECH-683** to `ia/backlog-archive/`; removed temporary T29.1–T29.4 project specs under `ia/projects/`; set Stage 29 table + Stage **Status** to **Done**; `tools/validate-parent-plan-locator.mjs` ESM import fix for `validate:all`; `materialize-backlog.sh` + `validate:all` + `validate:dead-project-specs`.

---
