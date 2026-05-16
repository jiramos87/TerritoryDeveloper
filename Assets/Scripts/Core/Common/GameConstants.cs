namespace Territory.Core.Common
{
    /// <summary>
    /// Cross-domain shared numeric constants. Centralized here so percent caps,
    /// game-start dates, and similar values are not duplicated as private consts
    /// across managers/services.
    /// Single source of truth for values that have identical semantics in
    /// multiple namespaces. Domain-specific tuning (clamp ranges per signal,
    /// per-zone thresholds, etc.) stays inside its owning class.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>Inclusive upper bound for 0-100 percent sliders / clamps.</summary>
        public const int PercentMax = 100;

        /// <summary>Inclusive upper bound for 0f-100f percent floats (Mathf.Clamp etc.).</summary>
        public const float PercentMaxF = 100f;

        /// <summary>New-game start year (calendar date).</summary>
        public const int StartYear = 2024;

        /// <summary>New-game start month (1-12).</summary>
        public const int StartMonth = 8;

        /// <summary>New-game start day-of-month (1-31).</summary>
        public const int StartDay = 27;
    }
}
