/**
 * MCP tool: spec_outline — heading tree for a registered IA document (supports spec key aliases).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { parseDocument } from "../parser/markdown-parser.js";
import { findEntryForSpecDoc } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const specOutlineInputShape = {
  spec: z
    .string()
    .describe(
      "Key or filename of the document (e.g. 'glossary', 'geo', 'roads-system', 'invariants').",
    ),
  expand: z
    .boolean()
    .optional()
    .describe(
      "When true, return full heading tree (all depths). When false/omitted, depth-1 only.",
    ),
};

interface HeadingNode {
  depth?: number;
  level?: number;
  [key: string]: unknown;
}

function filterDepth1(headings: unknown): unknown {
  if (!Array.isArray(headings)) return headings;
  return headings.filter((h: HeadingNode) => {
    const d = h.depth ?? h.level;
    return typeof d !== "number" || d <= 1;
  });
}

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
 * Register the spec_outline tool on the given server.
 */
export function registerSpecOutline(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "spec_outline",
    {
      description:
        "Get the heading outline (table of contents) for a spec, rule, or root document. Accepts short aliases (e.g. geo, roads, sim).",
      inputSchema: specOutlineInputShape,
    },
    async (args) =>
      runWithToolTiming("spec_outline", async () => {
        const envelope = await wrapTool(
          async (input: { spec?: string; expand?: boolean }) => {
            const spec = input?.spec ?? "";
            const expand = input?.expand === true;
            const entry = findEntryForSpecDoc(registry, spec);

            if (!entry) {
              const available_keys = registry.map((e) => e.key).sort();
              throw {
                code: "spec_not_found" as const,
                message: `No document found for key '${spec}'. Use list_specs to see available documents.`,
                details: { available_keys },
              };
            }

            const doc = parseDocument(entry.filePath);
            const outline = expand ? doc.headings : filterDepth1(doc.headings);
            return {
              key: entry.key,
              fileName: entry.fileName,
              description: entry.description,
              frontmatter: doc.frontmatter,
              outline,
              expanded: expand,
            };
          },
        )(args ?? {});

        return jsonResult(envelope);
      }),
  );
}
