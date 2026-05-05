// Lint canonical SKILL.md frontmatter + downstream rendered files for drift.
// MVP severity policy: schema = error, drift / parity / freshness = warning.
// Phase 3 lockdown promotes warnings to errors.

import fs from "node:fs";
import path from "node:path";
import {
  type SkillFrontmatter,
  REPO_ROOT,
  collapseDescription,
  listSkillSlugs,
  readSkillFrontmatter,
  splitFrontmatter,
} from "./frontmatter.js";
import { renderAgent } from "./render-agent.js";
import { renderCommand } from "./render-command.js";
import { renderCursor } from "./render-cursor.js";
import { resolveTools, TOOL_ROLE_BASELINES } from "./tool-roles.js";

export type Severity = "error" | "warning";

export interface LintFinding {
  slug: string;
  check: string;
  severity: Severity;
  message: string;
}

export interface LintReport {
  slugs_checked: number;
  errors: number;
  warnings: number;
  findings: LintFinding[];
}

const RETIRED_SKILL_NAMES = new Set([
  "code-fix-applier",
  "plan-fix-applier",
  "stage-closeout-applier",
  "plan-reviewer",
  "stage-file-planner",
  "stage-closeout-planner",
  "stage-file-applier",
]);

export interface LintOptions {
  slugs?: string[];
  promoteWarnings?: boolean;
}

export function lint(options: LintOptions = {}): LintReport {
  const slugs = options.slugs ?? listSkillSlugs();
  const findings: LintFinding[] = [];

  for (const slug of slugs) {
    let fm: SkillFrontmatter;
    try {
      fm = readSkillFrontmatter(slug);
    } catch (err) {
      findings.push({
        slug,
        check: "frontmatter-parses",
        severity: "error",
        message: (err as Error).message,
      });
      continue;
    }

    runChecks(slug, fm, findings);
  }

  const adjusted = options.promoteWarnings
    ? findings.map((f) => ({ ...f, severity: "error" as Severity }))
    : findings;

  const errors = adjusted.filter((f) => f.severity === "error").length;
  const warnings = adjusted.filter((f) => f.severity === "warning").length;

  return {
    slugs_checked: slugs.length,
    errors,
    warnings,
    findings: adjusted,
  };
}

function runChecks(slug: string, fm: SkillFrontmatter, findings: LintFinding[]): void {
  const agentPath = path.join(REPO_ROOT, ".claude", "agents", `${fm.name}.md`);
  const commandPath = path.join(REPO_ROOT, ".claude", "commands", `${fm.name}.md`);
  const cursorPath = path.join(REPO_ROOT, ".cursor", "rules", `cursor-skill-${fm.name}.mdc`);

  // Check 8: caller_agent-bearing skills SHOULD declare input_token_budget.
  // MVP severity = warning; promote to error once all caller_agent skills carry budgets (Stage 6.B lockdown).
  if (fm.caller_agent && fm.input_token_budget === undefined) {
    findings.push({
      slug,
      check: "input-token-budget-required",
      severity: "warning",
      message: `skill has caller_agent="${fm.caller_agent}" but missing input_token_budget frontmatter field`,
    });
  }

  // Surfaceless subskill: no agent file → tools list moot. Skip tools-baseline.
  const hasAgentSurface = fs.existsSync(agentPath);
  if (hasAgentSurface) {
    // Check 4: Tool list ⊇ baseline (warning in MVP; error after lockdown)
    const baseline = TOOL_ROLE_BASELINES[fm.tools_role] ?? [];
    const resolved = new Set(resolveTools(fm.tools_role, fm.tools_extra));
    const missing = baseline.filter((t) => !resolved.has(t));
    if (missing.length > 0) {
      findings.push({
        slug,
        check: "tools-baseline",
        severity: "warning",
        message: `tools_role=${fm.tools_role} missing baseline: ${missing.join(", ")}`,
      });
    }
    if (fm.tools_role === "custom" && fm.tools_extra.length === 0) {
      findings.push({
        slug,
        check: "tools-baseline",
        severity: "warning",
        message: "tools_role=custom requires non-empty tools_extra (set tools_role+tools_extra to silence)",
      });
    }
  }

  // Check 2: description parity
  if (fs.existsSync(agentPath)) {
    const agentText = fs.readFileSync(agentPath, "utf8");
    const agentDesc = extractDescriptionField(agentText);
    if (agentDesc && collapseDescription(agentDesc) !== collapseDescription(fm.description)) {
      findings.push({
        slug,
        check: "description-parity",
        severity: "warning",
        message: "agent description drifts from SKILL.md description",
      });
    }
  }
  if (fs.existsSync(commandPath)) {
    const cmdText = fs.readFileSync(commandPath, "utf8");
    const cmdDesc = extractDescriptionField(cmdText);
    if (cmdDesc && collapseDescription(cmdDesc) !== collapseDescription(fm.description)) {
      findings.push({
        slug,
        check: "description-parity",
        severity: "warning",
        message: "command description drifts from SKILL.md description",
      });
    }
  }

  // Check 5: no retired skill mentions in agent / command (warning in MVP)
  for (const filePath of [agentPath, commandPath]) {
    if (!fs.existsSync(filePath)) continue;
    const text = fs.readFileSync(filePath, "utf8");
    for (const retired of RETIRED_SKILL_NAMES) {
      // Trailing (?!-) excludes hyphen continuation: `plan-reviewer` must NOT match `plan-reviewer-mechanical` (split successor, current).
      const re = new RegExp(`\\b${retired}\\b(?!-)`);
      if (re.test(text)) {
        findings.push({
          slug,
          check: "no-retired-mentions",
          severity: "warning",
          message: `${path.basename(filePath)} mentions retired skill "${retired}"`,
        });
      }
    }
  }

  // Check 7: phases match agent recipe phase count (if agent uses default body)
  if (fs.existsSync(agentPath)) {
    const agentText = fs.readFileSync(agentPath, "utf8");
    if (!agentText.includes("<!-- skill-tools:body-override -->")) {
      const numbered = agentText.match(/^[0-9]+\.\s+/gm) ?? [];
      if (numbered.length !== fm.phases.length && numbered.length > 0) {
        findings.push({
          slug,
          check: "phase-count",
          severity: "warning",
          message: `agent recipe has ${numbered.length} numbered phases, frontmatter declares ${fm.phases.length}`,
        });
      }
    }
  }

  // Check 6: render drift — what would be regenerated equals what is on disk
  checkRenderDrift(slug, agentPath, () => renderAgent(fm), "agent-render-drift", findings);
  checkRenderDrift(slug, commandPath, () => renderCommand(fm), "command-render-drift", findings);
  checkRenderDrift(slug, cursorPath, () => renderCursor(fm), "cursor-render-drift", findings, true);
}

function checkRenderDrift(
  slug: string,
  filePath: string,
  render: () => string,
  check: string,
  findings: LintFinding[],
  ignoreOverride = false
): void {
  if (!fs.existsSync(filePath)) return;
  let expected: string;
  try {
    expected = render();
  } catch (err) {
    findings.push({
      slug,
      check,
      severity: "warning",
      message: `render failed (frontmatter likely incomplete): ${(err as Error).message}`,
    });
    return;
  }
  const actual = fs.readFileSync(filePath, "utf8");
  if (!ignoreOverride && actual.includes("<!-- skill-tools:body-override -->")) return;
  if (expected !== actual) {
    findings.push({
      slug,
      check,
      severity: "warning",
      message: "rendered output differs from on-disk; run `npm run skill:sync` to refresh",
    });
  }
}

function extractDescriptionField(content: string): string | null {
  try {
    const { fmBlock } = splitFrontmatter(content);
    const match = fmBlock.match(/^description:\s*(.+(?:\n {2,}.+)*)/m);
    if (!match) return null;
    return match[1].replace(/\s+/g, " ").trim().replace(/^["']|["']$/g, "");
  } catch {
    return null;
  }
}
