namespace Territory.Persistence
{
    /// <summary>
    /// Minimal interface bridging SaveCoordinator (Game assembly) and RegionSaveService (RegionScene assembly).
    /// Lets Game-side code stamp tick+seed into region save without taking a direct dependency on the
    /// RegionScene assembly (which would form a cyclic asmdef dep — RegionScene already references Game).
    /// </summary>
    public interface IRegionTickStamper
    {
        /// <summary>Stamp current tick + growth seed into the next region snapshot write.</summary>
        void StampTicks(long currentTick, uint growthSeed);

        /// <summary>Growth seed from last loaded region save (0 if not yet loaded).</summary>
        uint LoadedGrowthSeed { get; }

        /// <summary>Last-touched tick from last loaded region save.</summary>
        long LoadedLastTouchedTicks { get; }
    }
}
