# Cursor Skill Harness

Purpose: let Cursor sessions execute the same lifecycle skills stack with a rule-based compatibility layer, without duplicating `ia/skills/*/SKILL.md`.

## Document status

This is the canonical runbook for Cursor lifecycle-skill operation in this repo.

Legacy references (historical context, not primary operational source):

- `docs/cursor-agent-mcp-bridge.md`
- `docs/cursor-agent-master-plan-tasks.md`
- `docs/cursor-composer-4day-plan.md`
- `docs/cursor-agents-skills-mcp-study.md` (ADR/study)

## Architecture

The harness uses three tiers:

1. **Tier 1 router**: `.cursor/rules/cursor-skill-router.mdc` (`alwaysApply: true`)
   - global index from user intent -> skill path
   - caveman style reminder
   - host-level destructive-op deny reminders

2. **Tier 2 wrappers**: `.cursor/rules/cursor-skill-<name>.mdc` (`alwaysApply: false`)
   - one wrapper per skill
   - lightweight `description` for semantic matching
   - references the source skill via `@ia/skills/<name>/SKILL.md`

3. **Tier 3 adapters**: `.cursor/rules/cursor-lifecycle-adapters.mdc` (`alwaysApply: true`)
   - compatibility guidance for pair seams, mutation calls, chain dispatchers
   - model recommendations (Max/Opus vs Sonnet/Composer2)
   - checkpoint strategy for long chains

Cross-cutting:

- `.cursor/rules/cursor-caller-agent-cheatsheet.mdc`
- `.cursor/rules/cursor-model-gate.mdc`

## Why this works

- Cursor rules are injected into prompt context.
- Tier 2 wrappers reference canonical `SKILL.md` files directly, so there is no spec drift.
- MCP mutation gates in `tools/mcp-ia-server/src/auth/caller-allowlist.ts` are keyed by `caller_agent` strings; adapters document which value to send per lifecycle skill.

## MCP gates and id reservation (operational truth)

- `caller_agent` gate is enforced per mutation/authorship tool via `tools/mcp-ia-server/src/auth/caller-allowlist.ts`.
- Gate is based on the string provided to tool input; adapters/cheatsheet specify the accepted values.
- `tools/scripts/reserve-id.sh` is the required id reservation path for backlog ids.
- Never hand-edit `ia/state/id-counter.json`; script uses lock file + atomic update.

## Bridge quick runbook (consolidated)

1. Confirm MCP servers in `.cursor/mcp.json` are live (same shape as repo-root `.mcp.json`: `territory-ia` + `territory-ia-bridge`).
2. Run bridge preflight:
   - `npm run db:bridge-preflight`
3. Closed-loop command shape:
   - enqueue: `unity_bridge_command`
   - poll: `unity_bridge_get` until `succeeded` or `failed`
4. Use longer timeout for Play Mode scenarios.
5. Do not call worker-only lease endpoints from agent flow.

## Generated assets

Run from repo root:

```bash
npm run skill:sync:all
node tools/scripts/generate-cursor-caller-cheatsheet.mjs
```

Output:

- agent + command + cursor wrappers regenerated from `ia/skills/{slug}/SKILL.md` frontmatter (4-surface canonical pipeline; `tools/scripts/skill-tools/`)
- caller mapping file `.cursor/rules/cursor-caller-agent-cheatsheet.mdc`

Both are idempotent.

## Adding a new skill

1. Add `ia/skills/<new-skill>/SKILL.md` with canonical frontmatter (`name`, `description`, `tools_role`, `tools_extra`, `phases`, …) per `tools/scripts/skill-tools/frontmatter.ts`.
2. Regenerate all four surfaces (agent + command + cursor + lint):
   - `npm run skill:sync:all`
3. If skill touches MCP mutation/authorship tools:
   - add/confirm caller mapping in `tools/mcp-ia-server/src/auth/caller-allowlist.ts`
   - regenerate caller cheatsheet
4. If skill has pair seam/chain complexity:
   - add section in `.cursor/rules/cursor-lifecycle-adapters.mdc`

## Master-plan task execution rules (consolidated)

- One task = one commit.
- Keep task scope bounded to stage intent/spec implementation plan.
- For `_pending_` task rows, do not invent ids manually; use lifecycle filing path.
- Do not manually flip orchestrator task status cells in markdown tables.
- Prefer MCP slices (`backlog_issue`, `spec_section`, `router_for_task`) over full-doc dumping.

## Validation

Run:

```bash
npm run validate:frontmatter
```

This confirms `.mdc` frontmatter shape used by rules.

## Smoke tests

Open a fresh Cursor session and run:

1. **Simple skill**
   - Prompt: `implement TECH-283`
   - Expect: `cursor-skill-project-spec-implement.mdc` selected and `ia/skills/project-spec-implement/SKILL.md` loaded.

2. **Pair seam**
   - Prompt: `stage-file ia/projects/blip-master-plan.md Stage 1.1`
   - Expect: planner/apply split behavior from adapters; `caller_agent: "stage-file"` on mutation calls.

3. **Chain dispatcher**
   - Prompt: `ship-stage ia/projects/<plan>.md <stage-id>`
   - Expect: checkpoints between sub-skills (`author -> implement -> verify-loop -> code-review -> audit -> closeout`), not one opaque run.

## Non-goals

- No edits to `.claude/agents/`, `.claude/commands/`, or existing `ia/skills/*/SKILL.md`.
- No replacement of `tools/scripts/reserve-id.sh`; keep this as the only id reservation path.
- No duplicate operational runbooks for Cursor lifecycle in `docs/`; keep this file as source of truth.
