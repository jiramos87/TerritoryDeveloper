using System;
using UnityEngine;
using Territory.SceneManagement;

namespace Territory.Services
{
    /// <summary>CoreScene service — locks pan + zoom while ZoomTransitionController is active. Unlock via CellStreamingPipeline.FirstRingLoaded.</summary>
    public class InputLockService : MonoBehaviour
    {
        /// <summary>True while input (pan + zoom) is locked.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>Fired once when the lock is released.</summary>
        public event Action UnlockedEvent;

        [SerializeField] private ZoomTransitionController zoomController;
        [SerializeField] private UnityEngine.UIElements.UIDocument unlockToastDocument;

        // States that require input lock
        private static bool IsLockingState(TransitionState s) =>
            s == TransitionState.Saving ||
            s == TransitionState.TweeningOut ||
            s == TransitionState.AwaitLoad ||
            s == TransitionState.Landing;

        void Awake()
        {
            if (zoomController == null)
                zoomController = FindObjectOfType<ZoomTransitionController>();
        }

        void Start()
        {
            if (zoomController != null)
                zoomController.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            if (zoomController != null)
                zoomController.StateChanged -= OnStateChanged;
        }

        /// <summary>Release the input lock. Called by ZoomTransitionController when CellStreamingPipeline.FirstRingLoaded fires.</summary>
        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            UnlockedEvent?.Invoke();
            ShowUnlockTell();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnStateChanged(TransitionState state)
        {
            if (IsLockingState(state) && !IsLocked)
            {
                IsLocked = true;
            }
            else if (state == TransitionState.Idle && IsLocked)
            {
                // Safety: release lock if transition returns to Idle without FirstRingLoaded
                Unlock();
            }
        }

        /// <summary>Subtle UI tell: brief HUD beat. Re-uses Unity UI Toolkit scheduler for lightweight pulse.</summary>
        private void ShowUnlockTell()
        {
            if (unlockToastDocument == null) return;
            var root = unlockToastDocument.rootVisualElement;
            if (root == null) return;

            // Add unlock-tell CSS class for a brief pulse animation then remove after 800ms
            root.AddToClassList("input-unlock-tell");
            root.schedule.Execute(() => root.RemoveFromClassList("input-unlock-tell")).StartingIn(800);
        }
    }
}
