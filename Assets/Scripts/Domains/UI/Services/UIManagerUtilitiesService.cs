using System.Collections.Generic;
using Territory.Core;

namespace Domains.UI.Services
{
    /// <summary>
    /// Pure utilities POCO extracted from UIManager.Utilities partial (Stage 4.5 THIN).
    /// No MonoBehaviour. Scene refs (prefabs, coroutines, Instantiate) stay in UIManager hub.
    /// Invariants: #3 no per-frame FindObjectOfType, #4 no new singletons.
    /// </summary>
    public class UIManagerUtilitiesService
    {
        // ─── Placement fail reason → display message ───────────────────────────

        private static readonly Dictionary<PlacementFailReason, string> _placementReasonMap =
            new Dictionary<PlacementFailReason, string>
            {
                { PlacementFailReason.Footprint,    "Out of bounds or unsupported footprint." },
                { PlacementFailReason.Zoning,       "Wrong zone for this asset."              },
                { PlacementFailReason.Locked,       "Asset locked — research required."       },
                { PlacementFailReason.Unaffordable, "Insufficient funds."                     },
                { PlacementFailReason.Occupied,     "Cell already occupied."                  },
            };

        /// <summary>
        /// Resolve display message for <paramref name="reason"/>. Returns false when
        /// reason is <see cref="PlacementFailReason.None"/> or unmapped.
        /// </summary>
        public bool TryGetPlacementMessage(PlacementFailReason reason, out string message)
        {
            message = null;
            if (reason == PlacementFailReason.None) return false;
            return _placementReasonMap.TryGetValue(reason, out message);
        }

        // ─── Forest enum → index (pure; scene refs stay in hub) ────────────────

        /// <summary>0 = Sparse, 1 = Medium, 2 = Dense. -1 = unknown.</summary>
        public static int ForestTypeToIndex(Territory.Forests.Forest.ForestType type)
        {
            switch (type)
            {
                case Territory.Forests.Forest.ForestType.Sparse: return 0;
                case Territory.Forests.Forest.ForestType.Medium: return 1;
                case Territory.Forests.Forest.ForestType.Dense:  return 2;
                default:                                         return -1;
            }
        }

        // ─── Insufficient-funds message builder ────────────────────────────────

        /// <summary>
        /// Build the display string for the insufficient-funds tooltip.
        /// Hub calls this and sets the Text component value.
        /// </summary>
        public static string BuildInsufficientFundsMessage(string itemType, int cost, int available)
            => $"Cannot afford {itemType}!\nCost: ${cost}\nAvailable: ${available}";

        // ─── Cell details 5-tuple builder ──────────────────────────────────────

        /// <summary>
        /// Build display tuple for the details popup from a <see cref="CityCell"/>.
        /// Hub passes result to <c>detailsPopupController.ShowCellDetails</c>.
        /// </summary>
        public static (string cellType, string zoneType, string population, string landValue, string pollution)
            BuildCellDetailsTuple(CityCell cell)
        {
            return (
                cell.GetBuildingType(),
                cell.GetBuildingName(),
                "Occupancy: " + cell.GetPopulation(),
                "Desirability: " + cell.desirability.ToString("F1"),
                "Happiness: " + cell.GetHappiness()
            );
        }
    }
}
