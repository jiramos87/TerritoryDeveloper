# {ISSUE_ID} — {Title}

> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)
> **Status:** Draft | In Review | Final
> **Created:** YYYY-MM-DD
> **Last updated:** YYYY-MM-DD

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../../.cursor/specs/glossary.md (spec wins if glossary differs).
  Separate product behavior (sections 1–5.1, 8, Open Questions) from implementation notes (5.2+, 7, optional "Implementation investigation").
-->

## 1. Summary

<!-- 2-3 sentences: what this project does and why it matters. Domain vocabulary only. -->

## 2. Goals and Non-Goals

### 2.1 Goals

<!-- Specific, measurable outcomes this project delivers. -->

1. …

### 2.2 Non-Goals (Out of Scope)

<!-- What this project explicitly does NOT address. Prevents scope creep. -->

1. …

## 3. User / Developer Stories

<!-- Who benefits and how. Use the format: "As a [role], I want [capability] so that [benefit]." -->

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | … | … |
| 2 | Developer | … | … |

## 4. Current State

### 4.1 Domain behavior

<!-- Observed vs expected using canonical terms (glossary). No code. -->

### 4.2 Systems map

<!-- Short pointers: backlog Files, subsystems, spec sections. Optional file/class table for implementers. -->

### 4.3 Implementation investigation notes (optional)

<!-- Technical hypotheses for the implementing agent — not product requirements. -->

## 5. Proposed Design

### 5.1 Target behavior (product)

<!-- Player-visible rules and definitions; glossary-aligned. -->

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

<!-- Classes, data flow, algorithms. Agent proposes unless user locked design. -->

### 5.3 Method / algorithm notes (optional)

<!-- Signatures, pseudo-code — only if product owner must approve. -->

## 6. Decision Log

<!-- Record non-obvious choices made during spec work. Keep updating as the project evolves. -->

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| YYYY-MM-DD | … | … | … |

## 7. Implementation Plan

<!-- Ordered phases with concrete deliverables. Each phase should be independently testable. -->

### Phase 1 — {Name}

- [ ] …

### Phase 2 — {Name}

- [ ] …

## 8. Acceptance Criteria

<!-- Conditions that must be true for the project to be considered complete.
     These should map back to §2.1 Goals and §3 Stories. -->

- [ ] …

## 9. Issues Found During Development

<!-- Problems discovered during implementation that were not anticipated in the spec.
     Include root cause and resolution (or link to new backlog items). -->

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

<!-- Insights to carry forward into specs, rules, or architecture docs after the project closes.
     On completion: migrate relevant entries to AGENTS.md, coding-conventions, or canonical specs. -->

- …

## Open Questions (resolve before / during implementation)

<!--
  REQUIRED for collaborative specs.
  Rules: Use canonical terms from .cursor/specs/glossary.md only.
  Ask about GAME LOGIC and definitions — not specific code, APIs, or class names.
  The implementing agent resolves technical approach unless it would change intended behavior (then Decision Log or ask user).
  TOOLING-ONLY issues (CI, MCP, scripts, docs with no gameplay change): write "None — tooling only; see §8 Acceptance criteria" (or developer policy questions such as CI blocking vs advisory). Do not invent fake game rules to fill this section.
-->

1. …
