# TECH-67 — UI-as-code program (umbrella)

> **Issue:** [TECH-67](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

> **Parent program:** This row is the umbrella. **First child:** [TECH-68](TECH-68.md) — **as-built** documentation of **`ui-design-system.md`**. Further children (**runtime UI kit**, **Editor** / agent tooling) follow after **TECH-68** **§8** and workbook alignment — see [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md).

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

Territory Developer will treat **in-game UI** (Canvas / uGUI / TextMeshPro, **HUD**, **menus**, **panels**, **toolbars**) as a system that **developers and IDE agents** can reason about, generate, and refactor **primarily from the repository** — with **canonical patterns** in **`.cursor/specs/ui-design-system.md`**, optional **runtime C#** building blocks, **Unity Editor** automation, **CLI** / **`batchmode`** hooks where justified, and **Cursor Skills** (and optionally **territory-ia** tools) that encode safe recipes. The goal is to **minimize ad-hoc manual Editor steps** while staying aligned with **Unity**’s standard authoring model (Prefabs, Scenes, Inspector).

**Prerequisite:** The reference spec must first describe the **shipped** **UI** (**as-built**), not only aspirational **TBD** rows — owned by **TECH-68**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **As-built spec fidelity:** **`ui-design-system.md`** documents **current** **colors**, **typography**, **spacing**, **layout**, **Canvas** settings, and major **UX** surfaces (**TECH-68**).
2. **Spec authority (post-baseline):** Extend the same spec with **target** patterns where backlog issues (**TECH-07**, etc.) define future layout; keep **as-built** **vs** **target** explicit.
3. **Code-first workflows:** Land a **Territory Developer UI kit** direction (namespaces, assembly layout, prefab conventions) so new screens can be built from **C#** + **YAML** / **Prefab** diffs in the IDE (**child** issue **TBD**).
4. **Tooling parity:** **Editor** scripts, repo **tools/** helpers, and **Skills** so agents follow the same steps a human would — including validation and **machine-readable** summaries where useful (compare **`unity-development-context.md`** §10 pattern) (**child** issue **TBD**).
5. **Program structure:** Track **child** issues in **BACKLOG** + **`.cursor/projects/{ISSUE_ID}.md`**; keep [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) aligned.

### 2.2 Non-Goals (Out of Scope)

1. Replacing **Unity UI** with a third-party stack or custom renderer.
2. Defining **player-facing game rules** unrelated to presentation (those stay in gameplay specs and **FEAT-**/**BUG-** issues).
3. **CI** **Unity** test runner integration as a **Phase 0** requirement (optional follow-up per child issue).
4. Shipping **MCP** tools before **child** issues scope them (umbrella may list candidates only).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want **documented UI patterns** so that refactors stay consistent across **HUD** and **menus**. | **`ui-design-system.md`** sections referenced by **BACKLOG** / **Skills** cover **as-built** and **target** where applicable. |
| 2 | IDE agent | I want **`spec_section`** to return **real** layout and typography, not **TBD**. | **TECH-68** **§8** satisfied. |
| 3 | Maintainer | I want **exploration notes** and **child** specs so phases have clear boundaries. | Workbook + **TECH-68** + later **child** rows. |
| 4 | Developer | I want **Territory Developer** to help me add a new panel or button. | **Skill** + optional **MCP** — **child** issue **TBD** |

## 4. Current State

### 4.1 Domain behavior

**`ui-design-system.md`** lists foundations and patterns but many rows are still **TBD**; the game already has substantial **uGUI** implementation. **TECH-68** closes the **documentation / reality** gap. **Charter** and **inventory** live in [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md). **TECH-07** (**ControlPanel**) remains the primary **layout** refactor row; **soft** order: **TECH-68** before **TECH-07** for a diffable baseline.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Reference UI spec | `.cursor/specs/ui-design-system.md` |
| **As-built** pass | **TECH-68**, [`.cursor/projects/TECH-68.md`](TECH-68.md) |
| Toolbar / layout debt | **TECH-07**, [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) (**ControlPanel**) |
| Prefab / scene introspection | **TECH-33** |
| Editor diagnostics pattern | `.cursor/specs/unity-development-context.md` §10 |
| Exploration workbook | [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) |

### 4.3 Implementation investigation notes (optional)

- **Unity** APIs: `GameObject` + `RectTransform` construction in **Editor** vs runtime factories; **Prefab** variant strategy; **Canvas Scaler** reference resolutions.
- **Agent** ergonomics: whether **YAML** scene diffs or **Editor** menu commands reduce merge pain compared to manual hierarchy edits.
- **Overlap** with **TECH-59** (**EditorPrefs** staging) only if UI tooling needs **registry**-style workflows.

## 5. Proposed Design

### 5.1 Target behavior (product)

**Player-visible** layout and styling are **documented** (**as-built**) then **evolved** per **BACKLOG** issues; the reference spec remains the **single** **UI** normative doc for presentation patterns. Specific **HUD** logic changes remain in **FEAT-**/**BUG-** issues.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**Phased** delivery via **child** issues (**§7**). After **TECH-68**:

- A small **runtime** library for reusable **UI** primitives (names / folders **TBD** — future child).
- **Editor** folder scripts for **validate** / **scaffold** / **report** operations (future child).
- Optional **Node** + **`batchmode`** glue when headless validation is valuable.

### 5.3 Method / algorithm notes (optional)

_Order:_ **TECH-68** (spec) → **TECH-07** (optional parallel if baseline documented) → UI kit → Editor/agent tooling.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Capture exploration in **`projects/ui-as-code-exploration.md`** (not a **reference spec**) | Align stakeholders before bulk **`ui-design-system.md`** edits | Jump straight to spec edits only |
| 2026-04-04 | **TECH-68** = first child (**as-built** **`ui-design-system.md`**) | Agents and developers need **truth** before **target** refactors and tooling | Start with **UI kit** code without baseline |

## 7. Implementation Plan

### Phase 0 — Workbook and charter

- [ ] Keep [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) current as discoveries land.

### Phase 1 — As-built reference spec (**TECH-68**)

- [ ] Complete [`.cursor/projects/TECH-68.md`](TECH-68.md) **§7** / **§8** — **`ui-design-system.md`** describes **shipped** **UI**.

### Phase 2 — Target layout and migration (**TECH-07** and related)

- [ ] Execute **TECH-07** when ready; update spec **§3.3** so **target** matches implementation and **as-built** is updated post-merge.

### Phase 3 — Runtime UI kit (child issue TBD)

- [ ] Introduce **C#** primitives (panels, buttons, styles) per agreed architecture.

### Phase 4 — Editor / agent tooling (child issue TBD)

- [ ] **Editor** menus or **CLI** for scaffold / validate / export UI trees (shape **TBD**).
- [ ] **Cursor Skill(s)** + optional **MCP** tools documented in **`docs/mcp-ia-server.md`** when registered.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **BACKLOG** / **Spec** links valid | Node | `npm run validate:dead-project-specs` (repo root) | After **`Spec:`** or `.cursor/projects/` path edits |
| **IA** index after **`ui-design-system.md`** edits (**TECH-68**) | Node | `npm run generate:ia-indexes -- --check` | Per **TECH-68** **§7b** |
| **IA** / **MCP** package (if touched in a **child** issue) | Node | `npm run verify` under `tools/mcp-ia-server/` | Per **project-implementation-validation** skill |
| Runtime **UI** kit (**child** issue) | Manual / UTF | **TBD** | When scaffolding exists |

## 8. Acceptance Criteria

- [ ] **TECH-68** **§8** satisfied — **`ui-design-system.md`** **as-built** baseline complete.
- [ ] At least **one** additional **child** backlog row (beyond **TECH-68**) filed when **runtime kit** or **Editor**/**agent** tooling is scoped — or **Decision Log** records intentional deferral.
- [ ] **`AGENTS.md`** or **`.cursor/rules/agent-router.mdc`** updated only if the default **UI** task route changes (optional).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- _Fill at closure; migrate to **`ui-design-system.md`**, **`AGENTS.md`**, or **Skills**._

## Open Questions (resolve before / during implementation)

None — program charter, documentation, and developer/agent workflow only. **Player-facing** **UI** semantics and gameplay tie-ins belong in **child** **FEAT-**/**BUG-** specs under **`## Open Questions`** using **glossary** terms.
