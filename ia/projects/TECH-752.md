---
purpose: "TECH-752 — POST create transactional for catalog assets API."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.4
---
# TECH-752 — POST create transactional

> **Issue:** [TECH-752](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

`POST /api/catalog/assets` creates `catalog_asset` + `catalog_economy` + `catalog_asset_sprite` bindings in a single transaction; validate slot uniqueness server-side. Use `web/lib/db/` transactional pattern. Errors via TECH-751 helper.

## 2. Goals and Non-Goals

**Goals:** All-or-nothing write across three tables. Slot uniqueness check per asset before commit.

**Non-Goals:** Bulk create. External auth.

## 3. Acceptance Criteria

- All-or-nothing write across three tables.
- Slot uniqueness check per asset before commit.
- 400 on validation fail; 409 on unique conflict; 201 on success.
- Integration test / curl proves rollback on mid-tx fail.
