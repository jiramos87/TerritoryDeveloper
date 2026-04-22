/**
 * CD `data.js` heatmap matrix — presentation-only; real dashboard tasks live in plan loaders.
 * Shape preserved for ScreenDashboard density strip (D4 port).
 */
export const CD_WEEK_DENSITY: { stage: string; cells: number[] }[] = [
  { stage: 'STAGE-4.1 Session handshake', cells: [3, 4, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0] },
  { stage: 'STAGE-4.2 Org scope middleware', cells: [0, 2, 5, 3, 1, 0, 0, 0, 0, 0, 0, 0] },
  { stage: 'STAGE-5.1 DB provisioning', cells: [0, 0, 0, 2, 4, 3, 1, 0, 0, 0, 0, 0] },
  { stage: 'STAGE-5.2 Schema + migrations', cells: [0, 0, 0, 0, 1, 3, 6, 5, 4, 2, 1, 0] },
  { stage: 'STAGE-5.3 Middleware + auth', cells: [0, 0, 0, 0, 0, 0, 1, 2, 4, 5, 3, 2] },
  { stage: 'STAGE-6.1 Smoke suite', cells: [0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 3] },
  { stage: 'STAGE-7.1 MDX pipeline', cells: [0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 4] },
];
