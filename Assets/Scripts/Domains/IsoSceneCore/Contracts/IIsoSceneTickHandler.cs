namespace Territory.IsoSceneCore
{
    /// <summary>Subscriber contract for IsoSceneTickBus. Implement in Start; register via IsoSceneTickBus.Subscribe.</summary>
    public interface IIsoSceneTickHandler
    {
        void OnIsoTick(IsoTickKind kind);
    }
}
