---
purpose: "TECH-751 — HTTP error contract shared helper for catalog routes."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T1.3.3
---
# TECH-751 — HTTP error contract

> **Issue:** [TECH-751](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Map DB / validation errors to 400 / 404 / 409 with structured JSON body `{error, code, detail?}` consistent with other `web/app/api/*` routes. Log server-side stack; never return stack to client. Shared helper under `web/lib/api/errors.ts` (or project-standard location). Used by read routes (TECH-749/750) + write routes (TECH-752/753/754).

## 2. Goals and Non-Goals

**Goals:** One helper invoked from every catalog route. Codes + JSON shape match existing conventions.

**Non-Goals:** Global error boundary. Auth errors.

## 3. Acceptance Criteria

- One helper invoked from every catalog route.
- Codes + JSON shape match existing `web/app/api/*` conventions.
- No raw error bodies or stack traces leaked to client.
