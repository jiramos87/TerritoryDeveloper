# TECH-76 — Information Architecture system overview document

> **Issue:** [TECH-76](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Create a single `docs/information-architecture-overview.md` document that describes the complete IA system — philosophy, layer connections, knowledge lifecycle, consistency mechanisms, and extension guide — so that agents and human contributors understand *why* the system exists, not just how to use individual pieces.

## 2. Goals and Non-Goals

### 2.1 Goals

1. A single ~200-line document that a new agent or human contributor reads to understand the IA system as a coherent design
2. Diagram showing how layers connect: rules → specs → glossary → MCP → skills → project specs → backlog
3. Clear description of the knowledge lifecycle: issue creation → spec → implementation → closure → durable IA migration
4. Section on how to extend the system (add a spec, add a tool, add a skill, add a glossary term)
5. Quick-reference pointers to all existing scattered documentation

### 2.2 Non-Goals (Out of Scope)

1. Rewriting or replacing `AGENTS.md`, `docs/mcp-ia-server.md`, or `.cursor/skills/README.md`
2. Changing any runtime behavior or MCP tools
3. Documenting the game domain itself (that's what specs are for)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | New AI agent | As an agent starting work on an unfamiliar issue, I want to understand *why* the IA system is structured this way so I can make better judgment calls when the recipe doesn't cover my case | Document explains design philosophy and layer rationale |
| 2 | Human contributor | As a developer onboarding to the project, I want a single entry point that shows me the full IA picture without reading 8 separate files | Document links to all relevant files with one-line descriptions |
| 3 | IA maintainer | As someone extending the system (new spec, new tool, new skill), I want a checklist so I don't miss registration steps | Document includes extension checklists |

## 4. Current State

### 4.1 Domain behavior

The IA system is fully functional but its own description is scattered across: `AGENTS.md` (workflow), `docs/mcp-ia-server.md` (MCP tools), `docs/mcp-markdown-ia-pattern.md` (reusable pattern), `.cursor/skills/README.md` (skill conventions), `ARCHITECTURE.md` § Agent IA, `.cursor/specs/REFERENCE-SPEC-STRUCTURE.md` (spec authoring), `.cursor/projects/PROJECT-SPEC-STRUCTURE.md` (project spec lifecycle). No single document explains the system holistically.

### 4.2 Systems map

- `AGENTS.md` — workflow policies
- `docs/mcp-ia-server.md` — MCP tool catalog
- `.cursor/skills/README.md` — skill conventions
- `.cursor/specs/REFERENCE-SPEC-STRUCTURE.md` — spec authoring rules
- `.cursor/projects/PROJECT-SPEC-STRUCTURE.md` — project spec lifecycle
- `ARCHITECTURE.md` § Agent information architecture — system-level pointer

## 5. Proposed Design

### 5.1 Target behavior (product)

The document should cover:

1. **Philosophy:** Hierarchical documentation + semantic model + MCP slicing + skill workflows + optional Postgres persistence
2. **Layer diagram:** Visual (Mermaid or ASCII) showing rules → specs → glossary → MCP → skills → project specs → backlog and how they feed each other
3. **Knowledge lifecycle:** Issue creation → project-spec-kickoff → implement → close-dev-loop → project-spec-close → durable IA migration
4. **Consistency mechanisms:** terminology-consistency rule, invariant enforcement, `validate:*` scripts, IA index checks
5. **Extension guide:** Checklists for adding a reference spec, an MCP tool, a skill, a glossary term, a rule
6. **Index:** One-line pointers to every existing IA document with its purpose

**Example usage:** An agent receives a task "add a new MCP tool for building statistics." It reads `information-architecture-overview.md`, finds the "Adding an MCP tool" checklist (register in index.ts, add tests, update docs/mcp-ia-server.md, update glossary if new term, run validate:all), and follows it.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Document lives at `docs/information-architecture-overview.md`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Single new doc rather than restructuring AGENTS.md | AGENTS.md serves as workflow reference; the overview serves as system architecture reference — different audiences | Merge into AGENTS.md; add to ARCHITECTURE.md |

## 7. Implementation Plan

### Phase 1 — Draft document

- [ ] Write `docs/information-architecture-overview.md` with all sections described in §5.1
- [ ] Add layer diagram (Mermaid preferred, ASCII fallback)
- [ ] Add extension checklists (one per extensible component)
- [ ] Cross-link from AGENTS.md and ARCHITECTURE.md

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| No dead links in new doc | Manual | Verify all relative links resolve | N/A in CI |
| Existing docs not broken | Node | `npm run validate:dead-project-specs` | Repo root |

## 8. Acceptance Criteria

- [ ] `docs/information-architecture-overview.md` committed with all §5.1 sections
- [ ] Layer diagram present and readable
- [ ] Extension checklists cover: reference spec, MCP tool, skill, glossary term, rule
- [ ] Linked from `AGENTS.md` documentation hierarchy and `ARCHITECTURE.md` § Agent IA

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling/documentation only; see §8 Acceptance criteria.
