namespace Domains.Bridge.Services
{
    /// <summary>
    /// Dispatch-table facade for bridge mutation kinds (Editor-only).
    /// Stage 6 cutover: full inline extraction lives in
    /// Assets/Scripts/Editor/Bridge/Services/MutationDispatchService.cs (implicit editor assembly)
    /// so it can reach Territory.Editor.Bridge.*, Territory.UI.Themed.*, Territory.Catalog.*
    /// types that are in the default / implicit-editor assemblies.
    /// This stub preserved for namespace continuity.
    /// </summary>
    public class MutationDispatchService
    {
    }
}
