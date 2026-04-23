/**
 * MCP tool: mechanicalization_preflight_lint
 * Input: { artifact_path: string, artifact_kind: ArtifactKind }
 * Output: { pass: boolean, score: MechanicalizationScore, findings: Finding[] }
 * Runs 5 field checks per ia/rules/mechanicalization-contract.md.
 */

import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

type ScoreValue = "ok" | "partial" | "insufficient";
type Overall = "fully_mechanical" | "partial" | "insufficient";

interface MechanicalizationScore {
  anchors: ScoreValue;
  picks: ScoreValue;
  invariants: ScoreValue;
  validators: ScoreValue;
  escalation_enum: ScoreValue;
  overall: Overall;
}

interface Finding {
  field: string;
  anchor_path?: string;
  reason: string;
}

const ARTIFACT_KINDS = [
  "plan_digest",
  "stage_file_plan",
  "stage_closeout_plan",
  "code_fix_plan",
  "plan_fix",
] as const;

const inputShape = {
  artifact_path: z.string().min(1).describe("Path to the pair-head artifact (relative to repo root or absolute)."),
  artifact_kind: z.enum(ARTIFACT_KINDS).describe("Kind of pair-head artifact being linted."),
};

function worstScore(scores: ScoreValue[]): ScoreValue {
  if (scores.includes("insufficient")) return "insufficient";
  if (scores.includes("partial")) return "partial";
  return "ok";
}

function computeOverall(score: Omit<MechanicalizationScore, "overall">): Overall {
  const worst = worstScore([
    score.anchors,
    score.picks,
    score.invariants,
    score.validators,
    score.escalation_enum,
  ]);
  if (worst === "ok") return "fully_mechanical";
  return worst as Overall;
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

export function registerMechanicalizationPreflightLint(server: McpServer): void {
  server.registerTool(
    "mechanicalization_preflight_lint",
    {
      description:
        "Compute mechanicalization_score for a pair-head artifact. Returns {pass, score, findings}.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("mechanicalization_preflight_lint", async () => {
        const envelope = await wrapTool(
          async (input: { artifact_path: string; artifact_kind: (typeof ARTIFACT_KINDS)[number] }) => {
            const root = resolveRepoRoot();
            const absPath = path.isAbsolute(input.artifact_path)
              ? input.artifact_path
              : path.resolve(root, input.artifact_path);

            if (!fs.existsSync(absPath)) {
              return {
                pass: false,
                score: {
                  anchors: "insufficient" as ScoreValue,
                  picks: "insufficient" as ScoreValue,
                  invariants: "insufficient" as ScoreValue,
                  validators: "insufficient" as ScoreValue,
                  escalation_enum: "insufficient" as ScoreValue,
                  overall: "insufficient" as Overall,
                },
                findings: [{ field: "artifact_path", reason: `file not found: ${absPath}` }],
              };
            }

            const content = fs.readFileSync(absPath, "utf8");
            const lines = content.split("\n");
            const findings: Finding[] = [];

            // --- anchors: check before_string / anchor fields exist ---
            const anchorMatches = content.match(/\bbefore_string\b|\banchor\b/g) ?? [];
            const hasAnchors = anchorMatches.length > 0;
            const anchorsScore: ScoreValue = hasAnchors ? "ok" : "partial";
            if (!hasAnchors) {
              findings.push({ field: "anchors", reason: "no anchor/before_string fields found in artifact" });
            }

            // --- picks: check file paths referenced exist ---
            const pathRx = /(?:file[_\s]?path|target[_\s]?file|pick)[:\s]+([A-Za-z0-9_./-]+)/gi;
            let m: RegExpExecArray | null;
            let totalPaths = 0;
            let missingPaths = 0;
            pathRx.lastIndex = 0;
            while ((m = pathRx.exec(content)) !== null) {
              const p = m[1]!.trim();
              if (!p || p.startsWith("$") || p.includes("{")) continue;
              totalPaths++;
              const abs = path.resolve(root, p);
              if (!fs.existsSync(abs)) {
                missingPaths++;
                findings.push({ field: "picks", anchor_path: p, reason: `path not found: ${p}` });
              }
            }
            const picksScore: ScoreValue =
              totalPaths === 0 ? "partial" : missingPaths === 0 ? "ok" : "insufficient";
            if (totalPaths === 0) {
              findings.push({ field: "picks", reason: "no file_path/target_file picks found in artifact" });
            }

            // --- invariants: check invariant_touchpoints present ---
            const hasInvariantTouchpoints = /invariant_touchpoints/.test(content);
            const hasCsharpOrRuntime = /Assets\/|\.cs\b|runtime/.test(content);
            let invariantsScore: ScoreValue;
            if (!hasCsharpOrRuntime) {
              invariantsScore = "ok"; // vacuously ok
            } else if (hasInvariantTouchpoints) {
              invariantsScore = "ok";
            } else {
              invariantsScore = "insufficient";
              findings.push({ field: "invariants", reason: "C#/runtime steps present but no invariant_touchpoints field found" });
            }

            // --- validators: check validator_gate present ---
            const validatorGateCount = (content.match(/validator_gate/g) ?? []).length;
            // count tuple blocks (approximate by counting step/id markers)
            const tupleCount = Math.max(
              (content.match(/^#{2,3}\s+\d+\.|^- id:/gm) ?? []).length,
              1
            );
            let validatorsScore: ScoreValue;
            if (validatorGateCount === 0) {
              validatorsScore = "insufficient";
              findings.push({ field: "validators", reason: "no validator_gate fields found" });
            } else if (validatorGateCount >= tupleCount) {
              validatorsScore = "ok";
            } else {
              validatorsScore = "partial";
              findings.push({ field: "validators", reason: `${validatorGateCount} validator_gate found for ~${tupleCount} tuples` });
            }

            // --- escalation_enum: check escalation table present ---
            const hasEscalationEnum = /escalation_enum|ESCALAT/i.test(content);
            const escalationScore: ScoreValue = hasEscalationEnum ? "ok" : "partial";
            if (!hasEscalationEnum) {
              findings.push({ field: "escalation_enum", reason: "no escalation_enum table found in artifact" });
            }

            const scoreFields = {
              anchors: anchorsScore,
              picks: picksScore,
              invariants: invariantsScore,
              validators: validatorsScore,
              escalation_enum: escalationScore,
            };
            const overall = computeOverall(scoreFields);
            const score: MechanicalizationScore = { ...scoreFields, overall };
            const pass = overall === "fully_mechanical";

            return { pass, score, findings };
          },
        )(args as { artifact_path: string; artifact_kind: (typeof ARTIFACT_KINDS)[number] });
        return jsonResult(envelope);
      }),
  );
}
