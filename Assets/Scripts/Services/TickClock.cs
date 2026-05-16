using UnityEngine;

namespace Territory.Services
{
    /// <summary>CoreScene service — game-time tick counter. Persists with save via lastTouchedTicks on RegionSaveFile. Pauses during transition state.</summary>
    public sealed class TickClock : MonoBehaviour
    {
        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>Monotonically increasing game-time tick. Advances each Update when not paused.</summary>
        public long CurrentTick { get; private set; }

        /// <summary>When true, tick does not advance. Set during zoom transitions.</summary>
        public bool Paused { get; private set; }

        // ── Internal ─────────────────────────────────────────────────────────
        [SerializeField] private float tickIntervalSeconds = 1f;
        private float _accumulator;

        /// <summary>Restore tick counter from persisted value on load.</summary>
        public void SetTick(long tick) => CurrentTick = tick;

        /// <summary>Pause tick advancement (call at transition start).</summary>
        public void Pause()  => Paused = true;

        /// <summary>Resume tick advancement (call at Landing → Idle).</summary>
        public void Resume() => Paused = false;

        private void Update()
        {
            if (Paused) return;
            _accumulator += Time.deltaTime;
            while (_accumulator >= tickIntervalSeconds)
            {
                _accumulator -= tickIntervalSeconds;
                CurrentTick++;
            }
        }
    }
}
