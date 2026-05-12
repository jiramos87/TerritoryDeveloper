/**
 * MCP tool: research_doc_to_exploration_seed — promote a research doc
 * (`docs/research/{slug}.md`, emitted by topic-research-survey) into an
 * exploration doc (`docs/explorations/{slug}.md`) with the lean YAML
 * frontmatter shape expected by the `design-explore` Phase 4 emitter.
 *
 * Seed contract: ONE stage row (`stage_id: "1"`, status `pending`) + one task
 * row per numbered proposal in the research doc's §Exploration section.
 * `design-explore --resume {slug}` will then expand stages + tasks during its
 * grilling phases.
 *
 * Read input → write output. Idempotent only when `--overwrite=true`.
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
  research_path: z
    .string()
    .min(1)
    .describe("Repo-relative input path (e.g. 'docs/research/ui-as-code.md')."),
  exploration_path: z
    .string()
    .min(1)
    .optional()
    .describe(
      "Repo-relative output path. Defaults to docs/explorations/{slug}.md derived from research filename.",
    ),
  slug: z
    .string()
    .min(1)
    .optional()
    .describe(
      "Override slug. Defaults to research filename stem (e.g. 'ui-as-code').",
    ),
  parent_plan_slug: z
    .string()
    .nullable()
    .optional()
    .describe(
      "Parent plan slug for version-bump explorations. Default null.",
    ),
  target_version: z
    .number()
    .int()
    .min(1)
    .default(1)
    .describe("Target master-plan version. Default 1 (fresh plan)."),
  task_prefix: z
    .enum(["TECH", "FEAT", "BUG", "ART", "AUDIO"])
    .default("TECH")
    .describe("BACKLOG id prefix for seeded task rows."),
  task_kind: z
    .enum(["implementation", "refactor", "test", "doc", "spike"])
    .default("implementation")
    .describe("Task kind enum for seeded task rows."),
  overwrite: z
    .boolean()
    .default(false)
    .describe("If true, replaces existing exploration doc. Default false."),
});

type Input = z.infer<typeof inputSchema>;

interface SeedResult {
  research_path: string;
  exploration_path: string;
  slug: string;
  proposals_seeded: number;
  bytes_written: number;
  overwritten: boolean;
}

const PROPOSAL_RE = /^(\d+)\.\s+(.+)$/;
const EXPLORATION_HEAD_RE = /^##\s+Exploration/i;

function extractProposals(body: string): string[] {
  const lines = body.split(/\r?\n/);
  let inExploration = false;
  const proposals: string[] = [];
  for (const line of lines) {
    if (EXPLORATION_HEAD_RE.test(line)) {
      inExploration = true;
      continue;
    }
    if (inExploration && /^##\s+\S/.test(line)) break;
    if (!inExploration) continue;
    const m = PROPOSAL_RE.exec(line);
    if (m) proposals.push(m[2]!.trim());
  }
  return proposals;
}

function digestOutline(proposalText: string): string {
  // Strip bold tokens + trailing 'Source:' / '§Findings · ...' clauses to
  // produce a one-line digest suitable for `tasks[].digest_outline`.
  let s = proposalText.replace(/\*\*([^*]+)\*\*/g, "$1");
  s = s.replace(/\s*Source\s*:.*$/i, "");
  s = s.replace(/\s*\(§Findings[^)]*\)/g, "");
  s = s.replace(/\s+/g, " ").trim();
  // Cap length for the lean YAML row.
  if (s.length > 160) s = s.slice(0, 157) + "...";
  return s;
}

function buildSeedDoc(
  input: Input,
  proposals: string[],
  researchBody: string,
): string {
  const slug = input.slug!;
  const stageTitle = `${slug} — kickoff`;
  const yamlLines: string[] = [
    "---",
    `slug: ${slug}`,
    `parent_plan_slug: ${input.parent_plan_slug ?? "null"}`,
    `target_version: ${input.target_version}`,
    `stages:`,
    `  - stage_id: "1"`,
    `    title: "${stageTitle.replace(/"/g, '\\"')}"`,
    `    status: pending`,
    `tasks:`,
  ];
  for (const p of proposals) {
    const digest = digestOutline(p).replace(/"/g, '\\"');
    yamlLines.push(`  - prefix: ${input.task_prefix}`);
    yamlLines.push(`    depends_on: []`);
    yamlLines.push(`    digest_outline: "${digest}"`);
    yamlLines.push(`    touched_paths: []`);
    yamlLines.push(`    kind: ${input.task_kind}`);
  }
  yamlLines.push("---");
  yamlLines.push("");
  const note =
    `<!-- Seeded from research doc. Run \`design-explore --resume ${slug}\` to expand stages + tasks. -->`;
  return [yamlLines.join("\n"), note, "", researchBody].join("\n");
}

export async function runResearchDocToExplorationSeed(
  input: Input,
): Promise<SeedResult> {
  const repoRoot = resolveRepoRoot();
  const researchAbs = path.isAbsolute(input.research_path)
    ? input.research_path
    : path.join(repoRoot, input.research_path);

  if (!fs.existsSync(researchAbs)) {
    throw {
      code: "invalid_input" as const,
      message: `Research doc not found: ${input.research_path}`,
    };
  }

  const slug =
    input.slug ?? path.basename(researchAbs).replace(/\.md$/i, "");
  const explorationAbs = path.isAbsolute(input.exploration_path ?? "")
    ? (input.exploration_path as string)
    : path.join(
        repoRoot,
        input.exploration_path ?? `docs/explorations/${slug}.md`,
      );

  const exists = fs.existsSync(explorationAbs);
  if (exists && !input.overwrite) {
    throw {
      code: "invalid_input" as const,
      message: `Exploration doc already exists: ${path.relative(repoRoot, explorationAbs)}`,
      hint: "Pass overwrite=true to replace, or choose a new exploration_path.",
    };
  }

  const researchBody = fs.readFileSync(researchAbs, "utf8");
  const proposals = extractProposals(researchBody);

  const normalizedInput: Input = { ...input, slug };
  const body = buildSeedDoc(normalizedInput, proposals, researchBody);

  fs.mkdirSync(path.dirname(explorationAbs), { recursive: true });
  fs.writeFileSync(explorationAbs, body, "utf8");

  return {
    research_path: path
      .relative(repoRoot, researchAbs)
      .split(path.sep)
      .join("/"),
    exploration_path: path
      .relative(repoRoot, explorationAbs)
      .split(path.sep)
      .join("/"),
    slug,
    proposals_seeded: proposals.length,
    bytes_written: Buffer.byteLength(body, "utf8"),
    overwritten: exists,
  };
}

export function registerResearchDocToExplorationSeed(server: McpServer): void {
  server.registerTool(
    "research_doc_to_exploration_seed",
    {
      description:
        "Promote a research doc (`docs/research/{slug}.md`) into an exploration doc (`docs/explorations/{slug}.md`) with the lean YAML frontmatter shape expected by `design-explore` Phase 4. One stage_id='1' pending row + one task row per numbered proposal in §Exploration (digest_outline = bold-stripped proposal text, ≤160 chars). Caller then runs `design-explore --resume {slug}` to expand.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("research_doc_to_exploration_seed", async () => {
        const envelope = await wrapTool(async (input: unknown) => {
          const parsed = inputSchema.safeParse(input ?? {});
          if (!parsed.success) {
            throw {
              code: "invalid_input" as const,
              message: parsed.error.issues
                .map((i) => `${i.path.join(".")}: ${i.message}`)
                .join("; "),
            };
          }
          return await runResearchDocToExplorationSeed(parsed.data);
        })(args);
        return jsonResult(envelope);
      }),
  );
}
