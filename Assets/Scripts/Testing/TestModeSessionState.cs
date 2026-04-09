namespace Territory.Testing
{
    /// <summary>
    /// Session flags after successful <b>test mode</b> CLI parsing (before <see cref="Territory.Persistence.GameStartInfo"/> is consumed).
    /// Used for the on-screen <b>TEST-MODE</b> indicator and diagnostics.
    /// </summary>
    public static class TestModeSessionState
    {
        /// <summary>True when this process launched with a resolved scenario load via test mode.</summary>
        public static bool ActiveThisSession { get; internal set; }

        /// <summary>Resolved absolute path to the <c>GameSaveData</c> JSON file.</summary>
        public static string ResolvedScenarioPath { get; internal set; }

        /// <summary><b>Scenario id</b> from <c>-testScenarioId</c> when used; otherwise null.</summary>
        public static string ScenarioId { get; internal set; }
    }
}
