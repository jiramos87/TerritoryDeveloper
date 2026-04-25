import test from "node:test";
import assert from "node:assert/strict";
import { type SkillFrontmatter } from "../frontmatter.js";
import { renderAgent } from "../render-agent.js";
import { renderCommand } from "../render-command.js";
import { renderCursor } from "../render-cursor.js";

const sample: SkillFrontmatter = {
  name: "demo",
  purpose: "Demo purpose for testing the renderer end to end",
  audience: "agent",
  loaded_by: "skill:demo",
  slices_via: "none",
  description: "Demo description long enough to satisfy the 40-character minimum requirement",
  phases: ["Phase one", "Phase two"],
  triggers: ["/demo"],
  tools_role: "standalone-pipeline",
  tools_extra: ["mcp__territory-ia__backlog_issue"],
  caveman_exceptions: ["code", "commits"],
  hard_boundaries: ["no commits"],
};

test("renderCommand emits expected header", () => {
  const out = renderCommand(sample);
  assert.match(out, /^---\ndescription: Demo description/);
  assert.match(out, /argument-hint: ""/);
  assert.match(out, /# \/demo —/);
  assert.match(out, /## Triggers/);
  assert.match(out, /^- \/demo$/m);
});

test("renderAgent emits tools list with role baseline + extras", () => {
  const out = renderAgent(sample);
  assert.match(out, /^---\nname: demo/);
  assert.match(out, /tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__backlog_issue/);
  assert.match(out, /model: sonnet/);
  assert.match(out, /@ia\/skills\/_preamble\/stable-block\.md/);
  assert.match(out, /@\.claude\/agents\/_preamble\/agent-boot\.md/);
  assert.match(out, /1\. Phase one/);
  assert.match(out, /2\. Phase two/);
});

test("renderAgent throws on empty tools list (custom + no extras)", () => {
  assert.throws(() =>
    renderAgent({
      ...sample,
      tools_role: "custom",
      tools_extra: [],
    })
  );
});

test("renderCursor emits caller_agent line when set", () => {
  const out = renderCursor({ ...sample, caller_agent: "demo" });
  assert.match(out, /caller_agent: "demo"/);
  assert.match(out, /Read and follow: @ia\/skills\/demo\/SKILL\.md/);
});

test("renderCursor falls back to generic line when no caller_agent", () => {
  const out = renderCursor(sample);
  assert.match(out, /follow its Tool recipe order exactly/);
});
