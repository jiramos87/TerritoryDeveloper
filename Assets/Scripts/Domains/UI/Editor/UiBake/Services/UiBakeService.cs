namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Facade service for UI bake operations (Editor-only).
    /// Stage 9 tracer slice — establishes namespace + method surface.
    /// Full extraction (inline BakeFromPanelSnapshot + Parse from UiBakeHandler.cs) deferred
    /// to Stage 10+; UiBakeHandler.cs remains the runtime path.
    /// Guardrail #5: partial-class UiBakeHandler declaration files preserved.
    /// </summary>
    public class UiBakeService
    {
        /// <summary>
        /// Bake UI prefabs from a panels.json snapshot.
        /// Stage 9: stub — real bake lives in Territory.Editor.Bridge.UiBakeHandler.BakeFromPanelSnapshot.
        /// Stage 10+: inline full bake body here; wire UiBakeHandler.Bake to delegate to this.
        /// Public bake kinds: bake_ui_from_ir (panels_path → prefab write per item).
        /// </summary>
        public string BakeFromSnapshot(string panelsPath, string outDir, string themeSoPath)
        {
            // Stage 9 tracer stub — runtime bake remains in UiBakeHandler.BakeFromPanelSnapshot.
            return null;
        }

        /// <summary>
        /// Parse a panels.json snapshot JSON string.
        /// Stage 9: stub — real parse lives in Territory.Editor.Bridge.UiBakeHandler.ParsePanelSnapshot.
        /// </summary>
        public string ParseSnapshot(string snapshotJson)
        {
            // Stage 9 tracer stub — runtime parse remains in UiBakeHandler.ParsePanelSnapshot.
            return null;
        }
    }
}
