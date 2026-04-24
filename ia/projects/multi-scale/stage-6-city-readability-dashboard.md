### Stage 6 — City MVP close / City readability dashboard

**Status:** In Progress (FEAT-51 filed)

**Objectives:** Player reads city state at-a-glance: minimal HUD + ≥3 time-series charts. Delivers FEAT-51 §2.1–§2.5. Chart library decision recorded.

**Exit:**

- `UiTheme.cs` carries `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields; `ia/specs/ui-design-system.md` §tokens chart subsection added.
- Chart library decision (XCharts or equivalent) recorded in `ia/projects/FEAT-51.md` Decision Log.
- FEAT-51 acceptance (§8): history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts (population trend, employment rate, treasury balance) visible; no per-frame `FindObjectOfType`; `UiTheme` tokens applied throughout.
- Testmode smoke: ≥3 charts render after New Game tick.
- Phase 1 — UiTheme chart tokens + chart library spike.
- Phase 2 — Full dashboard delivery + acceptance gate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | UiTheme chart tokens | _pending_ | _pending_ | Add `chartLineColor`, `chartAxisColor`, `chartLabelFont`, `chartBackground` fields to `UiTheme.cs`; add chart-tokens subsection to `ia/specs/ui-design-system.md` §tokens. |
| T6.2 | Chart library spike | _pending_ | _pending_ | Evaluate XCharts vs alternatives in Unity; create `ChartDemo` prefab (new) with `LineChart` wired to dummy data; validate `UiTheme` token bind; record library decision in `ia/projects/FEAT-51.md` Decision Log. |
| T6.3 | FEAT-51 dashboard delivery | **FEAT-51** | Draft | Full game data dashboard per `ia/projects/FEAT-51.md` §8: history ringbuffer + derived metrics + chart engine + HUD card layout; ≥3 charts; UiTheme tokens applied; no per-frame `FindObjectOfType`. |
| T6.4 | Dashboard acceptance gate | _pending_ | _pending_ | Testmode smoke: ≥3 charts render after New Game tick; token audit — all chart colors sourced from `UiTheme`; `unity:compile-check`; confirm FEAT-51 §8 acceptance met; Decision Log entry verified complete. |
