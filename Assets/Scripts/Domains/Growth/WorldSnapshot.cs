using System;

namespace Territory.Domain.Growth
{
    /// <summary>Lightweight value-type snapshot of world population data used by GrowthCatchupRunner. Decoupled from Unity scene objects.</summary>
    [Serializable]
    public class WorldSnapshot
    {
        /// <summary>Per-zone population counts. Index maps to zone id.</summary>
        public int[] zonePops;

        /// <summary>Current game tick at which this snapshot was taken.</summary>
        public long tick;

        /// <summary>Deterministic seed used for all RNG during this snapshot's growth cycles.</summary>
        public uint growthSeed;

        public WorldSnapshot() { }

        public WorldSnapshot(int[] zonePops, long tick, uint growthSeed)
        {
            this.zonePops   = zonePops != null ? (int[])zonePops.Clone() : Array.Empty<int>();
            this.tick       = tick;
            this.growthSeed = growthSeed;
        }

        /// <summary>Deep clone — caller owns returned instance.</summary>
        public WorldSnapshot Clone()
            => new WorldSnapshot((int[])zonePops.Clone(), tick, growthSeed);

        /// <summary>Simple checksum for determinism verification.</summary>
        public long Checksum()
        {
            long h = tick * 31L + (long)growthSeed;
            if (zonePops == null) return h;
            for (int i = 0; i < zonePops.Length; i++)
                h = h * 31L + zonePops[i];
            return h;
        }
    }
}
