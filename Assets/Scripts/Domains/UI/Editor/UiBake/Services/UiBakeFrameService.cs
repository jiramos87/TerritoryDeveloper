namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Domain facade for UiBakeHandler frame/panel baking concern.
    /// Stage 2 (TECH-31979): FrameBaker POCO extracted here.
    /// Bridge FrameImpl.cs delegates SavePanelPrefab to FrameBaker.
    /// </summary>
    public class UiBakeFrameService
    {
        /// <summary>Concern tag for discovery — frame (panel prefab, rows, borders, tab wiring).</summary>
        public const string ConcernTag = "ui-bake-frame";
    }
}
