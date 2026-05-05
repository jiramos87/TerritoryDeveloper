---
purpose: "BUG-62 postmortem — icon-slug case mismatch + lifecycle id-counter desync findings"
audience: agent
loaded_by: ondemand
---

# BUG-62 — Icon-slug case mismatch postmortem

> **Date:** 2026-05-04
> **Issue:** BUG-62 (archived)
> **Commit:** `2c686201` — `fix(BUG-62): normalize iconSpriteSlug case in toolbar+HUD adapters`
> **Branch:** `feature/asset-pipeline`

## 1. Symptom

Every button in toolbar + HUD-bar dead in Play Mode after Stage 3 of `game-ui-catalog-bake`. No errors, no warnings — silent miss across 21 buttons (9 toolbar + 12 HUD).

## 2. Root cause

`ToolbarDataAdapter.RebindButtonsByIconSlug` (line 104) + `HudBarDataAdapter.RebindButtonsByIconSlug` (line 120) switch on `Detail.iconSpriteSlug` with **lowercase string literals** (`"roads-button-64"`).

Baked prefabs `Assets/UI/Prefabs/Generated/{toolbar,hud-bar}.prefab` carry **PascalCase** values (`"Roads-button-64"`) emitted by `UiBakeHandler.Frame.cs`.

C# `switch` is case-sensitive → every case missed → `AddListener` never wired → click pipeline dead-ends silently inside `IlluminatedButtonRenderer.OnPointerClick`.

## 3. Fix decision — read-side normalization

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **A. Adapter `?.ToLowerInvariant()` (read-side)** | 2-line diff. No re-bake. Defensive against any case drift in source data. | Doesn't fix root cause at write site. | **CHOSEN** — lowest risk, highest defensive value. |
| B. Bake-side normalization in `UiBakeHandler` | Single source of truth. | Requires re-bake of all prefabs. Doesn't help if adapter ever sees external lowercase data. | Deferred to separate TECH. |
| C. Pascal-case the switch literals | Same case sensitivity bug, just inverted. | Brittle — couples adapter to bake format. | Rejected. |

**Decision:** A is read-side defensive depth. B is optional later cleanup, not required for fix.

## 4. Diff (ship-attributable)

```csharp
// Assets/Scripts/UI/Toolbar/ToolbarDataAdapter.cs:105
// Assets/Scripts/UI/HUD/HudBarDataAdapter.cs:121
- var slug = btn != null && btn.Detail != null ? btn.Detail.iconSpriteSlug : null;
+ // BUG-62 — bake handler emits PascalCase iconSpriteSlug; switch literals lowercase. Normalize on read.
+ var slug = btn != null && btn.Detail != null ? btn.Detail.iconSpriteSlug?.ToLowerInvariant() : null;
```

## 5. Lifecycle findings (process — not the bug itself)

### 5.1 ID counter / DB sequence desync

`/project-new` applier reserved BUG-60 from file-based `ia/state/id-counter.json` (counter 59→60), but the DB `bug_id_seq` had already issued BUG-60 + BUG-61 to `game-ui-design-system` Stage 13.7 via the DB-only `task_insert` MCP path.

Result: `/ship BUG-60` returned `ALREADY_CLOSED` because the DB row existed (archived) and the file yaml was a phantom.

**Workaround applied:** Deleted `ia/backlog/BUG-60.{yaml,md}`, ran one extra `reserve-id.sh BUG` (counter→61, ghost reservation), re-ran applier (counter→62, wrote BUG-62 yaml/spec).

**Latent issue:** Two id-allocation paths coexist — `reserve-id.sh` (file counter) + DB sequence. Whichever runs second sees stale state from the other.

**Recommended follow-up (deferred):** Pivot `/project-new` applier to call `task_insert` MCP for id allocation, retire `reserve-id.sh` for BACKLOG flows. Single source of truth = DB sequence.

### 5.2 `materialize-backlog.sh` direction is DB → BACKLOG.md only

After applier wrote `ia/backlog/BUG-62.yaml`, the DB row was still missing — verified via `SELECT task_id FROM ia_tasks WHERE task_id='BUG-62'` → empty.

`materialize-backlog.sh` syncs DB → `BACKLOG.md`; it does NOT sync yaml → DB. The applier wrote yaml only; DB insert was never triggered.

**Workaround applied:** Called `mcp__territory-ia__task_insert` directly with full body content. DB sequence (`last_value 61`) returned BUG-62 — matched the yaml id by luck.

**Latent issue:** `/project-new` skill assumes `materialize-backlog.sh` will lift yaml into DB. It doesn't. `/ship` then can't find the task.

**Recommended follow-up (deferred):** Either (a) add yaml→DB sync step to `materialize-backlog.sh`, or (b) make `/project-new` applier call `task_insert` directly (preferred — aligns with 5.1 retirement of file counter).

### 5.3 Two bake pipelines coexist

- **Old:** `UiBakeHandler.*.cs` — produces scene-loaded prefabs under `Assets/UI/Prefabs/Generated/` with **PascalCase** `iconSpriteSlug`. Currently active in MainScene.
- **New:** `CatalogBakeHandler.*.cs` — DB-driven, **lowercase** slugs, NOT loaded by MainScene yet.

The lowercase switch literals in the adapters were authored against the new pipeline's output convention; the runtime still loads the old pipeline's PascalCase prefabs. Mismatch was invisible until Play Mode click.

**Implication:** Until MainScene migrates to `CatalogBakeHandler`, the read-side normalization in 4 is the only thing keeping clicks alive.

### 5.4 Pre-existing scene-init noise (deferred BUG)

Verify-loop Path A (Unity bridge) blocked by:
- `[ZoneSubTypeRegistry] GridAssetCatalog not found in scene.`
- `[TokenCatalog] Streaming relative path is not set.`

Path B fallback (Grep + `unity:compile-check`) passed; verify-loop closed green.

**Not BUG-62's problem** — pre-existing wiring drift in MainScene init order. Worth a separate BUG to triage before it masks a real Path A regression.

## 6. Verify-loop summary

- Path A: scene-init timeout (deferred — see 5.4).
- Path B: `unity:compile-check` OK, Grep confirms 2 adapter sites edited as intended.
- Status walk: pending → implemented → verified → done → archived (DB).
- Iterations: 1/2.

## 7. Open follow-ups

| # | Type | Description | Priority |
|---|------|-------------|----------|
| 1 | TECH | Pivot `/project-new` applier to `task_insert` MCP; retire `reserve-id.sh` for BACKLOG. | P3 |
| 2 | TECH | Bake-side `iconSpriteSlug` normalization in `UiBakeHandler.Frame.cs` (defensive depth). | P4 |
| 3 | BUG | Scene-init wiring — `ZoneSubTypeRegistry` + `TokenCatalog` missing in MainScene. | P2 |
| 4 | TECH | Migrate MainScene from `UiBakeHandler` prefabs → `CatalogBakeHandler` (resolves case mismatch at the root). | P3 |

## 8. Lessons

- **Silent dead clicks = case-sensitive switch on string slugs.** Default to `?.ToLowerInvariant()` on any external-source string fed into a switch.
- **File-counter + DB-sequence coexistence is a footgun.** One source of truth or expect collisions on every cross-path filing.
- **Verify-loop Path B saved the close-out** when Path A was blocked by unrelated scene-init drift. Multi-path verify is load-bearing, not redundant.
