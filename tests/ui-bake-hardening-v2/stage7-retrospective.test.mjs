// Stage 7 — Retrospective + v3 repair extension trigger — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task (T7.0.1) creates file in red state.
//   T7.0.2 appends V3TriggerTask_PointsAtShipPlan; file flips fully GREEN at T7.0.2.
//
// Tasks anchored by §Red-Stage Proof per task spec:
//   T7.0.1  Retrospective_CoversAllSixLayers      (TECH-28381) — RED seed (this file created)
//   T7.0.2  V3TriggerTask_PointsAtShipPlan         (TECH-28382) ← GREEN flip

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { resolve, dirname } from "node:path";
import { load as yamlLoad } from "js-yaml";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "../..");

// ── T7.0.1  Retrospective_CoversAllSixLayers ────────────────────────────────

describe("Retrospective_CoversAllSixLayers", () => {
  const retroPath = resolve(
    REPO_ROOT,
    "docs/ui-bake-pipeline-hardening-v2-retrospective.md"
  );

  it("retrospective doc exists", () => {
    let content;
    try {
      content = readFileSync(retroPath, "utf8");
    } catch {
      assert.fail(`retrospective doc not found: ${retroPath}`);
    }
    assert.ok(content.length > 0, "retrospective doc is empty");
  });

  for (let n = 1; n <= 6; n++) {
    it(`Layer ${n} section present with Delivered: and Open: markers`, () => {
      const content = readFileSync(retroPath, "utf8");
      assert.match(
        content,
        new RegExp(`## Layer ${n}`),
        `Missing ## Layer ${n} heading`
      );
      // Each layer section must contain "Delivered:" and "Open:" sub-markers
      // We check in a window after the heading.
      const headingIdx = content.indexOf(`## Layer ${n}`);
      const nextHeadingIdx = content.indexOf("## Layer", headingIdx + 1);
      const section =
        nextHeadingIdx === -1
          ? content.slice(headingIdx)
          : content.slice(headingIdx, nextHeadingIdx);
      assert.match(section, /\*\*Delivered:\*\*/, `Layer ${n}: missing Delivered:`);
      assert.match(section, /\*\*Open:\*\*/, `Layer ${n}: missing Open:`);
    });
  }
});

// ── T7.0.2  V3TriggerTask_PointsAtShipPlan ──────────────────────────────────

describe("V3TriggerTask_PointsAtShipPlan", () => {
  const v3DocPath = resolve(
    REPO_ROOT,
    "docs/explorations/cityscene-mainmenu-panel-rollout-v3-repair.md"
  );

  it("v3 repair exploration doc exists", () => {
    let content;
    try {
      content = readFileSync(v3DocPath, "utf8");
    } catch {
      assert.fail(`v3 repair doc not found: ${v3DocPath}`);
    }
    assert.ok(content.length > 0, "v3 repair doc is empty");
  });

  it("frontmatter slug = cityscene-mainmenu-panel-rollout-v3-repair", () => {
    const content = readFileSync(v3DocPath, "utf8");
    const fmMatch = content.match(/^---\n([\s\S]*?)\n---/);
    assert.ok(fmMatch, "no YAML frontmatter found in v3 doc");
    const fm = yamlLoad(fmMatch[1]);
    assert.strictEqual(
      fm.slug,
      "cityscene-mainmenu-panel-rollout-v3-repair",
      `slug mismatch: got ${fm.slug}`
    );
  });

  it("frontmatter declares exactly 4 stages (10.0–13.0)", () => {
    const content = readFileSync(v3DocPath, "utf8");
    const fmMatch = content.match(/^---\n([\s\S]*?)\n---/);
    assert.ok(fmMatch, "no YAML frontmatter found in v3 doc");
    const fm = yamlLoad(fmMatch[1]);
    assert.ok(Array.isArray(fm.stages), "stages must be an array");
    assert.strictEqual(fm.stages.length, 4, `expected 4 stages, got ${fm.stages.length}`);
    const ids = fm.stages.map((s) => s.id ?? s.stage_id ?? s.id);
    assert.ok(
      ids.includes("10.0") || ids.some((id) => String(id).startsWith("10")),
      "missing stage 10.0"
    );
    assert.ok(
      ids.includes("13.0") || ids.some((id) => String(id).startsWith("13")),
      "missing stage 13.0"
    );
  });

  it("each stage declares red_stage_proof_block", () => {
    const content = readFileSync(v3DocPath, "utf8");
    const fmMatch = content.match(/^---\n([\s\S]*?)\n---/);
    assert.ok(fmMatch, "no YAML frontmatter found in v3 doc");
    const fm = yamlLoad(fmMatch[1]);
    assert.ok(Array.isArray(fm.stages), "stages must be an array");
    for (const stage of fm.stages) {
      assert.ok(
        stage.red_stage_proof_block,
        `stage ${stage.id ?? stage.stage_id} missing red_stage_proof_block`
      );
    }
  });
});
