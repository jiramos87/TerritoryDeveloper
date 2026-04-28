import { describe, it, expect } from "vitest";

import { canonicalStringify } from "@/lib/json/canonical";

describe("canonicalStringify", () => {
  it("serializes primitives", () => {
    expect(canonicalStringify(null)).toBe("null");
    expect(canonicalStringify(true)).toBe("true");
    expect(canonicalStringify(false)).toBe("false");
    expect(canonicalStringify(0)).toBe("0");
    expect(canonicalStringify(1.5)).toBe("1.5");
    expect(canonicalStringify("hello")).toBe('"hello"');
  });

  it("sorts object keys lexicographically", () => {
    expect(canonicalStringify({ b: 1, a: 2 })).toBe('{"a":2,"b":1}');
    expect(canonicalStringify({ z: 1, a: 1, m: 1 })).toBe(
      '{"a":1,"m":1,"z":1}',
    );
  });

  it("preserves array insertion order", () => {
    expect(canonicalStringify([3, 1, 2])).toBe("[3,1,2]");
  });

  it("recurses through nested objects + arrays", () => {
    const value = {
      outer: {
        z: 1,
        a: [{ y: 2, x: 1 }, null, true],
      },
      first: "ok",
    };
    expect(canonicalStringify(value)).toBe(
      '{"first":"ok","outer":{"a":[{"x":1,"y":2},null,true],"z":1}}',
    );
  });

  it("produces stable output across runs over equivalent inputs", () => {
    const a = { foo: 1, bar: { baz: [1, 2, { z: 0, a: 0 }] } };
    const b = { bar: { baz: [1, 2, { a: 0, z: 0 }] }, foo: 1 };
    expect(canonicalStringify(a)).toBe(canonicalStringify(b));
  });

  it("rejects non-finite numbers", () => {
    expect(() => canonicalStringify(Number.NaN)).toThrow(RangeError);
    expect(() => canonicalStringify(Number.POSITIVE_INFINITY)).toThrow(
      RangeError,
    );
    expect(() => canonicalStringify(Number.NEGATIVE_INFINITY)).toThrow(
      RangeError,
    );
  });

  it("drops undefined + function values inside objects", () => {
    const value = { keep: 1, dropU: undefined, dropF: () => 0 } as unknown;
    expect(canonicalStringify(value)).toBe('{"keep":1}');
  });

  it("normalizes undefined inside arrays to null", () => {
    const value = [1, undefined, 2] as unknown[];
    expect(canonicalStringify(value)).toBe("[1,null,2]");
  });
});
