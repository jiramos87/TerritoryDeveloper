namespace Domains.UI.Editor.UiBake
{
    /// <summary>
    /// Public facade interface for the UI bake domain (Editor-only).
    /// Consumers bind to this interface — never to UiBakeHandler directly.
    /// Stage 9 tracer slice: UiBakeService stub established; full inline extraction deferred.
    /// Partial-class UiBakeHandler family retained in Territory.Editor.Bridge (Guardrail #5).
    /// </summary>
    public interface IUiBake
    {
        /// <summary>Bake UI prefabs from a panels.json snapshot path. Returns null on success; error JSON on failure.</summary>
        string BakeFromSnapshot(string panelsPath, string outDir, string themeSoPath);

        /// <summary>Parse a panels.json snapshot. Returns null on success; error JSON on parse fault.</summary>
        string ParseSnapshot(string snapshotJson);
    }
}
