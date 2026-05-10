---
purpose: "Universal agent behavioral principles — always apply"
audience: agent
loaded_by: force-loaded
alwaysApply: true
---

# Agent behavioral principles

## Testing + verification

- Prefer closed-loop testing (`verify-loop` / `testmode-batch`) over human-in-the-loop. Ask only when bridge/batch can't reach the surface.
- Unity mutations/reads → `unity_bridge_command` / `unity_bridge_get` end-to-end. Agent owns Editor work. Missing kind → propose bridge tool stub BEFORE escalating.
- **Stage = one test file, grown task-by-task (incremental TDD red→green protocol).** One file per stage under `tests/{plan-slug}/stage{N}-{slug}.test.{mjs|cs}`; red on first task, green on last. Stage close requires file fully green. Master-plan close unions all stage files. Full protocol: `ia/skills/ship-cycle/SKILL.md`.

## Token economy + speed

- Before any read/write: "does MCP already have this?" + "can one batch op replace N sequential?" Order: MCP slice → targeted read → full file.
- Propose options by execution speed: batch > sequential, in-place > roundtrip, validate-before-mutate > mutate-then-fix.
- Don't duplicate subagent work in main context.

## Output format

- All agent prose → caveman format. Exceptions: `ia/rules/agent-output-caveman.md §exceptions`.
- No multi-paragraph docstrings — one short line max.
- Only general principles, do not reference specific files, plans, stages, tasks, etc.

## Commits + mutations

- Never `git commit` unless skill `SKILL.md` explicitly instructs it.
- Validate before mutating shared state (BACKLOG, master plans, id counter).
- Never hand-edit `ia/state/id-counter.json` or `id:` field — always through `reserve-id.sh`.
- When skill chain files a BACKLOG issue: verify yaml + spec stub + BACKLOG row all three exist.
- After stage closeout: rollup + Task row flips run inline via `stage_closeout_apply` MCP — no manual edits.

## Spec authoring + validators

- `validate:all` scans all `*master-plan*.md`; CI red on plan B blocks plan A ship — triage with `validate:master-plan-status`.
- `validate:backlog-yaml` = source of truth for yaml schema.
- `validate:frontmatter` exits 0 on warnings — gate on stdout, not exit code.
- Run `npm run generate:ia-indexes` after any glossary edit.
- Prototype-first: every master plan ships a Stage 1.0 tracer slice + Stages 2+ §Visibility Delta lines. Details: `rule_content prototype-first-methodology`.
- `validate:red-stage-proof-anchor`: anchor drift gate. §Red-Stage Proof anchor `{file}::{method}` → method body must reference surface keywords from anchor prose. Drift → exit 1.

## On-demand hints

- Shell scripts, smoke preflight, Cursor multi-file edit quirks, IA test env overrides: [`agent-tooling-hints.md`](agent-tooling-hints.md) — `rule_content agent-tooling-hints`.

## Lesson integration loop

Lesson surfaces → write to skill `SKILL.md §Guardrails` or `ia/rules/*.md` → delete MEMORY.md entry → run `/skill-train` if friction recurs.
