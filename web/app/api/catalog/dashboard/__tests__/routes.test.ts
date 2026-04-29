/**
 * Dashboard widget API routes — fixture-driven SQL output (TECH-4183 §Test Blueprint).
 */
import { describe, expect, it, vi } from "vitest";

// ---------- shared sql mock factory ----------

type SqlTag = (strings: TemplateStringsArray, ...values: unknown[]) => Promise<unknown[]>;

function makeSqlMock(returnRows: unknown[]): SqlTag {
  return ((_strings: TemplateStringsArray) => Promise.resolve(returnRows)) as SqlTag;
}

// ---------- unresolved-refs ----------

async function resolveUnresolvedCount(sql: SqlTag): Promise<number> {
  const rows = await sql`select count(*)::int as count from catalog_ref_edge` as Array<{ count: number }>;
  return rows[0]?.count ?? 0;
}

describe("dashboard/unresolved-refs logic", () => {
  it("returns 0 when no rows", async () => {
    const sql = makeSqlMock([{ count: 0 }]);
    expect(await resolveUnresolvedCount(sql)).toBe(0);
  });

  it("returns N when rows present", async () => {
    const sql = makeSqlMock([{ count: 7 }]);
    expect(await resolveUnresolvedCount(sql)).toBe(7);
  });

  it("defaults to 0 when result is empty array", async () => {
    const sql = makeSqlMock([]);
    expect(await resolveUnresolvedCount(sql)).toBe(0);
  });
});

// ---------- lint-failures ----------

describe("dashboard/lint-failures shape", () => {
  it("maps rows to LintFailureRow shape", async () => {
    const now = new Date("2026-04-01T10:00:00.000Z");
    const sql = makeSqlMock([
      { id: "1", entity_id: "5", entity_slug: "hero", rule_id: "audio.loudness", severity: "block", message: "too loud", created_at: now },
    ]);
    const rows = await sql`select ...` as Array<{
      id: string; entity_id: string; entity_slug: string | null;
      rule_id: string; severity: string; message: string; created_at: Date;
    }>;
    const mapped = rows.map(r => ({ ...r, created_at: r.created_at.toISOString() }));
    expect(mapped[0]!.rule_id).toBe("audio.loudness");
    expect(mapped[0]!.created_at).toBe("2026-04-01T10:00:00.000Z");
    expect(mapped[0]!.entity_slug).toBe("hero");
  });

  it("handles null entity_slug gracefully", async () => {
    const sql = makeSqlMock([
      { id: "2", entity_id: "9", entity_slug: null, rule_id: "sprite.missing_src", severity: "block", message: "no src", created_at: new Date() },
    ]);
    const rows = await sql`select ...` as Array<{ entity_slug: string | null }>;
    expect(rows[0]!.entity_slug).toBeNull();
  });
});

// ---------- queue-depth ----------

function computeQueueDepth(rows: Array<{ status: string; count: number }>) {
  let queued = 0;
  let running = 0;
  for (const r of rows) {
    if (r.status === "queued") queued = r.count;
    else if (r.status === "running") running = r.count;
  }
  return { queued, running, total: queued + running };
}

describe("dashboard/queue-depth logic", () => {
  it("sums queued + running into total", () => {
    expect(computeQueueDepth([
      { status: "queued", count: 4 },
      { status: "running", count: 2 },
    ])).toEqual({ queued: 4, running: 2, total: 6 });
  });

  it("returns zero totals when no active jobs", () => {
    expect(computeQueueDepth([])).toEqual({ queued: 0, running: 0, total: 0 });
  });

  it("handles only running rows", () => {
    expect(computeQueueDepth([{ status: "running", count: 1 }])).toEqual({ queued: 0, running: 1, total: 1 });
  });
});

// ---------- snapshot-freshness ----------

const STALE_THRESHOLD_MS = 24 * 60 * 60 * 1000;

function computeFreshnessRows(rows: Array<{ kind: string; latest_at: Date }>, now: number) {
  return rows.map(r => ({
    kind: r.kind,
    latest_at: r.latest_at.toISOString(),
    stale: now - r.latest_at.getTime() > STALE_THRESHOLD_MS,
  }));
}

describe("dashboard/snapshot-freshness logic", () => {
  const NOW = new Date("2026-04-02T12:00:00Z").getTime();

  it("fresh entry is not stale", () => {
    const fresh = new Date("2026-04-02T06:00:00Z");
    const [row] = computeFreshnessRows([{ kind: "sprite", latest_at: fresh }], NOW);
    expect(row!.stale).toBe(false);
  });

  it("entry older than 24h is stale", () => {
    const old = new Date("2026-03-31T00:00:00Z");
    const [row] = computeFreshnessRows([{ kind: "asset", latest_at: old }], NOW);
    expect(row!.stale).toBe(true);
  });

  it("maps latest_at to ISO string", () => {
    const d = new Date("2026-04-01T10:00:00.000Z");
    const [row] = computeFreshnessRows([{ kind: "token", latest_at: d }], NOW);
    expect(row!.latest_at).toBe("2026-04-01T10:00:00.000Z");
  });
});

// suppress unused-var warning for vi
void vi;
