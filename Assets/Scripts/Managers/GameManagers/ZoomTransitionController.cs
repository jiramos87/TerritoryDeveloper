using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Territory.Managers;
using Territory.Persistence;
using Territory.UI.Panels;
using Territory.Services;

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

    /// <summary>CoreScene hub — drives City↔Region zoom transition state machine. Stage 4: real InOutCubic tween + CrossfadeTriggerEvaluator + spinner overlay. Invariant #3: resolve deps in Awake/Start.</summary>
    public class ZoomTransitionController : MonoBehaviour, IZoomTransitionController
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private float cityZoom   = 8f;
        [SerializeField] private float regionZoom = 32f;
        [SerializeField] private float defaultDuration = 1.75f; // adaptive midpoint
        [SerializeField] private float capDuration     = 5f;
        [SerializeField] private float spinnerThreshold = 3f;
        [SerializeField] private UnityEngine.Canvas regionLayerCanvas; // CanvasGroup carrier

        // ── Public API ───────────────────────────────────────────────────────
        public TransitionState State { get; private set; } = TransitionState.Idle;
        public event Action<TransitionState> StateChanged;

        /// <summary>Fired when tween elapsed changes (seconds). TweenSpinnerController subscribes.</summary>
        public event Action<float> TweenElapsed;

        /// <summary>Fired when save fails and transition cancels back to Idle.</summary>
        public event Action OnTransitionCanceledSaveFailed;

        // Auto-confirm gate for test/tracer use (true = skip confirm panel).
        public bool AutoConfirm { get; set; } = false;

        // SaveId used for the current transition's paired write.
        public string CurrentSaveId { get; set; } = "autosave";

        // ── Private ──────────────────────────────────────────────────────────
        private SceneOrchestratorManager _orchestrator;
        private ISaveCoordinator _saveCoordinator;
        private ErrorToastController _errorToast;
        private Camera _cam;
        private CrossfadeTriggerEvaluator _crossfade;
        private CellStreamingPipeline _streamingPipeline;
        private InputLockService _inputLock;
        private Territory.Services.TickClock _tickClock;
        private Territory.Services.IGrowthCatchupRunner _growthCatchupRunner;
        private Territory.RegionScene.Persistence.RegionSaveService _regionSaveService;

        void Awake()
        {
            _cam = Camera.main; // invariant #3 — cache in Awake
        }

        void Start()
        {
            _orchestrator        = FindObjectOfType<SceneOrchestratorManager>();
            _saveCoordinator     = FindObjectOfType<SaveCoordinator>();
            _errorToast          = FindObjectOfType<ErrorToastController>();
            _crossfade           = FindObjectOfType<CrossfadeTriggerEvaluator>();
            _streamingPipeline   = FindObjectOfType<CellStreamingPipeline>();
            _inputLock           = FindObjectOfType<InputLockService>();
            _tickClock           = FindObjectOfType<Territory.Services.TickClock>();
            _regionSaveService   = FindObjectOfType<Territory.RegionScene.Persistence.RegionSaveService>();
            _growthCatchupRunner = new Territory.Services.GrowthCatchupRunner();

            // Wire FirstRingLoaded → InputLockService.Unlock.
            if (_streamingPipeline != null && _inputLock != null)
                _streamingPipeline.FirstRingLoaded += _inputLock.Unlock;
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

            // Stage 4: real InOutCubic tween. City sprites stay live (no cullingMask change).
            // Stage 7: symmetric — City→Region zooms OUT; Region→City zooms IN.
            SetState(TransitionState.TweeningOut);
            _crossfade?.ResetForNewTransition();

            // Pause TickClock during transition.
            _tickClock?.Pause();

            float tweenDuration = Mathf.Clamp(defaultDuration, 1.5f, capDuration);
            float startSize     = _cam != null ? _cam.orthographicSize : (target == IsoSceneContext.Region ? cityZoom : regionZoom);
            float endSize       = target == IsoSceneContext.Region ? regionZoom : cityZoom;
            float elapsed       = 0f;
            bool  capHit        = false;

            while (elapsed < tweenDuration)
            {
                if (ct.IsCancellationRequested) { _tickClock?.Resume(); SetState(TransitionState.Idle); return TransitionResult.Cancelled; }

                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / tweenDuration);
                float eased = EaseInOutCubic(t);

                if (_cam != null)
                    _cam.orthographicSize = Mathf.LerpUnclamped(startSize, endSize, eased);

                // Raise elapsed event for spinner controller.
                TweenElapsed?.Invoke(elapsed);

                // 5s hard cap — abort transition if AwaitLoad not resolved.
                if (elapsed >= capDuration)
                {
                    capHit = true;
                    break;
                }

                // Profiler escape-hatch marker: city cullingMask is NOT modified — sprites remain live (Approach C).
                // Use Profiler.BeginSample/EndSample in Editor-only profiler sessions to measure draw cost.
                UnityEngine.Profiling.Profiler.BeginSample("ZoomTransitionController.TweeningOut");
                UnityEngine.Profiling.Profiler.EndSample();

                // Geometric crossfade trigger — fires region alpha once footprint ⊆ anchor.
                _crossfade?.EvaluateFrame(_cam);

                await Task.Yield();
            }

            if (capHit)
            {
                Debug.LogWarning("[ZoomTransitionController] 5s cap hit — aborting transition.");
                _errorToast?.Show(ToastKind.LoadFailed);
                _tickClock?.Resume();
                SetState(TransitionState.Idle);
                return TransitionResult.Failed;
            }

            // Snap to target zoom on completion.
            if (_cam != null) _cam.orthographicSize = endSize;

            SetState(TransitionState.AwaitLoad);
            string targetScene = target == IsoSceneContext.Region ? "RegionScene" : "CityScene";
            if (_orchestrator != null)
            {
                AsyncOperation op;
                try
                {
                    op = await _orchestrator.LoadAdditive(targetScene, ct);
                }
                catch (Exception loadEx)
                {
                    Debug.LogWarning($"[ZoomTransitionController] LoadAdditive threw: {loadEx.Message}");
                    _errorToast?.Show(ToastKind.LoadFailed);
                    _tickClock?.Resume();
                    SetState(TransitionState.Idle);
                    return TransitionResult.Failed;
                }

                if (op == null)
                {
                    // Scene not in build settings — show error, revert.
                    _errorToast?.Show(ToastKind.LoadFailed);
                    _tickClock?.Resume();
                    SetState(TransitionState.Idle);
                    return TransitionResult.Failed;
                }

                if (ct.IsCancellationRequested) { _tickClock?.Resume(); SetState(TransitionState.Idle); return TransitionResult.Cancelled; }
                await _orchestrator.Activate(op);
            }
            else
            {
                await Task.Yield();
            }

            SetState(TransitionState.Landing);

            // Stage 7: on landing, run growth catch-up then resume TickClock.
            if (target == IsoSceneContext.City && _growthCatchupRunner != null && _regionSaveService != null && _tickClock != null)
            {
                long loadedTick = _regionSaveService.LoadedLastTouchedTicks;
                uint seed       = _regionSaveService.LoadedGrowthSeed;
                long elapsed    = _tickClock.CurrentTick - loadedTick;
                if (elapsed > 0)
                {
                    var dormant  = new Territory.Domain.Growth.WorldSnapshot(null, loadedTick, seed);
                    _growthCatchupRunner.Catchup(dormant, elapsed); // result discarded — sim owns pop state
                }
            }

            _tickClock?.Resume();
            await Task.Yield(); // final yield for Landing frame

            SetState(TransitionState.Idle);
            return TransitionResult.Success;
        }

        // ── Easing ───────────────────────────────────────────────────────────

        /// <summary>Cubic ease-in-out: t&lt;0.5 → 4t³; t≥0.5 → 1 − pow(−2t+2,3)/2.</summary>
        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        private void SetState(TransitionState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
