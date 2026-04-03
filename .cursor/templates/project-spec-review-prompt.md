# Project spec review — paste prompt (no Cursor Skill frontmatter)

Use this in any chat when **Cursor Skills** are not loaded. The **authoritative** MCP tool order is in **[`.cursor/skills/project-spec-kickoff/SKILL.md`](../skills/project-spec-kickoff/SKILL.md)** (see **Tool recipe (territory-ia)**).

Replace `{SPEC_PATH}` with your project spec (e.g. `.cursor/projects/TECH-44a.md`).

---

Review `@{SPEC_PATH}` and ensure it uses canonical terms from the glossary and reference specs.
Analyze stated goals; avoid negatively affecting current subsystems unless the spec explicitly accepts tradeoffs.
Make `## 7. Implementation Plan` more concrete where possible.

Follow the **Tool recipe (territory-ia)** in `.cursor/skills/project-spec-kickoff/SKILL.md` (order: `backlog_issue` → `invariants_summary` → `router_for_task` → `spec_section` → `glossary_discover` with **keywords as array** → `glossary_lookup` → `spec_outline` / `list_specs` if needed).

If you make material edits, update related Information Architecture: linked project specs, glossary rows, and reference spec sections so implementation stays aligned.

---

**Index:** [`.cursor/skills/README.md`](../skills/README.md) · **Authoring rules:** same README (**Lessons learned**, conventions)

---

## After review: implement the spec

When the spec is ready and you need to **run** `## 7. Implementation Plan` in order, use the **implementation** skill: [`.cursor/skills/project-spec-implement/SKILL.md`](../skills/project-spec-implement/SKILL.md) (**Seed prompt** and **Tool recipe** inside).
