'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface CycleHistogramProps {
  data: number[];
  width?: number;
  height?: number;
  binCount?: number;
}

export default function CycleHistogram({
  data,
  width = 480,
  height = 200,
  binCount = 12,
}: CycleHistogramProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || data.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    const margin = { top: 10, right: 16, bottom: 28, left: 36 };
    const innerW = width - margin.left - margin.right;
    const innerH = height - margin.top - margin.bottom;
    const g = svg
      .attr('width', width)
      .attr('height', height)
      .append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    const max = d3.max(data) ?? 1;
    const x = d3.scaleLinear().domain([0, max]).nice().range([0, innerW]);
    const bins = d3
      .bin<number, number>()
      .domain(x.domain() as [number, number])
      .thresholds(binCount)(data);

    const yMax = d3.max(bins, (b) => b.length) ?? 1;
    const y = d3.scaleLinear().domain([0, yMax]).nice().range([innerH, 0]);

    g.selectAll('rect')
      .data(bins)
      .enter()
      .append('rect')
      .attr('x', (b) => x(b.x0 ?? 0) + 1)
      .attr('y', (b) => y(b.length))
      .attr('width', (b) => Math.max(0, x(b.x1 ?? 0) - x(b.x0 ?? 0) - 2))
      .attr('height', (b) => innerH - y(b.length))
      .attr('fill', 'var(--ds-raw-blue)');

    g.append('g')
      .attr('transform', `translate(0,${innerH})`)
      .call(d3.axisBottom(x).ticks(6).tickFormat((d) => `${d}d`))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');

    g.append('g')
      .call(d3.axisLeft(y).ticks(4).tickFormat(d3.format('d')))
      .selectAll('text')
      .attr('fill', 'var(--ds-text-muted)');
  }, [data, width, height, binCount]);

  if (data.length === 0) return <p className="text-text-muted text-sm">No completed tasks</p>;
  return <svg ref={ref} width={width} height={height} />;
}
