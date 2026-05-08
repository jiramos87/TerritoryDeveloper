namespace Domains.Bridge.Services
{
    /// <summary>
    /// Dispatch-table facade for bridge mutation kinds (Editor-only).
    /// Stage 7 tracer slice — establishes namespace + method surface.
    /// Full extraction (inline Run* methods from AgentBridgeCommandRunner.Mutations.cs)
    /// deferred to Stage 8+; AgentBridgeCommandRunner.Mutations.cs remains the runtime path.
    /// Guardrail #14: mutation dispatch shape preserved; no behavior changes in Stage 7.
    /// </summary>
    public class MutationDispatchService
    {
        /// <summary>
        /// Dispatch a mutation kind. Returns true if the kind was handled.
        /// Stage 7: stub — real dispatch lives in AgentBridgeCommandRunner.Mutations.cs.
        /// Stage 8+: inline all 20+ Run* methods here; wire runner to delegate to this.
        /// All mutation kinds: attach_component, remove_component, assign_serialized_field,
        /// create_gameobject, delete_gameobject, find_gameobject, set_transform,
        /// set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene,
        /// instantiate_prefab, apply_prefab_overrides, create_scriptable_object,
        /// modify_scriptable_object, refresh_asset_database, move_asset, delete_asset,
        /// execute_menu_item, bake_ui_from_ir, wire_asset_from_catalog,
        /// set_panel_visible, scene_replace_with_prefab.
        /// </summary>
        public bool TryDispatch(string kind, string repoRoot, string commandId, string requestJson)
        {
            // Stage 7 tracer stub — runtime dispatch remains in AgentBridgeCommandRunner.Mutations.cs.
            // Stage 8+: full inline extraction lands here.
            return false;
        }
    }
}
