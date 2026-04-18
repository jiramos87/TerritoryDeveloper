/**
 * status-tokens.ts — Shared status→CSS token map.
 *
 * Single source of truth for status background/text classes used by
 * BadgeChip and TreeNode (and future consumers). Import `STATUS_TOKEN_CLASS`
 * directly; do not duplicate strings.
 */

export type Status = 'done' | 'in-progress' | 'pending' | 'blocked';

export const STATUS_TOKEN_CLASS: Record<Status, string> = {
  done:          'bg-bg-status-done text-text-status-done-fg',
  'in-progress': 'bg-bg-status-progress text-text-status-progress-fg',
  pending:       'bg-bg-status-pending text-text-status-pending-fg',
  blocked:       'bg-bg-status-blocked text-text-status-blocked-fg',
};
