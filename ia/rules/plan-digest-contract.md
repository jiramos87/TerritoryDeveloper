---
purpose: "Canonical 11-point rubric for §Plan Digest (10 contract + per-section soft caps). Injected verbatim into stage-authoring Opus prompt as hard constraints. Relaxed shape — intent over verbatim code."
audience: agent
loaded_by: ondemand
slices_via: none
alwaysApply: false
---

# Plan-Digest Contract — 11-point rubric (relaxed shape, rubric-in-prompt)

Applies to every §Plan Digest section (per-Task) authored by `/stage-authoring`.

Digester's job: resolve **decisions** — picks, paths, names, design pivots — + pin **behavior** — §Acceptance + §Test Blueprint intents. Implementer's job: translate decisions into byte-level edits against current HEAD. Verbatim before/after code blocks NOT a digest deliverable.

Plan = "digested" iff rules **1–10** hold (hard); rule **11** soft (warn-only):

1. **Zero open picks.** No "user decides", "user picks", "likely", "probably", "we could", "might", "consider", "TBD", "up to you", "your call". Resolved picks → §Pending Decisions; deferred picks → §Implementer Latitude.
2. **Every path verified against HEAD.** Repo-relative paths under §Work Items resolve via `plan_digest_verify_paths`. Creates exempted (target path may not yet exist).
3. **Every §Work Items row has explicit intent.** Format `{path}: {1-line what + why}`. No "update the narrative" / "do the right thing" / "etc". Each row = one file target.
4. **§Pending Decisions covers every non-trivial pick implementer would otherwise face.** Helper choice, name choice, type choice, path choice, behavior pivot. Implementer Latitude rows must each cite bounding constraint (invariant id or §Acceptance row).
5. **One §Invariants & Gate block.** Carries `invariant_touchpoints`, `validator_gate`, `escalation_enum`, **Gate:**, **STOP:**. Per-step variants forbidden — implementer applies all work items then runs single gate.
6. **No parallelization prose.** §Work Items = unordered intent; implementer sequences. Body MUST NOT contain "in parallel", "|| true", "if-then-else" branching.
7. **Scope-narrow §Acceptance.** Each row = one observable behavior — concrete, glossary-aligned, gate-able by code-review or verify-loop. No vague "improve X" / "polish Y".
8. **Meta-stripped.** No audit history, no user-pick prose, no "human only" asides, no migration narrative, no aggregate-doc references.
9. **Single STOP route.** §Invariants & Gate carries one **STOP:** clause naming escalation triggers (anchor mismatch / acceptance unmet / invariant regression / validator fail). Per-step STOP forbidden.
10. **EARS prefix required on every §Acceptance row** (unless `plan.ears_grandfathered=TRUE` — plans created before Wave B; `validate:plan-digest-coverage` skips check). Each row begins with one of 5 EARS patterns (case-insensitive):
    - **WHEN** / **THE** (ubiquitous — always-applies behavior)
    - **WHEN ... IF** (event-driven — conditional event trigger)
    - **WHILE** (state-driven — continuous state condition)
    - **IF ... THEN** (unwanted-behavior — error / exception path)
    - **WHERE** (optional-feature — capability-conditional behavior)

    Example valid rows: `WHEN the user invokes /spec-freeze THE tool rejects open_questions_count > 0.` / `IF frozen_at IS NULL THEN /ship-plan emits spec_not_frozen escalation.` Rows starting with any other prefix → `validate:plan-digest-coverage` exits 1. Rows without any prefix → exit 1. Skip-exemption: `plan.ears_grandfathered=TRUE` bypasses enforcement entirely.

11. **Per-section soft byte caps (warn-only).** §Goal ≤400 B · §Acceptance ≤1500 B · §Pending Decisions ≤1500 B · §Implementer Latitude ≤800 B · §Work Items ≤2000 B · §Test Blueprint ≤1000 B · §Invariants & Gate ≤800 B; total target ≈8 KB. Overrun → emit `n_section_overrun` counter in handoff; do NOT abort.

Rubric is injected verbatim into the `/stage-authoring` Phase 4 Opus authoring prompt as hard constraints. NO post-author `plan_digest_lint` MCP call. NO retry loop. Author measures byte length per sub-section after composition; overruns recorded as warnings, chain proceeds.

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

## Enforcement

- Rubric injected verbatim into `/stage-authoring` Phase 4 Opus authoring prompt as hard constraints.
- NO post-author `plan_digest_lint` MCP call. NO retry loop.
- Author self-measures byte length per sub-section after composition; overruns recorded as `n_section_overrun` warnings in handoff; chain proceeds.
- Pick-word scan + path verification happen in-prompt under rules 1–2; no separate lint pass.
- EARS prefix enforcement (rule 10) runs via `validate:plan-digest-coverage` (exits 1 on violation); skipped when `plan.ears_grandfathered=TRUE`.

## Cross-references

- `ia/skills/stage-authoring/SKILL.md` — Stage-scoped bulk skill authoring §Plan Digest direct.
- `ia/templates/plan-digest-section.md` — section shape template (relaxed).
- `ia/skills/project-spec-implement/SKILL.md` — implementer reads both shapes; resolves anchors against HEAD on relaxed shape.
- `ia/rules/plan-apply-pair-contract.md` — §Plan Digest = Stage-scoped non-pair output alongside §Audit.
