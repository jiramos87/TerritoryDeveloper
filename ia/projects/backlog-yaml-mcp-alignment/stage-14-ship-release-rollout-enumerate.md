### Stage 14 — Skill patches + plan consumers / Dispatcher consumers (`/ship`, `release-rollout-enumerate`)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Swap scan-driven next-task + rollout-enumerate paths for direct MCP / yaml reads. `/ship` next-task uses `master_plan_next_pending`. `release-rollout-enumerate` reads yaml `parent_plan` + `task_key` directly instead of inferring from plan scans. Fallbacks kept; next-task behavior stays aligned with `.claude/commands/ship.md` (Next-handoff resolver — master-plan scan, no BACKLOG numeric adjacency).

**Exit:**

- `/ship` dispatcher (`.claude/commands/ship.md` or equivalent) — next-task-lookup step calls `master_plan_next_pending {plan, stage?}` first; plan-scan fallback kept.
- `ia/skills/release-rollout-enumerate/SKILL.md` — per-row data pull reads yaml `parent_plan` + `task_key` + `stage` directly via `backlog_list parent_plan=`; inference fallback noted.
- Rehearsal fixture proves one full `/project-new → /author → /implement → /closeout` cycle on schema-v2 yaml with MCP happy path + no scan fallbacks triggered (post-M6 flow; `/kickoff` retired, replaced by `/author` (`plan-author` Stage 1×N)).
- Canonical next-task behavior documented in `.claude/commands/ship.md` — or note added in skill that MCP path supersedes scan guidance where applicable.
- Phase 1 — `/ship` dispatcher wiring.
- Phase 2 — `release-rollout-enumerate` + end-to-end rehearsal.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | Verify `/ship` dispatcher surface | _pending_ | _pending_ | Glob for `/ship` dispatcher path — likely `.claude/commands/ship.md` OR a `/ship`-named skill under `ia/skills/`. Read + document the canonical surface in the spec. Do NOT guess; `stage-file` kicked this off via user-memory hint but dispatcher wiring may live in a different surface. |
| T14.2 | Wire `master_plan_next_pending` into `/ship` | _pending_ | _pending_ | Patch the dispatcher from T5.3.1 — next-task-lookup step calls `master_plan_next_pending {plan, stage?}` first; scan fallback kept with "if MCP unavailable" clause. Caveman body prose. |
| T14.3 | Wire `release-rollout-enumerate` to yaml direct | _pending_ | _pending_ | Edit `ia/skills/release-rollout-enumerate/SKILL.md` per-row-enumeration step — read `parent_plan` + `task_key` + `stage` directly from yaml via `backlog_list parent_plan=` (extended filter from Stage 4.2); inference-from-plan-scan fallback kept as "if yaml missing fields" clause. |
| T14.4 | End-to-end rehearsal fixture + note | _pending_ | _pending_ | Document in `docs/parent-plan-locator-fields-exploration.md` (append section) OR in this master plan's Acceptance section: one full `/project-new → /author → /implement → /closeout` cycle on fixture yaml with all MCP happy-path calls succeeding + zero fallback triggers (post-M6 flow). Rehearsal = manual; documentation = written evidence, not automated test. |

---
