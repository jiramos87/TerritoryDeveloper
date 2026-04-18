---
purpose: "TECH-427 — structured rule_content payload + markdown side-channel."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md"
task_key: "T2.3.2"
---
# TECH-427 — Structured rule_content payload + markdown side-channel

> **Issue:** [TECH-427](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Convert `rule-content.ts` response to structured `{ rule_key, title, sections: [{id, heading, body}], markdown?: string }`. Gives `rule_section` tool (from TECH-399 T2.2.2) structured base to slice. `markdown` side-channel preserves raw prose for agents still rendering full rule text.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `rule_content({ rule })` returns structured `sections: [{id, heading, body}]` array.
2. `markdown?: string` side-channel = raw file text.
3. Section ids stable across calls (derived from heading slug).
4. `rule_section({ rule, section })` slices align byte-identical w/ `sections[].body`.

### 2.2 Non-Goals

1. No changes to rule authoring flow or file format.
2. No new rules created.
3. No frontmatter parsing changes.

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/rule-content.ts` — current returns raw text blob.
- `rule_section` tool added in Stage 2.2 TECH-399 — needs structured sections to slice.
- Heading parser pattern already exists for `spec-outline.ts` (reuse or symmetric copy).

## 5. Proposed Design

### 5.2 Architecture / implementation

Reuse heading parser from `spec-outline.ts` / `spec-section.ts` or extract shared helper under `src/parser/markdown-sections.ts`. Parse rule markdown into `{id, heading, body}` slices keyed by stable slug. Include `markdown` side-channel equal to raw file read.

## 7. Implementation Plan

### Phase 1 — Parser + response reshape

- [ ] Extract / share heading parser helper for markdown slicing.
- [ ] Update `rule-content.ts` handler to build structured `sections` + `markdown` side-channel.
- [ ] Ensure `rule_section` consumes same structure (no drift).
- [ ] Unit tests: empty rule, multi-heading rule, flat rule.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Structured sections present | Node unit | `cd tools/mcp-ia-server && npm test` | Fixtures: invariants / agent-lifecycle rules |
| `rule_section` aligns | Node unit | same | Slice output = `sections[i].body` |
| `validate:all` green | Node | `npm run validate:all` | No IA-index regressions |

## 8. Acceptance Criteria

- [ ] `rule_content` response: `{ rule_key, title, sections: [{id, heading, body}], markdown }`.
- [ ] `rule_section` slices match `sections[].body`.
- [ ] Unit tests cover empty / multi-heading / flat rules.
- [ ] `validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
