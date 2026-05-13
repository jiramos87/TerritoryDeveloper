#if UNITY_EDITOR
using Territory.Core;
using Territory.Persistence;
using Territory.Roads;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Editor-only helper for the neighbor-stub new-game smoke.
    /// Keeps <see cref="AgentTestModeBatchRunner"/> thin by extracting scripted-build logic here.
    ///
    /// Flow:
    /// 1. Optionally pin <see cref="MapGenerationSeed"/> via <paramref name="testSeed"/>.
    /// 2. Call <see cref="GameSaveManager.NewGame"/> — seeds neighbor stubs + generates geography.
    /// 3. Call <see cref="InterstateManager.GenerateAndPlaceInterstate"/> — uses road preparation
    ///    family (invariant #10); the family calls <c>InvalidateRoadCache()</c> (invariant #2) and
    ///    <see cref="NeighborCityBindingRecorder.RecordExits"/> internally.
    /// </summary>
    public static class NeighborStubSmokeDriver
    {
        /// <summary>
        /// Drive new-game + scripted interstate build.
        /// Called from <see cref="AgentTestModeBatchRunner.PumpWaitGrid"/> when <c>new_game_mode</c> is true.
        /// </summary>
        /// <param name="saveManager">Active <see cref="GameSaveManager"/>; must be non-null (caller guards).</param>
        /// <param name="grid"><see cref="GridManager"/> from scene; used for logging only here.</param>
        /// <param name="testSeed">
        /// When non-zero, calls <see cref="MapGenerationSeed.SetSessionMasterSeed"/> before
        /// <see cref="GameSaveManager.NewGame"/> so the run is reproducible.
        /// 0 = not pinned (NewGame rolls its own seed via <see cref="MapGenerationSeed.RollNewMasterSeed"/>).
        /// </param>
        public static void RunNewGameSmoke(GameSaveManager saveManager, GridManager grid, int testSeed)
        {
            if (testSeed != 0)
            {
                MapGenerationSeed.SetSessionMasterSeed(testSeed);
                Debug.Log($"[NeighborStubSmokeDriver] Seed pinned to {testSeed}.");
            }

            saveManager.NewGame();
            Debug.Log($"[NeighborStubSmokeDriver] NewGame complete. NeighborStubs.Count={saveManager.NeighborStubs.Count}");

            // Scripted interstate build uses InterstateManager.GenerateAndPlaceInterstate, which
            // calls RoadManager.PlaceInterstateFromPath → road preparation family (PathTerraformPlan
            // → Phase-1 → Apply) → InvalidateRoadCache() → NeighborCityBindingRecorder.RecordExits.
            // This satisfies invariant #10 (never call ComputePathPlan alone) and invariant #2.
            InterstateManager im = saveManager.interstateManager != null
                ? saveManager.interstateManager
                : Object.FindObjectOfType<InterstateManager>();

            if (im == null)
            {
                Debug.LogWarning("[NeighborStubSmokeDriver] InterstateManager not found — interstate not built; bindings will be 0.");
                return;
            }

            bool placed = im.GenerateAndPlaceInterstate();
            Debug.Log($"[NeighborStubSmokeDriver] GenerateAndPlaceInterstate returned {placed}. neighborCityBindings.Count={saveManager.neighborCityBindings.Count}");
        }

        /// <summary>
        /// Assert stub seeding + binding + resolver contract post-build.
        /// Returns a <see cref="Domains.Testing.Dto.NeighborStubSmokeResultDto"/> with counts +
        /// <c>assertions_passed</c>.
        ///
        /// Assertions:
        /// 1. <c>stub_count &gt;= 1</c>
        /// 2. <c>binding_count &gt;= 1</c>
        /// 3. For each binding, <see cref="GridManager.GetNeighborStub(BorderSide)"/> returns non-null
        ///    stub whose <c>id == binding.stubId</c>.
        /// 4. <c>resolver_matches == binding_count</c>.
        /// </summary>
        public static Domains.Testing.Dto.NeighborStubSmokeResultDto RunSmokeAssertions(
            GameSaveManager saveManager,
            GridManager grid)
        {
            var result = new Domains.Testing.Dto.NeighborStubSmokeResultDto();

            if (saveManager == null)
            {
                result.failure_detail = "GameSaveManager is null.";
                return result;
            }

            result.stub_count = saveManager.NeighborStubs.Count;
            result.binding_count = saveManager.neighborCityBindings != null
                ? saveManager.neighborCityBindings.Count
                : 0;

            // Assert stub seeded.
            if (result.stub_count < 1)
            {
                result.failure_detail = $"stub_count={result.stub_count} (expected >= 1).";
                return result;
            }

            // Assert binding recorded.
            if (result.binding_count < 1)
            {
                result.failure_detail = $"binding_count={result.binding_count} (expected >= 1 after interstate build).";
                return result;
            }

            // Resolver round-trip: for each binding, GetNeighborStub(side) must return the seeded stub.
            if (grid == null)
            {
                result.failure_detail = "GridManager is null — cannot run resolver assertions.";
                return result;
            }

            int matches = 0;
            System.Text.StringBuilder failDetails = null;
            foreach (var binding in saveManager.neighborCityBindings)
            {
                // Find the matching stub by stubId to get its borderSide.
                BorderSide resolvedSide = BorderSide.South;
                bool foundStub = false;
                foreach (var stub in saveManager.NeighborStubs)
                {
                    if (stub.id == binding.stubId)
                    {
                        resolvedSide = stub.borderSide;
                        foundStub = true;
                        break;
                    }
                }

                if (!foundStub)
                {
                    if (failDetails == null) failDetails = new System.Text.StringBuilder();
                    failDetails.AppendLine($"Binding stubId={binding.stubId} not found in NeighborStubs.");
                    continue;
                }

                NeighborCityStub? resolved = grid.GetNeighborStub(resolvedSide);
                if (resolved == null)
                {
                    if (failDetails == null) failDetails = new System.Text.StringBuilder();
                    failDetails.AppendLine($"GetNeighborStub({resolvedSide}) returned null for stubId={binding.stubId}.");
                    continue;
                }

                if (resolved.Value.id != binding.stubId)
                {
                    if (failDetails == null) failDetails = new System.Text.StringBuilder();
                    failDetails.AppendLine($"GetNeighborStub({resolvedSide}).id={resolved.Value.id} != binding.stubId={binding.stubId}.");
                    continue;
                }

                matches++;
            }

            result.resolver_matches = matches;

            if (matches != result.binding_count || failDetails != null)
            {
                string details = failDetails != null ? failDetails.ToString().TrimEnd() : string.Empty;
                result.failure_detail = $"resolver_matches={matches} != binding_count={result.binding_count}. {details}".Trim();
                return result;
            }

            result.assertions_passed = true;
            return result;
        }
    }
}
#endif
