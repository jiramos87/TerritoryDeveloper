### Stage 11 — MCP reverse-lookup tooling / `backlog_list` filter extensions + catalog docs

**Status:** In Progress

**Objectives:** Extend `backlog_list` with three locator-field filters (`parent_plan`, `stage`, `task_key`). Lowercase substring compare per N3 (matches existing filter pattern). Document the three new tools (`master_plan_locate`, `master_plan_next_pending`, `backlog_list`-extended) in `docs/mcp-ia-server.md` + update `CLAUDE.md` §2 MCP-first ordering (additive only).

**Exit:**

- `backlog_list` accepts `parent_plan?`, `stage?`, `task_key?` as optional inputs. Filters applied in-memory via lowercase substring compare.
- Tests extended under `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — each new filter + multi-filter intersection + empty result + scope switch with new filters.
- `docs/mcp-ia-server.md` carries catalog entries for `master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate` (from Step 3), + notes the `backlog_list` filter extensions.
- `CLAUDE.md` §2 MCP-first ordering — `master_plan_locate` added to single-issue lookup flows; `master_plan_next_pending` added to `/ship` suggested order; additive only (no rewrite).
- Phase 1 — `backlog_list` filter extensions + tests.
- Phase 2 — Tool catalog + CLAUDE ordering updates.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Extend `backlog_list` inputs | **TECH-438** | Draft | In `tools/mcp-ia-server/src/tools/backlog-list.ts`, add optional input fields `parent_plan?`, `stage?`, `task_key?`. Apply filters after existing `section`/`priority`/`type`/`status`/`scope` filters (in-memory, lowercase substring compare per N3). Preserve id-desc ordering. Update tool descriptor. |
| T11.2 | Test `backlog_list` locator filters | **TECH-439** | Draft | Extend `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` fixture set to cover schema-v2 records across ≥2 plans + ≥2 stages. Assert: each new filter alone, multi-filter intersection with existing priority/type filters, empty result, scope switch. |
| T11.3 | Document new tools in `docs/mcp-ia-server.md` | **TECH-440** | Draft | Add catalog entries for `master_plan_locate` (from Stage 4.1), `master_plan_next_pending` (from Stage 4.1), `parent_plan_validate` (from Step 3 Stage 3.3). Append filter-extension note to existing `backlog_list` entry (3 new filters). Preserve catalog ordering + existing entries. |
| T11.4 | Update `CLAUDE.md` §2 MCP-first ordering | **TECH-441** | Draft | Edit `CLAUDE.md` §2 "MCP first" — append: `master_plan_locate` for issue→plan reverse lookup; `master_plan_next_pending` for `/ship` next-task; note `parent_plan_validate` runs in advisory mode during `validate:all`. Additive edits only — do not rewrite existing ordering. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 11 Task specs (TECH-438..441) aligned w/ Stage block + §Plan Author + backlog yaml locator fields. Orchestrator table uses T11.x labels; yaml carries T4.2.x `task_key` — specs mirror yaml for MCP. No fix tuples. Downstream: `/ship-stage` Pass 1 per task.

---
