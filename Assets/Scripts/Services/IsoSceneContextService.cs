using System;
using UnityEngine;
using Territory.SceneManagement;

namespace Territory.Services
{
    /// <summary>CoreScene service — tracks active scene context (City / Region / Transition). Subscribes to ZoomTransitionController state machine.</summary>
    public class IsoSceneContextService : MonoBehaviour
    {
        public enum SceneContext { City, Region, Transition }

        /// <summary>Current context. City on boot.</summary>
        public SceneContext Context { get; private set; } = SceneContext.City;

        /// <summary>Fired whenever Context changes.</summary>
        public event Action<SceneContext> ContextChanged;

        private ZoomTransitionController _transitionController;

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
            if (_transitionController != null)
                _transitionController.StateChanged += OnTransitionStateChanged;
        }

        void OnDestroy()
        {
            if (_transitionController != null)
                _transitionController.StateChanged -= OnTransitionStateChanged;
        }

        private void OnTransitionStateChanged(TransitionState state)
        {
            SceneContext next = Context;
            switch (state)
            {
                case TransitionState.TweeningOut:
                    next = SceneContext.Transition;
                    break;
                case TransitionState.Idle when Context == SceneContext.Transition:
                    // Landing completed — resolve target from orchestrator (approximation: toggle).
                    next = SceneContext.Region;
                    break;
                default:
                    return;
            }
            SetContext(next);
        }

        /// <summary>Explicit override — use when orchestrator knows the landed scene.</summary>
        public void SetContext(SceneContext ctx)
        {
            if (Context == ctx) return;
            Context = ctx;
            ContextChanged?.Invoke(ctx);
        }
    }
}
