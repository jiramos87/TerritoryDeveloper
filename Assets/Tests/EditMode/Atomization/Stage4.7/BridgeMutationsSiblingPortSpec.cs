using NUnit.Framework;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage4_7
{
    /// <summary>
    /// §Red-Stage Proof anchor: BridgeMutationsSiblingPortSpec.cs::bridge_mutations_sibling_port_verified
    /// Stage 4.7: Tier-B sibling-port — AgentBridgeCommandRunner.Mutations re-verify.
    /// Confirms mutation kinds are declared in MutationDispatchService.Dispatch()
    /// (guardrail #13: mutation kinds in stem-matching sibling partial/service).
    /// The .Mutations.cs partial is the thin connector (TryDispatchMutationKind + shared helpers only).
    /// Invariants: class/namespace/path UNCHANGED on Mutations partial; kind dispatch table lives in POCO service.
    /// </summary>
    public class BridgeMutationsSiblingPortSpec
    {
        private const string MutationsPartialPath =
            "Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs";

        private const string MutationDispatchServicePath =
            "Assets/Scripts/Editor/Bridge/Services/MutationDispatchService.cs";

        // §Red-Stage Proof anchor — bridge_mutations_sibling_port_verified
        [Test]
        public void bridge_mutations_sibling_port_verified()
        {
            string root = GetRepoRoot();

            // Assert 1: mutation_kinds_declared_in_mutations_partial — guardrail #13
            // Kind dispatch lives in MutationDispatchService.Dispatch() NOT in Mutations.cs partial.
            string mutationsSrc = File.ReadAllText(Path.Combine(root, MutationsPartialPath));
            // Mutations.cs should NOT contain the kind switch dispatch table (no 'case "attach_component"')
            Assert.IsFalse(mutationsSrc.Contains("\"attach_component\""),
                "AgentBridgeCommandRunner.Mutations.cs must NOT contain mutation kind switch cases. " +
                "Kind dispatch belongs in MutationDispatchService.Dispatch() per guardrail #13.");

            // Assert 2: MutationDispatchService owns the dispatch table
            string dispatchSrc = File.ReadAllText(Path.Combine(root, MutationDispatchServicePath));
            Assert.IsTrue(dispatchSrc.Contains("\"attach_component\""),
                "MutationDispatchService must declare mutation kind 'attach_component' in its Dispatch switch.");
            Assert.IsTrue(dispatchSrc.Contains("\"bake_ui_from_ir\""),
                "MutationDispatchService must declare mutation kind 'bake_ui_from_ir' in its Dispatch switch.");
            Assert.IsTrue(dispatchSrc.Contains("\"scene_replace_with_prefab\""),
                "MutationDispatchService must declare mutation kind 'scene_replace_with_prefab' in its Dispatch switch.");

            // Assert 3: bridge_smoke_green — Mutations.cs connector method present
            Assert.IsTrue(mutationsSrc.Contains("TryDispatchMutationKind"),
                "Mutations.cs must expose TryDispatchMutationKind connector method (thin pass-through to MutationDispatchService).");
            Assert.IsTrue(mutationsSrc.Contains("MutationDispatchService.Dispatch"),
                "Mutations.cs TryDispatchMutationKind must delegate to MutationDispatchService.Dispatch.");
        }

        [Test]
        public void mutations_partial_exists_at_locked_path()
        {
            string root = GetRepoRoot();
            string fullPath = Path.Combine(root, MutationsPartialPath);
            Assert.IsTrue(File.Exists(fullPath),
                $"AgentBridgeCommandRunner.Mutations.cs must exist at locked path: {fullPath}");
        }

        [Test]
        public void mutation_dispatch_service_exists_at_locked_path()
        {
            string root = GetRepoRoot();
            string fullPath = Path.Combine(root, MutationDispatchServicePath);
            Assert.IsTrue(File.Exists(fullPath),
                $"MutationDispatchService.cs must exist at locked path: {fullPath}");
        }

        [Test]
        public void mutations_partial_is_static_partial_class()
        {
            string root = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(root, MutationsPartialPath));
            Assert.IsTrue(src.Contains("public static partial class AgentBridgeCommandRunner"),
                "AgentBridgeCommandRunner.Mutations.cs must declare 'public static partial class AgentBridgeCommandRunner' (invariant #2 — class UNCHANGED).");
        }

        [Test]
        public void mutation_dispatch_service_owns_24_mutation_kinds()
        {
            string root = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(root, MutationDispatchServicePath));
            // Count 'case "' occurrences as proxy for kind count (each kind = one case line)
            string[] expectedKinds = new[]
            {
                "attach_component", "remove_component", "assign_serialized_field",
                "create_gameobject", "delete_gameobject", "find_gameobject",
                "set_transform", "set_gameobject_active", "set_gameobject_parent",
                "save_scene", "open_scene", "new_scene",
                "instantiate_prefab", "apply_prefab_overrides",
                "create_scriptable_object", "modify_scriptable_object",
                "refresh_asset_database", "move_asset", "delete_asset",
                "execute_menu_item", "bake_ui_from_ir", "wire_asset_from_catalog",
                "set_panel_visible", "scene_replace_with_prefab"
            };
            var missing = expectedKinds.Where(k => !src.Contains($"\"{k}\"")).ToArray();
            Assert.AreEqual(0, missing.Length,
                $"MutationDispatchService.Dispatch missing kinds: {string.Join(", ", missing)}");
        }

        [Test]
        public void mutations_partial_shared_helpers_present()
        {
            string root = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(root, MutationsPartialPath));
            // Helpers used by sibling partials (SceneReplacePrefab.cs, Conformance.cs) must remain in Mutations.cs
            Assert.IsTrue(src.Contains("TryParseMutationParams"),
                "Mutations.cs must retain TryParseMutationParams helper (used by sibling partials).");
            Assert.IsTrue(src.Contains("AssertEditMode"),
                "Mutations.cs must retain AssertEditMode helper (used by sibling partials).");
            Assert.IsTrue(src.Contains("EscapeJsonString"),
                "Mutations.cs must retain EscapeJsonString helper (used by sibling partials).");
            Assert.IsTrue(src.Contains("TryResolveGameObject"),
                "Mutations.cs must retain TryResolveGameObject helper (used by sibling partials).");
            Assert.IsTrue(src.Contains("ExtractParamsJsonBlock"),
                "Mutations.cs must retain ExtractParamsJsonBlock helper (used by sibling partials).");
        }

        [Test]
        public void mutation_param_dtos_declared_in_mutations_partial()
        {
            string root = GetRepoRoot();
            string src = File.ReadAllText(Path.Combine(root, MutationsPartialPath));
            // DTOs used across sibling partials must remain in Mutations.cs (shared scope)
            Assert.IsTrue(src.Contains("AttachComponentParamsDto"),
                "Mutations.cs must declare AttachComponentParamsDto (shared DTO for sibling partials).");
            Assert.IsTrue(src.Contains("AssignSerializedFieldParamsDto"),
                "Mutations.cs must declare AssignSerializedFieldParamsDto.");
            Assert.IsTrue(src.Contains("BakeUiFromIrParamsDto"),
                "Mutations.cs must declare BakeUiFromIrParamsDto (bake_ui_from_ir mutation DTO).");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md")))
                    return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
