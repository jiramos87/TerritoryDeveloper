/**
 * MCP tool: improvement_proposal_lint — validate each numbered improvement in
 * the §Exploration section of a research doc.
 *
 * Each numbered entry must contain four signals to be considered well-formed:
 *   1. methodology_name      — bold token (e.g. **UI Toolkit (UXML+USS)**).
 *   2. target_weakness       — explicit anchor reference (e.g.
 *                              "§Critique · Weaknesses · <something>" OR a
 *                              `Weakness:` label, OR an inline citation to a
 *                              named weakness from §Critique).
 *   3. mechanical_sketch     — at least one full sentence after the bold token
 *                              ending in `.` and containing >= MIN_SKETCH_WORDS
 *                              tokens (default 6).
 *   4. source_link           — either a `Source:` label OR a parenthesised
 *                              `§Findings · …` reference OR an inline URL.
 *
 * Read-only. Filesystem only — reads the doc at `output_path`.
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
  output_path: z
    .string()
    .min(1)
    .describe("Repo-relative path to the research doc."),
  expected_count: z
    .number()
    .int()
    .min(1)
    .max(50)
    .optional()
    .describe("If set, lint fails when count of numbered proposals differs."),
  min_sketch_words: z
    .number()
    .int()
    .min(3)
    .max(50)
    .default(6)
    .describe("Minimum word count for the mechanical_sketch sentence."),
});

type Input = z.infer<typeof inputSchema>;

interface ProposalLintRow {
  index: number;
  raw: string;
  methodology_name: boolean;
  target_weakness: boolean;
  mechanical_sketch: boolean;
  source_link: boolean;
  missing: string[];
}

interface LintResult {
  output_path: string;
  exploration_found: boolean;
  proposals_found: number;
  expected_count: number | null;
  proposals: ProposalLintRow[];
  ok: boolean;
  errors: string[];
}

const PROPOSAL_RE = /^(\d+)\.\s+(.+)$/;
const EXPLORATION_HEAD_RE = /^##\s+Exploration/i;
const SOURCE_LINK_LABELS = [
  /\bSource\s*:/i,
  /§Findings\s*·/, // canonical "§Findings · subsection"
  /https?:\/\//,
];
const TARGET_LABELS = [
  /§Critique\s*·/, // "§Critique · Weaknesses · ..."
  /\bWeakness\s*:/i,
  /\baddresses\b/i,
  /\btargets\b/i,
];

function readProposals(absPath: string): {
  exploration_found: boolean;
  raw_lines: { lineno: number; text: string }[];
} {
  const body = fs.readFileSync(absPath, "utf8");
  const lines = body.split(/\r?\n/);
  let inExploration = false;
  const out: { lineno: number; text: string }[] = [];
  let foundHeader = false;
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    if (EXPLORATION_HEAD_RE.test(line)) {
      inExploration = true;
      foundHeader = true;
      continue;
    }
    if (inExploration && /^##\s+\S/.test(line)) {
      // Next H2 — exit.
      break;
    }
    if (!inExploration) continue;
    if (PROPOSAL_RE.test(line)) {
      out.push({ lineno: i + 1, text: line });
    }
  }
  return { exploration_found: foundHeader, raw_lines: out };
}

function lintProposal(
  index: number,
  raw: string,
  minSketchWords: number,
): ProposalLintRow {
  const stripped = raw.replace(PROPOSAL_RE, "$2").trim();

  const methodology_name = /\*\*[^*]+\*\*/.test(stripped);

  const target_weakness = TARGET_LABELS.some((re) => re.test(stripped));

  const source_link = SOURCE_LINK_LABELS.some((re) => re.test(stripped));

  // Mechanical sketch: pick the first full sentence after the bold methodology
  // token. Fallback to the whole stripped string. Sentence = ends in `.`.
  let sketchPortion = stripped;
  const boldEnd = stripped.indexOf("**", stripped.indexOf("**") + 2);
  if (boldEnd !== -1) sketchPortion = stripped.slice(boldEnd + 2);
  const firstSentence =
    sketchPortion.match(/^[^.!?]+[.!?]/)?.[0] ?? sketchPortion;
  const wordCount = firstSentence.trim().split(/\s+/).filter(Boolean).length;
  const mechanical_sketch =
    wordCount >= minSketchWords && /[.!?]\s*$/.test(firstSentence.trim());

  const missing: string[] = [];
  if (!methodology_name) missing.push("methodology_name");
  if (!target_weakness) missing.push("target_weakness");
  if (!mechanical_sketch) missing.push("mechanical_sketch");
  if (!source_link) missing.push("source_link");

  return {
    index,
    raw,
    methodology_name,
    target_weakness,
    mechanical_sketch,
    source_link,
    missing,
  };
}

export async function runImprovementProposalLint(
  input: Input,
): Promise<LintResult> {
  const repoRoot = resolveRepoRoot();
  const absPath = path.isAbsolute(input.output_path)
    ? input.output_path
    : path.join(repoRoot, input.output_path);

  if (!fs.existsSync(absPath)) {
    throw {
      code: "invalid_input" as const,
      message: `File not found: ${input.output_path}`,
    };
  }

  const { exploration_found, raw_lines } = readProposals(absPath);
  const proposals: ProposalLintRow[] = raw_lines.map((row, idx) =>
    lintProposal(idx + 1, row.text, input.min_sketch_words),
  );

  const errors: string[] = [];
  if (!exploration_found) {
    errors.push("Could not locate '## Exploration' section.");
  }
  if (
    input.expected_count !== undefined &&
    proposals.length !== input.expected_count
  ) {
    errors.push(
      `Proposal count mismatch: expected=${input.expected_count} actual=${proposals.length}.`,
    );
  }
  for (const p of proposals) {
    if (p.missing.length > 0) {
      errors.push(`#${p.index} missing: ${p.missing.join(", ")}`);
    }
  }

  return {
    output_path: path.relative(repoRoot, absPath).split(path.sep).join("/"),
    exploration_found,
    proposals_found: proposals.length,
    expected_count: input.expected_count ?? null,
    proposals,
    ok: errors.length === 0,
    errors,
  };
}

export function registerImprovementProposalLint(server: McpServer): void {
  server.registerTool(
    "improvement_proposal_lint",
    {
      description:
        "Lint the §Exploration section of a topic-research-survey doc. Each numbered proposal must contain `methodology_name` (bold token), `target_weakness` (§Critique anchor / 'Weakness:' label / 'addresses' verb), `mechanical_sketch` (≥ min_sketch_words sentence ending in `.`), and `source_link` (`Source:` label / §Findings anchor / inline URL). Returns `ok=false` with per-proposal `missing[]` when any signal absent.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("improvement_proposal_lint", async () => {
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
          return await runImprovementProposalLint(parsed.data);
        })(args);
        return jsonResult(envelope);
      }),
  );
}
