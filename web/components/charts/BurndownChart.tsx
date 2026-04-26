'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface BurndownPoint {
  date: string;
  open: number;
  closed: number;
}

export interface BurndownChartProps {
  data: BurndownPoint[];
  width?: number;
  height?: number;
}

export default function BurndownChart({ data, width = 480, height = 220 }: BurndownChartProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || data.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    const margin = { top: 10, right: 56, bottom: 28, left: 36 };
    const innerW = width - margin.left - margin.right;
    const innerH = height - margin.top - margin.bottom;

    const g = svg
      .attr('width', width)
      .attr('height', height)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const parsed = data.map((d) => ({ ...d, t: new Date(d.date) }));
    const x = d3
      .scaleTime()
      .domain(d3.extent(parsed, (d) => d.t) as [Date, Date])
      .range([0, innerW]);

    const yMax = d3.max(parsed, (d) => Math.max(d.open, d.closed)) ?? 1;
    const y = d3.scaleLinear().domain([0, yMax]).nice().range([innerH, 0]);

    const lineOpen = d3
      .line<{ t: Date; open: number }>()
      .x((d) => x(d.t))
      .y((d) => y(d.open))
      .curve(d3.curveMonotoneX);
    const lineClosed = d3
      .line<{ t: Date; closed: number }>()
      .x((d) => x(d.t))
      .y((d) => y(d.closed))
      .curve(d3.curveMonotoneX);

    g.append('path')
      .datum(parsed)
      .attr('fill', 'none')
      .attr('stroke', 'var(--ds-raw-amber)')
      .attr('stroke-width', 1.6)
      .attr('d', lineOpen);
    g.append('path')
      .datum(parsed)
      .attr('fill', 'none')
      .attr('stroke', 'var(--ds-raw-green)')
      .attr('stroke-width', 1.6)
      .attr('d', lineClosed);

    const xAxis = d3.axisBottom(x).ticks(Math.min(6, parsed.length));
    g.append('g')
      .attr('transform', `translate(0,${innerH})`)
      .call(xAxis)
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');

    g.append('g')
      .call(d3.axisLeft(y).ticks(4).tickFormat(d3.format('d')))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');

    const legend = g.append('g').attr('transform', `translate(${innerW + 6},2)`);
    [
      ['Open', 'var(--ds-raw-amber)'],
      ['Closed', 'var(--ds-raw-green)'],
    ].forEach(([label, color], i) => {
      const row = legend.append('g').attr('transform', `translate(0,${i * 14})`);
      row.append('rect').attr('width', 8).attr('height', 8).attr('fill', color);
      row
        .append('text')
        .attr('x', 12)
        .attr('y', 8)
        .attr('font-size', '10px')
        .attr('fill', 'var(--ds-text-muted)')
        .text(label);
    });
  }, [data, width, height]);

  if (data.length === 0) return <p className="text-text-muted text-sm">No completion history</p>;
  return <svg ref={ref} width={width} height={height} />;
}
