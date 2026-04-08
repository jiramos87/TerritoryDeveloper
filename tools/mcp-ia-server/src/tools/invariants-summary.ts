/**
 * MCP tool: invariants_summary — numbered invariants + bulleted guardrails from invariants.mdc.
 */

import matter from "gray-matter";
import fs from "node:fs";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { findEntryByKey } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

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

export function parseInvariantsBody(body: string): {
  invariants: string[];
  guardrails: string[];
} {
  const lines = body.split(/\r?\n/);
  const invariants: string[] = [];
  const guardrails: string[] = [];
  let mode: "none" | "inv" | "guard" = "none";

  for (const line of lines) {
    if (/^#\s+System Invariants/i.test(line.trim())) {
      mode = "inv";
      continue;
    }
    if (/^#\s+Guardrails/i.test(line.trim())) {
      mode = "guard";
      continue;
    }
    if (mode === "inv") {
      const m = line.match(/^\d+\.\s+(.+)$/);
      if (m?.[1]) invariants.push(m[1].trim());
    } else if (mode === "guard") {
      const m = line.match(/^\s*-\s+(.+)$/);
      if (m?.[1]) guardrails.push(m[1].trim());
    }
  }

  return { invariants, guardrails };
}

/**
 * Register the invariants_summary tool (no inputs).
 */
export function registerInvariantsSummary(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "invariants_summary",
    {
      description:
        "Return the full system invariants and guardrails. These must NEVER be violated when making changes.",
    },
    async () =>
      runWithToolTiming("invariants_summary", async () => {
        const entry = findEntryByKey(registry, "invariants");
        if (!entry) {
          return jsonResult({
            error: "not_found",
            message: "invariants.mdc is not registered.",
          });
        }

        const raw = fs.readFileSync(entry.filePath, "utf8");
        const { data, content } = matter(raw);
        const d = data as Record<string, unknown>;
        const description =
          typeof d.description === "string"
            ? d.description
            : "System invariants and guardrails";

        const { invariants, guardrails } = parseInvariantsBody(content);

        return jsonResult({
          description,
          invariants,
          guardrails,
        });
      }),
  );
}
