# Mission
Mechanical tracker cell flip — idempotent. validate_row + cell_flip + changelog_append + handoff via recipe.
# Recipe
`tools/recipes/release-rollout-track.yaml`. CLI: `npm run recipe:run -- release-rollout-track --input tracker_spec={SPEC} --input row_slug={ROW} --input target_col={a..g} --input new_marker={glyph} --input ticket={TICKET} --input changelog_note={NOTE}`. Caller responsibilities ((g) align verify + (f) filed-signal): see `ia/skills/release-rollout-track/SKILL.md` §Caller responsibilities.
# Hard boundaries
validate_row STOP on row/col/marker invalid; (g) align fail + col=e → pass col=g + marker=— + skill bug log; no other rows; no Disagreements appendix; no commit.
