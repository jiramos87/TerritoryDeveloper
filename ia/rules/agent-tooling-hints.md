---
purpose: Shell, editor, smoke preflight, and IA test-harness notes for agents editing tools/scripts
audience: agent
loaded_by: ondemand
slices_via: none
description: macOS shell quirks, Node/ts-node script tips, smoke preflight, Jest registry patterns, IA_COUNTER env overrides
alwaysApply: false
---

# Agent tooling hints (on-demand)

Fetch via `rule_content agent-tooling-hints` when editing `tools/scripts`, validators, smoke tests, web deploy preflight — not force-loaded.

## Verification + preflight

- **Smoke specs:** run Phase 0 precondition checks before HTTP probes — blocked deploy misreads as smoke failure.
- **Web / Vercel parity:** web-platform preflight → `git status --porcelain web/` — untracked `web/` files break deploy parity silently.
- **Validator failures:** `npm run validate:dead-project-specs` / `validate:all` / any validator non-zero → capture **full stdout + stderr**, parse reported paths/rows before diagnosing. Don't infer cause from nearby issue ids / grep noise.
- **Failure ownership:** before naming `BUG-`/`FEAT-`/`TECH-`/… id as **owner** of failure/flake → confirm **open** in `BACKLOG.md`. Missing / only in `BACKLOG-ARCHIVE.md` → report pre-existing / unowned; never cite closed issue as current owner.

## Shell + scripting

- npm scripts: `npm --prefix {dir} run …` over `cd {dir} && …`.
- macOS `sed` lacks `\b` word boundary — use `perl -pi -e 's/\bFoo\b/Bar/g'` for in-place word edits.
- macOS `flock` (util-linux) may be absent on npm/sandbox PATH; Node callers of `reserve-id.sh` → `buildScriptEnv()` (or equivalent) so script sees lock helper.
- Node ≥22 + ts-node: avoid `const enum` — prefer `as const` object + derived type alias in `tools/scripts/*.ts`.
- `__dirname` depth can shift when ts-node reparses as ESM — verify path depth before `resolve(SCRIPT_DIR, …)`.

## IDE multi-file edit (search_replace)

- Replacing `for` loop header: include **full** loop body in `old_string` so matcher stays unique.

## IA / MCP test harness

- Registry-count tests: prefer `toBeGreaterThanOrEqual` over exact `toBe` — counts grow with fixtures.
- Smoke / integration tests mutating id-counter state: expose `IA_COUNTER_FILE` / `IA_COUNTER_LOCK` env overrides day-one — parallel runs + cleanup stay safe.
