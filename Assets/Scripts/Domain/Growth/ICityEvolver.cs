namespace Territory.Domain.Growth
{
    /// <summary>Contract for city population evolution during growth catch-up. Lifted from multi-scale Stage 9.0 shape; adapted to per-scene FS save model.</summary>
    public interface ICityEvolver
    {
        /// <summary>Advance city populations by one tick using deterministic seeded RNG.</summary>
        /// <param name="snapshot">Mutable snapshot; modified in-place.</param>
        /// <param name="seed">Deterministic seed derived from growthSeed + tick index.</param>
        void EvolveOneTick(WorldSnapshot snapshot, uint seed);
    }
}
