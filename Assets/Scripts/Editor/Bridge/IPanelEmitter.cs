namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Seam interface — bake one panel from a DB row and emit canonical artifact.
    /// Implementations: UiBakeHandler (prefab), UxmlBakeHandler (UXML+USS pair).
    /// Established by ui-bake-handler-atomization Stage 1 (DEC-A28 side-by-side emitter).
    /// </summary>
    public interface IPanelEmitter
    {
        /// <summary>Emit canonical artifact for the given panel row.</summary>
        void Emit(PanelRow row);
    }
}
