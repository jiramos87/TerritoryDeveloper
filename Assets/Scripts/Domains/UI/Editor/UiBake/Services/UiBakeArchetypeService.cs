namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler archetype baking concern.
    /// Stage 2 (TECH-31980/31981): StudioControlBaker + JuiceAttacher POCOs extracted here.
    /// Real bake body lives in Territory.Editor.Bridge.UiBakeHandler.Archetype.cs
    /// + Bridge/UiBakeHandler.ArchetypeImpl.cs delegated via StudioControlBaker + JuiceAttacher.
    /// </summary>
    public class UiBakeArchetypeService
    {
        /// <summary>Concern tag for discovery — archetype (interactive bake, juice, render-targets).</summary>
        public const string ConcernTag = "ui-bake-archetype";
    }
}
