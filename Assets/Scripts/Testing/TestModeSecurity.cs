using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Compile-time and runtime gate for <b>test mode</b> entry (scenario load from CLI).
    /// Release player builds cannot enable test mode unless <c>TERRITORY_ALLOW_TEST_MODE</c> is defined (explicit opt-in).
    /// </summary>
    public static class TestModeSecurity
    {
        /// <summary>
        /// True when test mode CLI flags may be honored (<b>Editor</b>, <b>development build</b>, or scripting define).
        /// </summary>
        public static bool IsTestModeEntryAllowed =>
#if UNITY_EDITOR
            true;
#elif DEVELOPMENT_BUILD
            true;
#elif TERRITORY_ALLOW_TEST_MODE
            true;
#else
            false;
#endif

        /// <summary>
        /// Logs a single warning when CLI requests test mode in a disallowed build.
        /// </summary>
        public static void LogBlockedAttempt(string detail)
        {
            Debug.LogWarning($"[TestMode] Ignored (not allowed in this build): {detail}");
        }
    }
}
