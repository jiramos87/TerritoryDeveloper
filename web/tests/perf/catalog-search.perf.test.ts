/**
 * Catalog search perf smoke — asserts query returns within 200 ms
 * against a dev DB with pg_trgm GIN indexes (0051_pg_trgm_search.sql).
 *
 * Skipped when DATABASE_URL is unset (CI without DB).
 * Run standalone: npm --prefix web run test -- web/tests/perf/catalog-search.perf.test.ts
 */

import { afterAll, describe, expect, it } from "vitest";

const DB_URL = process.env["DATABASE_URL"];
const RUN = DB_URL != null;

describe.skipIf(!RUN)("catalog search perf", () => {
  let sql: Awaited<ReturnType<typeof import("@/lib/db/client").getSql>> | undefined;

  afterAll(async () => {
    // postgres-js connections close on process exit; explicit end not required.
  });

  it("similarity query completes < 200 ms with GIN index", async () => {
    const { getSql } = await import("@/lib/db/client");
    sql = getSql();
    const start = Date.now();
    await sql`
      SELECT
        e.id::text AS entity_id,
        e.kind,
        e.slug,
        e.display_name,
        similarity(lower(coalesce(e.display_name, e.slug)), 'sprite') AS score
      FROM catalog_entity e
      WHERE e.retired_at IS NULL
        AND lower(coalesce(e.display_name, e.slug)) % 'sprite'
      ORDER BY score DESC, e.display_name ASC
      LIMIT 20
    `;
    const elapsed = Date.now() - start;
    expect(elapsed).toBeLessThan(200);
  });

  it("searchCatalogEntities returns valid shape", async () => {
    const { searchCatalogEntities } = await import("@/lib/catalog/search-query");
    const result = await searchCatalogEntities({ q: "a", limit: 5 });
    expect(result).toHaveProperty("results");
    expect(result).toHaveProperty("total");
    expect(Array.isArray(result.results)).toBe(true);
    expect(typeof result.total).toBe("number");
  });
});
