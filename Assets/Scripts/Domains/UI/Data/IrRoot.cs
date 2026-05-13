using System;

namespace Domains.UI.Data
{
    /// <summary>Top-level IR JSON shape — single bake-input root.</summary>
    [Serializable]
    public class IrRoot
    {
        public IrTokens tokens;
        public IrPanel[] panels;
        public IrInteractive[] interactives;
    }
}
