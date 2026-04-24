# Grid asset visual registry — Master Plan (Bucket 12 MVP spine)

> **Status:** In Progress — Stage 3.3 / TECH-772 (Stages 1.1–3.2 archived)
>
> **Scope:** Postgres-backed **grid asset catalog** (identity, sprites, economy, spawn pools) as source of truth; **HTTP + MCP** for agents; **Unity boot snapshot** consumed by **`GridAssetCatalog`** (no new singleton — Inspector + `FindObjectOfType` per `unity-invariants` #4); **Zone S** first consumer via **`ZoneSubTypeRegistry`** convergence; **`PlacementValidator`** owns place-here legality; **`wire_asset_from_catalog`** bridge kind for design-system-safe Control Panel wiring; export + import hygiene + IA scene contract. **Out:** sprite-gen composition logic (Bucket 5), deep sim rules beyond catalog reads, `web/` dashboard product UI (Bucket 9 transport only — this plan adds `/api/catalog/*` on the existing Next app). Post-MVP extensions → recommend `docs/grid-asset-visual-registry-post-mvp-extensions.md` (not authored by this workflow).
>
> **Exploration source:** `docs/grid-asset-visual-registry-exploration.md` (§8 Design Expansion — Chosen approach D, Architecture diagram, Subsystem impact table, Implementation points 1–12, Examples, Review notes; §4 locked decisions; §10 code refs).
>
> **Locked decisions (do not reopen in this plan):**
> - Catalog source of truth = **Postgres**; **`db/migrations/*.sql` is authoritative**. **`web/`** has **no Drizzle** (removed 2026-04-22 per `docs/architecture-audit-handoff-2026-04-22.md` Row 2); route/API typing uses **hand-written DTOs** under **`web/types/api/catalog*.ts`**. Unity loads **boot-time snapshot**; Resources JSON is **derived**, not authoritative.
> - **Sprite-first** authoring in DB rows; export step enforces **PPU / pivot** hygiene for allowlisted paths; **no collider** on baked world tiles under current **`GridManager`** hit-test contract.
> - Money in DB/API = **integer cents**; saves store stable **`asset_id`** (numeric PK); **`replaced_by`** soft-remap on load.
> - **Draft / published / retired** visibility; list defaults **published**; **`(category, slug)`** unique.
> - **Missing-asset policy:** dev = loud placeholder; ship = hide row + telemetry (per exploration §8.2).
> - **Concurrency:** optimistic **`updated_at`** on writes; conflicting PATCH returns retriable error.
> - **Bucket 12** child under `ia/projects/full-game-mvp-master-plan.md` (umbrella edit is a **separate** follow-up task, not auto-applied here).
>
> **Hierarchy rules:** `ia/projects/MASTER-PLAN-STRUCTURE.md` (canonical file + Stage block + 5-col Task table schema — authoritative). `ia/rules/project-hierarchy.md` (stage > task — 2-level cardinality). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable). `ia/rules/plan-apply-pair-contract.md` (§Plan section shape for pair seams).
>
> **Coordination:** **`ia/projects/ui-polish-master-plan.md`** owns widget/visual contracts; this plan owns **catalog + bridge recipes**. **`ia/projects/sprite-gen-master-plan.md`** feeds **`generator_archetype_id`** + paths. **`ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`** / **`ia/projects/session-token-latency-master-plan.md`** = registration-only follow-ups when new MCP kinds ship.
>
> **Read first if landing cold:**
> - `docs/grid-asset-visual-registry-exploration.md` — full design + §8 ground truth (amended 2026-04-22: **no Drizzle in `web/`**; DTOs in `web/types/api/`).
> - `docs/architecture-audit-handoff-2026-04-22.md` — **Pick 7** (Drizzle drop) + `docs/db-boundaries.md` when present.
> - `ia/specs/economy-system.md` §Zone sub-type registry (`lineStart` 28) + Zone S — **`ZoneSubTypeRegistry`** vocabulary.
> - `ia/specs/ui-design-system.md` §1 Foundations + §2 Components — **`UiTheme`**, **`IlluminatedButton`**, Control Panel paths (appendix lands Step 4).
> - `ia/specs/persistence-system.md` — Load pipeline order (`lineStart` 24) before mutating save fields.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — 2-level Stage/Task cardinality (≥2 tasks per Stage, hard; ≤6 soft).
> - `ia/rules/invariants.md` — #1 (specs vs `ia/projects/`), #2 (`reserve-id.sh`), #3 (MCP-first retrieval).
> - `ia/rules/unity-invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons — **`GridAssetCatalog`** is scene **`MonoBehaviour`**), #5 (no direct `cellArray` — **`PlacementValidator`** consumes **`GridManager`** API), #6 (do not grow **`GridManager`** — extract helpers).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Stage `Status:` uses enum `Draft | In Review | In Progress | Final` (per `ia/projects/MASTER-PLAN-STRUCTURE.md` §6.2). Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `plan-author` / `plan-digest` → `In Review`; `spec-implementer` → `In Progress`; `plan-applier` Mode stage-closeout → `Done (archived)` + Stage `Final` rollup.

### Stage index

- [Stage 1.1 — Migrations + Zone S seed](stage-1.1-migrations-zone-s-seed.md) — _Final_
- [Stage 1.2 — Catalog DTOs + API types (no Drizzle)](stage-1.2-catalog-dtos-api-types-no-drizzle.md) — _Final_
- [Stage 1.3 — Catalog API gap-patch: test harness + behavior fixes](stage-1.3-catalog-api-gap-patch-test-harness-behavior-fixes.md) — _Final_
- [Stage 1.4 — MCP `catalog_*` tools + allowlist](stage-1.4-mcp-catalog-tools-allowlist.md) — _Final_
- [Stage 2.1 — Export CLI + snapshot schema](stage-2.1-export-cli-snapshot-schema.md) — _Done_
- [Stage 2.2 — `GridAssetCatalog` runtime loader](stage-2.2-gridassetcatalog-runtime-loader.md) — _Final_
- [Stage 2.3 — Zone S consumer migration](stage-2.3-zone-s-consumer-migration.md) — _Final_
- [Stage 3.1 — `PlacementValidator` core API](stage-3.1-placementvalidator-core-api.md) — _Done — 5 tasks closed (**TECH-688**..**TECH-692**, all archived)_
- [Stage 3.2 — Ghost + tooltip integration](stage-3.2-ghost-tooltip-integration.md) — _Done — 2026-04-24 (5 tasks closed: **TECH-757**..**TECH-761**)_
- [Stage 3.3 — Save `asset_id` + `replaced_by` + sprite GC](stage-3.3-save-asset-id-replaced-by-sprite-gc.md) — _In Progress (tasks filed: TECH-772, TECH-773, TECH-774, TECH-775)_
- [Stage 4.1 — Bridge composite implementation](stage-4.1-bridge-composite-implementation.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4.2 — Transactional snapshot + dry-run](stage-4.2-transactional-snapshot-dry-run.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4.3 — IA scene contract + verification docs + glossary](stage-4.3-ia-scene-contract-verification-docs-glossary.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode stage-closeout) runs.
- Run `/stage-file ia/projects/grid-asset-visual-registry-master-plan.md Stage {N}.{M}` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs (planner→applier pair).
- Update Stage `Status` as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella **`full-game-mvp-master-plan.md`** when Bucket 12 row lands — separate PR/task from this file's author time.
- Extend via `/master-plan-extend {this-doc} {source-doc}` when a new exploration or extensions doc introduces new Stages — do NOT hand-insert Stage blocks.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal Stage landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into MVP stages — park them in `docs/grid-asset-visual-registry-post-mvp-extensions.md` once authored.
- Merge partial Stage state — every Stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file-apply` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend` so MCP context + cardinality gate + progress regen fire.
- Introduce new **singletons** for **`GridAssetCatalog`** — violates `unity-invariants` #4.

---

## Changelog

| Date | Note |
|------|------|
| 2026-04-21 | Orchestrator authored from `docs/grid-asset-visual-registry-exploration.md` §8 via `master-plan-new`. |
| 2026-04-24 | Canonical-shape refactor per `ia/projects/MASTER-PLAN-STRUCTURE.md`: dropped `### Step N` wrappers (Stages now flat siblings), promoted `#### Stage` → `### Stage`, demoted `### §Stage File Plan` / `### §Plan Fix` / `### §Stage Closeout Plan` → `#### §…`, stripped `**Phases:**` checkbox blocks, dropped Phase column from Task tables (5-col). Retroactive §Stage Audit sentinels for archived Stages (1.1–3.2) predating the 2026-04-24 lifecycle refactor; forward Stages 3.3 / 4.1 / 4.2 / 4.3 carry `_pending_` §Stage Audit sentinels. Header Hierarchy rules now cite `MASTER-PLAN-STRUCTURE.md` + `plan-apply-pair-contract.md`; Tracking legend replaced with canonical 4-value Stage enum. |
