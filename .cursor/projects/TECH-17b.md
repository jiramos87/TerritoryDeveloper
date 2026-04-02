# TECH-17b — Semantic Query Tools: Section Retrieval, Glossary, Router, Invariants

> **Issue:** [TECH-17](../../BACKLOG.md)
> **Status:** Final (historical — see **§11** and [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md))
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02 (§9–11 retrospective)
> **Sequence:** Part 2 of 3 (TECH-17a → **TECH-17b** → TECH-17c)
> **Prerequisite:** TECH-17a completed (server running, parser module, `list_specs` and `spec_outline` working).

## 1. Summary

Add four tools that let agents retrieve **specific slices** of Information Architecture documents: a single spec section by heading, a glossary entry by term, the task→spec routing table by domain, and the full invariants summary. After this phase, an agent can answer "which spec covers roads?" or "what does 'wet run' mean?" via MCP tool calls consuming **tens of lines** instead of loading entire 700+ line files.

### 1.1 Implementer guidance (AI agents)

Same as **TECH-17a §1.1**: while building `glossary_lookup`, `spec_section`, and smoke tests, **read `.cursor/specs/glossary.md`** to confirm term names, column shapes, and expected definitions. Use the **Spec** column for cross-checks against large specs. Do not invent domain meanings when validating parser output or example JSON.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `spec_section` tool — retrieve one section (heading + body) from any spec/rule/root-doc.
2. `glossary_lookup` tool — retrieve **one** glossary entry per call by exact term name (case-insensitive); fuzzy / suggestions are **TECH-17c**.
3. `router_for_task` tool — query the agent-router table and return matching rows.
4. `invariants_summary` tool — return the full invariants and guardrails content.
5. All tools return structured JSON with consistent error shapes (serialized per MCP SDK requirements — see TECH-17a §5.5).
6. **`spec_section`** output includes a `truncated` flag and respects `max_chars` (default 3000). Other tools in this phase do not use `max_chars` unless specified.

### 2.2 Non-Goals (Out of Scope)

1. Fuzzy/partial matching for terms or section names — that is TECH-17c.
2. Full-text search across all documents — out of TECH-17 scope entirely.
3. Unit tests — those are TECH-17c.
4. Tools for `.cursor/rules/*.mdc` individual rule retrieval — TECH-17c adds `list_rules` and `rule_content`.
5. Database or caching layer — TECH-19.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want to read just §13.4 of the geography spec so I understand bridge rules without loading 772 lines | `spec_section({ spec: "isometric-geography-system", section: "13.4" })` returns only that section's content |
| 2 | AI agent | I want to know what "wet run" means | `glossary_lookup({ term: "wet run" })` returns the definition row |
| 3 | AI agent | I'm working on roads — which specs should I read? | `router_for_task({ domain: "roads" })` returns the matching row(s) from agent-router |
| 4 | AI agent | I need to check all system invariants before making a change | `invariants_summary()` returns the full invariants + guardrails in one call |
| 5 | AI agent | A section is very long — I want just the first 2000 chars | `spec_section({ spec: "...", section: "...", max_chars: 2000 })` returns truncated content with `truncated: true` |

## 4. Current State

### 4.1 Relevant Files and Systems (from TECH-17a)

| File / Path | Role in this context |
|-------------|----------------------|
| `tools/mcp-ia-server/src/parser/markdown-parser.ts` | Existing parser from TECH-17a — has `parseDocument`, `buildHeadingTree`, `extractSectionId` |
| `tools/mcp-ia-server/src/parser/types.ts` | Shared types: `HeadingNode`, `ParsedDocument`, `SpecRegistryEntry` |
| `tools/mcp-ia-server/src/config.ts` | File registry, `buildRegistry`, `findEntryByKey` |
| `tools/mcp-ia-server/src/index.ts` | Server entry point where tools are registered |
| `.cursor/specs/glossary.md` | Glossary — Markdown tables with `| Term | Definition | Spec |` columns grouped by `## Category` headings |
| `.cursor/rules/agent-router.mdc` | Agent router — Markdown table with `| Task domain | Spec to read | Key sections |` columns |
| `.cursor/rules/invariants.mdc` | Invariants — numbered list + guardrails bulleted list |

### 4.2 Document Format Details

**Glossary** (`glossary.md`, 183 lines):
- 14 category sections (`## Grid & Coordinates`, `## Height System`, etc.).
- Each section has a Markdown table: `| **Term** | Definition | Spec |`.
- Terms are bold within pipes: `| **Wet run** | Contiguous water... | geo §13.4 |`.
- ~65 terms total across all tables.

**Agent Router** (`agent-router.mdc`, 51 lines):
- Frontmatter: `description: "Agent index — routes tasks to the right specs and rules"`, `alwaysApply: true`.
- Body has **two** Markdown tables; `router_for_task` must search **both** (union of matches):
  1. **Task → Spec routing** — columns `Task domain`, `Spec to read`, `Key sections` (14 rows). Match `domain` with **case-insensitive substring** against `Task domain`. Emit `{ taskDomain, specToRead, keySections }` from the three columns verbatim (trimmed).
  2. **Quick reference for geography spec sections** — columns `Need to understand...`, `Read sections` (14 rows). Match `domain` with **case-insensitive substring** against `Need to understand...`. Emit `{ taskDomain: <col1>, keySections: <col2>, specToRead: ".cursor/specs/isometric-geography-system.md (see Read sections column)" }` — this table is **geo-only**; all rows refer to the canonical geography spec.

**Invariants** (`invariants.mdc`, 31 lines):
- Frontmatter: `description: "System invariants and guardrails — never violate"`, `alwaysApply: true`.
- Body: `# System Invariants (NEVER violate)` with 12 numbered items, then `# Guardrails (IF → THEN)` with 9 bulleted items.

## 5. Proposed Design

### 5.1 New Files

```
tools/mcp-ia-server/src/
  parser/
    markdown-parser.ts    # Extended: add extractSection(), extractLines()
    glossary-parser.ts    # NEW: glossary-specific table parsing
    table-parser.ts       # NEW: generic Markdown table row extraction
    types.ts              # Extended: add GlossaryEntry, RouterRow, etc.
  tools/
    spec-section.ts       # NEW: spec_section tool
    glossary-lookup.ts    # NEW: glossary_lookup tool
    router-for-task.ts    # NEW: router_for_task tool
    invariants-summary.ts # NEW: invariants_summary tool
```

### 5.2 Parser Extensions

#### `table-parser.ts` — Generic Markdown Table Parser

Parses Markdown tables into structured rows. Used by glossary and router tools.

```typescript
interface TableRow {
  [columnHeader: string]: string;  // column header → cell content (stripped of bold markers, trimmed)
}

function parseMarkdownTables(lines: string[]): { headerLine: number; rows: TableRow[] }[];
```

**Algorithm:**
1. Scan for lines matching `^\|(.+\|)+$` (pipe-delimited).
2. Identify header row (first pipe row), separator row (`|---|`), then data rows until a non-pipe line.
3. Strip bold markers (`**...**`) from cell content.
4. Return array of tables found, each with its header line number and parsed rows.

#### `glossary-parser.ts` — Glossary-Specific Parser

```typescript
interface GlossaryEntry {
  term: string;          // e.g. "Wet run"
  definition: string;    // Full definition text
  specReference: string; // e.g. "geo §13.4, geo §14.5, roads"
  category: string;      // e.g. "Roads & Bridges"
}

function parseGlossary(filePath: string): GlossaryEntry[];
```

**Algorithm:**
1. Parse the document with `parseDocument` to get heading tree.
2. For each `## Category` section, extract the table within its line range using `parseMarkdownTables`.
3. Map table rows to `GlossaryEntry` objects: column "Term" → `term` (strip `**`), column "Definition" → `definition`, column "Spec" → `specReference`. The enclosing `##` heading → `category`.

#### `markdown-parser.ts` — Extended with Section Extraction

Add to existing module:

```typescript
function extractSection(doc: ParsedDocument, sectionId: string): {
  heading: HeadingNode;
  content: string;
  lineStart: number;
  lineEnd: number;
} | null;

function extractLines(filePath: string, lineStart: number, lineEnd: number): string;
```

**`extractSection` algorithm:**
1. Walk the heading tree (BFS or DFS) looking for a node where `sectionId` matches (case-insensitive, ignore leading `§`).
2. If not found by ID, try matching against the full `title` (case-insensitive substring).
3. If found, read lines `[lineStart, lineEnd]` from the file and return.
4. If not found, return `null`.

**Disambiguation for numeric `section` queries (e.g. `"13"` vs `"13.4"`):**
- Prefer **exact** `sectionId` match after normalization (strip leading `§`, trim).
- If the query is a **single segment** (e.g. `"13"`) and matches **multiple** nodes (`13`, `13.1`, `13.2`, …), return the node whose `sectionId` equals the query **exactly** (the parent `## 13` / `### 13` section), i.e. the **shortest** matching `sectionId` by string length / depth — not the first DFS hit.
- If still ambiguous (two headings share the same `sectionId` — should not happen if IDs are unique), return an error listing candidates instead of picking arbitrarily.

### 5.3 Tool Contracts

#### `spec_section`

| Property | Value |
|----------|-------|
| **Name** | `spec_section` |
| **Description** | Retrieve the content of a specific section from a spec, rule, or root document. Use `spec_outline` first to discover available sections. |
| **Input schema** | `z.object({ spec: z.string().describe("Key or filename (e.g. 'isometric-geography-system', 'roads-system')."), section: z.string().describe("Section ID (e.g. '13.4'), heading slug, or heading text substring (e.g. 'Bridges and water approach')."), max_chars: z.number().optional().describe("Maximum characters to return. Default: 3000. Truncates at the end.") })` |
| **Output (success)** | `{ key, sectionId, title, lineStart, lineEnd, content, truncated, totalChars }` |
| **Output (spec not found)** | `{ error: "unknown_spec", message: "...", available_keys: [...] }` |
| **Output (section not found)** | `{ error: "unknown_section", message: "...", available_sections: [{ sectionId, title }...] }` — includes up to 20 top-level sections for guidance |

**Example call:**
```
spec_section({ spec: "isometric-geography-system", section: "13.4" })
```

**Example response:**
```json
{
  "key": "isometric-geography-system",
  "sectionId": "13.4",
  "title": "Bridges and water approach",
  "lineStart": 665,
  "lineEnd": 673,
  "content": "### 13.4 Bridges and water approach\n\nDeck span: straight...",
  "truncated": false,
  "totalChars": 487
}
```

#### `glossary_lookup`

| Property | Value |
|----------|-------|
| **Name** | `glossary_lookup` |
| **Description** | Look up a domain term in the glossary. Returns the definition, spec reference, and category. |
| **Input schema** | `z.object({ term: z.string().describe("The term to look up (e.g. 'wet run', 'HeightMap', 'shore band'). Case-insensitive exact match.") })` |
| **Output (found)** | `{ term, definition, specReference, category }` |
| **Output (not found)** | `{ error: "term_not_found", message: "...", available_terms: [...] }` — returns all known terms so the agent can retry or pick the closest |

**Example call:**
```
glossary_lookup({ term: "wet run" })
```

**Example response:**
```json
{
  "term": "Wet run",
  "definition": "Contiguous water and/or water-slope cells along a stroke that a bridge crosses in one straight segment. Truncation rules keep wet runs intact for FEAT-44.",
  "specReference": "geo §13.4, geo §14.5, roads",
  "category": "Roads & Bridges"
}
```

#### `router_for_task`

| Property | Value |
|----------|-------|
| **Name** | `router_for_task` |
| **Description** | Query the agent-router to find which specs and sections to read. Searches **both** tables in `agent-router.mdc` (task→spec routing + geography quick reference); see §4.2. |
| **Input schema** | `z.object({ domain: z.string().describe("Keyword (e.g. 'roads', 'water', 'grid math', 'save'). Case-insensitive substring match against 'Task domain' (first table) OR 'Need to understand...' (second table).") })` |
| **Output (match)** | `{ matches: [{ taskDomain, specToRead, keySections }...] }` |
| **Output (no match)** | `{ error: "no_matching_domain", message: "...", available_domains: [...] }` — lists every **first-column** label from **both** tables (task domains + “Need to understand…” rows), deduplicated if needed |

**Example call:**
```
router_for_task({ domain: "roads" })
```

**Example response:**
```json
{
  "matches": [
    {
      "taskDomain": "Road logic, placement, bridges",
      "specToRead": "`.cursor/specs/roads-system.md` + geography spec §9, §10, §13, **§14.5**",
      "keySections": "Validation surface, resolver rules, stroke / lip / wet run"
    }
  ]
}
```

#### `invariants_summary`

| Property | Value |
|----------|-------|
| **Name** | `invariants_summary` |
| **Description** | Return the full system invariants and guardrails. These must NEVER be violated when making changes. |
| **Input schema** | `z.object({})` (no inputs) |
| **Output** | `{ description, invariants: string[], guardrails: string[] }` where `invariants` is the numbered list items and `guardrails` is the bulleted IF→THEN items, each as a plain string. |

**Example response:**
```json
{
  "description": "System invariants and guardrails — never violate",
  "invariants": [
    "`HeightMap[x,y]` == `Cell.height` — always in sync; update both on every write",
    "After road modification → call `InvalidateRoadCache()`",
    "..."
  ],
  "guardrails": [
    "IF adding a manager reference → THEN `[SerializeField] private` + `FindObjectOfType` fallback in `Awake`",
    "..."
  ]
}
```

### 5.4 Truncation Policy

All tools that return document content accept an optional `max_chars` parameter:

| Parameter | Default | Behavior |
|-----------|---------|----------|
| `max_chars` | 3000 | If `content.length > max_chars`, truncate to `max_chars` characters at the nearest preceding line break. Set `truncated: true`. Include `totalChars` so the agent knows how much was cut. |

The `invariants_summary` tool does **not** truncate (the file is 31 lines — always fits).

### 5.5 Wiring into `index.ts`

Add four new import/register calls to the existing entry point:

```typescript
import { registerSpecSection } from "./tools/spec-section.js";
import { registerGlossaryLookup } from "./tools/glossary-lookup.js";
import { registerRouterForTask } from "./tools/router-for-task.js";
import { registerInvariantsSummary } from "./tools/invariants-summary.js";

// ... after existing registrations
registerSpecSection(server, registry);
registerGlossaryLookup(server, registry);
registerRouterForTask(server, registry);
registerInvariantsSummary(server, registry);
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Exact match for glossary, substring match for router | Glossary terms are well-defined names; router domains are described with natural language phrases that benefit from partial matching | Full fuzzy for everything (deferred to TECH-17c) |
| 2026-04-02 | `max_chars` default of 3000 | Roughly ~750 tokens — enough for a full section of typical length; prevents blowing up context on very large sections like §13 (~60 lines) | 1500 (too aggressive), 5000 (too permissive) |
| 2026-04-02 | Section lookup by ID then title fallback | Numeric IDs (`§3.3`, `13.4`) are the primary addressing scheme in this repo; title fallback handles unnumbered headings in smaller specs | Title-only (misses the §-numbering convention), line-range-only (unintuitive) |
| 2026-04-02 | Return `available_terms`/`available_sections` on not-found | Agents can self-correct without a second round-trip; small overhead for lists of 20-65 items | Return only error message (agent must call `list_specs` or `spec_outline` separately) |
| 2026-04-02 | Separate `glossary-parser.ts` and `table-parser.ts` | Glossary-specific column mapping is cleaner in its own module; generic table parser is reusable for router and future tools | All parsing in one file (would grow large) |
| 2026-04-02 | `invariants_summary` returns parsed lists, not raw Markdown | Structured arrays are easier for agents to reference; the file is short and well-structured enough for reliable list parsing | Return raw Markdown content (less useful for programmatic consumption) |
| 2026-04-02 | `router_for_task` scans **two** tables | Second table answers queries like “grid math” that never appear in “Task domain”; union of matches preserves backlog acceptance | First table only (misses geo quick reference) |
| 2026-04-02 | Numeric `section` prefers exact `sectionId` | Avoids accidentally returning `13.4` when user asked for whole `13` | Substring-first (ambiguous) |

## 7. Implementation Plan

### Phase 1 — Parser extensions

- [ ] Create `src/parser/table-parser.ts` with `parseMarkdownTables` function.
- [ ] Create `src/parser/glossary-parser.ts` with `parseGlossary` function.
- [ ] Extend `src/parser/markdown-parser.ts` with `extractSection` and `extractLines` functions.
- [ ] Extend `src/parser/types.ts` with `GlossaryEntry`, `RouterRow`, `TableRow` types.

### Phase 2 — Core tools

- [ ] Create `src/tools/spec-section.ts`: register `spec_section` tool with section lookup, truncation, error handling.
- [ ] Create `src/tools/glossary-lookup.ts`: register `glossary_lookup` tool with case-insensitive exact match.
- [ ] Create `src/tools/router-for-task.ts`: register `router_for_task` tool with case-insensitive substring match against agent-router table.
- [ ] Create `src/tools/invariants-summary.ts`: register `invariants_summary` tool parsing numbered/bulleted lists from the `.mdc` body.

### Phase 3 — Integration and wiring

- [ ] Update `src/index.ts` to import and register all four new tools.
- [ ] Restart server, verify all six tools appear in Cursor's MCP tool list.
- [ ] **Spec keys in this phase:** use full registry keys (e.g. `roads-system`, `isometric-geography-system`). Short aliases like `roads` / `geo` are **TECH-17c** only — do not rely on them in TECH-17b smoke tests.

### Phase 4 — Smoke test

- [ ] `spec_section({ spec: "isometric-geography-system", section: "13.4" })` — returns bridge rules section content only.
- [ ] `spec_section({ spec: "isometric-geography-system", section: "1" })` — returns §1 including sub-sections.
- [ ] `spec_section({ spec: "glossary", section: "Roads & Bridges" })` — returns the roads glossary category.
- [ ] `spec_section({ spec: "roads-system", section: "Land slope stroke policy" })` — title-based lookup works.
- [ ] `spec_section({ spec: "isometric-geography-system", section: "999" })` — error with available sections.
- [ ] `spec_section({ spec: "isometric-geography-system", section: "13", max_chars: 500 })` — truncated response.
- [ ] `glossary_lookup({ term: "wet run" })` — returns definition with spec reference.
- [ ] `glossary_lookup({ term: "WET RUN" })` — case-insensitive match works.
- [ ] `glossary_lookup({ term: "nonexistent" })` — error with all available terms.
- [ ] `router_for_task({ domain: "roads" })` — returns road-related routing row.
- [ ] `router_for_task({ domain: "save" })` — matches "Save / load" domain.
- [ ] `router_for_task({ domain: "grid math" })` — matches **second** table row ("Grid math, coordinates") with geo spec pointer.
- [ ] `router_for_task({ domain: "xyz" })` — error with available domains (include labels from **both** tables).
- [ ] `invariants_summary()` — returns 12 invariants and 9 guardrails as arrays.

## 8. Acceptance Criteria

- [ ] All six MCP tools (2 from TECH-17a + 4 new) appear in Cursor and are callable.
- [ ] `spec_section` resolves sections by numeric ID (`§3.3`, `13.4`) and by heading text.
- [ ] `spec_section` respects `max_chars` and sets `truncated: true` when content exceeds the limit.
- [ ] `glossary_lookup` finds any of the ~65 glossary terms by exact name (case-insensitive).
- [ ] `router_for_task` finds routing rows by substring match against **both** router tables (task domain column + geography “Need to understand” column).
- [ ] `invariants_summary` returns all 12 invariants and all 9 guardrails as structured arrays.
- [ ] All not-found errors return structured JSON with enough information for the agent to self-correct.
- [ ] An agent can answer "which spec for roads?" using only MCP tool calls (no file reads).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Ambiguous numeric section (`13` vs `13.4`) | Multiple headings share numeric prefix | Exact `sectionId` preference; candidate list on ambiguity |
| 2 | Two router tables, different meaning | Geo quick-reference is not “task domain” | Union matches; synthetic `specToRead` for second table |
| 3 | Markdown tables with `\|`, bold, code | Naive splitting breaks rows | `table-parser` escaped pipes + cell normalization |
| 4 | Invariant/guardrail **counts** in tests | `invariants.mdc` edited over time | Update assertions when lists change intentionally |

## 10. Lessons Learned

- **Slice tools beat monolith reads:** `spec_section` + `max_chars` saves context; `spec_outline` first cuts wrong-section retries.
- **Domain tables deserve thin parsers:** generic `table-parser` + `glossary-parser` / router mapping scales when columns evolve.
- **Structured errors are API:** `available_sections`, `available_terms`, and router `available_domains` enable self-correction.
- **Routing vs retrieval:** `router_for_task` = *where* to read; `spec_section` = *what* to read — keep both roles explicit.

## 11. Post-ship outcomes (consolidated)

- **Delivered:** Semantic IA tools (`spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`) as specified; later hardened in TECH-17c (fuzzy, aliases).
- **Issues:** See §9.
- **Living docs:** [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md); generic pattern [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md).

## 12. Implementation checklist (completed)

Phases 1–4 of §7 and §8 acceptance items were completed during TECH-17. Checkboxes in §7–§8 are left as originally written for historical traceability; treat §12 + §11 as the completion record.
