namespace Territory.Simulation.Signals.Events
{
    /// <summary>
    /// Stage 8 hotspot event payload (TECH-1955) — emitted by
    /// <c>Consumers.CrimeHotspotEventEmitter</c> when a district's P90
    /// <see cref="SimulationSignal.Crime"/> rollup exceeds
    /// <see cref="SignalTuningWeightsAsset.CrimeHotspotThreshold"/>. Bucket 5
    /// protest-animation will subscribe later; Stage 8 ships emitter contract
    /// only with no production listener. <see cref="System.Serializable"/> for
    /// JsonUtility round-trip parity (sanity test); no Unity references.
    /// </summary>
    [System.Serializable]
    public struct CrimeHotspotEvent
    {
        public int districtId;
        public float level;
    }
}
