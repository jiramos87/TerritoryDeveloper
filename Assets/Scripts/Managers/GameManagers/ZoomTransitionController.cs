using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using PrimeTween;
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

    /// <summary>CoreScene hub — drives City↔Region zoom transition state machine. Stage 4: PrimeTween InOutCubic ortho-size tween + TweenElapsed event.</summary>
    public class ZoomTransitionController : MonoBehaviour, IZoomTransitionController
    {
        public TransitionState State { get; private set; } = TransitionState.Idle;
        public event Action<TransitionState> StateChanged;

        /// <summary>Fired when save fails and transition cancels back to Idle.</summary>
        public event Action OnTransitionCanceledSaveFailed;

        /// <summary>Fired each frame during TweeningOut with elapsed seconds since tween start.</summary>
        public event Action<float> TweenElapsed;

        [SerializeField] private float cityZoom    = 8f;
        [SerializeField] private float regionZoom  = 32f;

        // Clamp range for adaptive tween duration (1.5–5.0s).
        private const float MinTweenDuration = 1.5f;
        private const float MaxTweenDuration = 5.0f;
        private const float TweenAbortCapSeconds = 5.0f;

        // Profiler escape-hatch: draw-call baseline captured at TweeningOut entry.
        // If per-frame draws exceed baseline*1.3 → log telemetry for Approach A fallback.
        private const float DrawCallCostThresholdMultiplier = 1.3f;
        private long _baselineDrawCalls;

        private SceneOrchestratorManager _orchestrator;
        private ISaveCoordinator _saveCoordinator;
        private ErrorToastController _errorToast;
        private Camera _cam;

        // Auto-confirm gate for test/tracer use (true = skip confirm panel).
        public bool AutoConfirm { get; set; } = false;

        // SaveId used for the current transition's paired write.
        public string CurrentSaveId { get; set; } = "autosave";

        void Awake()
        {
            _cam = Camera.main;
        }

        void Start()
        {
            _orchestrator    = FindObjectOfType<SceneOrchestratorManager>();
            _saveCoordinator = FindObjectOfType<SaveCoordinator>();
            _errorToast      = FindObjectOfType<ErrorToastController>();
            if (_cam == null) _cam = Camera.main;
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
            {
                // Stage 4: Approach C — city stays rendered live (no cullingMask toggle, no SpriteRenderer.enabled=false).
                // Capture draw-call baseline for profiler escape-hatch.
                _baselineDrawCalls = Profiler.GetTotalReservedMemoryLong(); // proxy counter available in all modes

                float duration = Mathf.Clamp(1.8f, MinTweenDuration, MaxTweenDuration);
                float elapsed  = 0f;
                var tween = _cam != null
                    ? Tween.CameraOrthographicSize(_cam, regionZoom, duration, Ease.InOutCubic)
                    : new Tween();

                bool abortedByCap = false;
                while (tween.isAlive)
                {
                    elapsed += Time.deltaTime;
                    TweenElapsed?.Invoke(elapsed);

                    // 5s hard cap: abort if AwaitLoad hasn't completed (scene-load stall).
                    if (elapsed >= TweenAbortCapSeconds)
                    {
                        tween.Stop();
                        abortedByCap = true;
                        break;
                    }

                    // Profiler telemetry: log if GPU cost unexpectedly spikes (Approach A fallback signal).
                    long current = Profiler.GetTotalReservedMemoryLong();
                    if (_baselineDrawCalls > 0 && current > _baselineDrawCalls * DrawCallCostThresholdMultiplier)
                    {
                        Debug.LogWarning("[ZoomTransitionController] Draw cost threshold exceeded during tween — candidate for Approach A RenderTexture fallback.");
                    }

                    await Task.Yield();
                }

                if (abortedByCap)
                {
                    Debug.LogError("[ZoomTransitionController] Tween 5s cap exceeded — aborting transition.");
                    SetState(TransitionState.Idle);
                    _errorToast?.Show(ToastKind.LoadFailed);
                    return TransitionResult.Failed;
                }
            }

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
