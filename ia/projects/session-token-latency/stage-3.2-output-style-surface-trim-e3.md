### Stage 3.2 — Output-style surface trim (E3)


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Trim `.claude/output-styles/verification-report.md` from 87 lines to ≤35 lines by extracting field semantics to `docs/agent-led-verification-policy.md`. Audit and trim `closeout-digest.md`. Verify all verifier + closeout subagent dispatches still parse output styles correctly.

**Exit:**

- `.claude/output-styles/verification-report.md`: ≤35 lines; keeps Part 1 (JSON header) + Part 2 (caveman summary) structure + minimal example block; field-semantic prose moved to `docs/agent-led-verification-policy.md` §Verification output fields.
- `.claude/output-styles/closeout-digest.md`: ≤50 lines; audited; trimmed if over budget.
- Verifier + stage-closeout-applier subagents dispatch with correct output style shape (no regression).
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.2.1 | Trim verification-report.md | _pending_ | _pending_ | Read `.claude/output-styles/verification-report.md` (87 lines); extract field-semantic prose (per-field descriptions, JSON schema commentary) to new `docs/agent-led-verification-policy.md` §Verification output fields sub-section. Retain in file: brief purpose line, Part 1 JSON header shape (≤10 lines), Part 2 caveman summary shape (≤5 lines), one canonical example (≤15 lines). Target ≤35 lines. Update `verifier.md` + `verify-loop.md` agent bodies if they inline-reference line numbers. |
| T3.2.2 | Trim closeout-digest.md + validate | _pending_ | _pending_ | Read `.claude/output-styles/closeout-digest.md`; if > 50 lines apply same trim pattern (extract semantics to `docs/agent-led-verification-policy.md` §Closeout digest output fields). Update `stage-closeout-applier.md` if it references specific lines. Run full `/verify` + `/closeout` dispatch dry-run to confirm output shapes parse correctly. `npm run validate:all` green. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
