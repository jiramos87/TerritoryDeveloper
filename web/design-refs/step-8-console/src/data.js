// Shared seed data — Territory Developer master plan pilot.
// Caveman-style identifiers (STEP-X, STAGE-X.Y, T-NNN).

const MASTER_PLAN = [
  {
    id: "STEP-4", title: "Auth gate", status: "done", owner: "agent",
    objective: "Stand up the public-vs-internal split. Session + org scoping.",
    stages: [
      { id: "STAGE-4.1", title: "Session handshake", status: "done",
        phases: [
          { id: "PHASE-4.1.a", title: "Cookie + JWT", status: "done",
            tasks: [
              { id: "T-201", title: "Signed cookie helper", status: "done" },
              { id: "T-202", title: "Rotation schedule", status: "done" },
              { id: "T-203", title: "Expiry buffer", status: "done" },
            ]},
        ]},
      { id: "STAGE-4.2", title: "Org scope middleware", status: "done",
        phases: [
          { id: "PHASE-4.2.a", title: "Matcher + redirect", status: "done",
            tasks: [
              { id: "T-210", title: "Route matcher", status: "done" },
              { id: "T-211", title: "Sign-in redirect", status: "done" },
            ]},
        ]},
    ]
  },
  {
    id: "STEP-5", title: "Portal infrastructure", status: "progress", owner: "agent",
    objective: "Wire Neon DB + auth middleware. Architecture-only at this tier — no migrations run in prod.",
    stages: [
      { id: "STAGE-5.1", title: "DB provisioning", status: "done",
        phases: [
          { id: "PHASE-5.1.a", title: "Neon instance", status: "done",
            tasks: [
              { id: "T-240", title: "Project + branch", status: "done" },
              { id: "T-241", title: "Role + grants", status: "done" },
            ]},
        ]},
      { id: "STAGE-5.2", title: "Schema + migrations", status: "progress",
        phases: [
          { id: "PHASE-5.2.a", title: "Core tables", status: "progress",
            tasks: [
              { id: "T-251", title: "Draft users table", status: "done" },
              { id: "T-252", title: "Draft sessions table", status: "done" },
              { id: "T-253", title: "Orgs table", status: "pending" },
              { id: "T-254", title: "Drizzle snapshot commit", status: "progress" },
            ]},
          { id: "PHASE-5.2.b", title: "Billing slot", status: "blocked",
            tasks: [
              { id: "T-260", title: "Payment gateway slot", status: "blocked", reason: "Vendor decision pending" },
              { id: "T-261", title: "Invoice snapshot table", status: "pending" },
            ]},
        ]},
      { id: "STAGE-5.3", title: "Middleware + auth", status: "pending", owner: "jiramos87",
        phases: [
          { id: "PHASE-5.3.a", title: "Auth matcher", status: "pending",
            tasks: [
              { id: "T-358", title: "Auth middleware matcher", status: "pending" },
              { id: "T-359", title: "Sign-in redirect target", status: "pending" },
              { id: "T-360", title: "Token refresh loop", status: "pending" },
            ]},
        ]},
    ]
  },
  {
    id: "STEP-6", title: "E2E coverage", status: "pending", owner: "agent",
    objective: "Playwright against preview deploys. Smoke + deep scenarios.",
    stages: [
      { id: "STAGE-6.1", title: "Smoke suite", status: "pending",
        phases: [
          { id: "PHASE-6.1.a", title: "Landing + auth", status: "pending",
            tasks: [
              { id: "T-410", title: "Landing loads", status: "pending" },
              { id: "T-411", title: "Sign-in redirect", status: "pending" },
            ]},
        ]},
    ]
  },
  {
    id: "STEP-7", title: "Public site", status: "pending", owner: "agent",
    objective: "Game marketing surface. Reuses dev tokens, no hero illustration.",
    stages: [
      { id: "STAGE-7.1", title: "MDX pipeline", status: "pending",
        phases: [{ id: "PHASE-7.1.a", title: "Content routes", status: "pending", tasks: [
          { id: "T-510", title: "Landing MDX", status: "pending" },
          { id: "T-511", title: "About MDX", status: "pending" },
        ]}]},
    ]
  },
  {
    id: "STEP-8", title: "Dev dashboard pilot", status: "progress", owner: "agent",
    objective: "5-screen pilot — landing, master plan, releases, deep dive, design guide.",
    stages: [
      { id: "STAGE-8.1", title: "Screens", status: "progress",
        phases: [{ id: "PHASE-8.1.a", title: "Hi-fi pass", status: "progress", tasks: [
          { id: "T-601", title: "Landing", status: "progress" },
          { id: "T-602", title: "Dashboard tree", status: "pending" },
          { id: "T-603", title: "Release table", status: "pending" },
          { id: "T-604", title: "Progress deep-dive", status: "pending" },
          { id: "T-605", title: "Design guide", status: "pending" },
        ]}]},
    ]
  },
  {
    id: "STEP-9", title: "Backlog triage", status: "pending", owner: "agent",
    objective: "Post-pilot — grooming + re-stacking.",
    stages: []
  },
];

const RELEASES = [
  { id: "v0.4.0-alpha", title: "Portal infra + auth gate",          status: "progress", done: 7,  total: 12, owner: "agent",     date: "2026-04-18" },
  { id: "v0.3.2",       title: "Dashboard filters + URL multi-sel", status: "done",     done: 8,  total: 8,  owner: "agent",     date: "2026-04-02" },
  { id: "v0.3.1",       title: "StatBar thresholds + token map",    status: "done",     done: 6,  total: 6,  owner: "agent",     date: "2026-03-24" },
  { id: "v0.3.0",       title: "Hi-fi reskin of primitives",         status: "done",     done: 14, total: 14, owner: "agent",     date: "2026-03-10" },
  { id: "v0.2.4",       title: "Release picker scaffold",            status: "done",     done: 5,  total: 5,  owner: "agent",     date: "2026-02-28" },
  { id: "v0.5.0-draft", title: "Sim-core terrain rework",            status: "pending",  done: 0,  total: 24, owner: "jiramos87", date: "—"        },
  { id: "v0.4.1-hotfx", title: "Payment gateway slot",               status: "blocked",  done: 1,  total: 4,  owner: "agent",     date: "2026-04-10" },
  { id: "v0.2.3",       title: "Heatmap prototype",                  status: "done",     done: 3,  total: 3,  owner: "agent",     date: "2026-02-14" },
];

// 7 stages × 12 weeks of task counts (0..8). 0 => null bucket, 1 low, 2-3 mid, 4-5 high, 6+ peak.
const WEEK_DENSITY = [
  { stage: "STAGE-4.1 Session handshake",  cells: [3,4,2,1,0,0,0,0,0,0,0,0] },
  { stage: "STAGE-4.2 Org scope middleware", cells: [0,2,5,3,1,0,0,0,0,0,0,0] },
  { stage: "STAGE-5.1 DB provisioning",    cells: [0,0,0,2,4,3,1,0,0,0,0,0] },
  { stage: "STAGE-5.2 Schema + migrations", cells: [0,0,0,0,1,3,6,5,4,2,1,0] },
  { stage: "STAGE-5.3 Middleware + auth",  cells: [0,0,0,0,0,0,1,2,4,5,3,2] },
  { stage: "STAGE-6.1 Smoke suite",        cells: [0,0,0,0,0,0,0,0,1,2,2,3] },
  { stage: "STAGE-7.1 MDX pipeline",       cells: [0,0,0,0,0,0,0,0,0,1,2,4] },
];

function densityBucket(n) {
  if (n === 0) return "h-null";
  if (n <= 1) return "h-low";
  if (n <= 3) return "h-mid";
  if (n <= 5) return "h-high";
  return "h-peak";
}

function statusLabel(s) {
  return { done: "Done", progress: "In Progress", pending: "Pending", blocked: "Blocked" }[s] || s;
}

function rollup(node) {
  // Recursive count: { done, total }
  if (node.tasks) {
    const total = node.tasks.length;
    const done = node.tasks.filter(t => t.status === "done").length;
    return { done, total };
  }
  const children = node.phases || node.stages || [];
  return children.reduce((acc, c) => {
    const r = rollup(c);
    return { done: acc.done + r.done, total: acc.total + r.total };
  }, { done: 0, total: 0 });
}

// Flat task list for dashboard summary
function flattenTasks(plan) {
  const out = [];
  plan.forEach(step => (step.stages || []).forEach(stage =>
    (stage.phases || []).forEach(phase =>
      (phase.tasks || []).forEach(t => out.push({ ...t, step: step.id, stage: stage.id, phase: phase.id, stepTitle: step.title, stageTitle: stage.title })))));
  return out;
}

window.MASTER_PLAN = MASTER_PLAN;
window.RELEASES = RELEASES;
window.WEEK_DENSITY = WEEK_DENSITY;
window.statusLabel = statusLabel;
window.densityBucket = densityBucket;
window.rollup = rollup;
window.flattenTasks = flattenTasks;
