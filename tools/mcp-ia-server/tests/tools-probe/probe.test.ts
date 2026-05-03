import { test } from "node:test";
import assert from "node:assert/strict";
test("dynamic import works", async () => {
  const mod = await import("../../../scripts/claims-sweep-tick.js");
  console.log("keys:", Object.keys(mod));
  console.log("runSweepTick type:", typeof mod.runSweepTick);
});
