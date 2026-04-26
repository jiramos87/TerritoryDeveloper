'use client'

import { useEffect, useId, useRef, useState } from 'react'
import * as d3 from 'd3'
import type { PlanChartDatum } from './PlanChart'

export interface PlanRingChartProps {
  data: PlanChartDatum[]
  /** Center-label noun (uppercased). Defaults to "STAGES". */
  unitLabel?: string
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

const HOVER_OUTER_BOOST = 6
const HOVER_INNER_TRIM = 2

interface TipState {
  x: number
  y: number
  label: string
  status: string
  done: number
  total: number
  unit: string
}

export default function PlanRingChart({ data, unitLabel = 'STAGES' }: PlanRingChartProps) {
  const svgRef = useRef<SVGSVGElement>(null)
  const wrapRef = useRef<HTMLDivElement>(null)
  const reactId = useId()
  const filterId = `ring-glow-${reactId.replace(/[^a-zA-Z0-9_-]/g, '')}`
  const empty = data.length === 0
  const [tip, setTip] = useState<TipState | null>(null)

  useEffect(() => {
    if (empty || !svgRef.current) return

    const svg = d3.select(svgRef.current)
    svg.selectAll('*').remove()

    const radius = SIZE / 2
    const inner = radius - STROKE
    const gap = data.length > 1 ? 0.012 : 0
    const slice = (Math.PI * 2) / data.length

    const box = SIZE + HOVER_OUTER_BOOST * 2
    const g = svg
      .attr('width', box)
      .attr('height', box)
      .append('g')
      .attr('transform', `translate(${box / 2},${box / 2})`)

    const defs = svg.append('defs')
    const filter = defs
      .append('filter')
      .attr('id', filterId)
      .attr('x', '-50%')
      .attr('y', '-50%')
      .attr('width', '200%')
      .attr('height', '200%')
    filter.append('feGaussianBlur').attr('stdDeviation', 3.5).attr('result', 'blur')
    const merge = filter.append('feMerge')
    merge.append('feMergeNode').attr('in', 'blur')
    merge.append('feMergeNode').attr('in', 'SourceGraphic')

    const arc = d3.arc<{ start: number; end: number }>()
      .innerRadius(inner)
      .outerRadius(radius - 2)
      .cornerRadius(2)

    const arcHover = d3.arc<{ start: number; end: number }>()
      .innerRadius(inner - HOVER_INNER_TRIM)
      .outerRadius(radius - 2 + HOVER_OUTER_BOOST)
      .cornerRadius(2)

    data.forEach((d, i) => {
      const start = i * slice + gap / 2
      const end = (i + 1) * slice - gap / 2
      const state = stageState(d)
      const restD = arc({ start, end, startAngle: start, endAngle: end } as never) as string
      const hoverD = arcHover({ start, end, startAngle: start, endAngle: end } as never) as string
      const path = g.append('path')
        .attr('d', restD)
        .attr('fill', FILLS[state])
        .attr('fill-opacity', d.skeleton ? 0.5 : 1)
        .style('cursor', d.slug ? 'pointer' : 'default')
      // Native <title> kept as a11y / non-pointer fallback only.
      path.append('title')
        .text(`${d.label} — ${state}${d.skeleton ? ' (pending decompose)' : ''}`)

      const total = d.done + d.inProgress + d.pending

      const showTip = (event: PointerEvent | MouseEvent) => {
        const rect = wrapRef.current?.getBoundingClientRect()
        if (!rect) return
        setTip({
          x: event.clientX - rect.left,
          y: event.clientY - rect.top,
          label: d.label,
          status: d.status ?? state,
          done: d.done,
          total,
          unit: unitLabel,
        })
      }

      path
        .on('mouseenter', function (event: PointerEvent) {
          d3.select(this)
            .interrupt()
            .attr('filter', `url(#${filterId})`)
            .transition()
            .duration(140)
            .attr('d', hoverD)
          showTip(event)
        })
        .on('mousemove', function (event: PointerEvent) {
          showTip(event)
        })
        .on('mouseleave', function () {
          d3.select(this)
            .interrupt()
            .attr('filter', null)
            .transition()
            .duration(140)
            .attr('d', restD)
          setTip(null)
        })

      if (d.slug) {
        const slug = d.slug
        path.on('click', () => {
          document
            .getElementById(`plan-${slug}`)
            ?.scrollIntoView({ behavior: 'smooth', block: 'start' })
        })
      }
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
      .text(`${doneCount}/${data.length} ${unitLabel}`)
  }, [data, empty, unitLabel, filterId])

  if (empty) {
    return <p className="text-text-muted text-sm">No data</p>
  }

  const box = SIZE + HOVER_OUTER_BOOST * 2
  return (
    <div ref={wrapRef} className="relative" style={{ width: box, height: box }}>
      <svg ref={svgRef} width={box} height={box} />
      {tip ? (
        <div
          role="tooltip"
          className="pointer-events-none absolute z-10 min-w-[10rem] max-w-[16rem] rounded border border-black bg-[var(--ds-raw-panel)] px-2 py-1.5 font-mono text-[10px] uppercase tracking-wide text-[var(--ds-raw-text)] shadow-[0_4px_12px_rgba(0,0,0,0.45)]"
          style={{
            left: Math.min(Math.max(tip.x + 12, 0), box - 16),
            top: Math.max(tip.y - 8, 0),
          }}
        >
          <div className="truncate text-[11px] normal-case tracking-normal text-[var(--ds-raw-text)]">
            {tip.label}
          </div>
          <div className="mt-0.5 text-[var(--ds-text-meta)]">
            {tip.status} · {tip.done}/{tip.total} {tip.unit.toLowerCase()}
          </div>
        </div>
      ) : null}
    </div>
  )
}
