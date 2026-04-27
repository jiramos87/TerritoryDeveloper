// TECH-2094 / Stage 10.1 — one-hop semantic resolver coverage.

import { describe, expect, test } from "vitest";

import {
  resolveSemanticOneHop,
  type ResolvableTokenRow,
  type SemanticTokenFetcher,
} from "@/lib/tokens/resolve-semantic";

function makeFetcher(rows: ResolvableTokenRow[]): SemanticTokenFetcher {
  const map = new Map(rows.map((r) => [r.entity_id, r] as const));
  return async (id: number) => map.get(id) ?? null;
}

describe("resolveSemanticOneHop (TECH-2094)", () => {
  test("returns own value_json when source is non-semantic primitive", async () => {
    const source: ResolvableTokenRow = {
      entity_id: 1,
      token_kind: "color",
      value_json: { hex: "#ff0000" },
      semantic_target_entity_id: null,
    };
    const result = await resolveSemanticOneHop(source, makeFetcher([]));
    expect(result).toEqual({
      resolved: { hex: "#ff0000" },
      truncated: false,
      hops: 0,
    });
  });

  test("returns null resolved when semantic source has no target", async () => {
    const source: ResolvableTokenRow = {
      entity_id: 1,
      token_kind: "semantic",
      value_json: {},
      semantic_target_entity_id: null,
    };
    const result = await resolveSemanticOneHop(source, makeFetcher([]));
    expect(result.resolved).toBeNull();
    expect(result.truncated).toBe(false);
    expect(result.hops).toBe(0);
  });

  test("happy path: semantic -> primitive resolves in 1 hop, not truncated", async () => {
    const primitive: ResolvableTokenRow = {
      entity_id: 2,
      token_kind: "color",
      value_json: { hex: "#3366cc" },
      semantic_target_entity_id: null,
    };
    const source: ResolvableTokenRow = {
      entity_id: 1,
      token_kind: "semantic",
      value_json: {},
      semantic_target_entity_id: 2,
    };
    const result = await resolveSemanticOneHop(
      source,
      makeFetcher([primitive]),
    );
    expect(result.resolved).toEqual({ hex: "#3366cc" });
    expect(result.truncated).toBe(false);
    expect(result.hops).toBe(1);
  });

  test("truncated chain: semantic -> semantic returns first hop value with truncated=true", async () => {
    const intermediate: ResolvableTokenRow = {
      entity_id: 2,
      token_kind: "semantic",
      value_json: { fallback: "intermediate" },
      semantic_target_entity_id: 3,
    };
    const source: ResolvableTokenRow = {
      entity_id: 1,
      token_kind: "semantic",
      value_json: {},
      semantic_target_entity_id: 2,
    };
    const result = await resolveSemanticOneHop(
      source,
      makeFetcher([intermediate]),
    );
    expect(result.resolved).toEqual({ fallback: "intermediate" });
    expect(result.truncated).toBe(true);
    expect(result.hops).toBe(1);
  });

  test("returns null resolved when target id missing from fetcher", async () => {
    const source: ResolvableTokenRow = {
      entity_id: 1,
      token_kind: "semantic",
      value_json: {},
      semantic_target_entity_id: 99,
    };
    const result = await resolveSemanticOneHop(source, makeFetcher([]));
    expect(result.resolved).toBeNull();
    expect(result.truncated).toBe(false);
    expect(result.hops).toBe(1);
  });
});
