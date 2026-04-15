import type { Status } from '@/components/BadgeChip'

/**
 * toBadgeStatus — maps raw parser-domain status strings to BadgeChip UI Status.
 *
 * Strips trailing " — {detail}" before matching (detail belongs to hierarchy display).
 * Unknown strings fall through to 'pending'.
 *
 * Covers: TaskStatus ('_pending_' | 'Draft' | 'In Review' | 'In Progress' | 'Done (archived)' | 'Done')
 * and HierarchyStatus ('Draft' | 'In Review' | 'In Progress' | 'Final').
 */
export function toBadgeStatus(raw: string): Status {
  const base = raw.split(' — ')[0].trim()

  switch (base) {
    case 'Done':
    case 'Done (archived)':
    case 'Final':
      return 'done'
    case 'In Progress':
      return 'in-progress'
    case 'Draft':
    case 'In Review':
    case '_pending_':
      return 'pending'
    default:
      return 'pending'
  }
}
