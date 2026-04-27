using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>
    /// Stage 6 post-load warmup: re-converges the signal layer after a save reload by
    /// driving <see cref="SignalTickScheduler.Tick"/> a fixed number of times. Without
    /// warmup the first post-load tick exposes raw producer outputs before diffusion +
    /// district rollup settle, breaking <see cref="HappinessComposer"/> /
    /// <see cref="DesirabilityComposer"/> reads. See <c>ia/specs/simulation-signals.md</c>
    /// §Save warmup contract.
    /// </summary>
    public static class SignalWarmupPass
    {
        /// <summary>Default warmup tick count — chosen to outlast a single diffusion radius (≈3) plus margin.</summary>
        public const int DefaultTicks = 5;

        /// <summary>Drive <paramref name="scheduler"/> through <paramref name="ticks"/> ticks of <c>deltaSeconds=1f</c>. Null-guards <paramref name="registry"/> + <paramref name="scheduler"/>; <paramref name="districts"/> may be null (consumers tolerate empty district cache).</summary>
        public static void Run(SignalFieldRegistry registry, DistrictManager districts, SignalTickScheduler scheduler, int ticks = DefaultTicks)
        {
            if (registry == null || scheduler == null)
            {
                return;
            }
            if (ticks <= 0)
            {
                return;
            }
            for (int i = 0; i < ticks; i++)
            {
                scheduler.Tick(1f);
            }
        }
    }
}
