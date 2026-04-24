# Isometric Sprite Generator — Master Plan (Tools / Art Pipeline)

> **Status:** In Progress — Stage 7 next (Stages 1–6.7 + 7 addendum + 9 addendum Final / archived as of 2026-04-24; Stage 7 decoration primitives Draft — next to file; Stages 8–14 Draft; Stage 15 Deferred)
>
> **Scope:** Build `tools/sprite-gen/` — a Python CLI + N-layer hybrid composer that renders isometric pixel art building sprites from YAML archetype specs, with slope-aware foundations, per-class palette management, a decoration primitive library, multi-footprint support (1×1 / 2×2 / 3×3), tall-canvas growth for multi-floor towers, and a curation workflow that promotes approved PNGs to `Assets/Sprites/Generated/`. Diffusion overlay (Phase 2) and EA bulk render (Phase 3) follow once geometry MVP ships. Non-square footprints (2×1, 3×2, etc.) and animation frames remain out of scope for v1.
>
> **Last updated:** 2026-04-23 (Stages 6–14 appended as the DAS-driven scale-calibration + decoration + footprint-unlock extension. Lock L9 supersedes the earlier "v1 all 1×1" lock; water-facing slopes move in-scope; v1 primitive set expands from 3 to 20).
>
> **Exploration source:**
>
> - `docs/isometric-sprite-generator-exploration.md` (§2 Locked decisions, §3 Architecture, §5–§9 Primitive/Palette/Slope/YAML/Folder design, §13 Phase plan, §15 Success criteria — ground truth for Stages 1–5).
> - `docs/asset-snapshot-mvp-exploration.md` (§7.5 L6 + L7 + L8, §9.1 Architecture, §9.5.A) — extension source for Stage 5 push hook.
> - `docs/sprite-gen-art-design-system.md` — **canonical DAS** (dimensional math, palette anchors, outline policy, 17-primitive decoration set, archetype YAML schema v2) — ground truth for Stages 6–14.
> - `/tmp/sprite-gen-style-audit.md` — DAS polling transcript and audit raw data (197-sprite catalog inventory + bbox measurements + palette extraction).
>
> **Locked decisions (do not reopen in this plan):**
>
> - North star: unblock EA shipping — geometry-only MVP ships first; diffusion is opt-in.
> - Asset scope v1: buildings + slope-aware foundations only; terrain slope tiles stay hand-drawn.
> - Canvas math: `width = (fx+fy)×32`, `height = multiple of 32`; diamond bottom-center anchor.
> - Language: Python (diffusers ecosystem, Pillow/numpy/scipy, no compile step, Unity-isolated).
> - Primitives v1: `iso_cube` + `iso_prism` only; `iso_stepped_foundation` auto-inserted.
> - Palette: K-means auto-extract per class; 3-level ramp (bright/mid/dark); per-class JSON.
> - Generation architecture: 5-layer composer (primitive → compose+shade → palette → diffusion → curation).
> - Slope coverage: 17 land variants; water-facing deferred to v2.
> - EA scope: ~15 archetypes, all 1×1 **building footprint**.
> - Editor integration: Aseprite v1.3.17 (licensed). Tier 1 `.gpl` palette exchange in Stage 1.3. Tier 2 layered `.aseprite` emission + `promote --edit` round-trip in Stage 1.4. Tier 3 (Lua YAML runner) deferred.
> - **L6 (2026-04-22):** SOON finish-line = Stage 4 close + Stage 5 push hook. Animation descriptor / EA bulk render / anim-gen / archetype expansion (Steps 2–5 of exploration 5-step spine) stay deferred until MVP triangle closes.
> - **L7 (2026-04-22):** Sprite-gen emits PNG + `.meta` only. Postgres (registry) owns catalog rows; composite objects (panels / buttons / prefabs) are registry-side tables authored post-hoc. No composite sidecar YAML emitted by sprite-gen.
> - **L8 (2026-04-22):** Clean authoring/wiring split — sprite-gen writes catalog rows via HTTP POST `/api/catalog/assets` (never direct SQL, never file bundle). Unity bridge stays read-only from snapshot.
> - **L9 (2026-04-23):** Footprint lock amended — 1×1 + 2×2 + 3×3 all in v1 scope. Non-square footprints (2×1, 3×2, etc.) remain deferred. Water-facing slopes move into v1 (reverses the earlier "water-facing deferred to v2" line). v1 primitive set expands from 3 (iso_cube / iso_prism / iso_stepped_foundation) to 20 (adds `iso_ground_diamond`, `iso_slope_wedge`, plus the 17-primitive decoration set — see DAS R9). Legacy `iso_stepped_foundation` remains available but is no longer the default under-building foundation.
> - **L10 (2026-04-23):** Art calibration ground truth = `docs/sprite-gen-art-design-system.md` (DAS). Every Stage 6+ task cites a DAS section (e.g. "per DAS §4.2") rather than re-specifying rules inline. Audit corpus = all 197 sprites under `Assets/Sprites/` excluding Icons/Buttons/State/Roads. Primary reference: `House1-64.png` for 1×1; `LightResidentialBuilding-2-128.png` for 2×2; `HeavyIndustrialBuilding-1-192.png` for 3×3.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
>
> - `ia/projects/multi-scale-master-plan.md` — adds `RegionCell` / `CountryCell` types + parent-scale stubs + save-schema bumps. Sprite-gen v1 renders only city-scale 1×1 buildings; region / country scale sprite needs (cell sprites, city-node-at-region-zoom, region-node-at-country-zoom) surface when multi-scale Step 4 opens — see Deferred decomposition below.
> - `ia/projects/blip-master-plan.md` — audio subsystem. Disjoint surfaces (Python tool vs Unity C#); no sprite-gen collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
>
> - `docs/isometric-sprite-generator-exploration.md` — full design + architecture + examples. §2 Locked decisions + §3 Architecture + §13 Phase plan are ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — no runtime C# invariants at risk (tool is Python, Unity-isolated). Unity import pivot/PPU correctness enforced by `unity_meta.py` in Stage 1.4.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending`_ (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage index

- [Stage 1 — Geometry MVP / Scaffolding + Primitive Renderer (Layer 1)](stage-1-scaffolding-primitive-renderer-layer-1.md) — _Final (6 tasks archived as **TECH-123** through **TECH-128**; BACKLOG state: 6 archived / 6)_
- [Stage 2 — Geometry MVP / Composition + YAML Schema + CLI Skeleton (Layer 2)](stage-2-composition-yaml-schema-cli-skeleton-layer-2.md) — _Final (6 tasks archived as **TECH-147** through **TECH-152**; closed 2026-04-15)_
- [Stage 3 — Geometry MVP / Palette System (Layer 3)](stage-3-palette-system-layer-3.md) — _Final (all 9 tasks **TECH-153**..**TECH-158** archived; T1.3.3+T1.3.4 merged into **TECH-155**; T1.3.7+T1.3.8+T1.3.9 merged into **TECH-158**)_
- [Stage 4 — Geometry MVP / Slope-Aware Foundation](stage-4-slope-aware-foundation.md) — _Final — 4 tasks archived (**TECH-175**..**TECH-178**). Curation CLI (promote/reject + Unity `.meta`) + Aseprite Tier-2 integration (layered `.aseprite` emit + `promote --edit` round-trip) relocated to Stage 5 on 2026-04-22 so they sequence atomically with the snapshot push hook (promote → push in one pipeline)._
- [Stage 5 — Layer 5 Curation + Snapshot push hook / Unity meta + Aseprite Tier-2 + Registry catalog integration](stage-5-unity-meta-aseprite-tier-2-registry-catalog-integration.md) — _Final — tasks **TECH-179..183** + **TECH-674..679** archived 2026-04-22. Dependency gate (TECH-640..645) satisfied (archived)._
- [Stage 6 — Scale calibration + ground diamond primitive (DAS hotfix)](stage-6-scale-calibration-ground-diamond-primitive-das-hotfix.md) — _Final — closed 2026-04-23 (8 tasks **TECH-693**..**TECH-700** archived via `0837d3f`). Shipped as a **standalone hotfix PR** ahead of Stages 7–14 (Lock H2). Closes the 3× scale bug so the current `building_residential_small` archetype visually matches `House1-64.png`._
- [Stage 6.1 — Pivot hotfix + regression tighten](stage-6.1-pivot-hotfix-regression-tighten.md) — _Final — closed 2026-04-23 (3 tasks **TECH-701**..**TECH-703** archived; closeout residue repaired via `15d5f11`). Retroactive filing of the in-session pivot hotfix applied during the 2026-04-23 sprite-gen improvement session (`/tmp/sprite-gen-improvement-session.md` §3 Stage 6.1). The composer patch (`pivot_pad = 17 if spec.get("ground") != "none" else 0`) went live at `tools/sprite-gen/src/compose.py:256`; this stage produced the issue trail and tightened the regression suite. **Locks consumed:** L1 (pivot_pad=17 per DAS §2.1/§2.2). **Issues closed:** I1 (composer anchors buildings above ground diamond), I2 (regression loose)._
- [Stage 6.2 — Art Signatures per class](stage-6.2-art-signatures-per-class.md) — _Final — closed 2026-04-23 (5 tasks **TECH-704**..**TECH-708** archived via `959ab1a`). **Locks consumed:** L2 (Calibration = summarized Art Signatures per class; runtime never reads raw sprites), L3 (signature JSON carries `source_checksum`; stale raises actionable refresh), L4 (Spec YAML `include_in_signature: false` per-sprite override), L15 (sample-size policy: 0 → fallback, 1 → point-match, ≥2 → envelope)._
- [Stage 6.3 — Placement + variant randomness + split seeds](stage-6.3-placement-variant-randomness-split-seeds.md) — _Final — closed 2026-04-23 (6 tasks **TECH-709**..**TECH-714** archived via `7da3749`). **Locks consumed:** L5 (Spec gains `building.footprint_px`, `building.padding`, `building.align`), L6 (`variants:` becomes block `{count, vary, seed_scope}` with legacy scalar back-compat), L7 (`bootstrap-variants --from-signature` CLI; never auto-rewrites), L14 (split seeds `palette_seed` + `geometry_seed`)._
- [Stage 6.4 — Ground variation](stage-6.4-ground-variation.md) — _Final — closed 2026-04-23 (8 tasks **TECH-715**..**TECH-722** archived via `7dd80d7` + residual cleanup `0822391`). **Locks consumed:** L8 (`ground:` accepts string or object; back-compat by construction), L9 (`ground.`* joins `vary:` vocabulary; signature bounds jitter), L10 (new primitive `iso_ground_noise`; palette gains `accent_dark`/`accent_light`)._
- [Stage 6.5 — Curation-trained quality gate](stage-6.5-curation-trained-quality-gate.md) — _Final — closed 2026-04-23 (7 tasks **TECH-723**..**TECH-729** archived via `1ac0da0`). **Locks consumed:** L11 (curation/promoted.jsonl + rejected.jsonl feed the signature aggregator; composer gates renders against the evolving envelope)._
- [Stage 6.6 — Preset system](stage-6.6-preset-system.md) — _Final — closed 2026-04-23 (7 tasks **TECH-730**..**TECH-736** archived via `71a3a4d`). **Locks consumed:** L13 (`preset: <name>` top-level key injects a base spec; author fields override; `vary:` block from preset is preserved — author may extend / override individual `vary.*` entries but not wipe the block)._
- [Stage 6.7 — Animation schema reservation (tiny)](stage-6.7-animation-schema-reservation-tiny.md) — _Final — closed 2026-04-23 (4 tasks **TECH-737**..**TECH-740** archived via `36fbca5`). **Locks consumed:** L16 (reserve animation schema today; implementation deferred)._
- [Stage 7 — Decoration primitives — vegetation & yard](stage-7-decoration-primitives-vegetation-yard.md) — _Draft — 2026-04-24. Filed (10 tasks T7.1..T7.9b → TECH-762..TECH-771, all Draft status). Ready for `/plan-author`._
- [Stage 8 — Decoration primitives — building details (windows, doors, roof, signage)](stage-8-decoration-primitives-building-details-windows-doors-roof-si.md) — _Draft — 2026-04-23._
- [Stage 9 — Footprint unlock — 2×2 composites + multi-building clusters](stage-9-footprint-unlock-2-2-composites-multi-building-clusters.md) — _Draft — 2026-04-23. First archetype lock-break per L9._
- [Stage 10 — Footprint unlock — 3×3 industrial + paved-yard composition](stage-10-footprint-unlock-3-3-industrial-paved-yard-composition.md) — _Draft — 2026-04-23._
- [Stage 11 — Vertical unlock — tall canvases (+64 per floor tier)](stage-11-vertical-unlock-tall-canvases-64-per-floor-tier.md) — _Draft — 2026-04-23._
- [Stage 12 — Palette system v2 + outline policy](stage-12-palette-system-v2-outline-policy.md) — _Draft — 2026-04-23._
- [Stage 13 — Slope refactor — 2-tone cliff + water-facing slopes](stage-13-slope-refactor-2-tone-cliff-water-facing-slopes.md) — _Draft — 2026-04-23._
- [Stage 14 — Archetype library expansion + slope matrix per archetype](stage-14-archetype-library-expansion-slope-matrix-per-archetype.md) — _Draft — 2026-04-23. **No archetype cap (Lock H3).**_
- [Stage 15 — (Deferred) Effects & animation](stage-15-deferred-effects-animation.md) — _Deferred — separate future exploration per Lock I4._
