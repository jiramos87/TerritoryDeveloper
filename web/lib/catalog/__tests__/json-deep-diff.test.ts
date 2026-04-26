import { describe, it, expect } from "vitest";

import { deepDiff } from "@/lib/catalog/json-deep-diff";

describe("deepDiff", () => {
  it("returns empty when payloads are deeply equal", () => {
    expect(deepDiff({ a: 1, b: { c: [1, 2] } }, { a: 1, b: { c: [1, 2] } })).toEqual([]);
  });

  it("reports primitive change at a nested leaf path", () => {
    const diff = deepDiff({ a: { b: 1 } }, { a: { b: 2 } });
    expect(diff).toEqual([{ path: "a.b", status: "changed", base: 1, other: 2 }]);
  });

  it("reports array element added at the correct index", () => {
    const diff = deepDiff({ tags: ["x"] }, { tags: ["x", "y"] });
    expect(diff).toEqual([{ path: "tags[1]", status: "added", base: undefined, other: "y" }]);
  });

  it("reports array element removed at the correct index", () => {
    const diff = deepDiff({ tags: ["x", "y"] }, { tags: ["x"] });
    expect(diff).toEqual([{ path: "tags[1]", status: "removed", base: "y", other: undefined }]);
  });

  it("reports keys added and removed in an object", () => {
    const diff = deepDiff({ a: 1, b: 2 }, { a: 1, c: 3 });
    const byPath = Object.fromEntries(diff.map((d) => [d.path, d]));
    expect(byPath["b"].status).toBe("removed");
    expect(byPath["c"].status).toBe("added");
  });

  it("reports type mismatch as a single changed entry at the root path", () => {
    const diff = deepDiff({ a: 1 }, [1]);
    expect(diff).toEqual([{ path: "$", status: "changed", base: { a: 1 }, other: [1] }]);
  });

  it("handles deeply nested mixed shapes", () => {
    const diff = deepDiff(
      { items: [{ id: "a", v: 1 }, { id: "b", v: 2 }] },
      { items: [{ id: "a", v: 1 }, { id: "b", v: 3 }] },
    );
    expect(diff).toEqual([{ path: "items[1].v", status: "changed", base: 2, other: 3 }]);
  });
});
