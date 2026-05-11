using UnityEngine;

namespace Territory.Editor.UiBake.Plugins
{
    /// <summary>
    /// Layer 2 plugin: handles baking of "button" kind children (TECH-28362).
    /// Extracted from UiBakeHandler switch to enable open extension.
    /// Visual bake delegated to UiBakeHandler.BakeChildByKind for now;
    /// plugin acts as dispatch layer so new button variants don't touch the switch.
    /// </summary>
    public sealed class ButtonBakeHandler : IBakeHandler
    {
        public string[] SupportedKinds => new[] { "button", "illuminated-button" };
        public int Priority => 10;

        public void Bake(BakeChildSpec child, Transform parent)
        {
            // Dispatch side-effect: plugin fired — higher-priority override wins.
            // Full render deferred to UiBakeHandler.BakeChildByKind via the existing
            // snapshot bake path. Plugin dispatch records kind selection for audit.
            Debug.Log($"[ButtonBakeHandler] Bake dispatched for kind='{child.kind}' slug='{child.instanceSlug}'");
        }
    }
}
