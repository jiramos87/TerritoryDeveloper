---
purpose: "TECH-107 — Glossary rows for neighbor-city stub + interstate border."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-107 — Glossary rows: `neighbor-city stub` + `interstate border`

> **Issue:** [TECH-107](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-13
> **Last updated:** 2026-04-13

## 1. Summary

Stage 1.3 Phase 3 closer (IA). Add canonical glossary rows for **neighbor-city stub** + **interstate border**. Aligns code vocabulary (`NeighborCityStub`, `BorderSide`) w/ `ia/specs/glossary.md` Multi-scale simulation + Roads & Bridges categories. Spec-authored doc-only issue.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Glossary row for **neighbor-city stub** — category Multi-scale simulation; cites `multi-scale-master-plan.md` + code file.
2. Glossary row for **interstate border** — category Roads & Bridges; cites geo §13.5 + **Interstate** + **Map border** (related).
3. Terminology consistency: no new synonyms; existing rows unaffected.

### 2.2 Non-Goals

1. Reference spec edits (nothing permanent-domain yet).
2. Rename existing terms.

## 4. Current State

### 4.2 Systems map

- `ia/specs/glossary.md` — single canonical glossary.
- Cross-refs: existing **Parent-scale stub** row cites master plan.
- Depends: TECH-102 (struct exists — row cites code). Soft TECH-104/105 (semantics lived out).
- Orchestrator: Stage 1.3.

## 5. Proposed Design

### 5.1 Target behavior

Docs-only. Glossary becomes authoritative source for both terms.

## 7. Implementation Plan

### Phase 1 — Rows

- [ ] Add **neighbor-city stub** row in Multi-scale simulation category.
- [ ] Add **interstate border** row in Roads & Bridges category.
- [ ] `validate:all` green (chains `test:ia` + indexes).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc-only rows land | Node | `npm run validate:all` | N/A test contracts beyond this |

## 8. Acceptance Criteria

- [ ] Both rows present + alphabetized within category.
- [ ] Cross-refs cite canonical sources (master plan / geo §13.5).
- [ ] `validate:all` green.

## Open Questions

1. None — tooling / IA only.
