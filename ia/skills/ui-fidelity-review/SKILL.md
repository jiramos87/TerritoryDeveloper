---
name: ui-fidelity-review
purpose: >-
  Closed-loop UI fidelity verify pass. Composes bake_ui_from_ir + prefab_inspect + ui_tree_walk +
  claude_design_conformance bridge kinds against claude-design IR + UiTheme; emits structured
  Verification block; iterates fix → re-bake → re-verify until conformance row fail_count = 0 or
  MAX_ITERATIONS reached.
audience: agent
loaded_by: "skill:ui-fidelity-review"
slices_via: none
description: >-
  Closed-loop UI fidelity review against claude-design IR + UiTheme for one panel surface
  (pause-menu, info-panel, etc). Phases: Bake → Inspect → Walk → Conformance → Verify → Iterate.
  Wraps bake_ui_from_ir, prefab_inspect, ui_tree_walk, claude_design_conformance bridge kinds.
  Emits Verification block per `verification-report` output style. Requires Postgres
  agent_bridge_job (0008), DATABASE_URL, Unity Editor on REPO_ROOT, IR JSON +
  UiTheme ScriptableObject. Triggers: "/ui-fidelity-review", "ui fidelity review",
  "verify panel against IR", "claude-design conformance", "diff prefab vs IR + theme".
phases: []
triggers:
  - /ui-fidelity-review
  - ui fidelity review
  - verify panel against IR
  - claude-design conformance
  - diff prefab vs IR + theme
model: inherit
tools_role: custom
tools_extra: []
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries: []
---

# UI fidelity review — IR + theme conformance loop

Closed-loop diff between baked prefab/scene and claude-design IR (`ir.json`) + UiTheme ScriptableObject. Composes Stage 12 Step 14.1 (`prefab_inspect`) + 14.2 (`ui_tree_walk`) + 14.3 (`claude_design_conformance`) bridge kinds plus the existing `bake_ui_from_ir` mutation. Read-mostly: only mutation is `bake_ui_from_ir` (re-baking on iterate).

**Not** a substitute for [`close-dev-loop`](../close-dev-loop/SKILL.md) (Play Mode anomaly diff at seed cells) or [`debug-sorting-order`](../debug-sorting-order/SKILL.md) (sorting math). Use this skill when the question is **UI panel fidelity vs claude-design intent** — palette ramp binding, font face binding, frame style binding, panel kind, caption text, contrast ratio.

**Related:** [`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md) (raw bridge primitives) · [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md) (Step 0) · [`ui-hud-row-theme`](../ui-hud-row-theme/SKILL.md) (HUD row authoring; complementary, not conformance-driven). **Normative:** [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md), [`docs/game-ui-design-system-stage-12-content-layer-fix.md`](../../../docs/game-ui-design-system-stage-12-content-layer-fix.md) §Step 14.

## Prerequisites

| Requirement | Notes |
|---|---|
| `DATABASE_URL` or `config/postgres-dev.json` | Same registry as other bridge tools |
| Migration `0008_agent_bridge_job.sql` | `npm run db:migrate` |
| Unity Editor on `REPO_ROOT` | `AgentBridgeCommandRunner` polls dequeue |
| territory-ia MCP | `unity_bridge_command` + `unity_bridge_get` |
| IR JSON | e.g. `web/design-refs/step-1-game-ui/ir.json` (from `transcribe:cd-game-ui`) |
| UiTheme SO | e.g. `Assets/UI/Theme/DefaultUiTheme.asset` |
| Baked prefab OR scene root | one of `prefab_path` (e.g. `Assets/UI/Prefabs/Generated/PauseMenu.prefab`) or `scene_root_path` (Canvas-rooted GameObject name) |

## Parameterize

| Placeholder | Meaning |
|---|---|
| `{SURFACE_SLUG}` | Panel slug from IR (`pause-menu`, `info-panel`, etc) |
| `{IR_PATH}` | Path to IR JSON (default `web/design-refs/step-1-game-ui/ir.json`) |
| `{THEME_SO}` | Path to UiTheme asset (default `Assets/UI/Theme/DefaultUiTheme.asset`) |
| `{PREFAB_PATH}` | Asset path of baked prefab (mutex with `{SCENE_ROOT_PATH}`) |
| `{SCENE_ROOT_PATH}` | Active-scene Canvas-rooted GameObject name (mutex with `{PREFAB_PATH}`) |
| `{MAX_ITERATIONS}` | Bake-fix cycles before escalating (default 2) |

## Phase order — Bake → Inspect → Walk → Conformance → Verify → Iterate

### 0. Preflight

`npm run db:bridge-preflight` (or [`bridge-environment-preflight`](../bridge-environment-preflight/SKILL.md)). Exit 0 before any enqueue. Confirms Postgres + `agent_bridge_job` schema + Editor presence.

### 1. Bake (mutation; skip if prefab already current)

`unity_bridge_command` `kind: bake_ui_from_ir`, params `{ ir_path, theme_so, out_dir? }`. Re-bakes prefab from IR + theme. Skip on first iteration if `{PREFAB_PATH}` already exists + `git status` shows no IR/theme drift since last bake.

### 2. Inspect — semantic prefab tree (Edit Mode)

`unity_bridge_command` `kind: prefab_inspect`, params `{ prefab_path }`. Returns `response.prefab_inspect_result.root` with `name`, `relative_path`, `components[]{ type_name, fields[] }`, `RectTransform` per node. Use to confirm slot tree matches IR `panels[].slots[]` shape (no missing/extra slugs) **before** running conformance.

Skip when running scene mode — `prefab_inspect` requires a prefab asset, not a scene root.

### 3. Walk — runtime canvas tree (Edit Mode after Canvas layout, optional)

`unity_bridge_command` `kind: ui_tree_walk`, params `{ root_path?, active_only?, include_serialized_fields? }`. Returns `response.ui_tree_walk_result.canvases[].root` with screen-space rects per node (calls `Canvas.ForceUpdateCanvases()` first).

Use when:
- IR carries `layout-rects.json` and you want to diff design rect vs runtime rect.
- Suspect prefab-instance overrides or runtime layout overrides altered the baked tree.
- Scene-mode review (no prefab to inspect — only the live Canvas).

Skip when prefab inspect alone covers the scope.

### 4. Conformance — IR + theme diff (Edit Mode)

`unity_bridge_command` `kind: claude_design_conformance`, params `{ ir_path, theme_so, prefab_path }` **or** `{ ir_path, theme_so, scene_root_path }` (exactly one of `prefab_path` / `scene_root_path`).

Returns `response.claude_design_conformance_result`:

```json
{
  "ir_path": "web/design-refs/step-1-game-ui/ir.json",
  "theme_so": "Assets/UI/Theme/DefaultUiTheme.asset",
  "target_kind": "prefab",
  "target_path": "Assets/UI/Prefabs/Generated/PauseMenu.prefab",
  "row_count": 24,
  "fail_count": 3,
  "rows": [
    {
      "node_path": "PauseMenuRoot/Title",
      "component": "ThemedLabel",
      "check_kind": "palette_ramp",
      "slug": "pause-menu/title",
      "expected": "ramp[last]",
      "resolved": "#FFFFFF",
      "actual": "#E0E0E0",
      "severity": "fail",
      "pass": false,
      "message": "palette ramp[last] mismatch: theme #FFFFFF vs prefab #E0E0E0"
    }
  ]
}
```

Six check kinds emitted: `palette_ramp`, `font_face`, `frame_style`, `panel_kind`, `caption`, `contrast_ratio`.

Severity scale: `info` (deferred / read-only metadata; e.g. font_face binding, frame_style binding — always `pass=true`), `error` (gate-blocking; e.g. palette ramp mismatch, panel_kind enum drift, caption mismatch, contrast < 4.5:1 — `pass=false`).

Pass policy: gate on `fail_count == 0` (server-computed: count where `pass == false`, which always equals `severity == "error"`). Info rows logged but never gate.

### 5. Verify — emit Verification block

Emit a Verification block per [`verification-report`](../../../.claude/output-styles/verification-report.md) output style. Map UI fidelity row results into the JSON header:

- `validate_all` — surface `npm run validate:all` exit if recently run; else `skipped: true`, `reason: "ui-fidelity-review focuses on bridge conformance; validate:all run separately"`.
- `compile` — `applies: false` unless C# touched this iteration.
- `batch` — `applies: false` (test mode batch is a different surface).
- `bridge` — `outcome: "ok"` on `fail_count == 0`; `"error"` on `fail_count > 0`; `"timeout"` if any bridge call timed out (follow escalation per `docs/agent-led-verification-policy.md`).

Append a caveman markdown summary after the JSON: per-iteration row counts (`row_count`, `fail_count`, top 3 fail rows by `node_path` + `check_kind`), bake/inspect/walk artifact paths, surface slug.

### 6. Iterate

If `fail_count > 0` and cause is clear:

- **Theme drift** (palette ramp, font face, frame style mismatch) → edit `Assets/UI/Theme/DefaultUiTheme.asset` ramp / font face / frame style binding → re-bake (Phase 1) → re-inspect (Phase 2) → re-walk if needed (Phase 3) → re-run conformance (Phase 4) → re-emit Verification block (Phase 5).
- **IR drift** (caption text, panel kind, slot tree shape) → edit `{IR_PATH}` upstream OR re-run `transcribe:cd-game-ui` against updated cd-bundle → re-bake → re-verify.
- **Contrast fail** → swap palette ramp index in IR or theme → re-bake → re-verify.

Stop after `{MAX_ITERATIONS}` (default 2) without convergence; escalate with row dump + suspected drift surface (theme vs IR vs prefab override).

## MCP tools

| Tool | Role |
|---|---|
| `unity_bridge_command` | Enqueue + poll any `kind`. Use for `bake_ui_from_ir` (mutation), `prefab_inspect`, `ui_tree_walk`, `claude_design_conformance` (read-only). |
| `unity_bridge_get` | Read response by `command_id` when polling separately. |
| `unity_compile` | Alias for `kind: get_compilation_status`. Run after C# edits before any bridge enqueue depending on Editor compile state. |

Prefer raw `unity_bridge_command` over sugar tools — none of the four UI-fidelity kinds have dedicated sugar wrappers.

## Operational limits

- `timeout_ms`: 40000 initial; on timeout → `npm run unity:ensure-editor` → retry 60 s. Ceiling 120 s. Policy: [`docs/agent-led-verification-policy.md`](../../../docs/agent-led-verification-policy.md).
- `bake_ui_from_ir` mutates Asset Database — do not run while Editor user actively edits the same prefab.
- `claude_design_conformance` requires exactly one of `prefab_path` or `scene_root_path` (Zod validates).
- `prefab_inspect` is asset-only — fails with `ok: false` for scene-only roots; route those through `ui_tree_walk` instead.

## Verdict shape (caveman summary block)

```
ui-fidelity-review {SURFACE_SLUG} — iter N/{MAX_ITERATIONS}
target: prefab Assets/UI/Prefabs/Generated/PauseMenu.prefab
ir: web/design-refs/step-1-game-ui/ir.json
theme: Assets/UI/Theme/DefaultUiTheme.asset
row_count: 24  fail_count: 3
top fails:
  - PauseMenuRoot/Title    palette_ramp   ramp[last] #FFFFFF vs #E0E0E0
  - PauseMenuRoot/QuitBtn  contrast_ratio 3.8:1 vs 4.5:1 threshold
  - PauseMenuRoot/Body     caption        "Quit Game" vs "Quit"
suspected drift: theme palette ramp[last] override
next: edit DefaultUiTheme palette ramp → re-bake → iter N+1
```

## Seed prompt

```markdown
Run ui-fidelity-review (`ia/skills/ui-fidelity-review/SKILL.md`) for {SURFACE_SLUG}.
Bake → prefab_inspect → ui_tree_walk (optional) → claude_design_conformance against {IR_PATH} + {THEME_SO} on {PREFAB_PATH or SCENE_ROOT_PATH}. Emit Verification block. Iterate up to {MAX_ITERATIONS} on fail_count > 0; escalate with row dump if no convergence.
```
