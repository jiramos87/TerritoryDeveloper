# TECH-20 — In-repo Unity development context for agents

> **Issue:** [TECH-20](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02 (implementation landed)

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **7**; **TECH-28** (Editor **agent-context** JSON + optional **sorting** debug); **TECH-18** Phase **D** — MCP **`unity_context_section`** reads slices of **`.cursor/specs/unity-development-context.md`** once it exists.

## 1. Summary

Ship a **first-party reference spec** at **`.cursor/specs/unity-development-context.md`** so **territory-ia**-equipped agents and humans use **in-repo** Unity guidance (patterns that recur in `Assets/Scripts/`) before generic web manuals. Content must follow **`.cursor/rules/coding-conventions.mdc`**, **`.cursor/rules/invariants.mdc`**, and **`.cursor/rules/project-overview.mdc`**, defer **isometric** rules to **`.cursor/specs/isometric-geography-system.md`**, and use **glossary** vocabulary when touching game domains (**cell**, **HeightMap**, **Sorting order**, **GridManager**, **AUTO systems**, etc.). Wire **`.cursor/rules/agent-router.mdc`** and **`AGENTS.md`** so typical **Inspector** + **MonoBehaviour** tasks route here; confirm **territory-ia** **`list_specs`** / **`spec_section`** can target the new file (auto-discovery + optional short aliases in **`tools/mcp-ia-server/src/config.ts`**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. New **reference spec** with stable **Markdown** headings (anchors) and pointers to **in-repo** examples (class / file paths under `Assets/Scripts/` where patterns appear).
2. **`AGENTS.md`** (spec inventory table) and **`.cursor/rules/agent-router.mdc`** (task → spec row) updated so **Unity Editor** workflow tasks read **`unity-development-context`** before ad-hoc search.
3. Terminology aligned with **`.cursor/specs/glossary.md`** and linked specs; project guardrails explicit (**no `FindObjectOfType` in `Update`**, **no new singletons** — **`GameNotificationManager`** remains the documented exception per **invariants**).

### 2.2 Non-Goals (Out of Scope)

1. Duplicating Microsoft / Unity manual pages verbatim.
2. Replacing **`isometric-geography-system.md`** for terrain, **road preparation family**, water, cliff, or **Sorting order** math — this doc **links** to geography §7 (and related sections) instead of copying formulas.

### 2.3 General objective

Contributors and agents **default to repository-authored Unity guidance** (lifecycle, **Inspector** wiring, **Script Execution Order**, 2D renderer ordering vs script-driven **Sorting order**) constrained by this project’s rules, so implementation work stays consistent with **GridManager**-centric patterns and **invariants** without relying on external docs unless the task requires version-specific or undocumented **Unity** APIs.

### 2.4 Specific implementation objectives

| # | Objective | Measurable outcome |
|---|-----------|-------------------|
| O1 | Author **`unity-development-context.md`** as a permanent **reference spec** | File exists under **`.cursor/specs/`**, **English** prose, numbered / stable **`##` headings**, **Glossary alignment** section per **REFERENCE-SPEC-STRUCTURE** |
| O2 | Cross-link authoritative rules | Sections cite **`coding-conventions.mdc`**, **`invariants.mdc`**, **`project-overview.mdc`** where they duplicate or tighten Unity-generic advice |
| O3 | Route agents from **agent-router** + **AGENTS.md** | New table row + inventory row; wording matches other spec rows (path + “when to read”) |
| O4 | Enable **territory-ia** slices | **`list_specs`** returns registry key **`unity-development-context`**; **`spec_outline` / `spec_section`** resolve the doc by key or filename; optional **`SPEC_KEY_ALIASES`** (e.g. `unity`) documented in **`docs/mcp-ia-server.md`** and **`tools/mcp-ia-server/README.md`** if added |
| O5 | Unblock downstream tooling | **TECH-18** **`unity_context_section`** and **TECH-25** milestone checklist can assume this file path and content shape |

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want Unity onboarding without random web drift. | **agent-router** points to **`unity-development-context`** for scoped **MonoBehaviour** / **Inspector** / execution-order tasks. |
| 2 | Human developer | I want one place for “how we do Unity here.” | **Reference spec** lives at **`.cursor/specs/unity-development-context.md`** with **glossary**-aligned terms where it touches game concepts. |

## 4. Current State

### 4.1 Domain behavior

N/A (documentation / agent workflow only — no **simulation tick** or **save/load** behavior change).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | **`BACKLOG.md`** — **TECH-20**; slice milestones — **TECH-25** |
| Rules | **`coding-conventions.mdc`**, **`invariants.mdc`**, **`project-overview.mdc`**, **`agent-router.mdc`** |
| Meta | **`.cursor/specs/REFERENCE-SPEC-STRUCTURE.md`** — new reference spec checklist |
| MCP | **`buildRegistry()`** in **`tools/mcp-ia-server/src/config.ts`** auto-registers **`.cursor/specs/*.md`**; **`SPEC_KEY_ALIASES`** optional |
| Follow-on | **TECH-18** — **`unity_context_section`**; **TECH-28** — Editor **agent-context** JSON |

### 4.3 Implementation investigation notes (optional)

- **Slice-sized PRs** are encouraged (**TECH-25**): land sections incrementally (lifecycle → **Inspector** → **`FindObjectOfType`** policy → **Script Execution Order** → 2D sorting pointer).
- After adding the file, restart or rerun **territory-ia** so caches see the new path; verify with **`npm run verify`** under **`tools/mcp-ia-server/`**.

## 5. Proposed Design

### 5.1 Target behavior (product)

Contributors and agents open **`unity-development-context.md`** for **Unity Editor** and **MonoBehaviour** conventions tied to this repo before external documentation.

### 5.2 Architecture / implementation (reference spec outline)

Author **`unity-development-context.md`** using the minimal template in **REFERENCE-SPEC-STRUCTURE**, extended with sections such as:

1. **Purpose & scope** — Link **non-goals**; policy: prefer repo + **territory-ia**; when web docs are acceptable.
2. **`MonoBehaviour` lifecycle** — **`Awake`** / **`Start`**: cache references; match patterns in **`Managers/`** and **`Controllers/`**.
3. **`[SerializeField] private` + `FindObjectOfType` fallback in `Awake`** — **Guardrail** from **invariants**; contrast with forbidden per-frame lookup (**`Update`**, tight loops).
4. **Scenes, prefabs, Inspector** — Rename / missing reference pitfalls; **XML documentation** expectations from **coding-conventions**.
5. **2D `sortingOrder` vs Sorting Layers vs script-driven Sorting order** — Explain **Unity** renderer fields at a high level; **defer** isometric **Sorting order** formula and **cell** stacking rules to **`isometric-geography-system.md`** §7 (and **glossary** **Sorting order**).
6. **Script Execution Order** — Initialization races (**BUG-16**-class); when to adjust **Unity** execution order vs reordering **`Awake`** / **`Start`** logic.
7. **Anti-patterns** — New **singletons** (forbidden except documented **`GameNotificationManager`**), **`FindObjectOfType`** in per-frame paths, **`GridManager`** responsibility bloat (extract helpers per **invariants**).
8. **`ScriptableObject`** — Only where the codebase already uses them; pointer to examples if any; avoid inventing new asset workflows in this issue.
9. **Glossary alignment** — List domain terms this spec mentions that agents should cross-check in **`glossary.md`**.

Each substantive section should include at least one **in-repo** file or class pointer (e.g. a representative **Manager** showing **`SerializeField` + `FindObjectOfType` in `Awake`**), discovered via search at authoring time — not placeholder text.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created from §6 Summary “documentation/tooling” | Tracks phased authoring | — |
| 2026-04-02 | **Canonical filename:** `.cursor/specs/unity-development-context.md` | Matches **BACKLOG.md**, **TECH-18**, **TECH-25**, **`docs/agent-tooling-verification-priority-tasks.md`**, and MCP registry key derivation (`unity-development-context`) | Shorter alias filenames (rejected: would break cross-links and backlog **Files** field) |
| 2026-04-02 | **agent-router** task-domain wording: “not isometric stacking rules” | **`router_for_task`** matches tokens ≥3 chars; phrases like “not isometric math” made `domain: "grid math"` false-positive the **Unity** row | — |

## 7. Implementation Plan

### Phase 1 — Author reference spec v0

**Prerequisites:** Read **`REFERENCE-SPEC-STRUCTURE.md`**, **`invariants.mdc`**, **`coding-conventions.mdc`**, **`project-overview.mdc`**. Use **territory-ia** **`glossary_discover`** / **`glossary_lookup`** (queries in **English**) for terms like *sorting order*, *HeightMap*, *cell* before writing game-adjacent prose.

1. **Create** **`.cursor/specs/unity-development-context.md`** with title and one-line scope block (see **REFERENCE-SPEC-STRUCTURE** minimal template).
2. **Add a table of contents** at the top (linked list to each **`## N.`** heading) so anchors are navigable in **GitHub** / **Cursor** preview.
3. **Number major sections** (e.g. `## 1. …`, `## 2. …`) for stable **`spec_section`** targeting (same style as **`isometric-geography-system.md`** where practical).
4. **Draft sections** listed in §5.2 in order; keep each section **normative for this repo**, not a generic Unity tutorial.
5. **Embed in-repo examples:** run ripgrep searches such as `SerializeField`, `FindObjectOfType`, `Awake` under **`Assets/Scripts/Managers/`**; cite 1–3 real types (path + class name) per pattern. Do not invent fictional paths.
6. **Cross-link** (Markdown links) to **`.cursor/rules/invariants.mdc`**, **`.cursor/rules/coding-conventions.mdc`**, **`.cursor/specs/isometric-geography-system.md`** (§7 for **Sorting order**), and **`glossary.md`** as appropriate.
7. **Add `## Glossary alignment`** listing terms used (e.g. **Sorting order**, **GridManager**, **MonoBehaviour**, **Inspector**).
8. **Language:** All headings and body **English** per project rules.

**Checklist**

- [x] **`.cursor/specs/unity-development-context.md`** exists with TOC + numbered sections + **Glossary alignment**
- [x] Lifecycle, **`SerializeField`**, **`FindObjectOfType`** policy, **Script Execution Order**, 2D sorting vs **geo** §7 pointer covered
- [x] At least one real **codebase** citation per major pattern subsection

### Phase 2 — Wire agent-router and AGENTS.md

1. **Edit** **`.cursor/rules/agent-router.mdc`** — In **`## Task → Spec routing`**, insert a **new row** (order: group with **UI** / **coding** rows is fine):

   | Task domain | Spec to read | Key sections |
   |-------------|--------------|--------------|
   | **Unity** / **MonoBehaviour** / **Inspector** wiring, **Script Execution Order**, 2D renderer **`sortingOrder`** / layers (not isometric math) | **`.cursor/specs/unity-development-context.md`** | Full spec; **defer** **Sorting order** formula to **`isometric-geography-system.md`** §7 |

   Preserve table column alignment with existing rows.

2. **Edit** **`AGENTS.md`** — In **`.cursor/specs/` inventory** table (same section as other reference specs), add:

   | `unity-development-context.md` | **Unity** patterns for this repo: **MonoBehaviour** lifecycle, **Inspector** / **`SerializeField`**, **`FindObjectOfType`** policy, **Script Execution Order**, 2D sorting vs **Sorting order** (pointer to geography §7) |

3. **Optional:** If **`mcp-ia-default.mdc`** or **`AGENTS.md`** “Suggested order” should mention the new spec for **Unity**-only tasks, add a **single** clarifying sentence (do not duplicate the whole router table).

**Checklist**

- [x] **agent-router** row added; geography **Sorting order** explicitly deferred to **geo** §7
- [x] **`AGENTS.md`** inventory row added

### Phase 3 — territory-ia registry verification and optional aliases

**Fact:** **`buildRegistry()`** picks up every **`.cursor/specs/*.md`** file automatically — there is **no** separate manifest file to edit for registration.

1. **Verify discovery:** From repository root, run:

   ```bash
   cd tools/mcp-ia-server && npm run verify
   ```

   Confirm the verify script completes successfully and, if it exercises **`list_specs`**, that an entry exists with key **`unity-development-context`** (basename of file without **`.md`**, lowercased).

2. **Manual spot-check (optional):** With **territory-ia** running, call **`list_specs`** and confirm **`relativePath`** **`.cursor/specs/unity-development-context.md`**. Call **`spec_outline`** with **`spec`: `unity-development-context`** (or full filename).

3. **Optional aliases:** If short keys help agents, edit **`tools/mcp-ia-server/src/config.ts`** — extend **`SPEC_KEY_ALIASES`**, e.g. `unity: "unity-development-context"`, `unityctx: "unity-development-context"`. Keys must map to the registry key (lowercase basename).

4. **If aliases were added:** Update **`docs/mcp-ia-server.md`** and **`tools/mcp-ia-server/README.md`** — extend the **`spec_outline` / `spec_section`** example lines to include the new alias (same pattern as `geo`, `roads`).

5. **If no aliases:** Still ensure **`docs/mcp-ia-server.md`** requires no change unless you document the full key **`unity-development-context`** in an examples table for discoverability (optional sentence only).

**Checklist**

- [x] **`npm run verify`** passes under **`tools/mcp-ia-server/`**
- [x] **`list_specs`** / **`spec_outline`** / **`spec_section`** resolve the new doc by registry key
- [x] **`SPEC_KEY_ALIASES`** (`unity`, `unityctx`) and consumer docs updated together

### Phase 4 — Acceptance sweep (pre-close)

1. Re-read **§8 Acceptance Criteria** below; tick boxes in this project spec when true.
2. Scan for contradictions with **`invariants.mdc`**; any intentional exception must be recorded in **§6 Decision Log**.
3. **Do not** mark **TECH-20** completed in **`BACKLOG.md`** until the human confirms verification (**AGENTS.md** backlog workflow).

**Checklist**

- [x] §8 criteria reviewed; **`npm test`** + **`npm run verify`** green under **`tools/mcp-ia-server/`**
- [ ] **Human:** move **TECH-20** in **`BACKLOG.md`** after play / doc review if desired

## 8. Acceptance Criteria

- [x] **`unity-development-context.md`** exists with stable headings and **English** prose.
- [x] **`agent-router.mdc`** lists when to read it; **`AGENTS.md`** inventory references it.
- [x] No contradiction with **invariants** / **coding-conventions** (or **Decision Log** records intentional deltas).
- [x] **territory-ia** **`list_specs`** includes the file; **`spec_section`** can retrieve at least one section by heading id or title substring.
- [x] **MCP** aliases documented (`unity` / `unityctx` in **`config.ts`**, **`docs/mcp-ia-server.md`**, **`README.md`**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

**None for implementation readiness.** This issue is documentation-only; the only naming choice (**canonical path** **`.cursor/specs/unity-development-context.md`**) is fixed in **§6 Decision Log** (2026-04-02). Further content depth (e.g. extra subsections) is authoring discretion under **TECH-25** slice milestones, not a blocking **Open Question**.
