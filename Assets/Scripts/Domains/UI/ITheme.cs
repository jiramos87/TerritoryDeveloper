using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI
{
    /// <summary>
    /// Public facade interface for the UI Theme domain.
    /// Consumers bind to this interface only — never to UIManager or ThemeService directly.
    /// Stage 19 tracer slice: StyleSiblingLabelTexts, FindNamedAncestor, TryGetRectBoundsInParent,
    /// CreateDividerStripe extracted; full HUD theme pipeline methods remain on UIManager partial.
    /// </summary>
    public interface ITheme
    {
        /// <summary>Style all sibling Text components with caption tokens.</summary>
        void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor);

        /// <summary>Walk ancestors returning first match by exact name.</summary>
        Transform FindNamedAncestor(Transform t, string exactName);
    }
}
