# TECH-17c — Robustness, Rule Tools, Fuzzy Matching, Tests, and Polish

> **Issue:** [TECH-17](../../BACKLOG.md)
> **Status:** Final (historical — TECH-17 **[x] in BACKLOG** 2026-04-02; see [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md))
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02 (§9–11 retrospective; **9 tools** incl. `backlog_issue`)
> **Sequence:** Part 3 of 3 (TECH-17a → TECH-17b → **TECH-17c**)
> **Prerequisite:** TECH-17b completed (6 tools working: `list_specs`, `spec_outline`, `spec_section`, `glossary_lookup`, `router_for_task`, `invariants_summary`).

## 1. Summary

Harden the MCP server for daily production use: add fuzzy/partial matching to glossary and section lookups, add dedicated rule tools (`list_rules`, `rule_content`), write comprehensive unit tests for the parser and tools, handle edge cases, and finalize documentation. **Shipped scope:** everything here plus follow-on **`backlog_issue`** (BACKLOG.md by id), **`spec_section` input aliases** for mis-keyed LLM calls, **`npm run verify`** (SDK client smoke test), registry count bumps for new `.mdc` rules, and repo-wide MCP docs (`docs/mcp-ia-server.md`, `AGENTS.md`, `terminology-consistency.mdc`). After this phase, TECH-17 is **complete** for file-backed IA v1.

### 1.1 Implementer guidance (AI agents)

Same as **TECH-17a §1.1**. For **fuzzy matching** and fixture data, prefer **real terms and rows** from `.cursor/specs/glossary.md` so tests reflect actual corpus shape and naming. Avoid synthetic “game” definitions that could diverge from project vocabulary.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Fuzzy/partial matching for `glossary_lookup` and `spec_section` — agents don't need exact term names or section IDs.
2. Two new tools: `list_rules` and `rule_content` for dedicated `.mdc` rule access.
3. Comprehensive unit tests for the parser module and MCP tools (extended as new tools register).
4. Edge case handling: truncation at line boundaries, encoding safety, graceful errors on malformed files.
5. Final README update, inline JSDoc on all public functions, and end-to-end verification checklist.
6. Performance: ensure no tool call takes >500ms on the current ~2000-line corpus.

### 2.2 Non-Goals (Out of Scope)

1. Full-text search across all documents — belongs to TECH-18.
2. File watching / hot reload — complexity not justified for ~15 files; restart server to pick up changes.
3. PostgreSQL integration — TECH-19.
4. Additional tools beyond the **nine** tools now shipped (`backlog_issue` added after original “8 tools” wording) — future follow-ups (e.g. glossary keyword discovery, **FEAT-45**) are separate backlog items.
5. CI/CD pipeline — the project is local-only tooling for now.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I misspelled a glossary term — I want the server to suggest the closest match | `glossary_lookup({ term: "hight map" })` returns suggestion: "Did you mean 'HeightMap'?" with the entry |
| 2 | AI agent | I want to search for a section by partial name | `spec_section({ spec: "geo", section: "bridge" })` finds §13.4 "Bridges and water approach" |
| 3 | AI agent | I want to list all cursor rules to know what guardrails exist | `list_rules()` returns all `.mdc` rule files with descriptions |
| 4 | AI agent | I want to read the roads rule content | `rule_content({ rule: "roads" })` returns the full body of `roads.mdc` |
| 5 | Developer | I want confidence the parser handles edge cases | Unit tests cover: empty files, no headings, nested headings, frontmatter-only, tables with empty cells, Unicode |
| 6 | Developer | I want to know the server won't hang on large files | Performance test reads `isometric-geography-system.md` (772 lines) in <100ms |

## 4. Current State

### 4.1 Relevant Files and Systems (from TECH-17a + TECH-17b)

| File / Path | Role in this context |
|-------------|----------------------|
| `tools/mcp-ia-server/src/parser/markdown-parser.ts` | Core parser — to be hardened |
| `tools/mcp-ia-server/src/parser/glossary-parser.ts` | Glossary parser — add fuzzy matching |
| `tools/mcp-ia-server/src/parser/table-parser.ts` | Table parser — add edge case handling |
| `tools/mcp-ia-server/src/parser/types.ts` | Shared types — extend if needed |
| `tools/mcp-ia-server/src/config.ts` | Registry — add spec-key alias support |
| `tools/mcp-ia-server/src/tools/*.ts` | 6 existing tool files — harden error handling |
| `.cursor/rules/*.mdc` (11 files at TECH-17 close; may grow) | Rule files served by `list_rules` / `rule_content` |

### 4.2 Known Edge Cases from Corpus

| Edge case | Where it occurs | Impact |
|-----------|----------------|--------|
| Spec key aliases | Agents may type "geo" instead of "isometric-geography-system" | `findEntryByKey` needs alias map |
| Very long section (§13, ~60 lines) | `isometric-geography-system.md` | Truncation must work at line boundary |
| Tables with inline Markdown (`**bold**`, `` `code` ``, pipes `\|`) | `glossary.md`, `agent-router.mdc` | Table parser must handle escaped pipes and inline formatting |
| Headings without numeric prefix | `managers-reference.md`, `simulation-system.md` | Slug-based ID generation must be stable |
| `.mdc` frontmatter has `globs` field with glob patterns | `coding-conventions.mdc` (`"**/*.cs"`) | `gray-matter` handles this; just don't interpret as regex |
| Heading `§14.5` appears after `§14.2` (non-sequential) | `isometric-geography-system.md` | Section lookup by `14.5` must work regardless of document order |
| User query vs glossary term shape | Glossary canonical term is **`HeightMap`**; users may type ``HeightMap[x,y]`` or paste invariant text | Normalize or strip ``[x,y]`` / punctuation for fuzzy match; do not claim the glossary row is literally named `HeightMap[x,y]` |

## 5. Proposed Design

### 5.1 New Files

```
tools/mcp-ia-server/
  src/
    parser/
      fuzzy.ts              # NEW: fuzzy string matching utility
    tools/
      list-rules.ts         # NEW: list_rules tool
      rule-content.ts       # NEW: rule_content tool
  tests/
    parser/
      markdown-parser.test.ts
      glossary-parser.test.ts
      table-parser.test.ts
      fuzzy.test.ts
    tools/
      list-specs.test.ts
      spec-outline.test.ts
      spec-section.test.ts
      glossary-lookup.test.ts
      router-for-task.test.ts
      invariants-summary.test.ts
      list-rules.test.ts
      rule-content.test.ts
    fixtures/
      sample-spec.md        # Minimal .md with known headings for deterministic testing
      sample-rule.mdc       # Minimal .mdc with frontmatter
      sample-glossary.md    # Minimal glossary with 3-4 entries
```

### 5.2 Fuzzy Matching Module (`fuzzy.ts`)

Lightweight fuzzy string matching without external dependencies. Uses **normalized Levenshtein distance** for short strings (terms, section IDs) and **case-insensitive substring** as primary fallback.

```typescript
interface FuzzyMatch<T> {
  item: T;
  score: number;   // 0 = perfect match, 1 = no similarity
  matchType: "exact" | "substring" | "fuzzy";
}

function fuzzyFind<T>(
  query: string,
  items: T[],
  getText: (item: T) => string,
  options?: { maxResults?: number; threshold?: number }
): FuzzyMatch<T>[];
```

**Algorithm:**
1. **Exact match** (case-insensitive): score = 0. Return immediately if found.
2. **Substring match**: query is a substring of the item text (or vice versa). Score = `1 - (query.length / text.length)`.
3. **Levenshtein**: compute normalized edit distance. Score = `editDistance / max(query.length, text.length)`.
4. Filter by `threshold` (default: 0.4 — rejects matches worse than 40% different).
5. Sort by score ascending, limit to `maxResults` (default: 5).

No npm dependency — Levenshtein on strings ≤100 chars is trivial to implement (the longest glossary term is ~25 chars).

### 5.3 Spec Key Aliases (`config.ts` extension)

Add a static alias map so agents can use short names:

```typescript
const SPEC_KEY_ALIASES: Record<string, string> = {
  "geo": "isometric-geography-system",
  "geography": "isometric-geography-system",
  "roads": "roads-system",
  "water": "water-terrain-system",
  "terrain": "water-terrain-system",
  "sim": "simulation-system",
  "simulation": "simulation-system",
  "persist": "persistence-system",
  "save": "persistence-system",
  "load": "persistence-system",
  "mgrs": "managers-reference",
  "managers": "managers-reference",
  "ui": "ui-design-system",
  "arch": "ARCHITECTURE",
  "agents": "AGENTS",
};
```

`findEntryByKey` checks aliases before falling back to exact/fuzzy filename match. **Alias values must match the actual `key` strings emitted by `buildRegistry()`** (e.g. `ARCHITECTURE` vs `architecture` — align with whatever TECH-17a implements for root-doc filenames).

### 5.4 Enhanced Tool Behavior

#### `glossary_lookup` — Fuzzy Enhancement

When exact match fails:
1. Run `fuzzyFind` against all glossary terms.
2. If top result has score < 0.3 (strong match): return the entry with `matchType: "fuzzy"` and `suggestion: "Did you mean '{term}'?"`.
3. If no strong match: return error with `suggestions: [top 3 fuzzy results with terms]` and `available_terms`.

Updated output shape adds optional fields:
```typescript
{
  // ... existing fields ...
  matchType?: "exact" | "fuzzy";
  suggestion?: string;
}
```

#### `spec_section` — Fuzzy Enhancement

When section ID lookup fails and title substring fails:
1. Run `fuzzyFind` against all heading titles in the document.
2. If top result has score < 0.3: use it and add `suggestion: "Matched to '{title}' (fuzzy)."`.
3. Otherwise: return error with `suggestions: [top 5 heading titles]`.

#### `spec_outline` and `spec_section` — Alias Support

Accept spec aliases (e.g. `spec: "geo"`) by resolving through the alias map before registry lookup.

### 5.5 New Tool Contracts

#### `list_rules`

| Property | Value |
|----------|-------|
| **Name** | `list_rules` |
| **Description** | List all Cursor rule files (.mdc) with their descriptions and metadata. |
| **Input schema** | `z.object({})` (no inputs) |
| **Output** | `{ rules: [{ key, fileName, description, alwaysApply, globs }...] }` |

**Example response:**
```json
{
  "rules": [
    { "key": "invariants", "fileName": "invariants.mdc", "description": "System invariants and guardrails — never violate", "alwaysApply": true, "globs": null },
    { "key": "coding-conventions", "fileName": "coding-conventions.mdc", "description": "C# coding standards for Territory Developer", "alwaysApply": false, "globs": "**/*.cs" }
  ]
}
```

#### `rule_content`

| Property | Value |
|----------|-------|
| **Name** | `rule_content` |
| **Description** | Retrieve the full content of a Cursor rule file (.mdc), without frontmatter. |
| **Input schema** | `z.object({ rule: z.string().describe("Key or filename (e.g. 'invariants', 'coding-conventions', 'roads')."), max_chars: z.number().optional().describe("Maximum characters to return. Default: 3000.") })` |
| **Output (success)** | `{ key, fileName, description, alwaysApply, globs, content, truncated, totalChars }` |
| **Output (not found)** | `{ error: "unknown_rule", message: "...", available_rules: [{ key, description }...] }` |

### 5.6 Test Strategy

**Framework:** Node.js built-in test runner (`node:test`) + `node:assert`. No extra test framework dependency.

**Test categories:**

| Category | What is tested | Approach |
|----------|---------------|----------|
| Parser unit tests | Heading extraction, tree building, line ranges, frontmatter, table parsing, section extraction | Use fixture files with known structure; assert heading counts, IDs, line ranges |
| Glossary parser tests | Term extraction, bold stripping, category assignment, edge cases (empty cells, escaped pipes) | Use `sample-glossary.md` fixture with known terms |
| Fuzzy matcher tests | Exact match, substring, Levenshtein, threshold filtering, sorting | Pure function tests with string pairs and expected scores |
| Tool integration tests | Each tool's success path, error path, truncation, fuzzy fallback | Mock the registry to point at fixture files; call tool handler functions directly |
| Edge case tests | Empty file, file with only frontmatter, heading with no body, very long section, non-UTF8 safety | Fixture files for each case |

**Running tests:**
```json
// package.json scripts
{
  "test": "node --import tsx --test tests/**/*.test.ts",
  "test:watch": "node --import tsx --test --watch tests/**/*.test.ts",
  "test:coverage": "c8 node --import tsx --test tests/**/*.test.ts"
}
```

**Coverage (acceptance >90% on parser modules):** Add devDependency **`c8`** (or use Node’s experimental coverage if preferred) and run `npm run test:coverage`. Gate on **`src/parser/**`** only (exclude `tools/*.ts` glue if needed). Document the exact command and threshold in `README.md`.

### 5.7 Performance Guardrails

- **Target:** All tool calls complete in <500ms on the current corpus.
- **Measurement:** Add a `console.error` timing log (stderr, not stdout — won't interfere with stdio transport) for each tool call during dev.
- **Mitigation if slow:** Cache `ParsedDocument` instances in a `Map<string, ParsedDocument>` keyed by file path, invalidated on server restart. The current corpus is ~2000 lines total — memory impact is negligible.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Custom Levenshtein (no npm dep) | Strings are short (<100 chars); implementation is ~20 lines; avoids adding a fuzzy search library | `fuse.js` (heavy for this use case), `fastest-levenshtein` npm (adds a dependency for 20 lines of code) |
| 2026-04-02 | Fuzzy threshold of 0.4 | Empirically: "hight map" → "HeightMap" has distance ~0.3; "shore" → "shore band" ~0.2; rejects truly unrelated terms | 0.5 (too permissive — returns bad matches), 0.2 (too strict — misses typos) |
| 2026-04-02 | `node:test` runner (no Jest/Vitest) | Zero additional dependencies; runs natively with Node 20+; sufficient for this project's test scope | Vitest (nice but adds dep and config), Jest (heavy, TS config overhead) |
| 2026-04-02 | Spec key aliases as static map | The corpus is stable (~15 files); aliases are predictable from glossary abbreviations; no need for dynamic alias resolution | Dynamic alias from frontmatter (overcomplicated), no aliases (agents must type full keys) |
| 2026-04-02 | `list_rules` + `rule_content` as separate tools (not reusing `list_specs`/`spec_section`) | Rules have unique metadata (alwaysApply, globs) that specs don't; dedicated tools have clearer semantics for agents | Overload `list_specs` with extra fields (confusing output), single `get_document` tool (too generic) |
| 2026-04-02 | ParsedDocument cache in memory | The corpus is small enough (~2000 lines) to hold all parsed documents in memory; avoids re-parsing on every call | No cache (simple but slower on repeated calls), file-watcher-based cache (unnecessary complexity) |
| 2026-04-02 | **`c8` for coverage** | Satisfies “>90% parser modules” with one devDependency; works with `node:test` + `tsx` | Rely on undocumented Node experimental flags only |

## 7. Implementation Plan

### Phase 1 — Fuzzy matching module

- [ ] Create `src/parser/fuzzy.ts` with `fuzzyFind` function and `levenshteinDistance` helper.
- [ ] Verify with inline manual tests: `"hight map"` → `"HeightMap"`, `"shore"` → `"shore band"`, `"xyz"` → no match.

### Phase 2 — Spec key aliases

- [ ] Add `SPEC_KEY_ALIASES` map to `src/config.ts`.
- [ ] Update `findEntryByKey` to resolve aliases before exact match.
- [ ] Verify: `findEntryByKey(registry, "geo")` returns the geography spec entry.

### Phase 3 — Enhance existing tools with fuzzy

- [ ] Update `src/tools/glossary-lookup.ts`: on exact miss, run `fuzzyFind` against terms; return suggestion or error with suggestions.
- [ ] Update `src/tools/spec-section.ts`: on section miss, run `fuzzyFind` against heading titles; return suggestion or error with suggestions.
- [ ] Update `src/tools/spec-outline.ts` and `src/tools/spec-section.ts`: resolve spec aliases in input.
- [ ] Verify all existing smoke tests from TECH-17b still pass with the new fuzzy layer.

### Phase 4 — New rule tools

- [ ] Create `src/tools/list-rules.ts`: register `list_rules` tool, filter registry for `category === "rule"`, extract frontmatter metadata.
- [ ] Create `src/tools/rule-content.ts`: register `rule_content` tool, read `.mdc` file, strip frontmatter, return body with truncation support.
- [ ] Update `src/index.ts` to import and register both new tools.
- [x] Verify: `list_rules()` returns all rule-category registry entries (11 at close); `rule_content({ rule: "roads" })` returns `roads.mdc` body.

### Phase 5 — In-memory parsed document cache

- [ ] Add `documentCache: Map<string, ParsedDocument>` to `src/config.ts` or a new `src/cache.ts`.
- [ ] Wrap `parseDocument` calls in tools with cache-check: if path is in cache, return cached; else parse, cache, return.
- [ ] Add `console.error` timing for tool calls (stderr only).
- [ ] Cache keys use the same absolute paths as `parseDocument`; **line numbers stay full-file** (TECH-17a parsing algorithm + §line numbering).

### Phase 6 — Unit tests

- [ ] Create test fixture files: `tests/fixtures/sample-spec.md`, `sample-rule.mdc`, `sample-glossary.md`.
- [ ] Write `tests/parser/markdown-parser.test.ts`: heading extraction, tree structure, line ranges, frontmatter, section extraction.
- [ ] Write `tests/parser/glossary-parser.test.ts`: term extraction, bold stripping, categories, empty cells.
- [ ] Write `tests/parser/table-parser.test.ts`: header detection, separator handling, data rows, escaped pipes.
- [ ] Write `tests/parser/fuzzy.test.ts`: exact, substring, Levenshtein, threshold, max results.
- [x] Write tool tests (`tests/tools/*.test.ts`): success path, error path, truncation, fuzzy fallback per tool (extended when `backlog_issue` / `spec_section` args landed).
- [ ] Write edge case tests: empty file, no headings, heading without body, very long section, frontmatter-only `.mdc`.
- [ ] Add `npm test` script to `package.json`.
- [ ] Add devDependency **`c8`**, script **`test:coverage`**, and verify ≥90% on `src/parser/**`.
- [ ] Run full test suite, fix any failures.

### Phase 7 — Documentation and polish

- [x] Update `tools/mcp-ia-server/README.md`: tool inventory (9 tools), architecture diagram, test + verify instructions.
- [ ] Add JSDoc (`/** ... */`) to all exported functions in parser modules and tool registration functions.
- [ ] Review all error messages for consistency and helpfulness.
- [ ] Final `.gitignore` review: ensure `node_modules/`, `dist/`, and test coverage artifacts are excluded.

### Phase 8 — End-to-end verification

- [x] Restart Cursor; verify full tool list (see `npm run verify`).
- [ ] **Scenario 1 — Road task workflow:**
  1. `router_for_task({ domain: "roads" })` → get spec list.
  2. `spec_outline({ spec: "roads" })` → using alias, get outline.
  3. `spec_section({ spec: "roads", section: "Land slope stroke policy" })` → get section content.
  4. `glossary_lookup({ term: "wet run" })` → get definition.
  5. `invariants_summary()` → check relevant invariants.
- [ ] **Scenario 2 — Typo recovery:**
  1. `glossary_lookup({ term: "hight map" })` → fuzzy suggestion: "HeightMap".
  2. `spec_section({ spec: "geo", section: "bridg" })` → fuzzy match to §13.4.
- [ ] **Scenario 3 — Rule inspection:**
  1. `list_rules()` → see all rules in registry (11 `.mdc` files at close).
  2. `rule_content({ rule: "coding-conventions" })` → get full rule body.
- [ ] **Scenario 4 — Truncation:**
  1. `spec_section({ spec: "geo", section: "13", max_chars: 500 })` → truncated with flag.
- [ ] **Scenario 5 — Error handling:**
  1. `spec_section({ spec: "nonexistent", section: "1" })` → unknown_spec with available keys.
  2. `glossary_lookup({ term: "xyzzy" })` → term_not_found with suggestions.
- [ ] Run `npm test` — all tests pass.
- [ ] Confirm no tool call exceeds 500ms.

## 8. Acceptance Criteria

- [x] All **9** MCP tools appear and are callable.
- [ ] `glossary_lookup` finds terms even with typos (e.g. "hight map" → "HeightMap").
- [ ] `spec_section` finds sections by partial heading text (e.g. "bridge" → §13.4).
- [ ] Spec key aliases work across all tools (`"geo"`, `"roads"`, `"sim"`, etc.).
- [ ] `list_rules` returns all 9 `.mdc` rules with accurate metadata.
- [ ] `rule_content` returns rule body without frontmatter, respects `max_chars`.
- [ ] `npm run test:coverage` (or equivalent) reports **≥90%** line coverage for `src/parser/**` (see §5.6).
- [ ] All tool calls on the current corpus complete in <500ms.
- [x] README documents all tools with examples.
- [ ] All exported functions have JSDoc.
- [ ] End-to-end scenarios 1-5 pass in Cursor.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | MCP **-32602** on `spec_section` | Models sent `key` / `section_heading` instead of `spec` / `section` | Widen Zod shape + `normalizeSpecSectionInput()` (aliases + numeric → string) |
| 2 | Live **BACKLOG.md** broke unit tests | Issue moved sections (e.g. BUG-37 → In Progress) | Relax assertions (e.g. section regex); avoid brittle title literals |
| 3 | **Registry count** drift | New rules (`terminology-consistency.mdc`, etc.) | Update `verify-mcp.ts` + `build-registry.test.ts` when IA grows |
| 4 | **Stdio + stdout** | Accidental `console.log` breaks JSON-RPC | Timing/logs on **stderr** only (`instrumentation.ts`) |
| 5 | **Coverage scope** | `c8` gate on `src/parser/**` only | Documented in README; tool glue excluded by design |

## 10. Lessons Learned

- **Fuzzy matching is cheap insurance** when LLMs typo glossary terms or section titles; keep thresholds tunable and tested with **real corpus** strings.
- **Spec aliases** (`geo` → file key) pay off immediately; document them beside `list_specs` / README so models learn short names.
- **`npm run verify`** catches regressions Cursor UI might miss (tool list, `list_specs` count, representative `callTool` paths).
- **LLMs ignore JSON schemas** sometimes — optional **field aliases** at the server are cheaper than perfect prompts.
- **Parse cache** makes repeated `spec_section` / `spec_outline` calls cheap enough that 500ms budgets are easy on this corpus.
- **Durable docs belong outside `.cursor/projects/`:** `docs/mcp-ia-server.md` + `AGENTS.md` + `agent-router.mdc` now carry operator truth; these TECH-17* files are **historical specs**.

## 11. TECH-17 Completion Checklist

| Item | Status |
|------|--------|
| **9 tools** in Cursor (`backlog_issue` + eight IA tools) | Done |
| Unit tests + verify script | Done |
| README + `docs/mcp-ia-server.md` | Done |
| `BACKLOG.md` TECH-17 **[x]** (2026-04-02) | Done |
| `agent-router.mdc` MCP subsection | Done |
| Delete `TECH-17a/b/c.md` | **Deferred** — retained as design history + retrospective (§9–11); delete when team no longer needs them |
| Migrate lessons | Done into `docs/mcp-ia-server.md`, `AGENTS.md`, `ARCHITECTURE.md`, `.cursor/rules/` |

## 12. Post-ship extensions (same program, after original §11 list)

- **`backlog_issue`:** parses `BACKLOG.md` blocks by issue id; not part of `list_specs` registry.
- **`spec_section` aliases:** `key`, `section_heading`, `heading`, `doc`, numeric `section`, `maxChars`.
- **`terminology-consistency.mdc`:** always-on Cursor rule; registry **21** entries, **11** rules — `verify-mcp` encodes counts.
- **Cross-repo pattern doc:** [`docs/mcp-markdown-ia-pattern.md`](../../docs/mcp-markdown-ia-pattern.md).
