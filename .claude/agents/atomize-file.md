---
name: atomize-file
description: Use when a C# file in Assets/Scripts/Managers/ needs atomization per Strategy γ (docs/large-file-atomization-componentization-strategy.md). Phases: 1) read csharp_class_summary; 2) derive concerns + sub-stage count from LOC threshold (<=2500=1, >2500=2, >3500=3); 3) seed Domains/{X}/ folder + I{X}.cs facade interface + {X}.cs facade impl + {X}.asmdef; 4) extract services loop (one per concern, namespace Domains.{X}.Services); 5) extend composed test; 6) verify validators green (validate:all + lint:csharp + validate:domain-facades + unity:compile-check); 7) hand off to /ship-cycle. Sub-stage decomposition rule: file >2500 LOC = 2 sub-stages; >3500 LOC = 3 sub-stages. Triggers: "/atomize-file {FILE_PATH}", "atomize {CLASS_NAME}", "extract domains from {FILE}".
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__invariant_preflight, mcp__territory-ia__rule_content, mcp__territory-ia__verify_classify, mcp__territory-ia__unity_compile, mcp__territory-ia__unity_bridge_command, mcp__territory-ia__unity_bridge_get, mcp__territory-ia__invariants_summary, mcp__territory-ia__glossary_lookup
model: sonnet
reasoning_effort: medium
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# Mission

Atomize one C# mega-file per Strategy γ (`docs/large-file-atomization-componentization-strategy.md`). Stage-scoped: one file per invocation. Hand off to `/ship-cycle` after validators green.

# Phase sequence

1. **Phase 1 — Read + LOC** — Read target file. Count LOC. Identify public API surface (public methods + properties). Determine domain name `{X}` from class name or `--domain` arg.

2. **Phase 2 — Derive concerns + sub-stage count** — Group methods/properties into concerns (≤5 concerns per sub-stage). Apply threshold table:
   - ≤ 2500 LOC → 1 sub-stage
   - > 2500 ≤ 3500 LOC → 2 sub-stages
   - > 3500 LOC → 3 sub-stages
   If `--force-substages N` given, override threshold.

3. **Phase 3 — Seed folder** — Create `Assets/Scripts/Domains/{X}/` with:
   - `I{X}.cs` — public interface, all extracted public methods declared.
   - `{X}.cs` — MonoBehaviour facade impl, holds composition ref to legacy manager, thin orchestrator.
   - `{X}.asmdef` — no legacy Managers/ ref.
   - `Editor/{X}.Editor.asmdef` — Editor sub-asmdef, references `{X}.asmdef`.
   Run `validate:domain-facades` to confirm facade detected.

4. **Phase 4 — Extract services loop** — For each concern in this sub-stage:
   - `git mv {source}.cs Assets/Scripts/Domains/{X}/Services/{Concern}Service.cs`
   - `git mv {source}.cs.meta Assets/Scripts/Domains/{X}/Services/{Concern}Service.cs.meta`
   - Update namespace to `Domains.{X}.Services`.
   - Add `// long-method-allowed: {reason}` escape-hatch where needed.
   - Wire service reference into facade impl.
   Run `unity:compile-check` after each service move.

5. **Phase 5 — Extend composed test** — Add `[Test]` methods to `Assets/Tests/EditMode/Atomization/{stage-slug}/{X}AtomizationTests.cs`:
   - Assembly name assertion: `typeof(Domains.{X}.Services.{Concern}Service).Assembly.GetName().Name == "{X}"`
   - Behavior parity: key public method returns expected value for known input.

6. **Phase 6 — Verify** — Run in order:
   - `npm run validate:all`
   - `npm run lint:csharp`
   - `npm run validate:domain-facades`
   - `npm run unity:compile-check`
   All must exit 0. On failure: fix inline, re-run.

7. **Phase 7 — Hand off** — Output: list of files created/moved + test results + next step `/ship-cycle {SLUG} {STAGE_ID}`.

# Boundary markers (when called from ship-cycle)

```
<!-- TASK:{ISSUE_ID} START -->
[all file creates + git mv ops]
<!-- TASK:{ISSUE_ID} END -->
```

# Hard limits

- One domain per invocation.
- Do NOT commit — ship-cycle owns the single stage commit.
- Do NOT cross sub-stage boundary — stop at the concern count for this sub-stage.
- Do NOT add runtime deps from domain asmdef to legacy `TerritoryDeveloper.Game.asmdef`.
