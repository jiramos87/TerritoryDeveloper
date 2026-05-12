namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler orchestrator concern.
    /// Stage 6.0 Tier-D extraction — concern boundary stub.
    /// Real bake body lives in Territory.Editor.Bridge.UiBakeHandler (Bridge/UiBakeHandler.cs
    /// + Bridge/UiBakeHandler.CoreImpl.cs). This service represents the concern boundary
    /// so the domain folder has an explicit service surface per atomization spec.
    /// UI.Editor assembly (noEngineReferences=true) — no UnityEngine/UnityEditor usage.
    /// </summary>
    public class UiBakeCoreService
    {
        /// <summary>Concern tag for discovery — orchestrator (bake, write, parse, tokens).</summary>
        public const string ConcernTag = "ui-bake-core";
    }
}
