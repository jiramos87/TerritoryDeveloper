using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Abstract base for themed primitive ring; caches <see cref="UiTheme"/> ref in <c>Awake</c> per invariant #3.</summary>
    public abstract class ThemedPrimitiveBase : MonoBehaviour, IThemed
    {
        [SerializeField] private UiTheme _themeRef;

        /// <summary>Cached <see cref="UiTheme"/> resolved in <c>Awake</c>; null when no theme available.</summary>
        protected UiTheme Theme { get; private set; }

        protected virtual void Awake()
        {
            Theme = _themeRef != null ? _themeRef : FindObjectOfType<UiTheme>();
            if (Theme == null)
            {
                Debug.LogWarning("[ThemedPrimitiveBase] no UiTheme available");
                return;
            }
            ApplyTheme(Theme);
        }

        /// <summary>Override to consume token slugs; default no-op for composite roots that delegate.</summary>
        public virtual void ApplyTheme(UiTheme theme)
        {
        }
    }
}
