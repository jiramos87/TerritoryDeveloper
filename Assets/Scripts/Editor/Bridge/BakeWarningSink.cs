using System.Collections.Generic;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Call-scoped bake warning collector. Extracted from UiBakeHandler (TECH-31986).
    /// Install a collector before bake body runs; helpers call Append; clear on exit.</summary>
    public static class BakeWarningSink
    {
        /// <summary>Active warning list for the current bake call. Null when no bake active.</summary>
        public static List<Territory.Editor.Bridge.UiBakeHandler.BakeError> Current { get; private set; }

        /// <summary>Install a new warning list for the duration of a bake call.</summary>
        public static void Install(List<Territory.Editor.Bridge.UiBakeHandler.BakeError> sink)
        {
            Current = sink;
        }

        /// <summary>Clear the active warning list (call in finally after bake completes).</summary>
        public static void Clear()
        {
            Current = null;
        }

        /// <summary>Append a warning. Always logs to Debug.LogWarning regardless of collector state.</summary>
        public static void Append(string error, string details, string path)
        {
            Debug.LogWarning($"[UiBakeHandler] {error}: {details} @ {path}");
            Current?.Add(new Territory.Editor.Bridge.UiBakeHandler.BakeError
            {
                error = error,
                details = details,
                path = path,
            });
        }
    }
}
