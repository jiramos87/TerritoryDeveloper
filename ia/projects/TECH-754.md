---
purpose: "TECH-754 — Retire + preview-diff routes for catalog assets API."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.6
---
# TECH-754 — Retire + preview-diff

> **Issue:** [TECH-754](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Two routes: `POST /api/catalog/assets/:id/retire` (sets `status=retired` + optional `replaced_by` PK); `POST /api/catalog/preview-diff` (returns human/agent-readable plan of intended changes without committing). Both use TECH-751 error helper. Preview-diff payload aligns with TECH-628 DTO.

## 2. Goals and Non-Goals

**Goals:** Retire: 200 on success; 404 on missing; 409 if `replaced_by` invalid. Preview-diff: no DB writes; deterministic output.

**Non-Goals:** Bulk retire. Undo/unretire.

## 3. Acceptance Criteria

- Retire: 200 on success; 404 on missing; 409 if `replaced_by` invalid.
- Preview-diff: no DB writes; deterministic output; shape matches TECH-628 DTO.
- Integration smoke: curl shows retire flag + preview list.
