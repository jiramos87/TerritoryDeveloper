// Stage 1 — Author-time DB gates — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task (T1.0.1) creates file in red state.
//   Each subsequent task appends its own assertions. File flips green at T1.0.5.
//
// Tasks anchored by §Red-Stage Proof per task spec:
//   T1.0.1  publish_FailsOnArchetypeNoRenderer      (TECH-28356) — RED seed
//   T1.0.2  publish_FailsOnActionIdSinkCollision     (TECH-28357)
//   T1.0.3  publish_FailsOnUnknownBindId             (TECH-28358)
//           publish_AutoRegistersBindOnDeclareFlag
//   T1.0.4  publish_FailsOnDanglingToken             (TECH-28359)
//   T1.0.5  publish_FailsOnUnanchoredView            (TECH-28360)  ← GREEN flip

import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import {
  validateArchetypeKindCoverage,
  validateActionIdSinkUniqueness,
  validateBindIdContract,
  validateTokenReferences,
  validateViewSlotAnchors,
  registerActionIdSinks,
  registerDeclaredBindIds,
} from "../../tools/mcp-ia-server/dist/ia-db/mutations/catalog-panel.js";

// ── Minimal in-memory mock tx ─────────────────────────────────────────────────
//
// Tests below run without a live Postgres instance. We mock the tx (PoolClient)
// to control query responses. Each describe block stubs only what it needs.

function makeMockTx(queryFn) {
  return { query: queryFn };
}

// ── T1.0.1: archetype×kind renderer coverage (TECH-28356) ───────────────────

describe("publish_FailsOnArchetypeNoRenderer", () => {
  it("T1.0.1 — returns archetype_no_renderer for unknown child.kind", () => {
    const row = { slug: "test-panel", children: [{ kind: "unknown-kind" }] };
    const result = validateArchetypeKindCoverage(row);
    assert.equal(result.ok, false, "gate must fail");
    assert.ok(
      result.errors.some((e) => e.code === "archetype_no_renderer"),
      `expected archetype_no_renderer, got: ${JSON.stringify(result.errors)}`,
    );
  });

  it("T1.0.1 — passes for known child.kind 'button'", () => {
    const row = { slug: "test-panel", children: [{ kind: "button" }] };
    const result = validateArchetypeKindCoverage(row);
    assert.equal(result.ok, true, "gate must pass for known kind");
    assert.deepEqual(result.errors, []);
  });

  it("T1.0.1 — passes for empty children", () => {
    const row = { slug: "test-panel", children: [] };
    const result = validateArchetypeKindCoverage(row);
    assert.equal(result.ok, true);
  });
});

// ── T1.0.2: action-id sink uniqueness (TECH-28357) ────────────────────────────

describe("publish_FailsOnActionIdSinkCollision", () => {
  it("T1.0.2 — returns action_id_sink_collision when action_id owned by different panel", async () => {
    // Mock tx: first call returns existing owner
    const tx = makeMockTx(async (_sql, params) => {
      if (params[0] === "duplicate.id") {
        return { rows: [{ owner_panel_slug: "other-panel" }] };
      }
      return { rows: [] };
    });

    const result = await validateActionIdSinkUniqueness(
      { slug: "p3", children: [{ kind: "button", action_id: "duplicate.id" }] },
      tx,
    );
    assert.equal(result.ok, false, "gate must fail");
    assert.ok(
      result.errors.some((e) => e.code === "action_id_sink_collision"),
      `expected action_id_sink_collision, got: ${JSON.stringify(result.errors)}`,
    );
  });

  it("T1.0.2 — passes when action_id owned by same panel (idempotent republish)", async () => {
    const tx = makeMockTx(async () => ({ rows: [{ owner_panel_slug: "my-panel" }] }));
    const result = await validateActionIdSinkUniqueness(
      { slug: "my-panel", children: [{ kind: "button", action_id: "my.action" }] },
      tx,
    );
    assert.equal(result.ok, true);
  });

  it("T1.0.2 — passes when action_id not yet registered (fresh)", async () => {
    const tx = makeMockTx(async () => ({ rows: [] }));
    const result = await validateActionIdSinkUniqueness(
      { slug: "p2", children: [{ kind: "button", action_id: "fresh.action" }] },
      tx,
    );
    assert.equal(result.ok, true);
  });

  it("T1.0.2 — skips gracefully when ia_ui_action_sinks table absent (42P01)", async () => {
    const tx = makeMockTx(async () => {
      const e = new Error("relation does not exist");
      e.code = "42P01";
      throw e;
    });
    const result = await validateActionIdSinkUniqueness(
      { slug: "p2", children: [{ kind: "button", action_id: "any.action" }] },
      tx,
    );
    assert.equal(result.ok, true, "must be lenient when table absent");
  });
});

// ── T1.0.3: bind-id contract (TECH-28358) ────────────────────────────────────

describe("publish_FailsOnUnknownBindId", () => {
  it("T1.0.3 — returns unknown_bind_id when bind_id absent and no declare_on_publish", async () => {
    const tx = makeMockTx(async () => ({ rows: [] })); // bind not found
    const result = await validateBindIdContract(
      { slug: "panel-x", children: [{ kind: "label", bind_id: "nonexistent.bind" }] },
      tx,
    );
    assert.equal(result.ok, false);
    assert.ok(
      result.errors.some((e) => e.code === "unknown_bind_id"),
      `expected unknown_bind_id, got: ${JSON.stringify(result.errors)}`,
    );
  });

  it("T1.0.3 — passes when bind_id absent but declare_on_publish=true", async () => {
    const tx = makeMockTx(async () => ({ rows: [] })); // bind not found
    const result = await validateBindIdContract(
      {
        slug: "panel-x",
        children: [{ kind: "label", bind_id: "new.bind", declare_on_publish: true }],
      },
      tx,
    );
    assert.equal(result.ok, true, "declare_on_publish flag must bypass missing-bind error");
  });

  it("T1.0.3 — passes when bind_id exists in registry", async () => {
    const tx = makeMockTx(async () => ({ rows: [{ bind_id: "existing.bind" }] }));
    const result = await validateBindIdContract(
      { slug: "panel-x", children: [{ kind: "label", bind_id: "existing.bind" }] },
      tx,
    );
    assert.equal(result.ok, true);
  });
});

describe("publish_AutoRegistersBindOnDeclareFlag", () => {
  it("T1.0.3 — registerDeclaredBindIds inserts bind row when declare_on_publish=true", async () => {
    const inserted = [];
    const tx = makeMockTx(async (sql, params) => {
      if (sql.includes("INSERT INTO ia_ui_bind_registry")) {
        inserted.push(params[0]);
      }
      return { rows: [] };
    });
    await registerDeclaredBindIds(
      { slug: "panel-x", children: [{ kind: "label", bind_id: "new.bind", declare_on_publish: true }] },
      tx,
    );
    assert.ok(inserted.includes("new.bind"), "bind_id must be inserted into ia_ui_bind_registry");
  });
});

// ── T1.0.4: token reference graph (TECH-28359) ───────────────────────────────

describe("publish_FailsOnDanglingToken", () => {
  it("T1.0.4 — returns dangling_token_ref when token-* ref absent from both tables", async () => {
    const tx = makeMockTx(async () => ({ rows: [] })); // token not found
    const result = await validateTokenReferences(
      {
        slug: "panel-x",
        params_json: { color: "token-color-bg-primary-removed" },
        children: [],
      },
      tx,
    );
    assert.equal(result.ok, false);
    assert.ok(
      result.errors.some((e) => e.code === "dangling_token_ref"),
      `expected dangling_token_ref, got: ${JSON.stringify(result.errors)}`,
    );
  });

  it("T1.0.4 — passes when token ref resolves in catalog_entity (kind=token)", async () => {
    const tx = makeMockTx(async () => ({ rows: [{ token_id: "token-color-bg-primary" }] }));
    const result = await validateTokenReferences(
      {
        slug: "panel-x",
        params_json: { color: "token-color-bg-primary" },
        children: [],
      },
      tx,
    );
    assert.equal(result.ok, true);
  });

  it("T1.0.4 — passes when no token refs in params_json", async () => {
    const tx = makeMockTx(async () => ({ rows: [] }));
    const result = await validateTokenReferences(
      { slug: "panel-x", params_json: { size: 42 }, children: [] },
      tx,
    );
    assert.equal(result.ok, true);
  });
});

// ── T1.0.5: view-slot anchor required-by (TECH-28360) ────────────────────────

describe("publish_FailsOnUnanchoredView", () => {
  it("T1.0.5 — returns unanchored_view when view has no catalog_panel_anchors row", async () => {
    const tx = makeMockTx(async () => ({ rows: [] })); // anchor not found
    const result = await validateViewSlotAnchors(
      { slug: "settings-panel", views: ["audio", "video"] },
      tx,
    );
    assert.equal(result.ok, false);
    assert.ok(
      result.errors.some((e) => e.code === "unanchored_view"),
      `expected unanchored_view, got: ${JSON.stringify(result.errors)}`,
    );
  });

  it("T1.0.5 — passes when all views have anchor rows", async () => {
    const tx = makeMockTx(async () => ({ rows: [{ slot_name: "audio" }] }));
    const result = await validateViewSlotAnchors(
      { slug: "settings-panel", views: ["audio"] },
      tx,
    );
    assert.equal(result.ok, true);
  });

  it("T1.0.5 — passes when views[] is empty", async () => {
    const tx = makeMockTx(async () => ({ rows: [] }));
    const result = await validateViewSlotAnchors({ slug: "panel-x", views: [] }, tx);
    assert.equal(result.ok, true);
  });
});
