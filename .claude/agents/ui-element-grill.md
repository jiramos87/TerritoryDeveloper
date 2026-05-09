---
name: ui-element-grill
description: Guided 5-phase grill for UI element definition authoring. Phase 1 loads corpus + design-system spec. Phase 2 polls user for panel intent + variants. Phase 3 drafts the definition shape (DB rows + seed JSON). Phase 4 runs bake + verdicts loop (ui_calibration_verdict_record). Phase 5 publishes + cross-links docs. MCP slice usage order: ui_panel_list → ui_calibration_corpus_query → ui_token_list → ui_component_list → ui_panel_publish. Late-formalized per Q2 — corpus + verdicts + MCP slices proven by Stage 4 close. Triggers: "/ui-element-grill", "ui element grill", "grill ui panel", "define panel", "author panel definition".
tools: ui_panel_list, ui_panel_get, ui_calibration_corpus_query, ui_calibration_verdict_record, ui_token_list, ui_token_get, ui_component_list, ui_component_get, ui_panel_publish
model: sonnet
---

## Stable prefix (Tier 1 cache)

> `cache_control: {"type":"ephemeral","ttl":"1h"}` — per `docs/prompt-caching-mechanics.md` §3 Tier 1.

@ia/skills/_preamble/stable-block.md

Follow `caveman:caveman` for all responses. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

@.claude/agents/_preamble/agent-boot.md
<!-- skill-tools:body-override -->

# UI element grill — 5-phase definition authoring flow

5-phase grill for authoring a UI panel definition end-to-end. Corpus + verdicts = primary memory. MCP slices drive all reads/writes.

## MCP slice usage order

`ui_panel_list` → `ui_calibration_corpus_query` → `ui_token_list` → `ui_component_list` → `ui_panel_publish`

| Phase | Action | MCP tools |
|---|---|---|
| 1 | Load corpus + spec | `ui_panel_list`, `ui_calibration_corpus_query`, `ui_token_list`, `ui_component_list` |
| 2 | Poll user | (human interaction) |
| 3 | Draft definition | `ui_token_get`, `ui_component_get` |
| 4 | Bake + verdicts | `ui_calibration_verdict_record` |
| 4.5 | Bake-vs-design conformance | `unity_bridge_command(bake_ui_from_ir)`, `unity_bridge_command(capture_screenshot)`, `ui_calibration_verdict_record` |
| 5 | Publish + cross-link | `ui_panel_publish` |

## Phase 1 — Load corpus + design-system spec

1. `ui_panel_list` — enumerate existing panels; find `{panel_slug}` if already seeded.
2. `ui_calibration_corpus_query(panel_slug={panel_slug})` — load all corpus entries for this panel. Corpus = design calibration decisions + prior verdicts.
3. `ui_token_list` — enumerate available tokens (color + spacing kinds). Build reference map slug → value.
4. `ui_component_list` — enumerate available components. Build reference map slug → role + variants.
5. Load `ia/specs/ui-design-system.md §Tokens` + `§Components` via `spec_section` for authoritative counts + namespace conventions.

Context assembled: panel state, corpus history, token + component reference maps, spec counts.

## Phase 2 — Interview-poll user for panel intent + variants

Ask product-language questions (see `ia/rules/agent-human-polling.md`). Minimum required:

1. **Panel purpose** — what does this panel do in the game? (1 sentence)
2. **Anchor / position** — where does it live on screen? (bottom strip, overlay, corner card, etc.)
3. **Key components** — which components from the catalog does it use? (or new ones needed?)
4. **Token overrides** — any token values that differ from spec defaults?
5. **Variants** — panel states to define (e.g. idle, dimmed, modal-open)?

Stop after Phase 2 answer; proceed only when all 5 questions answered.

## Phase 3 — Draft definition shape

Produce:

- `catalog_entity` row shape: `kind='panel'`, `slug`, `display_name`
- `panel_detail` row shape: `rect_json` (anchor_min, anchor_max, size_delta), `layout`, `padding_json`, `gap_px`, `params_json` (component slug list + token references)
- Seed JSON block for `docs/ui-element-definitions.md §{PanelName} §JSON (seed source)`

Show draft to user before proceeding to Phase 4. Product-language summary of what was drafted (avoid JSONB internals). Ask: "Does this match the panel you described? Reply yes / edit."

Wait for confirmation. On edit: revise and re-show. On yes: proceed.

## Phase 4 — Bake + verdicts loop

1. If panel is already in DB (`ui_panel_get` returns row): compare draft definition against DB row. Flag drifts.
2. If panel not in DB yet: record corpus entry noting definition is draft-pending. Use `ui_calibration_verdict_record` to log outcome (pass/fail/improvement-ids).
3. Record at least one verdict row before publish: `ui_calibration_verdict_record(panel_slug={slug}, rebake_n=1, outcome=pass, ...)`.

Verdicts loop (up to 2 iterations): if definition review surfaces a gap, revise Phase 3 draft → re-record verdict. Stop after 2 iterations without convergence; escalate with gap description.

## Phase 4.5 — Bake-vs-design conformance

DB-row-only PASS ≠ visual conformance. Phase 4 verdicts can pass schema + corpus checks while Unity renders the panel wrong (zone routing skipped, binds unwired, sizes ignored, sounds missing). Phase 4.5 closes the loop on what Unity actually renders.

1. Re-bake the panel: `unity_bridge_command(refresh_asset_database)` → `unity_bridge_command(bake_ui_from_ir, panel_slug={slug})` → confirm Generated prefab updated.
2. Capture render proof:
   - Prefab path: `unity_bridge_command(prefab_inspect, path=Assets/UI/Prefabs/Generated/{slug}.prefab)` — dumps child hierarchy, components, RectTransform anchors, sizes.
   - Play-Mode screenshot when scene reachable: `unity_bridge_command(enter_play_mode)` → `unity_bridge_command(capture_screenshot)`. Edit-Mode fallback when scene cannot enter play (missing GridManager etc).
3. Diff against design definition: open `docs/ui-element-definitions.md §{PanelName}` lines (line range cited in Phase 1). For each visible defect surface in the design (layout zone, child kind, params_json key, layout_json key, bind, sound), confirm prefab/screenshot evidence matches.
4. Record one verdict per defect surface: `ui_calibration_verdict_record(panel_slug={slug}, rebake_n={N}, outcome=pass|fail, surface={zone|kind|size|bind|sound}, evidence={path|screenshot})`. Fail outcome blocks Phase 5.
5. Publish gate: every defect-surface verdict = pass. Any fail → revise Phase 3 draft (DB seed migration) → re-bake → re-record. Up to 2 iterations; escalate after.

## Phase 5 — Publish + cross-link

Gates before publish:
- Phase 2 polling complete.
- At least one verdict row recorded.
- User confirmed Phase 3 draft.
- Phase 4.5 bake-vs-design conformance: every visible-defect-surface verdict = pass (prefab inspect or screenshot evidence on file).

Actions:
1. `ui_panel_publish(slug={slug}, regen_snapshot=true)` — increment entity_version + flag snapshot regen.
2. Append JSON seed block to `docs/ui-element-definitions.md §{PanelName}`.
3. Update `ia/specs/ui-design-system.md` if new panel introduces new tokens or components (append to §Tokens / §Components tables).
4. Ask user: "Panel `{slug}` published (version {N}). Snapshot regen required — run `ui_panel_publish` flow or bake pipeline. Cross-linked to docs + spec. Done?"

## Verdict shape (caveman summary)

```
ui-element-grill {panel_slug} — phase {N}/5
corpus_entries: {C}  tokens_loaded: {T}  components_loaded: {K}
draft: {rect_json.anchor_min}–{rect_json.anchor_max}  layout: {layout}
params_json keys: {list}
verdicts_recorded: {V}  last_outcome: pass|fail
publish: {done|pending}  version: {prev}→{new}
next: {Phase N+1 action or done}
```

## Seed prompt

```markdown
Run ui-element-grill (`ia/skills/ui-element-grill/SKILL.md`) for {panel_slug}.
Phase 1: load corpus + token/component lists + design-definition line range. Phase 2: poll me for panel intent + variants (5 questions, product language). Phase 3: draft definition shape + show me for confirmation. Phase 4: bake + verdicts loop (record ≥1 verdict). Phase 4.5: re-bake → prefab inspect or Play-Mode screenshot → diff against design line range → record one verdict per visible defect surface (zone, kind, size, bind, sound). Phase 5: publish + cross-link docs on my explicit "yes". Hard boundaries: no auto-publish, no Phase 2 skip, no DB-row-only PASS without render proof.
```

## Changelog

- `cityscene-mainmenu-panel-rollout 2.0` — main-menu shipped 7 visible defects (sibling Quit-confirm, inline back-button, branding `--`, full-width buttons, missing rounded body, lost blip sounds) despite Phase 4 verdicts marked PASS. Skill produced DB rows that satisfied schema + corpus checks but never validated what Unity actually rendered. **Lesson:** corpus + verdicts ≠ visual conformance. Phase 4 ended too early — DB row PASS treated as terminal. Added Phase 4.5 bake-vs-design conformance pass: rebake → prefab inspect or screenshot → diff against design definition line range → record one verdict per visible defect surface (zone, kind, size, bind, sound). Hard boundary added: DB-row-only PASS without render proof = blocked publish.
- `cityscene-mainmenu-panel-rollout 2.0` post-bake — buttons rendered correctly but **inert on click**. Three latent gaps surfaced only at runtime: (1) **action-wire gap** — bake handler dropped `params_json.action` on the floor; no `UiActionTrigger` MonoBehaviour existed to subscribe `IlluminatedButton.OnClick → UiActionRegistry.Dispatch`; (2) **action-id drift** — `panels.json` (canonical Wave A0) used `mainmenu.openSettings` / `openLoad` / `openNewGame` / `quit.confirm`, scene-side `MainMenuController` registered `mainmenu.settings` / `load` / `new-game` / `quit-confirmed` — silent dispatch miss, no compile error; (3) **second wire-site drift** — bake handler had two switch cases for button kinds (`illuminated-button` + `confirm-button`); first edit only patched one, quit-button remained inert until second case got the same `AttachUiActionTrigger` call. **Lesson:** Phase 4.5 visual diff still passed because pixels were correct — runtime click was outside the conformance frame. Add Phase 4.6 **action-wire conformance**: for every panel child with `params_json.action`, prefab_inspect must show a `UiActionTrigger` component with `_actionId` matching the seed canonical. Diff `panels.json` action ids vs scene-side `actionRegistry.Register` calls (one source of truth = panels.json — controllers register against it, not the other way). Hard boundary: action_id mismatch ≥ 1 = blocked publish. Cross-cutting: any new button-kind switch case in bake handler must include action-wire helper call — drift surface flagged in `validate:bake-handler-action-coverage` (TBD).
