#!/usr/bin/env node
/**
 * drift-lint-sweep.mjs — callable entry point for async cron-driven drift lint.
 *
 * Delegates to validate-drift-lint.mjs. Accepts optional CLI args:
 *   --commit-sha <sha>   — informational; logged to stdout for audit trail.
 *   --slug <slug>        — informational; reserved for future per-plan scoping.
 *
 * Exit code = number of drift-lint errors (0 = clean), matching validate-drift-lint.mjs.
 *
 * Used by: tools/cron-server/handlers/drift-lint-cron-handler.ts
 * Cadence: every 10 min — low urgency sweep (cron expression "*\/10 * * * *")
 *
 * TECH-18105 / async-cron-jobs Stage 5.0.2
 */

import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Parse optional informational args.
const args = process.argv.slice(2);
const commitShaIdx = args.indexOf("--commit-sha");
const commitSha = commitShaIdx >= 0 ? args[commitShaIdx + 1] ?? null : null;
const slugIdx = args.indexOf("--slug");
const slug = slugIdx >= 0 ? args[slugIdx + 1] ?? null : null;

if (commitSha) console.log(`[drift-lint-sweep] commit_sha=${commitSha}`);
if (slug) console.log(`[drift-lint-sweep] slug=${slug}`);

// Delegate to the canonical validator. Dynamic import preserves its process.exit semantics.
await import(path.join(__dirname, "validate-drift-lint.mjs"));
