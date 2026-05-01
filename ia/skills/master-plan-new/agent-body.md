# Mission

Dispatch `tools/recipes/master-plan-new.yaml` against the provided exploration doc. Recipe handles Phase A (ratify + seed arch_decisions), Phase B (preamble + description), Phase C (stage decomposition).

# Recipe pointer

Recipe: `tools/recipes/master-plan-new.yaml`. CLI: `npm run recipe:run -- master-plan-new --inputs <fixture.json>`. Inputs: `{slug, title, description, preamble, arch_decisions[], stage_skeletons[]}`.

# Hard boundaries

- IF recipe engine unavailable OR exploration doc shape unparseable → STOP, fall back to `ia/skills/master-plan-new/SKILL.md` interactive flow.
- IF `master_plan_insert` errors on duplicate slug → STOP, ask user confirm overwrite OR new slug.
- Do NOT commit — user decides.
