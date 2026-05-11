namespace Territory.Core
{
    /// <summary>Structured denial reason for PlacementValidator.CanPlace (Stage 3.1, TECH-689).</summary>
    public enum PlacementFailReason
    {
        None = 0,
        Footprint,
        Zoning,
        Locked,
        Unaffordable,
        Occupied
    }

    /// <summary>Outcome of a placement check (TECH-689).</summary>
    public readonly struct PlacementResult
    {
        public bool IsAllowed => Reason == PlacementFailReason.None;
        public PlacementFailReason Reason { get; }
        public string Detail { get; }

        private PlacementResult(PlacementFailReason reason, string detail)
        {
            Reason = reason;
            Detail = detail ?? string.Empty;
        }

        /// <summary>Placement permitted.</summary>
        public static PlacementResult Allowed(string detail = null) => new PlacementResult(PlacementFailReason.None, detail);

        /// <summary>Placement denied for <paramref name="reason"/>.</summary>
        public static PlacementResult Fail(PlacementFailReason reason, string detail = null) =>
            new PlacementResult(reason, detail);
    }
}
