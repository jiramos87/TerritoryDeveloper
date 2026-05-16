using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Territory.Managers;
using Territory.Persistence;
using Territory.UI.Panels;

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
        event Action OnTransitionCanceledSaveFailed;
        Task<TransitionResult> RequestTransition(IsoSceneContext target, CancellationToken ct);
    }

    /// <summary>CoreScene hub — drives City↔Region zoom transition state machine. Stage 2: SaveCoordinator wired in Saving state. Invariant #3: resolve deps in Start.</summary>
    public class ZoomTransitionController : MonoBehaviour, IZoomTransitionController
    {
        public TransitionState State { get; private set; } = TransitionState.Idle;
        public event Action<TransitionState> StateChanged;

        /// <summary>Fired when save fails and transition cancels back to Idle.</summary>
        public event Action OnTransitionCanceledSaveFailed;

        private SceneOrchestratorManager _orchestrator;
        private ISaveCoordinator _saveCoordinator;
        private ErrorToastController _errorToast;

        // Auto-confirm gate for test/tracer use (true = skip confirm panel).
        public bool AutoConfirm { get; set; } = false;

        // SaveId used for the current transition's paired write.
        public string CurrentSaveId { get; set; } = "autosave";

        void Start()
        {
            _orchestrator    = FindObjectOfType<SceneOrchestratorManager>();
            _saveCoordinator = FindObjectOfType<SaveCoordinator>();
            _errorToast      = FindObjectOfType<ErrorToastController>();
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
                return TransitionResult.Cancelled;
            }

            SetState(TransitionState.Saving);

            // Stage 2: invoke SaveCoordinator.SavePair before advancing to TweeningOut.
            if (_saveCoordinator != null)
            {
                try
                {
                    await _saveCoordinator.SavePair(CurrentSaveId, IsoSceneContext.City, ct);
                }
                catch (SaveFailedException ex)
                {
                    Debug.LogWarning($"[ZoomTransitionController] SavePair failed: {ex.Message}");
                    SetState(TransitionState.Idle);
                    _errorToast?.Show(ToastKind.SaveFailed);
                    OnTransitionCanceledSaveFailed?.Invoke();
                    return TransitionResult.Failed;
                }
                catch (OperationCanceledException)
                {
                    SetState(TransitionState.Idle);
                    return TransitionResult.Cancelled;
                }
            }
            else
            {
                await Task.Yield();
            }

            SetState(TransitionState.TweeningOut);
            await Task.Yield(); // placeholder — PrimeTween lands Stage 4

            SetState(TransitionState.AwaitLoad);
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
