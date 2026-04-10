---
purpose: IA universal frontmatter schema.
audience: both
loaded_by: ondemand
slices_via: none
---

# IA universal frontmatter schema

Every Markdown file under `ia/{specs,rules,skills/{name}/SKILL.md,projects,templates}` carries a YAML frontmatter block with four required fields. Authors decide each value at file-creation time; the validator (`npm run validate:frontmatter`) flags any missing field.

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

| Field | Allowed values | Meaning |
|---|---|---|
| `purpose` | free text, one line, present tense | What the file is for. Avoid restating the title; describe the file's job. |
| `audience` | `human`, `agent`, `both` | Who the file is written for. `agent` = MCP / Claude Code / Cursor consumes it. `human` = humans read it directly. `both` = both. |
| `loaded_by` | `always`, `skill:{name}`, `router`, `ondemand` | How the file enters context. `always` = `@-imported` from `CLAUDE.md` / `AGENTS.md` or auto-applied as a Cursor `alwaysApply` rule. `skill:{name}` = body of the named skill (or attached recipe). `router` = reachable via `router_for_task` / spec router tables. `ondemand` = an agent reads it explicitly when needed. |
| `slices_via` | `spec_section`, `glossary_lookup`, `none` | The MCP slicing entry point for this file. `spec_section` = `mcp__territory-ia__spec_section` / `spec_sections` / `spec_outline`. `glossary_lookup` = the glossary file. `none` = read whole or no MCP slicer. |

## Co-existence with Cursor frontmatter

Cursor expects `description` + `alwaysApply` on `ia/rules/*.md` and `name` + `description` on `ia/skills/{name}/SKILL.md`. Those fields **stay**; the four IA fields are added alongside. YAML order is irrelevant. Validator only checks for the four IA fields' presence — it does not strip or rewrite Cursor fields.

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

`tools/mcp-ia-server/scripts/check-frontmatter.mjs` walks `ia/**/*.md`, parses the YAML, and prints one line per file missing one of the four fields. Exit non-zero on any missing field. Wired as `npm run validate:frontmatter`. Advisory at first (Stage 3); CI promotion deferred to a later stage.
