---
purpose: IA universal frontmatter schema.
audience: both
loaded_by: ondemand
slices_via: none
---

# IA universal frontmatter schema

Every Markdown file under `ia/{specs,rules,skills/{name}/SKILL.md,projects,templates}` carries YAML frontmatter with 4 required fields. Authors decide at file-creation. Validator `npm run validate:frontmatter` flags missing fields.

## Schema

```yaml
---
purpose: one line, present tense, what this file is for
audience: human|agent|both
loaded_by: always|skill:{name}|router|ondemand
slices_via: spec_section|glossary_lookup|none
---
```

### Fields

| Field | Values | Meaning |
|---|---|---|
| `purpose` | one-line present tense | Describe file's job; don't restate title. |
| `audience` | `human` / `agent` / `both` | `agent` = MCP / Claude Code / Cursor consumes. `human` = humans read. `both` = both. |
| `loaded_by` | `always` / `skill:{name}` / `router` / `ondemand` | `always` = `@`-imported from `CLAUDE.md` / `AGENTS.md` or Cursor `alwaysApply`. `skill:{name}` = named skill body / attached recipe. `router` = via `router_for_task` / spec router tables. `ondemand` = agent reads explicitly. |
| `slices_via` | `spec_section` / `glossary_lookup` / `none` | MCP slicing entry. `spec_section` = `mcp__territory-ia__spec_section` / `spec_sections` / `spec_outline`. `glossary_lookup` = glossary file. `none` = read whole / no slicer. |

## Co-existence with Cursor frontmatter

Cursor expects `description` + `alwaysApply` on `ia/rules/*.md`; `name` + `description` on `ia/skills/{name}/SKILL.md`. Those stay; 4 IA fields add alongside. YAML order irrelevant. Validator checks IA fields presence only — never strips/rewrites Cursor fields.

## Quick decision table

| File family | Typical `loaded_by` | Typical `slices_via` | Typical `audience` |
|---|---|---|---|
| `ia/specs/glossary.md` | `router` | `glossary_lookup` | `agent` |
| `ia/specs/{system}-system.md` | `router` | `spec_section` | `agent` |
| `ia/rules/{name}.md` (`alwaysApply: true`) | `always` | `none` | `agent` |
| `ia/rules/{name}.md` (router-reached) | `router` | `none` | `agent` |
| `ia/skills/{name}/SKILL.md` | `skill:{name}` | `none` | `agent` |
| `ia/projects/{ID}-{slug}.md` | `ondemand` | `none` | `both` |
| `ia/templates/*.md` | `ondemand` | `none` | `both` |

## Validator

`tools/mcp-ia-server/scripts/check-frontmatter.mjs` walks `ia/**/*.md`, parses YAML, prints one line per file with missing field. Exit non-zero on miss. Wired as `npm run validate:frontmatter`. Advisory at Stage 3; CI promotion deferred.
