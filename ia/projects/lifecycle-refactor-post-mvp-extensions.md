---
purpose: Deferred extensions doc for lifecycle-refactor-master-plan. Fed to /master-plan-extend post-M8.
audience: both
loaded_by: ondemand
slices_via: none
---

# Lifecycle Refactor — Post-MVP Extensions

Deferred scope parked here during M0–M8 freeze. Fold into `ia/projects/lifecycle-refactor-master-plan.md` via `/master-plan-extend` after T4.2.1 freeze lift.

Parent plan: `ia/projects/lifecycle-refactor-master-plan.md`
Pickup trigger: `/master-plan-extend ia/projects/lifecycle-refactor-master-plan.md ia/projects/lifecycle-refactor-post-mvp-extensions.md`
Freeze gate: M8 sign-off (Stage 8 T4.2.1).

## Context — why deferred

In-branch work (`feature/lifecycle-collapse-cognitive-split`) added `--tooling-only` runtime flag to `/verify-loop` to accelerate refactor task closeouts (zero Unity surface touched). Flag solves the pain today. Durable per-spec frontmatter option (`verify_mode: tooling_only`) was analyzed but deferred — three reasons:

1. Pain already solved by flag — frontmatter is convenience on top, not the fix.
2. Template (`ia/templates/project-spec-template.md`) + verify-loop skill (`ia/skills/verify-loop/SKILL.md`) + backlog parser (`tools/mcp-ia-server/src/parser/*.ts`) are three hottest files through Stages 5–7; adding frontmatter mid-refactor = merge churn.
3. Post-refactor landing is clean additive — Stage/Task schema settled, template rewritten, parser stable.

Memory anchor: `~/.claude-personal/projects/.../memory/feedback_refactor_tooling_only_verify.md` (rule + why + how-to-apply).

## Design Expansion — verify_mode spec frontmatter

### Goal

Add optional `verify_mode: tooling_only | full` field to project-spec frontmatter. Closeout skill reads the field; when `tooling_only`, skip the compile-gate / Path A / Path B prompts entirely — no `--tooling-only` flag required at call site.

### Scope variants

**C-min (~2h)** — frontmatter field only, advisory.
- Add `verify_mode` to `ia/templates/frontmatter-schema.md` §Fields (optional, default `full`).
- Add to `ia/templates/project-spec-template.md` §frontmatter stub as commented example.
- Validator (`tools/mcp-ia-server/scripts/check-frontmatter.mjs`) accepts the field; no enforcement yet.
- Documentation only — skills still read `--tooling-only` flag.

**C-med (~4h)** — wire into verify-loop skill + dispatchers.
- C-min +
- `ia/skills/verify-loop/SKILL.md` §"Pre-matrix mode gate" reads spec frontmatter `verify_mode` when ISSUE_ID present; treats `tooling_only` equivalent to `--tooling-only` flag.
- `.claude/agents/verify-loop.md` + `.claude/commands/verify-loop.md` document the frontmatter fallback.
- Flag still works (runtime override); frontmatter is the default.

**C-full (~1 day)** — MCP exposure + closeout gate.
- C-med +
- `backlog_issue` MCP tool surfaces `verify_mode` in payload.
- `project_spec_closeout_digest` (or renamed `stage_closeout_digest` per T7.14) validates frontmatter ↔ branch-diff consistency — fail closeout when `verify_mode: tooling_only` but dirty `Assets|Packages|ProjectSettings/` paths.
- `/closeout` surfaces the mode in Verification block.

### Surfaces to touch (9-row map)

| # | Surface | C-min | C-med | C-full | In-flight overlap risk |
|---|---|---|---|---|---|
| 1 | `ia/templates/frontmatter-schema.md` | ✓ | ✓ | ✓ | LOW — stable post-M8 |
| 2 | `ia/templates/project-spec-template.md` | ✓ | ✓ | ✓ | LOW — stable post-M8 |
| 3 | `ia/skills/verify-loop/SKILL.md` | — | ✓ | ✓ | MED — stable post-M8 |
| 4 | `.claude/agents/verify-loop.md` + `commands/verify-loop.md` | — | ✓ | ✓ | LOW |
| 5 | `tools/mcp-ia-server/scripts/check-frontmatter.mjs` | ✓ (allow) | ✓ | ✓ (strict) | LOW |
| 6 | `tools/mcp-ia-server/src/parser/backlog-parser.ts` + `backlog-yaml-loader.ts` | — | — | ✓ | LOW post-M8 |
| 7 | `mcp__territory-ia__backlog_issue` payload | — | — | ✓ | LOW post-M8 |
| 8 | `project_spec_closeout_digest` / `stage_closeout_digest` | — | — | ✓ | LOW post-M8 |
| 9 | `ia/projects/{id}.md` per-spec adoption | ✓ opt-in | ✓ opt-in | ✓ opt-in | — |

### Decision matrix — pick variant post-M8

- Default: **C-med**. Frontmatter + skill wiring covers 90% of refactor-adjacent work without MCP churn.
- Escalate to **C-full** only if we start seeing closeouts slip through with mismatched `verify_mode` (drift risk).
- **C-min** alone is half-measure — ship only if time-boxed.

### Acceptance

- `ia/templates/frontmatter-schema.md` §Fields row documents `verify_mode` with regex `^(tooling_only|full)$` and default `full`.
- At least one refactor-adjacent spec under `ia/projects/` adopts `verify_mode: tooling_only` end-to-end (closeout green).
- `npm run validate:all` passes; `/verify-loop` with no flag + `verify_mode: tooling_only` spec behaves identical to `/verify-loop --tooling-only`.

### Out of scope

- Generalizing to non-lifecycle-refactor tooling surfaces (separate exploration — requires blast-radius review of umbrella plans).
- Retroactive backfill of `verify_mode` on closed specs.

## Handoff to /master-plan-extend

When freeze lifts:

1. Confirm T4.2.1 Done in `ia/projects/lifecycle-refactor-master-plan.md`.
2. Pick scope variant (C-min / C-med / C-full) per Decision matrix above.
3. Run `/master-plan-extend ia/projects/lifecycle-refactor-master-plan.md ia/projects/lifecycle-refactor-post-mvp-extensions.md`.
4. `master-plan-extend` appends new Step with stages → phases → tasks decomposed; tasks seeded `_pending_`.
5. `/stage-file` bulk-files TECH issues against the new stage.
6. Normal `/ship-stage` chain from there.
