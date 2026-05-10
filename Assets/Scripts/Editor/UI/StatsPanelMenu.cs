#if UNITY_EDITOR
using Territory.UI.Registry;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor.UI
{
    /// <summary>
    /// Wave B2 (TECH-27086) — Editor menu for stats-panel closed-loop testing.
    /// Territory > UI > Open Stats → dispatches stats.open action until 9.15 hud-bar-stats-button lands.
    /// </summary>
    public static class StatsPanelMenu
    {
        [MenuItem("Territory/UI/Open Stats")]
        public static void OpenStats()
        {
            var registry = Object.FindObjectOfType<UiActionRegistry>();
            if (registry == null)
            {
                Debug.LogWarning("[StatsPanelMenu] UiActionRegistry not found in scene. Enter Play Mode first.");
                return;
            }
            bool dispatched = registry.Dispatch("stats.open", null);
            if (!dispatched)
                Debug.LogWarning("[StatsPanelMenu] stats.open handler not registered. Ensure StatsPanelAdapter is mounted and Start() has run.");
            else
                Debug.Log("[StatsPanelMenu] stats.open dispatched.");
        }

        [MenuItem("Territory/UI/Open Stats", validate = true)]
        public static bool OpenStatsValidate()
        {
            // Available in Play Mode only.
            return Application.isPlaying;
        }
    }
}
#endif
