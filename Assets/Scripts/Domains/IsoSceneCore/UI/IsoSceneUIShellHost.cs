using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.IsoSceneCore.UI
{
    /// <summary>
    /// Host MonoBehaviour for the shared IsoSceneCore UI shell (DEC-A28 Host pattern).
    /// Owns one UIDocument with 5 named slot containers. Per-scene plugins query via Slot(name)
    /// and add their visual elements; shell stays scene-agnostic.
    /// Slots: hud-slot, toolbar-slot, subtype-slot, modal-slot, toast-slot.
    /// CitySceneHudSlotParityBaseline — visibility-delta-test anchor for Stage 1.2 regression.
    /// </summary>
    public sealed class IsoSceneUIShellHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        VisualElement _root;

        void Awake()
        {
            if (_doc == null)
                _doc = GetComponent<UIDocument>();

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[IsoSceneUIShellHost] UIDocument missing or rootVisualElement null — shell inactive.");
                return;
            }

            _root = _doc.rootVisualElement;
            _root.style.position = Position.Absolute;
            _root.style.top = 0;
            _root.style.left = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Returns the named slot container. Plugin callers: Add(element) to the returned
        /// VisualElement. Returns null when slot name not found or shell not initialized.
        /// </summary>
        public VisualElement Slot(string name)
        {
            if (_root == null) return null;
            return _root.Q(name);
        }
    }
}
