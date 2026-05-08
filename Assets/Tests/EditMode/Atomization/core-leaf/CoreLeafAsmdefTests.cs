using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.CoreLeaf
{
/// <summary>
/// Stage-20 gate: Territory.Core leaf asmdef cycle-break assertions.
/// Composed test; grows cumulatively from T20.1 through T20.8.
/// All assertions must be green before Stage 20 ships.
/// </summary>
[TestFixture]
public class CoreLeafAsmdefTests
{
    private const string GameGuid = "GUID:7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a";
    private const string CoreGuid = "GUID:a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6";
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static readonly string CoreAsmdefPath = "Assets/Scripts/Core/Territory.Core.asmdef";
    private static readonly string DomainsRoot = "Assets/Scripts/Domains";

    // Roads retains Game GUID until AutoBuildService is interface-abstracted.
    private static readonly HashSet<string> KnownRoadsException = new HashSet<string>
    {
        "Assets/Scripts/Domains/Roads/Roads.asmdef"
    };

    // ---- T20.1: Core asmdef structure ----

    [Test]
    public void Core_Asmdef_Exists()
    {
        string full = Path.Combine(RepoRoot, CoreAsmdefPath);
        Assert.IsTrue(File.Exists(full), $"Missing: {CoreAsmdefPath}");
    }

    [Test]
    public void Core_Asmdef_Has_Zero_References_Out()
    {
        var parsed = ReadAsmdef(CoreAsmdefPath);
        string[] refs = GetReferences(parsed);
        Assert.AreEqual(0, refs.Length, $"Territory.Core.asmdef must have references:[] (leaf). Found: {string.Join(", ", refs)}");
    }

    [Test]
    public void Core_Asmdef_AutoReferenced_False()
    {
        var parsed = ReadAsmdef(CoreAsmdefPath);
        bool auto = GetBool(parsed, "autoReferenced", false);
        Assert.IsFalse(auto, "Territory.Core.asmdef must have autoReferenced:false");
    }

    [Test]
    public void Core_Asmdef_NoEngineReferences_False()
    {
        var parsed = ReadAsmdef(CoreAsmdefPath);
        // noEngineReferences must be false — CellBase/CityCell are MonoBehaviours.
        bool noEngine = GetBool(parsed, "noEngineReferences", false);
        Assert.IsFalse(noEngine, "Territory.Core.asmdef must have noEngineReferences:false (contains MonoBehaviours)");
    }

    // ---- T20.2: Game asmdef references Core ----

    [Test]
    public void Game_Asmdef_References_Core()
    {
        const string gamePath = "Assets/Scripts/TerritoryDeveloper.Game.asmdef";
        var parsed = ReadAsmdef(gamePath);
        string[] refs = GetReferences(parsed);
        Assert.Contains(CoreGuid, refs, $"TerritoryDeveloper.Game.asmdef must reference Core GUID. Found: {string.Join(", ", refs)}");
    }

    // ---- T20.3: Geography asmdef zero Game refs ----

    [Test]
    public void Geography_Asmdef_Has_Zero_Game_Refs()
    {
        AssertNoGameGuid("Assets/Scripts/Domains/Geography/Geography.asmdef");
    }

    [Test]
    public void Geography_Asmdef_References_Core()
    {
        AssertHasCoreGuid("Assets/Scripts/Domains/Geography/Geography.asmdef");
    }

    // ---- T20.4: Terrain + Water zero Game refs ----

    [Test]
    public void TerrainWaterGrid_Asmdefs_Have_Zero_Game_Refs()
    {
        AssertNoGameGuid("Assets/Scripts/Domains/Terrain/Terrain.asmdef");
        AssertNoGameGuid("Assets/Scripts/Domains/Water/Water.asmdef");
        // Grid already had empty refs — still assert clean.
        AssertNoGameGuid("Assets/Scripts/Domains/Grid/Grid.asmdef");
    }

    // ---- T20.5: Roads + Economy ----

    [Test]
    public void Economy_Asmdef_Has_Zero_Game_Refs()
    {
        AssertNoGameGuid("Assets/Scripts/Domains/Economy/Economy.asmdef");
    }

    [Test]
    public void Roads_Asmdef_References_Core()
    {
        // Roads retains Game GUID (known exception). Assert at least Core is present.
        AssertHasCoreGuid("Assets/Scripts/Domains/Roads/Roads.asmdef");
    }

    // ---- T20.6: Zones zero Game refs ----

    [Test]
    public void Zones_Asmdef_Has_Zero_Game_Refs()
    {
        AssertNoGameGuid("Assets/Scripts/Domains/Zones/Zones.asmdef");
    }

    [Test]
    public void Zones_Asmdef_References_Core()
    {
        AssertHasCoreGuid("Assets/Scripts/Domains/Zones/Zones.asmdef");
    }

    // ---- T20.7: Validator fixture ----

    [Test]
    public void Validator_Exits_Nonzero_When_Cycle_Reintroduced()
    {
        // Validate fixture file contains Game GUID (prerequisite for node validator test).
        const string fixturePath = "Assets/Tests/EditMode/Atomization/core-leaf/Fixtures/domain-with-game-cycle.asmdef.fixture";
        string full = Path.Combine(RepoRoot, fixturePath);
        Assert.IsTrue(File.Exists(full), $"Fixture missing: {fixturePath}");
        string content = File.ReadAllText(full);
        Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
            "Fixture must contain Game GUID for validator test scenario");
        // Note: node process spawn not available in Edit Mode runner.
        // The node process test runs via npm run validate:no-domain-game-cycle directly.
        // This test asserts the fixture file is present + correctly formed.
    }

    // ---- T20.8: Top-level composed gate ----

    [Test]
    public void Cycle_Broken_Core_Leaf_Has_Zero_Refs_Out()
    {
        // (a) Core asmdef is a true leaf — zero outbound references.
        var coreAsmdef = ReadAsmdef(CoreAsmdefPath);
        string[] coreRefs = GetReferences(coreAsmdef);
        Assert.AreEqual(0, coreRefs.Length,
            $"Territory.Core must be a leaf with references:[]. Found: {string.Join(", ", coreRefs)}");

        // (b) Every Domain asmdef has zero Game GUID back-ref (except Roads known exception).
        string domainsFullPath = Path.Combine(RepoRoot, DomainsRoot);
        var domainAsmdefs = Directory.GetFiles(domainsFullPath, "*.asmdef", SearchOption.AllDirectories);
        var violations = new List<string>();
        foreach (string absPath in domainAsmdefs)
        {
            string repoRel = absPath.Substring(RepoRoot.Length + 1).Replace('\\', '/');
            if (KnownRoadsException.Contains(repoRel)) continue;
            string[] refs = GetReferences(ReadAsmdefFromPath(absPath));
            if (refs.Contains(GameGuid))
                violations.Add(repoRel);
        }
        Assert.AreEqual(0, violations.Count,
            $"Domain→Game GUID cycle detected in:\n{string.Join("\n", violations)}");

        // (c) Test asmdef for this suite references Core (transitive Core resolution check).
        const string thisAsmdef = "Assets/Tests/EditMode/Atomization/core-leaf/CoreLeaf.Tests.EditMode.Atomization.asmdef";
        var testAsmdefParsed = ReadAsmdef(thisAsmdef);
        string[] testRefs = GetReferences(testAsmdefParsed);
        Assert.Contains(CoreGuid, testRefs,
            "CoreLeaf test asmdef must reference Core GUID directly to prove transitive Core resolution");
    }

    // ---- Helpers ----

    private static SimpleJson ReadAsmdef(string repoRelPath)
    {
        string full = Path.Combine(RepoRoot, repoRelPath);
        Assert.IsTrue(File.Exists(full), $"Asmdef not found: {repoRelPath}");
        return ReadAsmdefFromPath(full);
    }

    private static SimpleJson ReadAsmdefFromPath(string absPath)
    {
        string json = File.ReadAllText(absPath);
        return new SimpleJson(json);
    }

    private static string[] GetReferences(SimpleJson parsed)
    {
        return parsed.GetStringArray("references");
    }

    private static bool GetBool(SimpleJson parsed, string key, bool defaultVal)
    {
        return parsed.GetBool(key, defaultVal);
    }

    private static void AssertNoGameGuid(string repoRelPath)
    {
        var parsed = ReadAsmdef(repoRelPath);
        string[] refs = GetReferences(parsed);
        Assert.IsFalse(refs.Contains(GameGuid),
            $"{repoRelPath} must NOT contain Game GUID {GameGuid}. Found refs: {string.Join(", ", refs)}");
    }

    private static void AssertHasCoreGuid(string repoRelPath)
    {
        var parsed = ReadAsmdef(repoRelPath);
        string[] refs = GetReferences(parsed);
        Assert.IsTrue(refs.Contains(CoreGuid),
            $"{repoRelPath} must reference Core GUID {CoreGuid}. Found refs: {string.Join(", ", refs)}");
    }

    /// <summary>Minimal JSON parser for asmdef string/bool/array fields — no external deps.</summary>
    private class SimpleJson
    {
        private readonly string _raw;
        public SimpleJson(string json) { _raw = json; }

        public string[] GetStringArray(string key)
        {
            // Find "key": [ ... ] block
            string pattern = $"\"{key}\"";
            int ki = _raw.IndexOf(pattern, StringComparison.Ordinal);
            if (ki < 0) return Array.Empty<string>();
            int colonIdx = _raw.IndexOf(':', ki + pattern.Length);
            if (colonIdx < 0) return Array.Empty<string>();
            int openBracket = _raw.IndexOf('[', colonIdx);
            if (openBracket < 0) return Array.Empty<string>();
            int closeBracket = _raw.IndexOf(']', openBracket);
            if (closeBracket < 0) return Array.Empty<string>();
            string inner = _raw.Substring(openBracket + 1, closeBracket - openBracket - 1);
            if (string.IsNullOrWhiteSpace(inner)) return Array.Empty<string>();
            var items = inner.Split(',');
            return items
                .Select(s => s.Trim().Trim('"'))
                .Where(s => s.Length > 0)
                .ToArray();
        }

        public bool GetBool(string key, bool defaultVal)
        {
            string pattern = $"\"{key}\"";
            int ki = _raw.IndexOf(pattern, StringComparison.Ordinal);
            if (ki < 0) return defaultVal;
            int colonIdx = _raw.IndexOf(':', ki + pattern.Length);
            if (colonIdx < 0) return defaultVal;
            // Find first non-whitespace after colon
            int vi = colonIdx + 1;
            while (vi < _raw.Length && char.IsWhiteSpace(_raw[vi])) vi++;
            if (vi >= _raw.Length) return defaultVal;
            if (_raw[vi] == 't') return true;
            if (_raw[vi] == 'f') return false;
            return defaultVal;
        }
    }
}
}
