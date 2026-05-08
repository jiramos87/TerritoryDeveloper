using NUnit.Framework;
using System;
using System.IO;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Testmode
{
    /// <summary>
    /// Tracer tests: assert BatchStateService + GoldenCompareService + HeightIntegrityService +
    /// BatchSnapshotService + AgentTestModeBatchDtos extracted to Domains.Testing namespace.
    /// Red baseline: Domains/Testing/ absent → asserts fail.
    /// Green: Testing.Editor.asmdef + all four services + DTO types present; compile-check exits 0.
    /// §Red-Stage Proof anchor: AgentTestModeBatchRunnerAtomizationTests.cs::BatchStateService_is_in_domains_testing_services_namespace
    /// </summary>
    public class AgentTestModeBatchRunnerAtomizationTests
    {
        [Test]
        public void BatchStateService_is_in_domains_testing_services_namespace()
        {
            Type serviceType = typeof(Domains.Testing.Services.BatchStateService);
            Assert.AreEqual("Domains.Testing.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Testing.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void GoldenCompareService_is_in_domains_testing_services_namespace()
        {
            Type serviceType = typeof(Domains.Testing.Services.GoldenCompareService);
            Assert.AreEqual("Domains.Testing.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Testing.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void HeightIntegrityService_is_in_domains_testing_services_namespace()
        {
            Type serviceType = typeof(Domains.Testing.Services.HeightIntegrityService);
            Assert.AreEqual("Domains.Testing.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Testing.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void BatchSnapshotService_is_in_domains_testing_services_namespace()
        {
            Type serviceType = typeof(Domains.Testing.Services.BatchSnapshotService);
            Assert.AreEqual("Domains.Testing.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Testing.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void AgentTestModeBatchCitySnapshotDto_is_in_domains_testing_dto_namespace()
        {
            Type dtoType = typeof(Domains.Testing.Dto.AgentTestModeBatchCitySnapshotDto);
            Assert.AreEqual("Domains.Testing.Dto", dtoType.Namespace,
                $"Expected namespace 'Domains.Testing.Dto', got '{dtoType.Namespace}'");
        }

        [Test]
        public void NeighborStubRoundtripGoldenDto_is_in_domains_testing_dto_namespace()
        {
            Type dtoType = typeof(Domains.Testing.Dto.NeighborStubRoundtripGoldenDto);
            Assert.AreEqual("Domains.Testing.Dto", dtoType.Namespace,
                $"Expected namespace 'Domains.Testing.Dto', got '{dtoType.Namespace}'");
        }

        [Test]
        public void HeightIntegritySweepResultDto_is_in_domains_testing_dto_namespace()
        {
            Type dtoType = typeof(Domains.Testing.Dto.HeightIntegritySweepResultDto);
            Assert.AreEqual("Domains.Testing.Dto", dtoType.Namespace,
                $"Expected namespace 'Domains.Testing.Dto', got '{dtoType.Namespace}'");
        }

        [Test]
        public void NeighborStubSmokeResultDto_is_in_domains_testing_dto_namespace()
        {
            Type dtoType = typeof(Domains.Testing.Dto.NeighborStubSmokeResultDto);
            Assert.AreEqual("Domains.Testing.Dto", dtoType.Namespace,
                $"Expected namespace 'Domains.Testing.Dto', got '{dtoType.Namespace}'");
        }

        [Test]
        public void AgentTestModeBatchStateDto_is_in_domains_testing_dto_namespace()
        {
            Type dtoType = typeof(Domains.Testing.Dto.AgentTestModeBatchStateDto);
            Assert.AreEqual("Domains.Testing.Dto", dtoType.Namespace,
                $"Expected namespace 'Domains.Testing.Dto', got '{dtoType.Namespace}'");
        }

        [Test]
        public void GoldenCompareService_IsNeighborStubGolden_returns_true_for_neighbor_stubs_filename()
        {
            bool result = Domains.Testing.Services.GoldenCompareService.IsNeighborStubGolden(
                "/path/to/scenario-neighbor-stubs.json");
            Assert.IsTrue(result, "IsNeighborStubGolden must return true for filenames containing 'neighbor-stubs'");
        }

        [Test]
        public void GoldenCompareService_IsNeighborStubGolden_returns_false_for_city_stats_filename()
        {
            bool result = Domains.Testing.Services.GoldenCompareService.IsNeighborStubGolden(
                "/path/to/scenario-city-stats.json");
            Assert.IsFalse(result, "IsNeighborStubGolden must return false for non-neighbor-stubs filenames");
        }

        [Test]
        public void GoldenCompareService_IdMatches_sentinel_accepts_valid_guid()
        {
            string validGuid = System.Guid.NewGuid().ToString("D");
            bool result = Domains.Testing.Services.GoldenCompareService.IdMatches("<guid>", validGuid);
            Assert.IsTrue(result, "IdMatches must accept any valid GUID for sentinel '<guid>'");
        }

        [Test]
        public void GoldenCompareService_IdMatches_sentinel_rejects_non_guid()
        {
            bool result = Domains.Testing.Services.GoldenCompareService.IdMatches("<guid>", "not-a-guid");
            Assert.IsFalse(result, "IdMatches must reject non-GUID strings for sentinel '<guid>'");
        }

        [Test]
        public void HeightIntegrityService_MaxOffenderCount_is_ten()
        {
            Assert.AreEqual(10, Domains.Testing.Services.HeightIntegrityService.MaxOffenderCount,
                "HeightIntegrityService.MaxOffenderCount must be 10");
        }

        [Test]
        public void BatchSnapshotService_CitySnapshotSchemaVersion_is_two()
        {
            Assert.AreEqual(2, Domains.Testing.Services.BatchSnapshotService.CitySnapshotSchemaVersion,
                "BatchSnapshotService.CitySnapshotSchemaVersion must be 2");
        }

        [Test]
        public void BatchStateService_StateFileName_matches_runner_constant()
        {
            Assert.AreEqual(".agent-testmode-batch-state.json",
                Domains.Testing.Services.BatchStateService.StateFileName,
                "BatchStateService.StateFileName must match runner constant");
        }

        [Test]
        public void testing_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Testing",
                "Testing.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"Testing.Editor.asmdef not found at: {path}");
        }
    }
}
