/**
 * Snapshot GC cron route — POST coverage (TECH-2676 §Test Blueprint).
 *
 *   - With `gc.trigger` capability (admin role) → returns sweep result.
 *   - Without capability (viewer role) → returns 403 forbidden.
 *
 * `sweepRetiredSnapshots` is mocked so the test stays focused on the route's
 * auth + capability gating; the lib has its own coverage in
 * `web/lib/snapshot/__tests__/gc-sweep.test.ts`.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2676 §Plan Digest
 */

import {
  afterEach,
  beforeEach,
  describe,
  expect,
  test,
  vi,
} from "vitest";

import { POST } from "@/app/api/cron/snapshot-gc/route";

const ADMIN_USER_ID = "33333333-3333-4333-8333-333333333333";

let currentRole: "admin" | "viewer" = "admin";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: async () => ({
    id: ADMIN_USER_ID,
    email: "gc-route@example.com",
    role: currentRole,
  }),
}));

vi.mock("@/lib/auth/capabilities", () => ({
  loadCapabilitiesForRole: async (role: string) => {
    if (role === "admin") return new Set(["gc.trigger"]);
    return new Set<string>();
  },
  clearCapabilityCache: () => {},
}));

vi.mock("@/lib/snapshot/gc-sweep", () => ({
  sweepRetiredSnapshots: async () => ({
    removedCount: 2,
    removedIds: ["row-a", "row-b"],
  }),
  DEFAULT_MAX_AGE_DAYS: 7,
}));

beforeEach(() => {
  currentRole = "admin";
});

afterEach(() => {
  vi.clearAllMocks();
});

describe("POST /api/cron/snapshot-gc (TECH-2676)", () => {
  test("returns sweep result when caller has gc.trigger capability", async () => {
    const req = new Request("http://localhost/api/cron/snapshot-gc", {
      method: "POST",
    });
    const res = await POST(req as unknown as Parameters<typeof POST>[0]);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: true;
      data: { removedCount: number; removedIds: string[] };
    };
    expect(body.ok).toBe(true);
    expect(body.data.removedCount).toBe(2);
    expect(body.data.removedIds).toEqual(["row-a", "row-b"]);
  });

  test("returns 403 when caller lacks gc.trigger capability", async () => {
    currentRole = "viewer";
    const req = new Request("http://localhost/api/cron/snapshot-gc", {
      method: "POST",
    });
    const res = await POST(req as unknown as Parameters<typeof POST>[0]);
    expect(res.status).toBe(403);
    const body = (await res.json()) as { error: string; code: string };
    expect(body.code).toBe("forbidden");
  });
});
