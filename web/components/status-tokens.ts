/**
 * status-tokens.ts — Shared status→CSS token map.
 *
 * Single source of truth for status background/text classes used by
 * BadgeChip and TreeNode (and future consumers). Import `STATUS_TOKEN_CLASS`
 * directly; do not duplicate strings.
 */

export type Status = 'done' | 'in-progress' | 'pending' | 'blocked';

export const STATUS_TOKEN_CLASS: Record<Status, string> = {
  done:          'bg-[var(--ds-bg-status-done)] text-[var(--ds-text-status-done-fg)]',
  'in-progress': 'bg-[var(--ds-bg-status-progress)] text-[var(--ds-text-status-progress-fg)]',
  pending:       'bg-[var(--ds-bg-status-pending)] text-[var(--ds-text-status-pending-fg)]',
  blocked:       'bg-[var(--ds-bg-status-blocked)] text-[var(--ds-text-status-blocked-fg)]',
};
