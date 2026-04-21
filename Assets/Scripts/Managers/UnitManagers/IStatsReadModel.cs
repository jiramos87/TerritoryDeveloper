using System;
using System.Collections.Generic;

namespace Territory.Economy
{
    /// <summary>
    /// Pull contract for typed city metrics: scalar read, bounded series window, and optional row iteration.
    /// Implemented by <c>CityStatsFacade</c> (citystats-overhaul); backed by columnar ring-buffer store (TECH-304).
    /// </summary>
    public interface IStatsReadModel
    {
        /// <summary>Current running scalar for <paramref name="key"/> (pre-facade: store accumulator / shim).</summary>
        float GetScalar(StatKey key);

        /// <summary>Last <paramref name="windowTicks"/> flushed samples for <paramref name="key"/>, oldest-first.</summary>
        float[] GetSeries(StatKey key, int windowTicks);

        /// <summary>
        /// Optional dimension iteration for tooling / future drill-down; facade may return empty until wired.
        /// </summary>
        IEnumerable<object> EnumerateRows(string dimension, Predicate<object> filter);
    }
}
