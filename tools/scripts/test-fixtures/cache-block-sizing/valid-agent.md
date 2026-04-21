---
name: fixture-valid-agent
description: Fixture — valid agent body with cache_control block above F2 floor.
tools: Read, Edit
model: opus
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/rules/invariants.md
@ia/rules/terminology-consistency.md
@ia/rules/agent-output-caveman.md
@ia/rules/project-hierarchy.md
@ia/rules/orchestrator-vs-spec.md
@ia/rules/unity-invariants.md
@ia/rules/plan-apply-pair-contract.md

# Mission

Valid fixture. Token count from @-loaded rules clears Opus 4.7 F2 floor (4,096 tok).
