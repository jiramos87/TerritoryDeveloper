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

## Phase 5 — Publish + cross-link

Gates before publish:
- Phase 2 polling complete.
- At least one verdict row recorded.
- User confirmed Phase 3 draft.

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
Phase 1: load corpus + token/component lists. Phase 2: poll me for panel intent + variants (5 questions, product language). Phase 3: draft definition shape + show me for confirmation. Phase 4: bake + verdicts loop (record ≥1 verdict). Phase 5: publish + cross-link docs on my explicit "yes". Hard boundaries: no auto-publish, no Phase 2 skip.
```
