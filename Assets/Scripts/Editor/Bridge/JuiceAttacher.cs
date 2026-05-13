using Domains.UI.Data;
using Domains.UI.Editor.UiBake.Services;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Juice component attachment POCO. Extracted from UiBakeHandler.ArchetypeImpl.cs (TECH-31981).
    /// Constructor takes BakeContext; Attach(GameObject, IrInteractive) is the entry point.</summary>
    public class JuiceAttacher
    {
        readonly BakeContext _ctx;

        public JuiceAttacher(BakeContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Attach JuiceLayer components to a baked prefab root per per-kind defaults + IR juice[] overrides.
        /// Idempotent — delegates to UiBakeHandler.AttachJuiceComponents.</summary>
        public void Attach(GameObject prefabRoot, IrInteractive irRow)
        {
            Territory.Editor.Bridge.UiBakeHandler.AttachJuiceComponents(prefabRoot, irRow);
        }
    }
}
