### Stage 27 — Visual design layer / Full-flow screen port + port harness


**Status:** Final

**Objectives:** Port the full CD bundle screen flow per D4 lock (2026-04-18) — **all 4 production routes + 1 dev-only**: `ScreenLanding` → `/`, `ScreenDashboard` → `/dashboard`, `ScreenReleases` → `/dashboard/releases`, `ScreenDetail` → `/dashboard/releases/[releaseId]/progress`, `ScreenDesign` augmentation → `/design-system`. Stage 7.2 server-side fetcher contracts (`loadAllPlans`, `getReleasePlans`, `computePlanMetrics`, `buildPlanTree`, `deriveDefaultExpandedStepId`, `resolveRelease`) MUST be preserved — ports are presentation-layer only. Author the port harness as reusable `.jsx` → `.tsx` conversion notes + a localStorage-usage audit script so future CD bundle iterations can re-run the codemod mechanically. Per-screen schema diff gate (NB-CD2) enforces CD fixture shape vs loader output match before merge.

**Exit:**

- `tools/scripts/audit-localstorage.ts`: scans `web/design-refs/step-8-console/src/*.jsx` for `localStorage.` references + `useState`-backed routing; emits per-file audit report Markdown; runs pre-port as gate.
- `web/app/page.tsx`: reskinned via CD `ScreenLanding` JSX; full-English user-facing copy unchanged (B3 / CLAUDE.md §6); hero wrapped in `<Rack>` + `<Bezel>` + `<Heading level="display">`.
- `web/app/dashboard/page.tsx`: reskinned via CD `ScreenDashboard` JSX; summary bezels + heatmap + filters + step-tree per CD layout; existing `PlanChart` + `FilterChips` + `DataTable` contracts preserved.
- `web/app/dashboard/releases/page.tsx`: reskinned via CD `ScreenReleases` JSX; existing server-side `resolveRelease`/registry calls preserved verbatim; full-English user-facing labels unchanged.
- `web/app/dashboard/releases/[releaseId]/progress/page.tsx`: reskinned via CD `ScreenDetail` JSX; `loadAllPlans` + `getReleasePlans` + `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId` preserved; `<PlanTree>` Client island (TECH-352) contract unchanged.
- `web/app/(dev)/design-system/page.tsx` port augmentation: absorb CD `ScreenDesign` demo content NOT duplicated by Stage 8.2 T23.4 + Stage 8.4 T25.6 (NB-CD4 de-dupe); NODE_ENV guard + noindex preserved.
- Per-screen schema diff docs in PR body: CD `data.js` fixture shape vs loader output shape matched (NB-CD2).
- Lighthouse pre-port capture on all 4 production routes; post-port LCP ≤ baseline × 1.1, CLS < 0.1.
- `npm run validate:web` green.
- Phase 1 — Port harness audit + landing + dashboard ports.
- Phase 2 — Releases + Detail + design-system augmentation + Lighthouse gate.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T27.1 | **TECH-655** | Done | Author `tools/scripts/audit-localstorage.ts` — scans `web/design-refs/step-8-console/src/*.jsx` for `localStorage.` references + `useState`-backed pseudo-routing; emits `web/design-refs/step-8-console/.localstorage-audit.md` Markdown report (file + line + match context); tsx-runnable via `npx tsx`; JSDoc cites B-CD2 (localStorage conversion guard). Document in `web/lib/design-system.md` §7 appendix the port harness mechanics: `.jsx` → `.tsx` prop typing checklist, `localStorage.getItem` → `useEffect` + client island swap, `data.js` fixture → loader swap per D7. |
| T27.2 | **TECH-656** | Done | Port CD `ScreenLanding` → `web/app/page.tsx`; wrap hero in `<Rack>` + `<Bezel>` (Stage 8.4 T25.1/T25.2); `<Heading level="display">` on main title; `bg-[var(--ds-accent-terrain)]` on CTA button; full-English user-facing copy unchanged (CLAUDE.md §6 / B3); `npm run validate:web` green. |
| T27.3 | **TECH-657** | Done | Port CD `ScreenDashboard` → `web/app/dashboard/page.tsx`; summary bezels + heatmap + filters + step-tree per CD layout; wrap stat blocks in `<Surface tone="raised">` or `<Bezel>` per CD spec; replace raw `<h1>`/`<h2>` with `<Heading>`; preserve existing `PlanChart` + `FilterChips` + `DataTable` contracts; verify `/dashboard/releases/**` (Stage 7.2) still renders correctly; `npm run validate:web` green. |
| T27.4 | **TECH-658** | Done | Port CD `web/design-refs/step-8-console/src/console-screens.jsx` `ScreenReleases` → `web/app/dashboard/releases/page.tsx`; preserve Stage 7.2 server-side `resolveRelease` + registry calls; wrap in `<Rack>` + `<Bezel>` console chrome from Stage 8.4; replace CD `data.js` rollup call with existing registry read; per-screen schema diff documented in PR body; full-English user-facing labels unchanged (CLAUDE.md §6 / B3); `npm run validate:web` green. |
| T27.5 | **TECH-659** | Done | Port CD `ScreenDetail` → `web/app/dashboard/releases/[releaseId]/progress/page.tsx`; preserve Stage 7.2 `loadAllPlans` + `getReleasePlans` + `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId` flow verbatim; wrap in `<Rack>` + `<Bezel>`; `<PlanTree>` (TECH-352) Client island contract unchanged; reserved comment for `/rollout` sibling preserved (B1 guard); per-screen schema diff noted in PR body. |
| T27.6 | **TECH-660** | Done | Port CD `ScreenDesign` content augmentation into `web/app/(dev)/design-system/page.tsx` — absorb CD demo sections NOT already covered by Stage 8.2 T23.4 + Stage 8.4 T25.6 (color swatches matrix, motion stops demo, chrome wrap demo); de-duplicate against existing showcase content (NB-CD4); NODE_ENV guard + noindex preserved; unlinked from `Sidebar.tsx` (NB2). |
| T27.7 | **TECH-661** | Done | Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production routes (`localhost:4000/`, `/dashboard`, `/dashboard/releases`, `/dashboard/releases/full-game-mvp/progress`) BEFORE Phase 1 ports land (coordinate timing); after port, re-run Lighthouse; compare post-port scores against baseline (cap: LCP ≤ baseline × 1.1, CLS < 0.1); if regressed → flag in PR body + consider Surface motion downgrade on those routes; document schema diff (CD fixture shape vs loader output) per screen in PR body (NB-CD2); `npm run validate:web` green. |

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> **Applied 2026-04-22 (ship-stage-main-session):** archived **TECH-655**…**TECH-661** to `ia/backlog-archive/`; removed temporary `ia/projects/TECH-655`…`TECH-661` specs; set Stage 27 table + Stage **Status** to **Final**; `materialize-backlog.sh` + `validate:all`.

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: |-
    Author `tools/scripts/audit-localstorage.ts` — scans `web/design-refs/step-8-console/src/*.jsx` for `localStorage.` references + `useState`-backed pseudo-routing; emits `web/design-refs/step-8-console/.localstorage-audit.md` Markdown report (file + line + match context); tsx-runnable via `npx tsx`; JSDoc cites B-CD2 (localStorage conversion guard). Document in `web/lib/design-system.md` §7 appendix the port harness mechanics: `.jsx` → `.tsx` prop typing checklist, `localStorage.getItem` → `useEffect` + client island swap, `data.js` fixture → loader swap per D7.
  priority: medium
  notes: |-
    Audit script + design-system §7 port harness docs. tools/scripts + design-refs + web/lib.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.1: Author `tools/scripts/audit-localstorage.ts` — scans `web/design-refs/step-8-console/src/*.jsx` for `localStorage.` refe…
    goals: |-
      1. Meet master-plan Intent for T27.1.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.1.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Port CD `ScreenLanding` → `web/app/page.tsx`; wrap hero in `<Rack>` + `<Bezel>` (Stage 8.4 T25.1/T25.2); `<Heading level="display">` on main title; `bg-[var(--ds-accent-terrain)]` on CTA button; full-English user-facing copy unchanged (CLAUDE.md §6 / B3); `npm run validate:web` green.
  priority: medium
  notes: |-
    Landing reskin from CD ScreenLanding. web/app/page.tsx + Rack/Bezel/Heading.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.2: Port CD `ScreenLanding` → `web/app/page.tsx`; wrap hero in `<Rack>` + `<Bezel>` (Stage 8.4 T25.1/T25.2); `<Heading level…
    goals: |-
      1. Meet master-plan Intent for T27.2.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.2.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Port CD `ScreenDashboard` → `web/app/dashboard/page.tsx`; summary bezels + heatmap + filters + step-tree per CD layout; wrap stat blocks in `<Surface tone="raised">` or `<Bezel>` per CD spec; replace raw `<h1>`/`<h2>` with `<Heading>`; preserve existing `PlanChart` + `FilterChips` + `DataTable` contracts; verify `/dashboard/releases/**` (Stage 7.2) still renders correctly; `npm run validate:web` green.
  priority: medium
  notes: |-
    Dashboard reskin from CD ScreenDashboard. Preserve PlanChart, FilterChips, DataTable.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.3: Port CD `ScreenDashboard` → `web/app/dashboard/page.tsx`; summary bezels + heatmap + filters + step-tree per CD layout; …
    goals: |-
      1. Meet master-plan Intent for T27.3.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.3.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Port CD `web/design-refs/step-8-console/src/console-screens.jsx` `ScreenReleases` → `web/app/dashboard/releases/page.tsx`; preserve Stage 7.2 server-side `resolveRelease` + registry calls; wrap in `<Rack>` + `<Bezel>` console chrome from Stage 8.4; replace CD `data.js` rollup call with existing registry read; per-screen schema diff documented in PR body; full-English user-facing labels unchanged (CLAUDE.md §6 / B3); `npm run validate:web` green.
  priority: medium
  notes: |-
    Releases list port. Keep resolveRelease + registry; Rack/Bezel chrome.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.4: Port CD `web/design-refs/step-8-console/src/console-screens.jsx` `ScreenReleases` → `web/app/dashboard/releases/page.tsx…
    goals: |-
      1. Meet master-plan Intent for T27.4.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.4.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Port CD `ScreenDetail` → `web/app/dashboard/releases/[releaseId]/progress/page.tsx`; preserve Stage 7.2 `loadAllPlans` + `getReleasePlans` + `computePlanMetrics` + `buildPlanTree` + `deriveDefaultExpandedStepId` flow verbatim; wrap in `<Rack>` + `<Bezel>`; `<PlanTree>` (TECH-352) Client island contract unchanged; reserved comment for `/rollout` sibling preserved (B1 guard); per-screen schema diff noted in PR body.
  priority: medium
  notes: |-
    Release detail / progress port. Preserve loadAllPlans pipeline + PlanTree TECH-352 contract.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.5: Port CD `ScreenDetail` → `web/app/dashboard/releases/[releaseId]/progress/page.tsx`; preserve Stage 7.2 `loadAllPlans` +…
    goals: |-
      1. Meet master-plan Intent for T27.5.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.5.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Port CD `ScreenDesign` content augmentation into `web/app/(dev)/design-system/page.tsx` — absorb CD demo sections NOT already covered by Stage 8.2 T23.4 + Stage 8.4 T25.6 (color swatches matrix, motion stops demo, chrome wrap demo); de-duplicate against existing showcase content (NB-CD4); NODE_ENV guard + noindex preserved; unlinked from `Sidebar.tsx` (NB2).
  priority: medium
  notes: |-
    Design-system page augmentation from CD ScreenDesign. De-dupe NB-CD4; NODE_ENV guard.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.6: Port CD `ScreenDesign` content augmentation into `web/app/(dev)/design-system/page.tsx` — absorb CD demo sections NOT al…
    goals: |-
      1. Meet master-plan Intent for T27.6.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.6.
      - [ ] validate:web.
- reserved_id: ""
  title: |-
    Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production routes (`localhost:4000/`, `/dashboard`, `/dashboard/releases`, `/dashboard/releases/full-game-mvp/progress`) BEFORE Phase 1 ports land (coordinate timing); after port, re-run Lighthouse; compare post-port scores against baseline (cap: LCP ≤ baseline × 1.1, CLS < 0.1); if regressed → flag in PR body + consider Surface motion downgrade on those routes; document schema diff (CD fixture shape vs loader output) per screen in PR body (NB-CD2); `npm run validate:web` green.
  priority: medium
  notes: |-
    Lighthouse baseline + post-port compare on 4 routes. NB-CD2 schema diff in PR.
  depends_on: []
  related: []
  stub_body:
    summary: |-
      Stage 27 T27.7: Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production routes (`localhost:4000/`, `/dashboard`, `/dashboard/r…
    goals: |-
      1. Meet master-plan Intent for T27.7.
      2. npm run validate:web green.
      3. Preserve locked D4/D7 server contracts where applicable.
    systems_map: |-
      web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.
    impl_plan_sketch: |-
      ### Phase 1 — Implement
      
      - [ ] Execute per master-plan Intent T27.7.
      - [ ] validate:web.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

---
