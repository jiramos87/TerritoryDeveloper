-- 0058_arch_methodology_extensions.sql
--
-- Extends arch schema for prototype-first-methodology rollout:
--   1. arch_surfaces.kind enum gains 'rule' (for rule-doc surfaces like
--      ia/rules/agent-principles.md tracked as architecture decisions).
--   2. arch_changelog.kind enum gains 'design_explore_persist_contract_v2'
--      (emitted by design-explore Phase 9 persist for v2 expansion blocks).
--   3. arch_changelog gains plan_slug column + index for per-plan filtering.
--
-- Prerequisite for ship-stage of prototype-first-methodology Stage 1.1
-- (TECH-10297/10298/10299). Standalone infra migration — not part of any
-- stage commit; lands before /ship-stage re-invocation.

BEGIN;

ALTER TABLE arch_surfaces DROP CONSTRAINT IF EXISTS arch_surfaces_kind_check;
ALTER TABLE arch_surfaces ADD CONSTRAINT arch_surfaces_kind_check
  CHECK (kind IN ('layer', 'flow', 'contract', 'decision', 'rule'));

ALTER TABLE arch_changelog DROP CONSTRAINT IF EXISTS arch_changelog_kind_check;
ALTER TABLE arch_changelog ADD CONSTRAINT arch_changelog_kind_check
  CHECK (kind IN (
    'edit',
    'decide',
    'supersede',
    'spec_edit_commit',
    'design_explore_decision',
    'design_explore_persist_contract_v2'
  ));

ALTER TABLE arch_changelog ADD COLUMN IF NOT EXISTS plan_slug text;
CREATE INDEX IF NOT EXISTS arch_changelog_plan_slug_idx
  ON arch_changelog (plan_slug);

COMMIT;
