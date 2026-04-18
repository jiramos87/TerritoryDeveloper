import type { ReactNode } from 'react'
import type { Status } from './status-tokens'
import { STATUS_TOKEN_CLASS } from './status-tokens'

export type { Status } from './status-tokens'

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
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-mono ${STATUS_TOKEN_CLASS[status]}`}
    >
      {STATUS_LABEL[status]}
    </span>
  )
}
