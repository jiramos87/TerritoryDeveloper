using System;

namespace Territory.Economy
{
    /// <summary>
    /// Plain C# ring-buffer store: one column per <see cref="StatKey"/>, fixed capacity, tick-aligned flush.
    /// Running scalars accumulate via <see cref="Publish"/> / <see cref="Set"/>; <see cref="FlushToSeries"/>
    /// snapshots into the ring and clears accumulators (per citystats Stage 1 Exit).
    /// </summary>
    public sealed class ColumnarStatsStore
    {
        readonly int _ringCapacity;
        readonly int _keyCount;
        readonly float[,] _ring;
        readonly float[] _running;
        int _writeIdx;
        int _filledCount;

        /// <summary>Ring slots per key; default 256 for city-scale facade.</summary>
        public int RingCapacity => _ringCapacity;

        /// <summary>Creates a store with <paramref name="ringCapacity"/> samples per key column.</summary>
        public ColumnarStatsStore(int ringCapacity = 256)
        {
            if (ringCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(ringCapacity));
            _ringCapacity = ringCapacity;
            _keyCount = Enum.GetValues(typeof(StatKey)).Length;
            _ring = new float[_keyCount, _ringCapacity];
            _running = new float[_keyCount];
        }

        /// <summary>Adds <paramref name="delta"/> to the running accumulator for <paramref name="key"/>.</summary>
        public void Publish(StatKey key, float delta)
        {
            _running[(int)key] += delta;
        }

        /// <summary>Overwrites the running accumulator for <paramref name="key"/>.</summary>
        public void Set(StatKey key, float value)
        {
            _running[(int)key] = value;
        }

        /// <summary>Current running value (pre-flush) for <paramref name="key"/>.</summary>
        public float GetScalar(StatKey key)
        {
            return _running[(int)key];
        }

        /// <summary>
        /// Copies up to <paramref name="windowTicks"/> most recent flushed samples for <paramref name="key"/>,
        /// oldest index first.
        /// </summary>
        public float[] GetSeries(StatKey key, int windowTicks)
        {
            if (windowTicks <= 0 || _filledCount <= 0)
                return Array.Empty<float>();

            int k = (int)key;
            int n = Math.Min(windowTicks, _filledCount);
            var result = new float[n];
            int lastSlot = (_writeIdx - 1 + _ringCapacity) % _ringCapacity;
            for (int j = 0; j < n; j++)
            {
                int slot = (lastSlot - (n - 1 - j) + _ringCapacity) % _ringCapacity;
                result[j] = _ring[k, slot];
            }

            return result;
        }

        /// <summary>
        /// Writes each running accumulator into the current ring slot, advances the write head, bumps fill count,
        /// then clears all running accumulators.
        /// </summary>
        public void FlushToSeries()
        {
            for (int i = 0; i < _keyCount; i++)
                _ring[i, _writeIdx] = _running[i];
            _writeIdx = (_writeIdx + 1) % _ringCapacity;
            _filledCount = Math.Min(_filledCount + 1, _ringCapacity);
            Array.Clear(_running, 0, _keyCount);
        }
    }
}
