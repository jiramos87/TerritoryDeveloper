---
purpose: Shell, editor, smoke preflight, and IA test-harness notes for agents editing tools/scripts
audience: agent
loaded_by: ondemand
slices_via: none
description: macOS shell quirks, Node/ts-node script tips, smoke preflight, Jest registry patterns, IA_COUNTER env overrides
alwaysApply: false
---

# Agent tooling hints (on-demand)

Fetch via `rule_content agent-tooling-hints` when editing `tools/scripts`, validators, smoke tests, or web deploy preflight — not force-loaded.

## Verification + preflight

- **Smoke specs:** run Phase 0 precondition checks before HTTP probes — a blocked deploy can be misread as smoke failure.
- **Web / Vercel parity:** web-platform preflight should include `git status --porcelain web/` — untracked files under `web/` can break deploy parity silently.
- **Validator failures:** when `npm run validate:dead-project-specs`, `validate:all`, or any validator exits non-zero, capture **full stdout + stderr** and parse the reported paths/rows before diagnosing. Do not infer the cause from unrelated nearby issue ids or grep noise.
- **Failure ownership:** before naming a `BUG-`/`FEAT-`/`TECH-`/… id as the **owner** of a failure or flake, confirm it appears as **open** in `BACKLOG.md`. If the id is missing or only in `BACKLOG-ARCHIVE.md`, report as pre-existing / unowned — do not cite a closed issue as current owner.

## Shell + scripting

- npm scripts: `npm --prefix {dir} run …` over `cd {dir} && …`.
- macOS `sed` has no `\b` word boundary — use `perl -pi -e 's/\bFoo\b/Bar/g'` for in-place word edits.
- macOS `flock` from util-linux may be absent on the npm/sandbox PATH; Node callers of `reserve-id.sh` should use `buildScriptEnv()` (or equivalent) so the script sees the lock helper.
- Node ≥22 + ts-node: avoid `const enum` — prefer `as const` object + derived type alias in `tools/scripts/*.ts`.
- `__dirname` depth can change when ts-node reparses as ESM — verify path depth before `resolve(SCRIPT_DIR, …)`.

## IDE multi-file edit (search_replace)

- Replacing a `for` loop header: include the **full** loop body in `old_string` so the matcher stays unique.

## IA / MCP test harness

- Registry-count style tests: prefer `toBeGreaterThanOrEqual` over exact `toBe` when counts can grow with fixtures.
- Smoke or integration tests that mutate id-counter state: expose `IA_COUNTER_FILE` / `IA_COUNTER_LOCK` env overrides from day one so parallel runs and cleanup stay safe.
