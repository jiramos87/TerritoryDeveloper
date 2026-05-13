using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Territory.UI.Themed
{
    /// <summary>Stage 13.4 (TECH-9866) — bake-time interactive tab cell.
    /// Carries serialized cell index + cached parent <see cref="ThemedTabBar"/>; on pointer click
    /// invokes <see cref="ThemedTabBar.SetActiveTab"/>. Bake handler attaches one instance per
    /// `panel.tabs[]` entry under the tab bar root (Invariant #6 — no runtime AddComponent).</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined alongside ThemedTabBar; UXML tab-strip replaces.</remarks>
    [Obsolete("ThemedTabCell quarantined (TECH-32929). Use UXML tab-strip pattern. Deletion deferred to uGUI purge plan.")]
    [RequireComponent(typeof(RectTransform))]
    public class ThemedTabCell : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Bake-time per-tab index; used as the SetActiveTab argument.</summary>
        [SerializeField] private int _index;

        /// <summary>Optional bake-time parent ref. When null, falls back to GetComponentInParent at Awake.</summary>
        [SerializeField] private ThemedTabBar _parentTabBar;

        public int Index => _index;
        public ThemedTabBar ParentTabBar => _parentTabBar;

        private void Awake()
        {
            if (_parentTabBar == null)
            {
                _parentTabBar = GetComponentInParent<ThemedTabBar>();
                if (_parentTabBar == null)
                {
                    Debug.LogWarning(
                        $"[ThemedTabCell] {name}: no ThemedTabBar ancestor — clicks will be no-op. " +
                        "Bake handler should set _parentTabBar at emit time or wire under a tab bar root.");
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_parentTabBar == null) return;
            _parentTabBar.SetActiveTab(_index);
        }
    }
}
