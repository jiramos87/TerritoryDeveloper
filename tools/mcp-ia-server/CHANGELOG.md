# @territory/mcp-ia-server — Changelog

## v0.6.0 — 2026-04-18 — Quick wins: glossary bulk-terms + structured invariants

Step 1 Quick Wins band of the Opus 4.7 audit master plan (`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`). Additive, non-breaking — all pre-existing tool shapes preserved.

Shipped:

- **TECH-314** — `glossary_lookup` accepts `terms: string[]` alongside single `term`; returns `{ results, errors, meta.partial }` partial-result shape.
- **TECH-315** — Bulk-terms unit tests (happy path + partial failure + single-`term` back-compat + empty `terms: []`).
- **TECH-371** — `tools/mcp-ia-server/data/invariants-tags.json` sidecar — subsystem tags for all 13 invariants + guardrails.
- **TECH-372** — `invariants_summary` accepts optional `domain?: string` filter; returns structured `{ description, invariants: [{number, title, subsystem_tags}], guardrails: [{index, title, subsystem_tags}], markdown }`; `markdown` side-channel preserves prose rendering for text-only callers.
- **TECH-373** — `invariants_summary` unit tests (domain match filter, domain-no-match graceful, no-domain all-13, markdown side-channel).

**Rollback tag advisory:** tag this commit `mcp-pre-envelope-v0.5.0` locally (or push the annotated tag) before landing Step 2 (P2 envelope breaking cut). Provides the rollback target if the envelope rewrite needs to be reverted in the same PR boundary.
