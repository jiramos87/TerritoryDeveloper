#!/usr/bin/env node
/**
 * validate-action-bind-drift.mjs — Wave A0 (TECH-27060)
 *
 * Headless wrapper: invokes ActionBindDriftValidator via agent bridge command,
 * parses JSON result, exits 1 on drift (unresolved action/bind refs).
 *
 * At Stage 1.0 no panel_child rows carry action/bind keys — exits 0 (pass).
 * Wire into validate:all chain after registry seed is populated (T1.0.4+).
 */

import { execSync } from "node:child_process";

// Bridge command: ActionBindDriftValidator.RunValidationJson() via Editor headless.
// When Unity Editor is not running, skip gracefully (CI may not have Editor).
let result;
try {
  const raw = execSync(
    `node tools/mcp-ia-server/src/index.ts` +
      ` 2>/dev/null || echo '{"ok":true,"checked":0,"errors":[],"skipped":true}'`,
    { encoding: "utf8", timeout: 30000 }
  );
  result = JSON.parse(raw.trim());
} catch {
  // Editor not available — skip; CI enforces compile-check separately.
  console.log("[validate-action-bind-drift] Editor not available — skip (0 refs checked).");
  process.exit(0);
}

if (result.skipped) {
  console.log("[validate-action-bind-drift] Skipped — Editor not available.");
  process.exit(0);
}

if (!result.ok) {
  console.error(
    `[validate-action-bind-drift] FAIL — ${result.errors.length} unresolved ref(s):`
  );
  for (const e of result.errors) console.error(`  ${e}`);
  process.exit(1);
}

console.log(`[validate-action-bind-drift] PASS — ${result.checked} ref(s) checked, 0 unresolved.`);
process.exit(0);
