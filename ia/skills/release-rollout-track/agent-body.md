# Mission

Flip one cell in `{TRACKER_SPEC}` for `{ROW_SLUG}` at `{TARGET_COL}` to `{NEW_MARKER}`. Idempotent. Append Change log row. No decisions — mechanical cell flip only.

# Recipe

Mechanical phases (validate, cell flip, Change log append, handoff) run as recipe `release-rollout-track` (`tools/recipes/release-rollout-track.yaml`) — DEC-A19 Phase C recipify. Invoke:

```
npm run recipe:run -- release-rollout-track \
  --input tracker_spec={TRACKER_SPEC} \
  --input row_slug={ROW_SLUG} \
  --input target_col={a..g} \
  --input new_marker={✓|◐|—|❓|⚠️} \
  --input ticket={TICKET} \
  --input changelog_note={CHANGELOG_NOTE}
```

Recipe stops on first failure (validate row / column / marker; cell-flip header parse; row not matched). Both `cell_flip` and `changelog_append` are idempotent — re-runs return `noop` instead of duplicating edits.

# Caller responsibilities (NOT in recipe — defer to seam Phase D)

- Column (g) align verify when `target_col=g` OR `target_col=e` with (g) gate. Run `term-anchor-verify` subskill (`ia/skills/term-anchor-verify/SKILL.md`) over child orchestrator domain entities. `all_anchored=true` → marker `✓`; otherwise `—` + skill bug log entry. Caller picks final marker before invoking recipe.
- Column (f) filed-signal verify when `target_col=f`. Either run helper `tools/scripts/recipe-engine/release-rollout-track/filed-signal.sh --slug {ROW_SLUG}` for a coarse glyph, or inspect Glob output by hand. Caller passes resulting glyph as `new_marker`.

# Hard boundaries

- IF row not in tracker → recipe `validate_row` step STOPs; do not retry.
- IF `target_col` invalid → recipe STOPs.
- IF `new_marker` invalid glyph → recipe STOPs.
- IF (g) align verify fails AND `target_col = (e)` → caller passes `target_col=g` + `new_marker=—` + skill bug log entry. Do NOT tick (e).
- Do NOT touch other rows.
- Do NOT edit Disagreements appendix.
- Do NOT commit.
