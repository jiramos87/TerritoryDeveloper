# Parallel Carcass Rollout — Post-MVP Extensions

> **Created:** 2026-04-30
>
> **Status:** Wave 1 dogfood complete (8/8 stages done); Wave 2 pilot selection open.
>
> **Source plan:** `parallel-carcass-rollout` (DB slug). Preamble flagged this stub as the landing pad for items routed out of Wave 1 scope.
>
> **Related docs:** `docs/parallel-carcass-exploration.md` · `docs/parallel-carcass-rollout-skill-iteration.md` · `docs/parallel-carcass-claims-sweep-ops.md` · `docs/parallel-carcass-migration-cookbook.md` · `docs/parallel-carcass-rollout-carcass3-evidence.md` · `docs/design-explore-carcass-alignment-gap-analysis.md`.

---

## 1. Wave 1 dogfood — confirmed manual fixes during stage chain

Manual fixes landed mid-flight against carcass primitives + agents-as-recipe runner while Wave 1 stages 1.x–2.x were shipping. Recorded here so future passes can audit what was hand-stitched vs recipe-driven.

| Date | Commit | Surface | Fix |
|------|--------|---------|-----|
| 2026-04-29 | `2cc3119f` | Wave 0 carcass | V2 row-only refactor — drop `session_id` from `ia_section_claims` + `ia_stage_claims`; any agent may renew open claim. |
| 2026-04-29 | `6fa39b38` | skill-tools | In-flight skill + generated surface drift commit (subagent/command regeneration). |
| 2026-04-29 | `b3bd4d8e` | stage-authoring | Enforce literal `§` marker on Plan Digest section heading. |
| 2026-04-29 | `f53315e8` | db-lifecycle-extensions Stage 3 | Author-time quality gates (cross-cutting fix that fed back into stage-authoring). |
| 2026-04-28 | `c016ef51` | recipe-engine | DEC-A19 Phase D close-out — MCP regression smoke + selftest harness. |
| 2026-04-27 | `bd21d916` | recipe-runner | `stage-decompose` recipify (DEC-A19 Phase D). |
| 2026-04-26 | `4c845a19` | agent-recipe-runner | DEC-A19 design-explore lock + `arch_drift_scan` SQL fix. |

**Net:** Wave 1 was *partially recipe-driven, partially hand-stitched*. The carcass primitives (Wave 0 PRs 3.1–3.5) shipped clean, but the recipe engine + skill renderers needed iterative fixes once the dogfood load arrived. Skill-train pass (Stage 2.5) returned `friction_count: 0` at threshold 2 — no recurring friction, but structural gap noted: no `source: self-report` emitter stanzas wired in any of the 5 carcass-adjacent skills.

---

## 2. Deferred items — landing pad

Captured from Wave 1 preamble + skill-iteration doc.

### 2.1 `arch_decision_write` `plan_slug` Phase B/C wiring

Wave 1 deferred: `arch_decision_write` MCP exposes `plan_slug` (Phase A landed in Stage 2.4 migration cookbook), but Phase B (write-time supersession seal) + Phase C (cross-plan lock trigger) remain stubbed. Global DEC-A18 + DEC-A19 cover the architecture; per-plan seeding still hand-wired.

**Trigger to land:** first Wave 2+ plan that needs plan-scoped architecture decisions. Author as standalone TECH issue or fold into Wave 2 pilot's carcass.

### 2.2 `/architecture-supersede` skill

Exploration §6.6 tradeoff table (line 536) called out post-MVP supersede skill to wrap the lock-trigger workflow. Currently supersession is manual via `arch_decision_write` with explicit `superseded_by` field.

**Trigger:** when 2nd architecture pivot lands and manual supersede flow proves brittle.

### 2.3 `ia_plan_section_health` MV split fallback

Exploration §6.6 (line 537) — D10 fallback. If `ia_master_plan_health` MV refresh exceeds 200ms with new derived columns, split into `ia_plan_section_health` refreshed only on `ia_*_claims` UPDATE. Not yet measured under load; bench Stage 2.1 captured drift-scan P95 — sibling MV bench still pending.

**Trigger:** dashboard tile or `master_plan_health` MCP call latency regresses post-Wave 2.

### 2.4 `source: self-report` emitter stanzas

Skill-iteration §Top-3 Frictions row #2 — structural gap. Five carcass-adjacent skills (`master-plan-new`, `stage-decompose`, `ship-stage`, `section-claim`, `section-closeout`) lack Phase-N tail emitter stanzas. Future `/skill-train` runs return 0 signals until wired.

**Priority:** Medium (deferred to Wave 2 per skill-iteration doc).

### 2.5 Architecture-pivot emitter

Skill-iteration §Proposed Fixes row #2 — when V2/V3 protocol rewrites land, emit `architecture_pivot` `friction_type`. The V2 row-only drop on 2026-04-29 hit 3 skills same day; current emitter has no firing condition for that signal class.

**Priority:** Low.

### 2.6 Section primitive native exercise

Wave 1 plan itself never tagged its stages with `section_id` — `master_plan_sections(parallel-carcass-rollout)` returns empty `carcass_stages` + `sections` arrays. The "5 sections" of Wave 1 (drift hardening, dashboard, cron sweep, cookbook, skill train) are stored as flat Stage 2.x. **First plan to actually wire `section_id` end-to-end = Wave 2 pilot.** This is the core validation of D10 + D19 + D20.

---

## 3. Wave 2 pilot — selection

**Decision (2026-04-30, user):** Wave 2 pilot will be the **next fresh design exploration** authored after this date — NOT one of the existing exploration docs under `docs/`. Existing exploration docs are not retro-fitted into the carcass+section shape; they continue under their current plan structure (or remain unstarted).

Rationale: a fresh exploration enters `/design-explore` → `/master-plan-new` natively under the carcass+section primitive. No retro-fit drift. Cleanest signal for the dogfood validation goals.

**Status:** Paused. Wave 2 kickoff resumes when the next exploration topic is chosen + authored.

Validation goals (per exploration §Wave 2 line 524–526) carry forward unchanged:

- End-to-end signal ≤ 1 stage cycle from plan birth (carcass close).
- Section parallelism reduces total wall-clock vs legacy linear baseline.
- `section_id` populated on every Stage row; `ia_section_claims` actually opens + closes.

### 3.1 Candidate matrix (archived — not selected)

The matrix below was the original Wave 2 candidate shortlist drawn from existing exploration docs. Retained for reference; superseded by the fresh-exploration decision above.

| Candidate | Exploration doc | Carcass shape (est) | Section count (est) | Parallelism fit | Notes |
|-----------|-----------------|---------------------|---------------------|-----------------|-------|
| **lifecycle-opus-planner-sonnet-executor** | `docs/lifecycle-opus-planner-sonnet-executor-exploration.md` | 3 (planner + executor + handoff) | 4–5 (per skill family) | High — orthogonal skill surfaces | Direct continuity with Wave 0/1 lifecycle work. |
| **cost-catalog** | `docs/cost-catalog-exploration.md` | 3 (data model + lookup + UI) | 3 (catalog data · MCP · web) | High — clean DB/MCP/UI split | Bridges asset-pipeline + zone-s-economy. |
| **actionable-agent-dashboard** | `docs/actionable-agent-dashboard-exploration.md` | 3 (action queue + UI + handler) | 3–4 (queue · UI · handler · auth) | Medium — queue + UI couple | Builds on dashboard-prod-database. |
| **ui-data-dashboard** | `docs/ui-data-dashboard-exploration.md` | 2–3 (data model + UI) | 2–3 | Medium | Tighter scope; less section variety. |
| **web-dashboard-lifecycle-controls** | `docs/web-dashboard-lifecycle-controls-exploration.md` | 3 (control surfaces + auth + audit) | 3 | High | Sits cleanly on top of completed dashboard plans. |

**Recommended pilot:** `lifecycle-opus-planner-sonnet-executor` — strongest orthogonal section split + direct continuity with the lifecycle work the carcass primitives were built for (closes the dogfood loop).

**Backup pilot:** `cost-catalog` if lifecycle-opus exploration needs more design work first.

### 3.2 Decision pending — user input required

Three options presented; user picks one Wave 2 pilot or runs `/design-explore` on multiple candidates in parallel and selects after.

---

## 4. Parallelizable command actions — main session subagents

These can run concurrently in independent main sessions or via `Task` tool invocations. Each is independent (no shared write surface between tracks).

### 4.1 Wave 2 pilot kickoff (parallel-friendly)

- **Track A — primary pilot:** `/design-explore docs/lifecycle-opus-planner-sonnet-executor-exploration.md` → `/master-plan-new` once `## Design Expansion` block lands.
- **Track B — backup pilot:** `/design-explore docs/cost-catalog-exploration.md` → optional master-plan birth.
- **Track C — alt candidate:** `/design-explore docs/actionable-agent-dashboard-exploration.md`.

Run any subset in parallel. Each writes to a distinct exploration doc → no collision. After all return, pick the strongest expansion + birth its master plan.

### 4.2 Independent in-flight work (parallel with Wave 2 prep)

- **Track D — extend asset-pipeline:** `/master-plan-extend asset-pipeline` (5 pending stages already filed; check next actionable via `master_plan_next_actionable`).
- **Track E — decompose pending plans:** `/stage-decompose backlog-yaml-mcp-alignment 1` (18 pending stages — likely skeletons).
- **Track F — ship a no-Unity stage:** any pending stage on a plan with `db-lifecycle-extensions`-style scope (no Unity, no UI) can chain `/ship-stage` while design-explore tracks A/B/C run.

### 4.3 Post-MVP item pickup (low priority, parallel-eligible)

- **Track G — emitter stanzas:** Wire `source: self-report` emitter Phase-N tail in 5 skills (item §2.4). Each skill is independent; can fan out to 5 main-session agents.
- **Track H — `arch_decision_write` Phase B:** Standalone TECH issue → `/project-new` + `/ship` (item §2.1 Phase B wiring).

### 4.4 Suggested parallel kickoff (3 main sessions)

```
session-1: /design-explore docs/lifecycle-opus-planner-sonnet-executor-exploration.md
session-2: /design-explore docs/cost-catalog-exploration.md
session-3: /master-plan-extend asset-pipeline   (if 5 pending stages need new section roots)
```

After session-1 + session-2 return with `## Design Expansion` blocks, user selects one + spawns `/master-plan-new` in a 4th session.

---

## 5. Open log — Wave 2 decisions captured here

Append-only log of decisions made during Wave 2 pilot selection + execution. Replaces ad-hoc chat capture.

| Date | Decision | Rationale | Impact |
|------|----------|-----------|--------|
| 2026-04-30 | Authored this stub. | Wave 1 closeout — preamble flagged stub as landing pad. | Captures deferred items + Wave 2 kickoff plan. |
| 2026-04-30 | Wave 2 pilot = next fresh exploration (not an existing doc). | Avoid retro-fit drift; cleanest dogfood signal when first exploration enters carcass+section natively from birth. | All §3.1 candidates archived. Session paused until next exploration topic chosen. |
| 2026-04-30 | `/design-explore` ↔ carcass+section alignment gap analysis authored — `docs/design-explore-carcass-alignment-gap-analysis.md`. | Wave 2 pilot enters design-explore natively → pre-validate skill carries plan-shape gate + ≥3 plan-scoped arch_decisions + per-section surface clusters + carcass/section grouping in Implementation Points. | 10 gaps (4 critical / 3 important / 3 nice-to-have). Recommendation: land Critical tier as `/master-plan-extend parallel-carcass-rollout` Stage 3.x before Wave 2 kickoff. |
| 2026-04-30 | Stage 3.1 filed as Wave 2 prep extension — Critical-tier (C1-C4) alignment fixes. | `/master-plan-extend parallel-carcass-rollout docs/design-explore-carcass-alignment-gap-analysis.md` appended Stage 3.1 with 4 tasks (T3.1.1 plan-shape gate poll · T3.1.2 ≥3 plan-scoped arch_decisions · T3.1.3 carcass+sections persist + IP grouping · T3.1.4 master-plan-new + master-plan-extend upstream wiring). | Top status flipped In Progress — Stage 3.1 pending. Important + Nice-to-have tiers (I1-I3, N1-N3) remain in extensions stub §2.x for later pickup. Next: `/stage-file parallel-carcass-rollout Stage 3.1`. |

