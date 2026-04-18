'use client'

import { useState } from 'react'
import type { Step, TaskRow } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { DataTable } from '@/components/DataTable'
import type { Column } from '@/components/DataTable'
import { StatBar } from '@/components/StatBar'
import { toBadgeStatus } from '@/app/dashboard/_status'

const TASK_COLUMNS: Column<TaskRow>[] = [
  { key: 'id',     header: 'ID' },
  { key: 'phase',  header: 'Phase' },
  { key: 'issue',  header: 'Issue' },
  {
    key: 'status',
    header: 'Status',
    render: (r) => <BadgeChip status={toBadgeStatus(r.status)} />,
  },
  { key: 'intent', header: 'Intent' },
]

interface Props {
  step: Step
  stepDone: number
  stepTotal: number
}

export function CollapsiblePlanStep({ step, stepDone, stepTotal }: Props) {
  const [stepOpen, setStepOpen] = useState(false)
  const [openStages, setOpenStages] = useState<Set<string>>(new Set())

  function toggleStage(id: string) {
    setOpenStages((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const badgeStatus = toBadgeStatus(step.status)

  return (
    <div className="space-y-4 pl-2 border-l-2 border-border-subtle">
      <div className="space-y-1">
        <button
          type="button"
          onClick={() => setStepOpen((v) => !v)}
          className="flex items-center gap-2 flex-wrap text-left w-full hover:opacity-80 focus:outline-none focus-visible:ring-2"
          aria-expanded={stepOpen}
        >
          <span aria-hidden="true" className="text-xs">{stepOpen ? '▼' : '▶'}</span>
          <h3 className="text-base font-semibold text-text-primary">
            Step {step.id} — {step.title}
          </h3>
          <BadgeChip status={badgeStatus} />
          {badgeStatus === 'in-progress' && step.statusDetail !== '' && (
            <span className="text-text-muted text-sm">{step.statusDetail}</span>
          )}
          {stepTotal > 0 && (
            <div className="flex-1 min-w-[10rem] max-w-[20rem]">
              <StatBar label={`${stepDone} / ${stepTotal} done`} value={stepDone} max={stepTotal} />
            </div>
          )}
        </button>
        {step.objective && (
          <p className="text-sm text-text-muted leading-relaxed pl-5">{step.objective}</p>
        )}
      </div>

      {stepOpen && (
        step.stages.length === 0 ? (
          <p className="text-text-muted text-sm pl-4 italic opacity-60">Pending decompose</p>
        ) : (
          <div className="space-y-4">
            {step.stages.map((stage) => {
              const stageBadge = toBadgeStatus(stage.status)
              const stageOpen = openStages.has(stage.id)
              return (
                <div key={stage.id} className="space-y-2 pl-4">
                  <div className="space-y-1">
                    <button
                      type="button"
                      onClick={() => toggleStage(stage.id)}
                      className="flex items-center gap-2 flex-wrap text-left w-full hover:opacity-80 focus:outline-none focus-visible:ring-2"
                      aria-expanded={stageOpen}
                    >
                      <span aria-hidden="true" className="text-xs">{stageOpen ? '▼' : '▶'}</span>
                      <h4 className="text-sm font-medium text-text-primary">
                        Stage {stage.id} — {stage.title}
                      </h4>
                      <BadgeChip status={stageBadge} />
                      {stageBadge === 'in-progress' && stage.statusDetail !== '' && (
                        <span className="text-text-muted text-sm">{stage.statusDetail}</span>
                      )}
                    </button>
                    {stage.objective && (
                      <p className="text-xs text-text-muted leading-relaxed pl-5">{stage.objective}</p>
                    )}
                  </div>

                  {stageOpen && (
                    stage.tasks.length === 0 ? (
                      <p className="text-text-muted text-xs pl-2 italic opacity-60">Pending decompose</p>
                    ) : (
                      <DataTable<TaskRow>
                        columns={TASK_COLUMNS}
                        rows={stage.tasks}
                        getRowKey={(r) => r.id}
                      />
                    )
                  )}
                </div>
              )
            })}
          </div>
        )
      )}
    </div>
  )
}
