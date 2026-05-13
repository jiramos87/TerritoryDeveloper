using Territory.UI;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Static helper to propagate UiTheme refs recursively. Extracted from UiBakeHandler (TECH-31985).</summary>
    public static class ThemeRefPropagator
    {
        /// <summary>Recursively wire UiTheme ref onto every Component on root + descendants
        /// that exposes a _themeRef SerializedProperty.</summary>
        public static void PropagateRecursive(GameObject root, UiTheme theme)
        {
            if (root == null || theme == null) return;
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var c in components)
            {
                if (c == null) continue;
                Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(c, theme);
            }
        }
    }
}
