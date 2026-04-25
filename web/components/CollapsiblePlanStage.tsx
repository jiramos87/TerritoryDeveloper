'use client'

import { useState } from 'react'
import type { ReactNode } from 'react'
import type { Stage } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { StatBar } from '@/components/StatBar'
import { toBadgeStatus } from '@/app/dashboard/_status'

interface Props {
  stage: Stage
  stageDone: number
  stageTotal: number
  /** Pre-rendered objective markdown (server-side). */
  objective?: ReactNode | null
  /** Pre-rendered task rows (server-side, one per stage.tasks entry). */
  children?: ReactNode
}

export function CollapsiblePlanStage({
  stage,
  stageDone,
  stageTotal,
  objective,
  children,
}: Props) {
  const [stageOpen, setStageOpen] = useState(false)

  const badgeStatus = toBadgeStatus(stage.status)

  return (
    <div className="space-y-4 pl-2 border-l-2 border-border-subtle">
      <div className="space-y-1">
        <button
          type="button"
          onClick={() => setStageOpen((v) => !v)}
          className="flex items-center gap-2 flex-wrap text-left w-full hover:opacity-80 focus:outline-none focus-visible:ring-2"
          aria-expanded={stageOpen}
        >
          <span aria-hidden="true" className="text-xs">{stageOpen ? '▼' : '▶'}</span>
          <h3 className="text-base font-semibold text-text-primary">
            Stage {stage.id} — {stage.title}
          </h3>
          <BadgeChip status={badgeStatus} />
          {badgeStatus === 'in-progress' && stage.statusDetail !== '' && (
            <span className="text-text-muted text-sm">{stage.statusDetail}</span>
          )}
          {stageTotal > 0 && (
            <div className="flex-1 min-w-[10rem] max-w-[20rem]">
              <StatBar label={`${stageDone} / ${stageTotal} done`} value={stageDone} max={stageTotal} />
            </div>
          )}
        </button>
        {objective && (
          <div className="pl-5 text-text-muted">{objective}</div>
        )}
      </div>

      {stageOpen && (
        stage.tasks.length === 0 ? (
          <p className="text-text-muted text-sm pl-4 italic opacity-60">Pending decompose</p>
        ) : (
          <div className="space-y-2">{children}</div>
        )
      )}
    </div>
  )
}
