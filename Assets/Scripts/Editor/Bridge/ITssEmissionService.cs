namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Facade interface — Strategy γ contract for TSS emission.
    /// Reads token data for a given theme slug and writes
    /// Assets/UI/Themes/{themeSlug}.tss with :root { --ds-*: ...; } block.
    /// Producer: ui-toolkit-migration Stage 1.0; consumers: Stages 2.0-6.0.
    /// </summary>
    public interface ITssEmissionService
    {
        /// <summary>Emit .tss file to outPath for the given theme slug.</summary>
        void EmitTo(string outPath, string themeSlug);
    }
}
