/**
 * bulk-actions — SQL shape + audit payload unit tests (TECH-4182 §Test Blueprint).
 */
import { describe, expect, it } from "vitest";
import { runBulkRetire, runBulkRestore, runBulkPublish } from "@/lib/catalog/bulk-actions";
import type { BulkResult } from "@/lib/catalog/bulk-actions";

type CallRecord = { sql: string; ids: string[] };

function makeTxSpy(returnRows: object[] = []): { tx: object; calls: CallRecord[] } {
  const calls: CallRecord[] = [];
  const tag = (strings: TemplateStringsArray, ...values: unknown[]) => {
    calls.push({ sql: strings.join("?"), ids: values.filter(Array.isArray).flat() as string[] });
    return Promise.resolve(returnRows);
  };
  return { tx: tag, calls };
}

describe("runBulkRetire", () => {
  it("returns empty result when no rows updated", async () => {
    const { tx } = makeTxSpy([]);
    const result: BulkResult = await runBulkRetire(tx as never, ["1", "2"]);
    expect(result.updated).toBe(0);
    expect(result.audit_payloads).toHaveLength(0);
  });

  it("audit action is catalog.entity.retired_bulk", async () => {
    const { tx } = makeTxSpy([{ id: "1", slug: "hero", kind: "sprite", retired_at: "2026-01-01" }]);
    const result = await runBulkRetire(tx as never, ["1"]);
    expect(result.audit_payloads[0]!.action).toBe("catalog.entity.retired_bulk");
    expect(result.audit_payloads[0]!.entity_id).toBe("1");
    expect(result.audit_payloads[0]!.meta).toMatchObject({ slug: "hero", kind: "sprite", bulk_size: 1 });
  });

  it("bulk_size reflects input length even when fewer rows updated", async () => {
    const { tx } = makeTxSpy([{ id: "2", slug: "bar", kind: "button", retired_at: "now" }]);
    const result = await runBulkRetire(tx as never, ["1", "2", "3"]);
    expect(result.audit_payloads[0]!.meta.bulk_size).toBe(3);
  });
});

describe("runBulkRestore", () => {
  it("audit action is catalog.entity.restored_bulk", async () => {
    const { tx } = makeTxSpy([{ id: "5", slug: "foo", kind: "audio" }]);
    const result = await runBulkRestore(tx as never, ["5"]);
    expect(result.audit_payloads[0]!.action).toBe("catalog.entity.restored_bulk");
  });

  it("returns zero updated when no matching retired rows", async () => {
    const { tx } = makeTxSpy([]);
    const result = await runBulkRestore(tx as never, ["99"]);
    expect(result.updated).toBe(0);
  });
});

describe("runBulkPublish", () => {
  it("returns zero and skips job insert when no active entities", async () => {
    const calls: string[] = [];
    const tx = (strings: TemplateStringsArray) => {
      calls.push(strings.join(""));
      return Promise.resolve([]);
    };
    const result = await runBulkPublish(tx as never, ["1"]);
    expect(result.updated).toBe(0);
    expect(result.audit_payloads).toHaveLength(0);
    expect(calls).toHaveLength(1);
  });

  it("audit action is catalog.entity.published_bulk", async () => {
    let call = 0;
    const tx = (strings: TemplateStringsArray) => {
      call++;
      if (call === 1) return Promise.resolve([{ id: "10", slug: "sfx", kind: "audio" }]);
      return Promise.resolve([]);
    };
    const result = await runBulkPublish(tx as never, ["10"]);
    expect(result.audit_payloads[0]!.action).toBe("catalog.entity.published_bulk");
    expect(result.audit_payloads[0]!.entity_id).toBe("10");
  });
});
