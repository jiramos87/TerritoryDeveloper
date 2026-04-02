/**
 * Glossary table parsing: categories (## headings) and Term / Definition / Spec columns.
 */

import type { GlossaryEntry, HeadingNode, ParsedDocument } from "./types.js";
import { parseDocument, splitLines } from "./markdown-parser.js";
import { parseMarkdownTables } from "./table-parser.js";
import fs from "node:fs";

function collectDepth2Categories(doc: ParsedDocument): HeadingNode[] {
  const out: HeadingNode[] = [];
  const walk = (nodes: HeadingNode[]) => {
    for (const n of nodes) {
      if (n.depth === 2) out.push(n);
      walk(n.children);
    }
  };
  walk(doc.headings);
  return out;
}

/**
 * Parse glossary.md into structured entries (all categories).
 */
export function parseGlossary(filePath: string): GlossaryEntry[] {
  const doc = parseDocument(filePath);
  const raw = fs.readFileSync(filePath, "utf8");
  const fileLines = splitLines(raw);
  const categories = collectDepth2Categories(doc);
  const entries: GlossaryEntry[] = [];

  for (const cat of categories) {
    const categoryTitle = cat.title.trim();
    const slice = fileLines.slice(cat.lineStart - 1, cat.lineEnd);
    const tables = parseMarkdownTables(slice);
    for (const table of tables) {
      for (const row of table.rows) {
        const term = (row.Term ?? row.term ?? "").trim();
        const definition = (row.Definition ?? row.definition ?? "").trim();
        const specReference = (row.Spec ?? row.spec ?? "").trim();
        if (!term && !definition) continue;
        entries.push({
          term,
          definition,
          specReference,
          category: categoryTitle,
        });
      }
    }
  }

  return entries;
}
