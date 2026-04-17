# Landmarks — Exploration (stub)

> Pre-plan exploration stub for **Bucket 4-b** of the polished-ambitious MVP (per `docs/full-game-mvp-exploration.md` + `ia/projects/full-game-mvp-master-plan.md`). **Split** off from the original merged `utilities-landmarks-exploration.md` stub — sibling is `docs/utilities-exploration.md`. Seeds a `/design-explore` pass that expands Approaches + Architecture + Subsystem impact + Implementation points. **Scope = landmarks v1 (progression ladder + commissioning). NOT utilities sim (sibling doc), NOT Zone S + per-service budgets (Bucket 3) except as a commission-budget consumer, NOT city-sim signals (Bucket 2), NOT CityStats (Bucket 8), NOT multi-scale core (Bucket 1 — couples to tier ladder but does not define it).**

---

## Problem

Territory Developer has no landmark progression. A polished ambitious MVP needs one:

- **No progression hook.** Testers reach a population threshold and nothing happens. No unlock moment, no saved-for commission, no reason to keep playing past the initial growth curve.
- **Landmarks don't exist as a system.** A single landmark sprite + a placement tool have been discussed but no progression contract, no scale-unlock ladder, no "commission + months-long build" mechanic.

**Design goal (high-level):** landmarks v1 = two parallel progression tracks — tier-defining landmarks (scale-tier transition rewards) and intra-tier reward landmarks (super-building unlocks at intermediate pop milestones). Some intra-tier rewards are "super-utility" buildings that couple to the utilities sim via a narrow catalog interface (sibling `docs/utilities-exploration.md`).

## Approaches surveyed

_(To be expanded by `/design-explore` — seed list only.)_

- **Approach A — Simple landmark registry.** Flag on zone with static sprite. Minimal churn; no progression, no commissioning. Fails the "progression ladder" framing.
- **Approach B — Scale-unlock rewards only.** One landmark per scale-tier transition, free gift on threshold cross. Matches "tier-defining" half of the hybrid but drops intra-tier rewards.
- **Approach C — Saved-for big projects only.** Player commits budget → N in-game months build → landmark tile placed. Matches "commissioning" surface but drops the unlock-moment / tier-defining track.
- **Approach D — Hybrid two-track, both pop-driven.** Tier-defining landmarks (scale-tier transition reward) + intra-tier reward landmarks (designer-tuned pop milestones inside a tier — "super-buildings" scaling existing services / utilities). Couples to Bucket 1 scale ladder for the tier-defining track; intra-tier rewards are independent designer-tuned milestones.

## Recommendation

_TBD — `/design-explore` Phase 2 gate decides._ Author's prior lean: **Approach D** (hybrid two-track). Cadence model is already locked (see below); `/design-explore` should validate architecture and interface surface against that lock.

## Locked decisions (prior design session)

- **Landmark unlock cadence = hybrid two-track, both pop-driven.**
  1. **Tier-defining landmarks** — unlock at scale-tier transitions. When the city crosses the population threshold that advances it to the next scale tier (e.g. 50k pop → region scale), the landmark associated with that tier becomes available (e.g. regional plocks with region scale). These landmarks mark and define the new tier.
  2. **Intra-tier reward landmarks** — unlock at intermediate population milestones inside a tier. Designer-tuned thresholds between tier transitions surface "super-buildings" that scale up existing services or utilities — e.g. a big power plant outputting 10× a normal plant, or a big state university (vs a normal school). These reward sustained growth within a tier.
- **Explicit rejections:**
  - No prestige / spendable-points currency.
  - No multi-condition gating (no zone-mix prereqs like "needs ≥3 commercial blocks").
  - No fixed-pop-threshold-only model (tier-defining track must couple to scale tiers).
- **Coupling.** Tier-defining unlocks fire from the same pop trigger that advances scale (Bucket 1 scale ladder). Intra-tier rewards are independent designer-tuned milestones.
- **Commissioning surface.** Landmark commission = bond-backed multi-month expenditure drawn against Bucket 3 per-service budget. Tier-defining landmarks = free gift on threshold cross (no commission cost). Intra-tier rewards = commissioned (cost budget + build time).

## Open questions

- **Landmark catalog authoring.** ScriptableObject per landmark vs code table? Sprite pipeline (Bucket 5 archetype spec). Schema: id, display name, tier / pop-milestone gate, sprite ref, commission cost, build duration, optional utility-contributor registry pointer (for super-utility rows) + scaling factor.
- **Big-project build mechanic.** Player commits budget → construction starts → N in-game months progress bar → landmark tile placed. Cancellable mid-build (partial refund)? Pause-able? Interaction with Bucket 3 deficit spending (can you commission in deficit?).
- **Tier-defining vs intra-tier count.** Tier-defining = 1 per scale transition = 2 (city→region, region→country). Intra-tier = how many per tier? 1–2 per tier = 3–6 total? Confirm with designer intent.
- **Super-utility interface (contract).** Intra-tier reward "super-building" entries register as normal utility contributors with a scaling factor. Schema: catalog row carries `utilityContributorRef` (nullable) + `contributorScalingFactor` (float, default 1.0). Owned here; consumed by sibling `docs/utilities-exploration.md` and Bucket 3 service registry.
- **UI surface.** Landmark progress panel (unlocked / in-progress / available-to-commission), big-project commission dialog. Which ship MVP? Coordinate with Bucket 6 UI polish.
- **Save schema impact.** Landmark unlock flags, big-project commitment ledger (principal + progress + ETA). `schemaVersion` bump — coordinate with Bucket 3's bump.
- **Consumer-count inventory.** Which surfaces read landmark state (HUD, info panels, CityStats Bucket 8, web dashboard)? Decide at exploration time for Bucket 8 parity contract.
- **Invariant compliance.** No new singletons. `LandmarkProgressionService` / `BigProjectService` as MonoBehaviour + Inspector-wired. `HeightMap` safety — no big-project placement writes cell height except via existing carving path (invariant #1).
- **Hard deferrals re-check.** Heritage / cultural landmarks, landmark-specific tourism effects, destructible landmarks — confirmed OUT at bucket level.

---

_Next step._ Run `/design-explore docs/landmarks-exploration.md` to expand Approaches → Architecture → Subsystem impact → Implementation points → subagent review. Then `/master-plan-new` to author `ia/projects/landmarks-master-plan.md`.
