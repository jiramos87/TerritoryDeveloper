# FEAT-45 — MCP tool: glossary term discovery from keywords

> **Issue:** [FEAT-45](../../BACKLOG.md) — *provisional id; add a matching backlog row before implementation if not present.*
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

<!--
  Structure guide: PROJECT-SPEC-STRUCTURE.md
  Terminology: .cursor/specs/glossary.md (spec wins if glossary differs)
-->

## 1. Summary

Add a **territory-ia** MCP tool that accepts **keyword-style input** (one string and/or multiple tokens) and returns the **most appropriate canonical glossary terms** for follow-up deep reading. Matching should use glossary **Term**, **Definition**, and **Spec** column text from each row, with an **optional** extension to score against **body content** of the linked spec document (or selected sections) when the product owner accepts the latency and complexity cost. The tool complements `glossary_lookup` (single-term exact/fuzzy on the **term name**): here the intent is **discovery** (“I have rough words; what should I look up next?”) rather than **lookup** (“I know the term; give me the row”).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Reduce agent mistakes where the model picks the wrong spec or invents ad-hoc vocabulary instead of **glossary-backed** terms.
2. Return a **small ranked list** of canonical **Term** strings plus **specReference** (and optionally **category**) so the next steps are clearly `glossary_lookup` and/or `spec_section` / `spec_outline`.
3. Reuse existing parsers (`parseGlossary`, registry resolution, fuzzy helpers) where possible; keep behavior **deterministic** and **testable** (fixtures, thresholds documented).
4. Align output wording with [`.cursor/rules/terminology-consistency.mdc`](../rules/terminology-consistency.mdc) and [AGENTS.md](../../AGENTS.md).

### 2.2 Non-Goals (Out of Scope — unless promoted via Open Questions)

1. Replacing or removing `glossary_lookup`.
2. Full-text search across **all** `.cursor/specs/*.md` independent of glossary rows (that is a different product).
3. Semantic / embedding search (unless explicitly chosen later).
4. Writing or modifying `glossary.md` or specs (read-only tool).
5. Guaranteeing “correct” domain answers without human review — the tool **suggests** terms; authoritative definitions remain in specs.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer / agent | I pass rough keywords (e.g. “manual road trace wipes neighbors”) and get glossary terms to read next. | Response lists ≥1 ranked term when a reasonable match exists; each item includes `term` and `specReference`. |
| 2 | Developer / agent | I can chain tools: discover → `glossary_lookup` → `spec_section`. | Response includes stable fields documented in section 5.1 (e.g. suggested `spec` key alias where resolvable). |
| 3 | Maintainer | Behavior is covered by unit tests with fixed glossary/fixture snippets. | Tests do not depend on network; thresholds documented in Decision Log. |

## 4. Current State

### 4.1 Domain behavior

N/A for player-facing simulation. **Information architecture:** the canonical vocabulary lives in [`.cursor/specs/glossary.md`](../specs/glossary.md); each row has **Term**, **Definition**, and **Spec** (reference to sections in authoritative specs).

### 4.2 Systems map

| Area | Role |
|------|------|
| `tools/mcp-ia-server/src/tools/glossary-lookup.ts` | Today: exact + fuzzy match primarily on **collapsed term** string; returns one best row or `term_not_found`. |
| `tools/mcp-ia-server/src/parser/glossary-parser.ts` | Produces `GlossaryEntry[]`: `term`, `definition`, `specReference`, `category`. |
| `tools/mcp-ia-server/src/config.js` | Registry keys, spec aliases (`geo`, `roads`, …). |
| `tools/mcp-ia-server/src/parser/fuzzy.js` | `fuzzyFind`, normalization utilities reused for typos. |
| `parseDocument` / `extractSection` | Needed only if **spec body** participates in scoring. |

### 4.3 Implementation investigation notes (optional)

- **Spec column** often contains abbreviated references (e.g. `geo §14.5`, multiple sections). Parsing that into `(specKey, sectionHint)` is non-trivial; may start by treating **Spec** as plain text for keyword overlap only.
- Loading **full spec files** per glossary row on every call could be slow (~8 large specs × many rows). Caching parsed docs (already present for `parseDocument`) amortizes cost; still cap work per request.

## 5. Proposed Design

### 5.1 Target behavior (product)

**Inputs (illustrative — final names in Decision Log):**

- Primary: `query` string (free text; may contain multiple words) **and/or** `keywords` string array.
- Optional: `max_results` (default TBD), flags TBD (e.g. `include_spec_body: boolean`).

**Output (illustrative):**

- `matches`: ordered list of `{ term, specReference, category, score?, matchReasons? }` where `matchReasons` might note `term` | `definition` | `spec_reference` | `spec_excerpt` (if implemented).
- `hint_next_tools`: static or generated guidance, e.g. “Call `glossary_lookup` with `term`” then `spec_section` with resolved alias.
- On no confident match: structured `error` or empty `matches` plus `suggestions` (similar to `glossary_lookup`).

**Scoring (high level — agent-owned detail):**

1. **Phase A (baseline):** Tokenize/normalize query; score each glossary row using weighted overlap + fuzzy signals against concatenation of `term`, `definition`, `specReference` (and category title if useful).
2. **Phase B (optional):** For top-K rows after Phase A, resolve linked spec file(s), optionally restrict to sections mentioned in `specReference`, add score from body keyword hits (with strong caps on bytes read).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- New file e.g. `src/tools/glossary-discover.ts` (name TBD) + `registerTool` in `index.ts`.
- Pure functions for ranking in `src/parser/` or colocated module for unit tests.
- Follow `spec_section` lesson: accept **parameter aliases** if models often misname fields (document in README).
- Version bump + `verify-mcp.ts` if tool list or counts change + update `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md`, [`.cursor/rules/agent-router.mdc`](../rules/agent-router.mdc) MCP subsection if default tool order changes.

### 5.3 Method / algorithm notes (optional)

- Reuse `normalizeGlossaryQuery` / collapse strategies consistent with `glossary_lookup` where it avoids surprising divergence between tools.
- Consider BM25-like or simple TF-IDF across glossary corpus for Phase A — **only if** Open Questions confirm desired ranking quality vs simplicity.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec filed under provisional **FEAT-45** | Next free FEAT id at time of writing; user may renumber. | TECH-17 sub-issue only (rejected: visible backlog helps scope). |

## 7. Implementation Plan

*Blocked until Open Questions below are resolved or explicitly deferred.*

1. **Requirements lock:** Tool name, input shape, output shape, Phase A vs B, limits.
2. **Parser / ranker:** Implement Phase A + tests (fixture glossary subset).
3. **MCP wiring:** Register tool, Zod schema, JSON errors, timing logs.
4. **Phase B (if approved):** Spec resolution from `specReference`, bounded body scan, tests with tiny fixture spec.
5. **Docs & verify:** README, `docs/mcp-ia-server.md`, registry counts, `npm test`, `npm run verify`.

## 8. Acceptance Criteria

1. With a fixed fixture, given query keywords known to appear only in a **definition** (not the term title), the tool still surfaces that glossary **Term** in the top N results.
2. Tool responses are valid JSON and documented in README.
3. No regression in existing `glossary_lookup` behavior.
4. `npm test` and `npm run verify` pass from `tools/mcp-ia-server/`.
5. If Phase B ships: worst-case single-call latency documented (or hard caps enforced) on developer hardware.

## 9. Issues Found During Development

| # | Severity | Description | Resolution |
|---|----------|-------------|------------|
| — | — | — | — |

## 10. Lessons Learned

*Fill at closure; migrate durable guidance to `AGENTS.md`, `docs/mcp-ia-server.md`, or glossary as needed.*

## 11. Open Questions (resolve before / during implementation)

*This feature is **tooling / IA**, not game simulation. Questions below mix **product choices** for the MCP and **technical** trade-offs; resolve or defer explicitly in the Decision Log.*

### 11.1 Tool naming and placement

1. Final **tool name** (`snake_case`): e.g. `glossary_discover`, `glossary_suggest`, `glossary_resolve_keywords` — which reads best in tool lists and avoids confusion with `glossary_lookup` and `router_for_task`?
2. Should **`router_for_task`** documentation steer certain queries to this tool instead of (or before) raw spec reads?

### 11.2 Input contract

3. Single **`query`** string only vs **`keywords: string[]`** vs both? Should the server split on whitespace / commas automatically?
4. Minimum and maximum query length; behavior for empty or ultra-long input (reject vs truncate).
5. **Locale / language:** English-only normalization, or future multilingual queries (out of scope for v1)?

### 11.3 Ranking and quality

6. What is **N** (`max_results`) default and hard cap?
7. Should scores be **exposed** to the model or hidden to reduce token gaming?
8. **Weights:** definition vs term vs spec column — fixed constants, or user-tunable via tool args (probably not)?
9. When multiple rows tie, prefer **shorter term**, **category** order, or stable alphabetical tie-break?

### 11.4 Use of linked spec **content** (Phase B)

10. Is Phase B **in scope for v1** or deferred? (Feasible but heavier: resolve `specReference` to files + sections.)
11. If Phase B: parse **§** references into `spec_section` calls internally, or only full-document keyword scan?
12. Acceptable **latency budget** per call (ms) and **max bytes** read from disk per request?
13. How to handle rows whose **Spec** cell lists **multiple** specs (e.g. geo + roads)?

### 11.5 Relationship to other systems

14. Overlap with future **TECH-18** (Postgres IA): should this tool’s ranking live in SQL later, and v1 remain Markdown-only for simplicity?
15. Should results include **precomputed `spec` alias** (e.g. `geo`) for the first linked doc, or leave resolution to the agent?

### 11.6 Governance

16. Add a formal **BACKLOG.md** row for **FEAT-45** with Files / Notes / Depends on **TECH-17** — yes or merge into a TECH follow-up?
17. Any **privacy / telemetry** concerns if queries are logged (stderr, future analytics)? Default: no persistent logging of queries beyond local dev.

---

*End of spec.*
