---
purpose: "TECH-282 — Glossary rows + spec-index refresh for Zone S Stage 1.1 terms."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-282 — Glossary rows + spec-index refresh (Stage 1.1 terms)

> **Issue:** [TECH-282](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-17
> **Last updated:** 2026-04-17

## 1. Summary

Add 3 glossary rows to `ia/specs/glossary.md` — **Zone S**, **ZoneSubTypeRegistry**, **envelope (budget sense)**. Each row carries definition + authoritative spec link (points at forthcoming `ia/specs/economy-system.md` landing in Stage 3.3; cross-refs `docs/zone-s-economy-exploration.md` for now). Regenerate MCP glossary indexes via `npm run mcp-ia-index`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. 3 glossary rows land in `ia/specs/glossary.md` — correct alphabetical placement.
2. Each row has: Term, Definition (caveman prose, ≤2 sentences), Spec reference (forward-ref to `economy-system.md` + fallback to exploration doc).
3. `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json` regenerated via `npm run mcp-ia-index`.
4. `npm run validate:all` green (frontmatter + dead-link + IA indexes).

### 2.2 Non-Goals

1. No authoring of `ia/specs/economy-system.md` — that reference spec lands in Stage 3.3 (TECH-??? not yet filed).
2. No repoint of glossary rows once `economy-system.md` lands — repoint task = TECH-??? in Stage 3.3.
3. No coverage of 7 other terms (`BudgetAllocationService`, `BondLedgerService`, etc.) — those land as part of their owning stage.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | Resolve "Zone S" via MCP `glossary_lookup` | Tool returns row with definition + spec ref |
| 2 | Developer | Find canonical source for "envelope" | Glossary row points at exploration doc (+ forward-ref `economy-system.md`) |

## 4. Current State

### 4.1 Domain behavior

Terms "Zone S", "ZoneSubTypeRegistry", "envelope (budget sense)" appear in `ia/projects/zone-s-economy-master-plan.md` + `docs/zone-s-economy-exploration.md` but have no glossary row. Agents reading cold cannot resolve via MCP.

### 4.2 Systems map

- `ia/specs/glossary.md` — canonical glossary; alphabetical rows.
- `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json` — generated artifacts.
- `npm run mcp-ia-index` — regen script.
- `npm run validate:all` — chains dead-link + IA index drift checks.
- `docs/zone-s-economy-exploration.md` — fallback spec ref until `economy-system.md` lands.

## 5. Proposed Design

### 5.1 Target behavior

Seed 3 rows w/ caveman definitions aligned to `ia/rules/agent-output-caveman.md` §authoring. Regen runs as part of acceptance.

### 5.2 Row drafts (authoring starting point)

| Term | Definition | Spec reference |
|------|------------|---------------|
| Zone S | 4th zone channel (alongside R/C/I). State-owned buildings — 7 sub-types × 3 density tiers. Manual placement only in MVP; budget-gated via envelope allocator. | `docs/zone-s-economy-exploration.md` §Chosen Approach (forward-ref `ia/specs/economy-system.md`) |
| ZoneSubTypeRegistry | ScriptableObject cataloging 7 Zone S sub-types (police, fire, education, health, parks, public housing, public offices). Per-entry fields: id, displayName, prefab, baseCost, monthlyUpkeep, icon. | `docs/zone-s-economy-exploration.md` §IP-1 (forward-ref `ia/specs/economy-system.md`) |
| envelope (budget sense) | Per-sub-type monthly spending allowance. Global S monthly cap split 7 ways via pct sliders (sum-locked to 100%). `TryDraw` blocks spend when remaining < amount even if treasury has funds. | `docs/zone-s-economy-exploration.md` §IP-2 (forward-ref `ia/specs/economy-system.md`) |

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-17 | Forward-ref `economy-system.md` before it exists | Glossary rows must land in Stage 1.1 per orchestrator header rule; spec lands Stage 3.3 | Hold rows until Stage 3.3 (rejected — orchestrator explicitly distributes glossary landing across stages) |

## 7. Implementation Plan

### Phase 1 — Authoring

- [ ] Add 3 rows to `ia/specs/glossary.md` at correct alphabetical positions.
- [ ] Draft definitions per §5.2 (caveman prose).
- [ ] Point spec-ref col at exploration doc + forward-ref `economy-system.md`.

### Phase 2 — Index regen

- [ ] Run `npm run mcp-ia-index` — regenerates glossary-index.json + glossary-graph-index.json.
- [ ] Inspect diff; confirm 3 new term keys present.

### Phase 3 — Validation

- [ ] Run `npm run validate:all`.
- [ ] Confirm dead-link + index-drift checks green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Glossary row + index drift | Node | `npm run validate:all` | Chains validate:dead-project-specs + test:ia + generate:ia-indexes --check |
| MCP lookup resolves terms | MCP | `glossary_lookup "Zone S"` (manual spot check) | Post-reindex verification |

## 8. Acceptance Criteria

- [ ] 3 rows in `ia/specs/glossary.md` (alphabetical placement).
- [ ] Glossary indexes regenerated + committed.
- [ ] `npm run validate:all` green.
- [ ] `glossary_lookup "Zone S"` returns populated row (MCP spot check).

## Open Questions

1. None — IA-only task; no gameplay change.
