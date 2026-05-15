/**
 * Lightweight USS tokenizer.
 *
 * Parses Unity Style Sheet (.uss) files into structured rule objects.
 * Preserves literal hex color values (e.g. #1A2B3C) without transformation.
 */

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface UssRule {
  selector: string;
  props: Record<string, string>;
  line: number;
}

// ---------------------------------------------------------------------------
// Parser
// ---------------------------------------------------------------------------

/**
 * Parse a .uss file content string into an array of UssRule objects.
 *
 * Handles:
 * - Single-line and multi-line rule blocks
 * - Nested braces (skips — USS does not support nesting but guards against malformed input)
 * - Line comments (`/* ... *\/`) stripped before parsing
 * - Hex color literals preserved verbatim
 */
export function parseUssFile(content: string): UssRule[] {
  const rules: UssRule[] = [];

  // Strip block comments while preserving line counts.
  const stripped = content.replace(/\/\*[\s\S]*?\*\//g, (match) => {
    // Replace comment body with same number of newlines to preserve line numbers.
    const newlines = (match.match(/\n/g) ?? []).length;
    return "\n".repeat(newlines);
  });

  const lines = stripped.split(/\r?\n/);
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;
    const trimmed = line.trim();

    // Skip empty lines
    if (!trimmed) {
      i++;
      continue;
    }

    // Detect start of a rule block: something ending with `{` or containing `{`
    const braceIdx = trimmed.indexOf("{");
    if (braceIdx === -1) {
      i++;
      continue;
    }

    const selectorRaw = trimmed.slice(0, braceIdx).trim();
    if (!selectorRaw) {
      i++;
      continue;
    }

    const ruleLine = i + 1; // 1-based
    const props: Record<string, string> = {};

    // Collect everything until matching closing brace
    let depth = 1;
    const bodyLines: string[] = [];

    // Rest of the opening line after `{`
    const restOfOpen = trimmed.slice(braceIdx + 1);
    if (restOfOpen.includes("}")) {
      // Inline single-line rule: .Foo { color: red; }
      const closeIdx = restOfOpen.indexOf("}");
      bodyLines.push(restOfOpen.slice(0, closeIdx));
      depth = 0;
    } else {
      if (restOfOpen.trim()) bodyLines.push(restOfOpen);
      i++;

      while (i < lines.length && depth > 0) {
        const bodyLine = lines[i]!;
        const bt = bodyLine.trim();
        if (bt.includes("{")) depth++;
        if (bt.includes("}")) {
          depth--;
          if (depth === 0) {
            // Collect content before closing brace
            const closeIdx = bodyLine.indexOf("}");
            const before = bodyLine.slice(0, closeIdx).trim();
            if (before) bodyLines.push(before);
            break;
          }
        }
        bodyLines.push(bodyLine);
        i++;
      }
    }

    // Parse property declarations from bodyLines
    const propBody = bodyLines.join("\n");
    // Split on semicolons to get individual declarations
    const declarations = propBody.split(";");
    for (const decl of declarations) {
      const t = decl.trim();
      if (!t) continue;
      const colonIdx = t.indexOf(":");
      if (colonIdx === -1) continue;
      const propName = t.slice(0, colonIdx).trim();
      const propValue = t.slice(colonIdx + 1).trim();
      if (propName && propValue) {
        props[propName] = propValue;
      }
    }

    if (selectorRaw) {
      // Handle comma-separated selector groups — emit one rule per selector
      const selectors = selectorRaw
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean);

      for (const selector of selectors) {
        rules.push({ selector, props: { ...props }, line: ruleLine });
      }
    }

    i++;
  }

  return rules;
}
