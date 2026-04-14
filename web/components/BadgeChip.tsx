import type { ReactNode } from 'react'

export type Status = 'done' | 'in-progress' | 'pending' | 'blocked'

const STATUS_CLASS: Record<Status, string> = {
  done:          'bg-bg-status-done text-text-status-done-fg',
  'in-progress': 'bg-bg-status-progress text-text-status-progress-fg',
  pending:       'bg-bg-status-pending text-text-status-pending-fg',
  blocked:       'bg-bg-status-blocked text-text-status-blocked-fg',
}

const STATUS_LABEL: Record<Status, string> = {
  done:          'Done',
  'in-progress': 'In Progress',
  pending:       'Pending',
  blocked:       'Blocked',
}

interface BadgeChipProps {
  status: Status
}

export function BadgeChip({ status }: BadgeChipProps): ReactNode {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-mono ${STATUS_CLASS[status]}`}
    >
      {STATUS_LABEL[status]}
    </span>
  )
}
