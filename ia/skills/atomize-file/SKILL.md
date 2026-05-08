---
name: atomize-file
purpose: >-
  Orchestrate per-stage atomization of a C# mega-file: read class summary →
  derive concerns + sub-stage count from LOC threshold → seed Domains/{X}/ folder
  + facade + asmdef → extract services loop (one per concern) → extend composed
  test → verify validators green → hand off to /ship-cycle.
audience: agent
loaded_by: "skill:atomize-file"
slices_via: router_for_task, invariants_summary
description: >-
  Use when a C# file in Assets/Scripts/Managers/ needs atomization per Strategy γ
  (docs/large-file-atomization-componentization-strategy.md). Phases: 1) read
  csharp_class_summary; 2) derive concerns + sub-stage count from LOC threshold
  (<=2500=1, >2500=2, >3500=3); 3) seed Domains/{X}/ folder + I{X}.cs facade interface
  + {X}.cs facade impl + {X}.asmdef; 4) extract services loop (one per concern,
  namespace Domains.{X}.Services); 5) extend composed test; 6) verify validators green
  (validate:all + lint:csharp + validate:domain-facades + unity:compile-check); 7)
  hand off to /ship-cycle.
  Sub-stage decomposition rule: file >2500 LOC = 2 sub-stages; >3500 LOC = 3 sub-stages.
  Triggers: "/atomize-file {FILE_PATH}", "atomize {CLASS_NAME}", "extract domains from {FILE}".
phases:
  - Read csharp_class_summary + determine LOC + public API surface
  - Derive concerns + compute sub-stage count (LOC threshold table)
  - Seed Domains/{X}/ folder (I{X}.cs + {X}.cs + {X}.asmdef + Editor sub-asmdef)
  - Extract services loop (one {Concern}Service.cs per concern, POCO, namespace Domains.{X}.Services)
  - Extend composed test at Assets/Tests/EditMode/Atomization/{stage-slug}/
  - Verify validators green (validate:all + lint:csharp + validate:domain-facades + unity:compile-check)
  - Hand off to /ship-cycle (single stage commit covers all diffs)
triggers:
  - /atomize-file {FILE_PATH}
  - atomize {CLASS_NAME}
  - extract domains from {FILE}
argument_hint: "{FILE_PATH} [--domain {X}] [--force-substages {N}] [--force-model {model}]"
model: sonnet
reasoning_effort: medium
input_token_budget: 80000
tools_role: implementer
tools_extra:
  - mcp__territory-ia__invariants_summary
  - mcp__territory-ia__rule_content
  - mcp__territory-ia__glossary_lookup
caveman_exceptions:
  - code
  - commits
---

## §Sub-stage decomposition threshold table

| File LOC | Sub-stages | Rationale |
|----------|-----------|-----------|
| ≤ 2500 | 1 | Single stage; all concerns extracted in one pass. |
| > 2500, ≤ 3500 | 2 | Split concerns across 2 ship-cycle sub-stages. |
| > 3500 | 3 | Large file; 3 sub-stages prevent diff overload. |

Sub-stage ids use decimal suffix: Stage X.1a, X.1b, X.1c.

## §Invariant #5 carve-out reminder

Services under `Assets/Scripts/Domains/{X}/Services/*Service.cs` hold a `GridManager grid` (or equivalent) composition reference. Direct `grid.cellArray` / `grid.gridArray` access is permitted per invariant #5 carve-out. Document the rationale at each touch site.

## §Guardrails

- NEVER add `using` from another domain's concrete `Services/` namespace — only facade interface.
- ALWAYS preserve `.meta` GUID on `git mv` (`git mv file.cs` then `git mv file.cs.meta` separately).
- ALWAYS seed `I{X}.cs` with at least one method signature before moving any service — facade-validator requires non-empty interface.
- NEVER skip `unity:compile-check` after any `.cs` move or creation.
- NEVER grow facade impl beyond thin orchestration — extract new `{Concern}Service.cs` on first sign.
- IF file has `// long-method-allowed` escape hatch → carry it forward to the service that owns the method.

## §Changelog

- 2026-05-08 — Initial skill authored for Stage 1.5 (large-file-atomization-refactor).
