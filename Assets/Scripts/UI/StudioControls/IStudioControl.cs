namespace Territory.UI.StudioControls
{
    /// <summary>StudioControl interactive contract — kind slug + per-usage slug + detail-row apply hook.</summary>
    public interface IStudioControl
    {
        /// <summary>Archetype slug (e.g. <c>"knob"</c>, <c>"vu-meter"</c>); concrete variants return literal.</summary>
        string Kind { get; }

        /// <summary>Per-usage slug populated at bake time (e.g. <c>"build-knob"</c>); matches IR <c>interactives[].slug</c>.</summary>
        string Slug { get; }

        /// <summary>Apply typed detail row populated at bake time; concrete variants cast to their <see cref="IDetailRow"/> impl.</summary>
        void ApplyDetail(IDetailRow detail);
    }
}
