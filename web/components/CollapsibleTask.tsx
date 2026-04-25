'use client'

import { useState } from 'react'
import type { ReactNode } from 'react'
import type { TaskRow } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { toBadgeStatus } from '@/app/dashboard/_status'

interface Props {
  task: TaskRow
  /** Pre-rendered body markdown (server-side). Null when task has no body. */
  body?: ReactNode | null
}

/**
 * CollapsibleTask — single dashboard task row, expand-on-click.
 *
 * Header (always visible): id / issue / status badge / intent.
 * Body slot accepts a pre-rendered ReactNode from the server (so glossary
 * tooltip wiring stays on the RSC side and we don't ship a glossary index
 * across the client boundary).
 */
export function CollapsibleTask({ task, body }: Props) {
  const [open, setOpen] = useState(false)
  const hasBody = body != null

  return (
    <div className="rounded-md border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)]">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="grid w-full items-center gap-2 px-3 py-2 text-left hover:bg-[var(--ds-bg-elevated)] focus:outline-none focus-visible:ring-2 [grid-template-columns:auto_minmax(4rem,6rem)_minmax(5rem,7rem)_auto_1fr]"
        aria-expanded={open}
      >
        <span aria-hidden="true" className="text-xs text-[var(--ds-text-muted)]">
          {open ? '▼' : '▶'}
        </span>
        <span className="font-mono text-xs text-[var(--ds-text-muted)]">
          {task.id}
        </span>
        <span className="font-mono text-xs text-[var(--ds-text-meta)]">
          {task.issue}
        </span>
        <BadgeChip status={toBadgeStatus(task.status)} />
        <span className="truncate text-sm text-[var(--ds-text-primary)]">
          {task.intent}
        </span>
      </button>

      {open && (
        <div className="border-t border-[var(--ds-border-subtle)] px-4 py-3">
          {hasBody ? (
            body
          ) : (
            <p className="text-sm italic text-[var(--ds-text-muted)] opacity-70">
              No body yet.
            </p>
          )}
        </div>
      )}
    </div>
  )
}
