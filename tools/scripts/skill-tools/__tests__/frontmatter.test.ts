import test from "node:test";
import assert from "node:assert/strict";
import { parseRawFrontmatter, splitFrontmatter, validateFrontmatter, collapseDescription } from "../frontmatter.js";

test("splitFrontmatter extracts block + body", () => {
  const text = "---\nname: foo\ndescription: bar\n---\nbody here\n";
  const { fmBlock, body } = splitFrontmatter(text);
  assert.equal(fmBlock, "name: foo\ndescription: bar");
  assert.equal(body, "body here\n");
});

test("parseRawFrontmatter handles inline scalars + flow arrays", () => {
  const block = `name: ship
phases: [a, b, c]
tools_extra: []`;
  const raw = parseRawFrontmatter(block);
  assert.equal(raw.name, "ship");
  assert.deepEqual(raw.phases, ["a", "b", "c"]);
  assert.deepEqual(raw.tools_extra, []);
});

test("parseRawFrontmatter handles folded block scalar", () => {
  const block = `description: >-
  Single line one
  continuation here
phases:
  - "first phase"
  - second`;
  const raw = parseRawFrontmatter(block);
  assert.equal(raw.description, "Single line one continuation here");
  assert.deepEqual(raw.phases, ["first phase", "second"]);
});

test("validateFrontmatter passes minimal valid input", () => {
  const fm = validateFrontmatter({
    name: "foo-bar",
    description: "Long enough description string here please yes thanks",
    triggers: ["/foo"],
    tools_role: "standalone-pipeline",
    tools_extra: [],
  });
  assert.equal(fm.name, "foo-bar");
  assert.equal(fm.tools_role, "standalone-pipeline");
  assert.deepEqual(fm.tools_extra, []);
});

test("validateFrontmatter rejects bad name regex", () => {
  assert.throws(() =>
    validateFrontmatter({
      name: "Bad_Name",
      description: "Long enough description string here please yes thanks",
      tools_role: "custom",
    })
  );
});

test("collapseDescription strips multi-space + trims", () => {
  assert.equal(collapseDescription("  one  two\n  three  "), "one two three");
});
