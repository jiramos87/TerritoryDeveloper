using UnityEngine;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Abstract foundation for JuiceLayer behaviors (Stage 5 of Game UI Design System).
    /// Caches the scene <see cref="UiTheme"/> reference once in <c>Awake</c> per invariant #3
    /// (no <c>FindObjectOfType</c> per Update / LateUpdate / FixedUpdate). Subclasses resolve
    /// per-frame motion via <see cref="TryEvaluateCurve"/> using the cached dict.
    /// </summary>
    public abstract class JuiceBase : MonoBehaviour
    {
        /// <summary>Inspector-assignable theme override; falls back to scene <see cref="UiTheme"/> when null.</summary>
        [SerializeField] protected UiTheme themeRef;

        /// <summary>Slug used by <see cref="TryEvaluateCurve"/> to fetch motion-curve spec from theme.</summary>
        [SerializeField] protected string curveSlug;

        /// <summary>Cached <see cref="UiTheme"/> resolved in <c>Awake</c>; null when no theme available.</summary>
        protected UiTheme theme;

        /// <summary>Per-instance memo of last successfully resolved motion-curve spec (avoids dict lookup per frame).</summary>
        private UiTheme.MotionCurveSpec _cachedSpec;
        private string _cachedSlug;
        private bool _cachedHit;

        /// <summary>Invariant #3 + #4: cache the theme reference once; subclasses extend via <c>base.Awake()</c>.</summary>
        protected virtual void Awake()
        {
            theme = themeRef != null ? themeRef : FindObjectOfType<UiTheme>();
            if (theme == null)
            {
                Debug.LogWarning($"[JuiceBase] no UiTheme available (component={GetType().Name})");
            }
        }

        /// <summary>Editor-only: drop the per-instance memo so a slug change in the Inspector takes effect on next call.</summary>
        protected virtual void OnValidate()
        {
            _cachedSlug = null;
            _cachedHit = false;
        }

        /// <summary>
        /// Resolve the configured motion-curve spec from the cached <see cref="UiTheme"/>. Returns
        /// <c>false</c> when theme or slug unavailable. Subsequent calls with the same slug reuse
        /// the per-instance memo; slug changes invalidate via <see cref="OnValidate"/>.
        /// </summary>
        protected bool TryEvaluateCurve(out UiTheme.MotionCurveSpec spec)
        {
            if (theme == null || string.IsNullOrEmpty(curveSlug))
            {
                spec = default;
                return false;
            }
            if (_cachedHit && _cachedSlug == curveSlug)
            {
                spec = _cachedSpec;
                return true;
            }
            if (theme.TryGetMotionCurve(curveSlug, out spec))
            {
                _cachedSpec = spec;
                _cachedSlug = curveSlug;
                _cachedHit = true;
                return true;
            }
            return false;
        }

        /// <summary>Subclass hook for swap-in of an alternate slug at runtime; clears the memo.</summary>
        public void SetCurveSlug(string slug)
        {
            curveSlug = slug;
            _cachedSlug = null;
            _cachedHit = false;
        }
    }
}
