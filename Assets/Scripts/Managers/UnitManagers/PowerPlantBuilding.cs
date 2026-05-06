using UnityEngine;
using Territory.Zones;

namespace Territory.Buildings
{
    /// <summary>
    /// Stage 9.8 (TECH-15894) — IBuilding impl for coal/solar/wind power subtypes.
    /// Carries pollution enum; reader wiring deferred to economy v2 per §Scope locks #11.
    /// </summary>
    public class PowerPlantBuilding : MonoBehaviour, IBuilding
    {
        public enum PollutionLevel { High, Zero }

        [SerializeField] private PollutionLevel pollutionLevel = PollutionLevel.Zero;
        [SerializeField] private int constructionCost = 5000;
        [SerializeField] private int buildingSize = 3;
        [SerializeField] private GameObject prefabRef;

        public Building.BuildingType BuildingType => Building.BuildingType.Power;
        public int ConstructionCost => constructionCost;
        public GameObject Prefab => prefabRef;
        public int BuildingSize => buildingSize;
        public GameObject GameObjectReference => gameObject;

        /// <summary>Pollution level serialized on prefab. Consumer: economy v2 (deferred).</summary>
        public PollutionLevel Pollution => pollutionLevel;
    }
}
