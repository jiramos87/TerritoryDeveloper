---
purpose: "TECH-490 — MCP restart + schema verify."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.2"
---
# TECH-490 — MCP restart + schema verify

> **Issue:** [TECH-490](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Restart `territory-ia` MCP server post-merge so new schema (plan-apply-pair-contract enums, retired tools) is live.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Fresh MCP process on merged main branch.
2. Confirm schema includes plan_review enum + plan_apply_validate tool.

### 2.2 Non-Goals (Out of Scope)

1. Schema edits (frozen post-merge).
2. Tool catalog rewrite.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want MCP schema refreshed post-merge so that new lifecycle enums are visible to agents. | `router_for_task` returns ok for `plan_review` lifecycle stage. |

## 4. Current State

### 4.1 Domain behavior

MCP server may be running on pre-merge branch schema; restart needed after merge lands.

### 4.2 Systems map

- `.mcp.json` — MCP registration.
- `tools/mcp-ia-server/src/index.ts` — entrypoint.
- `ia/state/lifecycle-refactor-migration.json` — restart log target.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Kill existing `territory-ia` MCP process; respawn from merged main; send smoke-test tool calls; log success.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Use `pkill`/`kill` on MCP PID; relaunch via `tsx tools/mcp-ia-server/src/index.ts`; send test tool calls via MCP client.

### 5.3 Method / algorithm notes (optional)

None.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Restart post-merge before any agent invocations | Schema must match merged code | Hot-reload — not supported by MCP server design |

## 7. Implementation Plan

### Phase 1 — Restart MCP

- [ ] Kill existing `territory-ia` MCP process.
- [ ] Respawn process on post-merge main.
- [ ] Log PID to migration JSON.

### Phase 2 — Schema smoke test

- [ ] Send `router_for_task` with `lifecycle_stage: plan_review`; confirm ok.
- [ ] Confirm `plan_apply_validate` tool discoverable + responsive.
- [ ] Write restart success entry + timestamp to migration JSON.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP schema reflects merged main | MCP call | `router_for_task` + `plan_apply_validate` | Fail = schema stale |

## 8. Acceptance Criteria

- [ ] MCP process respawned on post-merge main (PID logged).
- [ ] `router_for_task` with `lifecycle_stage: plan_review` returns ok.
- [ ] `plan_apply_validate` tool discoverable + responsive.
- [ ] Migration JSON entry records restart success + timestamp.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- None yet.

## §Plan Author

### §Audit Notes

- Risk: stale MCP schema served to next agent session (old enum set, retired tools still listed). Mitigation: Phase 1 kill-before-respawn; Phase 2 smoke-test covers both `plan_review` enum + `plan_apply_validate` tool.
- Risk: Claude Code host still holds old MCP session handle after respawn → schema cache stale. Mitigation: document requirement to restart Claude Code host (or re-open MCP connection) after MCP process respawn; log note in migration JSON.
- Risk: PID reuse across kill/spawn → log ambiguity. Mitigation: log both old PID (pre-kill) + new PID (post-spawn) + respawn timestamp.
- Ambiguity: does "respawn" mean daemonized process or foreground-via-Claude-Code auto-start. Resolution: MCP server is spawned by Claude Code host per `.mcp.json`; restart = kill process + restart Claude Code (or equivalent host reconnect).
- Invariant touch: MCP schema-cache caveat (`CLAUDE.md` §2) — edits to tool descriptors require restart; this task is the explicit restart point.

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Pre-merge MCP running with pre-refactor schema | New MCP process with post-merge schema, old PID logged + killed | Happy path. |
| `router_for_task` with `lifecycle_stage: plan_review` | Returns ok (no enum rejection) | Validates plan-apply-pair-contract enum landed. |
| `plan_apply_validate` tool discovery | Tool present in MCP tool list | Validates new validator tool shipped. |
| `router_for_task` with retired enum (e.g. `spec_enrich`) | Returns enum rejection error | Validates retired stages absent. |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| mcp_process_respawned | Old PID killed, new PID logged | Migration JSON records both PIDs + timestamp | manual |
| plan_review_enum_ok | `router_for_task` with `lifecycle_stage: plan_review` | Response `{status: ok}`, no enum error | bridge |
| plan_apply_validate_present | MCP tool list query | `plan_apply_validate` in tool list | bridge |
| retired_enum_rejected | `router_for_task` with `lifecycle_stage: spec_enrich` | Zod invalid_enum_value error | bridge |
| schema_restart_logged | Migration JSON post-task | Contains restart row with old_pid + new_pid + signed_at | manual |

### §Acceptance

- [ ] Old MCP PID recorded in migration JSON before kill.
- [ ] New MCP PID recorded post-respawn.
- [ ] `router_for_task` with `lifecycle_stage: plan_review` returns ok.
- [ ] `plan_apply_validate` tool discoverable.
- [ ] Retired enum (e.g. `spec_enrich`) rejected.
- [ ] Claude Code host restart note recorded (schema-cache caveat).
- [ ] Restart row in migration JSON with ISO8601 timestamp.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
