// TECH-2093 / Stage 10.1 — semantic-cycle-check DFS contract.

import { describe, expect, test } from "vitest";

import { semanticCycleCheck } from "../semantic-cycle-check";

describe("semanticCycleCheck (TECH-2093)", () => {
  test("self-edge: src→src is a cycle", async () => {
    const fetcher = async (): Promise<number | null> => null;
    const result = await semanticCycleCheck(1, 1, fetcher);
    expect(result.cycle).toBe(true);
  });

  test("no cycle: A→B→null", async () => {
    const chain: Record<number, number | null> = { 2: null };
    const fetcher = async (id: number): Promise<number | null> => chain[id] ?? null;
    const result = await semanticCycleCheck(1, 2, fetcher);
    expect(result.cycle).toBe(false);
    expect(result.path).toEqual([1, 2]);
  });

  test("no cycle: A→B→C→null", async () => {
    const chain: Record<number, number | null> = { 2: 3, 3: null };
    const fetcher = async (id: number): Promise<number | null> => chain[id] ?? null;
    const result = await semanticCycleCheck(1, 2, fetcher);
    expect(result.cycle).toBe(false);
    expect(result.path).toEqual([1, 2, 3]);
  });

  test("cycle: A→B→C→A", async () => {
    const chain: Record<number, number | null> = { 2: 3, 3: 1 };
    const fetcher = async (id: number): Promise<number | null> => chain[id] ?? null;
    const result = await semanticCycleCheck(1, 2, fetcher);
    expect(result.cycle).toBe(true);
    expect(result.path).toEqual([1, 2, 3, 1]);
  });

  test("cycle: A→B→B (target self-loop visible after one hop)", async () => {
    const chain: Record<number, number | null> = { 2: 2 };
    const fetcher = async (id: number): Promise<number | null> => chain[id] ?? null;
    const result = await semanticCycleCheck(1, 2, fetcher);
    expect(result.cycle).toBe(true);
    expect(result.path).toEqual([1, 2, 2]);
  });
});
