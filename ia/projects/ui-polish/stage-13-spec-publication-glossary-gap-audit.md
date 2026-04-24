### Stage 13 — CityStats handoff artifacts / Spec publication + glossary gap audit

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Promote the token / primitive / juice catalog to normative status in `ui-design-system.md` §1–§2 + §1.5. Audit glossary for missing Stats-dashboard-consumable rows; patch gaps.

**Exit:**

- `ui-design-system.md` §1 (palette + tokens) + §1.5 (motion) + §2 (components) carry normative references to every shipped primitive + StudioControl + juice helper + token block.
- Glossary gap audit table captured in stage handoff (not persisted — validation only); any missing rows added.
- Phase 1 — ui-design-system.md normative promotion.
- Phase 2 — Glossary audit + gap patch.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | ui-design-system §1 + §1.5 promotion | _pending_ | _pending_ | Edit `ia/specs/ui-design-system.md` §1 — add "Extended token catalog" subsection listing every field in `UiTheme.studioRack` + `UiTheme.motion` with role description. §1.5 — motion block becomes normative (semantically-named entries + durations / curves). Cite back to `docs/ui-polish-exploration.md` as source of record. |
| T13.2 | ui-design-system §2 components | _pending_ | _pending_ | `ia/specs/ui-design-system.md` §2 extended with ThemedPrimitive family (10) + StudioControl family (8) + JuiceLayer + 6 helpers — each as a subsection with type name + file path + contract ref (`IThemed` / `IStudioControl`). Marks the catalog normative (CityStats must reference, not reinvent). |
| T13.3 | Glossary gap audit + patch | _pending_ | _pending_ | Cross-check terms in exploration §CityStats handoff table against `ia/specs/glossary.md`. Expected rows: token ring, studio-rack token, motion token, themed primitive, studio-control primitive, knob, fader, VU meter, oscilloscope, illuminated button, segmented readout, detent ring, LED, juice layer, tween counter, pulse on event, sparkle burst, shadow depth, needle ballistics, oscilloscope sweep, IThemed contract, IStudioControl contract, ThemeBroadcaster, signal source binding. Any missing → add in this task. |
