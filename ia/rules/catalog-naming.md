---
purpose: "Naming convention rule for catalog_entity slugs — {purpose}-{kind} pattern."
audience: agent
loaded_by: on-demand
slices_via: rule_content
---
# Catalog entity naming convention — `{purpose}-{kind}`

## Pattern

`catalog_entity.slug` MUST match:

```
^[a-z][a-z0-9]+(-[a-z0-9]+)*$
```

Semantic shape: `{purpose}-{kind}` where:

- **purpose** — one or more hyphen-joined lowercase segments describing the entity's domain role (e.g. `power-plant-tool`, `population-counter`, `building-info`).
- **kind** — final segment; MUST be one of the allowed kind suffixes below.

## Allowed kind suffixes

| Suffix | Catalog kind |
|---|---|
| `button` | button |
| `display` | panel / token (read-only display) |
| `readout` | token (numeric readout) |
| `picker` | panel (selection surface) |
| `panel` | panel (generic container) |
| `icon` | sprite (standalone icon) |

## Examples

| Slug | Verdict |
|---|---|
| `power-plant-tool-button` | GOOD |
| `population-counter-display` | GOOD |
| `building-info-panel` | GOOD |
| `budget-icon` | GOOD |
| `illuminated-button (5)` | BAD — trailing `(N)` segment |
| `Button7` | BAD — uppercase + missing kind suffix |
| `power_plant_tool_button` | BAD — underscores forbidden |
| `5-button` | BAD — leading digit |
| `picker` | BAD — missing purpose prefix |

## Forbidden patterns

| Pattern | Reason |
|---|---|
| Trailing `(N)` segment | Parenthesised numeric suffixes not valid identifier chars |
| All-numeric trailing segment | `…-72`, `…-2` ambiguous ordinals |
| Underscore anywhere | Convention is hyphen-only |
| Leading digit | Regex anchor `^[a-z]` |
| Missing kind suffix | Last segment must be an allowed kind |
| Uppercase letter | Slug must be all-lowercase |

## Enforcement

- **Hard-fail mode (default, post Stage 9.11-T3 migration):** `validate:catalog-naming` exits 1 on any offender. Wired into `validate:all`. Run with `--lint` flag for warning-only mode.
- **Lint mode:** pass `--lint` flag → emits offender table, exits 0. Use during DB migrations or local exploration.

## Cross-links

- `catalog_entity` → `ia/specs/glossary.md` § catalog_entity row.
- Schema: `ia/specs/catalog-architecture.md` §2.1 (spine + DEC-A24 regex superseded by this rule for semantic shape).
- Validator: `tools/scripts/validate-catalog-naming.mjs`.
- Migration: `db/migrations/0092_catalog_semantic_rename.sql`.
