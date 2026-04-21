---
purpose: "TECH-311 — DefaultUiTheme.asset Inspector defaults for studio-rack + motion blocks."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/ui-polish-master-plan.md"
task_key: "T1.1.3"
---
# TECH-311 — DefaultUiTheme.asset defaults (studio-rack + motion)

> **Issue:** [TECH-311](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-21

## 1. Summary

Populate `Assets/UI/Theme/DefaultUiTheme.asset` Inspector values for every new `studioRack` (TECH-309) + `motion` (TECH-310) field. Dark-first palette per `ia/specs/ui-design-system.md §7.1`. LED hues = green / amber / red triad. VU gradient = green → amber → red stops. Shadow depth stops + glow radius per exploration §Design Expansion. Motion durations: `moneyTick = 0.28s`, `needleAttack = 0.08s`, `needleRelease = 0.40s`; remaining entries (`alertPulse`, `sparkleDuration`, `panelElevate`) per exploration. Satisfies Stage 1.1 Exit "DefaultUiTheme.asset updated with dark-first + studio-rack default values per ui-design-system §7.1".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Every `studioRack` + `motion` field on `DefaultUiTheme.asset` carries non-default Inspector value.
2. LED hue triad present (green / amber / red).
3. VU gradient = green → amber → red stops.
4. Motion durations match values above; easing curves populated (not the default identity line).
5. Palette consistent w/ §7.1 dark-first hue family.

### 2.2 Non-Goals

1. Schema definitions — TECH-309 / TECH-310.
2. Spec / glossary — TECH-312 / TECH-313.
3. OnValidate broadcast — Stage 1.2.
4. Consumer widget defaults — Steps 2–4.

## 4. Current State

### 4.2 Systems map

Domain: **UI changes** — `Assets/UI/Theme/DefaultUiTheme.asset` is the sole authored `UiTheme` instance (per exploration §Subsystem Impact). Asset edits happen via Unity Editor Inspector on a human-authored `.asset` YAML file — author in Editor, commit resulting diff. Reference palette = `ia/specs/ui-design-system.md §1.1` (target `UiTheme` table) + §7.1 (dark-first defaults).

## 7. Implementation Plan

### Phase 1 — Asset authoring

- [ ] Select `DefaultUiTheme.asset` in Unity Editor Project window.
- [ ] Expand `studioRack` foldout → set each field per §7.1 + exploration §Design Expansion. LED hues = `[green, amber, red]` triad w/ hex anchors from §7.1. VU gradient stops = 3-stop green→amber→red. Knob detent color = warm neutral. Fader track gradient = dark-first subtle. Oscilloscope trace + glow = accent-forward hue. Shadow depth stops = `[0.3, 0.5, 0.7]` (tier anchors). Glow radius `~4`. Glow color = accent-forward translucent. Sparkle palette = multi-hue accent set.
- [ ] Expand `motion` foldout → set durations + easing curves. `moneyTick.durationSeconds = 0.28f` w/ ease-out curve; `needleAttack = 0.08f` w/ sharp ease-in; `needleRelease = 0.40f` w/ ease-out; `alertPulse`, `sparkleDuration`, `panelElevate` per exploration §Design Expansion.
- [ ] Save asset (Ctrl+S) → commit YAML diff.
- [ ] `npm run validate:dead-project-specs` green.

## 8. Acceptance Criteria

- [ ] Every `studioRack` field carries explicit value (diff on `.asset` YAML shows populated entries).
- [ ] Every `motion` entry carries non-zero duration + populated `AnimationCurve`.
- [ ] LED hue triad verified (green / amber / red anchors).
- [ ] Motion durations match values above.
- [ ] `npm run validate:all` green.

## §Plan Author

### §Audit Notes

- Risk: YAML `.asset` merge conflicts when two authors edit same foldout. Mitigation: single serial commit; prefer local Unity save over hand-merge of binary-ish YAML without Editor verify.
- Risk: `Gradient` stops not committed as expected (Unity version drift). Mitigation: open asset in Editor post-pull; re-save if Inspector shows empty gradient.
- Ambiguity: exploration doc path `docs/ui-polish-exploration.md` vs `ui-polish-exploration` — use repo path that exists. Resolution: glob before citing in commit message.
- Invariant touch: user-facing palette strings in spec remain English per web exception; asset is data only.

### §Examples

| Token | Expected visual intent |
|-------|------------------------|
| LED hues | Green / amber / red triad |
| VU gradient | Green → amber → red |
| `moneyTick` curve | Ease-out, not linear identity |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| asset_diff | `DefaultUiTheme.asset` saved | Git diff shows populated `studioRack` + `motion` | manual |
| validate_all | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] Every new `studioRack` + `motion` field has non-default Inspector authorship committed.
- [ ] Durations for `moneyTick`, `needleAttack`, `needleRelease` match §2.1.
- [ ] `npm run validate:all` green.

### §Findings

- TECH-312 must cite the same field names when normative prose lands — sync if any rename during Editor work.

## Open Questions

1. None — tooling / asset authoring only. Palette source of record = `ui-design-system.md §7.1` + exploration §Design Expansion.
