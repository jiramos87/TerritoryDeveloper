/**
 * recipe-engine seam step — unit tests (Phase E).
 *
 * Tests: validate-only regression; subagent dispatch with stubbed MCP;
 * dispatch_unavailable escalation; schema_out escalation; dry_run.
 */

import test, { describe, afterEach } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import os from "node:os";
import { setMcpInvoker } from "../../src/steps/mcp.js";
import { runSeamStep } from "../../src/steps/seam.js";
import type { SeamStep, RunContext, AuditSink } from "../../src/types.js";
import { makeSeamsRunStub } from "../helpers/stubSeamsRun.js";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..", "..", "..");

function makeCtx(overrides: Partial<RunContext> = {}): RunContext {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "recipe-test-"));
  const noop: AuditSink = {
    async begin() {},
    async end() {},
  };
  return {
    run_id: "test-run-1",
    recipe_slug: "test-recipe",
    inputs: {},
    vars: {},
    cwd: REPO_ROOT,
    dry_run: false,
    audit: noop,
    ...overrides,
  };
}

function makeStep(overrides: Partial<SeamStep> = {}): SeamStep {
  return {
    id: "s1",
    seam: "align-arch-decision",
    input: {
      decision_id: "DEC-A01",
      current_record: { title: "T", status: "active", body: "B" },
      proposed_change: "Update.",
    },
    ...overrides,
  };
}

afterEach(() => setMcpInvoker(undefined));

// ---------------------------------------------------------------------------
// dry_run
// ---------------------------------------------------------------------------

test("dry_run returns ok=true with dry_run:true", async () => {
  const ctx = makeCtx({ dry_run: true });
  const result = await runSeamStep(makeStep(), ctx);
  assert.equal(result.ok, true);
  const v = result.value as Record<string, unknown>;
  assert.equal(v["dry_run"], true);
  assert.equal(v["seam"], "align-arch-decision");
});

// ---------------------------------------------------------------------------
// validate-only (expected_output present)
// ---------------------------------------------------------------------------

describe("seam step — validate-only regression path", () => {
  test("expected_output passes schema → ok=true, dispatch_mode=validate-only", async () => {
    const ctx = makeCtx();
    const step = makeStep({
      expected_output: {
        aligned_record: { title: "T", status: "active", body: "Updated body." },
        change_kind: "amend",
        rationale: "Minor fix.",
      },
    } as never);
    const result = await runSeamStep(step, ctx);
    assert.equal(result.ok, true);
    const v = result.value as Record<string, unknown>;
    assert.equal(v["validated"], true);
    assert.equal(v["dispatch_mode"], "validate-only");
  });

  test("expected_output fails schema → ok=false + escalation handoff file", async () => {
    const ctx = makeCtx();
    const step = makeStep({
      expected_output: { wrong_field: "bad" },
    } as never);
    const result = await runSeamStep(step, ctx);
    assert.equal(result.ok, false);
    assert.equal(result.error?.code, "schema_out");
    const handoffDir = path.join(REPO_ROOT, "ia", "state", "recipe-runs", ctx.run_id);
    const handoffFile = path.join(handoffDir, `seam-${step.id}-error.md`);
    assert.ok(fs.existsSync(handoffFile), `Handoff file missing: ${handoffFile}`);
    const body = fs.readFileSync(handoffFile, "utf8");
    assert.ok(body.includes("schema_out"), "Handoff body missing schema_out");
    fs.rmSync(handoffDir, { recursive: true });
  });
});

// ---------------------------------------------------------------------------
// subagent dispatch path
// ---------------------------------------------------------------------------

describe("seam step — subagent dispatch", () => {
  test("happy path: MCP stub returns valid output → ok=true + token_totals", async () => {
    setMcpInvoker(makeSeamsRunStub("happy"));
    const ctx = makeCtx();
    const result = await runSeamStep(makeStep(), ctx);
    assert.equal(result.ok, true);
    const v = result.value as Record<string, unknown>;
    assert.equal(v["dispatch_mode"], "subagent");
    assert.equal(v["validated"], true);
    const totals = v["token_totals"] as Record<string, unknown>;
    assert.equal(totals["input_tokens"], 10);
  });

  test("dispatch_unavailable → ok=false + escalation handoff", async () => {
    setMcpInvoker(makeSeamsRunStub("dispatch_unavailable"));
    const ctx = makeCtx();
    const result = await runSeamStep(makeStep(), ctx);
    assert.equal(result.ok, false);
    assert.equal(result.error?.code, "dispatch_unavailable");
    const handoffDir = path.join(REPO_ROOT, "ia", "state", "recipe-runs", ctx.run_id);
    fs.rmSync(handoffDir, { recursive: true, force: true });
  });

  test("no MCP invoker → ok=false escalation", async () => {
    setMcpInvoker(undefined);
    const ctx = makeCtx();
    const result = await runSeamStep(makeStep(), ctx);
    assert.equal(result.ok, false);
    const handoffDir = path.join(REPO_ROOT, "ia", "state", "recipe-runs", ctx.run_id);
    fs.rmSync(handoffDir, { recursive: true, force: true });
  });

  test("seams_run invoke error → ok=false escalation", async () => {
    setMcpInvoker(makeSeamsRunStub("invoke_error"));
    const ctx = makeCtx();
    const result = await runSeamStep(makeStep(), ctx);
    assert.equal(result.ok, false);
    const handoffDir = path.join(REPO_ROOT, "ia", "state", "recipe-runs", ctx.run_id);
    fs.rmSync(handoffDir, { recursive: true, force: true });
  });
});
