// TECH-11940 — Stage 6 game-ui-catalog-bake — IR JSON runtime severance gate.
//
// Per DEC-A24 §6 D6: the claude-design IR JSON pipeline (under
// `web/design-refs/step-1-game-ui/`) is demoted to a sketchpad-only surface;
// the runtime bake path must NOT read any of its files nor mirror its schema.
//
// This validator scans every `Assets/Scripts/**/*.cs` source for any of:
//
//     design-refs    → claude-design sketchpad directory
//     cd-bundle      → claude-design bundle artifact
//     ir.json        → IR runtime json input (any nested form)
//     transcribe-cd  → claude-design transcribe pipeline
//     ir-schema      → claude-design IR schema mirror
//
// Each surviving match indicates a runtime tether to the demoted pipeline.
// The test asserts ZERO matches across all C# sources. Failure prints every
// match with its file + line so the implementer can scrub the reference.
//
// Anchor (declared in §Red-Stage Proof):
//   visibility-delta-test:tools/tests/validate-no-ir-bake-runtime-refs.test.mjs::NoRuntimeRefsToIrBakeHandler

import assert from "node:assert";
import { readFileSync, readdirSync, statSync } from "node:fs";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const SCAN_ROOT = path.join(REPO_ROOT, "Assets/Scripts");

const FORBIDDEN_RE = /design-refs|cd-bundle|ir\.json|transcribe-cd|ir-schema/;

function listCsFilesRecursive(root) {
  const out = [];
  for (const entry of readdirSync(root)) {
    const abs = path.join(root, entry);
    const st = statSync(abs);
    if (st.isDirectory()) {
      out.push(...listCsFilesRecursive(abs));
      continue;
    }
    if (st.isFile() && abs.endsWith(".cs")) out.push(abs);
  }
  return out;
}

test("NoRuntimeRefsToIrBakeHandler — Assets/Scripts/**/*.cs free of demoted IR JSON pipeline refs", () => {
  const files = listCsFilesRecursive(SCAN_ROOT);
  const hits = [];
  for (const file of files) {
    const text = readFileSync(file, "utf8");
    const lines = text.split("\n");
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (FORBIDDEN_RE.test(line)) {
        hits.push({
          file: path.relative(REPO_ROOT, file),
          line: i + 1,
          text: line.trim(),
        });
      }
    }
  }
  if (hits.length > 0) {
    const summary = hits
      .map((h) => `  ${h.file}:${h.line}  ${h.text}`)
      .join("\n");
    assert.fail(
      `Found ${hits.length} runtime ref(s) to demoted IR JSON pipeline (DEC-A24 §6 D6 violation):\n${summary}\n\n` +
        `Demoted: web/design-refs/step-1-game-ui/, cd-bundle artifacts, ir-schema.ts, transcribe-cd-game-ui.\n` +
        `Runtime path must not depend on these. Remove or reword every match.`,
    );
  }
  assert.strictEqual(hits.length, 0);
});
