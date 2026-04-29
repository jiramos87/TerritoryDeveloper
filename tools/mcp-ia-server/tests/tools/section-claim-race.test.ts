/**
 * PK INSERT-or-fail race test for `applySectionClaim` (TECH-4827).
 *
 * Two concurrent claims on same `(slug, section_id)`. PK uniqueness on
 * `ia_section_claims_pkey` serializes the INSERT; one path wins (`status:
 * claimed`), the other path either:
 *   (a) loses the INSERT race → throws `section_claim_held`, OR
 *   (b) observes the winner's row in its SELECT pre-check → refreshes
 *       `last_heartbeat` instead and returns `status: renewed`.
 *
 * Both interleavings leave exactly 1 open row (`released_at IS NULL`).
 * That single-open-row invariant is the assertion under test; status enum
 * partitioning is pre-condition noise.
 *
 * V2 row-only (mig 0052): no session_id column. The row IS the holder.
 *
 * Sandbox slug `__test_section_claim_race__` — disjoint per parallel
 * `node --test` worker. Reuses `seedCarcassHealthFixture(slug)` for the
 * minimal master-plan + section-A topology.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { applySectionClaim } from "../../src/tools/section-claim.js";
import {
  seedCarcassHealthFixture,
  teardownCarcassHealthFixture,
} from "../fixtures/parallel-carcass-health-mv.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_section_claim_race__";
const SECTION_ID = "A";

async function countOpenClaims(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ count: string }>(
    `SELECT count(*)::text AS count
       FROM ia_section_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return Number(res.rows[0]?.count ?? "0");
}

describe("section_claim — PK race (TECH-4827)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
    await seedCarcassHealthFixture(SLUG);
  });

  after(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
  });

  it("PK race — two concurrent claims yield exactly one open row", async () => {
    if (!pool) return;

    const results = await Promise.allSettled([
      applySectionClaim({ slug: SLUG, section_id: SECTION_ID }),
      applySectionClaim({ slug: SLUG, section_id: SECTION_ID }),
    ]);

    const fulfilled = results.filter(
      (r): r is PromiseFulfilledResult<Awaited<ReturnType<typeof applySectionClaim>>> =>
        r.status === "fulfilled",
    );
    const rejected = results.filter(
      (r): r is PromiseRejectedResult => r.status === "rejected",
    );

    const claimedCount = fulfilled.filter((r) => r.value.status === "claimed").length;
    const renewedCount = fulfilled.filter((r) => r.value.status === "renewed").length;
    const heldRejections = rejected.filter(
      (r) => (r.reason as { code?: string })?.code === "section_claim_held",
    ).length;

    assert.ok(
      claimedCount >= 1,
      `expected ≥1 claimed result, got claimed=${claimedCount} renewed=${renewedCount} held=${heldRejections}`,
    );
    assert.equal(
      claimedCount + renewedCount + heldRejections,
      2,
      "race must yield exactly two recognized outcomes (claimed/renewed/held)",
    );

    // Loser path is either renew (SELECT-saw-winner) or held (INSERT race loss).
    // Both leave one open row.
    const openCount = await countOpenClaims(SLUG);
    assert.equal(
      openCount,
      1,
      `expected exactly 1 open claim row post-race, got ${openCount}`,
    );
  });
});
