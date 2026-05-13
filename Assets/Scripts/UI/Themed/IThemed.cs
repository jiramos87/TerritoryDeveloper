using System;
using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Themed UI primitive contract — single hook to apply a <see cref="UiTheme"/> token snapshot.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined alongside ThemedPrimitiveBase ring.</remarks>
    [Obsolete("IThemed quarantined (TECH-32929). Use USS classes via UI Toolkit. Deletion deferred to uGUI purge plan.")]
    public interface IThemed
    {
        /// <summary>Apply token slugs from <paramref name="theme"/>; implementations null-guard accessor returns.</summary>
        void ApplyTheme(UiTheme theme);
    }
}
