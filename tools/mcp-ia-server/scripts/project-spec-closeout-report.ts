#!/usr/bin/env npx tsx
/**
 * Emit a closeout worksheet (Markdown or JSON) from a Task spec body in
 * `ia_task_specs.body_md` (DB-backed).
 * Usage: npx tsx scripts/project-spec-closeout-report.ts --issue FEAT-49 [--json]
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import {
  buildProjectSpecCloseoutDigest,
  normalizeIssueId,
} from "../src/parser/project-spec-closeout-parse.js";
import { queryTaskBody } from "../src/ia-db/queries.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
loadRepoDotenvIfNotCi(resolveRepoRoot());
if (!process.env.REPO_ROOT) {
  process.env.REPO_ROOT = path.resolve(__dirname, "../../..");
}

function parseArgs(argv: string[]): {
  issue_id?: string;
  json: boolean;
} {
  let issue_id: string | undefined;
  let json = false;
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--json") json = true;
    else if (a === "--issue" && argv[i + 1]) issue_id = argv[++i];
  }
  return { issue_id, json };
}

function worksheetMarkdown(d: ReturnType<typeof buildProjectSpecCloseoutDigest>): string {
  const lines: string[] = [];
  lines.push(`# Closeout worksheet — ${d.issue_id ?? "unknown"}`);
  lines.push("");
  lines.push(`- **Cited issue ids:** ${d.cited_issue_ids.join(", ") || "(none)"}`);
  lines.push(
    `- **Suggested \`glossary_discover\` keywords (English):** ${JSON.stringify(d.suggested_english_keywords)}`,
  );
  lines.push("");
  lines.push("## IA persistence hints (heuristic — verify against project-spec-close G1–I1)");
  lines.push("");
  const hints = d.checklist_hints;
  if (hints) {
    for (const k of ["G1", "R1", "A1", "U1", "D1", "M1", "I1"] as const) {
      const arr = hints[k];
      lines.push(`### ${k}`);
      if (arr?.length) {
        for (const b of arr) lines.push(`- ${b}`);
      } else {
        lines.push("- _(none extracted)_");
      }
      lines.push("");
    }
  }
  lines.push("## Section bodies (trimmed previews)");
  lines.push("");
  for (const [key, body] of Object.entries(d.sections)) {
    const preview = (body ?? "").slice(0, 400).replace(/\s+/g, " ").trim();
    lines.push(`### ${key}`);
    lines.push(preview ? `${preview}${(body?.length ?? 0) > 400 ? "…" : ""}` : "_(empty)_");
    lines.push("");
  }
  return lines.join("\n");
}

async function main(): Promise<void> {
  const { issue_id, json } = parseArgs(process.argv.slice(2));
  if (!issue_id) {
    console.error("Usage: project-spec-closeout-report.ts --issue FEAT-49 [--json]");
    process.exit(1);
  }

  const issueId = normalizeIssueId(issue_id);
  const markdown = await queryTaskBody(issueId);
  if (markdown == null) {
    console.error(`No task spec body for '${issueId}' in ia_task_specs.`);
    process.exit(1);
  }

  const digest = buildProjectSpecCloseoutDigest(markdown, issueId);

  if (json) {
    console.log(JSON.stringify(digest, null, 2));
  } else {
    console.log(worksheetMarkdown(digest));
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
