### Stage 9 — JuiceLayer ring / JuiceLayer + helpers batch A (TweenCounter / PulseOnEvent / ShadowDepth)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Scene-root `JuiceLayer` + first 3 helpers landing numeric tween + event pulse + elevation. Prove pooling pattern once before remaining helpers.

**Exit:**

- `JuiceLayer.cs` scene component with `[SerializeField] UiTheme theme` + helper pool references + `Awake` fallback lookup. No singleton.
- `TweenCounter` — poolable struct/class; `Animate(from, to, motionEntry, callback)` reads `theme.motion.moneyTick`; updated via `JuiceLayer.Update` central tick loop.
- `PulseOnEvent` — MonoBehaviour slot on any primitive; `Trigger()` applies scale + glow pulse over `theme.motion.alertPulse`.
- `ShadowDepth` — `MonoBehaviour` on `ThemedPanel`; renders shadow sprite offset + alpha per `theme.studioRack.shadowDepthStops[tier]`.
- Phase 1 — JuiceLayer scene MonoBehaviour + central tick loop.
- Phase 2 — Helpers A (TweenCounter / PulseOnEvent / ShadowDepth).

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | JuiceLayer scene MonoBehaviour | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/JuiceLayer.cs` — `class JuiceLayer : MonoBehaviour`. Inspector field `[SerializeField] UiTheme theme` + `Awake` fallback. Pool containers for active tweens (`List<TweenCounter> _activeTweens`). `Update` iterates active pools in-place (backward loop for removal). Primitives find it via `FindObjectOfType<JuiceLayer>()` in `Awake` only. Invariant #3 + #4 compliant. |
| T9.2 | TweenCounter helper | _pending_ | _pending_ | `Assets/Scripts/UI/Juice/Helpers/TweenCounter.cs` — value-type struct or poolable class. `Animate(from, to, MotionEntry entry, Action<float> onUpdate)` — pushed into `JuiceLayer._activeTweens`; ticked per frame. Easing curve from `entry.easing`; duration from `entry.durationSeconds`. Callback writes lerped value to caller (e.g. `SegmentedReadout.SetValue`). Covers Example 4. |
| T9.3 | PulseOnEvent + ShadowDepth | _pending_ | _pending_ | `PulseOnEvent.cs` MonoBehaviour — slot on any primitive; `Trigger()` pushes scale + glow animation into `JuiceLayer` tween pool reading `theme.motion.alertPulse`. `ShadowDepth.cs` MonoBehaviour — paired with `ThemedPanel`; reads `[SerializeField] int tier` → indexes `theme.studioRack.shadowDepthStops[tier]` → offsets child shadow sprite + sets alpha. Both `IThemed` so token swap retunes. |
