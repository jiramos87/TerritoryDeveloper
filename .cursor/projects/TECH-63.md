# TECH-63 — Spec pipeline layer C: Cursor Skills and project spec template

> **Issue:** [TECH-63](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

**Parent program:** [TECH-60](TECH-60.md) · **Prior layer:** [TECH-62](TECH-62.md) (**layer B**)
**Exploration:** [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md)

## 1. Summary

**Layer C** of [TECH-60](TECH-60.md): update **Cursor Skills** and **`.cursor/templates/project-spec-template.md`** so the spec pipeline includes **test contracts**, optional **impact** preflight, inter-phase checks, and validation manifest rows that reference **TECH-61** (**layer A**) scripts and **TECH-62** (**layer B**) MCP tools. Keep skills **thin**: **Tool recipe** order must point to **territory-ia** slices, not pasted **reference spec** bodies. Align **TECH-45**–**TECH-47** pointers in [`.cursor/skills/README.md`](../skills/README.md). **Does not** implement **`tools/mcp-ia-server`** handlers except when fixing **Skills**-blocked doc drift (route real code to **TECH-62**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Add **§7b Test Contracts** (or equivalent heading) to **`.cursor/templates/project-spec-template.md`** — table mapping acceptance to verifiable checks (**Node**, **golden** **JSON**, **manual**, future **UTF**).
2. Update **project-spec-kickoff** / **project-spec-implement** / **project-implementation-validation** / **project-spec-close** / **project-new** **SKILL.md** files as needed for: optional impact preflight, phase exit checks, **`validate:all`** reference, shipped **TECH-62** tool names in recipes.
3. **`.cursor/skills/README.md`**: index row or note for **TECH-60** program; keep **TECH-45**–**TECH-47** **Planned** line accurate.
4. Optional one-line **`AGENTS.md`** pointer if maintainers want global visibility.

### 2.2 Non-Goals (Out of Scope)

1. Implementing MCP handlers — **TECH-62**.
2. Implementing **Node** scripts — **TECH-61**.
3. Changing normative **reference spec** bodies unless **project-spec-close** migration requires it.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | I want the template to ask for test contracts next to acceptance. | Template section present and **PROJECT-SPEC-STRUCTURE** updated if section order changes. |
| 2 | Maintainer | I want skills to mention aggregate validation after MCP work. | **project-implementation-validation** lists **`validate:all`** when **TECH-61** ships it. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Skills README | [`.cursor/skills/README.md`](../skills/README.md) |
| Structure guide | [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](PROJECT-SPEC-STRUCTURE.md) |
| Template | [`.cursor/templates/project-spec-template.md`](../templates/project-spec-template.md) |
| Umbrella | [TECH-60](TECH-60.md) — **§7b** / **closeout** parser coordination |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed here)

If **PROJECT-SPEC-STRUCTURE** adds a normative section for **Test Contracts**, update the template comment and, when **closeout** must surface that section, extend **`project_spec_closeout-parse.ts`** (**`test_contracts`** key) in **TECH-62** — see [TECH-60](TECH-60.md) §5.2.

### 5.3 Method / algorithm notes (optional)

**Open Questions** in **project specs** remain **game logic** only per **AGENTS.md**; **§7b** is **tooling** / verification — do not conflate.

**Implementation waves:** (1) **Template** + **PROJECT-SPEC-STRUCTURE** + **Skills** copy that references *planned* **TECH-61**/**TECH-62** names; (2) after **TECH-61**/**TECH-62** ship, finalize **Tool recipe** tool order and **`validate:all`** row in **project-implementation-validation**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Layer **C** = Skills + template | Matches exploration layering | — |
| 2026-04-04 | Prefer **`## 7b. Test Contracts`** between **§7** and **§8** | Keeps **Open Questions** last; matches exploration; **closeout** parser may ignore until **TECH-62** adds **`test_contracts`** | New **§11** — would renumber **PROJECT-SPEC-STRUCTURE** list |

## 7. Implementation Plan

### Phase 1 — Template

- [ ] Add **Test Contracts** section to **project-spec-template.md** with placeholder table.
- [ ] Update **PROJECT-SPEC-STRUCTURE.md** if section order / rules change.

### Phase 2 — Skills

- [ ] **project-spec-kickoff**: test contract pass + optional impact preflight bullets in seed / body.
- [ ] **project-spec-implement**: phase exit / rollback note; reference **TECH-62** tools when shipped.
- [ ] **project-implementation-validation**: add **`validate:all`** row when **TECH-61** ships; **Unity** / **UTF** placeholders referencing **TECH-15**/**TECH-16**/**TECH-31**.
- [ ] **project-spec-close** / **project-new**: minimal cross-links if needed.

### Phase 3 — README / AGENTS

- [ ] **`.cursor/skills/README.md`**: program pointer to **TECH-60**; verify **TECH-45**–**TECH-47** line.
- [ ] Optional **`AGENTS.md`** bullet.

## 8. Acceptance Criteria

- [ ] Template ships **§7b** (or agreed heading) with clear mapping to **§8 Acceptance**.
- [ ] At least **two** **Skill** files materially updated for pipeline improvements.
- [ ] **Skills** remain **thin** (no large pasted spec excerpts).
- [ ] **`npm run validate:dead-project-specs`** passes (no broken **Spec:** links).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

**N/A — tooling only** for *game logic*; the following are **workflow** choices (resolve in **Decision Log** or during implementation):

1. **Kickoff** seed prompt: mandate **§7b** for all **BUG-**/**FEAT-**/**TECH-** specs with runtime touch, or only when **§8** lists testable claims — **Decision Log**.
2. Confirm **`## 7b. Test Contracts`** after **PROJECT-SPEC-STRUCTURE** update (**Decision Log** recommends 7b).
