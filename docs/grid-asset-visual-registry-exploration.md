# Grid asset visual registry & editor baker — exploration

> **Purpose:** Seed a **standalone master plan** (sibling to sprite-gen, not a sub-step of it) for a **coarse** Unity-side system: bind **Control Panel / toolbar art**, **on-grid visuals**, and **gameplay metadata** (costs, upkeep, and extensible columns) for every **placeable grid asset** that exposes a player-facing control — plus **agent-facing Unity Editor tooling**: extend or wrap the **IDE / Unity bridge mutation** surface so agents can **insert, modify, and wire** in-game **prefabs, buttons, panels, and hooks** that comply with the **game UI design system** (`ia/specs/ui-design-system.md`, UiTheme, flagship controls — **not** the `web/` Next.js stack). **First concrete consumer:** Zone S rows in `Assets/Resources/Economy/zone-sub-types.json` + `ZoneSubTypeRegistry`; the design generalizes to RCI zoning, roads, utilities, landmarks, and future generator-backed buildings.
>
> **Related:** `docs/isometric-sprite-generator-exploration.md`, `ia/projects/sprite-gen-master-plan.md` (Bucket 5), `ia/projects/full-game-mvp-master-plan.md`, `ia/projects/full-game-mvp-rollout-tracker.md`, `docs/zone-s-economy-exploration.md`, `ia/specs/economy-system.md`, `ia/specs/ui-design-system.md`, `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs`, `Assets/Scripts/Managers/GameManagers/GridManager.cs`, `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs`. *(Supersedes prior filename `docs/zone-s-sprite-registry-baker-exploration.md` — removed; use git history if needed.)*

---

## 0. Positioning vs sprite generator (coarse vs fine)

| Layer | Role | Owns |
|-------|------|------|
| **Sprite generator** (`tools/sprite-gen/`, Bucket 5) | **Fine-grained** rendering: YAML archetypes, palettes, slopes, layers, curation → PNGs under e.g. `Assets/Sprites/Generated/`. | Pixel-perfect isometric composition, variant counts, archetype IDs. |
| **Grid asset visual registry** (this exploration → future `grid-asset-visual-registry-master-plan.md`) | **Coarse** ingestion: accept **sprite paths** (hand-drawn or generator output), enforce **import hygiene** (PPU, pivot policy), **bake Unity prefabs** (world tile + button chrome), expose a **single runtime catalog** for placement + UI + metadata; **agent bridge** extensions/wrappers so MCP agents can **wire scenes** to that catalog under **game UI** constraints. | JSON/SO rows, editor baker, manifest, `ZoneManager` / placement hooks, Control Panel pairing, `unity_bridge_command` mutation kinds / composite recipes / docs. |

**Tight coupling, separate plans:** The generator **feeds** paths into registry JSON (or a manifest merge step); the registry **does not** re-implement composition. Contract between the two = **stable IDs** + **output folder convention** + optional **archetype / build fingerprint** column (see §3).

---

## 1. Problem and scope

### 1.1 Problem

- Placeable grid content needs **three surfaces** kept in sync: **world sprite**, **control button** (target / pressed / optional more), and **metadata** (economy, tooltips, save keys).
- Today, RCI leans on **Inspector lists** on `ZoneManager`; Zone S leans on **JSON** with `prefabPath` / `iconPath` and **deferred** world spawn. There is no **project-wide** pattern for “one logical row → grid + button + data.”
- **64×64** (or generator output) PNGs often import with **wrong PPU**; manual fixes do not scale.

### 1.2 Scope of the future master plan

- **In:** Editor baker, schema for registry rows, import allowlists, generated prefab layout, runtime loader, integration hooks for placement and UI, glossary / spec touchpoints, handoff from `sprite-gen`.
- **In (agents / bridge):** New or wrapped **Unity Editor mutation** operations, MCP **tool schemas** / **composite command recipes**, Editor menus, and **verification** steps so agents can place and connect **toolbar rows, modals, HUD strips, and registry-driven buttons** without violating **UiTheme** / **IlluminatedButton** / **ThemedPanel** patterns (per `ui-polish` + `ui-design-system` — see §4 sibling audit).
- **Out:** Generator geometry / palette / YAML (Bucket 5); deep simulation rules (other buckets consume **read** metadata from the catalog where possible).
- **Out:** `web/` dashboard UI (Bucket 9); Postgres **bridge transport** rewrite; generic MCP envelope redesign (owned elsewhere — this plan only **consumes** `unity_bridge_command` and adds **kinds** / docs / wrappers).

### 1.3 Sibling master-plan audit — who does **not** own this work

**Finding:** No existing child orchestrator under the full-game MVP umbrella files **pending** tasks whose primary deliverable is “extend Unity bridge mutations + MCP agent recipes for **in-game** Control Panel / registry-driven UI wiring.” Related plans touch adjacent surfaces only:

| Plan | Overlap | Gap vs this exploration |
|------|---------|-------------------------|
| **`ia/projects/ui-polish-master-plan.md` (Bucket 6)** | **Human-authored** studio prefabs (e.g. `IlluminatedButton` clusters, `ThemedPanel` rows), HUD binding tasks (`_pending_` Stage 11–12). | Does **not** specify **bridge mutation kinds**, **agent composable workflows**, or **registry-driven** instantiation of buttons from catalog rows. **Coordination:** ui-polish defines **visual/interaction contract**; **this** plan owns **agent-mechanical** wiring + catalog bake that **feeds** those prefabs. |
| **`ia/projects/sprite-gen-master-plan.md` (Bucket 5)** | Python `promote`, `.meta` (PPU), `Assets/Sprites/Generated/`. | **Output art only** — no Unity scene graph, no uGUI wiring, no Control Panel mutation API. |
| **`ia/projects/zone-s-economy-master-plan.md` (Bucket 3)** | `ZoneSubTypeRegistry`, empty `prefabPath` / `iconPath` (art deferred); UI modals already shipped. | **No** task for editor baker, sprite-first schema, or bridge tooling for Zone S chrome. |
| **`ia/projects/web-platform-master-plan.md` (Bucket 9)** | Tokens JSON “for future Unity” note. | **Explicitly** out of scope: Next.js only; **not** game UI. |
| **`ia/projects/session-token-latency-master-plan.md`** | Splits `territory-ia` vs `territory-ia-bridge` MCP servers; lists `unity_bridge_*` tools. | **Infrastructure** only — no feature work for **prefab/UI mutation recipes** or design-system compliance. |
| **`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`** | Mutation envelope, `caller_agent`, `unity_bridge_command` ergonomics. | **Cross-cutting MCP shape** — does **not** own **game-specific** mutation kinds or UiTheme validation. |

**Conclusion:** **Move** (scope here) all work on **agent-empowering bridge tools/wrappers** for **in-game** UI prefabs, panels, and catalog wiring into the future **`grid-asset-visual-registry-master-plan.md`**. Other plans **consume** outcomes (ui-polish: art direction; session-token / mcp-lifecycle: server registration only when new kinds ship).

### 1.4 Agent empowerment — Unity bridge mutations & MCP wrappers (program outline)

**Existing primitives (repo today):** `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` already implements Edit Mode **mutation kinds** (illustrative): `instantiate_prefab`, `apply_prefab_overrides`, `add_component`, `remove_component`, `set_component_property`, GameObject/scene lifecycle, asset moves, etc. Agents reach these via MCP **`unity_bridge_command`** (see `docs/mcp-ia-server.md`, verify-loop / ide-bridge-evidence skills).

**Gaps this master plan should close:**

1. **Design-system-safe recipes** — Documented **ordered sequences** (and optional **single composite kind**) for: instantiate registry-baked **ControlButton** prefab → parent under agreed **toolbar** path → set **UiTheme** / **IlluminatedButton** refs via `set_component_property` → wire **onClick** to existing `UIManager` entry points without ad-hoc string paths.
2. **Validation / guardrails** — Pre-flight checks: required components present, **ThemedPanel** tier, **GraphicRaycaster** on canvas when needed, **no** web-only components; optional **lint** pass (Editor script) before `save_scene`.
3. **Higher-level wrappers** — Thin C# helpers or **named mutation kinds** (e.g. `wire_registry_toolbar_button`) that encode defaults so agents do not hand-author 12 JSON payloads per button.
4. **Documentation + skills** — `docs/agent-led-verification-policy.md` / agent-lifecycle updates for “UI wiring via bridge”; skill chunk for **game UI** mutation (distinct from Play Mode evidence).
5. **MCP schema** — New `kind` values + DTO fields registered on **bridge** server; **caller_agent** allowlist updates per `tools/mcp-ia-server/src/auth/caller-allowlist.ts` policy (coordinate with mcp-lifecycle plan **only** for registration, not feature design).

**Explicit non-goals here:** Replacing the **Editor** with a full visual designer; automating **Play Mode**-only binding that cannot be expressed in Edit Mode.

### 1.5 First consumer (example)

- `zone-sub-types.json` + `ZoneSubTypeRegistry` migrate to the **generic** row shape (sprite-first paths, baked prefab manifest). Zone S remains the **reference implementation**, not the name of the umbrella system.

---

## 2. Colliders on world grid assets (audit summary)

`GridManager` picks cells via **screen-space rects** from **`SpriteRenderer.bounds`**, not `Physics2D` raycasts. Preview flows **strip** `Collider2D` from ghosts. **Conclusion:** Baked **world** prefabs should **default to no collider** unless a future feature requires physics. *(Prior draft had full step-by-step citations under the old doc name; see git history.)*

---

## 3. Metadata catalog — proposed columns

Rows describe one **placeable** (or **toolbar-selectable**) **grid asset kind** keyed for UI + simulation consumers. Not every column applies to every row; use **optional** fields and **channel** / **placementMode** to branch validation.

**Identity and routing**

| Column | Description |
|--------|-------------|
| `id` | Integer or string primary key within a **channel** (e.g. Zone S 0–6). |
| `stableSlug` | File-safe key for generated prefabs / saves (`police`, `water_tower_v1`). |
| `displayName` | Player-facing short label. |
| `descriptionKey` | Optional localization / tooltip table key. |
| `channel` | Enum: `StateService`, `Residential`, `Commercial`, `Industrial`, `Road`, `Water`, `Forest`, `Utility`, `Landmark`, `Power`, … |
| `placementMode` | How selection works: `ZoningPaint`, `SingleClick`, `Stroke`, `MenuPick`, … |
| `zoneType` / `enumKey` | Optional bind to `Zone.ZoneType` or future shared enum for save / stats. |
| `subTypeId` | Optional sidecar when one `zoneType` covers many rows (Zone S pattern). |

**Art inputs (sprite-first; baker resolves to prefabs)**

| Column | Description |
|--------|-------------|
| `worldSpritePath` | Resources-relative path to **on-grid** sprite (zoning overlay, building footprint, prop). |
| `buttonTargetPath` | Control strip / picker **default** sprite. |
| `buttonPressedPath` | **Pressed** (or primary active) sprite. |
| `buttonDisabledPath` | Optional grayed / locked state. |
| `buttonHoverPath` | Optional hover (if not color-only). |
| `minimapColor` | Optional override `Color` (hex or rgba in JSON); else channel default. |
| `sortingBias` | Optional int offset for `SpriteRenderer` / tile ordering. |

**Economy and upkeep (examples; align with Bucket 11 cost-catalog when it exists)**

| Column | Description |
|--------|-------------|
| `baseCost` | One-time place / commission cost. |
| `monthlyUpkeep` | Recurring maintenance. |
| `constructionTicks` | Optional delay before “built” state. |
| `demolitionRefundPct` | Optional bulldoze behavior. |
| `budgetEnvelopeId` | Which envelope draws spend (Zone S 0–6). |
| `costCatalogRowId` | Optional link to `CostTable` row when cost-catalog lands. |

**Simulation hooks (future-proof; optional per row)**

| Column | Description |
|--------|-------------|
| `footprintWidth` / `footprintHeight` | Default 1×1; multi-cell later. |
| `coverageRadius` | If services use radius (post-MVP). |
| `pollutionDelta`, `noiseDelta`, `desirabilityDelta` | When city-sim consumes catalog. |
| `powerMw`, `waterCapacity` | Utility-style stats if row represents a plant. |
| `unlocksAfter` | Tech / milestone id string. |
| `contributorComponent` | Optional hint: which `IMaintenanceContributor` / adapter to attach. |

**Pipeline and versioning**

| Column | Description |
|--------|-------------|
| `schemaVersion` | Row or file version for migrations. |
| `artRevision` | Hash or monotonic int when sprites change (cache bust). |
| `generatorArchetypeId` | Link to `tools/sprite-gen` YAML id when PNG came from generator. |
| `generatorBuildFingerprint` | CLI version / git SHA of sprite-gen that produced the PNG. |
| `sourceAssetGuid` | Optional Unity GUID of source texture (Editor manifest). |

**Persistence**

| Column | Description |
|--------|-------------|
| `savePrefabLogicalId` | Stable string stored in `CellData` / save DTO for remap across renames. |
| `deprecated` | Bool; hide from UI but keep for load migration. |

**Implementation note:** `JsonUtility` cannot express rich optional sets cleanly; long-term options include **nested objects** via `Newtonsoft` (if introduced), **ScriptableObject** authoring with JSON export for CI, or **split files** (economy.json + art-manifest.json).

---

## 4. Locked decisions (carried from prior exploration)

| Decision | Choice |
|----------|--------|
| Visual authoring in data | **Sprite paths** (world + button states) as primary author input; **editor baker** emits prefabs + optional manifest. |
| Prefab creation | **Editor-time** bake, not shipping runtime synthesis only. |
| PPU | **Centralize** (e.g. 64) for **allowlisted** paths or manifest-driven imports; avoid global blanket postprocessors. |
| Grid hit testing | **No collider** requirement for baked world tiles under current `GridManager` contract. |

---

## 5. Editor baker — high-level architecture (generic)

1. **Input:** Channel-specific JSON (e.g. `zone-sub-types.json`) or unified `grid-asset-registry.json` + templates under `Assets/Prefabs/Templates/GridAsset_*`.
2. **Import hygiene:** Set `TextureImporter.spritePixelsPerUnit`, filter mode, pivot per policy for listed paths only.
3. **Outputs:** `Assets/Prefabs/Generated/{Channel}/{slug}_World.prefab`, `{slug}_ControlButton.prefab` (names TBD), plus optional **`grid-asset-registry.baked.manifest`** (GUIDs).
4. **Runtime:** Single loader service reads **economy + manifest**; placement and UI query by `id` / `stableSlug`.

Placement must eventually wire **all** channels through a **shared** “instantiate from catalog” path where feasible (Zone S first).

---

## 6. PPU and blast radius

Same mitigation as prior draft: **folder allowlist**, **JSON-driven path list**, or **manifest-only** touches. Never project-wide PPU rewrite without audit.

---

## 7. Open questions (next passes)

Many items from the prior Zone-S-specific §7 apply **mutatis mutandis** to **multi-channel** registry: schema versioning, Resources vs GUID manifest, partial rows, save stability, UI templates, atlas workflow, baker idempotency, git policy for generated assets.

**Additional cross-channel questions:**

- **Single file vs per-channel files:** One `grid-asset-registry.json` with `channel` column vs `Resources/Economy/`, `Resources/Zoning/`, etc.?
- **Inspector override:** Does `ZoneManager` remain authoritative for RCI prefab lists with **optional** “sync from registry” bake, or full migration to catalog-only?
- **Cost-catalog precedence:** When Bucket 11 lands, do `baseCost` / `monthlyUpkeep` **move out** of this JSON into `CostTable` with only **foreign keys** here?
- **UiTheme:** Mandatory themed button template vs raw Image swap?
- **Web / dashboard:** Does any row surface in web tooling (Bucket 9) require **non-Unity** export of the same manifest?

### Schema, validation & migration

- **Validation surface:** Where does row validation run — JSON Schema in CI, Editor `OnValidate` on a ScriptableObject mirror, runtime asserts, or all three — and which classes of error must **block** the bake vs only warn?
- **Optional-column rollout:** When a new optional column lands mid-cycle, is the contract silent-default, explicit-null, or **forced re-bake** of every row so that absence is distinguishable from un-migrated?
- **`stableSlug` scoping:** Global uniqueness across channels, or namespaced per `channel`? What enforces collision rejection, and does renaming a slug require a save-migration shim?
- **Localization plumbing:** Which localization pipeline does `descriptionKey` resolve against — an existing catalog, a new table, or inline fallback — and how does the baker surface missing keys?
- **Numeric precision:** Are economy fields (`baseCost`, `monthlyUpkeep`, `demolitionRefundPct`) integers only, fixed-point, or floats — and who enforces rounding at the simulation boundary to keep saves deterministic?
- **Deprecation contract:** Does `deprecated: true` keep the prefab baked (for load of old saves) or strip it while preserving only the `savePrefabLogicalId` mapping? Is there a grace period / two-release window?
- **Partial rows:** What is the minimum column set for a row to bake successfully — is a row with sprites but no economy columns a build-breaker, a warning, or a valid “art-only” preview row?

### Baker determinism & CI

- **Incremental vs full:** Is the bake hash-driven per row, channel-scoped, or full-rebuild? What inputs participate in the cache key (JSON, source sprite GUID, template prefab, baker version)?
- **GUID stability:** Do baked prefab **GUIDs** stay stable across machines and across re-bakes, and how is `.meta` churn kept out of code-review diffs?
- **CI enforcement:** Does CI fail when baked artifacts are stale relative to JSON, or only when the bake command errors? Is there a `bake --check` mode?
- **Orphan sweep:** When a row is removed or `stableSlug` is renamed, who deletes the corresponding `Generated/` prefab — the baker, a separate janitor step, or manual cleanup?
- **Dry-run preview:** Is there a preview mode that emits the prospective diff (prefabs added / modified / removed) before writing, suitable for agent or PR-bot review?
- **Parallelism ceiling:** At what row count (100s? 1000s?) does single-threaded bake become a CI bottleneck, and is the design forward-compatible with parallel / sharded bakes?

### Runtime catalog lifecycle

- **Load strategy:** Is the catalog eagerly loaded at boot, lazily per channel, or streamed — and what is the acceptable steady-state memory budget on target hardware?
- **Missing-asset fallback:** When a runtime lookup finds a missing sprite or prefab, does it resolve to a placeholder, hard-fail, or channel-level default — and should the fallback differ between dev builds and release?
- **Hot reload:** Can designers edit JSON in Play Mode and see changes without a domain reload, or is restart required? What is the story for sprite swaps during Play?
- **Save compatibility window:** How many schema versions back must the loader accept saves from, and where does that migration code live — on the row, on the loader, or a dedicated migration table?
- **Threading:** Is the catalog read-only on the main thread only, or does any simulation subsystem read it from worker threads (requires immutability guarantees)?

### Sprite & theming variants

- **Theme variants:** Does the registry express theme / palette variants (dark mode, colorblind palettes, seasonal skins) natively, or is that a separate swap layer that keys off `stableSlug`?
- **Atlas strategy:** Per-channel atlas, shared button atlas, auto-grouping via Sprite Atlas v2, or no atlas — and who owns the decision per channel?
- **9-slice vs fixed:** Do button chromes need 9-slice scaling for variable label widths, or is the design system committed to fixed-width button sprites across all rows?
- **Animated sprites:** Are animated world tiles in scope (AnimatorController binding in the baked prefab) or strictly static for v1, with animation deferred to a future column?
- **Pivot policy split:** Do world tiles and button sprites share a pivot policy, or is there a per-channel split — and how is that enforced by import hygiene?

### Placement & footprint semantics

- **Multi-cell anchor:** For `footprintWidth × footprintHeight > 1×1`, which cell is the anchor (origin corner, centroid, user-click)? Are rotations allowed, and do they re-key the footprint?
- **Overlap rejection:** Who owns the “cannot place here” UX — the registry-driven placer, `ZoneManager`, or a shared validator — and where do we surface why (cost, footprint, unlock gate)?
- **Unlock gating:** Who enforces `unlocksAfter` — the registry loader, `ZoneManager`, or a separate tech-tree service — and does the toolbar hide locked rows or show-disabled?
- **Affordability gating:** Does `baseCost` vs current budget gate **selection** in the toolbar, **commit** only, or both? Who drives the disabled-button state?
- **Preview ghost source:** Is the preview ghost sprite derived from `worldSpritePath` with a tint, a dedicated `previewSpritePath`, or a separate generator variant?
- **Bulldoze adjacency:** Does `demolitionRefundPct` interact with chains (e.g. roads touching zoned lots) — and is that policy expressed in the row or in a separate demolition service?

### Generator ↔ registry handshake

- **Regeneration GC:** When sprite-gen regenerates an archetype, how are superseded PNGs garbage-collected without breaking baked prefabs that still reference them by path / GUID?
- **Mixed provenance:** Can a single row mix hand-drawn button chrome with generator-produced world sprite, and how does `artRevision` reconcile dual provenance?
- **Fingerprint mismatch:** Does a mismatched `generatorBuildFingerprint` warn, block the bake, or auto-rebake — and who is responsible for re-running the generator?
- **Reverse lookup:** Given a baked prefab on disk, can an agent resolve back to the originating JSON row + generator archetype for debugging, or is the trail one-way?
- **Curation gate:** If sprite-gen has a curation step, can an un-curated archetype land in a registry row via typo, or does the baker hard-reject unknown `generatorArchetypeId`?

### Authoring ergonomics & governance

- **Primary authoring UI:** Do designers edit rows via a ScriptableObject inspector with JSON export, raw JSON, or a shared Editor window — and which is the source of truth if they drift?
- **Schema-change review:** What is the process for adding or changing a column — PR label, dedicated CODEOWNERS on the schema file, an RFC doc, or an Editor-enforced version bump?
- **Analytics contract:** Do placement / demolition events carry `stableSlug` (and channel, archetypeId) for downstream telemetry, and is that contract owned here or by the analytics subsystem?
- **Row templates / inheritance:** Does the schema support prototype rows (e.g. a base “residential-small” row that specific rows inherit overrides from), or is every row flat and self-contained?
- **Bulk ops:** Is there tooling for bulk renames, channel migrations, or cost-catalog extraction, or are those manual JSON edits + re-bakes?

### Agent / bridge UI wiring

- **Composite vs many round-trips:** Single `unity_bridge_command` batch (if MCP adds it) vs documented multi-step recipe — performance + timeout policy (`UNITY_BRIDGE_PIPELINE_CEILING_MS`).
- **Edit Mode vs Play Mode:** Mutations are Edit Mode today — how to validate **Play Mode** clicks (handoff to verify-loop / test scene)?
- **Scene contract:** Canonical paths for toolbar root, HUD, `UIManager` hooks — single doc so agents parent under the correct canvas.
- **Security:** Which mutation kinds are agent-allowlisted (`caller_agent`) vs human-only for grid-asset automation?

### Agent recipe lifecycle

- **Idempotency:** Does re-running a toolbar-wire recipe **patch** existing prefabs in place or **duplicate** them? How are partially-applied recipes detected?
- **Transactional rollback:** When a composite recipe half-completes, is there a transactional undo (snapshot-restore), a replay-with-skip, or manual cleanup owned by the agent?
- **Reviewability:** How does a human review agent-driven scene edits before merge — scene-diff snapshot, screenshot artifact, Play Mode smoke test output, or all of the above?
- **Design-system drift:** How do pinned recipes version against UiTheme / IlluminatedButton evolution — hash-pinned inputs, forced re-bake on design-system release, or runtime contract assertions?
- **Concurrent editing:** What conflict-resolution applies when an agent edits a scene that a human is also editing — Git merge, Editor file lock, or serialized queue on the bridge?
- **Budget caps:** Are agent bridge calls budgeted per session (call count, wall-clock, mutated-GameObject count), and where is that cap defined — `caller-allowlist.ts`, env var, or per-recipe metadata?
- **Observability:** What telemetry does each recipe emit (recipe id, caller_agent, success / partial / fail, prefabs touched) and where is it aggregated for post-incident review?
- **Dry-run surface:** Can an agent request a **preview** of a recipe (diff of scene + prefab changes) without committing, analogous to baker dry-run, to support human-in-the-loop gating?
- **Template discovery:** How does an agent discover which baked prefab `id` / `stableSlug` to use — a bridge-exposed catalog query, a static manifest, or trial-and-error?

*(Full granular open questions from the 2026-04-21 draft are folded into implementation planning when `/design-explore` expands this doc.)*

---

## 8. Design Expansion

> Populated by `/design-explore` interview on 2026-04-21. Supersedes the three-approach stub (A unified JSON / B per-channel JSON / C ScriptableObject-primary) with a **DB-backed** approach (D) that evolved from the interview.

### 8.1 Chosen approach — **(D) Postgres-backed catalog, endpoints-first, Unity reads a boot-time snapshot**

The catalog's source of truth is the existing **Postgres** database (SQL migrations under `db/migrations/` are authoritative; **`web/`** uses hand-written DTOs — **no Drizzle** post-2026-04-22 audit), not `Resources/*.json` files. Files become a **derived export** produced by a build step for two reasons: (a) sprite-gen round-trip (YAML ↔ archetypes ↔ curation queue), (b) shipped-build snapshot the Unity client loads once at boot.

Logical tables (names illustrative):

- `catalog_asset` — identity row (id PK, category FK, slug, display_name, status ∈ {draft, published, retired}, replaced_by FK nullable, footprint_w, footprint_h, placement_mode, unlocks_after, has_button bool, updated_at).
- `catalog_sprite` — one row per concrete sprite (id PK, path, ppu, pivot, provenance ∈ {hand, generator}, generator_archetype_id nullable, generator_build_fingerprint nullable, art_revision).
- `catalog_asset_sprite` — M:N binding slot (asset_id, sprite_id, slot ∈ {world, button_target, button_pressed, button_disabled, button_hover}). Each slot is independent so provenance can mix.
- `catalog_economy` — economy rows keyed by asset_id (base_cost_cents, monthly_upkeep_cents, demolition_refund_pct, construction_ticks, budget_envelope_id, cost_catalog_row_id nullable). Separate table so Bucket 11's cost-catalog can own this cleanly without schema churn.
- `catalog_spawn_pool` — named pool (id PK, slug, owner_category, owner_subtype).
- `catalog_pool_member` — pool membership with weight (pool_id, asset_id, weight). **Pool-centric editing**; the asset page shows membership read-only.
- `catalog_asset_translation` — i18n (asset_id, locale, display_name, description). **Post-MVP**; MVP keeps English inline on `catalog_asset`.

**Invariants.** `(category, slug)` unique. Money is **integer cents**. `art_revision` increments when a sprite's bytes change so caches bust. Saves store `asset_id` (stable numeric PK) — renames are cosmetic.

### 8.2 Architecture

<!-- catalog-snapshot-schema-TECH-663: top-level `{ schemaVersion, generatedAt, assets[], sprites[], … }` matches hand-written DTOs in `web/lib/catalog/build-catalog-snapshot.ts`; stable key order is enforced at stringify time (see `tools/docs/catalog-snapshot-schema.md`). -->

```
DB (Postgres)
  ├── catalog_asset, catalog_sprite, catalog_asset_sprite
  ├── catalog_economy
  └── catalog_spawn_pool, catalog_pool_member
        │
        ▼
Backend CRUD endpoints  ── raw SQL escape hatch (agent-callable)
        │        └── MCP: catalog_list / catalog_get / catalog_upsert / catalog_pool_* (agent-safe, respects draft/published filter)
        │
        ├── Web admin UI (future, Shopify-style: list/show/create/edit/diff-preview)
        └── Export step → Unity-consumable snapshot + sprite import hygiene (PPU, pivot) — default file `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json` (Stage 2.1 TECH-664 §7 / §Findings; `StreamingAssets` over `Resources` for raw JSON)
                    │
                    ▼
              Unity client
              ├── GridAssetCatalog (boot loader, in-memory snapshot)
              ├── ZoneSubTypeRegistry (wraps catalog for Zone S today; converges long-term)
              ├── ZoneManager / GridManager / CursorManager (consume catalog)
              ├── PlacementValidator (single owner of can-place rules, drives green/red ghost)
              ├── UI layer: IlluminatedButton / ThemedPanel bound to live UiTheme
              └── Agent bridge: wire_asset_from_catalog (high-level, idempotent, transactional, dry-run)
```

**Runtime snapshot lifecycle.** Shipped builds load once at boot, cache in RAM. Dev / Editor also loads at boot but honours a **hot-reload signal** (DB change → broadcast → client refreshes) so designers see tweaks within seconds. Shipped players never see mid-session catalog mutation.

**Missing-asset policy.** Dev builds render a **loud pink placeholder** for any row that fails to resolve a sprite. Shipped builds silently skip the asset (hide from placement UI) and emit telemetry. No hard-fail on boot.

**Concurrency.** Shared DB across parallel agent worktrees. Writes use **optimistic locking** (`updated_at` check); conflicting writes return an error and the caller re-reads.

### 8.3 Subsystem impact

| Subsystem | Change |
|-----------|--------|
| **DB (`db/migrations/`)** | New migration series (`0011_catalog_*`) introducing the seven tables above. **Amendment 2026-04-22:** `web/` does **not** use Drizzle; hand-written DTOs in **`web/types/api/catalog*.ts`** (see `docs/architecture-audit-handoff-2026-04-22.md` Row 2). |
| **Backend endpoints** (`web/app/api/`) | New CRUD routes under `/api/catalog/*`. Optimistic-lock semantics. Draft/published filter on list. Preview-diff endpoint powers both the admin UI and agent dry-run. |
| **MCP bridge (`tools/mcp-ia-server/`)** | New `catalog_*` tool family, agent-safe. `unity_bridge_command` gains the `wire_asset_from_catalog` kind (high-level, idempotent, transactional, dry-run flag). Allowlist updates in `caller-allowlist.ts`: add + modify free, delete guarded. |
| **`Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs`** | Implements the `wire_asset_from_catalog` composite: instantiate → parent under scene-contract path → bind UiTheme → hook onClick. Snapshot + rollback on failure. |
| **`ZoneSubTypeRegistry` / `ZoneManager`** | Read from `GridAssetCatalog` (boot snapshot) for Zone S today. RCI / utilities / landmarks migrate category-by-category. Inspector lists retire as each category converges. |
| **`GridManager` / `CursorManager`** | Unchanged hit-testing contract (screen-space bounds, no colliders on baked world tiles). Preview ghost uses world sprite + runtime tint (green valid / red invalid). |
| **`PlacementValidator` (new or promoted)** | Single owner of place-here legality: footprint, zoning match, unlock gate, affordability. Drives ghost tint + tooltip reason. |
| **`GameSaveManager`** | Saves store `asset_id` (stable numeric PK). Soft-retired rows keep loading; if `replaced_by` is set, load-time remap swaps the ID transparently. |
| **`ia/specs/ui-design-system.md`** | Add an appendix **scene contract**: canonical parent paths (toolbar root, HUD strips, modal host) so agents never guess. |
| **sprite-gen** (Bucket 5) | Curation gate: generator output lands in a `pending_review` bucket; designers promote to `catalog_sprite` rows. `generator_archetype_id` + `generator_build_fingerprint` flow into the sprite row. |
| **Cost catalog** (Bucket 11, future) | `catalog_economy.cost_catalog_row_id` FK lets prices migrate out when the cost-catalog lands. No schema churn on the asset row. |

### 8.4 Implementation points

1. **Migrations** — add `0011_catalog_core.sql` (asset / sprite / binding / economy) and `0012_catalog_spawn_pools.sql` (pool / member). Seed Zone S seven rows as the first fixture.
2. **TypeScript DTOs** — under `web/types/api/catalog*.ts` (hand-written, aligned to migrations; no Drizzle in `web/` per architecture audit 2026-04-22). Optional **zod** at route boundary.
3. **Backend endpoints** — `GET /api/catalog/assets`, `GET /api/catalog/assets/:id`, `POST`, `PATCH` (optimistic lock), `POST /api/catalog/assets/:id/retire`, `POST /api/catalog/preview-diff`. Raw SQL stays available via the existing agent SQL tool as the escape hatch.
4. **MCP `catalog_*` tools** — thin wrappers over the endpoints so agents get typed tool schemas and dry-run flags.
5. **Export step** — script under `tools/` that reads the DB and writes a snapshot artefact Unity loads at boot. Dev path supports hot-reload; shipped builds embed the snapshot.
6. **`GridAssetCatalog`** — MonoBehaviour singleton (or static) under `Assets/Scripts/Managers/GameManagers/`. Queries by id or (category, slug). Publishes `OnCatalogReloaded` for hot-reload subscribers.
7. **`PlacementValidator`** — single class that answers `CanPlace(assetId, cell, rotation)` with a structured reason code. Ghost tint + tooltip read its output.
8. **`wire_asset_from_catalog`** — composite bridge kind in `AgentBridgeCommandRunner.Mutations.cs`. Steps: snapshot scene → resolve catalog row → instantiate button prefab template → parent per scene-contract → bind UiTheme + IlluminatedButton refs → wire onClick to existing `UIManager` entry point → save scene → optional Play-Mode smoke check via verify-loop. Rollback on any step failure.
9. **Scene-contract appendix** — add to `ia/specs/ui-design-system.md` enumerating toolbar root, HUD strip, modal host, Control Panel mount. Agents read this before any wiring.
10. **Dry-run** — every mutation supports `dry_run: true`, returning the planned diff without side effects. Powers both agent plans and the admin preview.
11. **Sprite GC** — periodic janitor (CI job or admin button) sweeps `catalog_sprite` rows with refcount 0 across `catalog_asset_sprite` AND `catalog_pool_member`.
12. **Draft vs published status** — endpoints default to `status = published`; admin can opt into draft visibility.

### 8.5 Examples

- **Zone S, 7 rows end-to-end.** `catalog_asset` rows for State Service subtypes 0–6. Each binds a world sprite (hand-drawn or generator-curated) and a `button_target` / `button_pressed` pair. Economy rows set base cost + monthly upkeep per envelope. `ZoneSubTypeRegistry` queries `GridAssetCatalog` at boot; no Inspector list.
- **Residential subtype with spawn pool.** `HeavyResidential` is a **tool** (button + picker). Its `catalog_spawn_pool` row lists N building `catalog_asset` rows (pool-only, `has_button = false`), each with a weight. Placement: player zones a cell; the zoning tick picks a pool member by weight and spawns its world prefab.
- **Agent adds a new monorail tool.** Human: "Add a monorail tool to the transport toolbar." Agent calls `catalog_upsert` (creates asset row + sprite bindings), then `wire_asset_from_catalog` with dry-run first (prints the plan), then for real. verify-loop runs a Play-Mode smoke check: clicks the new button, confirms world spawn on a test cell. Rollback if any step fails.

### 8.6 Review notes

Resolved during the interview — folded into the sections above:

- Catalog layout, runtime source, money precision, deprecation, draft/published, uniqueness, rebuild speed, diff preview, rename behaviour, hot-reload, missing-asset policy, save longevity, variants, animation, button sizing, affordability / unlock / multi-cell / ghost UX, curation gate, mixed provenance, GC, spawn-pool editing model, duplicate-vs-inheritance, analytics (none v1), bridge editing (endpoints-first + agent SQL), agent wiring command, scene contract, Play-Mode verify, agent permissions, idempotency, rollback, review gate (verify-loop, no per-change PR), concurrency, dry-run, catalog discovery, theme drift, localization (post-MVP), placement validator, umbrella bucket (new Bucket 12).

Non-blocking items carried to the master plan:

- **Per-session agent budget caps** — mirror existing `caller-allowlist.ts` + `UNITY_BRIDGE_PIPELINE_CEILING_MS` conventions; revisit when real workloads exist.
- **Sprite-atlas strategy** — defer to first performance pass; the schema does not assume an atlas either way.
- **Bulldoze refund adjacency** — gameplay rule, not catalog shape; owned by the economy master plan.
- **Reverse lookup from baked artefact → DB row** — add in the export step once the snapshot format is finalised.
- **Bulk rename / bulk category migration tooling** — admin v2 feature.
- **Parallelism / threading ceilings** — engineering detail at scale-out time; current catalog size does not warrant it.
- **Full localization pipeline** — the table shape is ready; wiring happens when first non-English locale is scoped.

### 8.7 Expansion metadata

| Field | Value |
|-------|-------|
| Date | 2026-04-21 |
| Model | claude-opus-4-7 |
| Approach selected | **(D) DB-backed catalog, endpoints-first, Unity snapshot at boot** (synthesises prior A/B/C into a DB-centric approach per user direction) |
| Blocking items resolved | 0 (no subagent review run — live user interview session) |
| Umbrella placement | New **Bucket 12** in `ia/projects/full-game-mvp-master-plan.md` (per §9.1 Option A, confirmed during interview) |

---

## 9. Umbrella integration — proposals (apply when promoting to master plan)

*The following edits are **not** applied in this commit; they are instructions for humans / agents when the child orchestrator is filed.*

### 9.1 `ia/projects/full-game-mvp-master-plan.md`

- **Option A (preferred):** Add a **new bucket** (e.g. **Bucket 12** `grid-asset-visual-registry`) with child orchestrator `ia/projects/grid-asset-visual-registry-master-plan.md`, tier **A or B**, depends on **Bucket 5** (generator output paths) and **consumes** Bucket 3 / 4 / 6 placement surfaces. Update bucket count line and any “11 buckets” wording.
- **Option B:** **Extend Bucket 5** (`sprite-gen-and-animation`) with a **Step** “Registry bake + manifest” — only if team wants a **single** orchestrator; risk: mixes Python tool + Unity editor pipeline in one plan.
- **Option C:** **Extend Bucket 6** (`ui-polish`) — **rejected as sole owner:** ui-polish correctly owns **studio-rack polish + widget contracts**, but **does not** own bridge mutation programs (§1.3). Optionally add **cross-link only**: “Control Panel wiring automation → `grid-asset-visual-registry-master-plan`.”

**Cross-links:** In Bucket 3 (`zone-s-economy`) and Bucket 5 rows, add **“See also grid-asset-visual-registry”** when registry bake blocks Zone S art completion. In Bucket 6 (`ui-polish`), add **See also** when flagship toolbar tasks need **agent-driven** prefab wiring (§1.4).

### 9.2 `ia/projects/full-game-mvp-rollout-tracker.md`

- Add a **new rollout row** after policy review:
  - **Row slug:** `grid-asset-visual-registry`
  - **Bucket:** map to chosen umbrella bucket (12 or 5 per §9.1).
  - **Tier:** A–B (foundational art path).
  - **(b) Explore:** this file path; **Design Expansion** must be filled (§8) before `(b)` ticks `✓`.
  - **(c) Plan:** `ia/projects/grid-asset-visual-registry-master-plan.md` after `/master-plan-new`.

### 9.3 `ia/projects/sprite-gen-master-plan.md`

- Add **exit-criteria / handoff** bullet: promoted PNGs (or curation queue) expose **stable `archetypeId`** + **relative path** consumable by **grid-asset-visual-registry** baker without manual retyping.
- Optional **new Stage** or **Step 1.x** task: “Emit `sprite-gen-manifest.json` fragment” listing `{ archetypeId, outputPngPath, recommendedPpu, pivot }` for registry merge.
- Cross-link **this exploration** from Scope or Read-first.

### 9.4 `docs/isometric-sprite-generator-exploration.md`

- Add subsection **“Consumer: grid asset visual registry”**: output folder layout under `Assets/Sprites/Generated/` (or agreed path), naming convention for **slug ↔ archetypeId**, and whether generator runs **before** or **inside** CI relative to Unity baker.
- Lock whether **curation promote** step copies files into `Resources/` or **only** updates manifest with project-relative asset paths (Editor-only resolution).

### 9.5 Durable IA

- When the child plan ships: glossary rows e.g. **Grid asset catalog**, **Grid asset baker**, **Art manifest (grid)**; extend `ia/specs/economy-system.md` or add `ia/specs/grid-asset-catalog.md` per terminology rules.

### 9.6 Sibling plans — coordination only (no task migration into them)

- **`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`** / **`docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md`:** If new `unity_bridge_command` **kinds** need envelope or allowlist updates, file **minimal** follow-up tasks there; **feature design** stays in **grid-asset-visual-registry** orchestrator.
- **`ia/projects/session-token-latency-master-plan.md`:** Same — **server split / tool list** only if new bridge tools are registered.
- **`ia/projects/ui-polish-master-plan.md`:** **No** expectation to add “bridge mutation” tasks; ui-polish remains **authoring + runtime HUD** quality. Reference this exploration when polish **depends** on catalog-baked prefabs.

---

## 10. Key code references (audit trail)

- Cell picking: `GridManager.TryGetCellBaseTileScreenBounds`, `GetCellFromWorldPoint`, `GetMouseGridCell`.
- Zone S registry: `ZoneSubTypeRegistry`, `Assets/Resources/Economy/zone-sub-types.json`.
- Preview collider strip: `CursorManager`, `UrbanizationProposalManager`.
- Unity Editor bridge mutations (agent path): `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` (partial class), `unity_bridge_command` MCP kind — see `docs/mcp-ia-server.md`.
- Game UI norms: `ia/specs/ui-design-system.md`, `ia/projects/ui-polish-master-plan.md` (widget targets, not bridge ownership).

---

## 11. Changelog

| Date | Note |
|------|------|
| 2026-04-21 | Initial Zone-S-focused draft (`zone-s-sprite-registry-baker-exploration.md`): colliders, baker, PPU, open questions. |
| 2026-04-21 | Expanded open questions (schema, persistence, UI, tooling). |
| 2026-04-21 | **Retitled and generalized** to `grid-asset-visual-registry-exploration.md`: multi-channel scope, metadata catalog, sprite-gen vs registry positioning, umbrella + sprite-gen doc **proposals** §9, Design Expansion stub §8. |
| 2026-04-21 | §1.3 sibling master-plan audit; §1.4 agent empowerment (bridge mutations, MCP wrappers, game UI design system); §7 Agent/bridge UI wiring questions; §9.1 Option C clarified; §9.6 coordination-only siblings; §10 bridge refs. |
| 2026-04-21 | §7 expanded — added open-question clusters: schema/validation/migration, baker determinism & CI, runtime catalog lifecycle, sprite & theming variants, placement & footprint semantics, generator↔registry handshake, authoring ergonomics & governance, agent recipe lifecycle. No existing questions resolved. |
| 2026-04-21 | **§8 Design Expansion populated** via `/design-explore` user interview. Approach D (DB-backed catalog, endpoints-first, Unity snapshot) supersedes the A/B/C stub. Locked: data-model tables, spawn-pool model, draft/published, soft-retire + stable IDs, integer-cents money, runtime hot-reload policy, missing-asset policy, agent wiring command + scene-contract appendix + Play-Mode verify, optimistic-lock concurrency, transactional dry-run recipes. Umbrella placement confirmed as new Bucket 12 of `full-game-mvp-master-plan.md`. |
