namespace Territory.UI.Toolbar
{
    /// <summary>
    /// Fixed-index overlay identifier shared between IR-bake (TECH-3232 catalog interactive
    /// <c>overlay_slugs</c> ordering) and runtime <see cref="OverlayToggleDataAdapter"/>.
    /// Index ordering is load-bearing — adapter <c>_overlayToggles[i]</c> aligns with the
    /// integer value of each member; <see cref="UIManager"/> overlay state list is keyed
    /// by the same integer index for save-load round-trip.
    /// </summary>
    /// <remarks>
    /// Append-only: never reorder existing members; never insert new members in the middle.
    /// Add new overlays at the tail to preserve save-file index stability (Stage 7 lock).
    /// </remarks>
    public enum OverlaySlug
    {
        Terrain = 0,
        Pollution = 1,
        LandValue = 2,
        RoadNetwork = 3,
        TrafficFlow = 4,
    }
}
