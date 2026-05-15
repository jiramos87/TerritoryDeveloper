namespace Territory.IsoSceneCore.Contracts
{
    /// <summary>
    /// Per-scene subtype picker contract. Scenes register their subtype catalogs at Start;
    /// the shared picker renders the catalog in the subtype-slot. RegionScene registers its
    /// own catalogs in Stage 3.0+; CityScene wires existing SubtypePickerController.
    /// </summary>
    public interface IIsoSceneSubtypePicker
    {
        /// <summary>Register a subtype catalog for a given tool family slug.</summary>
        void RegisterCatalog(SubtypeCatalog catalog);

        /// <summary>Activate the picker for the given tool family slug.</summary>
        void Select(string slug);
    }

    /// <summary>Descriptor for a per-scene subtype catalog entry.</summary>
    public sealed class SubtypeCatalog
    {
        /// <summary>Tool family slug this catalog belongs to (e.g. "zone-r", "services").</summary>
        public string Slug { get; }
        /// <summary>Human-readable label shown as picker header.</summary>
        public string Label { get; }

        public SubtypeCatalog(string slug, string label)
        {
            Slug = slug;
            Label = label;
        }
    }
}
