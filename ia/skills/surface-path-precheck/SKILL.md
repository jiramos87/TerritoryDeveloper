---
name: surface-path-precheck
purpose: >-
  Glob every Architecture-block entry/exit path; return [{path, exists, line_hint}] with `(new)`
  markers for greenfield paths. Prevents ghost line-number citations downstream.
audience: agent
loaded_by: "skill:surface-path-precheck"
slices_via: none
description: >-
  Sonnet subskill. Given a list of file/directory paths extracted from an Architecture or
  Component-map block, Globs each path and returns a structured result with `exists` flag and `(new)`
  marker for non-existent paths. Centralizes the Glob-then-classify loop shared by master-plan-new,
  master-plan-extend, and stage-decompose so ghost line numbers cannot propagate downstream. Triggers:
  "surface path precheck", "glob architecture paths", "surface-path-precheck subskill", "check path
  existence from architecture block".
phases: []
triggers:
  - surface path precheck
  - glob architecture paths
  - surface-path-precheck subskill
  - check path existence from architecture block
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# Surface path pre-check — Sonnet subskill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Purpose:** Globs every path cited in an Architecture / Component-map block. Returns structured
results that callers use to mark `(new)` vs existing paths in stage Relevant surfaces. Prevents
downstream stages from citing non-existent line numbers.

---

## Inputs

| Field | Type | Notes |
|-------|------|-------|
| `paths` | `string[]` | File or directory paths extracted from Architecture / Component map block. May be glob patterns or exact paths. |
| `context` | string | Optional label for the enclosing doc (e.g. `"Stage 2.1 Architecture"`). |

---

## Output

```json
[
  {"path": "Assets/Scripts/Managers/FooManager.cs", "exists": true, "line_hint": "line 42 (FooManager class)"},
  {"path": "Assets/Scripts/Utils/BarHelper.cs", "exists": false, "line_hint": "(new)"},
  {"path": "ia/skills/baz/SKILL.md", "exists": true, "line_hint": "line 1"}
]
```

---

## Contract

For each `path` in input:

1. **Glob** the path. If it resolves to one or more files → `exists: true`; note `line_hint` as first match path (or `"line 1"` if file present but no line context needed).
2. **No match** → `exists: false`, `line_hint: "(new)"`.
3. **Ambiguous name** (no extension, no slash) → Grep for plausible type name in `Assets/Scripts/` and `ia/`; if ≥1 hit, report first match as `exists: true`; if no hit, fall back to `exists: false, line_hint: "(new)"`.
4. Return array in same order as input.

---

## Usage in caller skills

Replace inline "Glob per entry/exit point..." blocks with:

> Run `surface-path-precheck` subskill on paths from Architecture / Component map block.
> Mark `(new)` for `exists: false` entries in stage Relevant surfaces.
> Never cite non-existent line numbers — use `line_hint` value verbatim.

---

## Guardrails

- Do NOT create missing files — presence check only.
- Do NOT modify the Architecture block — read-only classifier.
- Do NOT report `exists: true` without a confirmed Glob hit.

---

## Callers

`master-plan-new` Phase 2 surface-path pre-check sub-step ·
`master-plan-extend` Phase 2 surface-path pre-check sub-step ·
`stage-decompose` Phase 1 surface-path pre-check sub-step.
