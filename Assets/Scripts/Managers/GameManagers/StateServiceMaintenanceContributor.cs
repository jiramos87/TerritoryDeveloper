using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Per-building maintenance contributor for Zone S buildings.
    /// Implements <see cref="IMaintenanceContributor"/> with upkeep from
    /// <see cref="ZoneSubTypeRegistry"/> and stable contributor id
    /// <c>s-{subTypeId}-{instanceId}</c>.
    /// Registers with <see cref="EconomyManager"/> on <see cref="Start"/>;
    /// unregisters on <see cref="OnDestroy"/>.
    /// </summary>
    public class StateServiceMaintenanceContributor : MonoBehaviour, IMaintenanceContributor
    {
        [SerializeField] private int subTypeId = -1;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private ZoneSubTypeRegistry registry;

        /// <summary>Sub-type id this contributor draws from. Set by spawn hook.</summary>
        public int ConfiguredSubTypeId { get => subTypeId; set => subTypeId = value; }

        private void Awake()
        {
            if (economyManager == null)
                economyManager = FindObjectOfType<EconomyManager>();
            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
        }

        private void Start()
        {
            if (economyManager != null)
                economyManager.RegisterMaintenanceContributor(this);
        }

        private void OnDestroy()
        {
            if (economyManager != null)
                economyManager.UnregisterMaintenanceContributor(this);
        }

        /// <inheritdoc/>
        public int GetMonthlyMaintenance()
        {
            if (registry == null || subTypeId < 0)
                return 0;

            var entry = registry.GetById(subTypeId);
            return entry != null ? entry.monthlyUpkeep : 0;
        }

        /// <inheritdoc/>
        public string GetContributorId() => $"s-{subTypeId}-{GetInstanceID()}";

        /// <inheritdoc/>
        public int GetSubTypeId() => subTypeId;
    }
}
