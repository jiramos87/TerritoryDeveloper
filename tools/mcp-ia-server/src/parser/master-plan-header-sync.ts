/**
 * Master-plan step/stage header synchronisation helpers.
 *
 * TECH-322 cross-ref: if TECH-322 ships a shared Q5=C hybrid orchestrator parser
 * before this module is widely adopted, migrate to that shared parser and
 * retire the narrow regex here. Otherwise TECH-322 should inherit these
 * regex patterns as its starting point.
 *
 * Rewrites `**Status:**` + `**Backlog state (...):**` paragraph under
 * `### Step N — Title` (h3) and `#### Stage N.N — Title` (h4) headers
 * from task-table ground truth.
 *
 * Contract:
 *   - All task rows `Done (archived)` → `**Status:** Final`
 *   - Mix Done + open → `**Status:** In Progress — {first-open-task-id}`
 *   - All `_pending_` → `**Status:** Draft (tasks _pending_ — not yet filed)`
 *   - Backlog state count = rows with non-`_pending_` Issue cell value.
 *   - Rewrite is idempotent: re-running on an already-synced doc produces
 *     zero diff.
 */

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

/** Parsed info about one step or stage block. */
export interface StepStageBlock {
  /** "step" for `### Step N — ...`; "stage" for `#### Stage N.N — ...` */
  kind: "step" | "stage";
  /** e.g. "1" for Step 1, "1.1" for Stage 1.1 */
  number: string;
  /** Zero-based start index of the header line in the lines array. */
  headerLineIndex: number;
  /** Zero-based index of `**Status:**` line (may equal -1 if not found). */
  statusLineIndex: number;
  /** Zero-based index of `**Backlog state (...):**` line (-1 if not found). */
  backlogStateLineIndex: number;
}

/** Task row parsed from a task table under a step/stage heading. */
export interface TaskRow {
  /** Value of the "Issue" column cell (trimmed, including leading `**` / `[]`). */
  issueCell: string;
  /** Value of the "Status" column cell (trimmed). */
  statusCell: string;
}

// ---------------------------------------------------------------------------
// Regex constants
// ---------------------------------------------------------------------------

/**
 * Matches `### Step N — Title` (h3 step heading).
 * Capture group 1 = step number string (e.g. "1", "2").
 */
export const STEP_HEADING_RE = /^### Step (\d+) — .+$/;

/**
 * Matches `#### Stage N.N — Title` (h4 stage heading).
 * Capture group 1 = stage number string (e.g. "1.1", "2.3").
 */
export const STAGE_HEADING_RE = /^#### Stage (\d+\.\d+) — .+$/;

/**
 * Matches the `**Status:**` paragraph line under a step/stage header.
 * Accepts any current value after the colon.
 */
export const STATUS_LINE_RE = /^\*\*Status:\*\* .+$/;

/**
 * Matches `**Backlog state (Step N):**` or `**Backlog state (Stage N.N):**` line.
 * Capture group 1 = label inside parens (e.g. "Step 1", "Stage 1.1").
 * Capture group 2 = existing count digit string.
 */
export const BACKLOG_STATE_LINE_RE =
  /^\*\*Backlog state \((Step \d+|Stage \d+\.\d+)\):\*\* (\d+) filed$/;

// ---------------------------------------------------------------------------
// Task-table scanning
// ---------------------------------------------------------------------------

/**
 * Extract the "Issue" and "Status" column indices from a markdown table
 * separator row (the `|---|---|` row). Returns null when either column
 * is absent.
 */
function findTableColumnIndices(
  separatorLine: string,
): { issueIdx: number; statusIdx: number } | null {
  // Walk the header row (one line before separator) to find column positions.
  // We use this function with the header line passed in separately.
  return null; // placeholder — replaced by parseTaskTableRow below
}

/**
 * Given a header row (pipe-delimited) return a map of lower-cased column
 * name → zero-based column index.
 */
function parseTableHeader(headerLine: string): Map<string, number> {
  const cols = headerLine
    .split("|")
    .map((c) => c.trim().toLowerCase())
    .filter((c) => c.length > 0);
  const m = new Map<string, number>();
  cols.forEach((c, i) => m.set(c, i));
  return m;
}

/**
 * Parse one task table data row (pipe-delimited) with column index map.
 * Returns null for separator rows or non-table lines.
 */
function parseTaskTableRow(
  line: string,
  colMap: Map<string, number>,
): TaskRow | null {
  const t = line.trim();
  if (!t.startsWith("|")) return null;
  // Skip separator rows (contain only `-`, `|`, `:`, whitespace).
  if (/^\|[\s|:|-]+\|$/.test(t)) return null;

  const cells = t
    .split("|")
    .map((c) => c.trim())
    .filter((_, i, a) => i > 0 && i < a.length - 1);

  const issueIdx = colMap.get("issue");
  const statusIdx = colMap.get("status");
  if (issueIdx == null || statusIdx == null) return null;
  if (cells.length <= Math.max(issueIdx, statusIdx)) return null;

  return {
    issueCell: cells[issueIdx] ?? "",
    statusCell: cells[statusIdx] ?? "",
  };
}

/**
 * Scan forward from `startLineIndex` (exclusive) in `lines` until hitting
 * a heading at the same or higher depth, collecting TaskRow entries from
 * any task table encountered.
 *
 * @param lines        Full document lines array.
 * @param startLineIndex  Index of the step/stage heading line (exclusive start).
 * @param stopDepth    Heading depth to stop at (3 for step, 4 for stage).
 *                     Stops at `###` (depth 3) or shallower heading when
 *                     scanning inside a stage (depth 4).
 */
export function scanTaskRows(
  lines: string[],
  startLineIndex: number,
  stopDepth: number,
): TaskRow[] {
  const rows: TaskRow[] = [];
  let colMap: Map<string, number> | null = null;
  let inTable = false;
  let headerParsed = false;

  for (let i = startLineIndex + 1; i < lines.length; i++) {
    const line = lines[i];

    // Stop at a heading at same or higher (lower number) depth.
    const headingMatch = /^(#{1,6}) /.exec(line);
    if (headingMatch) {
      const depth = headingMatch[1].length;
      if (depth <= stopDepth) break;
    }

    const t = line.trim();

    if (t.startsWith("|")) {
      if (!inTable) {
        // First pipe line in this table block.
        inTable = true;
        headerParsed = false;
        colMap = null;
      }
      if (!headerParsed) {
        // Check if next non-empty line is a separator → this is the header row.
        // Look ahead one line for separator.
        const nextLine = lines[i + 1]?.trim() ?? "";
        if (/^\|[\s|:|-]+\|$/.test(nextLine)) {
          // This line is the header row.
          colMap = parseTableHeader(t);
          headerParsed = true;
          i++; // skip separator on next iteration
          continue;
        }
      }
      if (colMap) {
        const row = parseTaskTableRow(t, colMap);
        if (row) rows.push(row);
      }
    } else {
      if (inTable && t.length > 0) {
        // Non-empty, non-pipe line after table content → table ended.
        inTable = false;
        headerParsed = false;
        colMap = null;
      }
    }
  }

  return rows;
}

// ---------------------------------------------------------------------------
// Status line computation
// ---------------------------------------------------------------------------

/** Normalise an Issue cell value to a bare id or `_pending_`. */
function normalisedIssueCell(raw: string): string {
  // Strip bold markers: **TECH-42** → TECH-42
  const stripped = raw.replace(/\*\*/g, "").trim();
  // Strip link syntax: [TECH-42](...) → TECH-42
  const linkMatch = /^\[([^\]]+)\]/.exec(stripped);
  return linkMatch ? linkMatch[1].trim() : stripped;
}

/** True when an issue cell represents a real filed issue (not `_pending_`). */
function isFiled(cell: string): boolean {
  const n = normalisedIssueCell(cell);
  return n.length > 0 && n !== "_pending_" && /^(BUG|FEAT|TECH|ART|AUDIO)-/i.test(n);
}

/** True when a status cell represents a closed task. */
function isDone(statusCell: string): boolean {
  return /done/i.test(statusCell);
}

/** True when a status cell represents an open (not done) task. */
function isOpen(statusCell: string): boolean {
  return !isDone(statusCell);
}

export interface StatusComputation {
  /** New `**Status:** ...` line text (full line). */
  statusLine: string;
  /** New `**Backlog state (Label):** k filed` line text. */
  backlogStateLine: string;
}

/**
 * Compute the new Status + Backlog state lines for a step/stage block
 * given its scanned task rows.
 *
 * @param label   e.g. "Step 1" or "Stage 1.1" — inserted in Backlog state line.
 * @param rows    Task rows scanned from the block's task table.
 */
export function computeStatusLines(
  label: string,
  rows: TaskRow[],
): StatusComputation {
  if (rows.length === 0) {
    // No task table found — all pending.
    return {
      statusLine: "**Status:** Draft (tasks _pending_ — not yet filed)",
      backlogStateLine: `**Backlog state (${label}):** 0 filed`,
    };
  }

  const filedRows = rows.filter((r) => isFiled(r.issueCell));
  const filedCount = filedRows.length;
  const allDone = rows.every((r) => isDone(r.statusCell));
  const allPending = rows.every((r) => !isFiled(r.issueCell));

  let statusLine: string;

  if (allPending) {
    statusLine = "**Status:** Draft (tasks _pending_ — not yet filed)";
  } else if (allDone) {
    statusLine = "**Status:** Final";
  } else {
    // Find first open filed task id.
    const firstOpen = filedRows.find((r) => isOpen(r.statusCell));
    const firstId = firstOpen
      ? normalisedIssueCell(firstOpen.issueCell)
      : null;
    statusLine = firstId
      ? `**Status:** In Progress — ${firstId}`
      : "**Status:** In Progress";
  }

  return {
    statusLine,
    backlogStateLine: `**Backlog state (${label}):** ${filedCount} filed`,
  };
}

// ---------------------------------------------------------------------------
// Document-level header detection
// ---------------------------------------------------------------------------

/**
 * Scan `lines` for step/stage header blocks, returning one StepStageBlock
 * per heading found. Looks only for the Status and Backlog state lines in
 * the immediately following non-empty paragraph (before the next heading).
 */
export function findStepStageBlocks(lines: string[]): StepStageBlock[] {
  const blocks: StepStageBlock[] = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let kind: "step" | "stage" | null = null;
    let number = "";

    const stepM = STEP_HEADING_RE.exec(line);
    if (stepM) {
      kind = "step";
      number = stepM[1];
    }
    const stageM = STAGE_HEADING_RE.exec(line);
    if (stageM) {
      kind = "stage";
      number = stageM[1];
    }

    if (!kind) continue;

    // Search forward (skipping blank lines) for Status + Backlog state lines.
    // They should appear within the next ~10 lines, before any nested heading.
    let statusLineIndex = -1;
    let backlogStateLineIndex = -1;

    for (let j = i + 1; j < Math.min(i + 15, lines.length); j++) {
      const l = lines[j];
      // Stop at any heading.
      if (/^#{1,6} /.test(l)) break;
      if (STATUS_LINE_RE.test(l)) statusLineIndex = j;
      if (BACKLOG_STATE_LINE_RE.test(l)) backlogStateLineIndex = j;
    }

    blocks.push({
      kind,
      number,
      headerLineIndex: i,
      statusLineIndex,
      backlogStateLineIndex,
    });
  }

  return blocks;
}

// ---------------------------------------------------------------------------
// Document rewrite
// ---------------------------------------------------------------------------

/**
 * Sync all step/stage header Status + Backlog state lines in `markdown`
 * from the task-table ground truth. Returns the rewritten markdown string.
 *
 * Idempotent: re-running on an already-synced document returns the same string.
 *
 * Algorithm:
 *   1. Split doc into lines.
 *   2. Find all step/stage blocks.
 *   3. For each block (processed in reverse order to keep line indices stable):
 *      a. Scan forward from header to collect task rows.
 *      b. Compute new Status + Backlog state lines.
 *      c. Replace existing lines in-place (idempotent when already correct).
 *   4. After all step rewrites: propagate — if all sibling stages under a
 *      step are `Final`, force the step's Status to `Final` as well.
 *   5. Join and return.
 */
export function syncMasterPlanHeaders(markdown: string): string {
  const lines = markdown.split(/\r?\n/);
  const blocks = findStepStageBlocks(lines);

  // Process in reverse order so earlier line indices remain valid.
  for (let bi = blocks.length - 1; bi >= 0; bi--) {
    const block = blocks[bi];
    const label =
      block.kind === "step" ? `Step ${block.number}` : `Stage ${block.number}`;
    const stopDepth = block.kind === "step" ? 3 : 4;

    const rows = scanTaskRows(lines, block.headerLineIndex, stopDepth);
    const { statusLine, backlogStateLine } = computeStatusLines(label, rows);

    // Replace or insert Status line.
    if (block.statusLineIndex >= 0) {
      lines[block.statusLineIndex] = statusLine;
    }
    // Replace or insert Backlog state line.
    if (block.backlogStateLineIndex >= 0) {
      lines[block.backlogStateLineIndex] = backlogStateLine;
    }
  }

  // --- Step-level propagation from stage Final checks ---
  // Re-read blocks from the (now-mutated) lines to pick up stage edits.
  const reBlocks = findStepStageBlocks(lines);

  // For each step block: check if all child stage blocks are Final.
  const stepBlocks = reBlocks.filter((b) => b.kind === "step");
  for (const step of stepBlocks) {
    // Collect stage blocks that are direct children of this step.
    // A stage block is a child if its headerLineIndex > step.headerLineIndex
    // and < next step's headerLineIndex.
    const nextStepIdx =
      reBlocks.find(
        (b) => b.kind === "step" && b.headerLineIndex > step.headerLineIndex,
      )?.headerLineIndex ?? lines.length;

    const childStages = reBlocks.filter(
      (b) =>
        b.kind === "stage" &&
        b.headerLineIndex > step.headerLineIndex &&
        b.headerLineIndex < nextStepIdx,
    );

    if (childStages.length === 0) continue;

    const allStagesFinal = childStages.every(
      (s) =>
        s.statusLineIndex >= 0 &&
        lines[s.statusLineIndex].trim() === "**Status:** Final",
    );

    if (allStagesFinal && step.statusLineIndex >= 0) {
      lines[step.statusLineIndex] = "**Status:** Final";
    }
  }

  return lines.join("\n");
}
