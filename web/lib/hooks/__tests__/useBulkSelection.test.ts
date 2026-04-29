/**
 * useBulkSelection — toggle / selectAll / clear semantics (TECH-4182 §Test Blueprint).
 * Exercises the hook logic directly without React renderer.
 */
import { describe, expect, it } from "vitest";

// Pure logic extracted from useBulkSelection for unit testing.
function makeState() {
  let selected = new Set<string>();
  return {
    toggle(id: string) {
      const next = new Set(selected);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      selected = next;
    },
    toggleAll(ids: string[]) {
      const allSelected = ids.every((id) => selected.has(id));
      if (allSelected) {
        const next = new Set(selected);
        ids.forEach((id) => next.delete(id));
        selected = next;
      } else {
        const next = new Set(selected);
        ids.forEach((id) => next.add(id));
        selected = next;
      }
    },
    clear() { selected = new Set(); },
    get selected() { return selected; },
    get count() { return selected.size; },
  };
}

describe("useBulkSelection logic", () => {
  it("toggle adds an id", () => {
    const s = makeState();
    s.toggle("1");
    expect(s.selected.has("1")).toBe(true);
    expect(s.count).toBe(1);
  });

  it("toggle removes an already-selected id", () => {
    const s = makeState();
    s.toggle("1");
    s.toggle("1");
    expect(s.selected.has("1")).toBe(false);
    expect(s.count).toBe(0);
  });

  it("toggleAll adds all ids when none selected", () => {
    const s = makeState();
    s.toggleAll(["a", "b", "c"]);
    expect(s.count).toBe(3);
    expect(s.selected.has("b")).toBe(true);
  });

  it("toggleAll removes all ids when all selected", () => {
    const s = makeState();
    s.toggleAll(["a", "b"]);
    s.toggleAll(["a", "b"]);
    expect(s.count).toBe(0);
  });

  it("toggleAll adds missing ids when partially selected", () => {
    const s = makeState();
    s.toggle("a");
    s.toggleAll(["a", "b", "c"]);
    expect(s.count).toBe(3);
  });

  it("clear empties selection", () => {
    const s = makeState();
    s.toggle("1");
    s.toggle("2");
    s.clear();
    expect(s.count).toBe(0);
  });

  it("count matches selection size across transitions", () => {
    const s = makeState();
    s.toggleAll(["x", "y", "z"]);
    expect(s.count).toBe(3);
    s.toggle("x");
    expect(s.count).toBe(2);
    s.clear();
    expect(s.count).toBe(0);
  });
});
