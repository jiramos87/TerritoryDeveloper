# Zone S + Economy — Master Plan (MVP)

> **Last updated:** 2026-04-20
>
> **Status:** In Progress — Step 3 (Stage 9 Done)
>
> **Scope:** Zone S (state-owned 4th zone channel, 7 sub-types) + economy depth — per-sub-type envelope budget allocator, floor-clamped treasury (hard cap, no negative balance), single-bond-per-scale-tier ledger, extended monthly-maintenance contract via `IMaintenanceContributor` registry, save schema bump. Bucket 3 of full-game MVP umbrella (`ia/projects/full-game-mvp-master-plan.md`).
>
> **Exploration source:** `docs/zone-s-economy-exploration.md` (§Design Expansion — Interview summary, Chosen Approach, Architecture, Subsystem Impact, Implementation Points IP-1..IP-8, Examples, Review Notes).
>
> **Locked decisions (do NOT reopen in this plan):**
> - Approach E — 4th zone channel + budget service contract together, one save-schema bump.
> - 7 sub-types behind one shared enum extension (`StateServiceLight/Medium/Heavy` × Building/Zoning). Sub-type id stored in `Zone` sidecar field, NOT enum.
> - Envelope budget — 7 pct sliders sum-locked to 100% × global S monthly cap. `TryDraw` checks envelope remaining AND treasury floor BEFORE deduction.
> - Hard-cap treasury — balance NEVER negative across ALL spend call sites (systemic floor clamp, not opt-in).
> - Single concurrent bond per scale tier, fixed principal + fixed term + fixed interest rate. Proactive injection lever, not remedial overdraft.
> - Out-of-scope MVP: RCI service coverage, desirability, happiness contribution, cross-scale budget transfer, bond secondary market, bond rating, interest-rate tiering, S tax revenue.
>
> **Schema-version note:** exploration says v1→v2 but current repo `GameSaveData.CurrentSchemaVersion = 3` (see `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs:404`). Real bump is **v3→v4**; semantic migration payload is identical to the exploration spec. Locked.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Glossary gap (blocks umbrella column (e)):** new domain terms need glossary rows before any task files can close — `Zone S`, `BudgetAllocationService`, `BondLedgerService`, `TreasuryFloorClampService`, `ZoneSService`, `IMaintenanceContributor`, `ZoneSubTypeRegistry`, `IBudgetAllocator`, `IBondLedger`, `envelope (budget)`. Land rows inside Stage 1.1 scaffolding.
>
> **Spec gap (blocks umbrella column (g)):** no `ia/specs/economy-system.md` exists yet. Author new reference spec covering Zone S + envelope budget + bond ledger + maintenance contributor registry. Task lands in Step 3 integration stage.
>
> **Read first if landing cold:**
> - `docs/zone-s-economy-exploration.md` — full design + IP breakdown + 4 worked examples. §Design Expansion is ground truth.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella orchestrator (Bucket 3 owner).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons), #5 (no direct `gridArray` outside `GridManager`), #6 (no new `GridManager` responsibilities — helper carve-out under `Managers/GameManagers/*Service.cs`), #12 (project spec under `ia/projects/`).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / ZoneType enum extension + sub-type registry](stage-1-zonetype-enum-extension-sub-type-registry.md) — _Final_
- [Stage 2 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `TreasuryFloorClampService` + systemic spend delegation](stage-2-treasuryfloorclampservice-systemic-spend-delegation.md) — _Final_
- [Stage 3 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `BudgetAllocationService` + save-schema v3→v4 migration](stage-3-budgetallocationservice-save-schema-v3-v4-migration.md) — _Final_
- [Stage 4 — Economy services: bond ledger + maintenance registry + Zone S placement / `BondLedgerService` + save-schema bond registry](stage-4-bondledgerservice-save-schema-bond-registry.md) — _Final_
- [Stage 5 — Economy services: bond ledger + maintenance registry + Zone S placement / `IMaintenanceContributor` registry + deterministic iteration](stage-5-imaintenancecontributor-registry-deterministic-iteration.md) — _Final_
- [Stage 6 — Economy services: bond ledger + maintenance registry + Zone S placement / `ZoneSService` placement pipeline + `AutoZoningManager` guard](stage-6-zonesservice-placement-pipeline-autozoningmanager-guard.md) — _Final_
- [Stage 7 — UI surfaces + CityStats integration + economy-system reference spec / Toolbar + sub-type picker + budget panel](stage-7-toolbar-sub-type-picker-budget-panel.md) — _Final_
- [Stage 8 — UI surfaces + CityStats integration + economy-system reference spec / Bond dialog + `CityStats` + `MiniMap` palette](stage-8-bond-dialog-citystats-minimap-palette.md) — _In Progress_
- [Stage 9 — UI surfaces + CityStats integration + economy-system reference spec / `economy-system.md` reference spec + closeout alignment](stage-9-economy-system-md-reference-spec-closeout-alignment.md) — _Done_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/zone-s-economy-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/zone-s-economy-exploration.md` §Design Expansion.
- Keep this orchestrator synced with umbrella `ia/projects/full-game-mvp-master-plan.md` + rollout tracker per `/closeout` umbrella-sync rule.
- Land the 10 glossary rows inside Stages 1.1 / 1.2 / 1.3 / 2.1 / 2.2 / 2.3 / 3.3 (distributed per stage) BEFORE umbrella column (e) can tick.
- Author `ia/specs/economy-system.md` in Stage 3.3 BEFORE umbrella column (g) align gate can close.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (RCI service coverage, desirability, cross-scale transfer, bond market, bond rating, interest tiering, S tax revenue, per-sub-type mini-map color split, compounding arrears) into MVP stages — they belong in a future `docs/zone-s-economy-post-mvp-extensions.md` doc.
- Merge partial stage state — every stage must land on a green bar (`unity:compile-check` + `validate:all`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Skip the v3→v4 save-migration branch — legacy v3 saves must round-trip cleanly.
- Let any spend path bypass `TreasuryFloorClampService.TrySpend` — systemic floor clamp, not opt-in. Any direct `currentMoney -=` outside the service = invariant violation.

---
