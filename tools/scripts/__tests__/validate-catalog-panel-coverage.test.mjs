// TECH-19062 / game-ui-catalog-bake Stage 9.12
//
// Unit tests for validate-catalog-panel-coverage.mjs orphan-button detector.
//
// §Red-Stage Proof: OrphanButton_ExitsNonZero
//   Red:  no script exists → import fails → red.
//   Green: script exists + findOrphanButtons logic correct → green.
//
// Uses live DB (localhost:5434 postgres/postgres territory_ia_dev).
// Inserts + deletes test-only rows inside a cleanup block.

import assert from "node:assert/strict";
import { test } from "node:test";
import { findOrphanButtons, findDuplicateStubReferences } from "../validate-catalog-panel-coverage.mjs";
import { createRequire } from "node:module";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "../../..");

const pgRequire = createRequire(join(REPO_ROOT, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg");

const DATABASE_URL =
  process.env.DATABASE_URL ||
  "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

async function withClient(fn) {
  const client = new pg.Client({ connectionString: DATABASE_URL });
  await client.connect();
  try {
    return await fn(client);
  } finally {
    await client.end();
  }
}

// Insert a test catalog_entity + optional panel_child parenting.
// Returns { buttonId, panelId } (panelId null when orphan-only).
async function seedButton(client, slug, opts = {}) {
  const { parentSlug = null } = opts;

  // Button entity
  const btnRes = await client.query(
    `INSERT INTO catalog_entity (kind, slug, display_name)
     VALUES ('button', $1, $1)
     ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
     RETURNING id`,
    [slug]
  );
  const buttonId = btnRes.rows[0].id;

  let panelId = null;

  if (parentSlug) {
    // Panel entity (no panel_detail required for test; FK only on entity_id).
    const pnlRes = await client.query(
      `INSERT INTO catalog_entity (kind, slug, display_name)
       VALUES ('panel', $1, $1)
       ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
       RETURNING id`,
      [parentSlug]
    );
    panelId = pnlRes.rows[0].id;

    // panel_child referencing button via params_json->>'button_ref' (canonical)
    await client.query(
      `INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, child_entity_id, params_json)
       VALUES ($1, 'main', 99, 'button', $2, jsonb_build_object('button_ref', $3))
       ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING`,
      [panelId, buttonId, slug]
    );
  }

  return { buttonId, panelId };
}

async function cleanup(client, { buttonSlug, panelSlug }) {
  if (panelSlug) {
    await client.query(
      `DELETE FROM panel_child USING catalog_entity ce
       WHERE panel_child.panel_entity_id = ce.id AND ce.slug = $1`,
      [panelSlug]
    );
    await client.query(
      `DELETE FROM catalog_entity WHERE kind = 'panel' AND slug = $1`,
      [panelSlug]
    );
  }
  await client.query(
    `DELETE FROM catalog_entity WHERE kind = 'button' AND slug = $1`,
    [buttonSlug]
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test("OrphanButton_ExitsNonZero — orphan button slug appears in result", async () => {
  const orphanSlug = `test-orphan-button-${Date.now()}`;

  await withClient(async (client) => {
    try {
      await seedButton(client, orphanSlug); // no parent

      const orphans = await findOrphanButtons(DATABASE_URL);

      assert.ok(
        orphans.includes(orphanSlug),
        `Expected orphan slug '${orphanSlug}' in result, got: [${orphans.join(", ")}]`
      );
    } finally {
      await cleanup(client, { buttonSlug: orphanSlug });
    }
  });
});

test("ParentedButton_ExitsZero — parented button not in orphan list", async () => {
  const buttonSlug = `test-parented-button-${Date.now()}`;
  const panelSlug = `test-host-panel-${Date.now()}`;

  await withClient(async (client) => {
    try {
      await seedButton(client, buttonSlug, { parentSlug: panelSlug });

      const orphans = await findOrphanButtons(DATABASE_URL);

      assert.ok(
        !orphans.includes(buttonSlug),
        `Parented button '${buttonSlug}' must NOT appear in orphan list, got: [${orphans.join(", ")}]`
      );
    } finally {
      await cleanup(client, { buttonSlug, panelSlug });
    }
  });
});

// ---------------------------------------------------------------------------
// Rule 2: duplicate-stub-reference detection (TECH-19976 / Stage 9.13)
// ---------------------------------------------------------------------------

test("DuplicateStubReference_ExitsNonZero — panel with 3 children all same button_ref yields violation", async () => {
  const panelSlug  = `test-dup-panel-${Date.now()}`;
  const buttonSlug = `test-dup-button-${Date.now()}`;

  await withClient(async (client) => {
    try {
      // Insert panel entity
      const pnlRes = await client.query(
        `INSERT INTO catalog_entity (kind, slug, display_name)
         VALUES ('panel', $1, $1)
         ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
         RETURNING id`,
        [panelSlug]
      );
      const panelId = pnlRes.rows[0].id;

      // Insert button entity
      const btnRes = await client.query(
        `INSERT INTO catalog_entity (kind, slug, display_name)
         VALUES ('button', $1, $1)
         ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
         RETURNING id`,
        [buttonSlug]
      );
      const buttonId = btnRes.rows[0].id;

      // Insert 3 panel_child rows all referencing the same button_ref (duplicate-stub scenario)
      for (let i = 1; i <= 3; i++) {
        await client.query(
          `INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, child_entity_id, params_json)
           VALUES ($1, 'main', $2, 'button', $3, jsonb_build_object('kind', 'button', 'button_ref', $4))
           ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING`,
          [panelId, i, buttonId, buttonSlug]
        );
      }

      const violations = await findDuplicateStubReferences(DATABASE_URL);

      const match = violations.find(
        (v) => v.panelSlug === panelSlug && v.refSlug === buttonSlug
      );
      assert.ok(
        match != null,
        `Expected violation for panel='${panelSlug}' ref='${buttonSlug}', got: ${JSON.stringify(violations)}`
      );
      assert.ok(
        match.count >= 3,
        `Expected count ≥3 for duplicate ref, got: ${match.count}`
      );
    } finally {
      await client.query(
        `DELETE FROM panel_child USING catalog_entity ce
         WHERE panel_child.panel_entity_id = ce.id AND ce.slug = $1`,
        [panelSlug]
      );
      await client.query(
        `DELETE FROM catalog_entity WHERE kind = 'panel' AND slug = $1`,
        [panelSlug]
      );
      await client.query(
        `DELETE FROM catalog_entity WHERE kind = 'button' AND slug = $1`,
        [buttonSlug]
      );
    }
  });
});

test("DistinctReferences_ExitsZero — panel with 3 children all distinct button_ref yields no violation", async () => {
  const panelSlug = `test-distinct-panel-${Date.now()}`;
  const buttonSlugs = [
    `test-distinct-btn-a-${Date.now()}`,
    `test-distinct-btn-b-${Date.now()}`,
    `test-distinct-btn-c-${Date.now()}`,
  ];

  await withClient(async (client) => {
    try {
      // Insert panel entity
      const pnlRes = await client.query(
        `INSERT INTO catalog_entity (kind, slug, display_name)
         VALUES ('panel', $1, $1)
         ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
         RETURNING id`,
        [panelSlug]
      );
      const panelId = pnlRes.rows[0].id;

      // Insert 3 distinct button entities + 3 distinct panel_child rows
      for (let i = 0; i < buttonSlugs.length; i++) {
        const slug = buttonSlugs[i];
        const btnRes = await client.query(
          `INSERT INTO catalog_entity (kind, slug, display_name)
           VALUES ('button', $1, $1)
           ON CONFLICT (kind, slug) DO UPDATE SET display_name = EXCLUDED.display_name
           RETURNING id`,
          [slug]
        );
        const buttonId = btnRes.rows[0].id;

        await client.query(
          `INSERT INTO panel_child (panel_entity_id, slot_name, order_idx, child_kind, child_entity_id, params_json)
           VALUES ($1, 'main', $2, 'button', $3, jsonb_build_object('kind', 'button', 'button_ref', $4))
           ON CONFLICT (panel_entity_id, slot_name, order_idx) DO NOTHING`,
          [panelId, i + 1, buttonId, slug]
        );
      }

      const violations = await findDuplicateStubReferences(DATABASE_URL);

      const match = violations.find((v) => v.panelSlug === panelSlug);
      assert.ok(
        match == null,
        `Expected no violation for panel='${panelSlug}' with distinct refs, got: ${JSON.stringify(violations)}`
      );
    } finally {
      await client.query(
        `DELETE FROM panel_child USING catalog_entity ce
         WHERE panel_child.panel_entity_id = ce.id AND ce.slug = $1`,
        [panelSlug]
      );
      await client.query(
        `DELETE FROM catalog_entity WHERE kind = 'panel' AND slug = $1`,
        [panelSlug]
      );
      for (const slug of buttonSlugs) {
        await client.query(
          `DELETE FROM catalog_entity WHERE kind = 'button' AND slug = $1`,
          [slug]
        );
      }
    }
  });
});

test("RetiredButton_Excluded — retired button not flagged as orphan", async () => {
  const retiredSlug = `test-retired-button-${Date.now()}`;

  await withClient(async (client) => {
    try {
      const res = await client.query(
        `INSERT INTO catalog_entity (kind, slug, display_name, retired_at)
         VALUES ('button', $1, $1, NOW())
         ON CONFLICT (kind, slug) DO UPDATE SET retired_at = NOW()
         RETURNING id`,
        [retiredSlug]
      );
      void res; // inserted

      const orphans = await findOrphanButtons(DATABASE_URL);

      assert.ok(
        !orphans.includes(retiredSlug),
        `Retired button '${retiredSlug}' must NOT appear in orphan list`
      );
    } finally {
      await client.query(
        `DELETE FROM catalog_entity WHERE kind = 'button' AND slug = $1`,
        [retiredSlug]
      );
    }
  });
});
