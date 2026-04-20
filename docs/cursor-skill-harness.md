# Cursor Skill Harness

Purpose: let Cursor sessions execute the same lifecycle skills stack with a rule-based compatibility layer, without duplicating `ia/skills/*/SKILL.md`.

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

## Generated assets

Run from repo root:

```bash
node tools/scripts/generate-cursor-skill-wrappers.mjs
node tools/scripts/generate-cursor-caller-cheatsheet.mjs
```

Output:

- wrappers under `.cursor/rules/cursor-skill-*.mdc`
- caller mapping file `.cursor/rules/cursor-caller-agent-cheatsheet.mdc`

Both scripts are idempotent.

## Adding a new skill

1. Add `ia/skills/<new-skill>/SKILL.md` with frontmatter `name` and `description`.
2. Regenerate wrappers:
   - `node tools/scripts/generate-cursor-skill-wrappers.mjs`
3. If skill touches MCP mutation/authorship tools:
   - add/confirm caller mapping in `tools/mcp-ia-server/src/auth/caller-allowlist.ts`
   - regenerate caller cheatsheet
4. If skill has pair seam/chain complexity:
   - add section in `.cursor/rules/cursor-lifecycle-adapters.mdc`

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
