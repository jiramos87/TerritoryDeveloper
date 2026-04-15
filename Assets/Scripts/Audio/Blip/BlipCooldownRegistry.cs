using System.Collections.Generic;

namespace Territory.Audio
{
    /// <summary>
    /// Per-<see cref="BlipId"/> debounce gate keyed on DSP time (seconds, double).
    /// <para>
    /// <b>Clock-agnostic:</b> callers supply <c>nowDspTime</c> — the registry never
    /// reads <c>UnityEngine.AudioSettings.dspTime</c> directly, allowing pure-C#
    /// EditMode tests without a Play Mode harness.
    /// </para>
    /// <para>
    /// <b>Unit convention:</b> <c>nowDspTime</c> is in seconds (matching
    /// <c>AudioSettings.dspTime</c>); <c>cooldownMs</c> is in milliseconds.
    /// Internally the delta is multiplied by 1000 before comparison.
    /// </para>
    /// <para>
    /// <b>Window anchor:</b> the window is anchored to the last <em>accepted</em>
    /// timestamp, not to rejected attempts — prevents starvation under rapid spam.
    /// </para>
    /// <para>
    /// <b>Thread model:</b> main-thread only. No locking. Inherits the main-thread
    /// contract enforced by <c>BlipEngine</c> entry points (Stage 2.3 T2.3.1).
    /// </para>
    /// <para>
    /// No singleton / static state — invariant #4 compliant.
    /// Instantiated as <c>_cooldownRegistry</c> instance field on <c>BlipCatalog</c>.
    /// </para>
    /// </summary>
    public sealed class BlipCooldownRegistry
    {
        private readonly Dictionary<BlipId, double> _lastPlayDspTime = new();

        /// <summary>
        /// PlayMode / EditMode test-only counter — cumulative number of
        /// <see cref="TryConsume"/> calls that returned <c>false</c> (blocked).
        /// Not reset between tests; tests capture a baseline and compute the delta.
        /// </summary>
        internal int BlockedCount { get; private set; }

        /// <summary>
        /// Attempts to consume a cooldown slot for <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <see cref="BlipId"/> to gate.</param>
        /// <param name="nowDspTime">
        /// Current DSP clock in seconds (caller-supplied for testability).
        /// </param>
        /// <param name="cooldownMs">
        /// Minimum gap between accepted plays, in milliseconds.
        /// Pass <c>0</c> to disable gating (every call returns <c>true</c>).
        /// </param>
        /// <returns>
        /// <c>true</c> when the id is unseen or the elapsed time since the last
        /// accepted play meets or exceeds <paramref name="cooldownMs"/>; also
        /// records <paramref name="nowDspTime"/> as the new anchor.
        /// <c>false</c> when still inside the cooldown window; recorded timestamp
        /// is NOT updated.
        /// </returns>
        public bool TryConsume(BlipId id, double nowDspTime, double cooldownMs)
        {
            if (!_lastPlayDspTime.TryGetValue(id, out double last) ||
                (nowDspTime - last) * 1000.0 >= cooldownMs)
            {
                _lastPlayDspTime[id] = nowDspTime;
                return true;
            }

            BlockedCount++;
            return false;
        }
    }
}
