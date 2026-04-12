namespace Territory.Testing
{
    /// <summary>
    /// Session flags after <b>test mode</b> CLI parsing (before <see cref="Territory.Persistence.GameStartInfo"/> consumed).
    /// Used for on-screen <b>TEST-MODE</b> indicator + diagnostics.
    /// </summary>
    public static class TestModeSessionState
    {
        /// <summary>True when process launched with resolved scenario load via test mode.</summary>
        public static bool ActiveThisSession { get; internal set; }

        /// <summary>Resolved absolute path to <c>GameSaveData</c> JSON file.</summary>
        public static string ResolvedScenarioPath { get; internal set; }

        /// <summary><b>Scenario id</b> from <c>-testScenarioId</c> when used; else null.</summary>
        public static string ScenarioId { get; internal set; }
    }
}
