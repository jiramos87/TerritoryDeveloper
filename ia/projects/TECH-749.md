---
purpose: "TECH-749 — GET list route + filters for catalog assets API."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.1
---
# TECH-749 — GET list route + filters

> **Issue:** [TECH-749](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

`GET /api/catalog/assets` under `web/app/api/catalog/assets/route.ts`. Default filter `status=published`; optional `?includeDraft=1` (or admin header) for draft visibility. Consume `web/types/api/catalog*.ts` DTOs (TECH-626, TECH-628). No Drizzle. Pagination params (`limit`/`cursor` or `offset`) if row count grows; document chosen pagination in spec §7.

## 2. Goals and Non-Goals

**Goals:** Route returns published rows by default; draft opt-in only. Response shape matches `CatalogAssetListResponse` DTO (TECH-628). Pagination contract documented.

**Non-Goals:** Admin UI. Auth middleware.

## 3. Acceptance Criteria

- Route returns `published` rows by default; `draft` opt-in only.
- Response shape matches `CatalogAssetListResponse` DTO (TECH-628).
- Pagination contract documented (even if MVP returns full list).
- `npm run validate:web` green; `npm run validate:all` green.
