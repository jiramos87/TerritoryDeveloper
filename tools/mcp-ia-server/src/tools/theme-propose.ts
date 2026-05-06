/**
 * MCP tool: theme_propose — LLM theme proposer stub.
 * Returns a hardcoded 6-token palette tracer.
 * TECH-15229 — Stage 9.5 game-ui-catalog-bake.
 *
 * §Goal: stub returning ≥6 token palette entries so the tracer exit criterion is met.
 * Real LLM integration deferred to Stage N (theme-proposer-llm master plan).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";

const themeProposeInputSchema = z.object({
  mood: z
    .string()
    .optional()
    .describe("Optional mood hint (e.g. 'industrial', 'coastal', 'warm'). Ignored by the stub; reserved for real LLM integration."),
  seed: z
    .string()
    .optional()
    .describe("Optional hex seed color. Ignored by the stub; reserved for real LLM integration."),
});

/** Hardcoded 6-token tracer palette — night-city dark theme. */
const TRACER_PALETTE = [
  { slug: "ds-surface-base",      hex: "#111827", role: "Deepest chrome background" },
  { slug: "ds-surface-card",      hex: "#1c2333", role: "HUD / popup card background" },
  { slug: "ds-surface-elevated",  hex: "#283044", role: "Controls, active tool highlight" },
  { slug: "ds-text-primary",      hex: "#e8eaf6", role: "Primary readable text" },
  { slug: "ds-text-secondary",    hex: "#8b8fa8", role: "Secondary / muted text" },
  { slug: "ds-accent-primary",    hex: "#4a9eff", role: "Interactive accent / links" },
  { slug: "ds-accent-positive",   hex: "#34c759", role: "Positive feedback (income, growth)" },
  { slug: "ds-accent-negative",   hex: "#ff453a", role: "Negative feedback (debt, error)" },
];

export function registerThemePropose(server: McpServer): void {
  server.registerTool(
    "theme_propose",
    {
      description:
        "Propose a UI color palette (≥6 ds-* token slugs + hex values). " +
        "Stub — returns a hardcoded night-city dark theme tracer palette. " +
        "Real LLM proposer integration deferred to Stage N. " +
        "TECH-15229 — Stage 9.5 game-ui-catalog-bake.",
      inputSchema: themeProposeInputSchema,
    },
    async (_args) => {
      const payload = {
        ok: true,
        stub: true,
        note: "Hardcoded tracer palette — Stage 9.5 stub (TECH-15229). LLM integration deferred.",
        token_count: TRACER_PALETTE.length,
        tokens: TRACER_PALETTE,
      };
      return {
        content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
      };
    },
  );
}
