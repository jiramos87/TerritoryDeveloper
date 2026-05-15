namespace Territory.IsoSceneCore.Contracts
{
    /// <summary>
    /// Zone paint mechanism shell contract. Full per-scene zone definitions are deferred
    /// (region-zone-paint-zone-definitions follow-up plan). This shell establishes the
    /// seam for Stage 5.0+ RegionScene zone paint registration.
    /// </summary>
    public interface IIsoSceneZonePaintHost
    {
        /// <summary>Register a zone paint descriptor. Implementation deferred to region-zone-paint plan.</summary>
        void RegisterZone(IsoSceneZone zone);

        /// <summary>Begin a paint stroke at grid coordinates. Full impl deferred.</summary>
        void BeginPaint(int gridX, int gridY, string zoneSlug);

        /// <summary>End the current paint stroke.</summary>
        void EndPaint();
    }

    /// <summary>Lightweight zone descriptor placeholder for deferred region-zone-paint plan.</summary>
    public sealed class IsoSceneZone
    {
        /// <summary>Zone slug (e.g. "region-residential", "region-wilderness").</summary>
        public string Slug { get; }
        /// <summary>Display label shown in zone paint overlay.</summary>
        public string Label { get; }

        public IsoSceneZone(string slug, string label)
        {
            Slug = slug;
            Label = label;
        }
    }
}
