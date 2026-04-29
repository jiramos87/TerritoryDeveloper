using Territory.UI.Themed;
using UnityEngine;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Abstract render-layer companion for <see cref="StudioControlBase"/>; subscribes to <see cref="StudioControlBase.OnStateChanged"/> in <c>Awake</c> + invokes <see cref="OnStateApplied"/> on each event. Cache pattern matches invariant #3 (no per-frame <c>FindObjectOfType</c>).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StudioControlBase))]
    public abstract class StudioControlRendererBase : MonoBehaviour
    {
        [SerializeField] private UiTheme _themeRef;

        /// <summary>Cached <see cref="UiTheme"/> resolved in <c>Awake</c>; null when no theme available.</summary>
        protected UiTheme Theme { get; private set; }

        /// <summary>Cached sibling <see cref="CanvasRenderer"/>; optional (null on parents without one).</summary>
        protected CanvasRenderer CanvasRenderer { get; private set; }

        /// <summary>Cached sibling <see cref="StudioControlBase"/>.</summary>
        protected StudioControlBase Control { get; private set; }

        protected virtual void Awake()
        {
            Control = GetComponent<StudioControlBase>();
            CanvasRenderer = GetComponent<CanvasRenderer>();
            Theme = _themeRef != null ? _themeRef : FindObjectOfType<UiTheme>();
            if (Control == null)
            {
                Debug.LogWarning($"[StudioControlRendererBase] sibling StudioControlBase missing (gameObject={name})");
                return;
            }
            Control.OnStateChanged += OnStateAppliedHandler;
            // Initial sync — bake-time detail may already be applied before renderer Awake runs.
            OnStateApplied();
        }

        protected virtual void OnDestroy()
        {
            if (Control != null)
            {
                Control.OnStateChanged -= OnStateAppliedHandler;
            }
        }

        private void OnStateAppliedHandler()
        {
            OnStateApplied();
        }

        /// <summary>Invoked once per <see cref="StudioControlBase.ApplyDetail"/> call (plus once at Awake for initial sync). Subclasses re-read sibling state and write into render targets.</summary>
        protected abstract void OnStateApplied();
    }
}
