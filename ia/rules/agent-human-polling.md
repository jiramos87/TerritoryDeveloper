---
purpose: Agents polling the human must phrase questions and option labels in product/domain terminology, not IA/tooling jargon
audience: agent
loaded_by: ondemand
slices_via: none
description: User-gate prompts (approach select, disambiguation, disagreement, scope confirmation) must lead with game/feature semantics so the human decides on product tradeoffs, not internal plumbing.
alwaysApply: false
---

# Agent human-polling — product-easy terminology

Scope: every moment an agent, subagent, skill, or slash command pauses execution and polls the human for a decision — approach-select gate, ambiguity resolution, disagreement prompt, scope confirmation, `AskUserQuestion` call.

## Rule

- **Question stem** leads with the user-visible effect on the game or feature ("When the player's police budget runs out, should new stations be blocked or just warned?").
- **Option labels** describe the outcome the player/designer will observe ("Roads route around existing landmarks" beats "Approach B — landmark-first ordering in stage 1.2").
- **Ids, paths, cell coordinates, yaml filenames, skeleton-step numbers** go on a trailing `Context:` line — never inside the question text or labels.
- **Zero code identifiers** in question text: no class names, method signatures, MCP tool names, C# types, Unity-specific internals. Those are implementation details the agent resolves on its own.
- **Game-design vocabulary only** when the gate is about a feature tradeoff. IA/tooling vocabulary is allowed only when the gate is itself about IA/tooling (e.g. `/closeout` spec deletion confirmation — see exceptions).

## Why

User reviews many agent polls per session. Technical IA wording forces re-entry into orchestrator mental model per decision — slows gating, hides the actual game-design tradeoff, and makes "does this approach match the product vision" the wrong kind of hard. Product-easy wording lets the user decide on game semantics in one pass.

Also: cold-pasted polls (via subagent handoff, session resume) must be decidable without rereading the parent master plan, exploration doc, or tracker.

## How to apply

- Before emitting a poll, rewrite: strip all ids/paths/internal-only terms from question + labels; move them to a trailing `Context:` line if still needed for audit.
- Replace approach codenames (`A`, `B`, `approach-1`) with outcome labels: `Landmark-first` → `Landmarks generate before roads` (option stem is the result, not the ordering).
- If the gate is about splitting/merging stages, scope changes, or skeleton re-decomposition — translate to what the player/designer sees change, not which stage number moves.
- If the only honest framing requires an id (e.g. "should TECH-312 close alongside TECH-318?"), keep the ids but pair with one-line domain recap so the user does not have to look them up.

## Good / bad

| Bad (IA/tooling jargon) | Good (product-easy) |
|---|---|
| "Select approach A vs B for step 2 decomposition" | "Should landmarks generate before roads, or roads first and landmarks route around?" |
| "Cardinality gate failed on Stage 2.1 — pause, split, merge, or justify?" | "Stage has 6 tasks — user-visible checkpoints still feel like one chunk. Split into two releasable slices or ship as one?" |
| "Row `zone-s-economy` at cell (b) marker ⚠ — resolve disagreement?" | "Zone-S economy design disagrees with umbrella on tax ordering — pick: monthly tick first, or player-triggered first?" |
| "`BudgetAllocationService.TryDraw()` should check treasury floor?" | "When the player's budget runs out, should new buildings be blocked entirely, or just warned?" |

## Exceptions — technical wording allowed

These gates are themselves about tooling, not game tradeoffs:

1. **Destructive-op confirmations** — `/closeout` delete prompts (must name exact spec path + id), force-push warnings.
2. **Verification block failures** — need exact ids, file paths, line numbers for debugging.
3. **Bridge / compile error reports** — stack traces, class / method names are the payload.
4. **MCP tool / server misconfiguration prompts** — naming the failing tool is the point.
5. **Path-A vs Path-B verification choice** — internal policy decision, canonical terminology required (see `docs/agent-led-verification-policy.md`).

When exceptions apply, still keep the question stem short and put supporting ids/paths on trailing context lines.

## Where to apply

- `ia/skills/design-explore/SKILL.md` — Phase 0.5 interview, Phase 2 select gate, gap-analysis stops.
- `ia/skills/master-plan-new/SKILL.md` — any clarification prompt during orchestration.
- `ia/skills/master-plan-extend/SKILL.md` — Phase 0 source-doc stop, extension scope gate.
- `ia/skills/stage-decompose/SKILL.md` — decomposition ambiguity, cardinality gate verdict = `pause`.
- `ia/skills/release-rollout/SKILL.md` — per-cell dispatch disagreement gate, row pick under ambiguity.
- `ia/skills/stage-file/SKILL.md` — cardinality gate verdict = `pause`.
- Any future skill / subagent invoking `AskUserQuestion`.

## Relation to other rules

- `ia/rules/agent-output-caveman.md` — caveman is the default agent voice; this rule refines voice for human-polling surfaces so terseness does not land as jargon density.
- `ia/rules/terminology-consistency.md` — glossary terms stay canonical when they ARE the product vocabulary (`HeightMap`, `wet run`, `road stroke`); use them. This rule targets internal-plumbing terms (cell coords, yaml records, stage numbers, approach codes, id prefixes) that leak tooling.
- `docs/agent-lifecycle.md` — lifecycle gates that poll the human land here.
