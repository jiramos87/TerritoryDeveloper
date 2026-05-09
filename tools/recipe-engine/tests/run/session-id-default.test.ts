/**
 * recipe-engine — session_id auto-inject + uuid run_id regression.
 *
 * Repro: tools/recipes/ship-final.yaml declares `inputs.session_id` optional
 * with description "Defaults to recipe-run auto-generated id." Wired-up was
 * the literal `${inputs.session_id}` substitution which yielded `undefined`
 * → mcp step stripped the key → cron_journal_append_enqueue threw
 * `invalid_input: session_id, phase, and payload_kind are required.`
 *
 * Fix locks two contracts:
 *   1. `run_id` generator is `crypto.randomUUID()` so the value satisfies the
 *      Postgres `::uuid` cast inside cron_journal_append_enqueue.
 *   2. When the recipe declares `inputs.session_id` optional and the caller
 *      omits it, the engine auto-injects `inputs.session_id = run_id` before
 *      template substitution / step dispatch.
 *
 * Test mode: dry_run — no MCP client needed. mcp step returns
 * { dry_run: true, tool, args } so we can assert the resolved args carried
 * the injected session_id.
 */

import test, { describe } from "node:test";
import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import os from "node:os";
import { runRecipe } from "../../src/run.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..", "..");

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function setupFakeRepo(recipeName: string, body: string): string {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "recipe-engine-"));
  fs.mkdirSync(path.join(tmp, "tools", "recipes"), { recursive: true });
  fs.writeFileSync(
    path.join(tmp, "tools", "recipes", `${recipeName}.yaml`),
    body,
    "utf8",
  );
  return tmp;
}

describe("session_id auto-inject + uuid run_id", () => {
  test("uuid_run_id — generator emits valid Postgres uuid", async () => {
    const cwd = setupFakeRepo(
      "uuid-fix",
      [
        "recipe: uuid-fix",
        "description: smoke",
        "inputs:",
        "  type: object",
        "  additionalProperties: false",
        "  properties: {}",
        "steps:",
        "  - id: noop",
        "    mcp: list_specs",
        "    args: {}",
        "outputs: {}",
        "",
      ].join("\n"),
    );
    try {
      const res = await runRecipe("uuid-fix", { cwd, dry_run: true });
      assert.equal(res.ok, true, `expected ok; got ${JSON.stringify(res.error)}`);
      assert.match(
        res.run_id,
        UUID_RE,
        `run_id must be uuid (Postgres ::uuid cast at cron_journal_append_enqueue); got ${res.run_id}`,
      );
    } finally {
      fs.rmSync(cwd, { recursive: true, force: true });
    }
  });

  test("session_id_default — recipe declares optional + caller omits → engine injects run_id", async () => {
    const cwd = setupFakeRepo(
      "sid-default",
      [
        "recipe: sid-default",
        "description: smoke",
        "inputs:",
        "  type: object",
        "  additionalProperties: false",
        "  required:",
        "    - slug",
        "  properties:",
        "    slug:",
        "      type: string",
        "    session_id:",
        "      type: string",
        "      description: Optional. Defaults to recipe-run auto-generated id.",
        "steps:",
        "  - id: enqueue",
        "    mcp: cron_journal_append_enqueue",
        "    args:",
        "      session_id: ${inputs.session_id}",
        "      slug: ${inputs.slug}",
        "outputs: {}",
        "",
      ].join("\n"),
    );
    try {
      const res = await runRecipe("sid-default", {
        cwd,
        dry_run: true,
        inputs: { slug: "fx-slug" },
      });
      assert.equal(res.ok, true, `expected ok; got ${JSON.stringify(res.error)}`);
      const enqueue = res.step_results.find((r) => r.step_id === "enqueue");
      assert.ok(enqueue, "enqueue step result missing");
      assert.equal(enqueue.result.ok, true);
      const value = enqueue.result.value as
        | { dry_run: true; tool: string; args: Record<string, unknown> }
        | undefined;
      assert.ok(value && value.dry_run, "expected dry_run mcp value");
      assert.equal(value.args.slug, "fx-slug");
      assert.equal(
        value.args.session_id,
        res.run_id,
        "session_id must equal run_id when caller omits it (contract from ship-final.yaml)",
      );
      assert.match(value.args.session_id as string, UUID_RE);
    } finally {
      fs.rmSync(cwd, { recursive: true, force: true });
    }
  });

  test("session_id_explicit — caller-provided session_id wins over auto-inject", async () => {
    const cwd = setupFakeRepo(
      "sid-explicit",
      [
        "recipe: sid-explicit",
        "description: smoke",
        "inputs:",
        "  type: object",
        "  additionalProperties: false",
        "  properties:",
        "    session_id:",
        "      type: string",
        "steps:",
        "  - id: enqueue",
        "    mcp: cron_journal_append_enqueue",
        "    args:",
        "      session_id: ${inputs.session_id}",
        "outputs: {}",
        "",
      ].join("\n"),
    );
    try {
      const explicit = "11111111-2222-3333-4444-555555555555";
      const res = await runRecipe("sid-explicit", {
        cwd,
        dry_run: true,
        inputs: { session_id: explicit },
      });
      assert.equal(res.ok, true, `expected ok; got ${JSON.stringify(res.error)}`);
      const enqueue = res.step_results.find((r) => r.step_id === "enqueue");
      const value = enqueue!.result.value as {
        dry_run: true;
        tool: string;
        args: Record<string, unknown>;
      };
      assert.equal(value.args.session_id, explicit, "explicit session_id must NOT be overwritten");
    } finally {
      fs.rmSync(cwd, { recursive: true, force: true });
    }
  });

  test("session_id_required — when recipe marks session_id required, missing → inputs_invalid (no auto-inject)", async () => {
    const cwd = setupFakeRepo(
      "sid-required",
      [
        "recipe: sid-required",
        "description: smoke",
        "inputs:",
        "  type: object",
        "  additionalProperties: false",
        "  required:",
        "    - session_id",
        "  properties:",
        "    session_id:",
        "      type: string",
        "steps:",
        "  - id: noop",
        "    mcp: list_specs",
        "    args: {}",
        "outputs: {}",
        "",
      ].join("\n"),
    );
    try {
      const res = await runRecipe("sid-required", {
        cwd,
        dry_run: true,
        inputs: {},
      });
      assert.equal(res.ok, false);
      assert.equal(res.error?.code, "inputs_invalid");
    } finally {
      fs.rmSync(cwd, { recursive: true, force: true });
    }
  });
});
