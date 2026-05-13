namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Lightweight DTO — DB panel row passed to IPanelEmitter implementations.
    /// Carries the slug, panel kind, params_json, and theme slug needed for emission.
    /// </summary>
    public sealed class PanelRow
    {
        public string Slug { get; set; } = "";
        public string Kind { get; set; } = "";
        public string ParamsJson { get; set; } = "{}";
        public string ThemeSlug { get; set; } = "dark";
    }
}
