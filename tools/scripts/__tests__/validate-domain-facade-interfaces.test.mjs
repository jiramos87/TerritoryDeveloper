/**
 * validate-domain-facade-interfaces.test.mjs
 *
 * validator_fails_on_missing_facade_interface:
 *   - Domains/Foo/ without IFoo.cs → exit 1
 *   - Domains/Bar/ with IBar.cs (valid) → exit 0
 */

import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import { execSync } from "node:child_process";
import { mkdirSync, writeFileSync, rmSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const VALIDATOR = resolve(__dirname, "../validate-domain-facade-interfaces.mjs");
const TMP_DIR = resolve(__dirname, "../../../tmp-facade-validator-test");

function runValidator(domainsRoot) {
  try {
    const out = execSync(`node "${VALIDATOR}" --domains-root "${domainsRoot}"`, {
      encoding: "utf8",
      stdio: ["pipe", "pipe", "pipe"],
    });
    return { exitCode: 0, output: out };
  } catch (e) {
    return { exitCode: e.status ?? 1, output: (e.stdout ?? "") + (e.stderr ?? "") };
  }
}

before(() => {
  mkdirSync(TMP_DIR, { recursive: true });
});

after(() => {
  try { rmSync(TMP_DIR, { recursive: true, force: true }); } catch {}
});

describe("validator_fails_on_missing_facade_interface", () => {
  it("exits 1 when Domains/Foo/ has no IFoo.cs", () => {
    const root = resolve(TMP_DIR, "missing-facade");
    mkdirSync(resolve(root, "Foo"), { recursive: true });
    const { exitCode, output } = runValidator(root);
    assert.equal(exitCode, 1, `Expected exit 1. Output: ${output}`);
    assert.match(output, /IFoo\.cs absent/, "Expected 'IFoo.cs absent' message");
  });

  it("exits 0 when Domains/Bar/ has valid IBar.cs", () => {
    const root = resolve(TMP_DIR, "valid-facade");
    mkdirSync(resolve(root, "Bar"), { recursive: true });
    writeFileSync(
      resolve(root, "Bar", "IBar.cs"),
      `namespace Domains.Bar { public interface IBar { void DoSomething(); } }`
    );
    const { exitCode, output } = runValidator(root);
    assert.equal(exitCode, 0, `Expected exit 0. Output: ${output}`);
  });

  it("exits 1 when IFoo.cs exists but does not declare public interface IFoo", () => {
    const root = resolve(TMP_DIR, "wrong-interface");
    mkdirSync(resolve(root, "Foo"), { recursive: true });
    writeFileSync(
      resolve(root, "Foo", "IFoo.cs"),
      `namespace Domains.Foo { public class Foo {} }`
    );
    const { exitCode, output } = runValidator(root);
    assert.equal(exitCode, 1, `Expected exit 1. Output: ${output}`);
    assert.match(output, /does not declare/, "Expected 'does not declare' message");
  });

  it("exits 0 on empty Domains/ folder (initial run)", () => {
    const root = resolve(TMP_DIR, "empty-domains");
    mkdirSync(root, { recursive: true });
    const { exitCode } = runValidator(root);
    assert.equal(exitCode, 0, "Expected exit 0 for empty Domains/ (initial run)");
  });
});
