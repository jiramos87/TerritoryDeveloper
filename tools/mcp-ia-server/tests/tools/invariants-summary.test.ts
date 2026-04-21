/**
 * invariants_summary — structured payload + domain filter + markdown side-channel.
 * TECH-373 Stage 1.2 T1.2.3.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { buildRegistry } from "../../src/config.js";
import {
  buildInvariantsPayload,
  parseInvariantsBody,
} from "../../src/tools/invariants-summary.js";
import { wrapTool } from "../../src/envelope.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const invariantsMdPath = path.join(repoRoot, "ia/rules/invariants.md");
const unityInvariantsMdPath = path.join(
  repoRoot,
  "ia/rules/unity-invariants.md",
);

const registryAvailable =
  fs.existsSync(invariantsMdPath) && fs.existsSync(unityInvariantsMdPath);

function parsedSourceCounts(): { invariants: number; guardrails: number } {
  // Sum across both files — tool merges Unity + universal.
  const paths = [unityInvariantsMdPath, invariantsMdPath];
  let invariants = 0;
  let guardrails = 0;
  for (const p of paths) {
    const body = fs.readFileSync(p, "utf8");
    const stripped = body.replace(/^---[\s\S]*?---\s*/m, "");
    const parsed = parseInvariantsBody(stripped);
    invariants += parsed.invariants.length;
    guardrails += parsed.guardrails.length;
  }
  return { invariants, guardrails };
}

test(
  "buildInvariantsPayload with domain='roads' filters to roads-tagged subset",
  { skip: !registryAvailable },
  () => {
    const registry = buildRegistry();
    const result = buildInvariantsPayload(registry, "roads");
    assert.ok(result, "payload should be non-null when invariants registered");
    // At least one of invariants/guardrails non-empty for 'roads' (sidecar tags it).
    assert.ok(
      result!.invariants.length > 0 || result!.guardrails.length > 0,
      "expected 'roads' domain to match ≥1 invariant or guardrail",
    );
    for (const inv of result!.invariants) {
      assert.ok(
        inv.subsystem_tags.some((t) => t.toLowerCase().includes("roads")),
        `invariant #${inv.number} should have 'roads' tag`,
      );
    }
    for (const gr of result!.guardrails) {
      assert.ok(
        gr.subsystem_tags.some((t) => t.toLowerCase().includes("roads")),
        `guardrail #${gr.index} should have 'roads' tag`,
      );
    }
    assert.equal(typeof result!.markdown, "string");
  },
);

test(
  "buildInvariantsPayload with unmatched domain returns empty arrays without throwing",
  { skip: !registryAvailable },
  () => {
    const registry = buildRegistry();
    const result = buildInvariantsPayload(registry, "nonexistent-tag-xyz");
    assert.ok(result);
    assert.deepEqual(result!.invariants, []);
    assert.deepEqual(result!.guardrails, []);
    assert.equal(typeof result!.markdown, "string");
    assert.equal(result!.markdown, "");
  },
);

test(
  "buildInvariantsPayload with no domain returns all 13 invariants + all guardrails",
  { skip: !registryAvailable },
  () => {
    const registry = buildRegistry();
    const result = buildInvariantsPayload(registry);
    assert.ok(result);
    assert.equal(result!.invariants.length, 13);
    const counts = parsedSourceCounts();
    assert.equal(
      result!.guardrails.length,
      counts.guardrails,
      "guardrails length should match parsed source (not hard-coded)",
    );
    assert.ok(result!.markdown.length > 0, "markdown side-channel non-empty");
  },
);

test(
  "buildInvariantsPayload markdown side-channel is always a string",
  { skip: !registryAvailable },
  () => {
    const registry = buildRegistry();
    const withDomain = buildInvariantsPayload(registry, "roads");
    const noDomain = buildInvariantsPayload(registry);
    const unmatched = buildInvariantsPayload(registry, "nonexistent-tag-xyz");
    assert.equal(typeof withDomain!.markdown, "string");
    assert.equal(typeof noDomain!.markdown, "string");
    assert.equal(typeof unmatched!.markdown, "string");
  },
);

// ---------------------------------------------------------------------------
// Envelope shape tests (TECH-401 Phase 1)
// ---------------------------------------------------------------------------

test(
  "invariants_summary envelope — ok:true + InvariantsPayload on success",
  { skip: !registryAvailable },
  async () => {
    const registry = buildRegistry();
    const handler = wrapTool(async ({ domain }: { domain?: string }) => {
      const payload = buildInvariantsPayload(registry, domain);
      if (!payload) {
        throw { code: "spec_not_found" as const, message: "invariants.mdc not registered." };
      }
      return payload;
    });
    const result = await handler({});
    assert.equal(result.ok, true, "expected ok:true envelope");
    if (result.ok) {
      assert.ok(Array.isArray(result.payload.invariants), "payload.invariants should be array");
      assert.ok(Array.isArray(result.payload.guardrails), "payload.guardrails should be array");
      assert.equal(typeof result.payload.markdown, "string");
      assert.equal(typeof result.payload.description, "string");
    }
  },
);

test(
  "invariants_summary envelope — ok:false + spec_not_found when registry empty",
  async () => {
    const handler = wrapTool(async ({ domain }: { domain?: string }) => {
      // Empty registry → buildInvariantsPayload returns null
      const payload = buildInvariantsPayload([], domain);
      if (!payload) {
        throw { code: "spec_not_found" as const, message: "invariants.mdc not registered." };
      }
      return payload;
    });
    const result = await handler({});
    assert.equal(result.ok, false, "expected ok:false envelope");
    if (!result.ok) {
      assert.equal(result.error.code, "spec_not_found");
    }
  },
);
