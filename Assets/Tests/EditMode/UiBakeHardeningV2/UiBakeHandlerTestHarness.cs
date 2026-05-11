using Territory.Editor.UiBake;
using UnityEngine;

namespace Territory.Tests.EditMode.UiBakeHardeningV2
{
    /// <summary>
    /// Test harness for TECH-28361 non-empty child assert.
    /// Simulates baking a panel child of a given kind with no valid renderer output,
    /// then calls the empty-child guard to trigger BakeException.
    /// </summary>
    internal static class UiBakeHandlerTestHarness
    {
        /// <summary>
        /// Creates a stub child GameObject (kind=<paramref name="kind"/>), runs no
        /// renderer on it (simulates a plugin returning without attaching components),
        /// then invokes <see cref="BakeEmptyChildGuard.AssertNotEmpty"/> which throws
        /// <see cref="BakeException"/> with message "empty_child:{kind}:{panelSlug}".
        /// </summary>
        public static void BakeStubChildAndAssertEmpty(string kind, string panelSlug)
        {
            var parent = new GameObject($"test-panel-{panelSlug}").transform;
            var childGo = new GameObject($"stub-child", typeof(RectTransform));
            childGo.transform.SetParent(parent, worldPositionStays: false);
            try
            {
                // No renderer fires — child has only RectTransform (no meaningful component).
                BakeEmptyChildGuard.AssertNotEmpty(childGo, kind, panelSlug);
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }
    }
}
