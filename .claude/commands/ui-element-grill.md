---
description: Guided 5-phase grill for UI element definition authoring. Phase 1 loads corpus + design-system spec. Phase 2 polls user for panel intent + variants. Phase 3 drafts the definition shape (DB rows + seed JSON). Phase 4 runs bake + verdicts loop (ui_calibration_verdict_record). Phase 5 publishes + cross-links docs. MCP slice usage order: ui_panel_list → ui_calibration_corpus_query → ui_token_list → ui_component_list → ui_panel_publish. Late-formalized per Q2 — corpus + verdicts + MCP slices proven by Stage 4 close. Triggers: "/ui-element-grill", "ui element grill", "grill ui panel", "define panel", "author panel definition".
argument-hint: "{panel_slug}"
---

# /ui-element-grill — 5-phase grill flow for authoring and publishing a UI panel definition: load corpus + design-system spec context → interview-poll user for panel intent + variants → draft definition shape → bake + verdicts loop → publish + cross-link. Uses corpus + verdicts as primary memory surfaces; MCP slices order: ui_panel_list → ui_calibration_corpus_query → ui_token_list → ui_component_list → ui_panel_publish.

Drive `$ARGUMENTS` via the [`ui-element-grill`](../agents/ui-element-grill.md) subagent.

Follow `caveman:caveman` for all output. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

## Triggers

- /ui-element-grill
- ui element grill
- grill ui panel
- define panel
- author panel definition
## Dispatch

Single Agent invocation with `subagent_type: "ui-element-grill"` carrying `$ARGUMENTS` verbatim.

## Hard boundaries

See [`ia/skills/ui-element-grill/SKILL.md`](../../ia/skills/ui-element-grill/SKILL.md) §Hard boundaries.
