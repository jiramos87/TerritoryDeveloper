using Domains.UI.Data;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>POCO baker for illuminated-button interactives. Extracted from UiBakeHandler (TECH-31977).
    /// Constructor takes BakeContext; Bake(IrInteractive) is the entry point.
    /// Full bake body wired in Stage 3 hub thin — Stage 1 establishes the type boundary.</summary>
    public class ButtonBaker
    {
        readonly BakeContext _ctx;

        public ButtonBaker(BakeContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Bake an illuminated-button interactive row into a prefab root GameObject.
        /// Stage 1 stub — returns null until Stage 3 wires the full body from UiBakeHandler.ButtonImpl.cs.</summary>
        public GameObject Bake(IrInteractive row)
        {
            // Stage 3 migrates UiBakeHandler.ButtonImpl.cs body here.
            // Returning null here is intentional — hub still owns the bake path in Stage 1.
            return null;
        }
    }
}
