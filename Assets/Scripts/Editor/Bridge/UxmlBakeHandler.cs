namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Sidecar emitter — implements IPanelEmitter (DEC-A28 side-by-side emitter alongside UiBakeHandler).
    /// DB row → UXML+USS pair written to Assets/UI/Generated/ via IUxmlEmissionService facade.
    /// Legacy UiBakeHandler (prefab emitter) remains alive until Stage 6 quarantine plan.
    /// </summary>
    public sealed class UxmlBakeHandler : IPanelEmitter
    {
        readonly IUxmlEmissionService _svc;

        public UxmlBakeHandler(IUxmlEmissionService svc) => _svc = svc;

        public void Emit(PanelRow row) =>
            _svc.EmitTo("Assets/UI/Generated", row);
    }
}
