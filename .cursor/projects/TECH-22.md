# TECH-22 — Canonical terminology pass on reference specs

> **Issue:** [TECH-22](../../BACKLOG.md)
> **Status:** Completed (2026-04-02)
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

<!--
  Structure guide: PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: .cursor/specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

**Reference specs** under `.cursor/specs/` are the long-lived source for domain vocabulary, MCP retrieval, and backlog wording. This project aligns their prose with the **glossary** so search, **territory-ia** tools (`glossary_discover`, `glossary_lookup`, `spec_section`), and humans share one vocabulary. Work is editorial: replace ad-hoc synonyms with canonical terms, resolve glossary ↔ spec conflicts in favor of the **authoritative spec** text, and record any new concepts in both glossary and the right spec.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Every inventory **reference spec** file is reviewed in dependency order and marked complete in the checklist (see §7).
2. Non-canonical synonyms in those files are replaced with **glossary** terms where meaning matches (examples surfaced by discovery: informal “road” → **street** / **interstate**; “map edge” → **map border**).
3. No remaining contradiction between **glossary** definitions and **reference spec** meaning; glossary updated to defer or align per glossary header (spec wins).
4. New or clarified domain concepts appear as a **glossary** row **and** authoritative spec prose—not only in this project spec or backlog.
5. [`AGENTS.md`](../../AGENTS.md) spec inventory text stays consistent if filenames or described scope change.
6. [`tools/mcp-ia-server/src/config.ts`](../../tools/mcp-ia-server/src/config.ts) and MCP user docs are updated only if **spec keys** or aliases change.

### 2.2 Non-Goals (Out of Scope)

1. `.cursor/rules/*.mdc`, `docs/` outside MCP package docs, and `ARCHITECTURE.md` — follow-up issue if a broader pass is desired (per backlog).
2. C# code, Unity assets, or runtime behavior.
3. Rewriting specs for style-only polish without terminology benefit (keep edits purposeful).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer / agent | As someone reading or retrieving specs, I want one canonical term per concept so that search and MCP tools return consistent slices. | Glossary and specs agree; synonym cleanup done per §7 checklist. |
| 2 | Maintainer | As a spec author, I want clear rules for conflicts and new terms so that future edits do not reintroduce drift. | Decision log + optional deprecated-synonym table (§5) updated during the pass. |
| 3 | IA tooling user | As an agent using **territory-ia**, I want `glossary_discover` / `glossary_lookup` to match spec language. | Spec prose uses glossary table names for matched concepts (e.g. **Map border**, **Street (ordinary road)**, **Interstate**, **Road stroke**, **Road validation pipeline**). |

## 4. Current State

### 4.1 Domain behavior

This issue does not change **game logic**. It changes how **reference specs** name existing rules (terrain, **road stroke**, **map border**, water, simulation, persistence, UI, managers).

### 4.2 Systems map

| Area | Files |
|------|--------|
| Vocabulary source | `.cursor/specs/glossary.md` |
| Authoring meta | `.cursor/specs/REFERENCE-SPEC-STRUCTURE.md` |
| Canonical geography / roads vocabulary | `.cursor/specs/isometric-geography-system.md`, `.cursor/specs/roads-system.md`, `.cursor/specs/water-terrain-system.md` |
| Simulation / save / managers / UI | `.cursor/specs/simulation-system.md`, `.cursor/specs/persistence-system.md`, `.cursor/specs/managers-reference.md`, `.cursor/specs/ui-design-system.md` |
| Agent inventory | `AGENTS.md` (if needed) |
| MCP registry | `tools/mcp-ia-server/src/config.ts`, `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md` (only if keys/aliases change) |

### 4.3 Implementation investigation notes

- Use **`glossary_discover`** with rough phrases (e.g. “road street interstate map border”) to surface canonical **Term** rows and **spec** pointers before global search-and-replace.
- After discovery, use **`glossary_lookup`** on exact terms and **`spec_section`** on the indicated spec alias for context before editing a paragraph.
- Prefer repository search for known weak synonyms (e.g. `edge` near map bounds) and judge case-by-case: **map border** vs **cell** edge vs geometric “edge.”

## 5. Proposed Design

### 5.1 Target behavior (product)

**Reference specs** describe the same **domain** as today, but:

- Prose uses **glossary** table names for concepts that have a row (e.g. **Road stroke**, **Interstate**, **Map border**, **Reference spec**).
- Where both **street** and **interstate** apply, wording either names both explicitly or uses a defined umbrella phrase documented in glossary or meta spec—never an ambiguous informal “road” unless the glossary defines that alias.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Edit Markdown only in the files listed in §4.2 (and backlog **Files**).
- **Conflict resolution:** authoritative **reference spec** section wins; update **glossary** to match or to defer with a pointer (per glossary header).
- **New concepts:** add glossary row + spec section (not backlog-only).

### 5.3 Deprecated synonym → canonical table

The live table lives in [`REFERENCE-SPEC-STRUCTURE.md`](../specs/REFERENCE-SPEC-STRUCTURE.md) (authoring section). Agents should use **`glossary_discover`** when choosing terms.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec wins over glossary on meaning | Matches `glossary.md` header and backlog **Conflict rule** | Rewriting spec to match glossary without engineering review |

## 7. Implementation Plan

### Phase 1 — Glossary and geography foundation

- [x] `glossary.md` — align definitions with specs; fix any rows that contradict **isometric-geography-system.md**
- [x] `isometric-geography-system.md` — terminology pass (roads, water, **map border**, **road stroke**, etc.)

### Phase 2 — Roads and water specs

- [x] `roads-system.md`
- [x] `water-terrain-system.md`

### Phase 3 — Simulation, persistence, managers, UI

- [x] `simulation-system.md`
- [x] `persistence-system.md`
- [x] `managers-reference.md`
- [x] `ui-design-system.md`

### Phase 4 — Meta and tooling coherence

- [x] `REFERENCE-SPEC-STRUCTURE.md` — authoring terms + deprecated → canonical table
- [x] `AGENTS.md` — only if inventory or spec descriptions change *(no edit — inventory unchanged)*
- [x] `tools/mcp-ia-server/src/config.ts` and MCP docs — only if spec keys/aliases change *(no edit — keys/aliases unchanged; test fixture aligned with §13 heading)*

**Per-file workflow (repeat):**

1. Run **`glossary_discover`** on theme keywords for that file (roads, water, save, UI, etc.).
2. Read matching **`glossary_lookup`** entries and relevant **`spec_section`** slices.
3. Edit prose; add glossary rows + spec text for any new concept.
4. Tick the checkbox for that file in §8.

## 8. Acceptance Criteria

- [x] Checklist: every file in §7 Phases 1–4 marked reviewed (`glossary.md` through `REFERENCE-SPEC-STRUCTURE.md`, plus conditional `AGENTS.md` / MCP files).
- [x] No unresolved contradiction between **glossary** and **reference spec** meaning (spec authoritative).
- [x] `AGENTS.md` spec inventory and MCP spec keys/aliases remain coherent with `.cursor/specs/` reality.
- [x] New terms appear in **glossary** and authoritative spec, not only in TECH-22.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | MCP fuzzy test fixture used old §13 heading string | Heading renamed for TECH-22 | Updated `tools/mcp-ia-server/tests/parser/fuzzy.test.ts` sample title |

## 10. Lessons Learned

<!-- Fill at closure; migrate to REFERENCE-SPEC-STRUCTURE, glossary, or AGENTS.md as appropriate. -->

- …

## Open Questions (resolve before / during implementation)

1. When a sentence applies equally to **street** and **interstate**, should prose always write “**street** / **interstate**”, or is a single approved umbrella phrase (defined in **glossary**) acceptable to reduce repetition?
 Answer: Use a single approved umbrella phrase (defined in **glossary**) to reduce repetition.
2. For **map border** vs local geometry, should specs use “**map border**” only for the play-area boundary and reserve other wording for **cell** edges and **Moore** / **cardinal neighbor** contexts—documented in the optional synonym table? Answer: yes, this should be documented in the optional synonym table.
3. Should the **deprecated synonym → canonical** table live permanently in `REFERENCE-SPEC-STRUCTURE.md` after TECH-22 closes, or stay as a time-limited appendix here and then be folded into authoring guidance only? No. MCP tool should help find the right term when agents are editing specs.
