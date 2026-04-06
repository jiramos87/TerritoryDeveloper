# TECH-72 — HUD / uGUI scene hygiene for agents (UI inventory alignment)

> **Issue:** [TECH-72](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-06  
> **Last updated:** 2026-04-11

**Depends on:** none  
**Related:** **FEAT-51**, glossary **UI design system (reference spec)**, glossary **Urbanization proposal** (**obsolete**)

## 1. Summary

Bring **`MainScene.unity`** (and **`MainMenu.unity`** if the same class of issues appears) in line with **`.cursor/specs/ui-design-system.md`** **§1.3.1** so **Editor** **UI** inventory exports, **Transform.Find**, and agent-driven **UI** edits stay predictable. Work is **scene wiring, naming, and component placement** only—**no** changes to **simulation** rules, **economy** formulas, or **grid** behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Fix or document every **§1.3.1** violation called out in **§4.2** (inventory-derived list).
2. After scene edits, refresh **`docs/reports/ui-inventory-as-built-baseline.json`** from **`editor_export_ui_inventory`** (or the **Reports → Export UI Inventory** path) so the committed file matches **Editor** truth.
3. Confirm **`ProposalUI`** is consistent with the **obsolete** **Urbanization proposal** policy (**invariants**, glossary)—remove, hide, or disconnect if the flow is fully inert.

### 2.2 Non-Goals

1. Full **UiTheme** visual redesign passes (track as a separate **FEAT-** if needed).
2. New **dashboard** or chart surfaces (**FEAT-51**).
3. Migrating **StatsPanel** **UXML** to pure **uGUI** (optional future row unless product mandates).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I open **MainScene** and see **HUD** names and **Inspector** roots that match **§1.3.1** and the **UI** inventory baseline. | No trailing spaces in exported paths; no **manager** types on unrelated **panel** roots without an explicit exception in the **Decision Log**. |
| 2 | Agent | I use paths from **`ui-inventory-as-built-baseline.json`** or **Postgres** exports without **Transform.Find** traps. | Renamed nodes stay wired in **`UIManager`** / controllers; **`npm run validate:dead-project-specs`** green after **BACKLOG** / spec edits. |

## 4. Current State

### 4.1 Domain behavior

**N/A** — tooling and **Editor**-authored layout only.

### 4.2 Systems map — inventory findings (baseline **Postgres** **`editor_export_ui_inventory`** **id** **8**, **git** **`2245403e3531b5779c52b3480be6bd0ba085946c`**)

| Finding | Export path / note | Suggested direction |
|--------|-------------------|---------------------|
| Trailing space in name | `Canvas/DataPanelButtons/TaxPanel/CommercialTaxText ` | Rename; update **`UIManager.Theme.cs`** **`StartsWith("CommercialTaxText", …)`** callers if any path literals exist. |
| Auto-suffixed duplicate label | `…/RoadGrowthLabel` and `…/RoadGrowthLabel (1)` | Merge or rename to stable keys (**Value** vs **Caption**, etc.); update **`SerializeField`** / lookups. |
| Duplicate **Button** basename | **Main menu** `…/NewGameButton` vs **`Canvas/DataPanelButtons/NewGameButton`** | Rename **city** **HUD** instance (e.g. **NewGameHudButton**) so global searches and agents do not confuse the two. |
| **GameManager** on **panel** root | `Canvas/LoadGameMenuPanel` hosts **GameManager** | Move **GameManager** to a **scene** **bootstrap** object (or documented service host); keep **LoadGame** UI on a **controller**-only root. |
| **UI Toolkit** + **uGUI** | `Canvas/DataPanelButtons/StatsPanel` has **UIDocument** | Document **UXML** vs **uGUI** ownership in code or **`managers-reference.md`** when touching **stats**; optional follow-up to consolidate. |
| **TMP** + legacy mix | `Canvas/NotificationPanel/NotificationText` uses **TextMeshProUGUI** (most **HUD** uses legacy **Text**) | Either migrate **Notification** to legacy for one stack, or document **TMP** as intentional until a broader **§1.2** pass. |
| **MiniMapPanel** full stretch | `Canvas/MiniMapPanel` **anchor** `(0,0)`–`(1,1)` | **Not** automatically wrong—treat as **layout pattern**; when adding siblings (e.g. debug **HUD**), respect **§1.3.1** stacking / gap rules. |
| **ProposalUI** subtree | `Canvas/ProposalUI` (legacy **Text** under **Buttons**) | Verify **UrbanizationProposalManager** never surfaces player-facing proposals; if inert, remove subtree and dead **SerializeField** references; if still referenced, escalate—glossary says **Urbanization proposal** must **not** return. |

### 4.3 Implementation investigation notes

- **`GameSaveManager`** and **`SimulationManager`** still reference **`UrbanizationProposalManager`**—confirm lifecycle before deleting **scene** objects.
- **`UIManager.Theme.cs`** already normalizes **Commercial** tax **Text** by **name prefix**; renaming the **GameObject** is safe if the **string** prefix policy is preserved.

## 5. Proposed Design

### 5.1 Target behavior (product)

**N/A**.

### 5.2 Architecture / implementation (agent-owned)

Phased: (1) rename / move **Inspector** objects and rewire references; (2) code search for old paths; (3) re-export **UI** inventory and commit baseline JSON; (4) update **Decision Log** for any intentional exceptions.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-06 | Baseline promoted from **Postgres** row **id** **8** | Matches user export **2026-04-06** | — |
| 2026-04-11 | Issue id **TECH-72** (this spec) | **TECH-60** is **archived** for the **spec pipeline & verification program** umbrella ([`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md)); **TECH** ids are not reused for new work | Draft briefly used **TECH-60** for **HUD** hygiene — invalid |

## 7. Implementation Plan

### Phase 1 — Naming and **Inspector** cleanup

- [ ] Rename **`CommercialTaxText `** (trim trailing space); verify **tax** **UI** still themes.
- [ ] Replace **`RoadGrowthLabel (1)`** with explicit names; update **`CityStatsUIController`** / **`UIManager`** references as needed.
- [ ] Rename **`Canvas/DataPanelButtons/NewGameButton`** to avoid collision with **Main menu**.

### Phase 2 — Root components and obsolete **UI**

- [ ] Relocate **`GameManager`** off **`LoadGameMenuPanel`** (scene hierarchy + any **prefab** variants).
- [ ] Resolve **`ProposalUI`** per **§4.2** after confirming **Urbanization proposal** is inactive.

### Phase 3 — Documentation and baseline

- [ ] Record **StatsPanel** **UIDocument** boundary and **NotificationPanel** **TMP** stance in **`managers-reference.md`** or **`ui-design-system.md`** **as-built** notes if still mixed.
- [ ] Re-export **UI** inventory; commit **`docs/reports/ui-inventory-as-built-baseline.json`**.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| No stale **project spec** links after **BACKLOG** edit | Node | `npm run validate:dead-project-specs` | Repo root |
| **Unity** compiles after scene / script reference changes | Manual | **Unity** **Editor** build / play smoke | Developer machine |

## 8. Acceptance Criteria

- [ ] Items in **§4.2** are **fixed** or **documented** with rationale in **§6**.
- [ ] Committed **`docs/reports/ui-inventory-as-built-baseline.json`** matches a fresh **Editor** export after changes.
- [ ] **`ProposalUI`** outcome recorded: removed, hidden with **no** live path, or escalated with **PO** sign-off (only if product contradicts **glossary** **Urbanization proposal**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- *(Fill at closure.)*

## Open Questions (resolve before / during implementation)

None — tooling and **Editor** hygiene only; see **§8** and **§4.3** for implementation judgment.
