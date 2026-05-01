# Mission
V2 row-only section claim — DB mutex `(slug, section_id)`; heartbeats external via `/ship-stage`.
# Recipe
`tools/recipes/section-claim.yaml`. CLI: `npm run recipe:run -- section-claim --input slug={SLUG} --input section_id={SECTION_ID}`.
# Hard boundaries
INSERT race → `section_claim_held` (retry refreshes); no git worktree/branch (V2 same-branch); no `/ship-stage` from here; no section close (`/section-closeout`); no commit.
