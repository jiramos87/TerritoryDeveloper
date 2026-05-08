---
purpose: "Project spec for TECH-78 — Skill chaining engine."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-78 — Skill chaining engine

> **Issue:** [TECH-78](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Build an MCP tool `suggest_skill_chain(task_description)` that reads all SKILL.md files, matches trigger conditions against a task description, and returns an ordered skill chain with pre-populated MCP tool call sequences. Transforms skills from static Markdown recipes into a composable, task-aware orchestration layer.

## 2. Goals and Non-Goals

### 2.1 Goals

1. MCP tool `suggest_skill_chain` that accepts a task description and returns an ordered list of skills to execute
2. Each suggested skill includes: trigger match explanation, ordered MCP tool recipe with issue-specific parameters pre-filled where possible
3. Understands skill dependencies (e.g., project-spec-kickoff before project-spec-implement, close-dev-loop before project-spec-close)
4. Parses SKILL.md frontmatter `description` field for trigger matching
5. When given an `issue_id`, enriches the chain with data from `backlog_issue` (Files, Spec, Notes)

### 2.2 Non-Goals (Out of Scope)

1. Auto-executing skills (the tool suggests, the agent decides)
2. Replacing SKILL.md files or changing their format
3. Creating new skills — only orchestrating existing ones
4. Runtime execution engine or task queue

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent told "implement FEAT-43", I want to know which skills to use and in what order | `suggest_skill_chain("implement FEAT-43")` → [project-spec-kickoff, project-spec-implement, close-dev-loop] with FEAT-43-specific MCP calls |
| 2 | AI agent | As an agent told "close out TECH-70 after verified work", I want the full closure workflow | Returns [project-spec-close] with closeout_digest + journal_persist + validate steps pre-populated |
| 3 | AI agent | As an agent told "add a new HUD row for forest coverage", I want skill suggestions even for non-issue tasks | Returns [ui-hud-row-theme] with spec_section calls for ui-design-system §1, §3.0, §4.3, §5.2 |
| 4 | Developer | As a developer, I want to ask "what skills exist for debugging?" and get a filtered list | `suggest_skill_chain("debug Play Mode issue")` → [ide-bridge-evidence, close-dev-loop] with descriptions |

## 4. Current State

### 4.1 Domain behavior

Skills are static Markdown files under `ia/skills/*/SKILL.md`. Each has a YAML frontmatter with a `description` field listing trigger phrases. Agents must read the skill README or know which skill to open. There is no programmatic matching or chaining. The README at `ia/skills/README.md` lists all skills but doesn't describe sequencing.

### 4.2 Systems map

- `ia/skills/*/SKILL.md` — 8 active skills with frontmatter triggers and Tool Recipes
- `ia/skills/README.md` — skill index and conventions
- `tools/mcp-ia-server/src/index.ts` — MCP tool registration
- `tools/mcp-ia-server/src/config.ts` — registry building (extends to scan skills)

## 5. Proposed Design

### 5.1 Target behavior (product)

**Example interaction:**

```
suggest_skill_chain({ task: "implement the growth ring tuning feature FEAT-43" })
→ {
    chain: [
      {
        skill: "project-spec-kickoff",
        reason: "FEAT-43 has a project spec that should be reviewed before implementation",
        recipe: [
          { tool: "backlog_issue", params: { issue_id: "FEAT-43" } },
          { tool: "invariants_summary", params: {} },
          { tool: "router_for_task", params: { domain: "simulation", files: ["UrbanCentroidService.cs"] } },
          { tool: "spec_section", params: { spec: "sim", section: "Rings" } },
          { tool: "glossary_discover", params: { keywords: ["urban growth rings", "centroid", "AUTO"] } }
        ]
      },
      {
        skill: "project-spec-implement",
        reason: "Execute the Implementation Plan from FEAT-43.md",
        recipe: "Per-phase loop — see SKILL.md"
      },
      {
        skill: "close-dev-loop",
        reason: "Verify growth ring changes in Play Mode with debug_context_bundle",
        recipe: [
          { tool: "unity_bridge_command", params: { kind: "enter_play_mode" } },
          { tool: "unity_bridge_command", params: { kind: "debug_context_bundle", seed_cell: "TBD" } }
        ]
      }
    ],
    notes: "After implementation and verification, use project-spec-close to archive."
  }
```

**Lifecycle model (skill dependency graph):**

```
project-new → project-spec-kickoff → project-spec-implement
                                          ↓
                                    ide-bridge-evidence / close-dev-loop
                                          ↓
                                    project-implementation-validation
                                          ↓
                                    project-spec-close
```

`ui-hud-row-theme` is standalone (no lifecycle dependency).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: SKILL.md parsing, trigger matching strategy (keyword, fuzzy, or LLM-assisted), skill dependency graph encoding, parameter pre-filling from backlog_issue data.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | MCP tool (suggest only) rather than auto-execution engine | Agent autonomy: the agent decides whether to follow the suggestion. Simpler, safer, more transparent | Full orchestration runtime; Cursor-native skill chaining |
| 2026-04-07 | Parse SKILL.md frontmatter for triggers rather than a separate config | Single source of truth — skills already declare their triggers | Separate `skill-triggers.json` manifest |

## 7. Implementation Plan

### Phase 1 — Skill parser and trigger matcher

- [ ] Parse all SKILL.md files: extract frontmatter triggers, Tool Recipe steps, lifecycle position
- [ ] Build trigger matching logic (keyword overlap + fuzzy)
- [ ] Encode skill dependency graph

### Phase 2 — MCP tool and enrichment

- [ ] Register `suggest_skill_chain` MCP tool
- [ ] Integrate with `backlog_issue` for parameter pre-filling when `issue_id` is provided
- [ ] Tests and documentation

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP tool registered | Node | `npm run verify` | Repo root |
| Trigger matching produces correct chains | Node | `npm run test:ia` | Fixture: known task → expected skill chain |

## 8. Acceptance Criteria

- [ ] `suggest_skill_chain` MCP tool registered and documented
- [ ] Given a task description, returns ordered skill chain with trigger match explanations
- [ ] Given an `issue_id`, pre-fills MCP tool parameters from backlog data
- [ ] Skill dependency graph correctly sequences lifecycle skills
- [ ] `npm run verify` and `npm run test:ia` green

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
