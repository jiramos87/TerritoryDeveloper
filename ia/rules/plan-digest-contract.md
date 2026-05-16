---
purpose: "Canonical 9-point rubric for §Plan Digest. Enforced by plan_digest_lint MCP tool. Relaxed shape — intent over verbatim code."
audience: agent
loaded_by: ondemand
slices_via: none
alwaysApply: false
---

# Plan-Digest Contract — 9-point rubric (relaxed shape)

Applies to every §Plan Digest section (per-Task) authored by `/stage-authoring`.

Digester's job: resolve **decisions** — picks, paths, names, design pivots — + pin **behavior** — §Acceptance + §Test Blueprint intents. Implementer's job: translate decisions into byte-level edits against current HEAD. Verbatim before/after code blocks NOT a digest deliverable.

Plan = "digested" iff **all 9** hold:

1. **Zero open picks.** No "user decides", "user picks", "likely", "probably", "we could", "might", "consider", "TBD", "up to you", "your call". Resolved picks → §Pending Decisions; deferred picks → §Implementer Latitude.
2. **Every path verified against HEAD.** Repo-relative paths under §Work Items resolve via `plan_digest_verify_paths`. Creates exempted (target path may not yet exist).
3. **Every §Work Items row has explicit intent.** Format `{path}: {1-line what + why}`. No "update the narrative" / "do the right thing" / "etc". Each row = one file target.
4. **§Pending Decisions covers every non-trivial pick implementer would otherwise face.** Helper choice, name choice, type choice, path choice, behavior pivot. Implementer Latitude rows must each cite bounding constraint (invariant id or §Acceptance row).
5. **One §Invariants & Gate block.** Carries `invariant_touchpoints`, `validator_gate`, `escalation_enum`, **Gate:**, **STOP:**. Per-step variants forbidden — implementer applies all work items then runs single gate.
6. **No parallelization prose.** §Work Items = unordered intent; implementer sequences. Body MUST NOT contain "in parallel", "|| true", "if-then-else" branching.
7. **Scope-narrow §Acceptance.** Each row = one observable behavior — concrete, glossary-aligned, gate-able by code-review or verify-loop. No vague "improve X" / "polish Y".
8. **Meta-stripped.** No audit history, no user-pick prose, no "human only" asides, no migration narrative, no aggregate-doc references.
9. **Single STOP route.** §Invariants & Gate carries one **STOP:** clause naming escalation triggers (anchor mismatch / acceptance unmet / invariant regression / validator fail). Per-step STOP forbidden.

`plan_digest_lint` returns `{pass: boolean, failures: [{rule: 1..9, where, detail}]}`. Digester cap = 1 retry; second failure → abort chain + surface first failures verbatim.

## Section heading literal — `§Plan Digest`

Parent heading = `## §Plan Digest` (section-symbol `§`, U+00A7). All MCP reads + writes use literal `§Plan Digest` string verbatim:

- Author / re-author: `task_spec_section_write({task_id, section: "§Plan Digest", body})`.
- Read (readiness gate, implementer load, code-review, audit): `task_spec_section({task_id, section: "§Plan Digest"})`.

`task_spec_section` does case-fold + trim match only — does NOT strip `§`. Querying with bare `"Plan Digest"` returns `section_not_found` even when body populated. Every reader skill MUST pass `§` prefix.

## Required sub-sections (in order)

1. `### §Goal`
2. `### §Acceptance`
3. `### §Pending Decisions`
4. `### §Implementer Latitude`
5. `### §Work Items`
6. `### §Test Blueprint`
7. `### §Invariants & Gate`

§Examples = **optional** — include only when boundary case non-obvious from §Acceptance + §Test Blueprint. Default omit.

## Backwards compatibility

Legacy digests with `### §Mechanical Steps` (numbered steps + per-step Edit tuples + verbatim before/after blocks + per-step Gate/STOP) remain valid in DB + continue to ship via `/ship-stage` Pass A + `/ship` Phase 2 unchanged. Implementers detect shape by sub-heading presence:

- `§Work Items` present → relaxed-shape flow (locate anchors against HEAD, decide ops, single gate).
- `§Mechanical Steps` present → legacy flow (apply pre-rendered Edit tuples in order, per-step gates).

New authoring (`/stage-authoring` post-relaxation) writes relaxed shape. No migration of existing DB rows.

## Lint surface

`plan_digest_lint` enforces marker presence + path existence + pick-word absence — does NOT inspect code-block content or per-step structure. Both shapes (legacy + relaxed) PASS same lint when markers (`**Edits:**`, `**Gate:**`, `**STOP:**`) appear at least once in body.

## Enforcement

- `plan_digest_lint` runs on every per-Task §Plan Digest slice.
- `plan-review` (`plan-reviewer-mechanical` + `plan-reviewer-semantic`) runs AFTER `stage-authoring` — drift scan consumes final §Plan Digest written directly by `stage-authoring`.

## Cross-references

- `ia/skills/stage-authoring/SKILL.md` — Stage-scoped bulk skill authoring §Plan Digest direct.
- `ia/templates/plan-digest-section.md` — section shape template (relaxed).
- `ia/skills/project-spec-implement/SKILL.md` — implementer reads both shapes; resolves anchors against HEAD on relaxed shape.
- `ia/rules/plan-apply-pair-contract.md` — §Plan Digest = Stage-scoped non-pair output alongside §Audit.
