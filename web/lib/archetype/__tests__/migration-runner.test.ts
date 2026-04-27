import { describe, expect, it } from "vitest";

import { applyMigration } from "@/lib/archetype/migration-runner";

describe("applyMigration", () => {
  it("renames slug — value preserved, source removed", () => {
    const source = { color: "red", size: 3 };
    const out = applyMigration(source, { rename: [{ from: "color", to: "colour" }] });
    expect(out.params).toEqual({ colour: "red", size: 3 });
    expect(out.warnings).toEqual([]);
  });

  it("warns when rename source missing — treated as drop", () => {
    const source = { size: 3 };
    const out = applyMigration(source, { rename: [{ from: "color", to: "colour" }] });
    expect(out.params).toEqual({ size: 3 });
    expect(out.warnings).toHaveLength(1);
    expect(out.warnings[0].path).toBe("rename.color");
  });

  it("drops slug", () => {
    const source = { color: "red", legacy: 42 };
    const out = applyMigration(source, { drop: [{ slug: "legacy" }] });
    expect(out.params).toEqual({ color: "red" });
    expect(out.warnings).toEqual([]);
  });

  it("warns when drop slug not in source", () => {
    const source = { color: "red" };
    const out = applyMigration(source, { drop: [{ slug: "legacy" }] });
    expect(out.params).toEqual({ color: "red" });
    expect(out.warnings).toHaveLength(1);
    expect(out.warnings[0].path).toBe("drop.legacy");
  });

  it("adds default when slug absent", () => {
    const source = { color: "red" };
    const out = applyMigration(source, { default: [{ slug: "fresh", value: 7 }] });
    expect(out.params).toEqual({ color: "red", fresh: 7 });
    expect(out.warnings).toEqual([]);
  });

  it("warns when default would override existing slug", () => {
    const source = { color: "red", fresh: 99 };
    const out = applyMigration(source, { default: [{ slug: "fresh", value: 7 }] });
    expect(out.params).toEqual({ color: "red", fresh: 99 });
    expect(out.warnings).toHaveLength(1);
    expect(out.warnings[0].path).toBe("default.fresh");
  });

  it("composite hint — rename + drop + default in one pass", () => {
    const source = { color: "red", legacy: 42, size: 3 };
    const out = applyMigration(source, {
      rename: [{ from: "color", to: "colour" }],
      drop: [{ slug: "legacy" }],
      default: [{ slug: "fresh", value: 7 }],
    });
    expect(out.params).toEqual({ colour: "red", size: 3, fresh: 7 });
    expect(out.warnings).toEqual([]);
  });

  it("rename target collides with default — rename wins (default skipped, warns)", () => {
    const source = { color: "red" };
    const out = applyMigration(source, {
      rename: [{ from: "color", to: "colour" }],
      default: [{ slug: "colour", value: "blue" }],
    });
    expect(out.params).toEqual({ colour: "red" });
    expect(out.warnings).toHaveLength(1);
    expect(out.warnings[0].path).toBe("default.colour");
  });

  it("does not mutate source object", () => {
    const source = { color: "red" };
    const snapshot = { ...source };
    applyMigration(source, { rename: [{ from: "color", to: "colour" }] });
    expect(source).toEqual(snapshot);
  });

  it("empty hint — params unchanged", () => {
    const source = { color: "red", size: 3 };
    const out = applyMigration(source, {});
    expect(out.params).toEqual({ color: "red", size: 3 });
    expect(out.warnings).toEqual([]);
  });
});
