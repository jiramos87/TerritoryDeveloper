using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Layer 2 non-empty child assert (TECH-28361).
    /// Called after each child-bake iteration in UiBakeHandler.BakePanelSnapshotChildren.
    /// A child is considered "empty" when it carries only a RectTransform and no other
    /// components (i.e. no renderer fired, no StudioControl attached).
    /// Empty stub child → <see cref="BakeException"/> with code "empty_child:{kind}:{panelSlug}".
    /// </summary>
    public static class BakeEmptyChildGuard
    {
        /// <summary>
        /// Assert that <paramref name="childGo"/> is not an empty stub.
        /// Throws <see cref="BakeException"/> when the child carries only RectTransform.
        /// </summary>
        /// <param name="childGo">Baked child GameObject to inspect.</param>
        /// <param name="kind">Kind slug used for the error message.</param>
        /// <param name="panelSlug">Panel slug for diagnostics.</param>
        public static void AssertNotEmpty(GameObject childGo, string kind, string panelSlug)
        {
            if (childGo == null)
            {
                throw new BakeException($"empty_child:{kind}:{panelSlug}");
            }

            bool hasChildren      = childGo.transform.childCount > 0;
            bool hasMeaningfulComp = HasMeaningfulComponent(childGo);

            if (!hasChildren && !hasMeaningfulComp)
            {
                throw new BakeException($"empty_child:{kind}:{panelSlug}");
            }
        }

        // A "meaningful" component is anything beyond Transform / RectTransform.
        // LayoutElement alone doesn't count — it's added by the bake scaffold, not
        // by the renderer. Any renderer-attached Behaviour qualifies.
        private static bool HasMeaningfulComponent(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                var t = c.GetType();
                // RectTransform, Transform, LayoutElement — scaffold-only; skip.
                if (t == typeof(RectTransform)) continue;
                if (t == typeof(LayoutElement)) continue;
                // Any other component (IlluminatedButton, Image, ThemedLabel, etc.) = renderer fired.
                return true;
            }
            return false;
        }
    }
}
