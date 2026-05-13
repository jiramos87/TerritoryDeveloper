namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Facade interface — Strategy γ contract for UXML+USS emission.
    /// DB row + theme → VisualElement tree → write .uxml + .uss pair under outDir.
    /// Producer: ui-toolkit-migration Stage 1.0; consumers: Stages 2.0-5.0.
    /// </summary>
    public interface IUxmlEmissionService
    {
        /// <summary>Emit .uxml + .uss pair for the given panel row into outDir.</summary>
        void EmitTo(string outDir, PanelRow row);
    }
}
