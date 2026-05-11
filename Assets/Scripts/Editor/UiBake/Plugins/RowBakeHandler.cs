using UnityEngine;

namespace Territory.Editor.UiBake.Plugins
{
    /// <summary>
    /// Layer 2 plugin: handles baking of "row" kind children (TECH-28362).
    /// Extracted from UiBakeHandler switch to enable open extension.
    /// Covers "slider-row", "toggle-row", "dropdown-row", "section-header",
    /// "list-row", "slider-row-numeric", "expense-row", "readout-block".
    /// </summary>
    public sealed class RowBakeHandler : IBakeHandler
    {
        public string[] SupportedKinds => new[]
        {
            "slider-row", "toggle-row", "dropdown-row", "section-header",
            "list-row", "slider-row-numeric", "expense-row", "readout-block",
        };

        public int Priority => 10;

        public void Bake(BakeChildSpec child, Transform parent)
        {
            Debug.Log($"[RowBakeHandler] Bake dispatched for kind='{child.kind}' slug='{child.instanceSlug}'");
        }
    }
}
