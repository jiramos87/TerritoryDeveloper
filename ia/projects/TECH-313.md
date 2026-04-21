---
purpose: "TECH-313 — glossary rows for UiTheme token ring, studio-rack token, motion token."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/ui-polish-master-plan.md"
task_key: "T1.1.5"
---
# TECH-313 — Glossary rows (UiTheme token ring / Studio-rack token / Motion token)

> **Issue:** [TECH-313](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-21

## 1. Summary

Add three rows to `ia/specs/glossary.md`:

- `UiTheme token ring` — extended token catalog under `UiTheme` ScriptableObject covering surface / accent / studio-rack / motion blocks.
- `Studio-rack token` — LED / VU / knob / fader / oscilloscope visual params (rack-inspired visual language).
- `Motion token` — semantic named duration + easing curve entry under `UiTheme.motion`.

Each row cites `ia/specs/ui-design-system.md §1` / §1.5 (TECH-312). Satisfies terminology-consistency rule "new domain term → add a glossary row AND update the authoritative spec section" and Stage 1.1 Exit "Glossary rows added: UiTheme token ring, Studio-rack token, Motion token".

## 2. Goals and Non-Goals

### 2.1 Goals

1. Three glossary rows present w/ concise definitions + spec references pointing at `ui-design-system §1` / §1.5.
2. Terminology stays consistent w/ TECH-309 + TECH-310 field names + TECH-312 spec catalog.
3. `test:ia` green — glossary-index regenerate picks up new terms w/o duplicates.
4. `validate:all` green.

### 2.2 Non-Goals

1. Schema / asset / spec edits — TECH-309 / TECH-310 / TECH-311 / TECH-312.
2. Primitive / studio-control / juice glossary rows — Steps 2–4 own those.

## 4. Current State

### 4.2 Systems map

Domain: **Documentation / glossary**. `ia/specs/glossary.md` lists canonical domain terms — authoritative when referenced from code, specs, rules. Terminology-consistency rule: glossary row + spec section must both carry the term; spec wins on conflict. Upstream: TECH-312 lands §1 + §1.5 catalog; this issue adds glossary rows citing those sections.

## 7. Implementation Plan

### Phase 1 — Author + validate

- [ ] Locate correct category section in `ia/specs/glossary.md` (UI / theme neighborhood).
- [ ] Add three rows w/ canonical table format (Term | Definition | Spec reference).
- [ ] Cite `ui-design-system §1` / §1.5 per row.
- [ ] `npm run test:ia` + `npm run validate:dead-project-specs` green.
- [ ] `npm run validate:all` green (glossary-index regen picks up rows).

## 8. Acceptance Criteria

- [ ] Three rows present w/ exact terms above.
- [ ] Each row carries non-empty definition + spec reference.
- [ ] No duplicate term rows.
- [ ] `npm run test:ia` green.
- [ ] `npm run validate:all` green.

## §Plan Author

### §Audit Notes

- Risk: duplicate glossary rows if term already exists under UI section. Mitigation: `grep` `ia/specs/glossary.md` for `UiTheme token ring` / `Studio-rack token` / `Motion token` before add.
- Risk: wrong category bucket breaks alphabetization or IA index. Mitigation: place next to existing UI/theme rows; follow table format in file.
- Ambiguity: spec reference line must point to post-TECH-312 anchors. Resolution: land TECH-312 before TECH-313 if strict ordering required; otherwise cite section headings that TECH-312 will add.
- Invariant touch: glossary English definitions; caveman elsewhere per rules.

### §Examples

| Term | Definition must cite |
|------|---------------------|
| UiTheme token ring | `ui-design-system.md` §1 extended catalog |
| Motion token | §1.5 motion catalog |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_ia | glossary edited | `npm run test:ia` exit 0 | node |
| validate_all | repo | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] Three rows with exact canonical term spellings from §1 Summary.
- [ ] Each row has definition + spec pointer.
- [ ] No duplicate terms; `test:ia` + `validate:all` green.

### §Findings

- If TECH-312 renames a heading, update glossary spec refs in same PR chain.

## Open Questions

1. None — tooling / glossary authoring only.
