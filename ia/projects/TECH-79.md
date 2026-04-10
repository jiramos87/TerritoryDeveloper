# TECH-79 — Agent memory across sessions (persistent agent context)

> **Issue:** [TECH-79](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Track which MCP tools, spec sections, glossary terms, and invariants agents use per issue — persisted in Postgres — so the system can recommend relevant context to future agents working on similar issues. Transforms the IA system from stateless retrieval into a learning recommendation engine.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Postgres table `agent_session_log` recording MCP tool calls per issue, with parameters and usefulness signals
2. MCP tool `ia_recommend(issue_id?, domain?, files?)` that uses historical session data to recommend spec sections, glossary terms, and tool sequences
3. Per-issue-type patterns: "agents working on road issues typically need geo §10, §13, roads-system, invariant #10"
4. Efficiency metrics: average MCP calls per issue type, most-accessed spec sections

### 2.2 Non-Goals (Out of Scope)

1. Tracking agent reasoning or conversation content (only tool calls and parameters)
2. Replacing the skill system or router_for_task
3. Cross-project memory (scoped to this repository)
4. Privacy-sensitive data collection (only tool names, spec keys, glossary terms)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent starting on a water/terrain bug, I want to know which spec sections previous agents found useful for similar issues | `ia_recommend(domain: "water")` returns "agents on water issues used geo §11, §12, §5, water-terrain §Shore, invariant #7 (shore band), glossary 'Water body', 'Shore band'" |
| 2 | AI agent | As an agent working on FEAT-43, I want recommendations based on what agents did on related issues (FEAT-36, BUG-52) | `ia_recommend(issue_id: "FEAT-43")` checks Depends on and Related issues, returns their session patterns |
| 3 | IA maintainer | As a system maintainer, I want to see which spec sections are never accessed (candidates for cleanup or improvement) | Query `agent_session_log` for access frequency per spec section |
| 4 | Developer | As a developer tuning the IA, I want to know the average number of MCP calls agents make before starting code edits | Aggregate query on session logs |

## 4. Current State

### 4.1 Domain behavior

Each agent session starts fresh. MCP tools are stateless — they don't record which tools were called, in what order, or which results were useful. The `ia_project_spec_journal` records Decision Log and Lessons Learned text but not the tool interaction patterns that produced them.

### 4.2 Systems map

- `tools/mcp-ia-server/src/index.ts` — all tool registrations (instrumentation hook point)
- `tools/mcp-ia-server/src/instrumentation.ts` — per-tool timing (existing pattern)
- `tools/mcp-ia-server/src/ia-db/` — Postgres pool and existing table patterns
- `db/migrations/` — migration infrastructure

## 5. Proposed Design

### 5.1 Target behavior (product)

**Recording (automatic, transparent):**

Every MCP tool call is logged with: tool name, key parameters (issue_id, spec key, section, glossary term, domain), timestamp, and a session identifier. Logging is fire-and-forget — never blocks the tool response.

**Example session log entries:**

```
session: "abc123", tool: "backlog_issue", params: { issue_id: "FEAT-43" }, ts: ...
session: "abc123", tool: "router_for_task", params: { domain: "simulation" }, ts: ...
session: "abc123", tool: "spec_section", params: { spec: "sim", section: "Rings" }, ts: ...
session: "abc123", tool: "glossary_lookup", params: { term: "Urban growth rings" }, ts: ...
```

**Recommending:**

```
ia_recommend({ domain: "simulation" })
→ {
    recommended_spec_sections: [
      { spec: "sim", section: "Tick execution order", access_count: 12 },
      { spec: "sim", section: "Rings", access_count: 9 },
      { spec: "mgrs", section: "Demand", access_count: 7 }
    ],
    recommended_glossary: ["Urban centroid", "Growth budget", "Simulation tick"],
    recommended_invariants: [2, 6],
    typical_tool_sequence: ["backlog_issue", "invariants_summary", "router_for_task", "spec_section", "glossary_discover"],
    based_on_sessions: 15
  }
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: session identification strategy, logging overhead minimization, recommendation algorithm (frequency-based vs collaborative filtering), privacy (no conversation content stored).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Log tool calls only, not conversation content | Privacy, storage efficiency, and tool calls are the actionable signal | Full conversation logging; embedding-based similarity |
| 2026-04-07 | Postgres table over file-based logging | Enables SQL aggregation and joins with existing journal data | JSONL file per session; SQLite |

## 7. Implementation Plan

### Phase 1 — Session logging

- [ ] Design `agent_session_log` table
- [ ] Add transparent logging middleware to MCP tool dispatch
- [ ] Migration script

### Phase 2 — Recommendation tool

- [ ] Register `ia_recommend` MCP tool
- [ ] Implement frequency-based recommendation from session logs
- [ ] Tests and documentation

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP tools still functional with logging enabled | Node | `npm run verify` + `npm run test:ia` | Repo root |
| Logging does not block tool responses | Node | Timing test: tool response time with/without logging | Part of test suite |

## 8. Acceptance Criteria

- [ ] `agent_session_log` table created via migration
- [ ] All MCP tool calls transparently logged with tool name, key parameters, session id, timestamp
- [ ] `ia_recommend` MCP tool registered and documented
- [ ] Recommendations based on historical session data for a given domain or issue_id
- [ ] Graceful degradation when Postgres unavailable (logging skipped, recommend returns empty)
- [ ] `npm run verify` and `npm run test:ia` green

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
