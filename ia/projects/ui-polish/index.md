# UI Polish — Master Plan (Flagship polish for Main HUD + Toolbar + Overlays)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Bucket 6 of polished-ambitious MVP. Three concentric rings (token / primitive / juice) + flagship studio-rack polish on Main HUD + Toolbar + overlay toggles + CityStats handoff. Info panels / settings / save-load / pause / onboarding / glossary / tooltips stay functional-tier (consume primitives only; no juice). Web dashboard OUT. Accessibility / localisation / gamepad / touch OUT (bucket-level hard-deferral).
>
> **Exploration source:** `docs/ui-polish-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Review Notes).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E — Hybrid UiTheme-first + Shell/data separation + Shared Juice Layer.
> - Flagship studio-rack treatment on Main HUD + Toolbar + Overlays ONLY. Other screens consume primitives, no juice.
> - CityStats dashboard layout / charts / data wiring owned by CityStats bucket (`ia/projects/citystats-overhaul-master-plan.md`). This plan owns primitives + tokens + juice helpers only.
> - `UIManager` accretion forbidden — touch only via new `UIManager.ThemeBroadcast.cs` partial.
> - Invariant #3 hard (no per-frame `FindObjectOfType`); BUG-14 resolves in Step 5 HUD migration.
> - Invariant #4 hard (no new singletons) — `JuiceLayer` + `ThemeBroadcaster` are scene MonoBehaviours, Inspector-wired, `FindObjectOfType` fallback in `Awake` only.
> - Bucket 7 SFX wiring exposed via optional `ISfxEmitter` interface — NOT wired in this bucket.
> - `OnValidate` repaint broadcast guarded by `#if UNITY_EDITOR`.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/ui-polish-exploration.md` — full design + architecture + 4 examples. §Design Expansion is ground truth.
> - `ia/specs/ui-design-system.md` — existing UiTheme spec. This plan extends §1 (tokens) + §1.5 (motion) + §2 (components) normatively.
> - `ia/projects/citystats-overhaul-master-plan.md` — downstream consumer of token + primitive + juice handoff (FEAT-51).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — #3 (no per-frame `FindObjectOfType`), #4 (no new singletons), #6 (no `UIManager` bloat).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Token ring extension / Token schema extension + default asset defaults](stage-1-token-schema-extension-default-asset-defaults.md) — _In Progress (TECH-309, TECH-310, TECH-311, TECH-312, TECH-313 filed)_
- [Stage 2 — Token ring extension / OnValidate repaint broadcast + tests](stage-2-onvalidate-repaint-broadcast-tests.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3 — ThemedPrimitive ring / IThemed + ThemedPrimitiveBase](stage-3-ithemed-themedprimitivebase.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — ThemedPrimitive ring / Primitives batch A (Panel / Button / Label / Icon / Tooltip)](stage-4-tooltip.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — ThemedPrimitive ring / Primitives batch B (Tab / Slider / Toggle / List / OverlayToggleRow) + broadcaster wiring](stage-5-overlaytogglerow-broadcaster-wiring.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — StudioControl ring / IStudioControl + StudioControlBase + contract tests](stage-6-istudiocontrol-studiocontrolbase-contract-tests.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — StudioControl ring / Simple widgets (LED / IlluminatedButton / SegmentedReadout / DetentRing)](stage-7-detentring.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — StudioControl ring / Complex widgets (Knob / Fader / VUMeter / Oscilloscope) + tests](stage-8-oscilloscope-tests.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — JuiceLayer ring / JuiceLayer + helpers batch A (TweenCounter / PulseOnEvent / ShadowDepth)](stage-9-shadowdepth.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 10 — JuiceLayer ring / Helpers batch B (SparkleBurst / NeedleBallistics / OscilloscopeSweep) + alloc tests](stage-10-oscilloscopesweep-alloc-tests.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 11 — Flagship HUD + Toolbar + overlay polish / HUD migration + BUG-14 + TECH-72](stage-11-hud-migration-bug-14-tech-72.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 12 — Flagship HUD + Toolbar + overlay polish / Toolbar + overlay migration](stage-12-toolbar-overlay-migration.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 13 — CityStats handoff artifacts / Spec publication + glossary gap audit](stage-13-spec-publication-glossary-gap-audit.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 14 — CityStats handoff artifacts / Cross-plan update + handoff notify](stage-14-cross-plan-update-handoff-notify.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/ui-polish-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/ui-polish-exploration.md` + downstream plans (citystats-overhaul).
- Respect ring order strictly — do NOT start Step 2 until Step 1 Final; do NOT start Step 3 until Step 2 Final; Step 4 may parallelize with Step 3 Stage 3.3 retrofit window (see Stage 4.2 note). Step 5 blocked on Steps 2+3+4 Final. Step 6 (handoff) can run after Step 4 Final — does not block on Step 5.
- Keep this orchestrator synced with any umbrella issue (full-game-mvp tracker row) — per `/closeout` umbrella-sync rule.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers `Status: Final`; the file stays.
- Silently promote functional-tier screens (info panels / settings / save-load / pause / onboarding / glossary / tooltips) into flagship juice scope — they stay primitives-only per Locked decisions. Studio-rack on those surfaces is post-MVP.
- Edit `UIManager.cs` or existing `UIManager.*.cs` partials beyond the single one-line `Start_ThemeBroadcast()` call from Stage 2.3 T2.3.5 — invariant #6.
- Add per-frame `FindObjectOfType` anywhere — invariant #3 enforced by BUG-14 verification in Stage 5.1 T5.1.4.
- Add new singletons — `JuiceLayer` + `ThemeBroadcaster` + `UiTheme` all scene / asset components; invariant #4.
- Wire Bucket 7 SFX hooks in this bucket — `ISfxEmitter` interface exposed but intentionally unwired (Review Note E). Bucket 7 owns wiring.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.

---
