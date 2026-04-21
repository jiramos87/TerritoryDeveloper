---
purpose: "TECH-411 — Claude Design pilot: web Step 8 reset + validation."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ""
task_key: ""
---
# TECH-411 — Claude Design pilot: web Step 8 reset + validation

> **Issue:** [TECH-411](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

<!--
  Filename: ia/projects/TECH-411-claude-design-pilot-web-step-8-reset.md
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

Timeboxed methodology pilot measuring Claude Design (CD) ROI against hand-authored Step 8 path. Web master plan Step 8 (Visual Design Layer) pre-implementation w/ Stage 8.1 filed Draft (TECH-375..378, zero code shipped) — ideal reset candidate. Run CD manually on existing tokens + primitives + spec + extensions doc §8; persist bundle; gap-analyze via `/design-explore --against`; re-decompose Step 8; re-file + ship Stage 8.1; retrospective w/ 3 go/no-go decisions.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Phase 1 reset complete — TECH-375..378 unfiled + archived; Stage 8.1 rows back to `_pending_`; `validate:dead-project-specs` green.
2. CD bundle produced + persisted under `docs/web-platform-post-mvp-extensions.md` §8 w/ URL + capture date + input manifest + delta summary.
3. Re-decomposed Step 8 stages cite CD bundle; Stage 8.1 Phase 1 swaps hand-authored §1–§6 authorship → CD bundle extraction + validation.
4. Stage 8.1 re-filed w/ fresh TECH ids; shipped via `/ship-stage` (all tasks Done).
5. Pilot retrospective in this spec body — 3 go/no-go decisions (broader CD adoption; game ui-polish Step 2 pilot; `/design-explore --visual` flag wiring).
6. Follow-up TECH issues filed for green-lit items — ids cross-referenced in Related block.

### 2.2 Non-Goals (Out of Scope)

1. NOT wholesale migration — existing design system (`palette.json` + 6 primitives + `/design` showcase) stays source of truth.
2. NOT sprite-gen — isometric pixel-art pipeline wrong fit for CD.
3. NOT game ui-polish pilot — separate issue later, gated on this outcome + ui-polish Step 1 ship.
4. NOT flag wiring (`/design-explore --visual`) — retrospective output, not pilot scope.
5. NOT `palette.json` mutation — B1 guard (locked per Step 8 Exit criteria).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run CD manually on web Step 8 inputs, receive handoff bundle | Bundle URL captured + persisted under extensions doc §8. |
| 2 | Developer | Know whether CD saves net time vs hand-authored spec path | Retrospective reports time/phase + net delta + handoff pain points. |
| 3 | Developer | Decide go/no-go on broader CD adoption + game pilot + flag wiring | 3 decisions emitted in retrospective w/ stated criteria. |

## 4. Current State

### 4.1 Domain behavior

Web Step 8 Stage 8.1 filed Draft (2026-04-17) w/ 4 TECH tasks (TECH-375 spec §1–§6, TECH-376 accent derivation, TECH-377 `design-tokens.ts`, TECH-378 `globals.css` `@theme`). Zero code shipped. Existing design system: `web/lib/tokens/palette.json` (locked palette, 2026-04 freeze) + 6 primitives (`Button`, `BadgeChip`, `StatBar`, `DataTable`, `FilterChips`, `HeatmapCell`) + `/design` showcase route. Claude Design launched 2026-04-17.

### 4.2 Systems map

| Surface | Role |
|---------|------|
| `ia/projects/web-platform-master-plan.md` | Step 8 orchestrator target of pilot |
| `docs/web-platform-post-mvp-extensions.md` §8 | Canonical Design Expansion source + CD bundle persist destination |
| `web/lib/tokens/palette.json` | Locked palette — CD input, B1 guard (no mutation) |
| `ia/specs/web-ui-design-system.md` | Normative spec — CD input |
| `web/components/{Button,BadgeChip,StatBar,DataTable,FilterChips,HeatmapCell}.tsx` | 6 primitives — CD input |
| `web/app/{/,/design,/dashboard}/page.tsx` | Baseline screenshots — CD input |
| `ia/backlog/TECH-375..378.yaml` + specs | Unfile targets (Phase 1) |
| `ia/projects/ui-polish-master-plan.md` | Step 2 ThemedPrimitive ring — follow-up pilot gated on this issue |

## 5. Proposed Design

### 5.1 Target behavior (product)

Pilot produces measurable go/no-go signal on CD adoption. Deliverables: CD bundle persisted + Step 8 re-decomposed + Stage 8.1 re-filed + shipped + retrospective w/ 3 decisions. No product regression (`/dashboard/releases/**` Step 7 untouched; `palette.json` untouched).

### 5.2 Architecture / implementation

Manual CD invocation (no `/design-explore --visual` flag wiring). Bundle = whatever CD emits (Figma / Canva URL + token deltas + motion vocab + primitive renders + re-skin mockups + a11y annotations). Gap analysis via existing `--against` mode on `/design-explore`. Re-decomposition = manual edit to `web-platform-master-plan.md` Stage 8.1 phases + 8.2–8.4 scope review. Re-file via `/stage-file`. Ship via `/ship-stage`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Pilot on web Step 8 (not game) | Pre-implementation, zero code shipped, ideal reset candidate; web domain simpler CD input surface | Game ui-polish Step 2 (blocked on ui-polish Step 1 ship); greenfield issue (loses reset signal) |
| 2026-04-18 | No `--visual` flag wiring this pilot | Flag shape depends on bundle shape stability across re-runs; retrospective output instead | Wire flag upfront (premature; couples to single CD run) |
| 2026-04-18 | Timebox abort at Phase 5 if fidelity insufficient | Avoid sunk-cost on unusable bundle; preserve rollback to hand-authored path | No timebox (risks indefinite drift) |
| 2026-04-18 | Usable fidelity threshold = ≥3/6 primitives render correctly + token delta ≤30% of palette entries + motion vocab covers ≥3/4 duration stops | Concrete + measurable at Phase 2 capture; avoids hand-wave at Phase 5 gate | Subjective "looks good" (unfalsifiable); higher bar (risks false-negative pilot abort) |
| 2026-04-18 | Bundle URL persisted under extensions §8 + mirrored in §10 retrospective only | Single source of truth until bundle count ≥3; no premature `ia/artifacts/` drop | Dedicated `ia/artifacts/cd-bundles/` dir (premature for N=1) |
| 2026-04-18 | Handoff gap bugs logged via `release-rollout-skill-bug-log` + mirrored §10 | Reuses existing skill-bug channel; §10 keeps pilot-local retrospective complete | Only §10 (loses aggregation); only skill-bug-log (loses pilot narrative) |

## 7. Implementation Plan

### Phase 1 — Prep + reset

- [x] Confirm master-plan status sync audit finished (separate agent, user-managed).
- [x] Confirm Stage 8.1 Status still Draft + zero implementation shipped.
- [x] **Baseline snapshot (for Phase 8 time-saved computation):** before archive, record in §10 the TECH-375..378 yaml Notes acceptance effort + BACKLOG row Notes word-count as hand-authored proxy.
- [x] Unfile TECH-375..378: archive via `ia/backlog-archive/` move + spec deletion under `ia/projects/` + BACKLOG materialize.
- [x] Revert Stage 8.1 task rows in `web-platform-master-plan.md` to `_pending_`.
- [x] Run `npm run validate:dead-project-specs` green.

### Phase 2 — Run Claude Design manually (no flag wiring)

- [x] Feed CD input manifest:
  - Tokens: `web/lib/tokens/palette.json` (locked).
  - Normative spec: `ia/specs/web-ui-design-system.md`.
  - Extensions: `docs/web-platform-post-mvp-extensions.md` §8.
  - Primitives: `web/components/{Button,BadgeChip,StatBar,DataTable,FilterChips,HeatmapCell}.tsx`.
  - Baselines: screenshots of `/`, `/design`, `/dashboard`, `/dashboard/releases`, `/dashboard/releases/:id/progress`.
- [x] Capture bundle artifacts (all required for Phase 5 gate):
  - Bundle URL + capture date — 2026-04-18, persisted under `web/design-refs/step-8-console/` + mirror `docs/cd-pilot-step8-export.html`.
  - Token delta vs `palette.json` — +1 raw (blue #4a7bc8 for Signal/info accent); 14% delta; PASS vs 30% threshold.
  - Motion vocab — 4/4 duration stops (`--dur-fast` 80ms, `--dur-base` 160ms, `--dur-slow` 280ms, `--dur-reveal` 480ms); `--ease-enter` + `--ease-exit` curves.
  - Primitive renders — Rack, Bezel, Screen, LED, TapeReel, VuStrip, TransportStrip + Button, StatusChip, IdChip, StatBar, FilterChip, HeatCell, Legend, DensityToggle.
  - Re-skin mockups — Landing + Dashboard + Releases + ReleaseDetail + Design showcase screens (5 routes).
  - A11y annotations — focus-ring amber 2px + 2px offset; contrast pairs documented in `HANDOFF.md`.
- [x] Evaluate fidelity against Decision Log threshold (≥3/6 primitives correct + token delta ≤30% + motion ≥3/4 stops) — 6/6 primitives + 14% delta + 4/4 motion = PASS.

### Phase 3 — Persist bundle reference

- [x] Append CD bundle subsection to `docs/web-platform-post-mvp-extensions.md` under `## Design Expansion — Section 8: Visual Design Layer` — bundle URL + capture date + input manifest + delta summary vs existing Implementation Points.

### Phase 4 — Re-explore via gap analysis

- [ ] Run `/design-explore docs/web-platform-post-mvp-extensions.md --against ia/projects/web-platform-master-plan.md`.
- [ ] Persist gap analysis under same §8 Design Expansion block.

### Phase 5 — Re-decompose Step 8

- [ ] Edit `web-platform-master-plan.md` Step 8 stages:
  - Stage 8.1 Phase 1 swap: hand-authored §1–§6 spec authorship → CD bundle extraction + validation.
  - Stage 8.1 Phase 2 unchanged (token pipeline derivation still applies).
  - Stages 8.2–8.4 review for scope reduction if CD bundle covers primitive mockups + re-skin decisions.
  - Update Step 8 Exit criteria to reference CD bundle.
- [ ] **Timebox gate:** abort pilot here if CD bundle fidelity < usable threshold (Decision Log: <3/6 primitives correct OR token delta >30% OR motion <3/4 stops); document reason in §10 Lessons Learned; leave Stage 8.1 rows `_pending_` for later hand-authored re-fill.

### Phase 6 — Re-file Stage 8.1

- [ ] Run `/stage-file ia/projects/web-platform-master-plan.md Stage 8.1` — materialize fresh TECH ids.

### Phase 7 — Ship Stage 8.1

- [ ] Run `/ship-stage ia/projects/web-platform-master-plan.md 8.1` — drives all Stage 8.1 tasks through `/author` → implement → verify-loop → Stage-scoped `/closeout` pair.

### Phase 8 — Measure

- [ ] Actual time per phase vs estimate.
- [ ] CD bundle fidelity (how close to shippable tokens/primitives).
- [ ] Handoff pain points (`/implement` reading CD bundle vs hand-authored spec).
- [ ] Any skill bugs logged via `release-rollout-skill-bug-log`.
- [ ] Net time saved (or lost) vs non-CD Stage 8.1 authoring path estimate.

### Phase 9 — Decide gate + retrospective

- [ ] Write pilot-retrospective section in §10 Lessons Learned.
- [ ] Emit 3 decisions:
  1. Go/no-go on broader CD adoption (criteria: ≥20% net time saved + acceptable fidelity).
  2. Go/no-go on game ui-polish Step 2 CD pilot (conditional on #1 + ui-polish Step 1 shipped).
  3. Wire `/design-explore --visual` flag? (conditional on #1 + bundle shape stable across Phase 2 + hypothetical future re-run).
- [ ] File follow-up TECH issues for any green-lit items; cross-reference ids in Related block.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Phase 1 reset integrity | Node | `npm run validate:dead-project-specs` | TECH-375..378 specs gone; BACKLOG archived; master-plan rows `_pending_` |
| CD bundle persisted | Manual | `docs/web-platform-post-mvp-extensions.md` §8 diff | URL + capture date + input manifest + delta summary present |
| Re-decomposed Step 8 cites bundle | Manual | `web-platform-master-plan.md` diff | Stage 8.1 Phase 1 mentions CD bundle; Step 8 Exit criteria references bundle |
| Stage 8.1 re-filed w/ fresh ids | Node | `npm run validate:all` + materialized BACKLOG | `/stage-file` output shows fresh TECH ids; BACKLOG rows present |
| Stage 8.1 shipped | Agent report | `/ship-stage` chain digest | All Stage 8.1 tasks Done; batched Path B green at stage end |
| Retrospective + 3 decisions | Manual | this spec §10 Lessons Learned | 3 decisions explicit w/ stated criteria (broader CD adoption; game ui-polish Step 2 pilot; `/design-explore --visual` flag wiring) |
| Fidelity gate evaluation | Manual | Phase 2 capture artifacts vs Decision Log threshold | ≥3/6 primitives correct + token delta ≤30% + motion ≥3/4 stops; fail → Phase 5 abort |
| Follow-up TECH issues cross-referenced | Manual | Related block of this spec | Any green-lit decisions from §10 have filed TECH id in Related |

## 8. Acceptance Criteria

- [ ] Phase 1 reset green (`validate:dead-project-specs` pass; TECH-375..378 archived; Stage 8.1 rows back to `_pending_`).
- [ ] CD bundle URL + summary persisted in `docs/web-platform-post-mvp-extensions.md` §8.
- [ ] Re-decomposed Step 8 in `web-platform-master-plan.md` cites CD bundle.
- [ ] Stage 8.1 re-filed w/ fresh TECH ids (Draft).
- [ ] Stage 8.1 shipped via `/ship-stage` (all tasks Done).
- [ ] Pilot retrospective section present in §10 w/ 3 go/no-go decisions.
- [ ] Follow-up TECH issues filed (if any green-lit) — ids cross-referenced in Related block of this spec.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

<!-- Pilot retrospective lives here (Phase 9). On completion: migrate findings to AGENTS.md / canonical docs / skill bodies as applicable. -->

### Phase 1 Baseline Snapshot (hand-authored proxy for "≥20% time saved" computation)

Captured 2026-04-18 before TECH-375..378 archive move.

| Issue | yaml `notes` (words) | `acceptance` (words) | raw_markdown Notes (words) |
|-------|---------------------|---------------------|---------------------------|
| TECH-375 | ~62 | ~20 | ~32 |
| TECH-376 | ~42 | ~15 | ~22 |
| TECH-377 | ~72 | ~23 | ~30 |
| TECH-378 | ~62 | ~20 | ~28 |
| **Total** | **~238** | **~78** | **~112** |

Hand-authored proxy total: ~428 words across yaml notes + acceptance + raw_markdown Notes fields for 4 tasks. Acceptance effort estimate: 4 tasks × ~2–4 hrs each = ~8–16 hrs authoring window (rough; no time-tracking data). Revise at Phase 8 with actual CD bundle extraction time.

- *(pilot retrospective pending — Phase 9)*

## Open Questions (resolve before / during implementation)

Resolved at kickoff (moved to Decision Log):
- ~~Usable fidelity threshold~~ → Decision Log 2026-04-18: ≥3/6 primitives + token delta ≤30% + motion ≥3/4 stops.
- ~~Bundle URL location~~ → Decision Log 2026-04-18: extensions §8 + §10 mirror.
- ~~Handoff gap log channel~~ → Decision Log 2026-04-18: `release-rollout-skill-bug-log` + §10 mirror.

Deferred (resolve during Phase):
1. **Baseline for "≥20% net time saved" computation** — raw estimate source TBD. Phase 1 proposal: snapshot TECH-375..378 yaml Notes acceptance effort + BACKLOG row word-count as hand-authored proxy BEFORE archive move; record snapshot in §10 before Phase 2 starts. Revise at Phase 8 if proxy inadequate.
