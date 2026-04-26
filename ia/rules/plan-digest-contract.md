---
purpose: "Canonical 9-point rubric for §Plan Digest. Enforced by plan_digest_lint MCP tool. Relaxed shape — intent over verbatim code."
audience: agent
loaded_by: ondemand
slices_via: none
alwaysApply: false
---

# Plan-Digest Contract — 9-point rubric (relaxed shape)

Applies to every §Plan Digest section (per-Task) authored by `/stage-authoring`.

The digester's job is to resolve **decisions** — picks, paths, names, design pivots — and to pin **behavior** — §Acceptance + §Test Blueprint intents. The implementer's job is to translate decisions into byte-level edits against current HEAD. Verbatim before/after code blocks are NOT a digest deliverable.

A plan is "digested" iff **all 9** hold:

1. **Zero open picks.** No "user decides", "user picks", "likely", "probably", "we could", "might", "consider", "TBD", "up to you", "your call". Resolved picks live in §Pending Decisions; deferred picks live in §Implementer Latitude.
2. **Every path verified against HEAD.** Repo-relative paths under §Work Items resolve via `plan_digest_verify_paths`. Creates exempted (target path may not yet exist).
3. **Every §Work Items row has an explicit intent.** Format `{path}: {1-line what + why}`. No "update the narrative" / "do the right thing" / "etc". Each row is one file target.
4. **§Pending Decisions covers every non-trivial pick the implementer would otherwise face.** Helper choice, name choice, type choice, path choice, behavior pivot. Implementer Latitude rows must each cite their bounding constraint (invariant id or §Acceptance row).
5. **One §Invariants & Gate block.** Carries `invariant_touchpoints`, `validator_gate`, `escalation_enum`, **Gate:**, **STOP:**. Per-step variants forbidden — implementer applies all work items then runs the single gate.
6. **No parallelization prose.** §Work Items is unordered intent; implementer sequences. The body MUST NOT contain "in parallel", "|| true", "if-then-else" branching.
7. **Scope-narrow §Acceptance.** Each row is one observable behavior — concrete, glossary-aligned, gate-able by code-review or verify-loop. No vague "improve X" / "polish Y".
8. **Meta-stripped.** No audit history, no user-pick prose, no "human only" asides, no migration narrative, no aggregate-doc references.
9. **Single STOP route.** §Invariants & Gate carries one **STOP:** clause naming the escalation triggers (anchor mismatch / acceptance unmet / invariant regression / validator fail). Per-step STOP forbidden.

`plan_digest_lint` returns `{pass: boolean, failures: [{rule: 1..9, where, detail}]}`. Digester cap = 1 retry; second failure → abort chain + surface first failures verbatim.

## Section heading literal — `§Plan Digest`

Parent heading is `## §Plan Digest` (section-symbol `§`, U+00A7). All MCP reads + writes use the literal `§Plan Digest` string verbatim:

- Author / re-author: `task_spec_section_write({task_id, section: "§Plan Digest", body})`.
- Read (readiness gate, implementer load, code-review, audit): `task_spec_section({task_id, section: "§Plan Digest"})`.

`task_spec_section` does case-fold + trim match only — it does NOT strip `§`. Querying with bare `"Plan Digest"` returns `section_not_found` even when the body is populated. Every reader skill MUST pass the `§` prefix.

## Required sub-sections (in order)

1. `### §Goal`
2. `### §Acceptance`
3. `### §Pending Decisions`
4. `### §Implementer Latitude`
5. `### §Work Items`
6. `### §Test Blueprint`
7. `### §Invariants & Gate`

§Examples is **optional** — include only when a boundary case is non-obvious from §Acceptance + §Test Blueprint. Default omit.

## Backwards compatibility

Legacy digests with `### §Mechanical Steps` (numbered steps + per-step Edit tuples + verbatim before/after blocks + per-step Gate/STOP) remain valid in DB and continue to ship via `/ship-stage` Pass A and `/ship` Phase 2 unchanged. Implementers detect shape by sub-heading presence:

- `§Work Items` present → relaxed-shape flow (locate anchors against HEAD, decide ops, single gate).
- `§Mechanical Steps` present → legacy flow (apply pre-rendered Edit tuples in order, per-step gates).

New authoring (`/stage-authoring` post-relaxation) writes the relaxed shape. No migration of existing DB rows.

## Lint surface

`plan_digest_lint` enforces marker presence + path existence + pick-word absence — it does NOT inspect code-block content or per-step structure. Both shapes (legacy + relaxed) PASS the same lint when markers (`**Edits:**`, `**Gate:**`, `**STOP:**`) appear at least once in the body.

## Enforcement

- `plan_digest_lint` runs on every per-Task §Plan Digest slice.
- `mechanicalization_preflight_lint` runs on the same slice (one §Invariants & Gate block satisfies tuple-count regex; `anchor_hint:` token in §Work Items satisfies anchor check).
- `plan-review` (`plan-reviewer-mechanical` + `plan-reviewer-semantic`) runs AFTER `stage-authoring` — drift scan consumes the final §Plan Digest written directly by `stage-authoring`.

## Cross-references

- `ia/skills/stage-authoring/SKILL.md` — Stage-scoped bulk skill that authors §Plan Digest direct.
- `ia/templates/plan-digest-section.md` — section shape template (relaxed).
- `ia/skills/project-spec-implement/SKILL.md` — implementer reads both shapes; resolves anchors against HEAD on relaxed shape.
- `ia/rules/plan-apply-pair-contract.md` — §Plan Digest is a Stage-scoped non-pair output alongside §Audit.
