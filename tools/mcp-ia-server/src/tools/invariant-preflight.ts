/**
 * MCP tool: invariant_preflight — composite tool bundling invariants + router + spec sections for an issue.
 */

import { z } from "zod";
import fs from "node:fs";
import matter from "gray-matter";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { splitLines } from "../parser/markdown-parser.js";
import { resolveRepoRoot, findEntryByKey } from "../config.js";
import { parseBacklogIssue } from "../parser/backlog-parser.js";
import { parseInvariantsBody } from "./invariants-summary.js";
import {
  collectRouterData,
  inferDomainHintsFromPath,
} from "./router-for-task.js";
import { runSpecSectionExtract } from "./spec-section.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const INVARIANT_PREFLIGHT_MAX_CHARS_DEFAULT = 800;
const INVARIANT_PREFLIGHT_MAX_SECTIONS_DEFAULT = 6;

const inputShape = {
  issue_id: z
    .string()
    .describe(
      "Backlog issue id (e.g. FEAT-52, BUG-12). Pulls issue data, infers domains from Files/Notes, then bundles invariants + router matches + relevant spec sections.",
    ),
};

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

/**
 * Register the invariant_preflight tool.
 */
export function registerInvariantPreflight(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "invariant_preflight",
    {
      description:
        "Composite context tool: given an issue_id, returns invariants + router matches + relevant spec sections in a single call. Replaces the manual chain of invariants_summary → router_for_task → spec_section.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("invariant_preflight", async () => {
        const envelope = await wrapTool(
          async (input: { issue_id?: string }) => {
            const issueId = (input?.issue_id ?? "").trim();
            if (!issueId) {
              throw {
                code: "invalid_input" as const,
                message: "issue_id is required.",
              };
            }

            const repoRoot = resolveRepoRoot();

            // 1. Parse backlog issue
            const issue = parseBacklogIssue(repoRoot, issueId);
            if (!issue) {
              throw {
                code: "issue_not_found" as const,
                message: `No issue '${issueId}' in BACKLOG.md or BACKLOG-ARCHIVE.md.`,
              };
            }

            // 2. Invariants
            const invariantsEntry = findEntryByKey(registry, "invariants");
            let invariants: { invariants: string[]; guardrails: string[] } = {
              invariants: [],
              guardrails: [],
            };
            if (invariantsEntry) {
              const raw = fs.readFileSync(invariantsEntry.filePath, "utf8");
              const { content } = matter(raw);
              invariants = parseInvariantsBody(content);
            }

            // 3. Infer domains from Files and Notes
            const filesPaths = (issue.files ?? "")
              .split(/[,;`]/)
              .map((s) => s.trim())
              .filter((s) => s.length > 0);
            const domainHints = new Set<string>();
            for (const fp of filesPaths) {
              for (const h of inferDomainHintsFromPath(fp)) {
                domainHints.add(h);
              }
            }

            // 4. Router matches
            const routerEntry = findEntryByKey(registry, "agent-router");
            let routerMatches: Array<{
              taskDomain: string;
              specToRead: string;
              keySections: string;
            }> = [];

            if (routerEntry) {
              const raw = fs.readFileSync(routerEntry.filePath, "utf8");
              const { content } = matter(raw);
              const bodyLines = splitLines(content);
              const { matchesForDomain } = collectRouterData(bodyLines);

              for (const hint of domainHints) {
                routerMatches.push(...matchesForDomain(hint));
              }

              // Deduplicate
              const seen = new Set<string>();
              routerMatches = routerMatches.filter((r) => {
                const k = `${r.taskDomain}\0${r.specToRead}\0${r.keySections}`;
                if (seen.has(k)) return false;
                seen.add(k);
                return true;
              });
            }

            // 5. Spec sections from router matches (limited)
            // Per-call env read so tests can mutate process.env (Phase 3, TECH-401).
            const maxSectionsEnv = Number(
              process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS ?? INVARIANT_PREFLIGHT_MAX_SECTIONS_DEFAULT,
            );
            const maxCharsEnv = Number(
              process.env.INVARIANT_PREFLIGHT_MAX_CHARS ?? INVARIANT_PREFLIGHT_MAX_CHARS_DEFAULT,
            );
            const maxSections = Number.isFinite(maxSectionsEnv)
              ? maxSectionsEnv
              : INVARIANT_PREFLIGHT_MAX_SECTIONS_DEFAULT;
            const maxChars = Number.isFinite(maxCharsEnv)
              ? maxCharsEnv
              : INVARIANT_PREFLIGHT_MAX_CHARS_DEFAULT;

            const specSections: Array<Record<string, unknown>> = [];
            const fetchedSections = new Set<string>();

            for (const match of routerMatches) {
              if (specSections.length >= maxSections) break;

              // Extract spec key from specToRead (e.g. "roads-system.md" -> "roads-system")
              const specRef = match.specToRead
                .replace(/\.md.*$/, "")
                .replace(/^ia\/specs\//, "")
                .trim();
              if (!specRef) continue;

              // Extract section hints from keySections
              const sectionHints = match.keySections
                .split(/[,;]/)
                .map((s) => s.trim().replace(/^§\s*/, ""))
                .filter((s) => s.length > 0);

              for (const section of sectionHints) {
                if (specSections.length >= maxSections) break;
                const key = `${specRef}:${section}`;
                if (fetchedSections.has(key)) continue;
                fetchedSections.add(key);

                try {
                  const result = runSpecSectionExtract(
                    registry,
                    specRef,
                    section,
                    maxChars,
                  );
                  if (!("error" in result)) {
                    specSections.push(result as unknown as Record<string, unknown>);
                  }
                } catch {
                  // Silently skip sections that fail to extract
                }
              }
            }

            return {
              issue: {
                issue_id: issue.issue_id,
                title: issue.title,
                status: issue.status,
                type: issue.type ?? null,
                files: issue.files ?? null,
                section: issue.backlog_section,
              },
              invariants,
              router: {
                domain_hints: [...domainHints],
                matches: routerMatches,
              },
              spec_sections: specSections,
            };
          },
        )(args as { issue_id?: string });

        return jsonResult(envelope);
      }),
  );
}
