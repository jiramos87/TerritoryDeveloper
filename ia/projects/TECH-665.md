---
purpose: "TECH-665 — Import hygiene hooks."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.1.4
phases:
  - "Phase 2 — Hygiene manifest"
---
# TECH-665 — Import hygiene hooks

> **Issue:** [TECH-665](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Extend snapshot or sibling manifest with texture path hygiene fields so bake/import tooling can enforce PPU/pivot policy on allowlisted assets.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Emit path list aligned with catalog_sprite allowlist rules.
2. Embed or reference PPU/pivot per exploration §6.
3. Document consumer (editor script vs manual) as stub if not automated yet.

### 2.2 Non-Goals (Out of Scope)

1. Automated TextureImporter rewrite in this task (data-only hook).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Export includes texture hygiene metadata | Paths + PPU/pivot fields present for allowlisted rows |

## 4. Current State

### 4.1 Domain behavior

Sprite paths live in catalog; importer policy documented in exploration.

### 4.2 Systems map

tools/catalog-export manifest emitter, ia/specs/coding-conventions.md TextureImporter notes, exploration §6.

## 5. Proposed Design

### 5.1 Target behavior (product)

Downstream bake step can adjust imports without guessing paths from Unity-only heuristics.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Sidecar JSON or extra top-level section in snapshot; keep orthogonal to core asset rows.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Scope from Stage 2.1 orchestrator | Filed via stage-file | — |
| 2026-04-22 | Data in `importHygiene[]`; no Editor `TextureImporter` script in this task | Stage 2.1 data-only | Automated importer pass |

## 7. Implementation Plan

### Phase 2 — Hygiene manifest

- [ ] `importHygiene` top-level array in the main snapshot (sidecar optional); validate against sample rows from seeded catalog.
- [ ] Reference `ia/specs/coding-conventions.md` TextureImporter expectations in §6.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling | Node | `npm run validate:all` | |
| Manifest shape | Node | Fixture or snapshot sample | |

## 8. Acceptance Criteria

- [ ] Emit path list aligned with catalog_sprite allowlist rules.
- [ ] Embed or reference PPU/pivot per exploration §6.
- [ ] Document consumer (editor script vs manual) as stub if not automated yet.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Attach texture import hygiene data (paths + PPU/pivot hints) beside core snapshot so allowlisted `TextureImporter` passes stay data-driven.

### §Acceptance

- [ ] Manifest section lists sprite paths eligible for hygiene.
- [ ] PPU/pivot fields follow exploration §6 wording; gaps logged in §Findings.
- [ ] Consumer automation explicitly stubbed in §7 if no Editor script lands here.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| manifest_shape | fixture rows | JSON validates against TS type | node |

### §Examples

| Row kind | Hygiene fields |
|----------|----------------|
| allowlisted PNG | `texturePath`, `pixelsPerUnit`, `pivot` |

### §Mechanical Steps

#### Step 1 — expand §7 hygiene deliverables

**Goal:** Keep import policy out of Unity C# for this task.

**Edits:**

- `ia/projects/TECH-665.md` — **before**:

```
- [ ] Additional JSON section or sidecar file; validate against sample rows.
```

  **after**:

```
- [ ] Additional JSON section or sidecar file adjacent to main snapshot; validate against sample rows drawn from seeded catalog.
- [ ] Reference `ia/specs/coding-conventions.md` TextureImporter expectations in §6.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Fix spec tables if markdownlint surfaces issues during implement.

**MCP hints:** `plan_digest_resolve_anchor`

#### Step 2 — cite hygiene in exploration import bullet

**Goal:** Readers see data contract next to architecture tree.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before**:

```
              ├── GridAssetCatalog (boot loader, in-memory snapshot)
```

  **after**:

```
              ├── GridAssetCatalog (boot loader, in-memory snapshot)
              │     └── Import hygiene manifest (TECH-665) lists allowlisted texture paths + PPU/pivot hints for baker tooling
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Widen **before** snippet with parent bullets if multiple matches.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
