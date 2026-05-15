namespace Territory.IsoSceneCore.Contracts
{
    /// <summary>
    /// Per-scene tool registration contract. Scenes call Register at Start; shell toolbar
    /// slot enumerates registered tools to populate tile buttons.
    /// </summary>
    public interface IIsoSceneToolRegistry
    {
        /// <summary>Register a tool into the shared toolbar slot.</summary>
        void Register(IsoSceneTool tool);

        /// <summary>Returns all tools registered for a given toolbar slot position.</summary>
        System.Collections.Generic.IReadOnlyList<IsoSceneTool> ToolsForSlot(ToolbarSlot slot);
    }

    /// <summary>Toolbar slot position identifier for per-scene tool layout.</summary>
    public enum ToolbarSlot
    {
        Primary,
        Secondary,
        Utility
    }

    /// <summary>Lightweight descriptor for a scene-specific tool tile.</summary>
    public sealed class IsoSceneTool
    {
        /// <summary>Unique slug used for tile name and routing (e.g. "zone-r", "road").</summary>
        public string Slug { get; }
        /// <summary>Tooltip label shown on the tile.</summary>
        public string Label { get; }
        /// <summary>Target toolbar slot.</summary>
        public ToolbarSlot Slot { get; }

        public IsoSceneTool(string slug, string label, ToolbarSlot slot = ToolbarSlot.Primary)
        {
            Slug = slug;
            Label = label;
            Slot = slot;
        }
    }
}
