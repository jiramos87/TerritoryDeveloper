'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface VelocityPoint {
  week: string;
  count: number;
}

export interface VelocityAreaChartProps {
  data: VelocityPoint[];
  width?: number;
  height?: number;
}

export default function VelocityAreaChart({
  data,
  width = 480,
  height = 160,
}: VelocityAreaChartProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || data.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    const margin = { top: 8, right: 12, bottom: 24, left: 30 };
    const innerW = width - margin.left - margin.right;
    const innerH = height - margin.top - margin.bottom;

    const g = svg
      .attr('width', width)
      .attr('height', height)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const parsed = data.map((d) => ({ ...d, t: new Date(d.week) }));
    const x = d3
      .scaleTime()
      .domain(d3.extent(parsed, (d) => d.t) as [Date, Date])
      .range([0, innerW]);
    const y = d3
      .scaleLinear()
      .domain([0, d3.max(parsed, (d) => d.count) ?? 1])
      .nice()
      .range([innerH, 0]);

    const area = d3
      .area<{ t: Date; count: number }>()
      .x((d) => x(d.t))
      .y0(innerH)
      .y1((d) => y(d.count))
      .curve(d3.curveMonotoneX);

    g.append('path')
      .datum(parsed)
      .attr('fill', 'var(--ds-raw-amber)')
      .attr('fill-opacity', 0.35)
      .attr('stroke', 'var(--ds-raw-amber)')
      .attr('stroke-width', 1.4)
      .attr('d', area);

    g.append('g')
      .attr('transform', `translate(0,${innerH})`)
      .call(d3.axisBottom(x).ticks(4))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');
    g.append('g')
      .call(d3.axisLeft(y).ticks(3).tickFormat(d3.format('d')))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');
  }, [data, width, height]);

  if (data.length === 0)
    return <p className="text-text-muted text-sm">No velocity data</p>;
  return <svg ref={ref} width={width} height={height} />;
}
