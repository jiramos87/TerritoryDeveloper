---
purpose: "Universal agent behavioral principles — always apply"
audience: agent
loaded_by: force-loaded
alwaysApply: true
---

# Agent behavioral principles

## Testing + verification

- Prefer closed-loop testing (`verify-loop` / `testmode-batch`) over human-in-the-loop. Ask only when bridge/batch can't reach the surface.
- Unity mutations/reads → `unity_bridge_command` / `unity_bridge_get` first; escalate to human only when mutation kind doesn't exist.
- Smoke specs: Phase 0 precondition check before HTTP probes — blocked-on-deploy mislabels as smoke failure.
- Add `git status --porcelain web/` to web-platform preflight — untracked files silently break Vercel parity.

## Token economy + speed

- Before any read/write: "does MCP already have this?" + "can one batch op replace N sequential?" Order: MCP slice → targeted read → full file.
- Propose options by execution speed: batch > sequential, in-place > roundtrip, validate-before-mutate > mutate-then-fix.
- Don't duplicate subagent work in main context.

## Output format

- All agent prose → caveman format. Exceptions: `ia/rules/agent-output-caveman.md §exceptions`.
- No multi-paragraph docstrings — one short line max.

## Commits + mutations

- Never `git commit` unless skill `SKILL.md` explicitly instructs it.
- Validate before mutating shared state (BACKLOG, master plans, id counter).
- Never hand-edit `ia/state/id-counter.json` or `id:` field — always through `reserve-id.sh`.
- When skill chain files a BACKLOG issue: verify yaml + spec stub + BACKLOG row all three exist.
- After `/closeout`: flip master plan task row Done before re-running `validate:all`.

## Spec authoring

- `plan-author`: grep class/symbol before writing paths — audit specs cite stale filenames. `grep -rn "class {T}" Assets/Scripts/` as preflight.
- Stage-compress N tasks → 1 when every task ≤1 file and none kicked off.
- Before filing follow-up TECH, grep `BACKLOG.md` for matching scope — avoid duplicates.
- `validate:all` scans all `*master-plan*.md`; CI red on plan B blocks plan A ship — triage with `validate:master-plan-status`.
- `validate:backlog-yaml` = source of truth for yaml schema.
- `validate:frontmatter` exits 0 on warnings — gate on stdout, not exit code.
- Run `npm run generate:ia-indexes` after any glossary edit.
- Before new static-helper class, grep namespace for the proposed name — patch-data structs collide (CS0101); suffix `-Stepper`/`-Builder`/`-Service` when bare noun exists.

## Shell + tooling

- npm scripts: `npm --prefix {dir} run …` over `cd {dir} && …`.
- macOS `sed` no `\b` — use `perl -pi -e 's/\bFoo\b/Bar/g'`.
- macOS flock (util-linux) not on npm shell PATH; patch via `buildScriptEnv()` in Node callers of `reserve-id.sh`.
- Node ≥22 ts-node: no `const enum` — use `as const` object + derived type alias in `tools/scripts/*.ts`.
- `__dirname` flips when ts-node reparses as ESM — verify path depth before writing `resolve(SCRIPT_DIR, …)`.
- Edit tool replacing `for` loop header: include full body in `old_string`.

## Testing conventions

- Registry-count tests: `toBeGreaterThanOrEqual` not `toBe`.
- Smoke tests mutating state: expose `IA_COUNTER_FILE` / `IA_COUNTER_LOCK` env overrides from day one.

## Lesson integration loop

Lesson surfaces → write to skill `SKILL.md §Guardrails` or `ia/rules/*.md` → delete MEMORY.md entry → run `/skill-train` if friction recurs.
