// Stage 5 — Audit fix-up (C1–C4 + H1) — TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends same file with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `npm run test:bake-pipeline-hardening`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   5.1 (TECH-27540) FastPathMap_RegistersNewValidators
//   5.2 (TECH-27541) — SlotAnchorResolver_ConsumedFromRuntimeAsmdef (EditMode C# test)
//   5.3 (TECH-27542) — BakeChildByKind_ProducesRealSliderToggleDropdown (EditMode C# test)
//   5.4 (TECH-27543) ValidatePanelBlueprint_FailsOnMissingRequiredKey
//   5.5 (TECH-27544) ConsoleScan_ChainedIntoVerifyLocal

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");

// ── 5.1 (TECH-27540): FastPathMap_RegistersNewValidators ──────────────────────

describe("Stage 5 fix-up — audit findings C1–C4 + H1", () => {
  it("FastPathMap_RegistersNewValidators [task 5.1]", () => {
    const mapPath = path.join(
      REPO_ROOT,
      "tools/scripts/validate-fast-path-map.json"
    );
    assert.ok(fs.existsSync(mapPath), `fast-path-map not found: ${mapPath}`);

    const raw = fs.readFileSync(mapPath, "utf8");
    const pathMap = JSON.parse(raw);

    function entryId(e) {
      return typeof e === "string" ? e : e.id;
    }

    // Build universe of all covered script ids.
    const covered = new Set(pathMap.baseline ?? []);
    for (const list of Object.values(pathMap.path_globs ?? {})) {
      for (const s of list) covered.add(entryId(s));
    }

    assert.ok(
      covered.has("validate:ui-id-consistency"),
      "validate:ui-id-consistency must appear in fast-path-map baseline or path_globs"
    );
    assert.ok(
      covered.has("validate:bake-handler-kind-coverage"),
      "validate:bake-handler-kind-coverage must appear in fast-path-map baseline or path_globs"
    );
  });

  // ── 5.4 (TECH-27543): ValidatePanelBlueprint_FailsOnMissingRequiredKey ──────

  it("ValidatePanelBlueprint_FailsOnMissingRequiredKey [task 5.4]", async () => {
    // Writes a temp panels.json fixture with a slider-row child missing the `label`
    // required key, invokes the Node-shell validate_panel_blueprint harness,
    // asserts ok=false + missing[] contains the slider-row label gap.
    const harnessPath = path.join(
      REPO_ROOT,
      "tools/scripts/validate-panel-blueprint-harness.mjs"
    );
    assert.ok(
      fs.existsSync(harnessPath),
      `validate-panel-blueprint-harness.mjs not found at ${harnessPath}`
    );

    // Write a fixture panels.json missing the `label` key on a slider-row child.
    const fixture = {
      items: [
        {
          id: "test-fixture",
          slug: "test-fixture",
          children: [
            {
              kind: "slider-row",
              params_json: {}
              // intentionally missing `label`
            }
          ]
        }
      ]
    };

    const tmpDir = path.join(REPO_ROOT, "tests/bake-pipeline/.tmp");
    fs.mkdirSync(tmpDir, { recursive: true });
    const fixturePath = path.join(tmpDir, "panels-missing-label.json");
    fs.writeFileSync(fixturePath, JSON.stringify(fixture, null, 2), "utf8");

    // Run harness: node harness.mjs --panel-id test-fixture --panels-file <path>
    const { execFileSync } = await import("node:child_process");
    let stdout;
    try {
      stdout = execFileSync(
        "node",
        [harnessPath, "--panel-id", "test-fixture", "--panels-file", fixturePath],
        { encoding: "utf8", timeout: 10000 }
      );
    } catch (err) {
      // Harness may exit non-zero — capture stdout from error object.
      stdout = err.stdout ?? "";
    }

    let result;
    try {
      result = JSON.parse(stdout.trim());
    } catch {
      assert.fail(
        `validate-panel-blueprint-harness did not emit valid JSON. stdout: ${stdout}`
      );
    }

    assert.strictEqual(result.ok, false, "ok must be false when required key missing");
    assert.ok(
      Array.isArray(result.missing) && result.missing.length > 0,
      "missing[] must be non-empty when slider-row lacks required key"
    );

    const hasLabelGap = result.missing.some(
      (m) => m.required === "label" || (typeof m === "string" && m.includes("label"))
    );
    assert.ok(hasLabelGap, `missing[] must contain a label gap entry. Got: ${JSON.stringify(result.missing)}`);

    // Cleanup.
    fs.rmSync(fixturePath, { force: true });
  });

  // ── 5.5 (TECH-27544): ConsoleScan_ChainedIntoVerifyLocal ────────────────────

  it("ConsoleScan_ChainedIntoVerifyLocal [task 5.5]", () => {
    const pkgPath = path.join(REPO_ROOT, "package.json");
    const pkg = JSON.parse(fs.readFileSync(pkgPath, "utf8"));

    const verifyLocal = pkg.scripts?.["verify:local"] ?? "";
    assert.ok(
      verifyLocal.includes("console:scan"),
      `verify:local must include 'console:scan'. Got: ${verifyLocal}`
    );

    // console:scan must appear after db:bridge-playmode-smoke in the overall chain.
    // The chain may be in verify-local.sh, checked via the shell script body.
    const shellPath = path.join(
      REPO_ROOT,
      "tools/scripts/post-implementation-verify.sh"
    );
    const shellBody = fs.readFileSync(shellPath, "utf8");

    const smokeIdx = shellBody.indexOf("db:bridge-playmode-smoke");
    const scanIdx = shellBody.indexOf("console:scan");

    assert.ok(
      smokeIdx !== -1,
      "post-implementation-verify.sh must reference db:bridge-playmode-smoke"
    );
    assert.ok(
      scanIdx !== -1,
      "post-implementation-verify.sh must reference console:scan after db:bridge-playmode-smoke"
    );
    assert.ok(
      scanIdx > smokeIdx,
      "console:scan step must appear after db:bridge-playmode-smoke in post-implementation-verify.sh"
    );
  });
});
