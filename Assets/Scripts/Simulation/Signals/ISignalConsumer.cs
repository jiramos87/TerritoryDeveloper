namespace Territory.Simulation.Signals
{
    /// <summary>Consumer contract: read post-diffusion signal fields + district rollup cache once per signal-tick phase.</summary>
    public interface ISignalConsumer
    {
        /// <summary>Read signal fields and district aggregates; mutate consumer-owned state.</summary>
        void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache);
    }
}
