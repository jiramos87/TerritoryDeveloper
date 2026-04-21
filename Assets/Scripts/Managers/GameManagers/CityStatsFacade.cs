using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Tick-scoped MonoBehaviour facade over <see cref="ColumnarStatsStore"/>: <see cref="BeginTick"/> resets
    /// accumulators, <see cref="Publish"/>/<see cref="Set"/> during the tick, <see cref="EndTick"/> flushes to the
    /// ring and raises <see cref="OnTickEnd"/>. Read APIs implement <see cref="IStatsReadModel"/> by delegating to the store.
    /// </summary>
    public class CityStatsFacade : MonoBehaviour, IStatsReadModel
    {
        ColumnarStatsStore _store;

        /// <summary>Fired once per simulation tick after <see cref="ColumnarStatsStore.FlushToSeries"/> in <see cref="EndTick"/>.</summary>
        public event Action OnTickEnd;

        void Awake()
        {
            _store = new ColumnarStatsStore(256);
        }

        /// <summary>Starts a stats tick: clears running accumulators (no ring write).</summary>
        public void BeginTick()
        {
            _store.ResetAccumulators();
        }

        /// <summary>Adds <paramref name="delta"/> to the running accumulator for <paramref name="key"/>.</summary>
        public void Publish(StatKey key, float delta)
        {
            _store.Publish(key, delta);
        }

        /// <summary>Overwrites the running accumulator for <paramref name="key"/>.</summary>
        public void Set(StatKey key, float value)
        {
            _store.Set(key, value);
        }

        /// <summary>Flushes running values into the ring, then invokes <see cref="OnTickEnd"/>.</summary>
        public void EndTick()
        {
            _store.FlushToSeries();
            OnTickEnd?.Invoke();
        }

        /// <inheritdoc />
        public float GetScalar(StatKey key) => _store.GetScalar(key);

        /// <inheritdoc />
        public float[] GetSeries(StatKey key, int windowTicks) => _store.GetSeries(key, windowTicks);

        /// <inheritdoc />
        public IEnumerable<object> EnumerateRows(string dimension, Predicate<object> filter)
        {
            yield break;
        }
    }
}
