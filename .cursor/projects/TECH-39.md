# TECH-39 — territory-ia computational MCP tool suite

> **Issue:** [TECH-39](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-03
> **Last updated:** 2026-04-03

**Parent program:** [TECH-36](TECH-36.md) · **Depends on:** **TECH-37** ( **`compute-lib`**, **`registerTool`** pattern, **verify** harness) · **Soft:** **TECH-38** (stable **C#** **APIs** / **batchmode** **exports** for **heavy** tools)

## 1. Summary

Expand **territory-ia** with a **family** of **`snake_case`** **computational** tools that agents invoke for **World ↔ Grid conversion**, **growth ring** classification, **pathfinding cost** **previews**, **desirability** **top-k** **queries**, and (when **TECH-38** lands) **batchmode**-backed **grid** **snapshots**. Each tool has a **JSON** contract, **English** description using **glossary** terms, **limits** (max **map** size, timeout), and **tests** in **`npm run verify`**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **≥ 4** **new** **`registerTool`** entries beyond **TECH-37** pilot (total **≥ 5** **computational** tools including pilot), or fewer if **merged** **by design** in **Decision Log**.
2. **Every** tool documented in **`docs/mcp-ia-server.md`** and **`tools/mcp-ia-server/README.md`** with **glossary**-aligned **terminology**.
3. **`tools/compute-lib/`** implements **pure** **Node** **slices**; **MCP** **handlers** stay **thin**.
4. **Heavy** tools **either** call **Unity** **batchmode** **CLI** with **temp** **JSON** **output** **path** **or** return **`not_implemented`** with **English** **message** until **TECH-38** **hook** exists — **no** **silent** **wrong** **numbers**.

### 2.2 Non-Goals

1. Replacing **`spec_section`**, **`glossary_lookup`**, or **IA** **Postgres** (**TECH-18**/**TECH-44b**).
2. **Remote** **network** **execution** — **local** **repo** **only**.
3. **Player-facing** **UI**.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | IA agent | I classify a **cell** into a **growth ring** from **centroid** **JSON**. | Tool returns **ring** **index** + **canonical** **terms**. |
| 2 | IA agent | I preview **path** **costs** without Unity. | **compute-lib** **kernel** **or** **documented** **stub**; **verify** **test** **covers** **happy** **path**. |
| 3 | MCP maintainer | I can **audit** **tool** **count** and **schemas** in **one** **README** **table**. | **README** **section** **“Computational tools”**. |

## 4. Current State

### 4.1 Baseline

**TECH-37** delivers **`isometric_world_to_grid`** (or equivalent pilot) + **`compute-lib`**.

### 4.2 Systems map

| Component | Path |
|-----------|------|
| **Tool** **registration** | `tools/mcp-ia-server/src/index.ts` |
| **Handlers** | `tools/mcp-ia-server/src/tools/compute/*.ts` (suggested **folder**) |
| **Schemas** | `tools/compute-lib/src/schemas/*.ts` |
| **Verify** | `tools/mcp-ia-server/scripts/verify-mcp.ts` |
| **Unity** **bridge** | **TECH-38** **batchmode** **method** **TBD** |

## 5. Proposed Design

### 5.1 Tool catalog (target set)

| **Tool** **name** | **Source** **of** **truth** | **Purpose** |
|-------------------|-----------------------------|-------------|
| `isometric_world_to_grid` | **compute-lib** + **golden** | Pilot (**TECH-37**) |
| `growth_ring_classify` | **compute-lib** **parity** **with** **UrbanGrowthRingMath** | **Ring** **index** from **centroids** + **cell** |
| `grid_distance` | **compute-lib** | **Chebyshev** / **Manhattan** **between** **cells** (explicit **mode** **enum**) |
| `pathfinding_cost_preview` | **compute-lib** **v1** **simplified** **OR** **batchmode** **v2** | **Edge** **cost** **sample** **along** **segment** **(no** **full** **A***) |
| `desirability_top_cells` | **batchmode** **required** **when** **live** **grid** **needed** | **Top-k** **cells** **by** **score** **field** **export** |
| `geography_init_params_validate` | **Node** **only** | **Zod** **validate** **TECH-41** **GeographyInitParams** **subset** **(no** **Unity**)** — schemas aligned with **TECH-40** |

**Naming:** Final names **`snake_case`** **exactly** **as** **`registerTool`** **first** **argument**; **descriptions** **mention** **spec** **sections** **(geo** **§1,** **§10,** **sim** **§Rings)** **in** **English**.

### 5.2 Error and limit contract

**All** **tools** **return:**

```json
{ "ok": true, "data": { } }
```

**or**

```json
{ "ok": false, "error": { "code": "LIMIT_EXCEEDED", "message": "..." } }
```

**Limits:**

- **Max** **grid** **dimension** **256** **default** **(config** **constant**)** — **adjust** **in** **Decision** **Log** **if** **needed**.
- **Max** **centroids** **16**.
- **Timeout** **wrapper** **in** **handler** **(e.g.** **2s**)** **for** **batchmode** **spawn**.

### 5.3 Batchmode bridge (when implemented)

1. **MCP** **writes** **input** **JSON** **to** **temp** **file**.
2. **Spawn** **`Unity`** **with** **`-batchmode`**, **project** **path** **from** **`REPO_ROOT`**, **executeMethod** **`TerritoryTools.ComputeMcpExport`**, **args** **path**.
3. **Read** **output** **JSON**; **delete** **temps**; **return** **to** **model**.

**Security:** **Reject** **paths** **outside** **workspace** **root** **or** **/tmp** **allowlist**.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **Explicit** **`ok`** **boolean** | Easier **agent** **branching** **than** **exceptions** **only** |
| 2026-04-03 | **Batchmode** **for** **desirability** **top-k** | **C#** **authoritative** **scores** |

## 7. Implementation Plan

### 7.1 Package and folder structure

- [ ] Create **`tools/mcp-ia-server/src/tools/compute/`** directory.
- [ ] Move **pilot** **handler** **from** **inline** **`index.ts`** **into** **`compute/isometricWorldToGrid.ts`** **(refactor** **TECH-37** **if** **needed**)**.
- [ ] Add **`compute/index.ts`** **barrel** **export** **for** **handlers** **only** **(not** **automatic** **registration**)**.

### 7.2 Schema expansion in `compute-lib`

- [ ] **`schemas/growthRing.ts`**: **centroids** **array** **{** **x,** **y,** **weight?** **}**, **cell** **{** **x,** **y** **}**, **ring_thresholds** **number[]**.
- [ ] **`schemas/gridDistance.ts`**: **mode** **enum** **`chebyshev`** **|** **`manhattan`**.
- [ ] **`schemas/pathPreview.ts`**: **from** **cell**, **to** **cell**, **height** **strip** **or** **constant** **flag** **(v1** **may** **ignore** **slopes** **—** **document** **in** **tool** **description** **as** **approximation**)**.
- [ ] **`schemas/geographyParams.ts`**: **mirror** **TECH-41** **shipped** **fields** (and **TECH-40** **artifact** / **`schema_version`** policy when present).

### 7.3 Implement `growth_ring_classify`

- [ ] **`src/growthRing/classify.ts`** **in** **compute-lib** **implementing** **same** **logic** **as** **C#** **UrbanGrowthRingMath** **(copy** **tests** **from** **TECH-38** **vectors**)**.
- [ ] **`registerTool('growth_ring_classify', …)`** **description** **cites** **Urban growth rings**, **Urban centroid**, **sim** **§Rings**.
- [ ] **Verify** **fixture**: **two** **centroids**, **known** **cell** **→** **expected** **ring**.

### 7.4 Implement `grid_distance`

- [ ] **Pure** **function** **in** **compute-lib**.
- [ ] **Tool** **+** **verify** **(3** **cases** **per** **mode**)**.

### 7.5 Implement `pathfinding_cost_preview` (phased)

- [ ] **v1:** **Manhattan** **step** **count** **×** **constant** **(clearly** **labeled** **approximation**)** **OR** **flat** **per** **cell** **cost** **table** **from** **input** **JSON** **(agent** **supplies** **costs**)** — **pick** **one** **in** **PR** **and** **record** **in** **Decision** **Log**.
- [ ] **v2** **(optional** **milestone**)**:** **Unity** **batchmode** **returns** **true** **geo** **§10** **cost** **for** **2-cell** **move** **sample**.

### 7.6 Implement `geography_init_params_validate`

- [ ] **Zod** **`.safeParse`** **on** **input**; **return** **`errors`** **array** **(English** **messages**)**.
- [ ] **No** **Unity** **invocation**; **fast** **CI** **path**.

### 7.7 Implement `desirability_top_cells` (behind capability flag)

- [ ] **If** **TECH-38** **batchmode** **export** **exists:** **spawn** **Unity** **per** **§5.3**.
- [ ] **Else:** **register** **tool** **that** **returns** **`ok: false`,** **`code: NOT_AVAILABLE`** **with** **message** **pointing** **to** **TECH-38** **—** **verify** **asserts** **this** **shape** **(so** **tool** **exists** **but** **honest**)**.

### 7.8 Verification and docs

- [ ] **Extend** **`verify-mcp.ts`**: **one** **invocation** **per** **new** **tool** **(minimum** **happy** **path**)**.
- [ ] **Update** **`docs/mcp-ia-server.md`**: **tool** **count** **header** **“Tools** **(N)”**; **subsection** **“Computational** **tools** **(TECH-39)”**.
- [ ] **Update** **`tools/mcp-ia-server/README.md`** **mirror**.
- [ ] **Cross-link** **[TECH-36](TECH-36.md)** **in** **mcp** **doc** **Related** **issues**.

### 7.9 Rate limiting and ergonomics

- [ ] **Central** **`withLimits(toolFn)`** **wrapper** **in** **TypeScript** **checking** **input** **dimensions** **before** **compute**.
- [ ] **Log** **duration** **`console.error`** **diagnostic** **when** **`DEBUG_MCP_COMPUTE=1`** **(optional** **env**)**.

### 7.10 README for agents (`AGENTS.md` pointer optional)

- [ ] **One** **paragraph** **in** **`docs/mcp-ia-server.md`**: **when** **to** **use** **computational** **tools** **vs** **`spec_section`**.

## 8. Acceptance Criteria

- [ ] **≥ 4** **new** **tools** **beyond** **TECH-37** **pilot** **OR** **Decision** **Log** **explains** **consolidation**.
- [ ] **`npm run verify`** **green** **for** **mcp-ia-server**.
- [ ] **Docs** **updated** **(count** **+** **table** **+** **computational** **subsection**)**.
- [ ] **No** **tool** **returns** **plausible** **but** **wrong** **grid** **state** **without** **Unity** **—** **either** **compute-lib** **golden** **or** **NOT_AVAILABLE**.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

None — **batchmode** **availability** **gated** **honestly** **until** **TECH-38** **ships** **hooks**.
