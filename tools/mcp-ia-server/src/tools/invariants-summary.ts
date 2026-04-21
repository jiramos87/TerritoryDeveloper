/**
 * MCP tool: invariants_summary — numbered invariants + bulleted guardrails.
 * Merges ia/rules/invariants.md (universal IA rules 12–13 + universal safety)
 * with ia/rules/unity-invariants.md (Unity rules 1–11) so callers still see the
 * full cardinal set (13 invariants + 10 guardrails).
 * Supports optional domain filter (substring match against subsystem_tags).
 * Returns structured { description, invariants, guardrails, markdown }.
 */

import matter from "gray-matter";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { findEntryByKey } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

// ---------------------------------------------------------------------------
// Sidecar types
// ---------------------------------------------------------------------------

interface InvariantTag {
  number: number;
  subsystem_tags: string[];
}

interface GuardrailTag {
  index: number;
  subsystem_tags: string[];
}

interface InvariantsTagsSidecar {
  invariants: InvariantTag[];
  guardrails: GuardrailTag[];
}

// ---------------------------------------------------------------------------
// Structured return types
// ---------------------------------------------------------------------------

interface InvariantEntry {
  number: number;
  title: string;
  subsystem_tags: string[];
}

interface GuardrailEntry {
  index: number;
  title: string;
  subsystem_tags: string[];
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

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

function tagsMatch(tags: string[], domain: string): boolean {
  const d = domain.toLowerCase();
  return tags.some((t) => t.toLowerCase().includes(d));
}

function buildMarkdown(
  invariants: InvariantEntry[],
  guardrails: GuardrailEntry[],
): string {
  if (invariants.length === 0 && guardrails.length === 0) return "";
  const parts: string[] = [];
  if (invariants.length > 0) {
    parts.push("# System Invariants (NEVER violate)");
    for (const inv of invariants) {
      parts.push(`${inv.number}. ${inv.title}`);
    }
  }
  if (guardrails.length > 0) {
    parts.push("");
    parts.push("# Guardrails (IF → THEN)");
    for (const gr of guardrails) {
      parts.push(`- ${gr.title}`);
    }
  }
  return parts.join("\n");
}

// ---------------------------------------------------------------------------
// Register
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SIDECAR_PATH = path.resolve(
  __dirname,
  "../../data/invariants-tags.json",
);

/**
 * Load the invariants-tags sidecar. Missing / malformed file → empty sidecar
 * (non-fatal; subsystem_tags become empty arrays).
 */
export function loadInvariantsTagsSidecar(
  sidecarPath: string = SIDECAR_PATH,
): InvariantsTagsSidecar {
  try {
    const raw = fs.readFileSync(sidecarPath, "utf8");
    return JSON.parse(raw) as InvariantsTagsSidecar;
  } catch {
    return { invariants: [], guardrails: [] };
  }
}

export interface InvariantsPayload {
  description: string;
  invariants: InvariantEntry[];
  guardrails: GuardrailEntry[];
  markdown: string;
}

/**
 * Build the structured invariants payload from the live registry + sidecar.
 * Pure/testable: no MCP server dependency.
 *
 * Returns `null` when the invariants entry is not registered.
 */
export function buildInvariantsPayload(
  registry: SpecRegistryEntry[],
  domain?: string,
  sidecar?: InvariantsTagsSidecar,
): InvariantsPayload | null {
  const universalEntry = findEntryByKey(registry, "invariants");
  if (!universalEntry) return null;
  const unityEntry = findEntryByKey(registry, "unity-invariants");

  const tags = sidecar ?? loadInvariantsTagsSidecar();

  // Order: Unity rules 1–11 first, then universal rules 12–13, so positional
  // indexing (i+1) aligns with the sidecar's canonical numbering.
  const unityParsed = unityEntry
    ? parseInvariantsBody(
        matter(fs.readFileSync(unityEntry.filePath, "utf8")).content,
      )
    : { invariants: [], guardrails: [] };

  const universalRaw = fs.readFileSync(universalEntry.filePath, "utf8");
  const { data, content: universalContent } = matter(universalRaw);
  const d = data as Record<string, unknown>;
  const description =
    typeof d.description === "string"
      ? d.description
      : "System invariants and guardrails";

  const universalParsed = parseInvariantsBody(universalContent);

  const invTitles = [
    ...unityParsed.invariants,
    ...universalParsed.invariants,
  ];
  const grTitles = [
    ...unityParsed.guardrails,
    ...universalParsed.guardrails,
  ];

  const allInvariants: InvariantEntry[] = invTitles.map((title, i) => {
    const tag = tags.invariants.find((t) => t.number === i + 1);
    return {
      number: i + 1,
      title,
      subsystem_tags: tag?.subsystem_tags ?? [],
    };
  });

  const allGuardrails: GuardrailEntry[] = grTitles.map((title, i) => {
    const tag = tags.guardrails.find((t) => t.index === i);
    return {
      index: i,
      title,
      subsystem_tags: tag?.subsystem_tags ?? [],
    };
  });

  const filteredInvariants = domain
    ? allInvariants.filter((inv) => tagsMatch(inv.subsystem_tags, domain))
    : allInvariants;

  const filteredGuardrails = domain
    ? allGuardrails.filter((gr) => tagsMatch(gr.subsystem_tags, domain))
    : allGuardrails;

  const markdown = buildMarkdown(filteredInvariants, filteredGuardrails);

  return {
    description,
    invariants: filteredInvariants,
    guardrails: filteredGuardrails,
    markdown,
  };
}

/**
 * Register the invariants_summary tool.
 * Accepts optional domain filter; returns structured { description, invariants, guardrails, markdown }.
 */
export function registerInvariantsSummary(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  // Load sidecar once at register time, cache in closure.
  const sidecar = loadInvariantsTagsSidecar();

  server.registerTool(
    "invariants_summary",
    {
      description:
        "Return the full system invariants and guardrails. These must NEVER be violated when making changes.",
      inputSchema: {
        domain: z
          .string()
          .optional()
          .describe(
            "Optional subsystem filter. Substring match against subsystem_tags (case-insensitive). Omit to return all.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("invariants_summary", async () => {
        const envelope = await wrapTool(
          async ({ domain }: { domain?: string }) => {
            const payload = buildInvariantsPayload(registry, domain, sidecar);
            if (!payload) {
              throw {
                code: "spec_not_found" as const,
                message:
                  "ia/rules/invariants.md is not registered (universal rules file).",
              };
            }
            return payload;
          },
        )(args as { domain?: string });

        return jsonResult(envelope);
      }),
  );
}
