namespace Territory.Economy
{
    /// <summary>
    /// Contract for participants in the monthly maintenance registry.
    /// <see cref="EconomyManager.RegisterMaintenanceContributor"/> adds contributors;
    /// <see cref="EconomyManager.ProcessMonthlyMaintenance"/> iterates them sorted by
    /// <see cref="GetContributorId"/> (ordinal) for deterministic save-replay parity.
    /// </summary>
    public interface IMaintenanceContributor
    {
        /// <summary>
        /// Monthly upkeep cost this contributor requests.
        /// Returning 0 is a valid no-op (e.g. road adapter with zero road cells).
        /// </summary>
        int GetMonthlyMaintenance();

        /// <summary>
        /// Stable, unique string id used for deterministic iteration order
        /// (ordinal sort in <see cref="EconomyManager.ProcessMonthlyMaintenance"/>).
        /// </summary>
        string GetContributorId();

        /// <summary>
        /// Sub-type id for envelope routing.
        /// Return -1 for general-pool contributors (road, power plant);
        /// return 0..6 for Zone S sub-type contributors (draws from
        /// <see cref="BudgetAllocationService.TryDraw"/>).
        /// </summary>
        int GetSubTypeId();
    }
}
