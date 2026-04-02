/**
 * Shared types for Markdown parsing and the IA document registry.
 */

export interface HeadingNode {
  /** 1–6 (## = 2, ### = 3, …). */
  depth: number;
  /** Raw heading text. */
  title: string;
  /** Numeric prefix when present, otherwise slugified title. */
  sectionId: string;
  /** 1-based line in the physical file (including frontmatter). */
  lineStart: number;
  /** 1-based last line of this section (inclusive). */
  lineEnd: number;
  children: HeadingNode[];
}

export interface ParsedDocument {
  filePath: string;
  fileName: string;
  frontmatter: Record<string, unknown> | null;
  headings: HeadingNode[];
  lineCount: number;
}

export interface SpecRegistryEntry {
  key: string;
  fileName: string;
  filePath: string;
  description: string;
  category: "spec" | "rule" | "root-doc";
}

/** One row from `glossary.md` Term / Definition / Spec tables. */
export interface GlossaryEntry {
  term: string;
  definition: string;
  specReference: string;
  category: string;
}

/** Normalized router tool match (both agent-router tables). */
export interface RouterMatchRow {
  taskDomain: string;
  specToRead: string;
  keySections: string;
}
