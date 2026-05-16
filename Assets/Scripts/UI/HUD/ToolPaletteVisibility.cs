using UnityEngine;
using UnityEngine.UIElements;
using Territory.Services;

namespace Territory.UI.HUD
{
    /// <summary>Subscribes to IsoSceneContextService and hides the tool-palette VisualElement when Context = Region.</summary>
    public class ToolPaletteVisibility : MonoBehaviour
    {
        [SerializeField] private UIDocument hudDocument;
        [Tooltip("USS class name or element name used to query the tool-palette root. Default: 'tool-palette'.")]
        [SerializeField] private string toolPaletteSelector = "tool-palette";

        private IsoSceneContextService _contextService;
        private VisualElement _toolPalette;

        void Start()
        {
            _contextService = FindObjectOfType<IsoSceneContextService>();
            if (_contextService == null)
            {
                Debug.LogWarning("[ToolPaletteVisibility] IsoSceneContextService not found — visibility rule disabled.");
                return;
            }

            if (hudDocument == null)
                hudDocument = GetComponent<UIDocument>();
            if (hudDocument == null)
            {
                hudDocument = FindObjectOfType<UIDocument>();
            }

            if (hudDocument != null && hudDocument.rootVisualElement != null)
                _toolPalette = hudDocument.rootVisualElement.Q(toolPaletteSelector);

            _contextService.ContextChanged += OnContextChanged;
            // Apply immediately for current context.
            ApplyVisibility(_contextService.Context);
        }

        void OnDestroy()
        {
            if (_contextService != null)
                _contextService.ContextChanged -= OnContextChanged;
        }

        private void OnContextChanged(IsoSceneContextService.SceneContext ctx) => ApplyVisibility(ctx);

        private void ApplyVisibility(IsoSceneContextService.SceneContext ctx)
        {
            if (_toolPalette == null) return;
            _toolPalette.style.display = ctx == IsoSceneContextService.SceneContext.Region
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
