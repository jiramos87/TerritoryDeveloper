/**
 * MCP tool: audit_scope_resolve — single-call fusion of glossary discover +
 * router-for-task + active arch_decisions over an AUDIT_SCOPE phrase. Built
 * for the topic-research-survey skill's Phase 3 (Audit) to replace 3–4
 * sequential MCP calls.
 *
 * Read-only. No mutations.
 *
 * Inputs:
 *   - scope         — plain-language phrase (e.g. "ui-as-code system").
 *   - max_glossary  — cap on glossary matches returned (default 5, hard cap 15).
 *   - max_router    — cap on router matches returned (default 5, hard cap 15).
 *
 * Output:
 *   - glossary_matches[]  — `{term, specReference, category, score}`
 *   - router_matches[]    — `{taskDomain, specToRead, keySections}`
 *   - decisions[]         — active arch_decisions whose surface_slug appears in
 *                           router_matches.specToRead OR whose `title` /
 *                           `rationale` substring-matches `scope`. DB-optional —
 *                           omitted when DB unconfigured.
 *   - hint                — next-tool suggestion line.
 */

import { z } from "zod";
import fs from "node:fs";
import matter from "gray-matter";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { findEntryByKey } from "../config.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { rankGlossaryDiscover } from "../parser/glossary-discover-rank.js";
import { collectRouterData } from "./router-for-task.js";
import { splitLines } from "../parser/markdown-parser.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runArchDecisionList } from "./arch.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  scope: z
    .string()
    .min(2)
    .describe("Plain-language scope phrase (e.g. 'ui-as-code system')."),
  max_glossary: z.number().int().min(1).max(15).default(5),
  max_router: z.number().int().min(1).max(15).default(5),
});

type Input = z.infer<typeof inputSchema>;

interface GlossaryMatch {
  term: string;
  specReference: string;
  category: string;
  score: number;
}

interface RouterMatch {
  taskDomain: string;
  specToRead: string;
  keySections: string;
}

interface DecisionMatch {
  slug: string;
  title: string;
  status: string;
  surface_slug: string | null;
  match_reason: "surface" | "title" | "rationale";
}

interface AuditScopeResult {
  scope: string;
  glossary_matches: GlossaryMatch[];
  router_matches: RouterMatch[];
  decisions: DecisionMatch[] | null;
  hint: string;
}

const HINT =
  "Use glossary_matches[].term → glossary_lookup for canonical definitions. " +
  "Use router_matches[].specToRead → spec_section / spec_sections for spec body. " +
  "Use decisions[].slug → arch_decision_get for full rationale.";

export async function runAuditScopeResolve(
  registry: SpecRegistryEntry[],
  input: Input,
): Promise<AuditScopeResult> {
  // ---- Glossary discover ------------------------------------------------
  const glossaryMatches: GlossaryMatch[] = [];
  const glossaryEntry = findEntryByKey(registry, "glossary");
  if (glossaryEntry) {
    const entries = parseGlossary(glossaryEntry.filePath);
    const ranked = rankGlossaryDiscover(entries, input.scope, {
      maxResults: input.max_glossary,
    });
    for (const r of ranked) {
      glossaryMatches.push({
        term: r.entry.term,
        specReference: r.entry.specReference,
        category: r.entry.category,
        score: Math.round(r.score * 1000) / 1000,
      });
    }
  }

  // ---- Router-for-task --------------------------------------------------
  const routerMatches: RouterMatch[] = [];
  const routerEntry = findEntryByKey(registry, "agent-router");
  if (routerEntry) {
    const raw = fs.readFileSync(routerEntry.filePath, "utf8");
    const { content } = matter(raw);
    const bodyLines = splitLines(content);
    const { matchesForDomain } = collectRouterData(bodyLines);
    const rows = matchesForDomain(input.scope).slice(0, input.max_router);
    for (const r of rows) {
      routerMatches.push({
        taskDomain: r.taskDomain,
        specToRead: r.specToRead,
        keySections: r.keySections,
      });
    }
  }

  // ---- Active arch_decisions ------------------------------------------
  let decisions: DecisionMatch[] | null = null;
  const pool = getIaDatabasePool();
  if (pool) {
    try {
      const { decisions: rows } = await runArchDecisionList(pool, {
        status: "active",
      });
      const scopeLower = input.scope.toLowerCase();
      const routerSurfaces = new Set(
        routerMatches.map((r) => r.specToRead.toLowerCase()),
      );
      const matched: DecisionMatch[] = [];
      for (const d of rows) {
        let reason: DecisionMatch["match_reason"] | null = null;
        if (
          d.surface_slug &&
          routerSurfaces.has(d.surface_slug.toLowerCase())
        ) {
          reason = "surface";
        } else if (
          d.title &&
          d.title.toLowerCase().includes(scopeLower)
        ) {
          reason = "title";
        } else if (
          d.rationale &&
          d.rationale.toLowerCase().includes(scopeLower)
        ) {
          reason = "rationale";
        }
        if (reason) {
          matched.push({
            slug: d.slug,
            title: d.title,
            status: d.status,
            surface_slug: d.surface_slug,
            match_reason: reason,
          });
        }
      }
      decisions = matched;
    } catch {
      // DB connect error → leave decisions = null, caller can still proceed.
      decisions = null;
    }
  }

  return {
    scope: input.scope,
    glossary_matches: glossaryMatches,
    router_matches: routerMatches,
    decisions,
    hint: HINT,
  };
}

export function registerAuditScopeResolve(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "audit_scope_resolve",
    {
      description:
        "Single-call fusion for repo audit scope: returns ranked glossary matches (parse glossary + rankGlossaryDiscover) + router rows (collectRouterData) + active arch_decisions touching that scope (surface link OR title/rationale substring). Read-only. Replaces 3–4 sequential MCP queries during the topic-research-survey skill Phase 3.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("audit_scope_resolve", async () => {
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
          return await runAuditScopeResolve(registry, parsed.data);
        })(args);
        return jsonResult(envelope);
      }),
  );
}
