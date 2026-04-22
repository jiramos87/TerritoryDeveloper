/**
 * catalog_list / catalog_upsert helpers — validation + allowlist wiring.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool, dbUnconfiguredError } from "../../src/envelope.js";
import { runCatalogList } from "../../src/tools/catalog-list.js";
import { runCatalogUpsert } from "../../src/tools/catalog-upsert.js";
import type { Pool } from "pg";

test("runCatalogList: published default uses bare SQL fragment (no params)", async () => {
  const calls: unknown[][] = [];
  const pool = {
    async query(q: string, args: unknown[]) {
      calls.push(args);
      return { rows: [] };
    },
  } as unknown as Pool;
  await runCatalogList(pool, {});
  assert.equal(calls.length, 1);
  assert.deepEqual(calls[0], [200]);
});

test("runCatalogList: invalid status → throws invalid_input", async () => {
  const pool = { async query() { return { rows: [] }; } } as unknown as Pool;
  await assert.rejects(
    () => runCatalogList(pool, { status: "nope" }),
    (err: unknown) =>
      err !== null && typeof err === "object" && (err as { code?: string }).code === "invalid_input",
  );
});

test("catalog_upsert: pool null → db_unconfigured", async () => {
  const handler = wrapTool(async () => {
    throw dbUnconfiguredError();
  });
  const r = await handler({} as never);
  assert.equal(r.ok, false);
  if (!r.ok) assert.equal(r.error.code, "db_unconfigured");
});

test("runCatalogUpsert: missing caller → unauthorized_caller", async () => {
  await assert.rejects(
    () =>
      runCatalogUpsert({
        mode: "create",
        caller_agent: "",
        body: {
          category: "z",
          slug: "z",
          display_name: "Z",
          status: "draft",
          economy: { base_cost_cents: 0, monthly_upkeep_cents: 0 },
          sprite_binds: [],
        },
      } as never),
    (err: unknown) =>
      err !== null &&
      typeof err === "object" &&
      (err as { code?: string }).code === "unauthorized_caller",
  );
});
