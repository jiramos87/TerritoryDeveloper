#!/usr/bin/env node
/**
 * Validate master-plan status lifecycle consistency (TECH-397).
 *
 * Rules checked:
 *   R1 — Plan top Status != Draft when any task row has a filed issue id (≠ _pending_).
 *   R2 — Stage Status == Draft/Planned when tasks filed (issue id present + yaml open or archived).
 *        Expected: "In Progress" or "Final".
 *   R3 — Stage Status == Final when ALL tasks in stage are archived.
 *   R4 — Step Status == Final when ALL stages under step are Final. (advisory only — Step bodies
 *        may lack machine-readable stage lists; emits warning not error)
 *   R5 — Plan top Status == Final when ALL steps are Final. (checked via step parse)
 *   R6 — Task table rows all Done-like but any filed id still has open ia/backlog/{id}.yaml
 *        (Pass 2 tail — verify-loop → code-review → audit → stage closeout — not finished).
 *
 * Limitations:
 *   - Parsing is best-effort Markdown heuristic (no AST).
 *   - Step/Stage Status is extracted from "**Status:**" lines immediately below headers.
 *   - Task table rows parsed via regex; _pending_ id ⇒ not filed; TECH-/FEAT-/BUG-/ART-/AUDIO- id ⇒ filed.
 *   - "archived" = yaml exists in ia/backlog-archive/; "open" = ia/backlog/.
 *   - Top status "Final" ⇒ no R1/R2/R3 checks (plan closed).
 *
 * Usage:
 *   node tools/validate-master-plan-status.mjs [--advisory]
 *   node tools/validate-master-plan-status.mjs [--plan path/to/plan.md]
 *
 * Exit: 0 clean; 1 drift found (unless --advisory).
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..");

const BACKLOG_DIR = path.join(REPO_ROOT, "ia", "backlog");
const ARCHIVE_DIR = path.join(REPO_ROOT, "ia", "backlog-archive");
const PLANS_DIR = path.join(REPO_ROOT, "ia", "projects");

const ADVISORY = process.argv.includes("--advisory");
const SINGLE_PLAN = (() => {
  const idx = process.argv.indexOf("--plan");
  return idx !== -1 ? process.argv[idx + 1] : null;
})();

// ── helpers ──────────────────────────────────────────────────────────────────

/** @param {string} id e.g. "TECH-305" */
function isArchived(id) {
  return fs.existsSync(path.join(ARCHIVE_DIR, `${id}.yaml`));
}

/** @param {string} id */
function isFiled(id) {
  return (
    fs.existsSync(path.join(BACKLOG_DIR, `${id}.yaml`)) ||
    isArchived(id)
  );
}

/** Open backlog record (not yet archived on closeout). */
function isOpen(id) {
  return fs.existsSync(path.join(BACKLOG_DIR, `${id}.yaml`));
}

/**
 * Task-row Status column looks terminal-Done for ship-stage (not In Progress / Review / Draft).
 * @param {string} raw
 */
function statusLooksDoneLike(raw) {
  const t = raw.toLowerCase().replace(/\*/g, "").trim();
  if (t.includes("_pending_")) return false;
  if (t.includes("in progress") || t.includes("in review")) return false;
  if (/\bdraft\b/.test(t) && !t.includes("done")) return false;
  return (
    /\bdone\b/.test(t) ||
    t.includes("archived") ||
    t === "skipped" ||
    t.includes("skipped")
  );
}

const ISSUE_ID_RE = /\b(TECH|FEAT|BUG|ART|AUDIO)-\d+[a-z]?\b/g;

/** Extract all issue ids from a string (excludes _pending_). */
function extractIds(text) {
  const ids = [];
  let m;
  ISSUE_ID_RE.lastIndex = 0;
  while ((m = ISSUE_ID_RE.exec(text)) !== null) {
    ids.push(m[0]);
  }
  return [...new Set(ids)];
}

/**
 * Normalise a raw status string to a canonical token for comparison.
 * @param {string} raw
 * @returns {"Draft"|"Planned"|"Skeleton"|"InProgress"|"InReview"|"Final"|"Done"|"Unknown"}
 */
function normaliseStatus(raw) {
  const s = raw.trim().toLowerCase();
  if (s.startsWith("final")) return "Final";
  if (s.startsWith("done")) return "Final"; // stage "Done" = Final
  if (s.startsWith("in progress")) return "InProgress";
  // CamelCase / single-token (synthetic parser step, some authoring tools)
  if (s === "inprogress") return "InProgress";
  if (s.startsWith("in review")) return "InReview";
  if (s.startsWith("planned")) return "Planned";
  if (s.startsWith("skeleton")) return "Skeleton";
  if (s.startsWith("not started")) return "Draft";
  if (s.startsWith("draft")) return "Draft";
  if (s.startsWith("paused")) return "InProgress"; // treat Paused as in-progress for drift check
  return "Unknown";
}

// ── parser ────────────────────────────────────────────────────────────────────

/**
 * Parse a master-plan Markdown file into a structure:
 * {
 *   topStatus: string,              // raw from "> **Status:**" line
 *   steps: [{
 *     heading: string,
 *     rawStatus: string,
 *     stages: [{
 *       heading: string,
 *       rawStatus: string,
 *       taskIds: string[],          // filed ids (non-_pending_)
 *       pendingCount: number,
 *     }]
 *   }]
 * }
 */
function parsePlan(filePath) {
  const text = fs.readFileSync(filePath, "utf8");
  const lines = text.split(/\r?\n/);

  // Top status: first "> **Status:**" or "> **Status:** …" line in the preamble
  let topStatus = "Unknown";
  for (const line of lines) {
    const m = /^>\s*\*\*Status:\*\*\s*(.+)$/.exec(line.trim());
    if (m) {
      topStatus = m[1].trim();
      break;
    }
  }

  const steps = [];
  let currentStep = null;
  let currentStage = null;

  /** Append current stage to current step if exists. */
  function flushStage() {
    if (currentStage && currentStep) {
      currentStep.stages.push(currentStage);
    }
    currentStage = null;
  }

  /** Append current step to steps if exists. */
  function flushStep() {
    flushStage();
    if (currentStep) {
      steps.push(currentStep);
    }
    currentStep = null;
  }

  // Track state for multi-line task table parsing.
  let inTaskTable = false;
  // Track whether we're inside the ## Steps section (main body).
  // Decision Log, Deferred decomposition, Orchestration guardrails etc. are
  // top-level ## sections that follow ## Steps — flush + stop task parsing there.
  let inStepsSection = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const stripped = line.trim();

    // Top-level ## section headers (e.g. "## Steps", "## Decision Log")
    // But NOT "## Step N" which is a step heading in some plans (blip uses h2 for post-MVP steps)
    if (/^##\s+/.test(stripped) && !stripped.startsWith("###") && !/^##\s+Step\s+\d/.test(stripped)) {
      if (/^##\s+(Steps|Stages)\b/.test(stripped)) {
        // Accept both "## Steps" (canonical per template) and "## Stages"
        // (web-platform-master-plan uses this — plan has no Step layer).
        inStepsSection = true;
      } else if (inStepsSection) {
        // Leaving the Steps section (e.g. ## Decision Log, ## Deferred decomposition)
        // Flush everything and stop parsing steps/stages to avoid ID bleed from other tables
        flushStep();
        inStepsSection = false;
        inTaskTable = false;
        currentStep = null;
        currentStage = null;
      }
    }

    // ### Step N — … (or ## Step N — for plans using h2 for post-MVP steps)
    const stepMatch = /^#{2,3}\s+Step\s+\d/.test(stripped);
    if (stepMatch) {
      flushStep();
      currentStep = {
        heading: stripped,
        rawStatus: "Unknown",
        stages: [],
      };
      inTaskTable = false;
      // Peek next few lines for **Status:** (not blockquote style)
      for (let j = i + 1; j < Math.min(i + 10, lines.length); j++) {
        const next = lines[j].trim();
        const sm = /^\**Status:\**\s*(.+)$/.exec(next);
        const bm = /^>\s*\**Status:\**\s*(.+)$/.exec(next);
        if (sm) { currentStep.rawStatus = sm[1].trim(); break; }
        if (bm) { currentStep.rawStatus = bm[1].trim(); break; }
        if (next.startsWith("####") || next.startsWith("###")) break;
      }
      continue;
    }

    // ### Stage N.M — … or #### Stage N.M — …
    // Heading depth varies by authoring skill (master-plan-new emits ####,
    // template.md + some hand-authored plans emit ###). Accept both.
    const stageMatch = /^#{3,4}\s+Stage\s+[\d.]+/.test(stripped);
    if (stageMatch) {
      flushStage();
      if (!currentStep) {
        // Stage outside a step — create a synthetic step
        currentStep = {
          heading: "(implicit step)",
          rawStatus: "InProgress",
          stages: [],
        };
      }
      currentStage = {
        heading: stripped,
        rawStatus: "Unknown",
        taskIds: [],
        taskRows: [],
        pendingCount: 0,
      };
      inTaskTable = false;
      // Peek next few lines for **Status:**
      for (let j = i + 1; j < Math.min(i + 15, lines.length); j++) {
        const next = lines[j].trim();
        const sm = /^\**Status:\**\s*(.+)$/.exec(next);
        const bm = /^>\s*\**Status:\**\s*(.+)$/.exec(next);
        if (sm) { currentStage.rawStatus = sm[1].trim(); break; }
        if (bm) { currentStage.rawStatus = bm[1].trim(); break; }
        // Stop at next section header
        if (next.startsWith("###") || next.startsWith("**Objectives") || next.startsWith("**Backlog")) break;
      }
      continue;
    }

    // Task table rows: | Task | Name | Phase | Issue | Status | Intent |
    // Column layout (0-indexed after split by "|", skipping leading empty):
    //   0=Task, 1=Name, 2=Phase, 3=Issue, 4=Status, 5=Intent
    // Extract issue ids ONLY from the Issue column (col 3) to avoid false positives
    // from ids mentioned in Name or Intent columns.
    if (currentStage && stripped.startsWith("|")) {
      // Skip header/separator rows
      if (/^\|-+/.test(stripped) || /^\|\s*Task\s*\|/.test(stripped) || /^\|\s*-+\s*\|/.test(stripped)) {
        inTaskTable = true;
        continue;
      }
      if (inTaskTable || stripped.includes("_pending_") || ISSUE_ID_RE.test(stripped)) {
        inTaskTable = true;
        // Split into columns; first element is empty (before leading |)
        const cols = stripped.split("|");
        // cols[0] = "" (before leading |), cols[1]=Task, cols[2]=Name, cols[3]=Phase, cols[4]=Issue, cols[5]=Status
        // Issue column is cols[4] (1-indexed col 4 = Issue)
        const issueCol = cols.length >= 5 ? cols[4] : "";
        const statusCol = cols.length >= 6 ? cols[5] : "";
        // Extract ids from Issue column only
        const ids = extractIds(issueCol);
        for (const id of ids) {
          if (isFiled(id)) {
            currentStage.taskIds.push(id);
          }
        }
        currentStage.taskRows.push({
          ids,
          statusRaw: statusCol,
          hasPendingPlaceholder: issueCol.includes("_pending_"),
        });
        // Count pending — check Issue or Status column
        if (issueCol.includes("_pending_") || statusCol.includes("_pending_")) {
          currentStage.pendingCount += 1;
        }
      }
      continue;
    }

    // Reset inTaskTable on blank line or non-table content after table started
    if (inTaskTable && !stripped.startsWith("|")) {
      inTaskTable = false;
    }
  }

  flushStep();

  return { filePath, topStatus, steps };
}

// ── drift checks ─────────────────────────────────────────────────────────────

/**
 * @typedef {{ plan: string, location: string, message: string, rule: string }} DriftItem
 */

/**
 * @param {ReturnType<typeof parsePlan>} plan
 * @param {DriftItem[]} drifts
 */
function checkPlan(plan, drifts) {
  const planName = path.basename(plan.filePath);
  const topNorm = normaliseStatus(plan.topStatus);

  // Skip fully Final plans (no drift possible by definition)
  if (topNorm === "Final") return;

  let anyTaskFiled = false;
  let allStepsFinal = plan.steps.length > 0;

  for (const step of plan.steps) {
    const stepNorm = normaliseStatus(step.rawStatus);
    let allStagesFinal = step.stages.length > 0;

    for (const stage of step.stages) {
      const stageNorm = normaliseStatus(stage.rawStatus);
      const hasFiled = stage.taskIds.length > 0;
      const allArchived =
        hasFiled &&
        stage.taskIds.every((id) => isArchived(id));

      if (hasFiled) anyTaskFiled = true;

      // R3 — all tasks archived → stage should be Final
      if (allArchived && stageNorm !== "Final") {
        drifts.push({
          plan: planName,
          location: stage.heading,
          message: `All tasks archived but Status = "${stage.rawStatus}" (expected Final)`,
          rule: "R3",
        });
      }

      // R6 — table rows all Done-like but backlog still open (Pass 2 tail not landed)
      if (stage.taskRows && stage.taskRows.length > 0) {
        const anyPendingPlaceholder = stage.taskRows.some((r) => r.hasPendingPlaceholder);
        const filedRows = stage.taskRows.filter((r) => r.ids.length > 0);
        if (!anyPendingPlaceholder && filedRows.length > 0) {
          const allDoneLike = filedRows.every((r) => statusLooksDoneLike(r.statusRaw));
          if (allDoneLike) {
            const unionIds = [...new Set(filedRows.flatMap((r) => r.ids))];
            const openIds = unionIds.filter((id) => isOpen(id));
            if (openIds.length > 0) {
              drifts.push({
                plan: planName,
                location: stage.heading,
                message: `Task rows look Done but backlog record(s) still open (${openIds.join(", ")}). Stage tail incomplete — re-run \`/ship-stage\` (Pass 2 resume) or Stage-scoped \`/closeout\`.`,
                rule: "R6",
              });
            }
          }
        }
      }

      // R2 — tasks filed (any open or archived) but stage still Draft/Planned
      if (
        hasFiled &&
        !allArchived &&
        (stageNorm === "Draft" || stageNorm === "Planned" || stageNorm === "Unknown")
      ) {
        drifts.push({
          plan: planName,
          location: stage.heading,
          message: `Tasks filed (${stage.taskIds.join(", ")}) but Status = "${stage.rawStatus}" (expected In Progress)`,
          rule: "R2",
        });
      }

      if (stageNorm !== "Final") allStagesFinal = false;
    }

    // R4 — all stages Final → step should be Final (advisory)
    if (allStagesFinal && step.stages.length > 0 && stepNorm !== "Final") {
      drifts.push({
        plan: planName,
        location: step.heading,
        message: `All stages Final but Step Status = "${step.rawStatus}" (expected Final) [R4 advisory]`,
        rule: "R4",
      });
    }

    // R2-step — tasks filed in any stage but step is still Draft
    const stepHasFiled = step.stages.some((s) => s.taskIds.length > 0);
    if (
      stepHasFiled &&
      (stepNorm === "Draft" || stepNorm === "Planned" || stepNorm === "Unknown")
    ) {
      drifts.push({
        plan: planName,
        location: step.heading,
        message: `Stage(s) have filed tasks but Step Status = "${step.rawStatus}" (expected In Progress or Final)`,
        rule: "R2",
      });
    }

    if (stepNorm !== "Final") allStepsFinal = false;
  }

  // R1 — top status Draft but tasks filed
  if (
    anyTaskFiled &&
    (topNorm === "Draft" || topNorm === "Planned" || topNorm === "Unknown")
  ) {
    drifts.push({
      plan: planName,
      location: "(top)",
      message: `Tasks filed but top Status = "${plan.topStatus}" (expected In Progress or Final)`,
      rule: "R1",
    });
  }

  // R5 — all steps Final → top should be Final
  if (allStepsFinal && plan.steps.length > 0 && topNorm !== "Final") {
    drifts.push({
      plan: planName,
      location: "(top)",
      message: `All steps Final but top Status = "${plan.topStatus}" (expected Final)`,
      rule: "R5",
    });
  }
}

// ── main ──────────────────────────────────────────────────────────────────────

function collectPlanFiles() {
  if (SINGLE_PLAN) {
    const abs = path.resolve(REPO_ROOT, SINGLE_PLAN);
    if (!fs.existsSync(abs)) {
      console.error(`validate-master-plan-status: plan not found: ${abs}`);
      process.exit(1);
    }
    return [abs];
  }
  if (!fs.existsSync(PLANS_DIR)) {
    return [];
  }
  return fs
    .readdirSync(PLANS_DIR)
    .filter((f) => f.includes("master-plan") && f.endsWith(".md"))
    .map((f) => path.join(PLANS_DIR, f));
}

function main() {
  const planFiles = collectPlanFiles();
  /** @type {DriftItem[]} */
  const drifts = [];

  for (const f of planFiles) {
    const plan = parsePlan(f);
    checkPlan(plan, drifts);
  }

  if (drifts.length === 0) {
    console.log(
      `validate-master-plan-status: OK — ${planFiles.length} plan(s) checked, 0 drift rows.`
    );
    process.exit(0);
  }

  // Group by plan
  const byPlan = new Map();
  for (const d of drifts) {
    if (!byPlan.has(d.plan)) byPlan.set(d.plan, []);
    byPlan.get(d.plan).push(d);
  }

  console.error(
    `\nvalidate-master-plan-status: ${drifts.length} drift row(s) in ${byPlan.size} plan(s):\n`
  );
  for (const [plan, items] of [...byPlan.entries()].sort()) {
    console.error(`  ${plan}`);
    for (const item of items) {
      console.error(`    [${item.rule}] ${item.location}`);
      console.error(`         ${item.message}`);
    }
  }
  console.error(
    "\nFix: run Phase 2 of TECH-397 to patch drifting plan headers; for [R6] finish `/ship-stage` Pass 2 or `/closeout`; use `stage-file`/`project-stage-close` for other lifecycle drift."
  );

  // R4 is always advisory (step-level rollup hint) — never causes exit 1.
  const hardDrifts = drifts.filter((d) => d.rule !== "R4");

  if (ADVISORY) {
    console.error("\n(advisory mode: exit 0)");
    process.exit(0);
  }

  if (hardDrifts.length === 0) {
    console.error("\n(only R4 advisory drifts remain — exit 0)");
    process.exit(0);
  }
  process.exit(1);
}

main();
