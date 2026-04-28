using System;

namespace Territory.UI.Themed
{
    /// <summary>Slot graph entry baked from IR <c>panels[].slots[]</c>; mirrors transcribe-time accept-rule guard.</summary>
    [Serializable]
    public struct SlotSpec
    {
        /// <summary>Slot identifier matching IR slot name.</summary>
        public string slug;

        /// <summary>Allowed primitive type slugs for this slot.</summary>
        public string[] accepts;
    }
}
