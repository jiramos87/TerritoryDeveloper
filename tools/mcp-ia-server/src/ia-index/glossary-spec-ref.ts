/**
 * Map glossary "Spec" column text to MCP registry spec keys and section anchors.
 */

import { resolveSpecKeyAlias } from "../config.js";
import { extractSectionId } from "../parser/markdown-parser.js";

/**
 * Strip noise from a Spec cell (bold/backticks) and take the first comma/semicolon segment.
 */
export function primarySpecSegment(raw: string): string {
  const cleaned = raw.replace(/\*\*/g, "").replace(/`/g, "").trim();
  const m = cleaned.match(/^([^,;]+)/);
  return (m ? m[1] : cleaned).trim();
}

/**
 * Parse glossary Spec cell → registry `spec_key` + `anchor` (heading slug or numeric id).
 *
 * Examples: `geo §1, §2` → `isometric-geography-system` / `1`;
 * `persist §Save` → `persistence-system` / `save`;
 * `ARCHITECTURE.md` → `architecture` / ``.
 */
export function glossarySpecCellToIndex(
  specReference: string,
): { spec_key: string; anchor: string } | null {
  const trimmed = specReference.trim();
  if (!trimmed || trimmed === "—" || trimmed === "-") return null;

  const primary = primarySpecSegment(trimmed);
  const re = /^([A-Za-z0-9_.-]+(?:\.md)?)\s*(?:§\s*)?(.*)$/;
  const m = primary.match(re);
  if (!m) return null;

  let token = m[1]!.trim();
  const rest = (m[2] ?? "").trim();
  if (token.toLowerCase().endsWith(".md")) {
    token = token.slice(0, -3);
  }

  const aliasInput = token.toLowerCase();
  const spec_key = resolveSpecKeyAlias(aliasInput);

  const anchor =
    rest.length > 0 ? extractSectionId(rest) : "";

  return { spec_key, anchor };
}
