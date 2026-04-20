# Cursor User Rules — copy-paste handoff

**Purpose:** Cursor **User Rules** (global) live outside this repo. Paste the block below into **Cursor Settings → Rules for AI** if you want the same lifecycle hints in every workspace.

**Source of truth:** `.cursor/rules/cursor-lifecycle-adapters.mdc` (project rule, `alwaysApply: true`). When adapters change, refresh this doc or re-copy from that file.

---

## Block to paste (User Rules)

```markdown
## Territory Developer — validate:all / verify-loop (repo-wide master plans)

- `npm run validate:all` includes `validate:master-plan-status`, which validates **all** `ia/projects/*master-plan*.md` files, not only the orchestrator tied to the current `/ship-stage` or task.
- On failure during `/verify-loop` or post-stage verification: run `npm run validate:master-plan-status` alone, read output for **which** plan and row failed, fix or ticket that plan separately. Do not assume the active stage caused the drift.

## Territory Developer — ship-stage (Cursor + lifecycle)

- prerequisites (typical multi-task Stage, F6 re-fold 2026-04-20): `/stage-file` (chain tail owns `plan-author` + `plan-reviewer` → `plan-fix-applier` on critical, re-entry cap=1 → STOP) → `/ship-stage`. **`/ship-stage` does not run plan-author or plan-review** — specs arrive with `§Plan Author` populated + plan-review PASS. Standalone `/author` + `/plan-review` remain valid for ad-hoc re-author / recovery.
- caller_agent: `ship-stage` for mutation calls.
- chain strategy (align with `ia/skills/ship-stage/SKILL.md`): **Pass 1** — for each non-Done task in table order: `spec-implementer` → `npm run unity:compile-check` → atomic per-task commit. **Pass 2** (after all Pass 1 tasks): `verify-loop` on cumulative delta → Stage-level `opus-code-reviewer` → `opus-audit` → `stage-closeout-planner` → `stage-closeout-applier`. (Task-local seams inside `spec-implementer` follow that skill; this bullet is the chain dispatcher shape only.)
- `--per-task-verify`: legacy escape — promotes full `verify-loop` + `opus-code-reviewer` **per task** in Pass 1; Pass 2 skips batched verify + stage code-review (audit + closeout unchanged). Prefer when Stage has many tasks or very wide surface.
- checkpoint rule: stop after each sub-skill and summarize status before continuing.
- recommended model: Max/Opus for planner/review steps; Sonnet/Composer2 acceptable for apply steps.
- caveat: for long stages, split by task and open fresh session between tasks, or use `--per-task-verify`.
- caveat (Cursor): host may emit “suggested commit” text without an actual git commit — verify `git status` and commit before PR.
```

**Optional:** Durable project memory also lives in repo root `MEMORY.md` (Architecture decisions / CLI tips) — no need to duplicate in User Rules unless you want agents to see it without opening the repo.
