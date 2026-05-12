namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler button/themed-primitive baking concern.
    /// Stage 6.0 Tier-D extraction — concern boundary stub.
    /// Real bake body lives in Territory.Editor.Bridge.UiBakeHandler.Button.cs
    /// + Bridge/UiBakeHandler.ButtonImpl.cs. This service marks the concern boundary
    /// for the Themed* widget spawn and button-state application surface.
    /// UI.Editor assembly (noEngineReferences=true) — no UnityEngine/UnityEditor usage.
    /// </summary>
    public class UiBakeButtonService
    {
        /// <summary>Concern tag for discovery — button (themed primitives, button states, spawn helpers).</summary>
        public const string ConcernTag = "ui-bake-button";
    }
}
