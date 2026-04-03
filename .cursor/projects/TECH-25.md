# TECH-25 — Incremental authoring milestones for `unity-development-context.md`

> **Issue:** [TECH-25](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related:** [`.cursor/specs/unity-development-context.md`](../specs/unity-development-context.md) (baseline from **TECH-20** completed); [projects/agent-friendly-tasks-with-territory-ia-context.md](../../projects/agent-friendly-tasks-with-territory-ia-context.md) §4; [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) row **7**; **TECH-18** **`unity_context_section`**; **TECH-26** mechanical scans; **TECH-28** Editor **agent-context** export.

## 1. Summary

**TECH-25** tracks **optional, slice-sized** improvements to the **reference spec** [`.cursor/specs/unity-development-context.md`](../specs/unity-development-context.md). The umbrella document already covers **`MonoBehaviour`** lifecycle, **Inspector** / **`[SerializeField]`**, **`FindObjectOfType`** policy, **Script Execution Order**, and 2D renderer **`sortingOrder`** / **Sorting Layers** vs script-driven **Sorting order** (deferring formulas to [`isometric-geography-system.md`](../specs/isometric-geography-system.md) §7). This issue does **not** redefine **gameplay** or **simulation** rules; it deepens **in-repo** examples, cross-links, and maintainer guidance so agents rely on **territory-ia** and **`.cursor/`** docs with less drift.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Land **independent** documentation PRs (“milestones”) that each improve one slice of **`unity-development-context.md`** without requiring a monolithic rewrite.
2. Keep every slice **English**, **glossary-aligned** for **domain** terms ([`glossary.md`](../specs/glossary.md); spec wins if glossary differs), and consistent with [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) and [`.cursor/rules/coding-conventions.mdc`](../rules/coding-conventions.mdc).
3. Add or refresh **real** `Assets/Scripts/` pointers where a section is thin (**class** name + path under `Managers/GameManagers/`, `Managers/UnitManagers/`, or `Controllers/` per [`.cursor/rules/project-overview.mdc`](../rules/project-overview.mdc)).
4. After substantive edits, run **`cd tools/mcp-ia-server && npm run verify`** so **`list_specs`**, **`spec_outline`**, and **`spec_section`** still resolve **`unity-development-context`** (and aliases **`unity`** / **`unityctx`** in [`tools/mcp-ia-server/src/config.ts`](../../tools/mcp-ia-server/src/config.ts)).

### 2.2 Non-Goals (Out of Scope)

1. Changing **simulation tick** behavior, **save/load**, **road preparation family**, or **Sorting order** **math** — authoritative behavior stays in [`isometric-geography-system.md`](../specs/isometric-geography-system.md) and domain **reference specs**.
2. Replacing **Unity** vendor documentation for APIs not constrained by this repo.
3. Mandatory migration of all exposed **`public`** **Manager** reference fields to **`[SerializeField] private`** (document only; refactors belong to other **BACKLOG** items).
4. Duplicating [`managers-reference.md`](../specs/managers-reference.md); **link** to it when listing **Manager** responsibilities instead of copying tables.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want richer **Unity** onboarding from one spec without full-file reads. | At least one milestone adds concrete file pointers or a “where to look next” subsection without breaking **`spec_section`** anchors on **`##` numbered headings**. |
| 2 | Maintainer | I want to ship doc polish in small PRs. | Each **§7** milestone can merge alone; **Decision Log** records what shipped. |
| 3 | Tooling author | I want the **Unity** spec to mention mechanical checks when they exist. | When **TECH-26** (or CI) ships relevant scanners, **unity-development-context** §3 or §7 references them with **issue id** and script path. |

## 4. Current State

### 4.1 Domain behavior

N/A — documentation / agent workflow only. No **Cell**, **HeightMap**, **WaterMap**, or **simulation tick** behavior change.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Target doc | [`.cursor/specs/unity-development-context.md`](../specs/unity-development-context.md) |
| Router | [`.cursor/rules/agent-router.mdc`](../rules/agent-router.mdc) — **Unity** / **MonoBehaviour** row (defer **Sorting order** formula to **geography** §7) |
| MCP | Registry key **`unity-development-context`**; aliases **`unity`**, **`unityctx`** — [`tools/mcp-ia-server/src/config.ts`](../../tools/mcp-ia-server/src/config.ts) **`SPEC_KEY_ALIASES`** |
| Completed umbrella | **TECH-20** in **`BACKLOG.md`** — **Completed (last 30 days)** |
| Planning source | `projects/agent-friendly-tasks-with-territory-ia-context.md` §4 |
| **Sorting order** code entry points (for M4 planning) | [`GridManager.cs`](../../Assets/Scripts/Managers/GameManagers/GridManager.cs) (`#region Sorting Order`, **`GridSortingOrderService`**); [`GridSortingOrderService.cs`](../../Assets/Scripts/Managers/GameManagers/GridSortingOrderService.cs) |

### 4.3 Implementation investigation notes (optional)

- Prefer **territory-ia** **`spec_section`** with `spec`: **`unity`** or **`unity-development-context`** and `section`: **`2`** … **`9`** to read slices before editing.
- If top-level **`##` headings** change, `rg 'unity-development-context|#2-monobehaviour|\\(§2\\)'` across **`.cursor/`**, **`docs/`**, **`projects/`** for stale anchors; update **agent-router** only when the **Task domain** cell text must change ([`REFERENCE-SPEC-STRUCTURE.md`](../specs/REFERENCE-SPEC-STRUCTURE.md) — **Conventions** item **6**, **`router_for_task`** token collision avoidance).
- Optional: one short “**Related managers**” paragraph under §2 or §6 pointing to [`managers-reference.md`](../specs/managers-reference.md); no table duplication.

## 5. Proposed Design

### 5.1 Target behavior (product)

Contributors and agents continue to treat **`unity-development-context.md`** as the first-party **Unity** + **Editor** convention doc for this repo; milestones only **improve clarity and traceability**, not **gameplay** rules.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Suggested milestone slices (pick any order; skip what is already sufficient):

| Milestone | Focus | Suggested edits |
|-----------|--------|-----------------|
| **M1** | **Lifecycle** §2 | More **`Awake`** / **`Start`** / **`OnEnable`** notes; optional **`Coroutine`** / **`Invoke`** one-liner only if patterns recur under `Controllers/` (cite real types). |
| **M2** | **Inspector** §3–§4 | Do **not** document **Addressables** until `rg Addressables Assets/Scripts` finds usage (currently none). Expand missing **Mono Script** / **prefab** recovery at a high level. |
| **M3** | **Execution order** §6 | Link **`GeographyManager`** or other stable **New Game** / **geography initialization** entry if appropriate; else narrative + **BUG-16**-class pointer only. |
| **M4** | **2D vs Sorting order** §5 | Point to **`GridManager`** + **`GridSortingOrderService`** for *when* **Sorting order** is applied; never paste **geography** §7.1 formula here. |
| **M5** | **Anti-patterns** §7 | Add **`GetComponent` in `Update`** row if distinct from **`FindObjectOfType`** per-frame ban; align with **invariants**. |
| **M6** | **Glossary alignment** §9 | Refresh listed terms when §1–§8 introduce new **domain** nouns. |
| **M7** | **Cross-doc** §1 | One paragraph: **`unity_context_section`** (**TECH-18**), **TECH-28** JSON export, **TECH-26** scans — only when those land or already documented in **BACKLOG**. |

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Project spec created | Tracks optional milestones post-**TECH-20** | — |
| 2026-04-02 | Milestones are **optional** slices | **BACKLOG** describes optional depth / polish | Single mandatory mega-PR (rejected) |
| 2026-04-02 | No **Addressables** prose without code | Repo snapshot has no **Addressables** in `Assets/Scripts` | Speculative stack docs (rejected) |
| 2026-04-02 | **Open Questions** answered in § below | Policy clarity for implementers | Blank **Open Questions** (rejected) |

## 7. Implementation Plan

### Phase 0 — Baseline check

1. Read [`.cursor/specs/unity-development-context.md`](../specs/unity-development-context.md) end-to-end (TOC + §1–§9).
2. From repo root: `cd tools/mcp-ia-server && npm run verify` — record success; this is the regression gate after each milestone PR that touches **`.cursor/specs/`** or **BACKLOG** used by **`backlog_issue`** smoke test.
3. Optional: **territory-ia** **`spec_outline`** with `spec`: **`unity`** — capture heading tree; after edits, re-run and confirm numbered **`##` sections** unchanged unless intentionally renamed.
4. Skim [`.cursor/rules/invariants.mdc`](../rules/invariants.mdc) and [`.cursor/specs/glossary.md`](../specs/glossary.md) for terms you will cite (**Sorting order**, **GridManager**, **Cell**, etc.).

### Phase 1 — Milestone PRs (execute any subset; one milestone per PR recommended)

#### M1 — Lifecycle (reference spec §2)

- [ ] Search: `rg "void (Awake|Start|OnEnable)\\(" Assets/Scripts/Managers Assets/Scripts/Controllers --glob '*.cs' | head` — pick **1–2** representative **Managers** not already cited in §2 (avoid duplicating **`WaterManager`** unless adding new insight).
- [ ] Edit **only** §2 (and §6 **only** if lifecycle text must cross-reference **Script Execution Order**); keep **`## 2. MonoBehaviour lifecycle`** anchor string intact.
- [ ] If mentioning **`Coroutine`**: cite a real **Controller** or **Manager** that starts one; otherwise omit.
- [ ] Run **`npm run verify`**; commit.

#### M2 — Inspector, **SerializeField**, scenes / prefabs (§3–§4)

- [ ] Confirm no **Addressables**: `rg Addressables Assets/Scripts --glob '*.cs'` — if zero hits, do not add **Addressables** prose.
- [ ] Strengthen §3 with one more **`[SerializeField] private` + `FindObjectOfType`** example from `Assets/Scripts/Managers/` if found via `rg SerializeField.*\\n.*FindObjectOfType` (multiline) or manual scan.
- [ ] §4: add 2–4 bullets on **prefab** / **scene** **YAML** / **.meta** cautions without duplicating **`coding-conventions.mdc`** wholesale — link to rule file.
- [ ] Run **`npm run verify`**; commit.

#### M3 — Script Execution Order and initialization (§6)

- [ ] Search bootstrap: `rg "GeographyManager|New Game|Initialize" Assets/Scripts/Managers/GameManagers --glob '*.cs' | head -40` — identify a stable coordinator type for **geography initialization** if one clearly owns startup ordering; cite **file path + class** in §6. If none is stable, add a sentence pointing to **simulation** / **geography** specs for tick vs init distinction without inventing call order.
- [ ] Cross-link **BUG-16** in **`BACKLOG.md`** if the issue still exists (path only; no **gameplay** redefinition).
- [ ] Run **`npm run verify`**; commit.

#### M4 — 2D **`sortingOrder`** / **Sorting Layers** vs **Sorting order** (§5)

- [ ] Read [`isometric-geography-system.md`](../specs/isometric-geography-system.md) §7 via **`spec_section`** (`geo`, section **`7`**) — do not copy formulas into **unity-development-context**.
- [ ] In §5, add bullets: **`GridManager`** delegates to **`GridSortingOrderService`**; point to [`GridManager.cs`](../../Assets/Scripts/Managers/GameManagers/GridManager.cs) `#region Sorting Order` and [`GridSortingOrderService.cs`](../../Assets/Scripts/Managers/GameManagers/GridSortingOrderService.cs) for “where **Sorting order** is computed/applied” in code.
- [ ] Explicitly defer **typeOffset**, **depthOrder**, **heightOrder** vocabulary to **geography** §7 / **glossary** **Sorting order**.
- [ ] Run **`npm run verify`**; commit.

#### M5 — Anti-patterns (§7)

- [ ] Add table row or bullet: **`GetComponent<T>()`** (or **`GetComponentInChildren`**) in **`Update`** / per-frame paths — same **invariant** spirit as **`FindObjectOfType`** (cache in **`Awake`** / **`Start`** / init).
- [ ] Re-read **invariants** — no new **singletons** beyond **`GameNotificationManager`**; no dilution of **road preparation family** / **`GridManager`** helper extraction rules.
- [ ] Run **`npm run verify`**; commit.

#### M6 — Glossary alignment (§9)

- [ ] List terms used in updated §1–§8; for each **domain** term, confirm **`glossary.md`** has a row or add row + authoritative spec pointer per [`.cursor/rules/terminology-consistency.mdc`](../rules/terminology-consistency.mdc).
- [ ] Update §9 bullet list to match; **Unity**-only terms (**Prefab**, **Inspector**) need not appear in **glossary** unless already there.
- [ ] Run **`npm run verify`**; commit.

#### M7 — Cross-doc pointers (§1)

- [ ] When **TECH-26** / **TECH-28** / **`unity_context_section`** ship, add one short §1 paragraph: issue id, output path (e.g. `tools/reports/`), MCP tool name — link **`BACKLOG.md`** entries.
- [ ] If not yet shipped, skip M7 or add “Planned:” sentence only if **BACKLOG** still tracks the item (avoid promising dates).
- [ ] Run **`npm run verify`**; commit.

### Phase 2 — Closure hygiene

- [ ] **Decision Log** (§6): table row listing milestones merged (**M1**–**M7** or “none — baseline sufficient”) with date.
- [ ] Grep `TECH-25` in `projects/agent-friendly-tasks-with-territory-ia-context.md` — if text implies primary **Unity** doc work, tighten to “optional polish on **unity-development-context**”.
- [ ] Human verification: **English** prose, no **gameplay** contradiction, **agent-router** unchanged unless **Decision Log** explains **Task domain** wording change (token collision rules).
- [ ] **`BACKLOG.md`**: move **TECH-25** to **Completed (last 30 days)** with date; delete **`.cursor/projects/TECH-25.md`** per [PROJECT-SPEC-STRUCTURE.md](PROJECT-SPEC-STRUCTURE.md); migrate §10 **Lessons Learned** to canonical docs if any.

## 8. Acceptance Criteria

- [ ] Each merged milestone keeps **`unity-development-context.md`** in **English** with stable top-level **`## N.`** anchors (or **Decision Log** documents intentional anchor changes plus repo-wide link grep).
- [ ] No contradiction with **invariants**, **coding-conventions**, or **isometric-geography-system** §7 authority for **Sorting order** math.
- [ ] **`npm run verify`** passes under **`tools/mcp-ia-server/`** after changes that touch **`.cursor/specs/`** or **BACKLOG** consumed by verify.
- [ ] Issue closure: human-approved; **TECH-25** completed in **`BACKLOG.md`**; §10 migrated if needed; this project spec deleted.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure; migrate durable notes to **REFERENCE-SPEC-STRUCTURE**, **unity-development-context.md**, or **`AGENTS.md`** as appropriate.)

## Open Questions (resolve before / during implementation)

Policy questions for this **tooling-only** issue — answered below (no **gameplay** / **simulation** rule definitions).

1. **When does a milestone require editing [`glossary.md`](../specs/glossary.md) vs only `unity-development-context.md`?**  
   **Resolution:** If you introduce or redefine a **domain** term (**Cell**, **Sorting order**, **road stroke**, etc.) beyond a pointer to an existing spec, add or update the **glossary** row **and** the authoritative **reference spec** per **terminology-consistency**. **Unity**-only terms (**MonoBehaviour**, **Inspector**, **`sortingOrder`**) stay in **unity-development-context** unless the glossary already lists them.

2. **May milestones add new `###` subheadings under existing `## N.` sections?**  
   **Resolution:** Yes, when it improves readability. Do **not** renumber or rename top-level **`## N.`** without updating **territory-ia** consumers and grepping **`.cursor/`** / **`docs/`** for broken anchors. Prefer bullets inside **`## N.`** when a small change suffices — fewer anchor churn risks for **`spec_section`**.

3. **Should TECH-25 document Addressables or other packages not used in `Assets/Scripts`?**  
   **Resolution:** No. Only document third-party **Unity** stacks that appear in this repo (verified by search). If **Addressables** (or similar) is added later, a future milestone or issue may extend §4.

4. **When is `agent-router.mdc` edited during TECH-25?**  
   **Resolution:** Only if the **Task domain** string for the **Unity** row must change — and then follow **REFERENCE-SPEC-STRUCTURE** item **6** (avoid tokens that false-positive **`router_for_task`** queries such as **“grid math”**). Routine **unity-development-context** body edits do **not** require router changes.

5. **Can TECH-25 close with zero merged milestones?**  
   **Resolution:** Yes, if the maintainer records in **§6 Decision Log** that the **TECH-20** baseline is sufficient and **BACKLOG** is updated accordingly; still run Phase 0 verify once if closing without doc edits (optional hygiene).
