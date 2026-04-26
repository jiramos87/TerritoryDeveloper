using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-tick rollup math: collapses every <see cref="SignalField"/> cell into per-district mean / P90 keyed by <see cref="SignalMetadataRegistry.Entry.rollup"/>. Static — no per-frame <c>FindObjectOfType</c>. Output written to <see cref="DistrictSignalCache"/>; empty buckets emit <c>float.NaN</c>. See <c>ia/specs/simulation-signals.md</c> District layer.</summary>
    public static class DistrictAggregator
    {
        private static readonly int SignalCount = Enum.GetValues(typeof(SimulationSignal)).Length;

        /// <summary>Roll every signal field into per-district values via metadata.rollup. Clears <paramref name="cache"/> first so prior-tick state never leaks. No-op when any required dep is null.</summary>
        public static void Aggregate(SignalFieldRegistry registry, DistrictMap map, SignalMetadataRegistry metadata, DistrictSignalCache cache)
        {
            if (registry == null || map == null || metadata == null || cache == null)
            {
                return;
            }

            cache.Clear();

            // Per-district accumulators reused across signals (cleared per signal).
            List<float>[] buckets = new List<float>[DistrictMap.DistrictCount];
            for (int d = 0; d < DistrictMap.DistrictCount; d++)
            {
                buckets[d] = new List<float>();
            }

            int width = map.Width;
            int height = map.Height;

            for (int s = 0; s < SignalCount; s++)
            {
                SimulationSignal signal = (SimulationSignal)s;
                SignalField field = registry.GetField(signal);
                if (field == null)
                {
                    for (int d = 0; d < DistrictMap.DistrictCount; d++)
                    {
                        cache.Set(d, signal, float.NaN);
                    }
                    continue;
                }

                for (int d = 0; d < DistrictMap.DistrictCount; d++)
                {
                    buckets[d].Clear();
                }

                int fieldW = Mathf.Min(width, field.Width);
                int fieldH = Mathf.Min(height, field.Height);
                for (int y = 0; y < fieldH; y++)
                {
                    for (int x = 0; x < fieldW; x++)
                    {
                        int districtId = map.GetDistrictId(x, y);
                        if (districtId < 0 || districtId >= DistrictMap.DistrictCount)
                        {
                            continue;
                        }
                        buckets[districtId].Add(field.Get(x, y));
                    }
                }

                RollupRule rollup = metadata.GetMetadata(signal).rollup;
                for (int d = 0; d < DistrictMap.DistrictCount; d++)
                {
                    List<float> bucket = buckets[d];
                    if (bucket.Count == 0)
                    {
                        cache.Set(d, signal, float.NaN);
                        continue;
                    }

                    float value;
                    if (rollup == RollupRule.P90)
                    {
                        bucket.Sort();
                        int idx = Mathf.FloorToInt(0.9f * (bucket.Count - 1));
                        value = bucket[idx];
                    }
                    else
                    {
                        // RollupRule.Mean (default).
                        float sum = 0f;
                        for (int i = 0; i < bucket.Count; i++)
                        {
                            sum += bucket[i];
                        }
                        value = sum / bucket.Count;
                    }
                    cache.Set(d, signal, value);
                }
            }
        }
    }
}
