namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler frame/panel baking concern.
    /// Stage 6.0 Tier-D extraction — concern boundary stub.
    /// Real bake body lives in Territory.Editor.Bridge.UiBakeHandler.Frame.cs
    /// + Bridge/UiBakeHandler.FrameImpl.cs. This service marks the concern boundary
    /// for the panel prefab frame, row emission, and slot dispatch surface.
    /// UI.Editor assembly (noEngineReferences=true) — no UnityEngine/UnityEditor usage.
    /// </summary>
    public class UiBakeFrameService
    {
        /// <summary>Concern tag for discovery — frame (panel prefab, rows, borders, tab wiring).</summary>
        public const string ConcernTag = "ui-bake-frame";
    }
}
