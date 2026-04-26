'use client'

import { useEffect, useRef } from 'react'
import * as d3 from 'd3'

export interface PlanChartDatum {
  label: string
  pending: number
  inProgress: number
  done: number
  skeleton?: boolean
  /** Optional anchor slug — when set, chart marks become interactive (`#plan-{slug}` scroll). */
  slug?: string
  /** Optional human-readable status (e.g. "In Progress" / "Final") — shown in ring tooltip. */
  status?: string
}

export interface PlanChartProps {
  data: PlanChartDatum[]
}

const KEYS = ['pending', 'inProgress', 'done'] as const
type Key = typeof KEYS[number]

const FILLS: Record<Key, string> = {
  pending: 'var(--color-bg-status-pending)',
  inProgress: 'var(--color-bg-status-progress)',
  done: 'var(--color-bg-status-done)',
}

const LEGEND_LABELS: Record<Key, string> = {
  pending: 'Pending',
  inProgress: 'In Progress',
  done: 'Done',
}

const WIDTH = 480
const HEIGHT = 260
const MARGIN = { top: 10, right: 20, bottom: 90, left: 40 }

export default function PlanChart({ data }: PlanChartProps) {
  const svgRef = useRef<SVGSVGElement>(null)
  const empty = data.length === 0

  useEffect(() => {
    if (empty || !svgRef.current) return

    const svg = d3.select(svgRef.current)
    svg.selectAll('*').remove()

    const innerW = WIDTH - MARGIN.left - MARGIN.right
    const innerH = HEIGHT - MARGIN.top - MARGIN.bottom

    const g = svg
      .attr('width', WIDTH)
      .attr('height', HEIGHT)
      .append('g')
      .attr('transform', `translate(${MARGIN.left},${MARGIN.top})`)

    const xOuter = d3
      .scaleBand()
      .domain(data.map(d => d.label))
      .range([0, innerW])
      .padding(0.2)

    const xInner = d3
      .scaleBand()
      .domain([...KEYS])
      .range([0, xOuter.bandwidth()])
      .padding(0.05)

    const yMax = d3.max(data, d => Math.max(d.pending, d.inProgress, d.done)) ?? 0

    const y = d3
      .scaleLinear()
      .domain([0, yMax])
      .range([innerH, 0])
      .nice()

    data.forEach(d => {
      const xGroup = xOuter(d.label) ?? 0
      KEYS.forEach(key => {
        const xBar = xGroup + (xInner(key) ?? 0)
        const val = d[key]
        const yTop = y(val)
        const rect = g.append('rect')
          .attr('x', xBar)
          .attr('y', yTop)
          .attr('width', xInner.bandwidth())
          .attr('height', innerH - yTop)
          .attr('fill', FILLS[key])
        if (d.skeleton) {
          rect
            .attr('fill-opacity', 0.35)
            .attr('stroke', FILLS[key])
            .attr('stroke-dasharray', '2 2')
        }
      })
    })

    // Phase 1 — axisBottom (rotated labels for legibility)
    const xAxis = d3.axisBottom(xOuter)
      .tickFormat(d => (d as string).length > 24 ? (d as string).slice(0, 23) + '\u2026' : d as string)

    const xAxisG = g.append('g')
      .attr('transform', `translate(0, ${innerH})`)
      .call(xAxis)
    xAxisG.selectAll('text')
      .attr('fill', 'var(--color-text-muted)')
      .attr('transform', 'rotate(-40)')
      .attr('text-anchor', 'end')
      .attr('dx', '-0.5em')
      .attr('dy', '0.25em')

    // Click-to-scroll: when datum carries a slug, x-axis label navigates to `#plan-{slug}`.
    const slugByLabel = new Map(data.map(d => [d.label, d.slug]))
    xAxisG.selectAll<SVGTextElement, string>('text')
      .style('cursor', d => (slugByLabel.get(d) ? 'pointer' : 'default'))
      .style('text-decoration', d => (slugByLabel.get(d) ? 'underline' : 'none'))
      .on('click', (_event, d) => {
        const slug = slugByLabel.get(d)
        if (!slug) return
        document
          .getElementById(`plan-${slug}`)
          ?.scrollIntoView({ behavior: 'smooth', block: 'start' })
      })

    // Phase 1 — axisLeft
    const yAxis = d3.axisLeft(y)
      .tickFormat(d => d3.format('d')(d as number))
      .ticks(Math.min(5, Math.max(1, yMax)))

    g.append('g')
      .call(yAxis)
      .selectAll('text')
        .attr('fill', 'var(--color-text-muted)')

    // Phase 2 — inline SVG legend (top-right inside chart g)
    const legendWidth = 200
    const entryOffsets = [0, 70, 140]
    const legend = g.append('g')
      .attr('class', 'legend')
      .attr('transform', `translate(${innerW - legendWidth}, ${-MARGIN.top + 2})`)

    KEYS.forEach((key, i) => {
      const entry = legend.append('g')
        .attr('transform', `translate(${entryOffsets[i]}, 0)`)
      entry.append('rect')
        .attr('width', 10)
        .attr('height', 10)
        .attr('fill', FILLS[key])
      entry.append('text')
        .attr('dx', 14)
        .attr('dy', 9)
        .attr('fill', 'var(--color-text-muted)')
        .attr('font-size', '10px')
        .text(LEGEND_LABELS[key])
    })
  }, [data, empty])

  if (empty) {
    return <p className="text-text-muted text-sm">No tasks</p>
  }

  return <svg ref={svgRef} width={WIDTH} height={HEIGHT} />
}
