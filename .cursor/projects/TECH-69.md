# TECH-69 — UI improvements using UI-as-code (TECH-67 capstone)

> **Issue:** [TECH-69](../../BACKLOG.md)
> **Parent program:** [TECH-67](TECH-67.md) (**UI-as-code program** umbrella)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

This issue is the **program capstone** for [TECH-67](TECH-67.md): implement the **improvement track** derived from [`docs/ui-as-built-critique-TECH-67.md`](../../docs/ui-as-built-critique-TECH-67.md) — unifying **MainMenu** authoring, **theme** tokens, a **prefab** library **v0**, **typography** policy (**TMP** vs legacy **Text**), **Canvas Scaler** alignment, **`UIManager`** decomposition, **modal** patterns, **scroll** vs **camera** policy, and **hierarchy** hygiene — plus the **Editor** / **Cursor Skill** / optional **territory-ia** tooling needed so **IDE** agents apply changes **safely** and **repeatedly**. Work is intended **after** umbrella **Phase 1–4** milestones (**as-built** spec, **ControlPanel** / **toolbar** in scene, interim kit rows) unless the **Decision Log** records a deliberate **collapse** of order.

**Refined intent vs raw critique IDs:** The critique’s **P1–P9** are merged into **phased deliverables** below; **P5** (**Canvas Scaler**) is coordinated with **`ui-design-system.md`** **§3.3** / **§4.3** and the shipped **toolbar** layout; **P8** (renames) is front-loaded where it reduces merge pain.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Single authoring truth for MainMenu:** Serialized **Canvas** in **`MainMenu.unity`** (and/or prefabs); **`MainMenuController.BuildUI()`** narrowed to fallback, dev-only, or removed per **Decision Log**.
2. **Prescriptive theme:** **`ScriptableObject`** (or approved equivalent) holding **Color** + **font size** (+ optional **TMP** style references) — consumed by **new** code and **gradual** migration off **Inspector** literals.
3. **Prefab library v0:** Small set of reusable prefabs (e.g. **tool button**, **stat row**, **scroll list shell**, **modal shell**) under agreed **`Assets/`** paths and **`.cursor/rules/coding-conventions.mdc`** naming.
4. **Typography policy:** Record **Decision** — migrate **city** **HUD** toward **TMP** with shared styles **or** **freeze** legacy **Text** and stop **TMP** creep — then execute the chosen path in **waves** (no half-policy).
5. **Scaler strategy:** One **documented** table per scene (or unified rule) in **`ui-design-system.md`** **§4.3**; **Play Mode** checks at **1280×720** and **1920×1080** (and **800×600** if still a target minimum).
6. **`UIManager` surface reduction:** Facades / partial coordinators (**HUD**, **popups**, **toolbar** state) — **no** gameplay rule changes; **Inspector** wiring moves in **increments** with regression checks.
7. **Modal contract:** Shared overlay / close / optional **Esc** behavior for **`PopupType`** surfaces where product agrees.
8. **Input UX:** Close or down-scope **BUG-19**-class **scroll** vs **camera** behavior with a **checklist** in **`ui-design-system.md`** **§3.5**.
9. **Hygiene:** Fix **typo** / duplicate **GameObject** names from inventory (**e.g.** `UnenmploymentPanel`); update **`ui-design-system.md`** paths.
10. **Tooling:** Ship **Editor** validation and/or scaffold hooks (**§5.4**); add or extend **Cursor Skill**(s); optional **MCP** tools only if **§5.4** cost is accepted (register per **`docs/mcp-ia-server.md`**).

### 2.2 Non-Goals (Out of Scope)

1. Replacing **Unity UI** with **UI Toolkit** or a third-party stack.
2. Changing **simulation** / **economy** **game rules** (presentation-only unless a **FEAT-**/**BUG-** issue scopes logic).
3. Full **accessibility** / **WCAG** audit.
4. **Procedural** replacement of **entire** **Canvas** trees from Markdown alone — stay **Prefab**- and **Inspector**-compatible (**TECH-67** **§2.2**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want **one** place for **colors** and **type sizes** so new **HUD** rows do not invent new literals. | **Theme** asset exists; at least **one** migrated panel uses it; spec **§1** updated (**Target** / **as-built**). |
| 2 | IDE agent | I want **prefab** paths + **theme** names in repo docs so I instantiate instead of hand-authoring **RectTransform** trees. | **§5.2** inventory table + **Skill** or **Editor** menu documents paths. |
| 3 | Player | I want consistent **popup** dismiss and scroll behavior. | **Modal** contract applied to agreed **`PopupType`** set; **§3.5** checklist satisfied for listed surfaces. |
| 4 | Maintainer | I want **`UIManager`** changes to **merge** without **100+** field conflicts. | Facades land with **no** behavior regression; **managers-reference** / **§4.4** updated. |

## 4. Current State

### 4.1 Domain behavior

**As-built** **UI** is documented in [`.cursor/specs/ui-design-system.md`](../specs/ui-design-system.md) and critiqued in [`docs/ui-as-built-critique-TECH-67.md`](../../docs/ui-as-built-critique-TECH-67.md): emergent **tokens**, **legacy Text**-heavy **HUD**, **dual** **MainMenu** authoring, **no** prefab library, **split** **Canvas Scaler** baselines, large **`UIManager`**, **modal** inconsistency, **scroll** vs **camera** debt.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Reference **UI** spec | `.cursor/specs/ui-design-system.md` |
| Critique + **P1–P9** trace | `docs/ui-as-built-critique-TECH-67.md` |
| Machine snapshot | `docs/reports/ui-inventory-as-built-baseline.json` |
| **Toolbar** (**ControlPanel**) | **`ui-design-system.md`** **§3.3** (**as-built** in **`MainScene.unity`**) |
| **Editor** diagnostics pattern | `unity-development-context.md` **§10** |
| **Umbrella** | [TECH-67](TECH-67.md) **§4.4**, **§7** Phase **0** (inventory, [`docs/reports/`](../../docs/reports/README.md) baseline, **`UiInventoryReportsMenu`** allowlist — **not** this capstone) |

### 4.3 Implementation investigation notes

- **Order:** Prefer **P8** renames early; **P1** **MainMenu** serialization before heavy **JSON**/**theme** diff tooling depends on **MainMenu** **Canvas** in scene.
- **TMP migration:** Use **Unity** **TMP** conversion tools where safe; re-wire **`UIManager`** **Text** fields in batches; watch **sorting** / **Canvas** rebuild cost.
- **Tests:** **Edit Mode** **UTF** for **theme** optional; **Play Mode** smoke **MainMenu** → **city** mandatory before closure.

## 5. Proposed Design

### 5.1 Target behavior (product)

- **Look-and-feel:** Coherent **typography** steps (**key** vs **value**), **consistent** **modal** chrome, **predictable** **scroll** vs **zoom**.
- **No** change to underlying **simulation** outputs unless a separate issue ties **UI** to new data.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**Runtime**

| Deliverable | Role |
|-------------|------|
| **`UiTheme`** (name **TBD**) **`ScriptableObject`** | **Color** / size tokens; optional **TMP** asset refs |
| **Prefab folder** e.g. `Assets/UI/Prefabs/` (exact path in **Decision Log**) | **v0** library (**tool button**, **stat row**, **scroll shell**, **modal shell**) |
| **`UIManager` facades** | New types or partials binding **theme** + **prefab** instances — obey **invariants** (**no `FindObjectOfType` in `Update`**, no new singletons) |

**Editor / repo tooling**

| Tool | Role |
|------|------|
| **MenuItem** under **Territory Developer → Reports** (or **Tools → Territory UI**) | **Validate** theme vs inventory / report missing bindings (follow **§10** conventions) |
| Optional **batchmode** `-executeMethod` | Headless **validate** when **TECH-66**-class harness exists; else **defer** |
| **`tools/`** **Node** script (optional) | Diff **`ui-inventory-*.json`** token set vs **`UiTheme`** serialized export — **CI** advisory |
| **Cursor Skill** | Recipe: “add **HUD** row using **`UI_StatRow`** + **`UiTheme`**” + link **`ui-design-system.md`** **§5** |

**Optional territory-ia**

| Tool (concept) | Role |
|----------------|------|
| **`ui_theme_tokens`** (name **snake_case** if shipped) | Return **theme** field names + **spec** **§1** slice — **only** if **MCP** maintenance is approved |

### 5.3 Phased mapping (critique → work)

| Critique ID | Phase (suggested) | Notes |
|-------------|-------------------|--------|
| **P8** | **A** — Hygiene | Renames, **§4.1** cleanup |
| **P1** | **B** — **MainMenu** serialize | Unblocks **Edit Mode** export coverage |
| **P3**, **P4** | **C** — Theme + prefabs **v0** | Foundation for agents |
| **P2** | **D** — Typography | After **C** or parallel with facades |
| **P5** | **E** — Scaler | Align with **§3.3** / **§4.3** and **Play Mode** resolution matrix |
| **P6** | **F** — **`UIManager`** split | Incremental PRs |
| **P7**, **P9** | **G** — Modal + input | **BUG-19** alignment |
| Tooling polish | **H** | **Editor**/**Skill**/**MCP** |

### 5.4 Method / algorithm notes (optional)

- **Theme apply:** Prefer **runtime** reference on **root** **HUD** **MonoBehaviour** + propagate on **Awake** for migrated subtrees; avoid **Editor**-only mutators for shipped scenes unless a **TECH-** row scopes **SerializedObject** batching.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Single **capstone** issue **TECH-69** holds **P1–P9** + tooling | User requested one **BACKLOG** row executed at **umbrella** close | Many small **TECH-** rows (deferred) |
| 2026-04-04 | **Typography** policy must be **explicit** before large migration | Avoid **TMP**/**Text** split worsening | Implicit gradual mix (rejected) |

## 7. Implementation Plan

### Phase A — Hygiene (**P8**)

- [ ] Rename **GameObject** / fix **typo** paths cited in critique; update **`ui-design-system.md`** and refresh **`docs/reports/ui-inventory-as-built-baseline.json`** when scenes change.

### Phase B — **MainMenu** authoring (**P1**)

- [ ] Serialize **menu** **Canvas** in **`MainMenu.unity`** or prefab; narrow **`BuildUI()`** per **§2.1**.

### Phase C — Theme + prefabs **v0** (**P3**, **P4**)

- [ ] Add **`UiTheme`** (**SO**) + **v0** prefabs; document paths in **`ui-design-system.md`** **§4.1** / **§6**.

### Phase D — Typography (**P2**)

- [ ] **Decision Log** records **TMP** migration vs **legacy freeze**; execute chosen path in waves; update **§1.2** / **TECH-67** **§4.9**.

### Phase E — **Canvas Scaler** (**P5**)

- [ ] Align **MainMenu** / **city** scaler table with shipped **toolbar** + **`ui-design-system.md`** **§4.3**; **Play Mode** resolution matrix.

### Phase F — **`UIManager`** decomposition (**P6**)

- [ ] Land facades without behavior change; update **managers-reference** and **TECH-67** **§4.4**.

### Phase G — Modal + input (**P7**, **P9**)

- [ ] Shared **modal** behavior for agreed **`PopupType`**; **§3.5** checklist + **BUG-19** closure or split follow-up.

### Phase H — Tooling

- [ ] **Editor** validate menu; **Cursor Skill**; optional **MCP** + **`npm run verify`** if **MCP** code ships.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **IA** / spec bodies if **glossary** / **ui-design-system** / **MCP** touched | Node | `npm run validate:all` | Per **project-implementation-validation** |
| **Play Mode** smoke | Manual | **MainMenu** → **city**; **toolbar** + **popups** | After **F**/**G** |
| **MCP** registration | Node | `npm run verify` in `tools/mcp-ia-server/` | Only if Phase **H** registers tools |

## 8. Acceptance Criteria

- [ ] **P1–P9** outcomes met or explicitly **deferred** with **BACKLOG** follow-up + **Decision Log** entry.
- [ ] **`ui-design-system.md`** reflects **shipped** **theme**, **prefab** paths, **scaler** table, **modal** + **§3.5** guidance.
- [ ] **TECH-67** **§4.4** updated for **`UIManager`** / **Canvas** hierarchy changes.
- [ ] **Editor** tooling (**§5.2**) shipped or **deferred** with rationale.
- [ ] **Cursor Skill** shipped or **deferred** with rationale.
- [ ] `npm run validate:all` passes when **spec** / **index** / **MCP** artifacts change.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- _Fill at closure; migrate to **`ui-design-system.md`**, **TECH-67**, **Skills**._

## Open Questions (resolve before / during implementation)

None — tooling and presentation **refactor** only; **player-facing game logic** changes belong in **FEAT-**/**BUG-** specs. **Typography** **product** preference (**readability** vs **migration** cost) is a **team Decision Log** entry, not **Open Questions** gameplay.
