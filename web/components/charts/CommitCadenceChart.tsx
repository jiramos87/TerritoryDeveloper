'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface CommitCadencePoint {
  stageId: string;
  date: string;
  count: number;
}

export interface CommitCadenceChartProps {
  data: CommitCadencePoint[];
  stageIds: string[];
  width?: number;
  height?: number;
}

export default function CommitCadenceChart({
  data,
  stageIds,
  width = 720,
  height = 220,
}: CommitCadenceChartProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || data.length === 0 || stageIds.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    const margin = { top: 10, right: 16, bottom: 28, left: 60 };
    const innerW = width - margin.left - margin.right;
    const innerH = height - margin.top - margin.bottom;

    const g = svg
      .attr('width', width)
      .attr('height', height)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const dates = Array.from(new Set(data.map((d) => d.date))).sort();
    if (dates.length === 0) return;
    const x = d3
      .scaleTime()
      .domain([new Date(dates[0]), new Date(dates[dates.length - 1])])
      .range([0, innerW]);

    const y = d3
      .scaleBand<string>()
      .domain(stageIds)
      .range([0, innerH])
      .padding(0.15);

    const maxN = d3.max(data, (d) => d.count) ?? 1;
    const r = d3.scaleSqrt().domain([0, maxN]).range([1, Math.min(10, y.bandwidth() / 2)]);

    g.selectAll('circle')
      .data(data)
      .enter()
      .append('circle')
      .attr('cx', (d) => x(new Date(d.date)))
      .attr('cy', (d) => (y(d.stageId) ?? 0) + y.bandwidth() / 2)
      .attr('r', (d) => r(d.count))
      .attr('fill', 'var(--ds-raw-amber)')
      .attr('fill-opacity', 0.8);

    g.append('g')
      .attr('transform', `translate(0,${innerH})`)
      .call(d3.axisBottom(x).ticks(6))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');

    g.append('g')
      .call(d3.axisLeft(y).tickFormat((d) => `Stage ${d}`))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');
  }, [data, stageIds, width, height]);

  if (data.length === 0) return <p className="text-text-muted text-sm">No commit data</p>;
  return <svg ref={ref} width={width} height={height} />;
}
