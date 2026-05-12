namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler archetype baking concern.
    /// Stage 6.0 Tier-D extraction — concern boundary stub.
    /// Real bake body lives in Territory.Editor.Bridge.UiBakeHandler.Archetype.cs
    /// + Bridge/UiBakeHandler.ArchetypeImpl.cs. This service marks the concern boundary
    /// for the StudioControl interactive bake surface.
    /// UI.Editor assembly (noEngineReferences=true) — no UnityEngine/UnityEditor usage.
    /// </summary>
    public class UiBakeArchetypeService
    {
        /// <summary>Concern tag for discovery — archetype (interactive bake, juice, render-targets).</summary>
        public const string ConcernTag = "ui-bake-archetype";
    }
}
