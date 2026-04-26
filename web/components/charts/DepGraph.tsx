'use client';

import { useEffect, useRef } from 'react';
import * as d3 from 'd3';

export interface DepNode extends d3.SimulationNodeDatum {
  id: string;
  title: string;
  status: string;
  external: boolean;
  stage?: string | null;
}

export interface DepLink extends d3.SimulationLinkDatum<DepNode> {
  source: string | DepNode;
  target: string | DepNode;
  kind: 'depends_on' | 'related';
}

export interface DepGraphProps {
  nodes: DepNode[];
  links: DepLink[];
  width?: number;
  height?: number;
}

const NODE_FILL = 'var(--ds-raw-blue)';
const NODE_FILL_EXTERNAL = 'var(--ds-raw-amber)';
const EDGE_COLOR = '#7fa3c4';

export default function DepGraph({ nodes, links, width = 960, height = 540 }: DepGraphProps) {
  const ref = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!ref.current || nodes.length === 0) return;
    const svg = d3.select(ref.current);
    svg.selectAll('*').remove();

    // Connected-node set so isolated nodes can be faded.
    const connected = new Set<string>();
    for (const l of links) {
      connected.add(typeof l.source === 'string' ? l.source : l.source.id);
      connected.add(typeof l.target === 'string' ? l.target : l.target.id);
    }
    const isConnected = (d: DepNode) => connected.has(d.id);

    // Virtual canvas scales with node count so dense graphs are not crushed.
    const area = Math.max(width * height, nodes.length * 3500);
    const aspect = width / height;
    const innerH = Math.sqrt(area / aspect);
    const innerW = innerH * aspect;

    svg
      .attr('viewBox', `0 0 ${innerW} ${innerH}`)
      .attr('preserveAspectRatio', 'xMidYMid meet')
      .style('width', '100%')
      .style('height', 'auto')
      .style('max-height', `${height}px`)
      .style('cursor', 'grab');

    // Tiny arrow marker for depends_on direction.
    const defs = svg.append('defs');
    defs
      .append('marker')
      .attr('id', 'dep-arrow')
      .attr('viewBox', '0 -4 8 8')
      .attr('refX', 10)
      .attr('refY', 0)
      .attr('markerWidth', 5)
      .attr('markerHeight', 5)
      .attr('orient', 'auto')
      .append('path')
      .attr('d', 'M0,-3L8,0L0,3')
      .attr('fill', EDGE_COLOR);

    const root = svg.append('g').attr('class', 'zoom-root');

    const linkDist = nodes.length > 40 ? 60 : 80;
    const charge = nodes.length > 40 ? -120 : -200;
    const collideR = nodes.length > 40 ? 18 : 24;
    const nodeR = nodes.length > 40 ? 5 : 7;
    const nodeRext = nodes.length > 40 ? 6 : 9;
    const fontSize = nodes.length > 40 ? 9 : 10;

    // Sim mutates these copies in place — render selections must reference the
    // SAME arrays (otherwise forceLink resolves source/target on the copy but
    // the rendered `<line>` data still holds the original string ids → NaN).
    const simNodes: DepNode[] = nodes.map((n) => ({ ...n }));
    const simLinks: DepLink[] = links.map((l) => ({ ...l }));

    const sim = d3
      .forceSimulation<DepNode>(simNodes)
      .force(
        'link',
        d3
          .forceLink<DepNode, DepLink>(simLinks)
          .id((d) => d.id)
          .distance(linkDist)
          .strength(0.7),
      )
      .force('charge', d3.forceManyBody().strength(charge))
      .force('center', d3.forceCenter(innerW / 2, innerH / 2))
      .force('x', d3.forceX(innerW / 2).strength(0.04))
      .force('y', d3.forceY(innerH / 2).strength(0.04))
      .force('collision', d3.forceCollide<DepNode>().radius(collideR));

    const link = root
      .append('g')
      .selectAll('line')
      .data(simLinks)
      .enter()
      .append('line')
      .attr('stroke', EDGE_COLOR)
      .attr('stroke-opacity', 0.9)
      .attr('stroke-width', 1.6)
      .attr('vector-effect', 'non-scaling-stroke')
      .attr('stroke-dasharray', (d) => (d.kind === 'related' ? '4 3' : null))
      .attr('marker-end', (d) => (d.kind === 'depends_on' ? 'url(#dep-arrow)' : null));

    const node = root
      .append('g')
      .selectAll<SVGGElement, DepNode>('g')
      .data(simNodes)
      .enter()
      .append('g')
      .attr('opacity', (d) => (isConnected(d) ? 1 : 0.25));

    node
      .append('circle')
      .attr('r', (d) => (d.external ? nodeRext : nodeR))
      .attr('fill', (d) => (d.external ? NODE_FILL_EXTERNAL : NODE_FILL))
      .attr('stroke', 'var(--ds-bg-panel)')
      .attr('stroke-width', 1.5);

    node
      .append('title')
      .text(
        (d) =>
          `${d.id} — ${d.title}\nstage ${d.stage ?? '—'} · ${d.status}${d.external ? ' (cross-plan)' : ''}`,
      );

    node
      .append('text')
      .attr('dx', (d) => (d.external ? nodeRext : nodeR) + 4)
      .attr('dy', 4)
      .attr('font-family', 'var(--font-mono, ui-monospace)')
      .attr('font-size', `${fontSize}px`)
      .attr('fill', 'var(--ds-text-muted)')
      .text((d) => d.id);

    sim.on('tick', () => {
      link
        .attr('x1', (d) => (d.source as unknown as DepNode & { x: number }).x)
        .attr('y1', (d) => (d.source as unknown as DepNode & { y: number }).y)
        .attr('x2', (d) => (d.target as unknown as DepNode & { x: number }).x)
        .attr('y2', (d) => (d.target as unknown as DepNode & { y: number }).y);
      node.attr(
        'transform',
        (d) => `translate(${(d as DepNode & { x: number }).x},${(d as DepNode & { y: number }).y})`,
      );
    });

    // Pan + zoom (scroll-wheel / drag). Keep root group as zoom target.
    const zoom = d3
      .zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.2, 4])
      .on('zoom', (event) => {
        root.attr('transform', event.transform.toString());
      });
    svg.call(zoom);
    // Fit-to-viewport on first layout (after sim has settled a bit).
    setTimeout(() => {
      const bbox = (root.node() as SVGGElement | null)?.getBBox();
      if (!bbox || bbox.width === 0) return;
      const pad = 16;
      const fitScale = Math.min(
        innerW / (bbox.width + pad * 2),
        innerH / (bbox.height + pad * 2),
        1,
      );
      // Don't zoom out further than 0.6 — beyond that edges + labels become illegible.
      const scale = Math.max(fitScale, 0.6);
      const tx = innerW / 2 - scale * (bbox.x + bbox.width / 2);
      const ty = innerH / 2 - scale * (bbox.y + bbox.height / 2);
      svg
        .transition()
        .duration(400)
        .call(zoom.transform, d3.zoomIdentity.translate(tx, ty).scale(scale));
    }, 600);

    return () => {
      sim.stop();
    };
  }, [nodes, links, width, height]);

  if (nodes.length === 0) return <p className="text-text-muted text-sm">No dependencies</p>;
  return <svg ref={ref} />;
}
