### Stage 6 — StudioControl ring / IStudioControl + StudioControlBase + contract tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Lock the signal-binding contract + base class every widget inherits. Prove allocation-free `Update` path before any widget ships.

**Exit:**

- `IStudioControl.cs` final signature (per exploration §CityStats handoff table).
- `StudioControlBase.cs` — inherits `ThemedPrimitiveBase`; caches `Func<float> _source`; `Update` reads `_source?.Invoke() ?? 0f` into `Value` then calls abstract `RenderValue(float)`.
- PlayMode test — bind source → 1000 `Update` frames → `GC.Alloc` delta == 0.
- Phase 1 — Contract + base class.
- Phase 2 — Alloc-free PlayMode fixture.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | IStudioControl interface | _pending_ | _pending_ | `Assets/Scripts/UI/StudioControls/IStudioControl.cs` — `float Value`, `Vector2 Range`, `AnimationCurve ValueToDisplay`, `void BindSignalSource(Func<float>)`, `void Unbind()`. XML doc: bind once in `Awake` / `OnEnable`; never rebind per frame. Matches exploration §CityStats handoff table row 3. |
| T6.2 | StudioControlBase abstract class | _pending_ | _pending_ | `StudioControlBase.cs` — `abstract class StudioControlBase : ThemedPrimitiveBase, IStudioControl`. Fields: `protected Func<float> _source; public float Value { get; private set; }; public Vector2 Range; public AnimationCurve ValueToDisplay;`. `Update`: if `_source != null` → `Value = _source.Invoke()` → call `protected abstract void RenderValue(float normalized)` with `Mathf.InverseLerp(Range.x, Range.y, Value)`. |
| T6.3 | Alloc-free PlayMode fixture | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/StudioControlAllocTests.cs`: mock widget subclass; bind `() => Time.time`; run 1000 frames via `yield return null`; assert `GC.GetTotalMemory(false)` delta < 64 bytes (headroom for Unity internals). Covers Review Note C concern — baseline for JuiceLayer tuning in Step 4. |
| T6.4 | Glossary rows (studio contracts) | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `StudioControl primitive` (interactive MonoBehaviour under `Assets/Scripts/UI/StudioControls/*` implementing `IStudioControl` + `IThemed`), `IStudioControl contract` (value / range / curve / bind-source interface), `Signal source binding` (Awake-cached `Func<float>` read each `Update` without alloc). |
