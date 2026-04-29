import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { canonHeadingText, sliceSection } from "../../src/ia-db/queries.js";

describe("sliceSection", () => {
  it("returns null on empty section", () => {
    assert.equal(sliceSection("# a\nb\n", ""), null);
    assert.equal(sliceSection("# a\nb\n", "   "), null);
  });

  it("returns null when heading not found", () => {
    assert.equal(sliceSection("# a\nb\n", "missing"), null);
  });

  it("slices from heading to EOF when last section", () => {
    const body = "# Intro\nintro text\n## Details\nd1\nd2\n";
    const r = sliceSection(body, "Details");
    assert.ok(r);
    assert.equal(r!.heading, "Details");
    assert.equal(r!.level, 2);
    assert.equal(r!.content, "## Details\nd1\nd2\n");
  });

  it("stops at next same-or-shallower heading", () => {
    const body = [
      "# Top",
      "top line",
      "## A",
      "a1",
      "### A.1",
      "a1-child",
      "## B",
      "b1",
    ].join("\n");
    const r = sliceSection(body, "A");
    assert.ok(r);
    assert.equal(r!.heading, "A");
    assert.equal(r!.level, 2);
    assert.equal(r!.content, "## A\na1\n### A.1\na1-child");
  });

  it("includes deeper subheadings under target", () => {
    const body = [
      "## Plan",
      "p",
      "### Sub 1",
      "s1",
      "### Sub 2",
      "s2",
      "## Next",
      "n",
    ].join("\n");
    const r = sliceSection(body, "Plan");
    assert.ok(r);
    assert.equal(
      r!.content,
      "## Plan\np\n### Sub 1\ns1\n### Sub 2\ns2",
    );
  });

  it("is case-insensitive on heading text", () => {
    const body = "# Goal\nline\n";
    const r = sliceSection(body, "goal");
    assert.ok(r);
    assert.equal(r!.heading, "Goal");
  });

  it("handles CRLF line endings", () => {
    const body = "# A\r\nx\r\n# B\r\ny\r\n";
    const r = sliceSection(body, "A");
    assert.ok(r);
    assert.equal(r!.level, 1);
    assert.equal(r!.content, "# A\nx");
  });

  it("matches §-prefixed heading when needle drops § (and vice versa)", () => {
    const body = "# Top\n\n## §Plan Digest\n\nbody\n";
    const a = sliceSection(body, "Plan Digest");
    assert.ok(a);
    assert.equal(a!.level, 2);
    assert.equal(a!.heading, "§Plan Digest");
    const bareBody = "# Top\n\n## Plan Digest\n\nbody\n";
    const b = sliceSection(bareBody, "§Plan Digest");
    assert.ok(b);
    assert.equal(b!.level, 2);
    assert.equal(b!.heading, "Plan Digest");
  });
});

describe("canonHeadingText", () => {
  it("strips leading § and lowercases", () => {
    assert.equal(canonHeadingText("§Plan Digest"), "plan digest");
    assert.equal(canonHeadingText("§ Plan Digest"), "plan digest");
    assert.equal(canonHeadingText("Plan Digest"), "plan digest");
    assert.equal(canonHeadingText("  §Goal  "), "goal");
  });
});
