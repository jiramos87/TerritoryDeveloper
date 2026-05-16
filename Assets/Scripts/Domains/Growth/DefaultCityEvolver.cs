using System;

namespace Domains.Growth
{
    /// <summary>Default ICityEvolver — deterministic LCG-based population advance per tick. Each zone grows by seeded pseudo-random fraction of current pop.</summary>
    public sealed class DefaultCityEvolver : ICityEvolver
    {
        // LCG parameters (same constants as Java util.Random for portability).
        private const long LcgA = 0x5DEECE66DL;
        private const long LcgC = 0xBL;
        private const long LcgM = (1L << 48);

        public void EvolveOneTick(WorldSnapshot snapshot, uint seed)
        {
            if (snapshot?.zonePops == null || snapshot.zonePops.Length == 0) return;

            long state = (long)seed ^ LcgA;
            for (int i = 0; i < snapshot.zonePops.Length; i++)
            {
                state = (state * LcgA + LcgC) & (LcgM - 1);
                // Growth rate: 0..4% per tick per zone (deterministic).
                int growthRatePpm = (int)((state >> 16) % 41); // 0..40 → 0..4%
                int delta = Math.Max(1, snapshot.zonePops[i] * growthRatePpm / 1000);
                snapshot.zonePops[i] += delta;
            }
        }
    }
}
