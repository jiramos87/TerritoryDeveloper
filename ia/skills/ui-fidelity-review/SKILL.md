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

> **RETIRED — replaced by catalog-bake** (DEC-A24 §6 D6; game-ui-catalog-bake Stage 6 closeout, 2026-05-05). Claude-design IR JSON pipeline demoted to sketchpad-only (`web/design-refs/**`). `LayoutRectsLoader` deleted; `bake_ui_from_ir` runtime tether severed; red test `validate:no-ir-bake-runtime-refs` locks the demotion. New conformance loop = `CatalogBakeHandler` snapshot diff + bridge `screenshot_game_view` (per master plan `game-ui-catalog-bake` Stage 6+). IR paths below preserved for historical context only.

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
| `{LAYOUT_RECTS_PATH}` | Optional design-reference layout rects (default `web/design-refs/step-1-game-ui/layout-rects.json`) — enables advisory layout-rect diff in Phase 3 |
| `{HUMAN_QA_GATE}` | When `on` (default), the skill stops after Phase 5 of each iteration and asks the human for QA before allowing the next bake-fix work item. Human-QA is **manual + out-of-band** — human launches Editor Play Mode, takes their own screenshots, adds comments. The skill's closed-loop bridge captures (`prefab_inspect`, `ui_tree_walk`, `claude_design_conformance` rows, any auto-saved Game-view PNGs) are **agent-internal review only** — never required input for the human gate. When `off`, the loop runs autonomously up to `{MAX_ITERATIONS}`. |

## Phase order — Bake → Inspect → Walk → Conformance → Verify → Human QA → Iterate

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

**Layout-rect advisory diff (optional).** When `{LAYOUT_RECTS_PATH}` is provided, after the walk completes, load the JSON + diff per-node rect (`x`, `y`, `width`, `height` at 1920×1080 viewport) against the design-reference rect keyed by `{panel_slug}/{child_slug}`. Emit advisory rows in the Verification block under a `layout_rect_drift` section: `{ node_path, slug, expected_rect, actual_rect, dx, dy, dw, dh }`. **Advisory only — never gate `fail_count`.** Use to flag regressions; visual / human QA judges severity.

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

Eight check kinds emitted: `palette_ramp`, `font_face`, `frame_style`, `panel_kind`, `caption`, `contrast_ratio`, `frame_sprite_bound`, `button_state_block`.

| Check kind | Severity rule | Gate |
|---|---|---|
| `palette_ramp` | `error` on color drift > 1.5/255 epsilon | yes |
| `font_face` | `info` (read-only metadata, runtime binding deferred) | no |
| `frame_style` | `info` until sprite-bake lands; `error` once `frame_sprite_bound` enforced | conditional |
| `panel_kind` | `error` on enum / IR string mismatch | yes |
| `caption` | `error` on TMP `m_text` vs IR labels mismatch | yes |
| `contrast_ratio` | `error` on WCAG body-text < 4.5:1 | yes |
| `frame_sprite_bound` | `error` when `ThemedFrame.Image.sprite == null` on a panel root carrying a non-null `frame_style` slug | yes |
| `button_state_block` | `error` when `Selectable.colors` ColorBlock state colors do not match expected ramp indices within 1.5/255 epsilon (ramp[last] / ramp[last-1] / ramp[last-2] / ramp[0]) OR when `interactions.json` carries a per-slug override that the ColorBlock does not honor | yes |

Severity scale: `info` (deferred / read-only metadata — always `pass=true`), `error` (gate-blocking — `pass=false`).

Pass policy: gate on `fail_count == 0` (server-computed: count where `pass == false`, which always equals `severity == "error"`). Info rows logged but never gate. Layout-rect drift rows from Phase 3 are **advisory** and never fold into `fail_count`.

### 5. Verify — emit Verification block

Emit a Verification block per [`verification-report`](../../../.claude/output-styles/verification-report.md) output style. Map UI fidelity row results into the JSON header:

- `validate_all` — surface `npm run validate:all` exit if recently run; else `skipped: true`, `reason: "ui-fidelity-review focuses on bridge conformance; validate:all run separately"`.
- `compile` — `applies: false` unless C# touched this iteration.
- `batch` — `applies: false` (test mode batch is a different surface).
- `bridge` — `outcome: "ok"` on `fail_count == 0`; `"error"` on `fail_count > 0`; `"timeout"` if any bridge call timed out (follow escalation per `docs/agent-led-verification-policy.md`).

Append a caveman markdown summary after the JSON: per-iteration row counts (`row_count`, `fail_count`, top 3 fail rows by `node_path` + `check_kind`), bake/inspect/walk artifact paths, surface slug.

### 6. Human QA gate (when `{HUMAN_QA_GATE} = on`, default)

After Phase 5 emits the Verification block, **stop**. Ask the human for QA confirmation before proceeding to the next bake-fix work item.

**Closed-loop captures vs human QA — separate channels.** Bridge captures from Phases 1–4 (`prefab_inspect` / `ui_tree_walk` / `claude_design_conformance` rows, any auto-saved Game-view PNGs) are **agent-internal review only**. Do not link, attach, or paste them as evidence the human is expected to QA. Human runs Editor Play Mode manually, takes their own screenshots, and adds their own comments per section. Closed-loop captures stay under bridge artifact paths for the agent's iterate decision; human-QA evidence is whatever the human posts in reply.

Polling shape (product language; see `ia/rules/agent-human-polling.md`) — short prompt, no agent screenshots inlined:

```
Pause-menu border art + button hover/press states baked. Closed-loop conformance: row_count=N, fail_count=0 (agent-internal).
Please QA manually in Editor Play Mode (Esc → pause menu):
  - Border / corners visible on panel chrome.
  - Buttons lighten on hover, darken on press.
  - Click still fires action + blip.
Take your own screenshots + add comments. Reply: pass / fail.
```

Wait for human reply. On `pass`, advance to the next work item. On `fail`, treat as Phase 7 iterate with the human-flagged surface (and human's screenshots / comments) as the suspected drift target.

Skip this phase when `{HUMAN_QA_GATE} = off` (autonomous mode); flow straight from Phase 5 to Phase 7.

### 7. Iterate

If `fail_count > 0` (conformance) **or** human QA returned `fail`, and cause is clear:

- **Theme drift** (palette ramp, font face, frame style mismatch) → edit `Assets/UI/Theme/DefaultUiTheme.asset` ramp / font face / frame style binding → re-bake (Phase 1) → re-inspect (Phase 2) → re-walk if needed (Phase 3) → re-run conformance (Phase 4) → re-emit Verification block (Phase 5) → re-poll human (Phase 6).
- **IR drift** (caption text, panel kind, slot tree shape) → edit `{IR_PATH}` upstream OR re-run `transcribe:cd-game-ui` against updated cd-bundle → re-bake → re-verify → re-poll.
- **Contrast fail** → swap palette ramp index in IR or theme → re-bake → re-verify → re-poll.
- **Frame sprite unbound** (`frame_sprite_bound` error) → bake handler `themed-panel` path: look up `frame_style` slug via `AtlasIndex` → write resolved sprite into `ThemedFrame.Image.sprite` SerializedField + set `Image.type = Sliced` → re-bake → re-verify. **Bake-time only — runtime never assigns sprite.**
- **Button state block drift** (`button_state_block` error) → `ThemedButton.ApplyTheme`: write `Selectable.colors` ColorBlock with ramp[last] / ramp[last-1] / ramp[last-2] / ramp[0] → optionally apply per-slug override from `interactions.json` → re-bake → re-verify.
- **Layout-rect drift advisory** (Phase 3 advisory rows) → not auto-gated; raise to human in Phase 6 polling block; act only when human flags in QA reply.

Stop after `{MAX_ITERATIONS}` (default 2) without convergence; escalate with row dump + suspected drift surface (theme vs IR vs prefab override vs bake handler).

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
layout_rects: web/design-refs/step-1-game-ui/layout-rects.json (advisory)
row_count: 26  fail_count: 3  layout_drift_count: 2 (advisory)
top fails:
  - PauseMenuRoot/Title         palette_ramp        ramp[last] #FFFFFF vs #E0E0E0
  - PauseMenuRoot/Frame         frame_sprite_bound  expected non-null sprite, got null
  - PauseMenuRoot/QuitBtn       button_state_block  pressed #1c2024 vs ramp[last-2] #131618
top layout drifts (advisory):
  - PauseMenuRoot/QuitBtn       dx=+12  dy=-6  dw=+8   dh=0
suspected drift: bake handler — ThemedFrame sprite write missing
next: edit UiBakeHandler themed-panel case → AtlasIndex lookup → re-bake → iter N+1
human QA: pending (gate=on; closed-loop captures are agent-internal — human runs Play Mode + posts own screenshots)
```

## Seed prompt

```markdown
Run ui-fidelity-review (`ia/skills/ui-fidelity-review/SKILL.md`) for {SURFACE_SLUG}.
Bake → prefab_inspect → ui_tree_walk (with layout-rect advisory diff against {LAYOUT_RECTS_PATH}) → claude_design_conformance (8 check kinds) against {IR_PATH} + {THEME_SO} on {PREFAB_PATH or SCENE_ROOT_PATH}.
Emit Verification block. When {HUMAN_QA_GATE}=on (default), stop after Phase 5 and poll human (product-language QA prompt: border art visible, hover/press states, click+blip). Closed-loop captures (prefab_inspect / ui_tree_walk / conformance rows / auto-saved PNGs) are agent-internal review only — never inline them as human-QA evidence. Human runs Play Mode manually, posts own screenshots + comments. Iterate on fail_count > 0 OR human-fail. Stop after {MAX_ITERATIONS} without convergence; escalate with row dump + suspected drift surface.
```

## Changelog

- 2026-04-30 — Added `frame_sprite_bound` + `button_state_block` strict check kinds; added Phase 3 advisory layout-rect diff (`{LAYOUT_RECTS_PATH}`); added Phase 6 human QA gate (`{HUMAN_QA_GATE}`, default `on`); renumbered iterate to Phase 7. Driven by Stage 12 Step 16 polish iteration (pause + info-panel; bake-time-only frame sprite, ramp+interactions.json button states).
- 2026-05-01 — Clarified Phase 6 channel separation: closed-loop captures (`prefab_inspect`, `ui_tree_walk`, conformance rows, auto-saved PNGs) are **agent-internal review only**. Human QA is manual + out-of-band — human runs Editor Play Mode, takes own screenshots, adds own comments. Polling prompt no longer asks human to inspect agent screenshots.
