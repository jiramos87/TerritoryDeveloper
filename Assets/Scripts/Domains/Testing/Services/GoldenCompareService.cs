#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Domains.Testing.Dto;
using UnityEngine;

namespace Domains.Testing.Services
{
    /// <summary>
    /// Golden-file comparison logic extracted from AgentTestModeBatchRunner.
    /// Handles city-stats golden compare + neighbor-stub golden compare. Stage 13 tracer slice.
    /// </summary>
    public static class GoldenCompareService
    {
        /// <summary>
        /// Returns true when the golden file's basename contains "neighbor-stubs",
        /// dispatching to <see cref="TryCompareNeighborStubGolden"/> instead of city-stats compare.
        /// </summary>
        public static bool IsNeighborStubGolden(string goldenPath)
        {
            if (string.IsNullOrEmpty(goldenPath))
                return false;
            string stem = Path.GetFileNameWithoutExtension(goldenPath);
            return stem.IndexOf("neighbor-stubs", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Tolerant id comparison: golden sentinel "&lt;guid&gt;" accepts any valid GUID (format D).
        /// Plain equality for all other values.
        /// </summary>
        public static bool IdMatches(string goldenValue, string runtimeValue)
            => goldenValue == "<guid>"
                ? Guid.TryParseExact(runtimeValue, "D", out _)
                : goldenValue == runtimeValue;

        /// <summary>
        /// Compares actual city-stats snapshot against committed golden JSON.
        /// Returns true on match; false + diff string on mismatch.
        /// </summary>
        public static bool TryCompareGolden(string goldenPath, AgentTestModeBatchCitySnapshotDto actual,
            int ticksRequested, out string diff)
        {
            diff = null;
            if (string.IsNullOrEmpty(goldenPath) || actual == null)
                return true;

            string json;
            try
            {
                json = File.ReadAllText(goldenPath);
            }
            catch (Exception ex)
            {
                diff = $"Could not read golden file: {ex.Message}";
                return false;
            }

            var expected = JsonUtility.FromJson<AgentTestModeBatchCitySnapshotDto>(json);
            if (expected == null)
            {
                diff = "Golden JSON deserialized to null.";
                return false;
            }

            if (expected.schema_version != 1 && expected.schema_version != 2)
            {
                diff = $"Unsupported golden schema_version {expected.schema_version} (expected 1 or 2).";
                return false;
            }

            if (expected.simulation_ticks != ticksRequested)
            {
                diff = $"Golden simulation_ticks {expected.simulation_ticks} does not match requested ticks {ticksRequested}.";
                return false;
            }

            var sb = new StringBuilder();
            void Cmp(string name, int a, int b)
            {
                if (a != b) sb.AppendLine($"{name}: golden={a} actual={b}");
            }

            Cmp(nameof(actual.population), expected.population, actual.population);
            Cmp(nameof(actual.money), expected.money, actual.money);
            Cmp(nameof(actual.roadCount), expected.roadCount, actual.roadCount);
            Cmp(nameof(actual.grassCount), expected.grassCount, actual.grassCount);
            Cmp(nameof(actual.residentialZoneCount), expected.residentialZoneCount, actual.residentialZoneCount);
            Cmp(nameof(actual.commercialZoneCount), expected.commercialZoneCount, actual.commercialZoneCount);
            Cmp(nameof(actual.industrialZoneCount), expected.industrialZoneCount, actual.industrialZoneCount);
            Cmp(nameof(actual.residentialBuildingCount), expected.residentialBuildingCount, actual.residentialBuildingCount);
            Cmp(nameof(actual.commercialBuildingCount), expected.commercialBuildingCount, actual.commercialBuildingCount);
            Cmp(nameof(actual.industrialBuildingCount), expected.industrialBuildingCount, actual.industrialBuildingCount);
            Cmp(nameof(actual.forestCellCount), expected.forestCellCount, actual.forestCellCount);

            if (expected.schema_version >= 2)
            {
                if (!IdMatches(expected.regionId ?? "", actual.regionId ?? ""))
                    sb.AppendLine($"regionId: golden={expected.regionId} actual={actual.regionId}");
                if (!IdMatches(expected.countryId ?? "", actual.countryId ?? ""))
                    sb.AppendLine($"countryId: golden={expected.countryId} actual={actual.countryId}");
            }

            if (sb.Length > 0)
            {
                diff = sb.ToString().TrimEnd();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Compares actual neighbor-stub snapshot against committed golden JSON.
        /// Stubs sorted by id; bindings by (stubId, exitCellX, exitCellY).
        /// Returns true on match; false + diff string on mismatch.
        /// </summary>
        public static bool TryCompareNeighborStubGolden(string goldenPath,
            NeighborStubRoundtripGoldenDto actual, out string diff)
        {
            diff = null;
            if (string.IsNullOrEmpty(goldenPath) || actual == null)
                return true;

            string json;
            try
            {
                json = File.ReadAllText(goldenPath);
            }
            catch (Exception ex)
            {
                diff = $"Could not read neighbor-stub golden file: {ex.Message}";
                return false;
            }

            var expected = JsonUtility.FromJson<NeighborStubRoundtripGoldenDto>(json);
            if (expected == null)
            {
                diff = "Neighbor-stub golden JSON deserialized to null.";
                return false;
            }

            if (expected.schema_version != 1)
            {
                diff = $"Unsupported neighbor-stub golden schema_version {expected.schema_version} (expected 1).";
                return false;
            }

            var expStubs = new List<NeighborStubGoldenEntry>(
                expected.neighborStubs ?? Array.Empty<NeighborStubGoldenEntry>());
            expStubs.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));

            var expBindings = new List<NeighborBindingGoldenEntry>(
                expected.neighborCityBindings ?? Array.Empty<NeighborBindingGoldenEntry>());
            expBindings.Sort((a, b) =>
            {
                int c = string.Compare(a.stubId, b.stubId, StringComparison.Ordinal);
                if (c != 0) return c;
                c = a.exitCellX.CompareTo(b.exitCellX);
                return c != 0 ? c : a.exitCellY.CompareTo(b.exitCellY);
            });

            var sb = new StringBuilder();

            var actStubs = actual.neighborStubs ?? Array.Empty<NeighborStubGoldenEntry>();
            if (expStubs.Count != actStubs.Length)
            {
                sb.AppendLine($"neighborStubs count: golden={expStubs.Count} actual={actStubs.Length}");
            }
            else
            {
                for (int i = 0; i < expStubs.Count; i++)
                {
                    var e = expStubs[i]; var a = actStubs[i];
                    if (!IdMatches(e.id ?? "", a.id ?? ""))
                        sb.AppendLine($"stub[{i}].id: golden={e.id} actual={a.id}");
                    if (e.displayName != a.displayName)
                        sb.AppendLine($"stub[{i}].displayName: golden={e.displayName} actual={a.displayName}");
                    if (!string.Equals(e.borderSide, a.borderSide, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine($"stub[{i}].borderSide: golden={e.borderSide} actual={a.borderSide}");
                }
            }

            var actBindings = actual.neighborCityBindings ?? Array.Empty<NeighborBindingGoldenEntry>();
            if (expBindings.Count != actBindings.Length)
            {
                sb.AppendLine($"neighborCityBindings count: golden={expBindings.Count} actual={actBindings.Length}");
            }
            else
            {
                for (int i = 0; i < expBindings.Count; i++)
                {
                    var e = expBindings[i]; var a = actBindings[i];
                    if (!IdMatches(e.stubId ?? "", a.stubId ?? ""))
                        sb.AppendLine($"binding[{i}].stubId: golden={e.stubId} actual={a.stubId}");
                    if (e.exitCellX != a.exitCellX)
                        sb.AppendLine($"binding[{i}].exitCellX: golden={e.exitCellX} actual={a.exitCellX}");
                    if (e.exitCellY != a.exitCellY)
                        sb.AppendLine($"binding[{i}].exitCellY: golden={e.exitCellY} actual={a.exitCellY}");
                    if (!string.Equals(e.borderSide, a.borderSide, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine($"binding[{i}].borderSide: golden={e.borderSide} actual={a.borderSide}");
                }
            }

            if (sb.Length > 0)
            {
                diff = sb.ToString().TrimEnd();
                return false;
            }
            return true;
        }
    }
}
#endif
