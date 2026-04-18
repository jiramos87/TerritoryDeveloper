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
| `parent_plan` | `string (path)` | **Optional until Step 6.** Repo-relative path to the owning master-plan markdown (e.g. `ia/projects/zone-s-economy-master-plan.md`). Mirror of yaml `parent_plan`. Used by `master_plan_locate` + `/closeout` plan-row flip. Becomes required when `parent_plan_validate` flips to `--strict` default. |
| `task_key` | `string` — regex `^T\d+\.\d+(\.\d+)?$` | **Optional until Step 6.** Step/stage/phase locator derived from the master-plan task table row key (e.g. `T1.1.3`). Mirror of yaml `task_key`; step/stage/phase derivable by parser — no 5-field frontmatter needed. Non-conforming values rejected by validator. Becomes required with `parent_plan`. |

## 2-field parent-plan mirror

`parent_plan` + `task_key` are the spec-frontmatter half of the 2-field locator mirror introduced in the Backlog YAML ↔ MCP alignment program (Approach B). The yaml record (`ia/backlog/{id}.yaml`) is the source of truth; these two fields mirror it in the spec markdown so agents can resolve the owning plan without a grep scan.

Status: **optional until Step 6** — validator runs in advisory mode (exits 0, prints drift count). Step 6 late-hardening task flips the default to blocking.

Source / rationale: `docs/parent-plan-locator-fields-exploration.md`. Sibling template edit: TECH-384 (`ia/templates/project-spec-template.md`).

## Co-existence with Cursor frontmatter

Cursor expects `description` + `alwaysApply` on `ia/rules/*.md`; `name` + `description` on `ia/skills/{name}/SKILL.md`. Those stay; 4 IA fields add alongside. YAML order irrelevant. Validator checks IA fields presence only — never strips/rewrites Cursor fields.

## Quick decision table

| File family | Typical `loaded_by` | Typical `slices_via` | Typical `audience` | Notes |
|---|---|---|---|---|
| `ia/specs/glossary.md` | `router` | `glossary_lookup` | `agent` | |
| `ia/specs/{system}-system.md` | `router` | `spec_section` | `agent` | |
| `ia/rules/{name}.md` (`alwaysApply: true`) | `always` | `none` | `agent` | |
| `ia/rules/{name}.md` (router-reached) | `router` | `none` | `agent` | |
| `ia/skills/{name}/SKILL.md` | `skill:{name}` | `none` | `agent` | |
| `ia/projects/{ID}-{slug}.md` | `ondemand` | `none` | `both` | May carry `parent_plan` + `task_key` 2-field mirror (optional until Step 6). |
| `ia/templates/*.md` | `ondemand` | `none` | `both` | |

## Validator

`tools/mcp-ia-server/scripts/check-frontmatter.mjs` walks `ia/**/*.md`, parses YAML, prints one line per file with missing field. Exit non-zero on miss. Wired as `npm run validate:frontmatter`. Advisory at Stage 3; CI promotion deferred.
