/**
 * Generic Markdown pipe-table parsing (headers, separator row, data rows).
 */

export type TableRow = Record<string, string>;

export interface ParsedMarkdownTable {
  /** 1-based line index within the `lines` array passed to the parser. */
  headerLine: number;
  rows: TableRow[];
}

function stripCellMarkers(cell: string): string {
  return cell.replace(/\*\*([^*]*)\*\*/g, "$1").trim();
}

/**
 * Split a table row on `|` boundaries, ignoring pipes inside backticks and escaped `\|`.
 */
function splitPipeRow(line: string): string[] {
  const t = line.trim();
  if (!t.startsWith("|") || !t.endsWith("|")) return [];
  const inner = t.slice(1, -1);
  const cells: string[] = [];
  let buf = "";
  let inTick = false;
  for (let i = 0; i < inner.length; i++) {
    const ch = inner[i]!;
    if (ch === "`") {
      inTick = !inTick;
      buf += ch;
      continue;
    }
    if (ch === "|" && !inTick) {
      if (buf.endsWith("\\")) {
        buf = buf.slice(0, -1) + "|";
        continue;
      }
      cells.push(stripCellMarkers(buf.trim()));
      buf = "";
      continue;
    }
    buf += ch;
  }
  cells.push(stripCellMarkers(buf.trim()));
  return cells;
}

function isSeparatorRow(line: string): boolean {
  const t = line.trim();
  if (!t.startsWith("|") || !t.endsWith("|")) return false;
  const inner = t.slice(1, -1);
  const parts = inner.split("|");
  return parts.every((p) => /^[\s:-]+$/.test(p.trim()));
}

function isTableRowLine(line: string): boolean {
  const t = line.trim();
  return t.startsWith("|") && t.endsWith("|") && t.includes("|", 1);
}

/**
 * Parse all pipe tables in the given line array.
 */
export function parseMarkdownTables(lines: string[]): ParsedMarkdownTable[] {
  const tables: ParsedMarkdownTable[] = [];
  let i = 0;
  while (i < lines.length) {
    if (!isTableRowLine(lines[i]!)) {
      i++;
      continue;
    }
    const headerIdx = i;
    const headerCells = splitPipeRow(lines[i]!);
    if (headerCells.length < 2 || !lines[i + 1]) {
      i++;
      continue;
    }
    if (!isSeparatorRow(lines[i + 1]!)) {
      i++;
      continue;
    }
    const headers = headerCells.map((h) => h.trim());
    i += 2;
    const rows: TableRow[] = [];
    while (i < lines.length && isTableRowLine(lines[i]!)) {
      if (isSeparatorRow(lines[i]!)) {
        i++;
        continue;
      }
      const cells = splitPipeRow(lines[i]!);
      if (cells.length !== headers.length) {
        break;
      }
      const row: TableRow = {};
      for (let c = 0; c < headers.length; c++) {
        row[headers[c]!] = cells[c] ?? "";
      }
      rows.push(row);
      i++;
    }
    tables.push({
      headerLine: headerIdx + 1,
      rows,
    });
  }
  return tables;
}
