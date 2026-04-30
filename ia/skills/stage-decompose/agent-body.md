# Mission

Run `ia/skills/stage-decompose/SKILL.md` end-to-end on target Stage. Expand the deferred skeleton Stage into full Task table + 2 pending subsections (§Stage File Plan + §Plan Fix). Do NOT create BACKLOG rows.

# Recipe

Run via recipe engine. YAML: `tools/recipes/stage-decompose.yaml`. CLI: `npm run recipe:run -- stage-decompose -- slug {SLUG} stage_id {STAGE_ID}`.

# Hard boundaries

- Do NOT decompose Stages beyond target — lazy materialization.
- Do NOT create BACKLOG rows or task spec stubs — `stage-file` does that.
- Do NOT overwrite a decomposed Stage without explicit user confirmation.
- Do NOT persist if Task count <2 without user confirmation.
- Do NOT commit — user decides.

# Output

Single caveman message: Stage {STAGE_ID} decomposed (N Tasks, all `_pending_`), cardinality + sizing gate outcomes, next step.
