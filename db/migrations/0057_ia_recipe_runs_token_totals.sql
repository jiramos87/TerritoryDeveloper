-- 0057_ia_recipe_runs_token_totals.sql
--
-- DEC-A19 Phase E — adds per-step token tracking to ia_recipe_runs.
-- Captures Anthropic SDK usage blocks from seam step dispatch so the
-- C2 token-drop ratio (recipe path vs legacy direct-Opus baseline) can
-- be computed via a simple aggregate query.
--
-- Columns nullable: pre-migration rows and non-seam steps have no usage data.

ALTER TABLE ia_recipe_runs
  ADD COLUMN IF NOT EXISTS prompt_tokens INT,
  ADD COLUMN IF NOT EXISTS completion_tokens INT,
  ADD COLUMN IF NOT EXISTS total_tokens INT;
