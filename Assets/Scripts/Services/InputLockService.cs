using System;
using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;

namespace Territory.Services
{
    /// <summary>CoreScene MonoBehaviour — manages input lock during zoom transition.
    /// IsLocked = true while ZoomTransitionController state ∈ {Saving, TweeningOut, AwaitLoad, Landing}.
    /// CellStreamingPipeline.FirstRingLoaded → Unlock(). Invariant #3: deps in Start.</summary>
    public class InputLockService : MonoBehaviour
    {
        // ── Public API ───────────────────────────────────────────────────────
        public bool IsLocked { get; private set; }
        public event Action UnlockedEvent;

        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private UnityEngine.UIElements.UIDocument unlockToastDocument;

        // ── Private ──────────────────────────────────────────────────────────
        private ZoomTransitionController _zoom;
        private CellStreamingPipeline _streaming;

        void Start()
        {
            _zoom = FindObjectOfType<ZoomTransitionController>();
            _streaming = FindObjectOfType<CellStreamingPipeline>();

            if (_zoom != null)
                _zoom.StateChanged += OnStateChanged;

            if (_streaming != null)
                _streaming.FirstRingLoaded += Unlock;
        }

        /// <summary>Unlock input — called by CellStreamingPipeline.FirstRingLoaded or manually.</summary>
        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            UnlockedEvent?.Invoke();
            ShowUnlockTell();
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void OnStateChanged(TransitionState state)
        {
            switch (state)
            {
                case TransitionState.Saving:
                case TransitionState.TweeningOut:
                case TransitionState.AwaitLoad:
                case TransitionState.Landing:
                    IsLocked = true;
                    break;
                case TransitionState.Idle:
                    // Idle after failed/cancelled transition — release lock.
                    if (IsLocked)
                    {
                        IsLocked = false;
                        UnlockedEvent?.Invoke();
                    }
                    break;
            }
        }

        /// <summary>Subtle UI tell on unlock — brief toast-style beat using UIDocument if wired,
        /// otherwise a debug log only.</summary>
        private void ShowUnlockTell()
        {
            if (unlockToastDocument != null)
            {
                var root = unlockToastDocument.rootVisualElement;
                var banner = root?.Q<UnityEngine.UIElements.VisualElement>("input-unlock-banner");
                if (banner != null)
                {
                    banner.AddToClassList("input-unlock__banner--visible");
                    // Auto-hide after 1.5s.
                    banner.schedule.Execute(() =>
                        banner.RemoveFromClassList("input-unlock__banner--visible"))
                        .StartingIn(1500);
                }
            }
            else
            {
                Debug.Log("[InputLockService] Input unlocked — pan + zoom re-enabled.");
            }
        }

        void OnDestroy()
        {
            if (_zoom != null)
                _zoom.StateChanged -= OnStateChanged;
            if (_streaming != null)
                _streaming.FirstRingLoaded -= Unlock;
        }
    }
}
