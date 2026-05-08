---
name: ui-element-grill
purpose: >-
  5-phase grill flow for authoring and publishing a UI panel definition: load
  corpus + design-system spec context → interview-poll user for panel intent
  + variants → draft definition shape → bake + verdicts loop → publish + cross-link.
  Uses corpus + verdicts as primary memory surfaces; MCP slices order:
  ui_panel_list → ui_calibration_corpus_query → ui_token_list → ui_component_list
  → ui_panel_publish.
audience: agent
loaded_by: "skill:ui-element-grill"
slices_via: none
description: >-
  Guided 5-phase grill for UI element definition authoring. Phase 1 loads
  corpus + design-system spec. Phase 2 polls user for panel intent + variants.
  Phase 3 drafts the definition shape (DB rows + seed JSON). Phase 4 runs
  bake + verdicts loop (ui_calibration_verdict_record). Phase 5 publishes
  + cross-links docs. MCP slice usage order: ui_panel_list →
  ui_calibration_corpus_query → ui_token_list → ui_component_list →
  ui_panel_publish. Late-formalized per Q2 — corpus + verdicts + MCP slices
  proven by Stage 4 close. Triggers: "/ui-element-grill", "ui element grill",
  "grill ui panel", "define panel", "author panel definition".
phases:
  - "Phase 1: Load corpus + design-system spec"
  - "Phase 2: Interview-poll user for panel intent + variants"
  - "Phase 3: Draft definition shape"
  - "Phase 4: Bake + verdicts loop"
  - "Phase 5: Publish + cross-link"
triggers:
  - /ui-element-grill
  - ui element grill
  - grill ui panel
  - define panel
  - author panel definition
argument_hint: "{panel_slug}"
model: sonnet
tools_role: custom
tools_extra:
  - ui_panel_list
  - ui_panel_get
  - ui_calibration_corpus_query
  - ui_calibration_verdict_record
  - ui_token_list
  - ui_token_get
  - ui_component_list
  - ui_component_get
  - ui_panel_publish
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
hard_boundaries:
  - Never auto-publish without explicit human confirmation in Phase 5.
  - Do not skip Phase 2 polling — panel intent drives definition correctness.
  - Verdicts loop (Phase 4) must record at least one verdict before publish.
---

@ia/skills/ui-element-grill/agent-body.md
