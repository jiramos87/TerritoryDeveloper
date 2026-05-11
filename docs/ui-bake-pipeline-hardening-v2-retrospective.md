# UI Bake-Pipeline Hardening v2 — Retrospective

> Lineage: Closes gaps from `ui-bake-pipeline-hardening-and-closed-loop-validation` (v1) that shipped
> cityscene-mainmenu-panel-rollout Stages 6.0–9.0 PASS but Editor QA revealed cascading silent failures.
> v3 repair extension: `docs/explorations/cityscene-mainmenu-panel-rollout-v3-repair.md`.

---

## Layer 1 — Author-time DB gates

**Stage:** `stage-1.0-author-gates` (TECH-28356 – TECH-28360)

**Delivered:**
- `catalog_panel_publish` MCP write blocked when archetype lacks renderer (TECH-28356)
- action-id sink uniqueness enforced — duplicate sinks error at publish boundary (TECH-28357)
- bind-id contract gate — bind reads/writes validated against registered bind-ids (TECH-28358)
- token reference graph gate — every DS-token usage resolves to `ui_design_tokens` row (TECH-28359)
- view-slot anchor required-by gate — panels declaring views[] must declare anchor required-by edge (TECH-28360)

**Tests:** `tests/ui-bake-hardening-v2/stage1-author-gates.test.mjs`

**Open:**
- None. Gates fire at MCP boundary; no runtime bypass path known.

**Consumed by:** Layer 2 bake handler (plugin dispatch validates same schema), Layer 5 B.7c/d/e gates.

---

## Layer 2 — Bake-time correctness

**Stage:** `stage-2.0-bake-correctness` (TECH-28361 – TECH-28364)

**Delivered:**
- Non-empty child assert in `UiBakeHandler.Apply` — silent empty-prefab eliminated (TECH-28361)
- Bake-handler plugin pattern (`IBakeHandler[]` dispatch) — new kinds = new class, no switch edit (TECH-28362)
- Bake diff baseline + golden manifest per panel — drift flagged on re-bake (TECH-28363)
- `.meta`-file write proof post-bake — AssetDatabase consistency confirmed (TECH-28364)

**Tests:** EditMode tests in stage2 test file.

**Open:**
- Golden manifest reset workflow not automated; manual `--reset-baseline` needed after intentional panel redesign.

**Consumed by:** Layer 5 visual diff harness reads bake diff baseline.

---

## Layer 3 — Scene-wire DB-driven (keystone)

**Stage:** `stage-3.0-scene-wire` (TECH-28365 – TECH-28369)

**Delivered:**
- `scene-wire-plan.yaml` emitted from bake — declares every (scene, canvas, slot) target + (controller, adapter, panel) binding (TECH-28365)
- Scene drift detector — flags legacy GOs contradicting plan + unwired controllers + wrong-target HUD buttons (TECH-28366)
- Canvas layering audit — `sortingOrder` hierarchy enforced (TECH-28367)
- Adapter↔panel binding test fixture — proves HUD buttons open the panel DB says (TECH-28368)
- Legacy-GO purge planner + retire markers — identifies candidates for v3 purge (TECH-28369)

**Tests:** EditMode + PlayMode files in stage3 test suite.

**Open:**
- Actual GO removal deferred to cityscene v3 Stage 11.0 (purge planner marks, does not delete).
- HUD button rewire (wrong-target fix) deferred to v3 Stage 10.0.

**Consumed by:** Layer 4 runtime contract (derives panel-mount expectations from scene-wire-plan), Layer 5 legacy-drift sweep.

---

## Layer 4 — Runtime contract tests + telemetry + bridge read_panel_state

**Stage:** `stage-4.0-runtime-contract` (TECH-28370 – TECH-28373)

**Delivered:**
- Bridge kind `read_panel_state(panel_slug)` — returns live (mounted, child_count, bind_count, action_count) (TECH-28370)
- DB-derived contract test — every published panel mounts + binds + dispatches confirmed (TECH-28371)
- Action-fire telemetry — every dispatch logged with handler class + timestamp (TECH-28372)
- Synthetic click harness — bridge `dispatch_action` triggers full pipeline (TECH-28373)

**Tests:** PlayMode stage4 test file.

**Open:**
- Telemetry sink is in-memory log only; no persistent `ia_action_telemetry` table (deferred).
- `dispatch_action` harness covers HUD buttons; sub-panel nested actions not yet exercised.

**Consumed by:** Layer 5 functional smoke harness uses `read_panel_state` + `dispatch_action`.

---

## Layer 5 — Visual diff + functional smoke + legacy-drift gates

**Stage:** `stage-5.0-visual-functional` (TECH-28374 – TECH-28377)

**Delivered:**
- Visual diff harness — per-panel screenshot + SSIM tolerance baseline (TECH-28374)
- Functional smoke harness — synthetic click trace through every HUD button (TECH-28375)
- Legacy-drift sweep — every `catalog_legacy_gos` row retired confirmed (TECH-28376)
- Ship-cycle Pass B gates B.7c (visual diff) + B.7d (functional smoke) + B.7e (legacy drift) — hard-fail before stage commit (TECH-28377)

**Tests:** PlayMode stage5 test file.

**Open:**
- SSIM tolerance threshold is global (0.95); per-panel overrides not yet supported.
- Functional smoke does not cover keyboard/gamepad input paths.

**Consumed by:** Every future ship-cycle stage touching UI panels — B.7c/d/e block commit on failure.

---

## Layer 6 — Auditability

**Stage:** `stage-6.0-auditability` (TECH-28378 – TECH-28380)

**Delivered:**
- `ia_ui_bake_history` + `ia_bake_diffs` tables + bake writer — every bake leaves audit row (TECH-28378)
- MCP read tool `ui_bake_history_query` — agent-readable bake history per panel (TECH-28379)
- Web dashboard `/admin/ui-bake-history` — last N bakes per panel + drift over time (TECH-28380)

**Tests:** `tests/ui-bake-hardening-v2/stage6-auditability.test.mjs`

**Open:**
- Dashboard shows bake diff summary strings; full diff viewer (unified-diff modal) not built.
- Bake history retention policy (max rows, TTL) not configured — table will grow unbounded.

**Consumed by:** v3 repair stages can query bake history to confirm panel state before purge operations.

---

## Lineage

| Plan | Relationship |
|---|---|
| `ui-bake-pipeline-hardening-and-closed-loop-validation` (v1) | Predecessor — established bake foundation; gaps found in cityscene-mainmenu-panel-rollout Stages 6.0–9.0 QA |
| `cityscene-mainmenu-panel-rollout` | Consumer — v2 layers enforced on every Stage 6.0–9.0 ship-cycle |
| `cityscene-mainmenu-panel-rollout-v3-repair` | Downstream — uses Layer 3 purge planner + Layer 5 gates for full panel rewire + legacy purge |

## Stage 7.0 — Retrospective task

v3 trigger: `docs/explorations/cityscene-mainmenu-panel-rollout-v3-repair.md` (TECH-28382).
