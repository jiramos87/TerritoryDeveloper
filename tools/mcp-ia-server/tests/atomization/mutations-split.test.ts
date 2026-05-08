/**
 * Atomization smoke test — mutations.ts split (Stage 6 / TECH-23779).
 *
 * Asserts:
 *   1. All mutation functions exported from the barrel mutations/index.ts.
 *   2. All mutation functions still reachable via the legacy mutations.ts
 *      re-export surface (callers unaffected).
 *   3. IaDbValidationError exported from both surfaces.
 *
 * No DB connection required — pure import resolution checks.
 *
 * §Red-Stage Proof anchor:
 *   tools/mcp-ia-server/tests/atomization/mutations-split.test.ts::*
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";

// Test the cluster modules directly.
import * as taskMod from "../../src/ia-db/mutations/task.js";
import * as stageMod from "../../src/ia-db/mutations/stage.js";
import * as masterPlanMod from "../../src/ia-db/mutations/master-plan.js";
import * as journalMod from "../../src/ia-db/mutations/journal.js";
import * as sharedMod from "../../src/ia-db/mutations/shared.js";

// Test the barrel index.
import * as barrelIdx from "../../src/ia-db/mutations/index.js";

// Test the legacy surface (mutations.ts thin barrel).
import * as legacySurface from "../../src/ia-db/mutations.js";

describe("mutations-split atomization", () => {
  // -------------------------------------------------------------------------
  // shared module
  // -------------------------------------------------------------------------
  describe("shared.ts", () => {
    it("exports IaDbValidationError", () => {
      assert.strictEqual(typeof sharedMod.IaDbValidationError, "function");
    });
    it("exports poolOrThrow", () => {
      assert.strictEqual(typeof sharedMod.poolOrThrow, "function");
    });
    it("exports withTx", () => {
      assert.strictEqual(typeof sharedMod.withTx, "function");
    });
    it("exports enqueueCacheBust", () => {
      assert.strictEqual(typeof sharedMod.enqueueCacheBust, "function");
    });
    it("exports PREFIX_SEQ with expected prefixes", () => {
      assert.strictEqual(typeof sharedMod.PREFIX_SEQ, "object");
      assert.ok("TECH" in sharedMod.PREFIX_SEQ);
      assert.ok("FEAT" in sharedMod.PREFIX_SEQ);
      assert.ok("BUG" in sharedMod.PREFIX_SEQ);
    });
  });

  // -------------------------------------------------------------------------
  // task cluster
  // -------------------------------------------------------------------------
  describe("task.ts", () => {
    const TASK_EXPORTS = [
      "mutateTaskInsert",
      "mutateTaskDepRegister",
      "mutateTaskRawMarkdownWrite",
      "mutateTaskStatusFlip",
      "mutateTaskStatusFlipBatch",
      "mutateTaskSpecSectionWrite",
      "mutateTaskCommitRecord",
      "mutateTaskBatchInsert",
    ] as const;
    for (const fn of TASK_EXPORTS) {
      it(`exports ${fn}`, () => {
        assert.strictEqual(typeof (taskMod as Record<string, unknown>)[fn], "function");
      });
    }
  });

  // -------------------------------------------------------------------------
  // stage cluster
  // -------------------------------------------------------------------------
  describe("stage.ts", () => {
    const STAGE_EXPORTS = [
      "mutateStageVerificationFlip",
      "mutateStageCloseoutApply",
      "mutateStageInsert",
      "mutateStageUpdate",
      "mutateStageBodyWrite",
      "mutateStageDecomposeApply",
    ] as const;
    for (const fn of STAGE_EXPORTS) {
      it(`exports ${fn}`, () => {
        assert.strictEqual(typeof (stageMod as Record<string, unknown>)[fn], "function");
      });
    }
  });

  // -------------------------------------------------------------------------
  // master-plan cluster
  // -------------------------------------------------------------------------
  describe("master-plan.ts", () => {
    const MP_EXPORTS = [
      "mutateMasterPlanPreambleWrite",
      "mutateMasterPlanDescriptionWrite",
      "mutateMasterPlanChangeLogAppend",
      "mutateMasterPlanInsert",
      "mutateMasterPlanClose",
      "mutateMasterPlanVersionCreate",
    ] as const;
    for (const fn of MP_EXPORTS) {
      it(`exports ${fn}`, () => {
        assert.strictEqual(typeof (masterPlanMod as Record<string, unknown>)[fn], "function");
      });
    }
  });

  // -------------------------------------------------------------------------
  // journal cluster
  // -------------------------------------------------------------------------
  describe("journal.ts", () => {
    const JOURNAL_EXPORTS = [
      "mutateJournalAppend",
      "mutateFixPlanWrite",
      "mutateFixPlanConsume",
    ] as const;
    for (const fn of JOURNAL_EXPORTS) {
      it(`exports ${fn}`, () => {
        assert.strictEqual(typeof (journalMod as Record<string, unknown>)[fn], "function");
      });
    }
  });

  // -------------------------------------------------------------------------
  // barrel index — everything reachable via mutations/index.ts
  // -------------------------------------------------------------------------
  describe("mutations/index.ts barrel", () => {
    const ALL_EXPORTS = [
      "IaDbValidationError",
      "mutateTaskInsert",
      "mutateTaskDepRegister",
      "mutateTaskRawMarkdownWrite",
      "mutateTaskStatusFlip",
      "mutateTaskStatusFlipBatch",
      "mutateTaskSpecSectionWrite",
      "mutateTaskCommitRecord",
      "mutateTaskBatchInsert",
      "mutateStageVerificationFlip",
      "mutateStageCloseoutApply",
      "mutateStageInsert",
      "mutateStageUpdate",
      "mutateStageBodyWrite",
      "mutateStageDecomposeApply",
      "mutateMasterPlanPreambleWrite",
      "mutateMasterPlanDescriptionWrite",
      "mutateMasterPlanChangeLogAppend",
      "mutateMasterPlanInsert",
      "mutateMasterPlanClose",
      "mutateMasterPlanVersionCreate",
      "mutateJournalAppend",
      "mutateFixPlanWrite",
      "mutateFixPlanConsume",
    ] as const;
    for (const name of ALL_EXPORTS) {
      it(`barrel re-exports ${name}`, () => {
        assert.ok(
          typeof (barrelIdx as Record<string, unknown>)[name] !== "undefined",
          `${name} missing from mutations/index.ts`,
        );
      });
    }
  });

  // -------------------------------------------------------------------------
  // legacy surface — mutations.ts thin barrel
  // -------------------------------------------------------------------------
  describe("mutations.ts legacy surface (backward compat)", () => {
    const LEGACY_EXPORTS = [
      "IaDbValidationError",
      "mutateTaskInsert",
      "mutateTaskDepRegister",
      "mutateTaskRawMarkdownWrite",
      "mutateTaskStatusFlip",
      "mutateTaskStatusFlipBatch",
      "mutateTaskSpecSectionWrite",
      "mutateTaskCommitRecord",
      "mutateTaskBatchInsert",
      "mutateStageVerificationFlip",
      "mutateStageCloseoutApply",
      "mutateStageInsert",
      "mutateStageUpdate",
      "mutateStageBodyWrite",
      "mutateStageDecomposeApply",
      "mutateMasterPlanPreambleWrite",
      "mutateMasterPlanDescriptionWrite",
      "mutateMasterPlanChangeLogAppend",
      "mutateMasterPlanInsert",
      "mutateMasterPlanClose",
      "mutateMasterPlanVersionCreate",
      "mutateJournalAppend",
      "mutateFixPlanWrite",
      "mutateFixPlanConsume",
      // Read helpers re-exported by legacy barrel.
      "queryTaskBody",
      "queryTaskState",
    ] as const;
    for (const name of LEGACY_EXPORTS) {
      it(`legacy mutations.ts still exports ${name}`, () => {
        assert.ok(
          typeof (legacySurface as Record<string, unknown>)[name] !== "undefined",
          `${name} missing from legacy mutations.ts surface`,
        );
      });
    }

    it("IaDbValidationError is same class across surfaces", () => {
      assert.strictEqual(
        legacySurface.IaDbValidationError,
        barrelIdx.IaDbValidationError,
      );
    });
  });
});
