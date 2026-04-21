---
purpose: "TECH-440 — Document `master_plan_locate` / `master_plan_next_pending` / `parent_plan_validate` + `backlog_list` filter ext in MCP catalog."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T4.2.3"
---
# TECH-440 — Document new MCP tools in `docs/mcp-ia-server.md`

> **Issue:** [TECH-440](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-21

## 1. Summary

Add catalog entries to `docs/mcp-ia-server.md` for the three new MCP tools shipped in Steps 3–4 (`parent_plan_validate`, `master_plan_locate`, `master_plan_next_pending`). Append filter-extension note to existing `backlog_list` catalog entry. Preserve existing catalog ordering. Satisfies Stage 4.2 Phase 2 exit of `backlog-yaml-mcp-alignment-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Three new catalog entries added — each carries input schema, output shape, canonical use case.
2. Schema-cache restart note (N4) referenced for newly registered tools.
3. Existing `backlog_list` entry gains filter-extension note (3 new inputs + lowercase substring behavior).
4. Existing catalog ordering + pre-existing entries untouched.

### 2.2 Non-Goals

1. Code changes — pure doc edit.
2. `CLAUDE.md` update — lives in sibling TECH-441.
3. Tool impl / tests — shipped in Stage 3.3 + Stage 4.1 + TECH-438/439.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent author | As an agent reading the MCP catalog for tool discovery, I want the three new tools listed w/ canonical use cases so I don't miss them when planning retrievals. | All three tools findable via catalog scan; prose states canonical use case. |

## 4. Current State

### 4.1 Domain behavior

`docs/mcp-ia-server.md` documents existing MCP tool catalog. Three tools shipped in Step 3 + Stage 4.1 have no catalog entry yet. `backlog_list` entry does not mention locator filters added in TECH-438.

### 4.2 Systems map

- `docs/mcp-ia-server.md` — target doc.
- `tools/mcp-ia-server/src/tools/parent-plan-validate.ts` / `master-plan-locate.ts` / `master-plan-next-pending.ts` — source of input/output schemas to mirror in catalog.
- `tools/mcp-ia-server/src/tools/backlog-list.ts` — filter-extension source.

## 5. Proposed Design

### 5.1 Target behavior

Reader scanning the catalog finds each new tool section-by-section w/ existing ordering conventions. `backlog_list` entry's "Inputs" block gains three new bullets.

### 5.2 Architecture / implementation

Implementer reads existing entries, mirrors style (heading level, input-table / list shape, "when to use" prose). No logic change.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | Three tools documented in one issue | Same surface file, same editing pass; split creates thrash | One issue per tool (rejected — cardinality explosion, identical file touch) |

## 7. Implementation Plan

### Phase 1 — Catalog additions

- [ ] Read existing `docs/mcp-ia-server.md` structure + ordering.
- [ ] Append catalog entries for `parent_plan_validate`, `master_plan_locate`, `master_plan_next_pending`.
- [ ] Append filter-ext note to existing `backlog_list` entry.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc chain green | Node | `npm run validate:all` | Doc validators only |

## 8. Acceptance Criteria

- [ ] Three new catalog entries present w/ input schema + output shape + use case.
- [ ] `backlog_list` entry gains 3-input filter note + lowercase substring prose.
- [ ] Existing catalog ordering preserved.
- [ ] Schema-cache restart note referenced for newly registered tools.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: `docs/mcp-ia-server.md` catalog order enforced by validator — inserting entries wrong place fails CI. Mitigation: read file TOC / existing tool order before edit.
- Risk: tool names drift (`master_plan_locate` vs code `registerTool` name). Mitigation: copy names from `tools/mcp-ia-server/src` registrations.
- Ambiguity: **parent_plan_validate** advisory mode wording — Stage Exit asks to note advisory behavior. Resolution: one sentence in catalog per tool.
- Invariant touch: English catalog prose for humans; keep param tables accurate.

### §Examples

| Catalog entry | Must include |
|---------------|--------------|
| `master_plan_locate` | inputs, output shape, caller use case |
| `backlog_list` extension | bullet listing 3 new optional params |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| doc_link_check | optional | `npm run validate:all` if doc checks exist | node |
| manual_review | PR diff | no duplicate tool headings | human |

### §Acceptance

- [ ] Catalog entries for `master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate`.
- [ ] `backlog_list` section notes new filters.
- [ ] Ordering preserved; `validate:all` green.

### §Findings

- If `docs/mcp-ia-server.md` lags code, prefer aligning to code in same PR as TECH-438.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
