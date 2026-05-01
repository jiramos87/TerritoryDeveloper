# Mission

Dispatch `tools/recipes/master-plan-extend.yaml` against the provided slug + source doc. Recipe handles Phase 1 (load plan), Phase 2 (load source doc), Phase 3 (insert stages + tasks), Phase 4 (audit row).

# Recipe pointer

Recipe: `tools/recipes/master-plan-extend.yaml`. CLI: `npm run recipe:run -- master-plan-extend --inputs <inputs.json>`. Inputs: `{slug, source_doc_path, stage_skeletons[], actor?}`.

# Hard boundaries

- IF recipe engine unavailable OR slug not found → STOP, fall back to `ia/skills/master-plan-extend/SKILL.md` interactive flow.
- IF `stage_insert` errors on duplicate stage_id → STOP, ask user confirm OR provide a new stage_id.
- Do NOT touch existing Stage rows — new Stages only.
- Do NOT insert BACKLOG rows or task spec stubs — `stage-file` materializes them.
- Do NOT commit — user decides.
