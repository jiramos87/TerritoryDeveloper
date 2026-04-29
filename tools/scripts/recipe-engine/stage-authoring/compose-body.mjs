#!/usr/bin/env node
// stage-authoring — compose §Plan Digest markdown body from seam output.
//
// Output shape (literal § character throughout — rubric #0):
//
//   ## §Plan Digest
//
//   ### §Goal
//   {goal}
//
//   ### §Acceptance
//   {acceptance}
//
//   ### §Pending Decisions
//   {pending_decisions}
//
//   ### §Implementer Latitude
//   {implementer_latitude}
//
//   ### §Work Items
//   {work_items}
//
//   ### §Test Blueprint
//   {test_blueprint}
//
//   ### §Invariants & Gate
//   {invariants_and_gate}
//
// Empty sections still emit the heading (rubric #1–7 require all 7 subsections).

import process from "node:process";

const args = parseArgs(process.argv.slice(2));
const required = [
  "goal",
  "acceptance",
  "pending-decisions",
  "implementer-latitude",
  "work-items",
  "test-blueprint",
  "invariants-and-gate",
];
for (const key of required) {
  if (args[key] === undefined) {
    process.stderr.write(`compose-body: missing --${key}\n`);
    process.exit(1);
  }
}

const sections = [
  ["§Goal", args["goal"]],
  ["§Acceptance", args["acceptance"]],
  ["§Pending Decisions", args["pending-decisions"]],
  ["§Implementer Latitude", args["implementer-latitude"]],
  ["§Work Items", args["work-items"]],
  ["§Test Blueprint", args["test-blueprint"]],
  ["§Invariants & Gate", args["invariants-and-gate"]],
];

const out = ["## §Plan Digest", ""];
for (const [heading, body] of sections) {
  out.push(`### ${heading}`);
  out.push("");
  out.push(body.trim());
  out.push("");
}
process.stdout.write(out.join("\n").replace(/\n{3,}/g, "\n\n"));
process.exit(0);

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith("--")) {
      const key = a.slice(2);
      const next = argv[i + 1];
      if (next !== undefined && !next.startsWith("--")) {
        out[key] = next;
        i++;
      } else {
        out[key] = "";
      }
    }
  }
  return out;
}
