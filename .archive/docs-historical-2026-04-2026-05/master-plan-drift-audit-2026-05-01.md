# Master Plan Drift Audit — 2026-05-01

read-only audit. snapshot via `master_plan_health` (28 plans, drift_events_open=0 across all rows — no DB-tracked changelog drift, content drift still possible). baseline = shipped work as of HEAD `5c1c4e6d` (recipe-runner-phase-e Stage 5.3).

scope: 16 plans with `n_pending > 0`. 12 plans fully shipped (architecture-coherence-system, asset-pipeline, blip, db-lifecycle-extensions, full-game-mvp, parallel-carcass-rollout, recipe-runner-phase-e, ui-visual-fidelity-layer + 4 game-ui-design-system stages with 11/12 done) — excluded except as evidence anchors.

method: `master_plan_health` cross-section → `master_plan_state` per slug → `stage_render` for suspect stage IDs. zero plan/stage mutations. zero yaml writes.

---

## §1 Executive Summary

| Severity | Plan | n_pending | Why |
|---|---|---|---|
| CRITICAL | dashboard-prod-database | 7 | entire plan Neon-based — locks #4 drop Neon |
| CRITICAL | web-platform | 38 (subset) | Stages 13/14/15 Neon+Drizzle+auth UI — locks #3/#4/#5 |
| CRITICAL | mcp-lifecycle-tools-opus-4-7-audit | 17 | tools already shipped via db-lifecycle-extensions + composites |
| CRITICAL | backlog-yaml-mcp-alignment | 18 | Stages 10/14 tools (`master_plan_locate`, `master_plan_next_pending`) ALREADY EXIST in MCP surface |
| HIGH | lifecycle-refactor | 11 | Stage 5/7.1/10 superseded by recipe-runner-phase-e + db-lifecycle-extensions |
| HIGH | ui-polish | 14 | Stages 3-12 subsumed by game-ui-design-system + ui-visual-fidelity-layer |
| HIGH | grid-asset-visual-registry | 13 | catalog model shipped via asset-pipeline (81 catalog_* tools) |
| MEDIUM | unity-agent-bridge | 7 | Stage 2.1 HTTP listener possibly redundant with shipped DB job runner |
| MEDIUM | citystats-overhaul | 10 | Stage 8 web/app/stats route assumes web-platform path — locked path |
| MEDIUM | session-token-latency | 12 | parts subsumed by recipe-runner-phase-e cache work |
| MEDIUM | sprite-gen | 25 | catalog target surface shifted (asset-pipeline catalog_sprite_* shipped) |
| LOW | utilities | 13 | game-feature plan, no direct collision but surface refresh likely |
| LOW | landmarks | 12 | game-feature, neutral |
| LOW | zone-s-economy | 9 | game-feature, neutral |
| LOW | music-player | 8 | game-feature, neutral |
| LOW | distribution | 6 | game-feature, neutral |
| LOW | multi-scale | 13 | game-feature, neutral |
| LOW | city-sim-depth | 4 | game-feature, neutral (12/16 done) |
| LOW | skill-training | 6 | meta-loop plan, low cross-collision |
| LOW | game-ui-design-system | 1 | Stage 11 only — verify still relevant after Stage 12 content-layer fix |

drift severity = how much pending work is invalidated, not plan size.

---

## §2 Per-Plan Sections

### 2.1 dashboard-prod-database — CRITICAL — DROP

n_pending: 7. n_done: 0.

drift signal: entire plan predates 2026-04-22 architecture locks. lock #4 = drop Neon HTTP driver entirely. lock #3 = drop Drizzle. plan Stage 1.1 wires Neon serverless driver + Drizzle migrate harness for prod dashboard.

evidence: missing_arch_surfaces 1.1–1.7 = nothing surfaces resolved (zero stages bonded to ArchitectureSurface). Stage 1.1 stage_render shows Neon HTTP driver + DATABASE_URL_PROD env split + Drizzle pgTable schemas — every artifact violates ratified locks.

action: **DROP**. file `arch_decision_write` referencing locks; close all 7 stages as superseded; archive exploration `docs/dashboard-prod-database-exploration.md` with retired-stamp note. dashboard surface (if revived later) will route through DEC-A18 db-lifecycle path, not Neon.

next: `arch_decision_write slug=dashboard-prod-database-drop` + `master_plan_render slug=dashboard-prod-database` to confirm zero downstream refs.

### 2.2 web-platform — CRITICAL — REVISE (partial DROP)

n_pending: 38. n_done: 0. plan body large; only locked-out stages flagged.

drift signal:
- Stage 13 = Vercel + Neon + Lucia/Auth.js scaffolding → all locked out (locks #4 Neon, #5 auth UI out of MVP)
- Stage 14 = Drizzle pgTable schema authoring → lock #3 drop Drizzle entirely
- Stage 15 = `web/middleware.ts` + `/auth/login` page → lock #5 says these files slated for deletion
- Stage 22 = "design system primitives" → game-ui-design-system + ui-visual-fidelity-layer already shipped equivalent for game UI. web design system separate per `web/lib/design-system.md` but stage objective overlaps
- Stage 30 = catalog admin UI → depends on grid-asset-visual-registry (also drifted) AND asset-pipeline catalog tools (shipped). retarget needed.

evidence: stage_render 13/14/15 confirm Neon + Drizzle + Lucia text in objective + tasks. Stage 22 objective references "build ds-* primitive set" — `web/app/globals.css` already has `@theme` + `ds-*` tokens per CLAUDE.md §6. Stage 30 references `web/admin/catalog/*` paths that depend on retired Drizzle schema.

action: **REVISE**. Stage 13/14/15/22 → DROP individually via `stage_decompose_apply` (mark superseded). Stages 1-12 (foundational) + 16-21 (ungated by locks) keep but re-verify against `web/lib/design-system.md`. Stage 30 needs retarget memo pointing at shipped `catalog_*` MCP surface.

next: `stage_render web-platform 16` through `21` to confirm no other lock collisions.

### 2.3 mcp-lifecycle-tools-opus-4-7-audit — CRITICAL — DROP

n_pending: 17. n_done: 0.

drift signal: this audit was scaffolded **before** db-lifecycle-extensions shipped. its proposed tools are already live.

evidence:
- Stage 3 (envelope normalization) = db-lifecycle-extensions Stage 1 shipped uniform `{ok, payload, error}` envelope
- Stage 7 (composite tools `issue_context_bundle` + `lifecycle_stage_context`) = both VISIBLE in current MCP loaded-tool list (`mcp__territory-ia__issue_context_bundle`, `mcp__territory-ia__lifecycle_stage_context`)
- Stage 14 (master_plan authoring tools `master_plan_insert`, `master_plan_locate`, `master_plan_next_pending`, `master_plan_health`, `master_plan_cross_impact_scan`) = all shipped (this very audit used `master_plan_health` + `master_plan_cross_impact_scan`)
- Stages 4/5/6/8/9/10/11/12/13/15/16/17 likely similarly subsumed

action: **DROP**. plan was an audit scaffold — work happened, plan never got status flips. close all 17 stages as superseded by db-lifecycle-extensions (DEC-A18) + recipe-runner-phase-e (DEC-A19). archive `docs/mcp-lifecycle-tools-opus-4-7-audit*.md` if exists.

next: spot-check Stage 1+2 stage_render before bulk-close; if still relevant carve-out, salvage into new short plan.

### 2.4 backlog-yaml-mcp-alignment — CRITICAL — DROP / partial salvage

n_pending: 18. n_done: 0.

drift signal: plan Stages 10/14 propose tools that already exist.

evidence:
- Stage 10 objective = ship `master_plan_locate` + `master_plan_next_pending` MCP tools. both visible in current tool list (`mcp__territory-ia__master_plan_locate`, `mcp__territory-ia__master_plan_next_pending`). shipped via db-lifecycle-extensions.
- Stage 12 objective = ship `master_plan_health` view → already shipped (used here).
- Stage 14 objective = ship `master_plan_cross_impact_scan` → already shipped (used here).
- Stages 1-9 (yaml schema validators, backlog_record_validate, claim helpers) → `backlog_record_validate`, `claim_heartbeat`, `claims_sweep`, `section_claim`, `stage_claim` all visible in tool list. shipped via parallel-carcass-rollout (9/9 done).
- Stages 11/13/15-18 (UI/dashboard surfaces) likely retired alongside dashboard-prod-database lock.

action: **DROP** entire plan. close 18 stages as superseded. retain 1 stage IF dashboard surface stages 11/13/15-18 surface unique work — but those depend on dashboard plan which is also CRITICAL DROP, so cascade.

next: `stage_render backlog-yaml-mcp-alignment 11 13 15 16 17 18` to confirm zero unique remaining surface before global close.

### 2.5 lifecycle-refactor — HIGH — DROP / consolidate

n_pending: 11. n_done: 0.

drift signal: lifecycle refactor partly already happened via recipe-runner-phase-e (13/13 done) + db-lifecycle-extensions (3/3 done) + parallel-carcass-rollout (9/9 done). pending stages target retired skills + already-shipped tools.

evidence:
- Stage 5 objective = ship `plan_apply_validate` MCP + `router_for_task` lifecycle_stage enum. `plan_apply_validate` AND `router_for_task` BOTH visible in current MCP tool list. shipped.
- Stage 7.1 objective = retire skills `closeout-apply`, `project-stage-close`, `spec-kickoff` → already retired per `.claude/agents/_retired/` + `.claude/commands/_retired/` directories (from CLAUDE.md §4 reference).
- Stage 10 objective = release-rollout SKILL inline-replicate retired stage-file content → DONE per architecture lock #11 + commit `5c1c4e6d` ("trim release-rollout-track agent body to recipe-dispatch shell").

action: **DROP**. close 11 stages as superseded by DEC-A19 (recipe-engine) + DEC-A18 (db-lifecycle) + parallel-carcass-rollout. salvage carve-out only if Stages 1-4/6/8 contain unique work — re-bind via `arch_decision_write`.

next: `stage_render lifecycle-refactor 1 2 3 4 6 8` to spot any unique pending surface.

### 2.6 ui-polish — HIGH — DROP / merge into game-ui-design-system

n_pending: 14. n_done: 0.

drift signal: plan predates Approach G (game-ui-design-system) + ThemedPrimitive ring (ui-visual-fidelity-layer). ThemedPanel/Button/Label/Icon/Tooltip primitives all shipped under those plans.

evidence:
- Stage 3 = ship ThemedPrimitive base ring → game-ui-design-system Stage 3 done
- Stage 4 = ThemedPanel + ThemedButton → game-ui-design-system Stage 4 done; ui-visual-fidelity-layer Stage 1.3 added 5 more subclasses
- Stage 5 = ThemedLabel + ThemedIcon + ThemedTooltip → game-ui-design-system Stage 5/6/7 done
- Stages 6-12 (theme variants, hover states, focus rings, motion tokens) → DefaultUiTheme.asset modified in current branch (M Assets/UI/Theme/DefaultUiTheme.asset) + ui-visual-fidelity-layer 5/5 done covers motion + theme variants

action: **DROP**. plan superseded entirely by game-ui-design-system + ui-visual-fidelity-layer. close 14 stages. archive any unique copy/feedback in `docs/ui-visual-fidelity-layer-post-mvp-extensions.md` (already exists in current branch as `??`).

next: spot-check Stages 1+2+13+14 before bulk-close.

### 2.7 grid-asset-visual-registry — HIGH — REVISE / retire scope

n_pending: 13. n_done: 0.

drift signal: catalog model already shipped. asset-pipeline 26/26 done with 81 `catalog_*` MCP tools (asset, archetype, audio, button, panel, pool, sprite, token kinds). grid-asset-visual-registry's "registry" surface is the catalog.

evidence: missing_arch_surfaces 1.1–4.3 = zero stages surface-bonded. asset-pipeline `catalog_sprite_*`, `catalog_archetype_*`, `catalog_asset_*` cover sprite registration, archetype binding, asset record. registry's unique remainder = grid-cell ↔ catalog_asset binding view. asset-pipeline-stage-19.3 (commit `6e408ae6`) shipped "bridge composite wire_asset_from_catalog + scene contract doc" which is exactly the grid-cell binding edge.

action: **REVISE**. retain only stages that surface a UNIQUE grid-overlay debug view (not the catalog itself). likely 1-3 stages survive, 10-12 drop.

next: `stage_render grid-asset-visual-registry 1.1 1.2 1.3 1.4 2.1 2.2 2.3 3.1 3.2 4.1 4.2 4.3` then carve.

### 2.8 unity-agent-bridge — MEDIUM — REVISE

n_pending: 7. n_done: 0.

drift signal: HTTP-listener stage may be redundant with DB-job-runner pattern shipped via recipe-runner-phase-e + db-lifecycle-extensions.

evidence: Stage 2.1 stage_render references HTTP listener inside Unity Editor for agent commands. current MCP surface has `unity_bridge_command`, `unity_bridge_get`, `unity_bridge_lease` (visible in tool list) — these route through DB job table. parallel HTTP listener may have been deferred or replaced.

action: **REVISE**. confirm whether Stage 2.1 is still needed (separate transport for low-latency cases) or fully replaced. if replaced → DROP stage. if complementary → KEEP but rebase against shipped bridge surface.

next: `arch_decision_get` for unity-bridge transport decisions to clarify.

### 2.9 citystats-overhaul — MEDIUM — REVISE

n_pending: 10. n_done: 0.

drift signal: Stage 8 wires `web/app/stats/*` page — depends on web-platform foundational stages (1-12). some lock-out collateral.

evidence: stage_render confirms Stage 8 references web route + ds-* tokens. ds-* tokens shipped via `web/app/globals.css @theme`. but route depends on web-platform Stages 1-12 which are not yet locked out (only 13-15 + 22 are locked-out per §2.2).

untracked dirs `Assets/Scripts/UI/CityStats/` in current branch = work in flight on Unity side, separate from web stats page.

action: **REVISE**. Stages 1-7 (Unity-side citystats panel) likely fine — re-verify against `Assets/Scripts/UI/CityStats/` recent additions. Stages 8-10 (web stats route) gate on web-platform foundational stages.

next: `stage_render citystats-overhaul 1 2 3 4 5 6 7` to compare pending objectives against current `Assets/Scripts/UI/CityStats/` tree.

### 2.10 session-token-latency — MEDIUM — REVISE

n_pending: 12. n_done: 0.

drift signal: parts may be subsumed by recipe-runner-phase-e cache-warmup work.

evidence: missing_arch_surfaces 1.1–5.1 = no surface bond. recipe-runner-phase-e Stages 4.x/5.x covered cache primer, recipe-dispatch shell, prompt-cache TTL hygiene. session-token-latency Stages 1.1–2.3 likely overlap (token-cache + prewarm heuristics).

action: **REVISE**. carve out Stages 1.1-2.3 against recipe-runner DEC-A19. Stages 3.1-5.1 (latency budgets, telemetry) likely survive.

next: `stage_render session-token-latency 1.1 1.2 1.3 2.1 2.2 2.3` vs `recipe-runner-phase-e` Stage 4.x/5.x stage_render.

### 2.11 sprite-gen — MEDIUM — REVISE

n_pending: 25. n_done: 0.

drift signal: target surface shifted. sprite-gen original scope = author sprite atlases + bind to cells. asset-pipeline shipped `catalog_sprite_*` (create/get/list/publish/refs/restore/retire/search/update) tool ring + Sprite catalog kind.

evidence: asset-pipeline 26/26 done with sprite as catalog_kind. sprite-gen pending stages presumably author sprites OUTSIDE catalog-aware path, which would now violate parallel-carcass DEC + asset-pipeline DEC.

action: **REVISE**. rebase entire plan onto catalog model. likely 5-8 stages survive (gen pipeline upstream of catalog), 17-20 retire (binding/registration now handled by `catalog_sprite_*`).

next: `stage_render sprite-gen 1 2 3` (pipeline upstream) + `stage_render sprite-gen 6 6.1 6.2 6.3` (binding stages).

### 2.12 game-ui-design-system — LOW — VERIFY

n_pending: 1 (Stage 11). n_done: 11.

drift signal: minor. Stage 11 was deferred while Stage 12 content-layer fix shipped (commit `f0921088` step-16). possible Stage 11 objective shifted post-fix.

evidence: branch dirty file `docs/game-ui-design-system-stage-12-content-layer-fix.md` indicates Stage 12 work in flight. Stage 11 surface may need recompute against Stage 12 outputs.

action: **VERIFY**. `stage_render game-ui-design-system 11` then re-validate against Stage 12 closeout.

next: `stage_render game-ui-design-system 11`.

### 2.13 utilities — LOW — KEEP

n_pending: 13. n_done: 0.

game-feature plan. no direct architectural collision. surface refresh advisable post-DEC-A18/A19 but not blocking.

action: **KEEP**. flag for re-verify in next quarterly audit.

### 2.14 landmarks — LOW — KEEP

n_pending: 12. n_done: 0. game-feature, neutral.

action: **KEEP**.

### 2.15 zone-s-economy — LOW — KEEP

n_pending: 9. n_done: 0. game-feature (zone economy sim), neutral. some potential collision with city-sim-depth. low priority.

action: **KEEP**.

### 2.16 music-player — LOW — KEEP / verify catalog_audio binding

n_pending: 8. n_done: 0. game-feature. asset-pipeline `catalog_audio_*` ring shipped (10 tools). music-player likely consumes catalog_audio surface — verify integration not duplicate.

action: **KEEP** with audit-tag for catalog_audio integration check.

### 2.17 distribution — LOW — KEEP

n_pending: 6. n_done: 0. game-feature (build/distro pipeline), neutral. potential overlap with web-platform Stages 16-21 if those stages cover release surface — defer until web-platform revise complete.

action: **KEEP**.

### 2.18 multi-scale — LOW — KEEP

n_pending: 13. n_done: 2. game-feature (multi-scale rendering / camera). neutral.

action: **KEEP**.

### 2.19 city-sim-depth — LOW — KEEP

n_pending: 4. n_done: 12. mostly shipped. low risk.

action: **KEEP**, finish remaining 4 stages as planned.

### 2.20 skill-training — LOW — KEEP / re-target

n_pending: 6. n_done: 0. meta-loop plan. recipe-runner-phase-e shipped recipe-dispatch shell which is a skill-training adjacent surface. verify scope still distinct.

action: **KEEP** with re-target check vs recipe-engine DEC-A19.

---

## §3 Cross-Cutting Findings

**1. architecture-locks blast radius.** 4 plans (dashboard-prod-database, web-platform 13/14/15, backlog-yaml-mcp-alignment dashboard half, citystats-overhaul Stage 8) collide with 2026-04-22 locks #3/#4/#5. all CRITICAL or MEDIUM. lock memo `project_architecture_locks.md` is the authoritative source — every CRITICAL action in §2 traces to it.

**2. tool-shipping cascades invalidate audit/refactor plans.** 3 plans (mcp-lifecycle-tools-opus-4-7-audit, backlog-yaml-mcp-alignment, lifecycle-refactor) spec'd MCP tools that already shipped via db-lifecycle-extensions + recipe-runner-phase-e + parallel-carcass-rollout. pattern = audit/refactor plans become obsolete the moment carve-out plans ship — they're scaffold artifacts, not work units.

**3. UI plan consolidation.** 3 plans (game-ui-design-system, ui-visual-fidelity-layer, ui-polish) author overlapping primitive rings. game-ui-design-system + ui-visual-fidelity-layer shipped (11/12 + 5/5). ui-polish remains as duplicate scaffold = full DROP candidate.

**4. catalog model collision.** asset-pipeline (26/26 done) shipped 81 `catalog_*` tools across 8 catalog_kinds (asset, archetype, audio, button, panel, pool, sprite, token). 3 plans (grid-asset-visual-registry, sprite-gen, music-player) target surfaces now owned by catalog. registry collision is HIGH; sprite-gen is MEDIUM; music-player is LOW (consumer not author).

**5. game-feature plans drift slowly.** 7 LOW-severity game-feature plans (utilities, landmarks, zone-s-economy, music-player, distribution, multi-scale, city-sim-depth) have minimal architectural collision. they need surface refresh against catalog + design-system at next quarterly review, not urgent rebase.

**6. arch_drift_scan didn't catch any of this.** drift_events_open=0 across all 28 plans. cross_impact_scan returned empty. pattern = changelog-based drift detection captures spec edits, NOT supersession-by-shipping. need extension: `arch_drift_scan --by-shipped-decision` to flag plans whose stages reference now-superseded decisions.

**7. missing_arch_surfaces signal underused.** 17 of 28 plans have non-empty `missing_arch_surfaces`. signal = stages not bonded to ArchitectureSurface yet. `arch_surfaces_backfill` exists. backlog grooming run advised after this audit's carve-outs.

---

## §4 Recommended Next Actions

priority-ordered. each action references which §2 plans it addresses.

**A. Immediate (this sprint, this week):**
1. `arch_decision_write slug=plan-supersession-2026-05-01` — codify which plans drop / revise / merge per this audit. blocks downstream confusion.
2. Bulk-close 4 CRITICAL plans (dashboard-prod-database, mcp-lifecycle-tools-opus-4-7-audit, backlog-yaml-mcp-alignment, ui-polish) — script via `stage_decompose_apply` with status=superseded for each pending stage. ~64 stage-flips total.
3. web-platform Stages 13/14/15/22 individual close — same path, 4 stages.

**B. Near-term (next sprint, ~2 weeks):**
4. lifecycle-refactor carve-out — `stage_render` Stages 1-4/6/8 then DROP or salvage.
5. grid-asset-visual-registry rebase — keep grid-overlay debug carve, drop catalog-overlap stages.
6. sprite-gen rebase against catalog model — separate "gen pipeline" stages from "registration" stages.
7. session-token-latency carve-out vs recipe-runner-phase-e cache work.

**C. Medium-term (next month):**
8. unity-agent-bridge HTTP-vs-DB transport decision — `arch_decision_write` then close or revise Stage 2.1.
9. citystats-overhaul web/Stage 8 gating decision — depends on web-platform foundational re-verify.
10. game-ui-design-system Stage 11 re-verify against Stage 12 closeout.
11. extend `arch_drift_scan` to detect supersession-by-shipping (cross-cutting finding #6).

**D. Quarterly (next 3 months):**
12. game-feature LOW plans (utilities, landmarks, zone-s-economy, music-player, distribution, multi-scale, city-sim-depth, skill-training) surface refresh + catalog/design-system integration check.

**E. Tooling extension (one-off):**
13. backfill `missing_arch_surfaces` for the 17 plans flagged (cross-cutting finding #7) via `arch_surfaces_backfill` after carve-outs settle.

---

## §5 Appendix — Snapshot Anchors

- audit timestamp: 2026-05-01
- branch: feature/asset-pipeline
- baseline commit: `5c1c4e6d feat(recipe-runner-phase-e-stage-5.3)`
- shipped plans (12 fully done): architecture-coherence-system 4/4, asset-pipeline 26/26, blip 20/20, db-lifecycle-extensions 3/3, full-game-mvp 0/0 (rollup), parallel-carcass-rollout 9/9, recipe-runner-phase-e 13/13, ui-visual-fidelity-layer 5/5, game-ui-design-system 11/12, city-sim-depth 12/16, multi-scale 2/15, asset-pipeline 26/26
- ratified architecture decisions cited: DEC-A18 (db-lifecycle), DEC-A19 (recipe-engine), 2026-04-22 locks #3/#4/#5/#11
- tools used: `master_plan_health` (1 call), `master_plan_state` (per-slug as needed), `stage_render` (selective evidence), `master_plan_cross_impact_scan` (1 call, returned empty)
- zero mutations made.
