### Stage 10 — JuiceLayer ring / Helpers batch B (SparkleBurst / NeedleBallistics / OscilloscopeSweep) + alloc tests

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Remaining 3 helpers + allocation-free verification suite + retrofit Step 3 widgets (VUMeter / Oscilloscope) to consume shared helpers instead of inlined logic.

**Exit:**

- `SparkleBurst` — particle burst helper using pooled `ParticleSystem` + token palette.
- `NeedleBallistics` — struct helper consumed by `VUMeter`; replaces inline state from Stage 3.3.
- `OscilloscopeSweep` — ring-buffer helper consumed by `Oscilloscope`; replaces inline buffer.
- VUMeter + Oscilloscope retrofitted to use helpers (reduces Stage 3.3 debt).
- PlayMode test suite — 6 helpers each running 1000 frames `GC.Alloc` == 0. Covers Review Note C baseline for `/verify-loop` profiler validation.
- Phase 1 — SparkleBurst w/ pooled particles.
- Phase 2 — NeedleBallistics + OscilloscopeSweep + widget retrofit.
- Phase 3 — Alloc-free suite + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | SparkleBurst helper | _pending_ | _pending_ | `SparkleBurst.cs` MonoBehaviour — pooled `ParticleSystem` with color-over-lifetime from `theme.studioRack.sparklePalette`. `Burst(Vector2 position)` emits fixed particle count; duration from `theme.motion.sparkleDuration`. `ParticleSystem.Emit` uses `EmitParams` struct — no alloc. |
| T10.2 | NeedleBallistics struct | _pending_ | _pending_ | `NeedleBallistics.cs` — value-type struct w/ `_displayed`, `_peakHold`, `_peakTimer`. `Tick(float target, float dt, MotionEntry attack, MotionEntry release, float peakHoldSeconds) → float`. Called from `VUMeter.Update` — replaces inline state from T3.3.3. Returns interpolated needle position. |
| T10.3 | OscilloscopeSweep + widget retrofit | _pending_ | _pending_ | `OscilloscopeSweep.cs` — ring-buffer helper class; owns `float[bufferSize]` + `_head`. `Write(float sample)` + `CopyTo(Vector3[] positions)`. Retrofit `Oscilloscope.cs` (T3.3.4) to delegate to this helper. Refit `VUMeter.cs` (T3.3.3) to delegate to `NeedleBallistics` struct. |
| T10.4 | Alloc-free PlayMode suite | _pending_ | _pending_ | `Assets/Tests/PlayMode/UI/JuiceAllocTests.cs`: per helper — 1000 frames of activity (active tween, repeated pulse, continuous sparkle loop, needle sweep, oscilloscope write). Assert `GC.GetTotalMemory` delta < 64 bytes. Baseline captured for `/verify-loop` profiler validation on closeout. |
| T10.5 | Juice glossary rows | _pending_ | _pending_ | Add to `ia/specs/glossary.md`: `Juice layer` (scene MonoBehaviour hosting pooled tween / particle / pulse helpers), `Tween counter`, `Pulse on event`, `Sparkle burst`, `Shadow depth`, `Needle ballistics`, `Oscilloscope sweep`. Each row cites token contract + alloc-free guarantee. |

---
