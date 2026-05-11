using System.Collections.Generic;
using Territory.Timing;
using UnityEngine;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave B2 (TECH-27084) — exclusive modal open/close coordinator.
    /// TryOpen/Close/IsAnyExclusiveOpen. Calls TimeManager pause-owner APIs on open/close.
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

            // Toggle GameObject visibility when registered.
            if (_panelRoots.TryGetValue(modalSlug, out var root) && root != null)
                root.SetActive(true);

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
