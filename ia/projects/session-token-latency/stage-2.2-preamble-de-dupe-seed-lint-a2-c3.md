### Stage 2.2 — Preamble de-dupe + seed lint (A2 + C3)


**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-condition:** lifecycle-refactor Stage 10 T10.2 Done (caveman rule entered `ia/skills/_preamble/stable-block.md`; stable-block is in canonical ingestion path).

**Objectives:** Replace full-text caveman directive restatements across 13 subagent bodies, ~30 skill preambles, and slash-command seeds with a single canonical reference line. Ship `npm run validate:skill-seeds` as the CI gate preventing future seed-subagent drift.

**Exit:**

- All 13 subagent bodies (`.claude/agents/*.md`): caveman restatement replaced by `@ia/skills/_preamble/stable-block.md` or equivalent single-reference line (≤15 tokens each).
- All `ia/skills/*/SKILL.md` preamble sections: full-text caveman directive replaced by reference line where restated.
- All `.claude/commands/*.md` "forward verbatim" blocks: caveman directive stripped; forwarded parameters only.
- `npm run validate:skill-seeds` passes: every `Seed prompt` code block names an existing `.claude/agents/{name}.md` file + references files that exist on disk.
- `npm run validate:all` green.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T2.2.1 | Subagent preamble collapse | _pending_ | _pending_ | In all 13 `.claude/agents/*.md` bodies: locate full-text caveman directive block (typically the first 10–20 lines or `@`-loaded preamble section); replace with `@ia/skills/_preamble/stable-block.md` reference line (single line, ≤15 tokens). Verify behavior unchanged: stable-block already contains `agent-output-caveman` rule. Run `npm run validate:all`. |
| T2.2.2 | Skill + command preamble collapse | _pending_ | _pending_ | In all `ia/skills/*/SKILL.md` preamble sections where caveman directive is restated verbatim: replace with `Caveman default — see \`ia/skills/_preamble/stable-block.md\``. In `.claude/commands/*.md` "forward verbatim" blocks: strip caveman directive restatement; keep parameter-forwarding prose only. Spot-check 5 skills + 3 commands before/after. `npm run validate:all`. |
| T2.2.3 | validate:skill-seeds script | _pending_ | _pending_ | Author `tools/scripts/validate-skill-seeds.sh` (or Node equivalent): reads every `Seed prompt` fenced code block in `ia/skills/*/SKILL.md`; extracts subagent name + referenced file paths; asserts each subagent maps to existing `.claude/agents/{name}.md`; asserts each file path resolves on disk. Add `npm run validate:skill-seeds` to `package.json` + `validate:all` chain. |
| T2.2.4 | Seed-drift remediation | _pending_ | _pending_ | Run `npm run validate:skill-seeds`; fix any seed-subagent name drift or stale file references found (expected: subagent renames from lifecycle-refactor M3/M6 collapse — `spec-kickoff` → retired, `closeout` → `stage-closeout-planner`). Commit fixes. `npm run validate:all` green. |

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
