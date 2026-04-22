---
purpose: "Canonical 9-point rubric for §Plan Digest. Enforced by plan_digest_lint MCP tool (Q9 decision 2026-04-22)."
audience: agent
loaded_by: ondemand
slices_via: none
alwaysApply: false
---

# Plan-Digest Contract — 9-point rubric

Applies to every §Plan Digest section (per-Task) and every compiled aggregate stage doc.

A plan is "digested" iff **all 9** hold:

1. **Zero open picks.** No "user decides", "user picks", "likely", "probably", "we could", "might", "consider", "TBD", "up to you", "your call".
2. **Every path verified against HEAD.** Repo-relative paths resolve via `plan_digest_verify_paths`.
3. **Every edit has concrete before-string + after-string.** No "update the narrative". Creates use verbatim new-file content. Deletes name the exact path.
4. **Before-strings are unique.** `plan_digest_resolve_anchor` returns exactly 1 hit per (file, before) pair.
5. **Every step has a gate command** with a stated pass criterion (exit 0 / zero matches / prints `OK`).
6. **Sequential only** — no parallelization block; if order-free, pick an order.
7. **Scope-narrowed** — "11 lines" must become N exact anchors, not N generic hits.
8. **Meta-stripped** — no audit history, no user-pick prose, no "human only" asides.
9. **STOP condition per step** — what to do if the gate fails (re-open which edit, or escalate to which upstream surface).

`plan_digest_lint` returns `{pass: boolean, failures: [{rule: 1..9, where, detail}]}`. Digester cap = 1 retry; second failure → abort chain + surface first failures verbatim.

## Enforcement

- `plan_digest_lint` runs on every per-Task §Plan Digest slice AND on the aggregate stage doc.
- `plan-review` runs AFTER `plan-digest` — its drift scan consumes the final §Plan Digest, not §Plan Author.

## Cross-references

- `ia/skills/plan-digest/SKILL.md` — the skill that authors §Plan Digest.
- `ia/templates/plan-digest-section.md` — section shape template.
- `ia/rules/plan-apply-pair-contract.md` — §Plan Digest is a Stage-scoped non-pair output alongside §Plan Author / §Audit.
