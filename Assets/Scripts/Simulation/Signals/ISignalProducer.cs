namespace Territory.Simulation.Signals
{
    /// <summary>Producer contract: emit per-cell signal contributions into the registry once per signal-tick phase.</summary>
    public interface ISignalProducer
    {
        /// <summary>Add this producer's per-cell contributions to the relevant <see cref="SignalField"/>s.</summary>
        void EmitSignals(SignalFieldRegistry registry);
    }
}
