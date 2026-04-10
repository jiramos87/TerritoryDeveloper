# TECH-31e — MCP tool and agent workflows

**Program:** [TECH-31-agent-scenario-generator-program.md](TECH-31-agent-scenario-generator-program.md) **Stage 31e**.  
**Backlog:** [TECH-31](../BACKLOG.md).  
**Prerequisite stages:** **31a**–**31c** (file-based verification + **Agent test mode batch** goldens — shipped); **31d** if **MCP** responses should mention **city history** queries.

## Summary

Register a **territory-ia** **MCP** tool (name TBD, e.g. `scenario_resolve` / `test_scenario_materialize`) that returns small payloads: artifact path, **test mode** invocation hints, pointers to **Test contracts** and **close-dev-loop** recipe. Update **`docs/mcp-ia-server.md`** and **`tools/mcp-ia-server/README.md`**.

## Goals

- Tool inputs: **scenario id** and/or inline descriptor reference path.
- Tool outputs: paths, invariant-safe hints, links to verification steps—keep responses small per agent-tooling rules.
- Cross-link [`ia/skills/close-dev-loop/SKILL.md`](../ia/skills/close-dev-loop/SKILL.md).
- Run **`project-implementation-validation`** subset if **`tools/mcp-ia-server`** or **`docs/schemas`** change.

## Non-goals

- Replacing **backlog_issue** or full spec retrieval—thin wrapper only.

## Implementation checklist

- [ ] Register tool in `tools/mcp-ia-server/src/`.
- [ ] Update **`docs/mcp-ia-server.md`** + package README.
- [ ] Skill cross-links (optional: **project-spec-implement** / **close-dev-loop** mentions).

## Test contracts (stage)

| Goal | Check | Notes |
|------|--------|--------|
| **MCP** tests | `npm test` under **`tools/mcp-ia-server`** | If tool registered |
| IA index | `generate:ia-indexes --check` if descriptions feed indexes | Per validation skill |

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
|  |  |  |

## Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |
