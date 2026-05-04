---
purpose: "Force-loaded canonical rule for TDD red/green methodology — every Stage ships a failing test before implementation (red), a passing test after (green). §Red-Stage Proof 4-field schema + anchor grammar + enum tables locked from DEC-A23. Companion to prototype-first-methodology.md."
audience: agent
loaded_by: on-demand
alwaysApply: false
---

# TDD red/green methodology

## Driving intent

Every Stage's player-visible delta = a failing test before any code (red), a passing test after (green). Stage 1.0 §Tracer Slice = the first red→green cycle of a master plan. Stages 2+ §Visibility Delta each own ≥1 red→green pair scoped to the new visible behavior. Pre-impl test-run blob is captured at Pass A entry gate via `red_stage_proof_capture` MCP — no commit-ordering walk required; survives squash.

Adopted from DEC-A23 (`docs/tdd-red-green-methodology-exploration.md` §Design Expansion). Layers onto `prototype-first-methodology.md` without overwriting §Tracer Slice 5-field block or §Visibility Delta contract (zero retrofit cost on existing plans).

## §Red-Stage Proof — 4-field schema

Mandatory block on every Stage that carries a player-visible delta. Schema fields:

| Field | Semantics |
|---|---|
| `red_test_anchor` | Resolved anchor string in one of the 4 canonical grammar forms (see §Anchor grammar below). Non-empty. |
| `target_kind` | Which visibility category the proof covers. See `target_kind` enum table. |
| `proof_artifact_id` | UUID assigned by `red_stage_proof_capture` when it writes the `ia_red_stage_proofs` row. Non-empty after capture. |
| `proof_status` | Current status of the proof row. See `proof_status` enum table. |

## §Anchor grammar — 4 canonical forms

All `red_test_anchor` values must match exactly one of:

| Form | Pattern | Example |
|---|---|---|
| Tracer verb test | `tracer-verb-test:{path}::{method}` | `tracer-verb-test:tools/mcp-ia-server/tests/tools/red-stage-proof.test.ts::TracerCapturesUnexpectedPass` |
| Visibility delta test | `visibility-delta-test:{path}::{method}` | `visibility-delta-test:Assets/Tests/PlayMode/Economy/BudgetPanelDeltaTest.cs::ShowsNewLineItem` |
| Bug repro test | `BUG-NNNN:{path}::{method}` | `BUG-1042:Assets/Tests/EditMode/Economy/NegativeBalanceTest.cs::ReproducesOverdraft` |
| Not applicable | literal `n/a` | `n/a` (design-only Stages — `target_kind=design_only`) |

`{path}` = repo-relative file path. `{method}` = exact test method name. Anchor must resolve to a real test file at Pass A entry gate (except `n/a`).

## `target_kind` enum

| Value | When |
|---|---|
| `tracer_verb` | Stage 1.0 §Tracer Slice — proof covers the tracer verb end-to-end. |
| `visibility_delta` | Stages 2+ §Visibility Delta — proof covers the new player-visible behavior. |
| `bug_repro` | Bugfix Stages — proof = failing regression test reproducing the defect. §Visibility Delta requirement waived (Q3=B). |
| `design_only` | Architecture-lock / dependency-graph / plumbing-only Stages with no new player-visible behavior. `red_test_anchor` = `n/a`. |

## `proof_status` enum

| Value | Meaning |
|---|---|
| `pending` | Proof row created; `red_stage_proof_capture` not yet invoked. |
| `failed_as_expected` | Pre-impl test run returned non-zero exit; gate passed; implementation may proceed. |
| `unexpected_pass` | Pre-impl test run returned zero (test already green). Pass A REJECTS — stop; investigate false green. |
| `not_applicable` | `target_kind=design_only`; no test run required; capture skipped. |

## `command_kind` allowlist

`red_stage_proof_capture` MCP accepts only:

| Value | Runner |
|---|---|
| `npm-test` | `npm test` / `npm run test:mcp` |
| `dotnet-test` | .NET MSTest / xUnit via `dotnet test` |
| `unity-testmode-batch` | Unity batch-mode TestRunner via `npm run unity:testmode-batch` |

No other runner may be spawned. Expanding the allowlist requires a new DEC-A23 amendment.

## Pass A entry gate

Before any implementation code is written:

1. `red_stage_proof_capture` MCP invoked with `{slug, stage_id, target_kind, anchor, proof_artifact_id, command_kind}`.
2. Tool resolves anchor → test file + method; spawns allowlisted runner; captures stdout/stderr blob.
3. If `proof_status = unexpected_pass` → tool returns rejection envelope (`ok: false`). Pass A **STOPS**. Escalate to caller; do not silently adapt or skip.
4. If `proof_status = failed_as_expected` → capture row written to `ia_red_stage_proofs`; implementation proceeds.
5. If `target_kind = design_only` → `proof_status` set `not_applicable`; gate skipped.

Pass B `verify-loop` re-runs the anchored test; asserts green; calls `red_stage_proof_finalize` (Stage 2+) to set `green_status = passed`.

## Validator gate

`npm run validate:plan-red-stage` — CI gate (shipped Stage 3). Asserts: (i) every non-`design_only` Stage with a player-visible delta carries a non-empty `red_test_anchor`; (ii) anchor resolves to a real test file; (iii) `target_kind` value is in the enum. CI red blocks merge on missing proof anchor.

## Retrofit policy

Forward-only enforcement. Pre-Stage 6 master plans (including all plans created before the `0062_master_plan_grandfathered` migration applies) are **exempt** from the §Red-Stage Proof gate.

Exemption is tracked via `tdd_red_green_grandfathered BOOLEAN` on `ia_master_plans`:
- Migration `0062` backfills `TRUE` for every plan that exists at apply time (covers all in-flight plans, including `tdd-red-green-methodology` Stages 1.0–5).
- New plans inserted after migration apply default to `FALSE` → gate enforced.

Both `validate:plan-red-stage` and the Pass A entry gate (`red_stage_proof_capture` call site in `/ship-stage`) honor the flag:
- Grandfathered plan → emit `{slug, grandfathered: true, skipped: true}` log line; exit 0 / skip capture.
- Non-grandfathered plan → full proof check applies; missing block = violation.

The first non-grandfathered pilot is the plan authored as part of TECH-10908 (Stage 6 closeout pilot).

See §Driving intent above for the cross-link to this policy.

## Cross-links

- `prototype-first-methodology.md` — D1/D7/D8/D9/D10 anchor; this methodology layers onto without overwriting.
- `docs/tdd-red-green-methodology-exploration.md` — DEC-A23 source decisions Q1=C, Q2=D, Q3=B, Q4=C+prototype-first, Q5=B.
- `ia/rules/plan-digest-contract.md` — §Test Blueprint optional `red_proof:` clause (Stage 2 ship surface).
- DEC-A23 — `arch_surface_resolve({slug: "rules/tdd-red-green-methodology"})` returns this file.
