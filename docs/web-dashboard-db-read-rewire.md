# Web Dashboard DB-Read Rewire — Implementation Plan

**Status:** complete (2026-04-25, journal entry id=10)
**Owner:** `feature/skill-files-audit` branch
**Scope:** READ-only. CRUD writes deferred to future iteration.

---

## 1. Problem

Post `feature/ia-dev-db-refactor` merge (Step 9.6 deleted all `ia/projects/*master-plan*.md` flat files), the CD `/dashboard` page reads filesystem via `web/lib/plan-loader.ts → loadAllPlansFromFilesystem()` and returns `[]` → renders "No plans match the current filters."

Step 10 of the refactor added a parallel `/ia` route + DB-backed `lib/ia/queries.ts` but did NOT migrate `/dashboard`. Gap was unfiled — TECH-876 covers MCP-transport revisit, not this rewire.

DB has 20 master plans, 877 tasks intact. Backend is healthy; the dashboard reads the wrong source.

## 2. Goal

Wire `/dashboard` to a dedicated DB-backed backend endpoint. No filesystem, no GitHub-raw, no markdown parse on render path. Frontend renders only. Backend queries DB + maps to existing `PlanData` shape so existing chrome (Bezel / HeatCell / Sparkline / StatBar / CollapsiblePlanStage) keeps rendering unchanged.

## 3. Architecture

```
/dashboard RSC (page.tsx)
   │
   ▼ loadAllPlans()
   │
web/lib/plan-loader.ts   ── thin wrapper
   │
   ▼ fetch(/api/ia/dashboard)  OR  direct lib call
   │
web/app/api/ia/dashboard/route.ts   ── HTTP boundary
   │
   ▼ loadDashboardData()
   │
web/lib/ia/dashboard-data.ts   ── DB query + DB→PlanData mapping
   │
   ▼ sql tagged template
   │
ia_master_plans, ia_stages, ia_tasks
```

RSC calls the lib function directly (same process — avoids HTTP roundtrip overhead). Endpoint exists for external/client consumers + future API expansion. Both share the same `loadDashboardData()` source.

## 4. Data shape mapping

### 4.1 PlanData shape (target — kept verbatim from `web/lib/plan-loader-types.ts`)

```ts
interface PlanData {
  title: string;
  filename: string;
  overallStatus: string;
  overallStatusDetail: string;
  siblingWarnings: string[];
  stages: Stage[];
  allTasks: TaskRow[];
}
```

### 4.2 DB → PlanData

| Target field | Source |
|---|---|
| `PlanData.title` | `cleanPlanTitle(ia_master_plans.title)` (existing util) |
| `PlanData.filename` | `${slug}-master-plan.md` (synth — for legacy callers + React keys) |
| `PlanData.overallStatus` | derived: all-done → `'Final'`, any-active → `'In Progress'`, else → `'Draft'` |
| `PlanData.overallStatusDetail` | `''` |
| `PlanData.siblingWarnings` | `[]` (DB has no sibling-orchestrator markers; deferred) |
| `Stage.id` | `ia_stages.stage_id` |
| `Stage.title` | `ia_stages.title` |
| `Stage.status` | mapped (see 4.3); `deriveHierarchyStatus` later overrides from tasks anyway |
| `Stage.statusDetail` | `''` |
| `Stage.objective` | `ia_stages.objective` |
| `Stage.tasks` | rows from `ia_tasks` joined by `(slug, stage_id)` |
| `TaskRow.id` | `ia_tasks.task_id` |
| `TaskRow.issue` | `ia_tasks.task_id` (same value — UI shows in both ID + Issue columns) |
| `TaskRow.status` | mapped (see 4.3) |
| `TaskRow.intent` | `ia_tasks.title` |

### 4.3 Status enum mapping

Task (`task_status` enum → `TaskStatus`):

| DB | Loader |
|---|---|
| `pending` | `_pending_` |
| `implemented` | `In Progress` |
| `verified` | `In Progress` |
| `done` | `Done` |
| `archived` | `Done (archived)` |

Stage (`stage_status` enum → `HierarchyStatus`):

| DB | Loader |
|---|---|
| `pending` | `Draft` |
| `in_progress` | `In Progress` |
| `done` | `Final` |

## 5. Mechanical steps

1. **Env** — append `DATABASE_URL=postgresql://postgres:postgres@localhost:5434/territory_ia_dev` to `web/.env.local`. Mirror in `.env.local.example`.
2. **Backend lib** — write `web/lib/ia/dashboard-data.ts`:
   - exports `loadDashboardData(): Promise<PlanData[]>`
   - 3 batched queries (master plans, stages, tasks) + JS stitch (avoids N+1)
   - status mapping helpers `mapTaskStatus(db) → TaskStatus`, `mapStageStatus(db) → HierarchyStatus`, `synthOverallStatus(tasks) → string`
3. **Backend endpoint** — write `web/app/api/ia/dashboard/route.ts`:
   - GET handler → `loadDashboardData()` → JSON envelope
   - error handling via existing `iaJsonError` / `postgresErrorResponse` helpers
4. **Loader rewrite** — `web/lib/plan-loader.ts`:
   - replace `loadAllPlans()` body with `loadDashboardData()` call
   - delete `loadAllPlansFromFilesystem`, `loadAllPlansFromGitHub`, `githubRef`, `resolveRepoRoot`, `GH_*` constants, `REVALIDATE_SECONDS`, all FS/GitHub imports
5. **Dashboard page** — no edit required (`page.tsx` already calls `loadAllPlans()`)
6. **Plan-parser** — keep `cleanPlanTitle`, `computePlanMetrics`, `isTaskDone/Active/Pending`, `deriveHierarchyStatus`. Drop nothing — utilities still serve dashboard render.
7. **Verify** — see §6.

## 6. Verification (direct testing)

```bash
# A. Endpoint smoke
curl -s http://localhost:4000/api/ia/dashboard | jq 'length'   # expect ~20

# B. Page render — count plan racks
curl -s http://localhost:4000/dashboard | grep -c 'Master plan'   # expect ≥1 per plan

# C. Status code on broken DB (negative test)
DATABASE_URL=postgresql://wrong curl -s http://localhost:4000/api/ia/dashboard
# expect {error,code:'internal'}

# D. Build
cd web && npm run build   # expect exit 0
```

Browser walk-through:
- visit `/dashboard` → plan racks render, stage rows expand on click, task tables show issue + status chips
- click plan filter chip → narrows visible plans
- click status filter chip → narrows visible tasks within each plan

## 7. Ticket / stage updates (post-verify)

**Discovered during execution:**
- TECH-876 does not exist in BACKLOG — drafted reference was speculative. No update applied. MCP-bridge transport revisit, if needed, lives in a future filed issue.
- web-platform Stages 6/7/8 are **placeholder stages with zero filed tasks** (counts confirm `pending=0, implemented=0, verified=0, done=0, archived=0` via `master_plan_state`). Original FS-based intent was never decomposed into tasks pre-refactor; the rewire here delivers the equivalent surface using DB reads.

**Action taken:**
- `journal_append` MCP entry recorded with `payload_kind: rewire_supersede`, slug `web-platform`, payload pointing at `docs/web-dashboard-db-read-rewire.md` — audit trail that Stages 6/7/8 are functionally superseded.
- Stage status enum left at `pending` (no task rows to flip; stage status currently follows from task aggregation in DB schema).
- Future cleanup: when web-platform master plan is next revised, drop Stages 6/7/8 or rewrite their objectives to reference this doc.

## 8. Rollback

If broken:
- revert this branch's three new files (`dashboard-data.ts`, `route.ts`, doc)
- restore prior `plan-loader.ts` from `git show HEAD~1:web/lib/plan-loader.ts`
- remove `DATABASE_URL` line from `web/.env.local`

Pre-rewire dashboard rendered "No plans" but did not crash — rollback safe.

## 9. Out of scope (future work)

- CRUD writes from web (status flips, body edits) — IA writes go through MCP tools by design
- MCP-bridge transport (TECH-876) — direct pg is the chosen architectural deviation
- `/ia` route consolidation with `/dashboard` — separate UX surfaces, keep both
- Real-time updates / SSE — current page is request-time render with `force-dynamic`

---
