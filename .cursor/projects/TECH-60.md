# TECH-60 — Spec pipeline & verification program (umbrella)

> **Issue:** [TECH-60](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

**Depends on:** none (prerequisite issues are separate **BACKLOG** rows; see §4.2)
**Child phases:** [TECH-61](TECH-61.md) → [TECH-62](TECH-62.md) → [TECH-63](TECH-63.md)
**Exploration:** [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md)

## 1. Summary

Umbrella program for improving the **spec-driven** development pipeline described in **`AGENTS.md`** (**.cursor/projects/** policy, **territory-ia** MCP first): **Layer A** (**TECH-61**) adds root **`npm run`** validation and optional repo scripts; **Layer B** (**TECH-62**) extends **territory-ia** tools to cut redundant round-trips and mis-routing; **Layer C** (**TECH-63**) updates Cursor **Skills** and the **project spec** template so **test contracts** and recipes stay aligned with shipped scripts and tools. Work is **tooling / documentation** only (see **Reference spec**, **Project spec**, **project-spec-close**, **project-implementation-validation** in **glossary**). Long-term **Unity** **UTF** and **grid**/**invariant** checks depend on prerequisite **BACKLOG** rows (§4.3), not on this umbrella alone.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Ship phased work on **TECH-61** (layer **A**), **TECH-62** (layer **B**), **TECH-63** (layer **C**) per the **BACKLOG** dependency narrative (**TECH-61** → **TECH-62** → **TECH-63**, with **soft** parallelism where each child **Notes** allow spikes).
2. Keep **canonical** vocabulary in **`.cursor/specs/glossary.md`** and **reference specs**; program issues improve *workflow*, not player **Save data** or normative geography unless a child issue explicitly requires it.
3. Document prerequisite **BACKLOG** rows and their **`.cursor/projects/*.md`** specs so **TECH-60** closure can verify cross-program alignment with **§ Compute-lib program** and **§ Agent ↔ Unity & MCP context lane**.
4. After all children **Complete**, run **project-spec-close** once for **TECH-60**: migrate durable lessons to **glossary** / **reference specs** / **`docs/`** as needed, then delete this spec and fix any stale links (**`npm run validate:dead-project-specs`**).

### 2.2 Non-Goals (Out of Scope)

1. Replacing **TECH-36** / **TECH-37**–**TECH-39** (**compute-lib** charter) — coordinate only.
2. **Postgres**-backed IA (**TECH-18**) — out of scope for **TECH-60** unless a child **Decision Log** records explicit overlap.
3. Player-facing gameplay features — use **FEAT-**/**BUG-** issues instead.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want one prioritized program row for spec-pipeline work so dependencies stay visible. | **BACKLOG** **§ Spec pipeline & verification program** lists **TECH-60**–**TECH-63** and prerequisites. |
| 2 | Agent | I want fewer redundant MCP round-trips and clearer guardrails when implementing **project specs**. | **TECH-62** / **TECH-63** **Acceptance** satisfied when shipped. |
| 3 | Developer | I want aggregate **`npm run`** validation after IA or MCP edits. | **TECH-61** ships documented aggregate command(s). |

## 4. Current State

### 4.1 Domain behavior

N/A — tooling and agent workflow only.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| **Exploration write-up** | [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md) |
| **Skills index** | [`.cursor/skills/README.md`](../skills/README.md) |
| **MCP docs** | [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |
| **IA tools CI** | [`.github/workflows/ia-tools.yml`](../../.github/workflows/ia-tools.yml) |
| **Child specs** | [TECH-61](TECH-61.md) (**layer A**), [TECH-62](TECH-62.md) (**layer B**), [TECH-63](TECH-63.md) (**layer C**) |
| **Glossary / IA** | **Project spec**, **project-spec-close**, **project-implementation-validation**, **Reference spec** — [`.cursor/specs/glossary.md`](../specs/glossary.md) |
| **Unity Editor exports** | [`.cursor/specs/unity-development-context.md`](../specs/unity-development-context.md) **§10** (when verification uses **`tools/reports/`**) |

### 4.3 Prerequisites (canonical **BACKLOG** rows — existing project specs)

These issues are **not** duplicated under **§ Spec pipeline**; they remain in **§ Compute-lib program** or **§ Agent ↔ Unity & MCP context lane**. Each has an initial **project spec** (already on disk); this program **tracks** them for **Unity** test harnesses, **golden** **JSON**, **invariant** surfaces, and **project spec** id hygiene.

| Issue | Role for **TECH-60** | Spec |
|-------|------------------------|------|
| **TECH-37** | **compute-lib**, **Zod**/**TS** schemas, pilot MCP, **Node** ↔ **golden** tests | [TECH-37](TECH-37.md) |
| **TECH-38** | **Pure** **C#** extractions, **`tools/reports/`**, **UTF** / batch hooks | [TECH-38](TECH-38.md) |
| **TECH-15** | **New Game** / **geography initialization** **JSON** profiler output | [TECH-15](TECH-15.md) |
| **TECH-16** | **Simulation tick** harness **JSON**, **ProfilerMarker** names | [TECH-16](TECH-16.md) |
| **TECH-31** | **AUTO** / **simulation** fixtures / **Play Mode** regression capsules | [TECH-31](TECH-31.md) |
| **TECH-35** | Optional **invariant** fuzzing spike (predicates from **invariants.mdc**) | [TECH-35](TECH-35.md) |
| **TECH-30** | Validate **BACKLOG** ids referenced in **`.cursor/projects/*.md`** | [TECH-30](TECH-30.md) |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — developer / agent workflow only.

### 5.2 Architecture / implementation (agent-owned unless fixed here)

**Layer definitions (must stay aligned with child specs):**

| Layer | Issue | Delivers | Does not |
|-------|-------|----------|----------|
| **A** | **TECH-61** | Root **`npm run`** aggregates (**`validate:all`** or agreed name), optional **`tools/`** scripts (**impact** / **diff** / **backlog-deps**), stubs for **`test:invariants`** / **`test:golden`** when fixtures exist | **`registerTool`** in **territory-ia** |
| **B** | **TECH-62** | MCP tool and handler changes; **`npm run test:ia`** / **`verify`** green; **`docs/mcp-ia-server.md`** | Root aggregate **`npm run`** without MCP code paths |
| **C** | **TECH-63** | **`.cursor/templates/project-spec-template.md`**, **`.cursor/skills/*.md`**, **`.cursor/skills/README.md`** program row; optional **`AGENTS.md`** pointer | Parser/handler code in **`tools/mcp-ia-server`** (unless **TECH-62** owns a shared parser change triggered by template needs) |

**Execution rules:**

- **Soft start:** **BACKLOG** allows **TECH-62** spikes before **TECH-61** **Completed**; **TECH-63** may draft template/**Skill** text in parallel — shipped **Tool recipe** order must be updated when **TECH-62** tools go live.
- **TECH-48** vs **TECH-62:** Path-based discovery and composite reads may overlap; record merge/split in **TECH-62** **Decision Log** so **TECH-48** **Notes** stay honest.
- **Closeout / digest:** Optional **§7b Test Contracts** (template) may require extending **`project_spec_closeout-parse.ts`** (**`test_contracts`** key) in **TECH-62** or accepting omission from **`project_spec_closeout_digest`** until then — see **TECH-63** **Decision Log**.

### 5.3 Method / algorithm notes (optional)

**IA tools** **Node** job order (dead specs → **`npm ci`** under **`tools/mcp-ia-server`** → **`npm test`** → **`validate:fixtures`** → **`generate:ia-indexes --check`**) is the source of truth for **layer A** aggregate scripts; root **`validate:all`** should mirror that subset (see **TECH-61**).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Program ids **TECH-60**–**TECH-63** | Next free **TECH-** band after **TECH-59** | Single monolithic issue — rejected (too large) |
| 2026-04-04 | Three layers **A/B/C** map to **TECH-61**/**62**/**63** | Clear separation: scripts vs MCP vs **Skills**/template | More than three children — rejected (noise) |
| 2026-04-04 | **Kickoff** enrichment | Align umbrella with **glossary**, **BACKLOG**, and child **Acceptance** | — |

## 7. Implementation Plan

### Phase 1 — Charter and backlog

- [x] Add **§ Spec pipeline & verification program** to **BACKLOG.md** with **TECH-60**–**TECH-63**.
- [x] Create umbrella + child **project specs** (this file and **TECH-61**–**TECH-63**).
- [x] Cross-link prerequisite specs (§4.3) with **TECH-60** pointer where missing.

### Phase 2 — Layer A (**TECH-61**)

- [ ] Ship **`validate:all`** (or agreed name) and document parity with [`.github/workflows/ia-tools.yml`](../../.github/workflows/ia-tools.yml).
- [ ] Record deferred optional scripts (**impact** / **diff** / **backlog-deps** / **invariant** stubs) in **TECH-61** **Decision Log** if not implemented.

### Phase 3 — Layer B (**TECH-62**)

- [ ] Ship ≥1 MCP improvement per **TECH-62** §8; reconcile with **TECH-48** in **Decision Log**.
- [ ] If **§7b** needs **closeout** parsing, implement or defer parser extension in **TECH-62** (coordinate **TECH-63**).

### Phase 4 — Layer C (**TECH-63**)

- [ ] Ship **project spec** template update (**§7b** or agreed heading) and **PROJECT-SPEC-STRUCTURE** alignment.
- [ ] Update pipeline **Skills** (**kickoff**, **implement**, **validation**, **close**, **project-new** as needed) and **`.cursor/skills/README.md`** index for **TECH-60**.

### Phase 5 — Program closure

- [ ] Verify **TECH-61**, **TECH-62**, **TECH-63** each **Completed** in **BACKLOG.md** (user-confirmed).
- [ ] **project-spec-close** for **TECH-60**: migrate durable lessons to **glossary** / **reference specs** / **`docs/`** / rules if needed.
- [ ] Delete **`.cursor/projects/TECH-60.md`**; run **`npm run validate:dead-project-specs`**; move **TECH-60** row to **Completed** (user-confirmed).

## 8. Acceptance Criteria

- [ ] **TECH-61**, **TECH-62**, and **TECH-63** each moved to **Completed** with user verification.
- [ ] **BACKLOG** program row **Acceptance** line for **TECH-60** satisfied.
- [ ] No stale **`.cursor/projects/TECH-60.md`** links in durable docs after closure.
- [ ] **Layer A/B/C** boundaries respected: no MCP **`registerTool`** in **TECH-61**; no aggregate-only root script as sole deliverable of **TECH-62**; no **`tools/mcp-ia-server`** handler work listed only under **TECH-63**.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

**N/A — tooling only.** Technical choices (**script names**, MCP tool pick list, **§7b** heading, **closeout** parser) live in **TECH-61**–**TECH-63** **Open Questions** / **Decision Log** per [.cursor/projects/PROJECT-SPEC-STRUCTURE.md](PROJECT-SPEC-STRUCTURE.md).
