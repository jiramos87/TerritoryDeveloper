#!/usr/bin/env node
// stage-decompose — compose new Stage body from existing skeleton body + seam output.
//
// Args:
//   --existing-body <string>    Verbatim body returned by stage_render (everything below
//                               `### Stage X.Y — Name` heading). MUST be supplied.
//   --stage-id <X.Y>            Stage id used to mint task ids `TX.Y.{i}`.
//   --seam-output <json>        Single JSON-serialized object matching seam
//                               `decompose-skeleton-stage` output schema:
//                               { tasks[], stage_file_plan, plan_fix }.
//
// Stdout: full new Stage body markdown — caller hands to `stage_body_write` MCP.
//
// Behavior:
//   - Header fields (Status / Notes / Backlog state / Objectives / Exit / Art /
//     Relevant surfaces) preserved verbatim from existing body, up to (not including)
//     any `**Tasks:**` line or first `#### §` heading.
//   - `**Tasks:** _TBD_` placeholder dropped; new Task table appended.
//   - 5-column canonical Task table (Task | Name | Issue | Status | Intent).
//   - Pending subsections appended verbatim from seam output. Empty plan_fix → pending stub.
//   - Pipe `|` chars in cell prose escaped to `\|`.

import process from "node:process";

const args = parseArgs(process.argv.slice(2));
for (const key of ["existing-body", "stage-id", "seam-output"]) {
  if (args[key] === undefined) {
    process.stderr.write(`compose-body: missing --${key}\n`);
    process.exit(1);
  }
}

const stageNorm = String(args["stage-id"]).replace(/^Stage /, "").trim();
const seam = parseJson(args["seam-output"], "seam-output");
const existingBody = String(args["existing-body"]);

if (!Array.isArray(seam.tasks) || seam.tasks.length < 2) {
  process.stderr.write(`compose-body: seam.tasks must have ≥2 entries (got ${(seam.tasks ?? []).length})\n`);
  process.exit(1);
}

const header = stripTrailingTasksAndSections(existingBody).trimEnd();

const taskRows = seam.tasks.map((t, idx) => {
  const taskId = `T${stageNorm}.${idx + 1}`;
  const name = cell(t.title);
  const intent = cell(t.summary);
  return `| ${taskId} | ${name} | _pending_ | _pending_ | ${intent} |`;
});

const out = [];
if (header.length > 0) {
  out.push(header);
  out.push("");
}
out.push("**Tasks:**");
out.push("");
out.push("| Task | Name | Issue | Status | Intent |");
out.push("|---|---|---|---|---|");
out.push(...taskRows);
out.push("");
out.push("#### §Stage File Plan");
out.push("");
out.push(String(seam.stage_file_plan).trim());
out.push("");
out.push("#### §Plan Fix");
out.push("");
const planFix = String(seam.plan_fix ?? "").trim();
out.push(planFix.length > 0 ? planFix : "_pending — populated by `/plan-review` when fixes are needed._");
out.push("");

process.stdout.write(out.join("\n").replace(/\n{3,}/g, "\n\n"));
process.exit(0);

function stripTrailingTasksAndSections(body) {
  const lines = body.split("\n");
  let cutAt = lines.length;
  for (let i = 0; i < lines.length; i++) {
    const ln = lines[i];
    if (/^\*\*Tasks:\*\*/.test(ln)) {
      cutAt = i;
      break;
    }
    if (/^####\s+§/.test(ln)) {
      cutAt = i;
      break;
    }
    if (/^\| Task \| Name \|/.test(ln)) {
      cutAt = i;
      break;
    }
  }
  return lines.slice(0, cutAt).join("\n");
}

function cell(s) {
  return String(s ?? "").replace(/\r?\n+/g, " ").replace(/\|/g, "\\|").trim();
}

function parseJson(raw, label) {
  try {
    return JSON.parse(raw);
  } catch (err) {
    process.stderr.write(`compose-body: --${label} not valid JSON: ${err instanceof Error ? err.message : String(err)}\n`);
    process.exit(1);
  }
}

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
