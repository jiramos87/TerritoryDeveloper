---
purpose: "TECH-750 — GET by id joined shape for catalog assets API."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.2
---
# TECH-750 — GET by id joined shape

> **Issue:** [TECH-750](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

`GET /api/catalog/assets/:id` under `web/app/api/catalog/assets/[id]/route.ts`. Return asset + economy + sprite slot join as documented DTO shape (TECH-626). Stable JSON key naming; document in spec §7. 404 on missing or retired-not-found per HTTP error contract (TECH-751).

## 2. Goals and Non-Goals

**Goals:** Joined DTO matches TECH-626 composite shape. 404 when asset missing.

**Non-Goals:** Auth. Admin-only fields.

## 3. Acceptance Criteria

- Joined DTO matches TECH-626 composite shape.
- 404 when asset missing; document retire visibility (hidden by default).
- Integration smoke: curl returns expected keys for seeded Zone S id.
