namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Reads catalog_token rows (DB canonical) and emits Assets/UI/Themes/{themeSlug}.tss
    /// with :root { --ds-*: ...; } block. Token slugs map to USS custom properties.
    /// Strategy γ: delegates emission to ITssEmissionService POCO.
    /// </summary>
    public sealed class TssEmitter
    {
        readonly ITssEmissionService _svc;

        public TssEmitter(ITssEmissionService svc) => _svc = svc;

        public void Emit(string themeSlug) =>
            _svc.EmitTo($"Assets/UI/Themes/{themeSlug}.tss", themeSlug);
    }
}
