---
purpose: "TECH-328 — Implement backlog_list MCP tool."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-328 — Implement `backlog_list` MCP tool

> **Issue:** [TECH-328](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Ship `backlog_list` MCP tool (IP4) — structured filter queries across backlog records. Replaces ad-hoc Grep patterns agents use to enumerate "all open TECH issues in section X". Opens Phase 3 of Stage 1.2. Consumes the `priority` field added to `ParsedBacklogIssue` in Stage 1.1 (TECH-295).

## 2. Goals and Non-Goals

### 2.1 Goals

1. New file `tools/mcp-ia-server/src/tools/backlog-list.ts`.
2. Input schema: `{ section?: string, priority?: string, type?: string, status?: string, scope?: "open" | "archive" | "all" }` — `scope` defaults to `"open"`.
3. Load records via `parseAllBacklogIssues` (existing helper).
4. Filter in-memory — exact match on each provided field (case-insensitive for `priority` / `type`).
5. Return `{ issues: ParsedBacklogIssue[], total: number }` ordered by numeric id descending within prefix, prefix-stable.
6. Register in `tools/mcp-ia-server/src/index.ts`.

### 2.2 Non-Goals

1. Full-text search — that's `backlog_search`.
2. Date-range filters — IP9 / Stage 2.3.
3. Pagination — defer (small N today).
4. Cross-field `OR` queries — all filters `AND`.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Orchestrator agent | As `/release-rollout`, want `{ type: "TECH", status: "open", section: "Blip audio program" }` list so I skip Grep + parse. | Tool returns expected subset. |

## 4. Current State

### 4.1 Domain behavior

Agents Grep `BACKLOG.md` headers + manually sift. `backlog_search` covers keyword scoring but no structured filters. Enumerate-by-field is a gap.

### 4.2 Systems map

- `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts` — `parseAllBacklogIssues`.
- `tools/mcp-ia-server/src/tools/backlog-search.ts` — neighbor pattern.
- `ParsedBacklogIssue` shape — TECH-295 extended it.

## 5. Proposed Design

### 5.2 Architecture

- Handler: load all records (open + archive conditionally by scope); apply filters; sort; return.
- Sort: group by prefix alphabetic, within prefix numeric desc.
- Empty result → `{ issues: [], total: 0 }`, never error.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | AND semantics across filters | Matches SQL-like mental model | OR — rejected, too loose |
| 2026-04-17 | Prefix-stable sort | Keeps `TECH-329` above `FEAT-53` for agent readability | Pure numeric — rejected, cross-prefix collisions read weird |

## 7. Implementation Plan

### Phase 1 — Handler + registry

- [ ] Author `backlog-list.ts`.
- [ ] Register in registry.
- [ ] Smoke test handler direct (happy path).
- [ ] `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Handler + registry | Node | `npm run validate:all` | MCP boot |
| Fixture coverage | Follow-up | TECH-329 | Dedicated test |

## 8. Acceptance Criteria

- [ ] Tool file authored + registered.
- [ ] Smoke call w/ no filters returns all open records.
- [ ] Smoke call w/ each filter returns non-empty subset for a known-matching fixture.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions

1. None — tooling only.
