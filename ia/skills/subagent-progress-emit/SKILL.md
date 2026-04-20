---
purpose: "Cross-cutting progress-marker preamble — every lifecycle SKILL.md declares a top-level `phases:` YAML array; on entering Phase N, subagent emits one stderr line in the canonical shape so the parent agent (and user terminal) see realtime progress without log-file polling or MCP round-trip."
audience: agent
loaded_by: always
slices_via: none
name: subagent-progress-emit
description: >
  Defines canonical stderr progress-marker shape and the `phases:` frontmatter
  convention. @-loaded once by every `.claude/agents/*.md` common preamble so
  the emission contract is uniform across the surface. Non-lifecycle one-shot
  skills (glossary patchers, view regenerators) are exempt from both the
  frontmatter convention and the emission contract. Never introduces MCP
  round-trips or log-file polling.
model: inherit
phases: []
---

# Subagent progress emit

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

## Emission shape

One stderr line per phase entry, literal shape:

```
⟦PROGRESS⟧ {skill_name} {phase_index}/{phase_total} — {phase_name}
```

- `⟦ ⟧` — Unicode math brackets (U+27E6 / U+27E7). Reserved as canonical regex-stable delimiter. Forbidden in skill body prose outside the emission line itself — reserved purely so the parent agent's stderr reader can `grep -E '^⟦PROGRESS⟧'` without false positives from prose.
- `skill_name` — value of top-level `name:` field in the emitting skill's frontmatter.
- `phase_index` — 1-based index into the skill's `phases:` array for the phase being entered.
- `phase_total` — `phases.length` of the same array.
- `phase_name` — array element at index `phase_index − 1`, verbatim.

## Frontmatter convention

Every lifecycle `SKILL.md` carries a top-level `phases:` YAML array listing phase names in the exact order they execute in the skill body. Each array entry is a short human-readable label matching one `### Phase N — {label}` heading in the body. Example:

```yaml
phases:
  - "Load Stage context"
  - "Drift scan"
  - "Write §Plan Fix tuples"
  - "Hand-off"
```

Body headings MUST be `### Phase N — {label}` where `{label}` equals the array entry verbatim (trimmed). `validate:frontmatter` asserts 1:1 drift-free parity between array and body headings; mismatch fails strict mode.

## Emission contract

A subagent reads its own frontmatter `phases:` on boot. On entering each phase, it writes ONE stderr line in the canonical shape and no others. No stdout emission, no MCP tool call, no log-file write. The parent agent surfaces matching stderr lines verbatim to the user terminal.

Per-skill emission boilerplate is zero — the `@`-loaded common preamble handles the mechanics. Skills do not inline their own emission code; they simply carry the `phases:` array and let the preamble do the work.

## Scope — lifecycle vs non-lifecycle

**Lifecycle skills** (MUST carry `phases:` + emit markers):

- Pair-head / pair-tail: `plan-review`, `plan-fix-apply`, `stage-file-plan`, `stage-file-apply`, `project-new-apply`, `opus-audit`, `opus-code-review`, `code-fix-apply`, `stage-closeout-plan`, `stage-closeout-apply`.
- Non-pair bulk: `plan-author`.
- Existing executors: `project-spec-implement`, `verify-loop`, `ship-stage`, `stage-file`, `project-new`, `stage-compress`.

**Non-lifecycle one-shots** (EXEMPT):

- Glossary patchers, progress-view regenerators (`progress-regen`), release-rollout helpers (`release-rollout-enumerate`, `release-rollout-track`, `release-rollout-skill-bug-log`, `release-rollout-repo-sweep`, `rollout-row-state`), cross-cutting utility skills (`domain-context-load`, `cardinality-gate-check`, `term-anchor-verify`, `surface-path-precheck`, `ide-bridge-evidence`, `bridge-environment-preflight`, `project-implementation-validation`, `agent-test-mode-verify`, `close-dev-loop`, `stage-decompose`), authoring / retrospective skills (`design-explore`, `master-plan-new`, `master-plan-extend`, `skill-train`), and this skill itself (`subagent-progress-emit`).

Validator exemption: `check-frontmatter.mjs` only enforces the `phases:` ↔ body-heading parity on the lifecycle subset above. Non-lifecycle skills may declare `phases: []` (empty array) or omit the field entirely.

## Delimiter reservation

The token `⟦PROGRESS⟧` and the bracket pair `⟦ ⟧` are reserved across every file under `ia/skills/**/SKILL.md` body prose, agent bodies, command bodies, and rule docs. Prose MUST NOT use these characters for any other purpose. `validate:all` greps for off-contract occurrences outside the canonical emission line.

## No runtime state

This skill has no phases of its own. It is a preamble declaring a contract. `phases: []` in its frontmatter is deliberate — validators skip parity-check when array is empty.

## Changelog
