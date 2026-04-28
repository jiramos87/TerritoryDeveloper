using Territory.UI.Themed;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Abstract base for StudioControl interactive ring; mirrors <see cref="ThemedPrimitiveBase"/> theme cache pattern (invariant #3).</summary>
    public abstract class StudioControlBase : MonoBehaviour, IStudioControl, IThemed
    {
        [SerializeField] private string _slug;
        [SerializeField] private UiTheme _themeRef;

        /// <summary>Cached <see cref="UiTheme"/> resolved in <c>Awake</c>; null when no theme available.</summary>
        protected UiTheme Theme { get; private set; }

        /// <inheritdoc />
        public abstract string Kind { get; }

        /// <inheritdoc />
        public string Slug => _slug;

        protected virtual void Awake()
        {
            Theme = _themeRef != null ? _themeRef : FindObjectOfType<UiTheme>();
            if (Theme == null)
            {
                Debug.LogWarning($"[StudioControlBase] no UiTheme available (slug={_slug}, kind={Kind})");
                return;
            }
            ApplyTheme(Theme);
        }

        /// <summary>Override to consume detail row baked from IR; default no-op.</summary>
        public virtual void ApplyDetail(IDetailRow detail)
        {
        }

        /// <summary>Override to consume token slugs from <paramref name="theme"/>; default no-op.</summary>
        public virtual void ApplyTheme(UiTheme theme)
        {
        }
    }
}
