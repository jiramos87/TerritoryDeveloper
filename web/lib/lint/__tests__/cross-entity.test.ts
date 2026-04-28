/**
 * Layer 2 cross-entity lint runner unit tests (TECH-2569 / Stage 12.1).
 *
 * Pure unit coverage with a stubbed `Sql` template fn — no DB required.
 * Asserts:
 *   - panel resolved children → []
 *   - panel with retired/missing child → block row
 *   - button with retired icon sprite → block row
 *   - sprite with no inbound refs → warn row
 *   - archetype + pool stubs → []
 *   - aggregator groups by severity
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2569 §Test Blueprint
 */

import { describe, expect, it } from "vitest";

import type { Sql } from "postgres";

import {
  aggregateLintResults,
  runLayer2,
} from "@/lib/lint/cross-entity";
import type { LintResult } from "@/lib/lint/types";

type StubResponse = {
  panelChildRows?: Array<{
    id: number;
    slot_name: string;
    order_idx: number;
    child_kind: string;
    child_entity_id: number | null;
  }>;
  buttonDetailRow?: Record<string, number | null> | null;
  resolvedIds?: Set<number>;
  panelInboundCount?: number;
  buttonInboundCount?: number;
};

function buildSqlStub(opts: StubResponse): Sql {
  const fn = (strings: TemplateStringsArray, ...values: unknown[]) => {
    const text = strings.join("?");
    if (text.includes("from panel_child")) {
      // Disambiguate inbound-orphan-scan vs panel ref scan: inbound query has
      // `where child_entity_id = ?` — outbound has `panel_entity_id = ?`.
      if (text.includes("panel_entity_id")) {
        return Promise.resolve(opts.panelChildRows ?? []);
      }
      // orphan inbound check
      const count = opts.panelInboundCount ?? 0;
      return Promise.resolve(
        count > 0 ? Array.from({ length: count }, (_, i) => ({ id: i })) : [],
      );
    }
    if (text.includes("from button_detail")) {
      if (text.includes("entity_id =") && !text.includes("or sprite_idle")) {
        return Promise.resolve(
          opts.buttonDetailRow === null || opts.buttonDetailRow === undefined
            ? []
            : [opts.buttonDetailRow],
        );
      }
      // orphan inbound scan
      const count = opts.buttonInboundCount ?? 0;
      return Promise.resolve(
        count > 0
          ? Array.from({ length: count }, (_, i) => ({ entity_id: i }))
          : [],
      );
    }
    if (text.includes("from catalog_entity")) {
      const id = values[0];
      const idNum = typeof id === "string" ? Number(id) : (id as number);
      const set = opts.resolvedIds ?? new Set<number>();
      return Promise.resolve(
        set.has(idNum) ? [{ id: idNum }] : [],
      );
    }
    return Promise.resolve([]);
  };
  return fn as unknown as Sql;
}

describe("runLayer2 panel — TECH-2569 §Test Blueprint", () => {
  it("resolved children → no panel.unresolved_ref row", async () => {
    const sql = buildSqlStub({
      panelChildRows: [
        {
          id: 1,
          slot_name: "body",
          order_idx: 0,
          child_kind: "button",
          child_entity_id: 42,
        },
      ],
      resolvedIds: new Set([42]),
      panelInboundCount: 1, // panel has consumer — suppress orphan row
    });
    const out = await runLayer2("panel", "1", "1", sql);
    expect(out.find((r) => r.rule_id === "panel.unresolved_ref")).toBeUndefined();
  });

  it("missing child → 1 block row", async () => {
    const sql = buildSqlStub({
      panelChildRows: [
        {
          id: 1,
          slot_name: "body",
          order_idx: 0,
          child_kind: "button",
          child_entity_id: 99,
        },
      ],
      resolvedIds: new Set([]),
    });
    const out = await runLayer2("panel", "1", "1", sql);
    expect(out.length).toBeGreaterThanOrEqual(1);
    const blocked = out.find((r) => r.rule_id === "panel.unresolved_ref");
    expect(blocked).toBeDefined();
    expect(blocked?.severity).toBe("block");
  });

  it("NULL child_entity_id (spacer) skips ref check", async () => {
    const sql = buildSqlStub({
      panelChildRows: [
        {
          id: 1,
          slot_name: "body",
          order_idx: 0,
          child_kind: "spacer",
          child_entity_id: null,
        },
      ],
    });
    const out = await runLayer2("panel", "1", "1", sql);
    expect(out.find((r) => r.rule_id === "panel.unresolved_ref")).toBeUndefined();
  });
});

describe("runLayer2 button — TECH-2569 §Test Blueprint", () => {
  it("retired icon sprite → 1 block row", async () => {
    const sql = buildSqlStub({
      buttonDetailRow: {
        sprite_idle_entity_id: null,
        sprite_hover_entity_id: null,
        sprite_pressed_entity_id: null,
        sprite_disabled_entity_id: null,
        sprite_icon_entity_id: 77,
        sprite_badge_entity_id: null,
        token_palette_entity_id: null,
        token_frame_style_entity_id: null,
        token_font_entity_id: null,
        token_illumination_entity_id: null,
      },
      resolvedIds: new Set(),
      buttonInboundCount: 1, // not orphan
    });
    const out = await runLayer2("button", "1", "1", sql);
    const blocked = out.find((r) => r.rule_id === "button.unresolved_ref");
    expect(blocked).toBeDefined();
    expect(blocked?.severity).toBe("block");
    expect(blocked?.message).toContain("sprite_icon");
  });
});

describe("runLayer2 orphan — TECH-2569 §Test Blueprint", () => {
  it("sprite with no inbound refs → 1 warn row", async () => {
    const sql = buildSqlStub({
      panelInboundCount: 0,
      buttonInboundCount: 0,
    });
    const out = await runLayer2("sprite", "1", "1", sql);
    const orphan = out.find((r) => r.rule_id === "sprite.orphan_candidate");
    expect(orphan).toBeDefined();
    expect(orphan?.severity).toBe("warn");
  });

  it("sprite with inbound panel ref → no orphan row", async () => {
    const sql = buildSqlStub({
      panelInboundCount: 1,
    });
    const out = await runLayer2("sprite", "1", "1", sql);
    expect(out.find((r) => r.rule_id === "sprite.orphan_candidate")).toBeUndefined();
  });
});

describe("runLayer2 stubs", () => {
  it("archetype kind → []", async () => {
    const sql = buildSqlStub({});
    const out = await runLayer2("archetype", "1", "1", sql);
    expect(out).toEqual([]);
  });

  it("pool kind → []", async () => {
    const sql = buildSqlStub({});
    const out = await runLayer2("pool", "1", "1", sql);
    expect(out).toEqual([]);
  });
});

describe("aggregateLintResults — TECH-2569 §Acceptance row 6", () => {
  it("buckets by severity", () => {
    const block: LintResult = {
      rule_id: "x.block",
      severity: "block",
      message: "b",
    };
    const warn: LintResult = {
      rule_id: "x.warn",
      severity: "warn",
      message: "w",
    };
    const info: LintResult = {
      rule_id: "x.info",
      severity: "info",
      message: "i",
    };
    const out = aggregateLintResults([block, warn], [info]);
    expect(out.block.length).toBe(1);
    expect(out.warn.length).toBe(1);
    expect(out.info.length).toBe(1);
  });

  it("empty inputs → empty buckets", () => {
    const out = aggregateLintResults([], []);
    expect(out).toEqual({ block: [], warn: [], info: [] });
  });
});
