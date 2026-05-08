---
description: Use when a C# file in Assets/Scripts/Managers/ needs atomization per Strategy γ (docs/large-file-atomization-componentization-strategy.md). Phases: 1) read csharp_class_summary; 2) derive concerns + sub-stage count from LOC threshold (<=2500=1, >2500=2, >3500=3); 3) seed Domains/{X}/ folder + I{X}.cs facade interface + {X}.cs facade impl + {X}.asmdef; 4) extract services loop (one per concern, namespace Domains.{X}.Services); 5) extend composed test; 6) verify validators green (validate:all + lint:csharp + validate:domain-facades + unity:compile-check); 7) hand off to /ship-cycle. Sub-stage decomposition rule: file >2500 LOC = 2 sub-stages; >3500 LOC = 3 sub-stages. Triggers: "/atomize-file {FILE_PATH}", "atomize {CLASS_NAME}", "extract domains from {FILE}".
argument-hint: "{FILE_PATH} [--domain {X}] [--force-substages {N}] [--force-model {model}]"
---

# /atomize-file — Orchestrate per-stage atomization of a C# mega-file: read class summary → derive concerns + sub-stage count from LOC threshold → seed Domains/{X}/ folder + facade + asmdef → extract services loop (one per concern) → extend composed test → verify validators green → hand off to /ship-cycle.

Drive `$ARGUMENTS` via the [`atomize-file`](../agents/atomize-file.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /atomize-file {FILE_PATH}
- atomize {CLASS_NAME}
- extract domains from {FILE}
## Dispatch

Single Agent invocation with `subagent_type: "atomize-file"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/atomize-file/SKILL.md`](../../ia/skills/atomize-file/SKILL.md) §Hard boundaries.
