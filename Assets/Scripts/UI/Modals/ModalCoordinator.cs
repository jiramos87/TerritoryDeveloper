using System.Collections.Generic;
using Territory.Timing;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave B2 (TECH-27084) — exclusive modal open/close coordinator.
    /// TryOpen/Close/IsAnyExclusiveOpen. Calls TimeManager pause-owner APIs on open/close.
    /// Stage 4.0 (TECH-32916) — co-existence overload: Show(VisualElement) routes migrated
    /// panels via UIDocument; legacy panels stay on Canvas path (strangler co-existence).
    /// MonoBehaviour; mount under CityScene Canvas (Inv #4 — scene component).
    /// ServiceLocator-registered via FindObjectOfType fallback.
    /// </summary>
    public class ModalCoordinator : MonoBehaviour
    {
        private static readonly HashSet<string> ExclusiveGroup = new HashSet<string>
        {
            "stats-panel",
            "budget-panel",
            "pause-menu",
        };

        private readonly HashSet<string> _openModals = new HashSet<string>();

        /// <summary>
        /// Stage 10 hotfix — slug → GameObject registry for SetActive toggling on TryOpen/Close.
        /// Adapters call <see cref="RegisterPanel"/> in OnEnable so coordinator owns visibility.
        /// </summary>
        private readonly Dictionary<string, GameObject> _panelRoots = new Dictionary<string, GameObject>();

        /// <summary>
        /// Stage 4.0 (TECH-32916) — in-memory slug → VisualElement map for migrated panels.
        /// Hosts call <see cref="RegisterMigratedPanel"/> during OnEnable.
        /// Show(VisualElement) overload routes slugs in this dict via UIDocument.Add instead of Canvas prefab.
        /// </summary>
        private readonly Dictionary<string, VisualElement> _migratedPanels = new Dictionary<string, VisualElement>();

        /// <summary>
        /// Stage 4.0 — root UIDocument visual element hosting migrated modal panels (stacked VisualElements).
        /// Wired by ModalDocumentHost (scene component). Null = no UIDocument in scene; migrated route degrades to warning.
        /// </summary>
        [SerializeField] private UIDocument _modalDocument;

        [SerializeField] private TimeManager _timeManager;

        private void Awake()
        {
            if (_timeManager == null)
                _timeManager = FindObjectOfType<TimeManager>();
        }

        /// <summary>
        /// Register a panel GameObject so this coordinator can toggle SetActive on TryOpen/Close.
        /// Idempotent — re-registering the same slug overwrites the previous root.
        /// Panel starts inactive after registration.
        /// </summary>
        public void RegisterPanel(string modalSlug, GameObject root)
        {
            if (string.IsNullOrEmpty(modalSlug) || root == null) return;
            _panelRoots[modalSlug] = root;
            if (!_openModals.Contains(modalSlug))
                root.SetActive(false);
        }

        /// <summary>
        /// Stage 4.0 (TECH-32916) — register a migrated UI Toolkit panel VisualElement.
        /// Hosts call this in OnEnable with their rootVisualElement.
        /// Idempotent; re-registration overwrites previous entry.
        /// Panel starts hidden after registration (display:none).
        /// </summary>
        public void RegisterMigratedPanel(string modalSlug, VisualElement root)
        {
            if (string.IsNullOrEmpty(modalSlug) || root == null) return;
            _migratedPanels[modalSlug] = root;
            if (!_openModals.Contains(modalSlug))
                root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Stage 4.0 (TECH-32916) — Show overload for migrated panels.
        /// Routes slug to VisualElement path when registered; falls back to legacy TryOpen for unregistered slugs.
        /// Exclusive-group semantics preserved (closes other exclusive modals first).
        /// Returns the VisualElement shown, or null when routed to legacy path.
        /// </summary>
        public VisualElement Show(string modalSlug)
        {
            if (string.IsNullOrEmpty(modalSlug)) return null;

            UnityEngine.Debug.Log($"[ModalCoordinator] Show({modalSlug}) — migratedRegistered={_migratedPanels.ContainsKey(modalSlug)} totalRegistered={_migratedPanels.Count}");
            if (_migratedPanels.TryGetValue(modalSlug, out var ve))
            {
                // Close other exclusive-group members first.
                if (IsExclusiveGroup(modalSlug))
                {
                    var toClose = new List<string>();
                    foreach (var slug in _openModals)
                        if (IsExclusiveGroup(slug)) toClose.Add(slug);
                    foreach (var slug in toClose)
                        CloseInternal(slug);
                }

                _openModals.Add(modalSlug);
                ve.style.display = DisplayStyle.Flex;
                var innerShow = ve.Q<UnityEngine.UIElements.VisualElement>(modalSlug);
                if (innerShow != null) innerShow.style.display = DisplayStyle.Flex;

                if (_timeManager != null)
                    _timeManager.SetModalPauseOwner(modalSlug);

                return ve;
            }

            // Legacy fallback — routes to Canvas prefab path.
            TryOpen(modalSlug);
            return null;
        }

        /// <summary>
        /// Stage 4.0 — close a migrated panel by slug (counterpart to Show).
        /// Routes to VisualElement hide when registered; legacy Close otherwise.
        /// </summary>
        public void HideMigrated(string modalSlug)
        {
            if (string.IsNullOrEmpty(modalSlug)) return;

            if (_migratedPanels.TryGetValue(modalSlug, out var ve))
            {
                _openModals.Remove(modalSlug);
                ve.style.display = DisplayStyle.None;
                var innerHide = ve.Q<UnityEngine.UIElements.VisualElement>(modalSlug);
                if (innerHide != null) innerHide.style.display = DisplayStyle.None;
                if (_timeManager != null)
                    _timeManager.ClearModalPauseOwner(modalSlug);
                return;
            }

            CloseInternal(modalSlug);
        }

        /// <summary>Stage 4.0 — true when slug is in the migrated-panels dict (UIDocument route).</summary>
        public bool IsMigrated(string modalSlug) =>
            !string.IsNullOrEmpty(modalSlug) && _migratedPanels.ContainsKey(modalSlug);

        /// <summary>
        /// Attempt to open <paramref name="modalSlug"/>.
        /// Exclusive-group members close all others before opening.
        /// Returns false when already open.
        /// </summary>
        public bool TryOpen(string modalSlug)
        {
            if (string.IsNullOrEmpty(modalSlug)) return false;
            if (_openModals.Contains(modalSlug)) return false;

            if (IsExclusiveGroup(modalSlug))
            {
                // Close all other exclusive-group modals first.
                var toClose = new List<string>();
                foreach (var slug in _openModals)
                {
                    if (IsExclusiveGroup(slug)) toClose.Add(slug);
                }
                foreach (var slug in toClose)
                    CloseInternal(slug);
            }

            _openModals.Add(modalSlug);

            // Toggle GameObject visibility when registered (legacy uGUI prefab path).
            if (_panelRoots.TryGetValue(modalSlug, out var root) && root != null)
                root.SetActive(true);

            // Iter-6: also toggle migrated UI Toolkit panel display via inner element.
            if (_migratedPanels.TryGetValue(modalSlug, out var velement) && velement != null)
            {
                velement.style.display = DisplayStyle.Flex;
                var inner = velement.Q<VisualElement>(modalSlug);
                if (inner != null) inner.style.display = DisplayStyle.Flex;
            }

            if (_timeManager != null)
                _timeManager.SetModalPauseOwner(modalSlug);

            return true;
        }

        /// <summary>Close <paramref name="modalSlug"/>. Clears TimeManager pause-owner when owner matches.</summary>
        public void Close(string modalSlug)
        {
            if (string.IsNullOrEmpty(modalSlug)) return;
            CloseInternal(modalSlug);
        }

        /// <summary>Returns true when any exclusive-group modal is currently open.</summary>
        public bool IsAnyExclusiveOpen()
        {
            foreach (var slug in _openModals)
            {
                if (IsExclusiveGroup(slug)) return true;
            }
            return false;
        }

        private void CloseInternal(string modalSlug)
        {
            _openModals.Remove(modalSlug);
            if (_panelRoots.TryGetValue(modalSlug, out var root) && root != null)
                root.SetActive(false);
            // Iter-6: also hide migrated UI Toolkit panel inner element.
            if (_migratedPanels.TryGetValue(modalSlug, out var velement) && velement != null)
            {
                var inner = velement.Q<VisualElement>(modalSlug);
                if (inner != null) inner.style.display = DisplayStyle.None;
            }
            if (_timeManager != null)
                _timeManager.ClearModalPauseOwner(modalSlug);
        }

        /// <summary>True when modal slug currently open.</summary>
        public bool IsOpen(string modalSlug) => !string.IsNullOrEmpty(modalSlug) && _openModals.Contains(modalSlug);

        private static bool IsExclusiveGroup(string slug)
        {
            return ExclusiveGroup.Contains(slug);
        }
    }
}
