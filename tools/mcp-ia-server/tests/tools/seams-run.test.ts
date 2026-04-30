/**
 * seams_run MCP tool — Phase E unit tests.
 *
 * Uses stub SubagentDispatchHandler so no real LLM calls occur.
 * Tests: validate-only regression, subagent stubbed dispatch,
 * schema_out reject, headless fallback, align-arch-decision enum.
 */

import test, { describe, afterEach } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  setSubagentDispatchHandler,
  type SubagentDispatchHandler,
} from "../../src/tools/seams-run.js";

const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..", "..", "..");

// Import handler directly (not via MCP server) to avoid needing a live Postgres DB
import { registerSeamsRun } from "../../src/tools/seams-run.js";

// Minimal MCP server stub to capture registered handler
interface ToolHandler {
  (args: unknown): Promise<unknown>;
}
function makeServerStub(): { handler: ToolHandler | undefined; registerTool: (name: string, schema: unknown, fn: ToolHandler) => void } {
  const stub = { handler: undefined as ToolHandler | undefined };
  stub.registerTool = (_name: string, _schema: unknown, fn: ToolHandler) => {
    stub.handler = fn;
  };
  return stub;
}

function extractPayload(result: unknown): unknown {
  const r = result as { content?: Array<{ text?: string }> };
  if (!r?.content?.[0]?.text) return result;
  const parsed = JSON.parse(r.content[0].text);
  if (parsed.ok === true && "payload" in parsed) return parsed.payload;
  if (parsed.ok === false) throw Object.assign(new Error(parsed.error?.message ?? "mcp error"), parsed.error ?? {});
  return parsed;
}

// ---------------------------------------------------------------------------
// validate-only regression
// ---------------------------------------------------------------------------

describe("seams_run — validate-only path", () => {
  afterEach(() => setSubagentDispatchHandler(undefined));

  test("validate-pair returns validated input+output for align-glossary", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    const input = {
      spec_body: "Use HeightMap for terrain.",
      glossary_table: [{ term: "HeightMap", definition: "Terrain elevation store" }],
    };
    const output = { replacements: [], warnings: [] };

    const result = await stub.handler!({
      name: "align-glossary",
      dispatch_mode: "validate-only",
      mode: "validate-pair",
      input,
      output,
    });
    const payload = extractPayload(result) as Record<string, unknown>;
    assert.equal(payload["seam"], "align-glossary");
    assert.equal(payload["dispatch_mode"], "validate-only");
    assert.deepEqual(payload["output"], output);
  });

  test("validate-only rejects unknown seam with invalid_input", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    await assert.rejects(
      async () => {
        const result = await stub.handler!({
          name: "no-such-seam",
          dispatch_mode: "validate-only",
          mode: "validate-pair",
          input: {},
          output: {},
        });
        extractPayload(result);
      },
    );
  });
});

// ---------------------------------------------------------------------------
// subagent dispatch — stubbed handler
// ---------------------------------------------------------------------------

describe("seams_run — subagent dispatch path", () => {
  afterEach(() => setSubagentDispatchHandler(undefined));

  test("dispatch_mode=subagent calls handler + validates output", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    const dispatchOutput = {
      aligned_record: { title: "DEC-A01", status: "active", body: "Updated body." },
      change_kind: "amend",
      rationale: "Minor body update.",
    };

    const mockHandler: SubagentDispatchHandler = async (_name, _dir, _input) => ({
      output: dispatchOutput,
      token_totals: { input_tokens: 100, output_tokens: 50 },
    });
    setSubagentDispatchHandler(mockHandler);

    const prevEnv = process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
    process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"] = "1";
    try {
      const result = await stub.handler!({
        name: "align-arch-decision",
        dispatch_mode: "subagent",
        input: {
          decision_id: "DEC-A01",
          current_record: { title: "DEC-A01", status: "active", body: "Original body." },
          proposed_change: "Add missing context.",
        },
      });
      const payload = extractPayload(result) as Record<string, unknown>;
      assert.equal(payload["seam"], "align-arch-decision");
      assert.equal(payload["dispatch_mode"], "subagent");
      assert.equal(payload["validated"], true);
      assert.deepEqual(payload["output"], dispatchOutput);
      const totals = payload["token_totals"] as Record<string, unknown>;
      assert.equal(totals["input_tokens"], 100);
    } finally {
      if (prevEnv === undefined) delete process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
      else process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"] = prevEnv;
    }
  });

  test("dispatch returns dispatch_unavailable when env var absent", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    const prevEnv = process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
    delete process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
    try {
      const result = await stub.handler!({
        name: "align-arch-decision",
        dispatch_mode: "subagent",
        input: {
          decision_id: "DEC-A01",
          current_record: { title: "T", status: "active", body: "B" },
          proposed_change: "change",
        },
      });
      const payload = extractPayload(result) as Record<string, unknown>;
      assert.equal(payload["dispatch_unavailable"], true);
    } finally {
      if (prevEnv !== undefined) process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"] = prevEnv;
    }
  });

  test("dispatch rejects malformed output with invalid_input (schema_out)", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    const badHandler: SubagentDispatchHandler = async () => ({
      output: { wrong_field: "bad" }, // missing required fields
    });
    setSubagentDispatchHandler(badHandler);

    const prevEnv = process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
    process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"] = "1";
    try {
      await assert.rejects(async () => {
        const result = await stub.handler!({
          name: "align-arch-decision",
          dispatch_mode: "subagent",
          input: {
            decision_id: "DEC-A01",
            current_record: { title: "T", status: "active", body: "B" },
            proposed_change: "change",
          },
        });
        extractPayload(result);
      });
    } finally {
      if (prevEnv === undefined) delete process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"];
      else process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"] = prevEnv;
    }
  });

  test("align-arch-decision golden validates against schemas", async () => {
    const stub = makeServerStub();
    registerSeamsRun(stub as never);

    const { default: goldenRaw } = await import(
      path.join(REPO_ROOT, "tools/seams/align-arch-decision/golden/example-amend.json"),
      { with: { type: "json" } }
    );
    const golden = goldenRaw as { input: unknown; output: unknown };

    const result = await stub.handler!({
      name: "align-arch-decision",
      dispatch_mode: "validate-only",
      mode: "validate-pair",
      input: golden.input,
      output: golden.output,
    });
    const payload = extractPayload(result) as Record<string, unknown>;
    assert.equal(payload["seam"], "align-arch-decision");
  });
});
