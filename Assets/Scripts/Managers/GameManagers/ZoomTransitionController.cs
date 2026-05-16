using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Territory.SceneManagement
{
    /// <summary>Transition result discriminator.</summary>
    public enum TransitionResult { Success, Cancelled, Failed }

    /// <summary>State machine states for the zoom transition lifecycle.</summary>
    public enum TransitionState
    {
        Idle,
        AwaitConfirm,
        Saving,
        TweeningOut,
        AwaitLoad,
        Landing
    }

    /// <summary>Interface for ZoomTransitionController — allows test injection.</summary>
    public interface IZoomTransitionController
    {
        TransitionState State { get; }
        event Action<TransitionState> StateChanged;
        Task<TransitionResult> RequestTransition(IsoSceneContext target, CancellationToken ct);
    }

    /// <summary>CoreScene hub — drives City↔Region zoom transition state machine. Placeholder tween = instant cut (Stage 4 lands PrimeTween). Invariant #3: resolve deps in Start.</summary>
    public class ZoomTransitionController : MonoBehaviour, IZoomTransitionController
    {
        public TransitionState State { get; private set; } = TransitionState.Idle;
        public event Action<TransitionState> StateChanged;

        private SceneOrchestratorManager _orchestrator;

        // Optional: auto-confirm gate for test/tracer use (true = skip confirm panel)
        public bool AutoConfirm { get; set; } = false;

        void Start()
        {
            _orchestrator = FindObjectOfType<SceneOrchestratorManager>();
        }

        /// <summary>Request a scene transition to <paramref name="target"/>. State machine: Idle→AwaitConfirm→Saving→TweeningOut→AwaitLoad→Landing→Idle.</summary>
        public async Task<TransitionResult> RequestTransition(IsoSceneContext target, CancellationToken ct)
        {
            if (State != TransitionState.Idle)
            {
                Debug.LogWarning("[ZoomTransitionController] Transition already in progress.");
                return TransitionResult.Failed;
            }

            SetState(TransitionState.AwaitConfirm);
            if (!AutoConfirm)
            {
                // Real path: ConfirmTransitionPanelController drives AutoConfirm=true then re-calls.
                // For tracer: caller sets AutoConfirm=true before RequestTransition.
                return TransitionResult.Cancelled;
            }

            SetState(TransitionState.Saving);
            await Task.Yield(); // placeholder — SaveCoordinator lands Stage 2

            SetState(TransitionState.TweeningOut);
            await Task.Yield(); // placeholder — PrimeTween lands Stage 4

            SetState(TransitionState.AwaitLoad);
            // Load target scene additive via orchestrator
            string targetScene = target == IsoSceneContext.Region ? "RegionScene" : "CityScene";
            if (_orchestrator != null)
            {
                var op = await _orchestrator.LoadAdditive(targetScene, ct);
                if (ct.IsCancellationRequested) { SetState(TransitionState.Idle); return TransitionResult.Cancelled; }
                await _orchestrator.Activate(op);
            }
            else
            {
                await Task.Yield();
            }

            SetState(TransitionState.Landing);
            await Task.Yield(); // placeholder — camera center-on-anchor lands Stage 7

            SetState(TransitionState.Idle);
            return TransitionResult.Success;
        }

        private void SetState(TransitionState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
