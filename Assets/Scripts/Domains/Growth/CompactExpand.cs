using System;

namespace Territory.Domain.Growth
{
    /// <summary>Compact/Expand transformer rescued from multi-scale T12.3. Compact = serialize dormant city to compressed form; Expand = re-inflate after catchup.</summary>
    public static class CompactExpand
    {
        /// <summary>Compact snapshot to a minimal int[] payload (zone populations only). Returns new array; input unchanged.</summary>
        public static int[] Compact(WorldSnapshot snapshot)
        {
            if (snapshot?.zonePops == null) return Array.Empty<int>();
            return (int[])snapshot.zonePops.Clone();
        }

        /// <summary>Expand compact payload back into a WorldSnapshot at the given tick + seed.</summary>
        public static WorldSnapshot Expand(int[] compactPops, long tick, uint growthSeed)
        {
            var pops = compactPops != null ? (int[])compactPops.Clone() : Array.Empty<int>();
            return new WorldSnapshot(pops, tick, growthSeed);
        }
    }
}
