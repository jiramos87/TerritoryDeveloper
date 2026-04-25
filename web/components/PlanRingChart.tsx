'use client'

import { useEffect, useRef } from 'react'
import * as d3 from 'd3'
import type { PlanChartDatum } from './PlanChart'

export interface PlanRingChartProps {
  data: PlanChartDatum[]
}

type StageState = 'done' | 'inProgress' | 'pending'

const FILLS: Record<StageState, string> = {
  done: 'var(--color-bg-status-done)',
  inProgress: 'var(--color-bg-status-progress)',
  pending: 'var(--color-bg-status-pending)',
}

const SIZE = 220
const STROKE = 26

function stageState(d: PlanChartDatum): StageState {
  if (d.skeleton) return 'pending'
  const total = d.pending + d.inProgress + d.done
  if (total === 0) return 'pending'
  if (d.done === total) return 'done'
  if (d.inProgress > 0 || d.done > 0) return 'inProgress'
  return 'pending'
}

export default function PlanRingChart({ data }: PlanRingChartProps) {
  const svgRef = useRef<SVGSVGElement>(null)
  const empty = data.length === 0

  useEffect(() => {
    if (empty || !svgRef.current) return

    const svg = d3.select(svgRef.current)
    svg.selectAll('*').remove()

    const radius = SIZE / 2
    const inner = radius - STROKE
    const gap = data.length > 1 ? 0.012 : 0
    const slice = (Math.PI * 2) / data.length

    const g = svg
      .attr('width', SIZE)
      .attr('height', SIZE)
      .append('g')
      .attr('transform', `translate(${radius},${radius})`)

    const arc = d3.arc<{ start: number; end: number }>()
      .innerRadius(inner)
      .outerRadius(radius - 2)
      .cornerRadius(2)

    data.forEach((d, i) => {
      const start = i * slice + gap / 2
      const end = (i + 1) * slice - gap / 2
      const state = stageState(d)
      g.append('path')
        .attr('d', arc({ start, end, startAngle: start, endAngle: end } as never))
        .attr('fill', FILLS[state])
        .attr('fill-opacity', d.skeleton ? 0.5 : 1)
        .append('title')
        .text(`${d.label} — ${state}${d.skeleton ? ' (pending decompose)' : ''}`)
    })

    const doneCount = data.filter(d => stageState(d) === 'done').length
    const pct = Math.round((doneCount / data.length) * 100)

    g.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '-0.1em')
      .attr('fill', 'var(--color-text-primary)')
      .attr('font-family', 'var(--font-mono, ui-monospace)')
      .attr('font-size', '28px')
      .attr('font-weight', '700')
      .text(`${pct}%`)

    g.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '1.4em')
      .attr('fill', 'var(--color-text-muted)')
      .attr('font-family', 'var(--font-mono, ui-monospace)')
      .attr('font-size', '10px')
      .attr('letter-spacing', '0.15em')
      .text(`${doneCount}/${data.length} STAGES`)
  }, [data, empty])

  if (empty) {
    return <p className="text-text-muted text-sm">No stages</p>
  }

  return <svg ref={svgRef} width={SIZE} height={SIZE} />
}
