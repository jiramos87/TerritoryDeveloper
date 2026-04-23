---
purpose: "TECH-753 — PATCH optimistic lock for catalog assets API."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.5
---
# TECH-753 — PATCH optimistic lock

> **Issue:** [TECH-753](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

`PATCH /api/catalog/assets/:id` compares client-provided `updated_at`; bumps column on success; returns 409 with fresh row body on stale lock. Uses TECH-751 error helper. Covers write to `catalog_asset` + `catalog_economy` scope.

## 2. Goals and Non-Goals

**Goals:** Optimistic lock via `updated_at` comparison. 409 + fresh payload on stale lock.

**Non-Goals:** Pessimistic locking. Partial field patch.

## 3. Acceptance Criteria

- `UPDATE ... WHERE id = :id AND updated_at = :client_updated_at`.
- Row count 0 → 409 + fresh GET payload.
- Row count 1 → 200 with updated DTO.
- Integration test: simulate stale client + fresh client.
