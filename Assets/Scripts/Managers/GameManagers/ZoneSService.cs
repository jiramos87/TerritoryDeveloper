using UnityEngine;
using Territory.Core;
using Territory.Zones;
using Territory.UI;

namespace Territory.Economy
{
    /// <summary>
    /// Single entry point for Zone S (state-service) placement.
    /// Wraps envelope draw via <see cref="BudgetAllocationService.TryDraw"/>,
    /// zone placement via <see cref="ZoneManager"/>, and maintenance contributor
    /// registration via <see cref="EconomyManager.RegisterMaintenanceContributor"/>.
    /// Extracted per invariant #6 (no new GridManager responsibilities).
    /// </summary>
    public class ZoneSService : MonoBehaviour
    {
        [SerializeField] private BudgetAllocationService budgetAllocation;
        [SerializeField] private TreasuryFloorClampService treasuryFloorClamp;
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private ZoneManager zoneManager;
        [SerializeField] private GridManager grid;

        private void Awake()
        {
            if (budgetAllocation == null)
                budgetAllocation = FindObjectOfType<BudgetAllocationService>();
            if (budgetAllocation == null)
                Debug.LogWarning("ZoneSService: BudgetAllocationService not found. Assign via Inspector.");

            if (treasuryFloorClamp == null)
                treasuryFloorClamp = FindObjectOfType<TreasuryFloorClampService>();
            if (treasuryFloorClamp == null)
                Debug.LogWarning("ZoneSService: TreasuryFloorClampService not found. Assign via Inspector.");

            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
            if (registry == null)
                Debug.LogWarning("ZoneSService: ZoneSubTypeRegistry not found. Assign via Inspector.");

            if (zoneManager == null)
                zoneManager = FindObjectOfType<ZoneManager>();
            if (zoneManager == null)
                Debug.LogWarning("ZoneSService: ZoneManager not found. Assign via Inspector.");

            if (grid == null)
                grid = FindObjectOfType<GridManager>();
            if (grid == null)
                Debug.LogWarning("ZoneSService: GridManager not found. Assign via Inspector.");
        }

        /// <summary>
        /// Place a state-service zone at the given cell coordinates.
        /// Checks registry, envelope budget, and treasury floor before placement.
        /// </summary>
        /// <param name="cellX">Grid X coordinate.</param>
        /// <param name="cellY">Grid Y coordinate.</param>
        /// <param name="subTypeId">Sub-type id indexing <see cref="ZoneSubTypeRegistry"/>.</param>
        /// <returns>True if placement succeeded; false if blocked by budget, registry, or invalid cell.</returns>
        public bool PlaceStateServiceZone(int cellX, int cellY, int subTypeId)
        {
            var entry = registry != null ? registry.GetById(subTypeId) : null;
            if (entry == null)
            {
                GameNotificationManager.Instance?.PostError(
                    $"Zone S placement failed: unknown sub-type id {subTypeId}.");
                return false;
            }

            CityCell cell = grid != null ? grid.GetCell(cellX, cellY) : null;
            if (cell == null)
                return false;

            if (budgetAllocation == null || !budgetAllocation.TryDraw(subTypeId, entry.baseCost))
            {
                GameNotificationManager.Instance?.PostNotification(
                    $"{entry.displayName} envelope exhausted",
                    GameNotificationManager.NotificationType.Error,
                    3f);
                return false;
            }

            if (zoneManager == null)
                return false;

            if (!zoneManager.PlaceStateServiceZoneAt(
                cellX, cellY, Zone.ZoneType.StateServiceLightZoning, subTypeId))
                return false;

            AttachMaintenanceContributor(cellX, cellY, subTypeId);
            return true;
        }
        /// <summary>
        /// Attach <see cref="StateServiceMaintenanceContributor"/> to the cell GO
        /// so monthly maintenance draws from the correct sub-type envelope.
        /// The contributor's <see cref="StateServiceMaintenanceContributor.Start"/>
        /// auto-registers with <see cref="EconomyManager"/>.
        /// </summary>
        private void AttachMaintenanceContributor(int cellX, int cellY, int subTypeId)
        {
            CityCell cell = grid != null ? grid.GetCell(cellX, cellY) : null;
            if (cell == null) return;

            var contributor = cell.gameObject.AddComponent<StateServiceMaintenanceContributor>();
            contributor.ConfiguredSubTypeId = subTypeId;
        }
    }
}
