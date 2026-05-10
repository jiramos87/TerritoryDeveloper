// Stage 3 — Blueprint authoring (ship-plan UI-from-DB branch) — TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `npm run test:bake-pipeline-hardening`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   3.1 taskKindEnum_AcceptedByBacklogValidator
//   3.2 blueprintMarkdown_CarriesFiveStageSectionIds
//   3.3 shipPlan_BranchesOnTaskKindUiFromDb

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");

// Dynamically require compiled validator (tsx runtime resolves .ts via esm loader)
const require = createRequire(import.meta.url);

// Helper: load validateBacklogRecord via tsx dynamic import
async function loadValidator() {
  const mod = await import(
    path.join(REPO_ROOT, "tools/mcp-ia-server/src/parser/backlog-record-schema.ts")
  );
  return mod.validateBacklogRecord;
}

describe("Stage 3 — blueprint authoring (red until last task green)", () => {
  it("taskKindEnum_AcceptedByBacklogValidator [task 3.1]", async () => {
    const validateBacklogRecord = await loadValidator();

    // valid ui_from_db
    const validKinds = ["ui_from_db", "implementation", "refactor", "docs", "tooling"];
    for (const kind of validKinds) {
      const yaml = `id: TECH-99999\ntype: tech\ntitle: Test\nstatus: open\nsection: test\ntask_kind: ${kind}\n`;
      const result = validateBacklogRecord(yaml);
      assert.equal(result.errors.filter(e => e.includes("bad_task_kind")).length, 0,
        `Expected no bad_task_kind error for valid kind '${kind}', got: ${result.errors.join(", ")}`);
    }

    // absent task_kind → defaults to implementation (no error)
    const noKindYaml = `id: TECH-99999\ntype: tech\ntitle: Test\nstatus: open\nsection: test\n`;
    const noKindResult = validateBacklogRecord(noKindYaml);
    assert.equal(noKindResult.errors.filter(e => e.includes("bad_task_kind")).length, 0,
      "Expected no bad_task_kind error when task_kind absent");

    // invalid kind → error
    const invalidYaml = `id: TECH-99999\ntype: tech\ntitle: Test\nstatus: open\nsection: test\ntask_kind: garbage_kind\n`;
    const invalidResult = validateBacklogRecord(invalidYaml);
    assert.equal(invalidResult.errors.filter(e => e.includes("bad_task_kind")).length, 1,
      `Expected exactly 1 bad_task_kind error for invalid kind 'garbage_kind', got: ${invalidResult.errors.join(", ")}`);
  });

  it("blueprintMarkdown_CarriesFiveStageSectionIds [task 3.2]", () => {
    const blueprintPath = path.join(REPO_ROOT, "ia/templates/blueprints/ui-from-db.md");
    assert.ok(fs.existsSync(blueprintPath), `Blueprint file not found: ${blueprintPath}`);

    const content = fs.readFileSync(blueprintPath, "utf8");

    // Extract H2 headings — these are the deterministic section ids
    const h2Headings = [];
    for (const line of content.split("\n")) {
      if (line.startsWith("## ")) {
        h2Headings.push(line.slice(3).trim());
      }
    }

    const EXPECTED_SECTION_IDS = [
      "Schema-Probe",
      "Bake-Apply",
      "Render-Check",
      "Console-Sweep",
      "Tracer",
    ];

    assert.equal(h2Headings.length, EXPECTED_SECTION_IDS.length,
      `Expected exactly ${EXPECTED_SECTION_IDS.length} H2 section ids, got ${h2Headings.length}: [${h2Headings.join(", ")}]`);

    for (let i = 0; i < EXPECTED_SECTION_IDS.length; i++) {
      assert.equal(h2Headings[i], EXPECTED_SECTION_IDS[i],
        `Section id at position ${i} expected '${EXPECTED_SECTION_IDS[i]}' but got '${h2Headings[i]}'`);
    }

    // Check bake_handler_version stamp present
    assert.ok(content.includes("bake_handler_version:"),
      "Blueprint must carry bake_handler_version stamp comment");
  });

  it("shipPlan_BranchesOnTaskKindUiFromDb [task 3.3]", () => {
    const skillPath = path.join(REPO_ROOT, "ia/skills/ship-plan/SKILL.md");
    assert.ok(fs.existsSync(skillPath), `ship-plan SKILL.md not found: ${skillPath}`);

    const content = fs.readFileSync(skillPath, "utf8");

    // Phase 4.1 branch on task_kind: ui_from_db must be present
    assert.ok(content.includes("task_kind: ui_from_db") || content.includes("task_kind`"),
      "ship-plan SKILL.md Phase 4 must reference task_kind: ui_from_db branch");

    // Blueprint loader section must mention the 5 deterministic section ids
    const EXPECTED_IDS = ["Schema-Probe", "Bake-Apply", "Render-Check", "Console-Sweep", "Tracer"];
    for (const id of EXPECTED_IDS) {
      assert.ok(content.includes(id),
        `ship-plan SKILL.md must reference blueprint section id '${id}'`);
    }

    // Default branch must be explicitly documented as unchanged
    assert.ok(content.includes("default branch") || content.includes("Default branch"),
      "ship-plan SKILL.md must document that the default branch is unchanged");

    // Blueprint file must exist and carry the 5 sections (integration check)
    const blueprintPath = path.join(REPO_ROOT, "ia/templates/blueprints/ui-from-db.md");
    assert.ok(fs.existsSync(blueprintPath),
      `Blueprint file must exist at ${blueprintPath} for ship-plan loader to work`);

    const bpContent = fs.readFileSync(blueprintPath, "utf8");
    const ORDERED_IDS = ["Schema-Probe", "Bake-Apply", "Render-Check", "Console-Sweep", "Tracer"];
    const idPositions = ORDERED_IDS.map(id => bpContent.indexOf(`## ${id}`));
    for (let i = 0; i < idPositions.length; i++) {
      assert.ok(idPositions[i] !== -1,
        `Blueprint missing section '## ${ORDERED_IDS[i]}'`);
      if (i > 0) {
        assert.ok(idPositions[i] > idPositions[i - 1],
          `Blueprint section '${ORDERED_IDS[i]}' must appear after '${ORDERED_IDS[i - 1]}'`);
      }
    }
  });
});
