using System;
using UnityEngine;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Abstract base for renderer-layer companions of <see cref="ThemedPrimitiveBase"/> primitives;
    /// caches <see cref="UiTheme"/> ref + child render targets in <c>Awake</c> per invariant #3, and
    /// exposes the <see cref="OnStateApplied"/> hook for state-holder change notifications.
    /// Bake-time-attached only (Stage 9 lock — no runtime <c>AddComponent</c>).
    /// </summary>
    /// <remarks>
    /// State-holder communication: a sibling primitive fires a C# event; subclasses subscribe in
    /// <see cref="OnEnable"/> and unsubscribe in <see cref="OnDisable"/>. The default subscription
    /// is no-op; subclasses opt in by overriding <see cref="OnEnable"/> / <see cref="OnDisable"/>.
    /// TECH-32929 Stage 6.0 — Quarantined alongside ThemedPrimitiveBase ring.
    /// </remarks>
    [Obsolete("ThemedPrimitiveRendererBase quarantined (TECH-32929). Use USS classes / UI Toolkit. Deletion deferred to uGUI purge plan.")]
    public abstract class ThemedPrimitiveRendererBase : ThemedPrimitiveBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected virtual void OnEnable()
        {
            // Subclasses subscribe to state-holder events here.
        }

        protected virtual void OnDisable()
        {
            // Subclasses unsubscribe to mirror OnEnable.
        }

        /// <summary>Invoked when the sibling state holder reports a state change.</summary>
        protected abstract void OnStateApplied();
    }
}
