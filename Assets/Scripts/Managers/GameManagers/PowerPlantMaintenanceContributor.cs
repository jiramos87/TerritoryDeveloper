using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Adapter wrapping the legacy power-plant maintenance formula as an
    /// <see cref="IMaintenanceContributor"/>. Returns the same cost as the prior
    /// <c>ComputeMonthlyUtilityMaintenanceCost</c> (plantCount × ratePerPlant).
    /// General-pool contributor (<see cref="GetSubTypeId"/> returns -1).
    /// Registered from <see cref="EconomyManager.Start"/> after cityStats resolves.
    /// </summary>
    public class PowerPlantMaintenanceContributor : MonoBehaviour, IMaintenanceContributor
    {
        [SerializeField] private CityStats cityStats;
        [SerializeField] private EconomyManager economy;

        private void Awake()
        {
            if (cityStats == null)
                cityStats = FindObjectOfType<CityStats>();
            if (economy == null)
                economy = FindObjectOfType<EconomyManager>();
        }

        private void Start()
        {
            if (economy != null)
                economy.RegisterMaintenanceContributor(this);
        }

        private void OnDestroy()
        {
            if (economy != null)
                economy.UnregisterMaintenanceContributor(this);
        }

        /// <inheritdoc/>
        public int GetMonthlyMaintenance()
        {
            if (cityStats == null || economy == null)
                return 0;
            int rate = economy.maintenanceCostPerPowerPlant;
            if (rate <= 0)
                return 0;
            return Mathf.Max(0, cityStats.GetRegisteredPowerPlantCount()) * rate;
        }

        /// <inheritdoc/>
        public string GetContributorId() => "power-aggregate";

        /// <inheritdoc/>
        public int GetSubTypeId() => -1;
    }
}
