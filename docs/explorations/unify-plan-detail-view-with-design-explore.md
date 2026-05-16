---
slug: unify-plan-detail-view-with-design-explore
status: seed
title: "Unify Plan Detail View with design-explore"
parent_plan_id: null
target_version: 1
priority: P1
parent_exploration: null
companion_explorations: []
arch_decisions_inherited:
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
notes: >-
  Authored from prior chat session d61114ce (2026-05-15).
  Side-by-side capability matrix + locked decisions captured below verbatim.
  Open questions = items left unresolved when prior session paused before /design-explore could run.
---

# Unify Plan Detail View with `design-explore` — Exploration Seed

**Status:** Seed (problem + capability matrix + locked decisions + open questions + approach forks). Ready for `/design-explore`.
**Gate:** `/dashboard/plan/[slug]` web view shipped (yes — 22 widgets across groups A–G live).
**Parent:** prior chat session `d61114ce-3553-48b0-9585-1d2473243431` (2026-05-15) produced the side-by-side analysis + locked decisions; never authored the seed doc.

---

## Problem Statement

Master-plan information lives in two parallel surfaces today:

1. **HTML render** — `design-explore` Phase 9 produces a standalone `.html` snapshot via `tools/scripts/render-design-explore-html.mjs` + `ia/templates/design-explore.html.template`. Source = MD frontmatter (frozen at render time). Strengths: visual goals grid, patterns observed callout, approach-compare tabs, decision matrix, reference library, per-task enrichment (mockup SVG, before/after diff, failure modes, glossary anchors, decision deps, path previews), per-stage enrichment (red-stage proof, edge cases, shared seams, checkpoint screenshots, iteration log, paste-ready handoff), Gantt roadmap, slide-deck mode (keyboard nav), TOC sidebar with scrollspy + search, export buttons (YAML / `/ship-plan` cmd / Stage MD), token sampler, theme toggle.
2. **Web RSC** — `/dashboard/plan/[slug]` (`web/app/dashboard/plan/[slug]/page.tsx`, 22 widgets across groups A–G). Source = live Postgres (`ia_master_plans` + `ia_stages` + `ia_tasks` + `ia_task_deps` + `ia_task_commits` + `ia_stage_verifications` + `ia_ship_stage_journal` + `ia_fix_plan_tuples` + `ia_task_spec_history` + `ia_master_plan_change_log`). Strengths: live status, %, last-touched, LCD tiles, ring + stack progress, time-series (velocity, burndown, cycle histogram, commit cadence), verifications table, spec churn chips, fix-plan rounds chips, change log + journal + commits tables, cross-plan deps, glossary hits, sibling-plan index, live `DepGraph`.

**Asymmetry:** HTML = authoring snapshot (rich enrichment, paste-ready handoffs, slide deck). Web = live ops (live %, verifications, time-series, cross-plan). Today they live in two places; users must context-switch.

**Goal:** one canonical surface (web `/dashboard/plan/[slug]`) backed by one source of truth (Postgres). `design-explore` Phase 9 writes the enriched payload into a JSONB sidecar on `ia_master_plans`; the web RSC pulls it; the standalone `.html` retires.

---

## Capability matrix (verbatim from prior session)

| Capability | HTML (design-explore) | Web (`/dashboard/plan/[slug]`) |
|---|---|---|
| Source | MD frontmatter snapshot | Live Postgres |
| Header / latest state | ✓ derived from first non-done task | ✓ live status, %, last touched, LCD tiles |
| Visual goals (screenshots + annotations) | ✓ | — |
| Patterns observed callout | ✓ | — |
| Approach-compare tabs | ✓ | — |
| Decision matrix grid | ✓ | — |
| Reference library cards | ✓ | — |
| Per-task enriched (mockup SVG, before/after diff, failure modes, glossary anchors, decision deps, path previews) | ✓ | — |
| Stage cards w/ red-stage proof, edge cases, shared seams | ✓ | partial (objective only) |
| Checkpoint screenshots + iteration log per stage | ✓ | — |
| Paste-ready handoff prompt per stage (copy btn) | ✓ | — |
| Export YAML / `/ship-plan` cmd / Stage MD | ✓ | — |
| Slide-deck mode (keyboard nav) | ✓ | — |
| TOC sidebar + scrollspy + search | ✓ | — |
| Gantt roadmap | ✓ task-count weighted | — |
| Dep graph | ✓ static SVG | ✓ live client component |
| Token sampler / DS dogfood | ✓ | implicit (DS-native everywhere) |
| Theme toggle | ✓ | — |
| Verifications table | — | ✓ |
| Velocity / burndown / cycle / commit-cadence charts | — | ✓ |
| Spec churn + fix-plan rounds | — | ✓ |
| Change log + journal + commits tables | — | ✓ |
| Cross-plan deps + other-plans index | — | ✓ |
| Glossary hits in body | — | ✓ |

---

## Locked Decisions (from prior session)

1. **Storage:** single `exploration_payload jsonb` column on `ia_master_plans`. Migration adds the column; payload schema is open.
2. **HTML retire:** single release. Web parity ships first; then renderer script + template + existing `.html` files retire in one cleanup stage.
3. **Routing:** inline on `/dashboard/plan/[slug]` — no separate `/dashboard/plan/[slug]/explore` route. Sections gated on payload presence (zero payload → web stays at today's behavior).
4. **Port:** slide-deck overlay (keyboard nav) · export buttons (YAML · `/ship-plan` · Stage MD) · TOC sidebar with scrollspy + search.
5. **Drop:** theme toggle (web is DS-native; light/dark handled at app level).

---

## Open Questions (to be grilled by `/design-explore`)

### Payload shape + write path

1. **JSONB schema versioning.** Embed `schema_version` field + drift-lint? Or rely on DB migration cadence (column-level `ALTER TABLE` per shape change)? Affects re-render strategy when `design-explore` evolves.
2. **Phase 9 write contract.** Does Phase 9 write `exploration_payload` synchronously alongside `master_plan_bundle_apply`? Or async via `cron_*_enqueue` to keep `design-explore` deterministic on DB hiccup? Affects skill SKILL.md Phase 9 mechanical steps.
3. **Stale payload detection.** Plan body edited in Postgres directly (not via `design-explore`) → payload now reflects stale snapshot. Detect via `body_md` hash stored inside payload? Surface a "stale snapshot" badge on the web?
4. **Per-task / per-stage payload nesting.** Single top-level JSON object keyed by stage id → task id? Or normalized into `ia_stages.exploration_payload` + `ia_tasks.exploration_payload` columns? Affects loader join + cache strategy.

### Web RSC composition

5. **Section gating granularity.** Gate per top-level group (`visual_goals` present → show grid) or per-card (`visual_goals.entries[0]` present → show one)? Affects empty-state handling.
6. **Slide-deck overlay UX.** Triggered from a header button? Keyboard shortcut (`?`)? Modal vs route? Affects component placement.
7. **TOC scrollspy threshold.** Element ratio for "active" section. Reuse existing dashboard scrollspy convention or pick new.
8. **Export button targets.** YAML (full plan body) vs `/ship-plan` cmd (one-line copy) vs Stage MD (per-stage). Which buttons live at plan-header level vs per-stage card? Clipboard API only or also "download as file"?
9. **Mermaid rendering inside `body_md`.** Web's `Markdown` renderer (`web/lib/markdown/render.tsx`) does not render mermaid today. Need a client-side mermaid wrapper (small adjacent task) — or strip-and-warn, or keep mermaid blocks rendered as code? Affects per-stage architecture block fidelity.

### Per-task enrichment

10. **Mockup SVG storage.** Inline SVG strings in JSONB (large payload risk) or external file paths (added file-resolution step on web)?
11. **Before/after diff source.** Pre-computed in payload (frozen) or computed live from `ia_task_spec_history` (current shape)? Affects whether the diff stays accurate after task edits.
12. **Failure modes + edge cases.** Free-text list per task, or structured `{cause, mitigation}`? Affects which UI component renders them.

### Per-stage enrichment

13. **Checkpoint screenshots strip.** Image source — `docs/explorations/assets/` paths copied into payload, or symbolic links via slug + filename convention? Affects build pipeline + commit footprint.
14. **Paste-ready handoff prompt.** Copy button triggers what — raw text from payload, or template-rendered with current task state (live)? Live = always fresh; raw = frozen at render.
15. **Iteration log table.** Source = payload only, or augmented by `ia_ship_stage_journal` rows already on web? Avoid duplicating the same surface twice.

### Gantt + dep graph

16. **Gantt roadmap.** Weight by task count (HTML default) or by `ia_task_commits` cycle-time (live)? Or pick at render time.
17. **Static HTML dep graph vs live web `DepGraph`.** Web already has live `DepGraph` (Group E). Drop the HTML version entirely, or keep a payload-side snapshot for "as designed" comparison vs "as built"?

### Migration + retirement

18. **Existing plans without payload.** Re-render via batch `design-explore --replay` on all closed plans, or only seed payload on next edit? Affects backfill labor.
19. **Renderer script retirement timing.** Delete `tools/scripts/render-design-explore-html.mjs` + `tools/scripts/extract-exploration-md.mjs` + `ia/templates/design-explore.html.template` + existing `.html` files in same stage as web parity ships, or hold a "deprecation lap" stage?
20. **Phase 9 emit URL.** `http://localhost:3001/dashboard/plan/{slug}` only? Or env-driven (`WEB_BASE_URL`) for CI / remote? Already-running dev server assumption.

### Validators

21. **`validate:all` impact.** Renderer-script removal breaks which validators? Need replacement check that `exploration_payload` is well-formed?
22. **Drift lint between body_md + payload.** If a stage exists in `body_md` but not in payload, lint or accept?

---

## Approaches

*To be developed during `/design-explore` Phase 1 (compare matrix).*

The locked decisions narrow the design space substantially. Likely fork shapes:

- **A — Single migration + inline gating (default starting point).** One DB migration adds `ia_master_plans.exploration_payload jsonb`. `design-explore` Phase 9 writes the payload synchronously alongside `master_plan_bundle_apply`. Web loader `web/lib/ia/plan-detail-data.ts` pulls payload into `loadPlanDetail` return shape. Per-group RSC sections in `web/app/dashboard/plan/[slug]/page.tsx` gated on payload key presence — zero payload → today's behavior unchanged. HTML retires in one stage at end.
- **B — Per-stage / per-task JSONB columns (normalized).** Three columns: `ia_master_plans.exploration_payload`, `ia_stages.exploration_payload`, `ia_tasks.exploration_payload`. Loader joins all three. Higher write complexity, finer gating granularity, smaller per-row payload size, easier per-stage edit-in-isolation.
- **C — JSONB sidecar + async Phase 9 write.** Same column as A but Phase 9 enqueues via `cron_*_enqueue` instead of synchronous write. Decouples `design-explore` durability from DB write success; needs a "payload pending" badge on web until cron drains.

`/design-explore` will rank these against constraint fit (DEC-A22 prototype-first, DEC-A23 TDD red→green), payload-rewrite cost when shape evolves, web loader complexity, and ship effort.

---

## Mechanical Stage Outline (from prior session — for reference)

Tracer-first (Stage 1 thin slice end-to-end; Stages 2+ widget-by-widget).

| Stage | Scope | Exit |
|---|---|---|
| **1. Tracer** | DB migration `ia_master_plans.exploration_payload jsonb`. `design-explore` Phase 9 writes payload alongside `master_plan_bundle_apply`. Web RSC pulls payload into `loadPlanDetail`. New `<ExplorationCard>` shows raw payload JSON in a `<details>` block on `/dashboard/plan/[slug]`. End-to-end signal: edit exploration → re-run skill → web reflects | Visit one slug, see JSON payload on web. Round-trip green. |
| **2. Snapshot widgets (read-only)** | Visual goals grid · patterns observed · approach-compare tabs · decision matrix · reference library. All gated on payload key presence. | `region-scene-prototype` shows all 5 widgets on web matching HTML. |
| **3. Per-stage enriched** | Red-stage proof block · edge cases · shared seams · checkpoint screenshots strip · iteration log table · paste-ready handoff prompt with copy button. | All 6 per-stage subsections render on web. |
| **4. Per-task enriched** | Visual mockup SVG · before/after diff · failure modes · glossary anchors · decision deps · path previews. | Per-task expansion within stage row. |
| **5. Navigation + ergonomics** | Gantt roadmap · TOC sidebar (scrollspy + search) · slide-deck overlay (keyboard nav) · export buttons (YAML · `/ship-plan` · Stage MD). | Keyboard ←→ steps slides; TOC highlights active section; clipboard copy verified. |
| **6. HTML retire** | Delete `tools/scripts/render-design-explore-html.mjs` · `tools/scripts/extract-exploration-md.mjs` · `ia/templates/design-explore.html.template` · existing `.html` files. Update `ia/skills/design-explore/SKILL.md` Phase 9 → web URL only. Update validators. | `validate:all` green. Phase 9 emits `http://localhost:3001/dashboard/plan/{slug}`. |

**Risk flagged:** mermaid + marked currently render inside HTML browser. Web has `Markdown` + glossary linker but NOT mermaid. Mermaid blocks in `body_md` need a client-side mermaid wrapper (small adjacent task — see Open Question 9).

---

## Notes

- Seed authored from prior chat session `d61114ce-3553-48b0-9585-1d2473243431` (2026-05-15). That session produced the capability matrix + locked decisions but paused before `/design-explore` ran.
- Stage outline above is provisional — `/design-explore` Phase 6 (implementation points) will replace with a phased checklist derived from selected approach.
- BACKLOG single-row alternative remains open: `/project-new "Unify master plan detail view: fold design-explore HTML widgets into /dashboard/plan/[slug] · retire standalone .html · single JSONB sidecar on ia_master_plans" FEAT high`. Use only if scope shrinks to one ticket after grill.

---
