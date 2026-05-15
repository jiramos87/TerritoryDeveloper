using UnityEngine;
using Domains.Registry;
using Territory.IsoSceneCore.Contracts;

namespace Territory.RegionScene.UI
{
    /// <summary>
    /// City subtype catalog for RegionScene. Registers three footprint entries (small/medium/large)
    /// into IIsoSceneSubtypePicker at Start (invariant #12). Attached as a MonoBehaviour in
    /// RegionScene so it participates in scene lifecycle without touching RegionManager hub.
    /// </summary>
    public sealed class RegionSubtypeCatalog : MonoBehaviour
    {
        // Stable slugs — must not change post-prototype (picker selection persists across loads).
        public const string SlugSmall  = "city.small";
        public const string SlugMedium = "city.medium";
        public const string SlugLarge  = "city.large";

        private IIsoSceneSubtypePicker _picker;

        private void Start()
        {
            var registry = FindObjectOfType<ServiceRegistry>();
            if (registry == null)
            {
                Debug.LogWarning("[RegionSubtypeCatalog] ServiceRegistry not found — subtype picker not registered.");
                return;
            }

            _picker = registry.Resolve<IIsoSceneSubtypePicker>();
            if (_picker == null)
            {
                Debug.LogWarning("[RegionSubtypeCatalog] IIsoSceneSubtypePicker not found — catalog skipped.");
                return;
            }

            // Register catalog entries: small / medium / large city footprint (scaffold; full semantics deferred)
            _picker.RegisterCatalog(new SubtypeCatalog(SlugSmall,  "Small (32x32)"));
            _picker.RegisterCatalog(new SubtypeCatalog(SlugMedium, "Medium (48x48)"));
            _picker.RegisterCatalog(new SubtypeCatalog(SlugLarge,  "Large (64x64)"));

            // Register self so RegionToolCreateCity can query the active subtype
            registry.Register<RegionSubtypeCatalog>(this);
        }

        /// <summary>Currently selected city subtype slug. Defaults to small until user picks.</summary>
        public string SelectedSubtypeSlug { get; private set; } = SlugSmall;

        /// <summary>Called when the player selects a city subtype tile in the picker.</summary>
        public void SelectSubtype(string slug)
        {
            SelectedSubtypeSlug = slug;
            _picker?.Select(slug);
        }
    }
}
