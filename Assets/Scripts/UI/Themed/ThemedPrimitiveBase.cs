using System;
using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Abstract base for themed primitive ring; caches <see cref="UiTheme"/> ref in <c>Awake</c> per invariant #3.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined. USS classes consumed by UI Toolkit panels replace this ring.</remarks>
    [Obsolete("ThemedPrimitiveBase quarantined (TECH-32929). Use USS classes / UI Toolkit VisualElement composition instead. Deletion deferred to uGUI purge plan.")]
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
