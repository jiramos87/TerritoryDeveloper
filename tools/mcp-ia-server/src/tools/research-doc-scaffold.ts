/**
 * MCP tool: research_doc_scaffold — write the canonical 4-section research doc
 * skeleton at OUTPUT_PATH. Headings only; phases of the topic-research-survey
 * skill fill in the body.
 *
 * Sections (order is the invariant):
 *   1. Findings (pure external)
 *   2. Audit — current implementation in repo (pure internal)
 *   3. Critique — strengths and weaknesses (audit-only basis)
 *   4. Exploration — N ways to improve (Findings × Critique)
 *
 * Idempotent on `--overwrite=false`: errors out if file exists. Idempotent on
 * `--overwrite=true`: replaces file unconditionally.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  topic: z
    .string()
    .min(1)
    .describe("Subject phrase (e.g. 'unity ui-as-code'). Becomes the H1 title."),
  output_path: z
    .string()
    .min(1)
    .describe(
      "Repo-relative output path (e.g. 'docs/research/unity-ui-as-code.md').",
    ),
  as_of: z
    .string()
    .regex(/^\d{4}-\d{2}$/)
    .describe("Recency anchor YYYY-MM (e.g. '2026-05')."),
  audit_scope: z
    .string()
    .min(1)
    .describe(
      "Plain-language description of the repo subsystem to audit (e.g. 'ui-as-code system').",
    ),
  n_improvements: z
    .number()
    .int()
    .min(1)
    .max(50)
    .default(10)
    .describe("Count of numbered improvement proposals in §Exploration."),
  overwrite: z
    .boolean()
    .default(false)
    .describe("If true, replaces an existing file. Default false (error on exists)."),
});

type Input = z.infer<typeof inputSchema>;

interface ResearchDocScaffoldResult {
  output_path: string;
  bytes_written: number;
  sections: string[];
  improvement_placeholders: number;
  overwritten: boolean;
}

function buildSkeleton(input: Input): string {
  const { topic, as_of, n_improvements } = input;
  const proposals: string[] = [];
  for (let i = 1; i <= n_improvements; i++) {
    proposals.push(`${i}. **{Methodology name}.** {target weakness anchor}. {mechanical sketch}. Source: §Findings · {subsection}.`);
  }
  return [
    `# ${topic} — research, audit, critique, improvement (as of ${as_of})`,
    ``,
    `## Findings`,
    ``,
    `<!-- Pure external. No repo references. Each subsection = methodology / library / pattern surfaced by web research. Append citation bullets. -->`,
    ``,
    `### Cross-cutting observations`,
    ``,
    `- Dominant:`,
    `- Emerging:`,
    `- Declining:`,
    `- Recency anchor: ${as_of}`,
    ``,
    `## Audit — current implementation in repo`,
    ``,
    `<!-- Pure repo. No external methodology references. Scope: ${input.audit_scope}. -->`,
    ``,
    `### Entry points`,
    ``,
    `### Data flow`,
    ``,
    `### Constraints`,
    ``,
    `### Coverage`,
    ``,
    `## Critique — strengths and weaknesses`,
    ``,
    `<!-- Reasoned from §Audit alone. -->`,
    ``,
    `### Strengths`,
    ``,
    `### Weaknesses`,
    ``,
    `## Exploration — ${n_improvements} ways to improve`,
    ``,
    `<!-- Cross §Findings × §Critique. Each entry must name a methodology from §Findings + cite the source. -->`,
    ``,
    ...proposals,
    ``,
    `### Conflicts with locked decisions`,
    ``,
    `<!-- Optional. Populate via arch_decision_conflict_scan. -->`,
    ``,
  ].join("\n");
}

export async function runResearchDocScaffold(
  input: Input,
): Promise<ResearchDocScaffoldResult> {
  const repoRoot = resolveRepoRoot();
  const absPath = path.isAbsolute(input.output_path)
    ? input.output_path
    : path.join(repoRoot, input.output_path);

  const exists = fs.existsSync(absPath);
  if (exists && !input.overwrite) {
    throw {
      code: "invalid_input" as const,
      message: `File already exists: ${input.output_path}`,
      hint: "Pass overwrite=true to replace, or choose a new output_path.",
    };
  }

  const dir = path.dirname(absPath);
  fs.mkdirSync(dir, { recursive: true });

  const body = buildSkeleton(input);
  fs.writeFileSync(absPath, body, "utf8");

  return {
    output_path: path.relative(repoRoot, absPath).split(path.sep).join("/"),
    bytes_written: Buffer.byteLength(body, "utf8"),
    sections: ["Findings", "Audit", "Critique", "Exploration"],
    improvement_placeholders: input.n_improvements,
    overwritten: exists,
  };
}

export function registerResearchDocScaffold(server: McpServer): void {
  server.registerTool(
    "research_doc_scaffold",
    {
      description:
        "Write the canonical 4-section research doc skeleton (Findings · Audit · Critique · Exploration) at `output_path`. Headings + placeholders only; the topic-research-survey skill fills the body across its 6 phases. Idempotent only when `overwrite=true`.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("research_doc_scaffold", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const parsed = inputSchema.safeParse(input ?? {});
          if (!parsed.success) {
            throw {
              code: "invalid_input" as const,
              message: parsed.error.issues.map((i) => i.message).join("; "),
            };
          }
          return await runResearchDocScaffold(parsed.data);
        })(args as Input);
        return jsonResult(envelope);
      }),
  );
}
