---
purpose: "Use when reviewing, tightening, or enriching a ia/projects/{ISSUE_ID}.md project spec before writing code—especially for BUG-/FEAT-/TECH- work, JSON or infra program specs, or when aligning vocabulary with the…"
audience: agent
loaded_by: skill:project-spec-kickoff
slices_via: none
name: project-spec-kickoff
description: >
  Use when reviewing, tightening, or enriching a ia/projects/{ISSUE_ID}.md project spec before
  writing code—especially for BUG-/FEAT-/TECH- work, JSON or infra program specs,
  or when aligning vocabulary with the glossary. Triggers include "kickoff spec", "review project spec",
  "enrich TECH-xx.md", "canonical terms audit", "Implementation Plan too vague", "pre-implementation spec pass".
---

# Project spec kickoff and IA alignment

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md) (loaded by parent context or agent def).

No MCP calls from skill body. Follow **Tool recipe** below — context as slices, not whole specs.

**Related:** [`project-spec-implement`](../project-spec-implement/SKILL.md) · [`project-implementation-validation`](../project-implementation-validation/SKILL.md) (`npm` checks) · [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (§7b Play Mode) · [`project-spec-close`](../project-spec-close/SKILL.md) (verified close). **Conventions:** [`ia/skills/README.md`](../README.md).

Verified + closing → use [`project-spec-close`](../project-spec-close/SKILL.md), not this skill.

## Orchestrator routing

Orchestrator docs (`*master-plan*`, `step-*-*.md`, `stage-*-*.md`) → step/stage review, not issue-level kickoff. Per `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md`.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path from the backlog **Spec:** line (`ia/projects/{ISSUE_ID}.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Review @{SPEC_PATH} and ensure it uses canonical terms from the glossary and reference specs.
Analyze stated goals; avoid negatively affecting current subsystems unless the spec explicitly accepts tradeoffs.
Make ## 7. Implementation Plan more concrete where possible. For each bullet whose action is non-obvious (complex transform, guard pattern, new integration, non-trivial rename), add a brief inline example — a short pseudo-code snippet, a before/after method signature, or a concrete call site — immediately after the bullet. Keep examples ≤ 4 lines; omit when the bullet is self-evident.
For **FEAT-** / **BUG-** specs, ensure ## 7b. Test Contracts maps **§8 Acceptance** to verifiable checks (see `ia/templates/project-spec-template.md`).
Follow the MCP tool sequence in this skill's "Tool recipe (territory-ia)" section (do not skip steps unless the spec is tooling-only and cannot touch game subsystems).
If you make material edits, update related Information Architecture: linked project specs, glossary rows, and reference spec sections so implementation stays aligned.
```

## Tool recipe (territory-ia)

Run in order. Skip steps only when spec is pure doc hygiene (no code/subsystem touch).

1. **Parse target** — Load `{SPEC_PATH}`. Extract `ISSUE_ID` from `> **Issue:**`.
2. **`backlog_issue`** — Pull Files, Notes, Depends on, Acceptance, `depends_on_status`. Hard dep unsatisfied (`satisfied: false`, `soft_only: false`) → **stop** unless user overrides.
3. **`invariants_summary`** — Once per session if spec implies code/game changes. Skip for pure doc/IA.
4. **Domain routing** — 1–3 domains from Summary/Goals/Files. `router_for_task` with `domain` matching agent-router vocabulary. On `no_matching_domain`: retry with `files` (repo-relative paths).
5. **`spec_section`** — Only sections spec implies; set `max_chars`. No full `ia/specs/*.md` unless `spec_outline` forces it.
6. **`glossary_discover`** — `keywords` as JSON array, English tokens from ambiguous prose. Run after domain hints — avoid generic keywords.
7. **`glossary_lookup`** — Exact term strings from discover or glossary table.
8. **`spec_outline`** / **`list_specs`** — Only if `spec` key unknown.

### Optional: journal (Postgres)

Only when Open Questions fuzzy, Summary/Goals unclear, or user requests exploration context. Requires `DATABASE_URL`.

1. `project_spec_journal_search` — English `query`; `max_results` ≤ 8.
2. `project_spec_journal_get` — sparingly, when excerpt insufficient.
3. `db_unconfigured` → skip.

### Branching

- **Roads/bridges/wet run** → roads-system + isometric-geography-system via `router_for_task` + `spec_section`.
- **Water/HeightMap/shore** → water-terrain-system + geo sections.
- **JSON/schema/DTO** (Save-adjacent) → persistence-system (Load pipeline, Save data); no on-disk Save data changes unless issue requires.

### Impact preflight (optional)

1. Classify backlog Files as read vs write.
2. Write paths touching runtime C# → `invariants_summary` + `ia/rules/invariants.md` cross-check.
3. Flag cross-subsystem edits → `spec_section` pulls both domains.

After MCP slices → editorial pass: Open Questions, Implementation Plan, Decision Log, sibling spec cross-links.

## §7b Test Contracts (optional alignment)

When enriching `## 7b. Test Contracts` ([`PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md)):
- Map §8 Acceptance → verifiable checks: Node (`npm run …`), Unity manual, MCP tools. Use glossary terms, not backlog ids.
- Play Mode Console / visual checks → add `unity_bridge_command` kind values (`get_console_logs`, `capture_screenshot`) + params (`severity_filter`, `include_ui: true`). Point to [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md).
- Bridge-backed rows: mark **Check type** as `MCP / dev machine` — N/A in CI.

Kickoff does not call bridge — ensures §7b prose matches territory-ia tools.

## Open Questions policy

- Canonical game vocabulary from glossary/reference specs only.
- Game logic and definitions — not APIs, class names, implementation mechanics.
- Tooling-only issues: Open Questions N/A or point to Acceptance/Decision Log per [`PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md).

## Follow-up

Domain guardrail skills (roads, terrain/water, new managers) — see [`BACKLOG.md`](../../../BACKLOG.md). This skill for spec quality → [`project-spec-implement`](../project-spec-implement/SKILL.md) for execution → domain skills from BACKLOG when implementing.
