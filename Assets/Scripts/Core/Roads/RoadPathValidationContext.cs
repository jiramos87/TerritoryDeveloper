namespace Territory.Roads
{
/// <summary>
/// Shared terraform validation opts: manual draw, interstate, auto-road (same rules via <see cref="IRoadManager.TryPrepareRoadPlacementPlan"/>).
/// Core-leaf — Domains.Roads + Game both consume.
/// </summary>
public struct RoadPathValidationContext
{
    /// <summary>True (interstate) → paths needing cut-through hill flattening invalid.</summary>
    public bool forbidCutThrough;
}
}
