using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Unity 2022.3 compatibility shim — the runtime dataSource API ships in 2023.x. Hosts call
    /// <see cref="SetCompatDataSource"/> instead of <c>rootVisualElement.dataSource = vm</c>; on this
    /// Unity version the call is a no-op and Hosts must wire labels/buttons via manual <c>Q&lt;&gt;</c>
    /// queries. When the project lands on 2023+, swap the body for the native dataSource set.
    /// </summary>
    internal static class VisualElementBindingCompat
    {
        public static void SetCompatDataSource(this VisualElement element, object source)
        {
            // No-op on Unity 2022.3 — see class summary.
        }
    }
}
