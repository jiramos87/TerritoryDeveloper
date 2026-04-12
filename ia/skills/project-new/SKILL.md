---
purpose: "Use when creating a new BACKLOG.md issue from a user prompt: next BUG-/FEAT-/TECH-/ART-/AUDIO- id, row in the correct priority section, bootstrap ia/projects/{ISSUE_ID}.md from the template, and Depends on /…"
audience: agent
loaded_by: skill:project-new
slices_via: none
name: project-new
description: >
  Use when creating a new BACKLOG.md issue from a user prompt: next BUG-/FEAT-/TECH-/ART-/AUDIO- id,
  row in the correct priority section, bootstrap ia/projects/{ISSUE_ID}.md from the template,
  and Depends on / Related with verified ids (territory-ia MCP + optional web_search). Triggers:
  "/project-new", "new backlog issue", "create TECH-xx from prompt", "bootstrap project spec",
  "add issue to backlog from description".
---

# New backlog issue and project spec bootstrap

No MCP calls from skill body. Follow **Tool recipe** below before editing BACKLOG or creating spec — thin context via `AGENTS.md` step 3 + `mcp-ia-default.md`.

**vs kickoff:** kickoff starts from existing spec. This skill creates backlog row + spec stub from user prompt. After stub → [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md) → [`project-spec-implement`](../project-spec-implement/SKILL.md) → [`project-spec-close`](../project-spec-close/SKILL.md).

**Related:** [`project-implementation-validation`](../project-implementation-validation/SKILL.md) · [`BACKLOG.md`](../../../BACKLOG.md) · [`ia/skills/README.md`](../README.md).

## Seed prompt (parameterize)

Replace placeholders before sending.

```markdown
Create a new backlog issue and initial project spec from this description:

**Title / intent:** {SHORT_TITLE}
**Issue type:** {BUG-|FEAT-|TECH-|ART-|AUDIO-} (or ask me if unsure)
**User / product prompt:**

{USER_PROMPT}

Follow `ia/skills/project-new/SKILL.md`: run the Tool recipe (territory-ia), then add the row to `BACKLOG.md`, create `ia/projects/{ISSUE_ID}.md` from `ia/templates/project-spec-template.md`, set `Spec:` on the backlog row, and link Depends on / Related with verified ids only. Run `npm run validate:dead-project-specs` before finishing the PR.
```

## When to use `web_search`

Only for external facts (vendor APIs, third-party packages, standards) not in repo. Never override glossary/specs/invariants. Cite URLs in Decision Log or Notes.

## Tool recipe (territory-ia)

Run in order. Pure meta (no domain terms) → skip steps marked optional.

1. **`glossary_discover`** — `keywords` as JSON array, English tokens from prompt. Avoid generic-only arrays.
2. **`glossary_lookup`** — High-confidence terms from discover or known rows.
3. **`router_for_task`** — 1–3 domains; `domain` must match agent-router table vocabulary. Ad-hoc phrases → `no_matching_domain`.
4. **`spec_section`** — Only sections prompt implies; set `max_chars`. Editor Reports → include unity-development-context §10.
5. **`invariants_summary`** — If issue touches runtime C# / game subsystems. Skip for doc/IA-only.
6. **`backlog_issue`** — For each related id in Depends on / Related / Notes. Hard dep unsatisfied → align or wait. Searches BACKLOG then BACKLOG-ARCHIVE.
7. **`list_specs`** / **`spec_outline`** — Only if `spec` key unknown.

### Optional: journal (Postgres)

Only when prompt ambiguous/cross-cutting or user requests exploration context. `project_spec_journal_search` English query, `max_results` ≤ 8. `db_unconfigured` → skip.

### Branching

- **Roads/bridge/wet run** → roads-system + geo via `router_for_task` + `spec_section`.
- **Water/HeightMap/shore** → water-terrain-system + geo.
- **JSON/schema/Save** → persistence-system; no on-disk Save data changes unless user requires.

## File and backlog checklist

1. **Prefix** — `BUG-`/`FEAT-`/`TECH-`/`ART-`/`AUDIO-` per [`AGENTS.md`](../../../AGENTS.md).
2. **Next id** — Scan BACKLOG + BACKLOG-ARCHIVE for highest number in prefix; assign max + 1. Never reuse (monotonic per prefix).
3. **Priority section** — Match severity + existing BACKLOG structure. Follow Priority order in AGENTS.md.
4. **Backlog row** — Type, Files, Notes, `Spec: ia/projects/{ISSUE_ID}.md`, Depends on / Acceptance. Every cited id must exist in BACKLOG (or same edit batch).
5. **Project spec** — Copy [`project-spec-template.md`](../../templates/project-spec-template.md) → `ia/projects/{ISSUE_ID}.md`. Fill header, Summary, Goals, stub Implementation Plan, Open Questions per [`PROJECT-SPEC-STRUCTURE.md`](../../projects/PROJECT-SPEC-STRUCTURE.md).
6. **Validate** — `npm run validate:dead-project-specs`.
7. **Next** — Offer [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md) to refine before implementation.

## Follow-up

Domain skills (roads, terrain/water, new managers) from [`BACKLOG.md`](../../../BACKLOG.md) when implementing.
