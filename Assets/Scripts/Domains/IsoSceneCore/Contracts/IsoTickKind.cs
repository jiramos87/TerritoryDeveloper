namespace Territory.IsoSceneCore
{
    /// <summary>Tick kinds published by IsoSceneTickBus. GlobalTick = one per sim day. RegionTick = one per region evolution step (Stage 4.0).</summary>
    public enum IsoTickKind
    {
        GlobalTick,
        RegionTick
    }
}
