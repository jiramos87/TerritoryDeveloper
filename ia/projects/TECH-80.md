---
purpose: "Project spec for TECH-80 — Bidirectional IA: agents propose glossary additions and flag spec ambiguity."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-80 — Bidirectional IA: agents propose glossary additions and flag spec ambiguity

> **Issue:** [TECH-80](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Enable agents to feed knowledge *back* into the IA system during implementation — proposing glossary additions when they encounter undefined terms, flagging spec ambiguity when `spec_section` doesn't answer their question, and suggesting invariant additions when they discover constraints empirically. Creates a Postgres-backed `ia_suggestion` queue with a human review lifecycle.

## 2. Goals and Non-Goals

### 2.1 Goals

1. MCP tool `suggest_ia_improvement(kind, content, context?)` for agents to submit IA suggestions
2. Postgres table `ia_suggestion` with lifecycle: `proposed` → `reviewed` → `accepted` / `rejected`
3. Three suggestion kinds: `glossary_addition`, `spec_ambiguity`, `invariant_addition`
4. MCP tool `ia_suggestions_pending(kind?)` for human reviewers to list pending suggestions
5. MCP tool `ia_suggestion_resolve(id, action, notes?)` for humans to accept/reject

### 2.2 Non-Goals (Out of Scope)

1. Auto-applying suggestions (human review is mandatory)
2. Modifying specs, glossary, or invariants directly — suggestions are proposals only
3. Full editorial workflow (no drafts, no multi-reviewer approval)
4. Suggestions about code (only about IA content: glossary, specs, invariants, rules)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent implementing FEAT-43, I discover that "ring boundary fraction" is used in code but not in the glossary. I want to propose adding it | `suggest_ia_improvement({ kind: "glossary_addition", content: "Ring boundary fraction — the fraction of urban radius that defines each growth ring boundary", context: { issue_id: "FEAT-43", file: "UrbanCentroidService.cs" } })` creates a pending suggestion |
| 2 | AI agent | As an agent reading geo §5 about shore band, I find the spec doesn't clarify what happens when two water bodies with different surface heights are Moore-adjacent. I want to flag this | `suggest_ia_improvement({ kind: "spec_ambiguity", content: "geo §5 does not define shore band behavior when two adjacent water bodies have different surface heights", context: { spec: "geo", section: "5" } })` |
| 3 | AI agent | As an agent fixing a bug, I discover that `AutoZoningManager` must never zone cells within 1 cell of the map border. This constraint isn't in invariants | `suggest_ia_improvement({ kind: "invariant_addition", content: "AUTO zoning must not zone cells on the map border row/column" })` |
| 4 | Developer | As a human reviewer, I want to see all pending IA suggestions and decide which to accept | `ia_suggestions_pending()` returns list with id, kind, content, context, proposed_at |
| 5 | Developer | As a reviewer accepting a glossary suggestion, I want to mark it accepted and then manually add the term | `ia_suggestion_resolve({ id: 42, action: "accepted", notes: "Added to glossary as 'Ring boundary fraction'" })` |

## 4. Current State

### 4.1 Domain behavior

Knowledge flows one way: docs → agent. The reverse flow (agent → docs) only happens during project-spec-close, which migrates lessons from temporary project specs into durable IA. There is no mechanism for agents to flag gaps or propose additions during normal implementation work.

### 4.2 Systems map

- `tools/mcp-ia-server/src/ia-db/` — Postgres pool, existing table patterns
- `db/migrations/` — migration infrastructure
- `.cursor/specs/glossary.md` — glossary (target for glossary_addition suggestions)
- `.cursor/rules/invariants.mdc` — invariants (target for invariant_addition suggestions)
- `.cursor/specs/*.md` — specs (target for spec_ambiguity suggestions)

## 5. Proposed Design

### 5.1 Target behavior (product)

**Agent submits a suggestion:**

```
suggest_ia_improvement({
  kind: "glossary_addition",
  content: "Ring boundary fraction — the normalized distance (0-1) from urban centroid that separates growth rings. Configured per ring in UrbanCentroidService.",
  context: {
    issue_id: "FEAT-43",
    spec: "sim",
    section: "Rings",
    file: "UrbanCentroidService.cs"
  }
})
→ { ok: true, suggestion_id: 42, status: "proposed" }
```

**Human reviews pending suggestions:**

```
ia_suggestions_pending()
→ [
    { id: 42, kind: "glossary_addition", content: "Ring boundary fraction — ...", context: {...}, proposed_at: "2026-04-07T10:30:00Z" },
    { id: 43, kind: "spec_ambiguity", content: "geo §5 does not define...", context: {...}, proposed_at: "2026-04-07T11:15:00Z" }
  ]
```

**Human resolves:**

```
ia_suggestion_resolve({ id: 42, action: "accepted", notes: "Added to glossary row" })
→ { ok: true, status: "accepted" }
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: table schema, suggestion deduplication (fuzzy match on content to avoid duplicates), integration with project-spec-close workflow (auto-suggest from Decision Log content).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Human review mandatory before applying suggestions | IA quality is critical — incorrect glossary terms or invariants would degrade all future agent work | Auto-apply with rollback; LLM-judged auto-accept |
| 2026-04-07 | Three kinds only (glossary, spec ambiguity, invariant) | These are the most impactful gaps agents encounter. Rules and skills are less frequently ambiguous | Open-ended "any IA improvement" kind |

## 7. Implementation Plan

### Phase 1 — Suggestion infrastructure

- [ ] Design `ia_suggestion` table (id, kind, content, context jsonb, status, proposed_at, resolved_at, resolved_by, resolution_notes)
- [ ] Migration script
- [ ] Register `suggest_ia_improvement` MCP tool

### Phase 2 — Review tools

- [ ] Register `ia_suggestions_pending` and `ia_suggestion_resolve` MCP tools
- [ ] Tests and documentation

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tools registered and functional | Node | `npm run verify` + `npm run test:ia` | Repo root |
| Suggestion lifecycle works end-to-end | Node | Test: propose → list pending → resolve → verify status | Part of test suite |

## 8. Acceptance Criteria

- [ ] `ia_suggestion` table created via migration
- [ ] `suggest_ia_improvement` MCP tool accepts kind, content, context and creates proposed entries
- [ ] `ia_suggestions_pending` lists pending suggestions filtered by optional kind
- [ ] `ia_suggestion_resolve` transitions suggestion to accepted/rejected with notes
- [ ] Graceful `db_unconfigured` when Postgres unavailable
- [ ] Documented in `docs/mcp-ia-server.md`
- [ ] `npm run verify` and `npm run test:ia` green

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
