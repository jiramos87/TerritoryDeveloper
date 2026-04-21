---
purpose: "TECH-312 — ui-design-system §1 + §1.5 normative studio-rack + motion catalog."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/ui-polish-master-plan.md"
task_key: "T1.1.4"
---
# TECH-312 — ui-design-system spec §1 + §1.5 (studio-rack + motion token catalog)

> **Issue:** [TECH-312](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-21

## 1. Summary

Extend `ia/specs/ui-design-system.md` §1 (palette / spacing) w/ studio-rack token names + role descriptions. Extend §1.5 (motion) w/ motion token catalog (`moneyTick`, `alertPulse`, `needleAttack`, `needleRelease`, `sparkleDuration`, `panelElevate`). Every field in `StudioRackBlock` (TECH-309) + `MotionBlock` (TECH-310) present normatively. Token names match `UiTheme` field names exactly — consumer rings read these by name. Link from §2 anchors primitives-to-tokens mapping. Source of record: `docs/ui-polish-exploration.md` §Design Expansion. Satisfies Stage 1.1 Exit "ui-design-system.md §1 + §1.5 sections extended; normative token names match field names".

## 2. Goals and Non-Goals

### 2.1 Goals

1. §1 gains "Extended token catalog" subsection listing every `StudioRackBlock` field w/ role description.
2. §1.5 extended w/ motion catalog — each entry maps semantic name to duration + easing intent.
3. Token names verbatim match `UiTheme.cs` field identifiers.
4. §2 Components anchor link to new catalog subsections.
5. `validate:all` green (IA index regeneration picks up new sections).

### 2.2 Non-Goals

1. Schema definitions — TECH-309 / TECH-310.
2. Asset default values — TECH-311.
3. Glossary rows — TECH-313.
4. Primitive / studio-control / juice catalog (§2 extension) — Step 6.1 T6.1.2.

## 4. Current State

### 4.2 Systems map

Domain: **UI changes** (router). `ia/specs/ui-design-system.md` is the authoritative reference spec for `UiTheme`. §1 Foundations covers palette / typography / spacing today; §1.5 covers motion foundations. Extension is additive subsections — existing content untouched. Source of record for values + semantic intent: `docs/ui-polish-exploration.md` §Design Expansion + §CityStats handoff table.

## 7. Implementation Plan

### Phase 1 — §1 studio-rack catalog

- [ ] Append "Extended token catalog — studio-rack" subsection under §1. Markdown table w/ columns: Token name, Role, Type. One row per `StudioRackBlock` field (10 rows).
- [ ] Link back to `UiTheme.cs` path + exploration §Design Expansion as source of record.

### Phase 2 — §1.5 motion catalog + §2 anchor

- [ ] Under §1.5 append "Motion token catalog" subsection. Table: Token name, Semantic role, Default duration, Easing shape. Six rows matching `MotionBlock` entries.
- [ ] Add §2 cross-link pointing primitives-to-tokens mapping at the catalog anchors.
- [ ] `npm run validate:dead-project-specs` green; `npm run validate:all` green.

## 8. Acceptance Criteria

- [ ] §1 carries studio-rack catalog subsection w/ 10 normative rows matching `StudioRackBlock` fields by name.
- [ ] §1.5 carries motion catalog subsection w/ 6 normative rows matching `MotionBlock` entries by name.
- [ ] §2 anchor link present.
- [ ] `npm run test:ia` green (spec-index regenerate picks up new section ids).
- [ ] `npm run validate:all` green.

## §Plan Author

### §Audit Notes

- Risk: spec table drift vs `UiTheme.cs` field renames — `test:ia` may not catch spelling mismatch. Mitigation: char-for-char compare token column to C# identifiers before merge.
- Risk: §2 anchor link breaks if heading slug changes. Mitigation: use stable heading text from existing §2 structure; run link check if available.
- Ambiguity: `ui-design-system §7.1` vs §1 extensions — orchestrator says §1 + §1.5; keep motion catalog under §1.5 per spec summary.
- Invariant touch: terminology-consistency — new prose uses same tokens as TECH-313 will define in glossary.

### §Examples

| Token name in spec | Must match C# |
|--------------------|---------------|
| `oscilloscopeGlowColor` | `StudioRackBlock.oscilloscopeGlowColor` |
| `needleRelease` | `MotionBlock.needleRelease` |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_ia | edited spec | `npm run test:ia` exit 0 | node |
| validate_all | repo | `npm run validate:all` exit 0 | node |
| dead_specs | repo | `npm run validate:dead-project-specs` exit 0 | node |

### §Acceptance

- [ ] §1 subsection lists all 10 `StudioRackBlock` fields with roles.
- [ ] §1.5 lists all six `MotionBlock` entries with semantic + duration/easing intent.
- [ ] §2 link to catalog present.
- [ ] `test:ia` + `validate:all` green.

### §Findings

- **Primitives** catalog in §2 extension is explicitly out of scope — do not expand §2 beyond anchor link in this task.

## Open Questions

1. None — tooling / reference-spec authoring only.
