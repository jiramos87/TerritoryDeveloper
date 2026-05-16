using Territory.Domain.Growth;

namespace Territory.Services
{
    /// <summary>Interface for GrowthCatchupRunner — allows test injection.</summary>
    public interface IGrowthCatchupRunner
    {
        /// <summary>Advance dormant snapshot by elapsedTicks using deterministic seeded RNG. Returns new snapshot at tick + elapsedTicks.</summary>
        WorldSnapshot Catchup(WorldSnapshot dormantSnapshot, long elapsedTicks);
    }

    /// <summary>CoreScene service — rescues Compact/Expand + ICityEvolver from multi-scale Stage 9.0. Runs one-shot on entry to destination scene to advance dormant world by elapsed ticks.</summary>
    public sealed class GrowthCatchupRunner : IGrowthCatchupRunner
    {
        private readonly ICityEvolver _evolver;

        public GrowthCatchupRunner() : this(new DefaultCityEvolver()) { }

        public GrowthCatchupRunner(ICityEvolver evolver)
        {
            _evolver = evolver;
        }

        /// <summary>Deterministic catchup: compact → advance elapsedTicks → expand. Returns new snapshot; input unchanged.</summary>
        public WorldSnapshot Catchup(WorldSnapshot dormantSnapshot, long elapsedTicks)
        {
            if (dormantSnapshot == null || elapsedTicks <= 0)
                return dormantSnapshot?.Clone() ?? new WorldSnapshot();

            // Compact → work on isolated payload.
            var compactPops = CompactExpand.Compact(dormantSnapshot);
            var working     = CompactExpand.Expand(compactPops, dormantSnapshot.tick, dormantSnapshot.growthSeed);

            // Advance tick by tick deterministically.
            for (long i = 0; i < elapsedTicks; i++)
            {
                // Per-tick seed = growthSeed XOR lower 32 bits of (tick+i) for variety.
                uint tickSeed = dormantSnapshot.growthSeed ^ (uint)((dormantSnapshot.tick + i) & 0xFFFFFFFFL);
                _evolver.EvolveOneTick(working, tickSeed);
            }

            working.tick = dormantSnapshot.tick + elapsedTicks;
            return working;
        }
    }
}
