import assert from "node:assert";
import { describe, it } from "node:test";

import { stableJsonStringify } from "./stable-json-stringify.js";

describe("stableJsonStringify", () => {
  it("is byte-identical for reordered top-level keys", () => {
    const a = stableJsonStringify({ z: 1, m: { b: 2, a: 1 } });
    const b = stableJsonStringify({ m: { a: 1, b: 2 }, z: 1 });
    assert.strictEqual(a, b);
  });

  it("ends with newline", () => {
    assert.match(stableJsonStringify({}), /\n$/);
  });
});
