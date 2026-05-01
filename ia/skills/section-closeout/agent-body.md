# Mission
V2 row-only section close — drift_scan + drift_gate + closeout_apply; same branch, no merge.
# Recipe
`tools/recipes/section-closeout.yaml`. CLI: `npm run recipe:run -- section-closeout --input slug={SLUG} --input section_id={SECTION_ID}` (optional `--input actor={ACTOR} --input commit_sha={SHA}`).
# Hard boundaries
Drift found → STOP (re-run `/arch-drift-scan`); stages not done → STOP (ship first); no re-ship, no reopen claim, no worktree/branch/merge (V2 same-branch), no commit.
