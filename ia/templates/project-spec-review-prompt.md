---
purpose: "Project spec review — paste prompt (no Cursor Skill frontmatter)."
audience: both
loaded_by: ondemand
slices_via: none
---
# Project spec review — paste prompt (no Cursor Skill frontmatter)

For chats where Cursor Skills not loaded. Authoritative MCP tool order: **[`ia/skills/project-spec-kickoff/SKILL.md`](../skills/project-spec-kickoff/SKILL.md)** (see **Tool recipe (territory-ia)**).

Replace `{SPEC_PATH}` with project spec (e.g. `ia/projects/FEAT-49.md`).

---

Review `@{SPEC_PATH}`. Ensure canonical glossary + reference-spec terms.
Analyze goals; avoid negative impact on current subsystems unless spec accepts tradeoffs.
Make `## 7. Implementation Plan` more concrete.

Follow **Tool recipe (territory-ia)** in `ia/skills/project-spec-kickoff/SKILL.md` (order: `backlog_issue` → `invariants_summary` → `router_for_task` → `spec_section` → `glossary_discover` with **keywords as array** → `glossary_lookup` → `spec_outline` / `list_specs` if needed).

Material edits → update related IA: linked project specs, glossary rows, reference spec sections.

---

**Index:** [`ia/skills/README.md`](../skills/README.md) · **Authoring:** same README (**Lessons learned**, conventions)

---

## After review: implement

Spec ready + run `## 7. Implementation Plan` in order → use [`ia/skills/project-spec-implement/SKILL.md`](../skills/project-spec-implement/SKILL.md) (**Seed prompt** + **Tool recipe** inside).
