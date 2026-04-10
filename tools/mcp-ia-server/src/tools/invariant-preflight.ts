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

const PREFLIGHT_MAX_CHARS = 800;
const MAX_SPEC_SECTIONS = 6;

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
        const issueId = (args?.issue_id ?? "").trim();
        if (!issueId) {
          return jsonResult({
            error: "invalid_input",
            message: "issue_id is required.",
          });
        }

        const repoRoot = resolveRepoRoot();

        // 1. Parse backlog issue
        const issue = parseBacklogIssue(repoRoot, issueId);
        if (!issue) {
          return jsonResult({
            error: "unknown_issue",
            message: `No issue '${issueId}' in BACKLOG.md or BACKLOG-ARCHIVE.md.`,
          });
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
        const specSections: Array<Record<string, unknown>> = [];
        const fetchedSections = new Set<string>();

        for (const match of routerMatches) {
          if (specSections.length >= MAX_SPEC_SECTIONS) break;

          // Extract spec key from specToRead (e.g. "roads-system.md" -> "roads-system")
          const specRef = match.specToRead
            .replace(/\.md.*$/, "")
            .replace(/^(?:\.cursor|ia)\/specs\//, "")
            .trim();
          if (!specRef) continue;

          // Extract section hints from keySections
          const sectionHints = match.keySections
            .split(/[,;]/)
            .map((s) => s.trim().replace(/^§\s*/, ""))
            .filter((s) => s.length > 0);

          for (const section of sectionHints) {
            if (specSections.length >= MAX_SPEC_SECTIONS) break;
            const key = `${specRef}:${section}`;
            if (fetchedSections.has(key)) continue;
            fetchedSections.add(key);

            const result = runSpecSectionExtract(
              registry,
              specRef,
              section,
              PREFLIGHT_MAX_CHARS,
            );
            if (!("error" in result)) {
              specSections.push(result);
            }
          }
        }

        return jsonResult({
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
        });
      }),
  );
}
