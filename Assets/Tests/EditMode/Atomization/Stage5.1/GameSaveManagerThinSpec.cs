using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Stage5_1
{
    /// <summary>
    /// §Red-Stage Proof anchor: GameSaveManagerThinSpec.cs::game_save_manager_is_thin
    /// Stage 5.1: GameSaveManager Tier-C NO-PORT — hub collapses to ≤200 LOC; file ops delegated to SaveService.
    /// Green: GameSaveManager.cs ≤200 LOC AND SaveService.cs exists at Domains/Save/Services/ AND hub delegates via _svc.
    /// </summary>
    public class GameSaveManagerThinSpec
    {
        private const string HubPath =
            "Assets/Scripts/Managers/GameManagers/GameSaveManager.cs";

        private const string SvcPath =
            "Assets/Scripts/Domains/Save/Services/SaveService.cs";

        [Test]
        public void game_save_manager_is_thin()
        {
            string repoRoot = GetRepoRoot();

            // Assert 1: GameSaveManager.cs ≤200 LOC
            string hub = Path.Combine(repoRoot, HubPath);
            Assert.IsTrue(File.Exists(hub), $"GameSaveManager.cs not found at {hub}");
            int lineCount = File.ReadAllLines(hub).Length;
            Assert.LessOrEqual(lineCount, 200,
                $"GameSaveManager.cs must be ≤200 LOC after THIN. Got {lineCount} lines.");

            // Assert 2: SaveService.cs exists under Domains/Save/Services/
            string svc = Path.Combine(repoRoot, SvcPath);
            Assert.IsTrue(File.Exists(svc), $"SaveService.cs must exist at {svc}.");

            // Assert 3: hub delegates to SaveService (_svc field present)
            string hubSource = File.ReadAllText(hub);
            Assert.IsTrue(hubSource.Contains("SaveService"),
                "GameSaveManager hub must reference SaveService.");
            Assert.IsTrue(hubSource.Contains("_svc"),
                "GameSaveManager hub must hold a _svc delegate field.");

            // Assert 4: hub calls WireDependencies
            Assert.IsTrue(hubSource.Contains("WireDependencies"),
                "GameSaveManager hub must call _svc.WireDependencies(...).");

            // Assert 5: locked fields still present (invariant #3)
            Assert.IsTrue(hubSource.Contains("public GridManager gridManager"),
                "GameSaveManager hub must retain public GridManager gridManager field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public CityStats cityStats"),
                "GameSaveManager hub must retain public CityStats cityStats field (locked #3).");
            Assert.IsTrue(hubSource.Contains("public TimeManager timeManager"),
                "GameSaveManager hub must retain public TimeManager timeManager field (locked #3).");
        }

        [Test]
        public void save_service_is_in_correct_namespace()
        {
            Type t = typeof(Domains.Save.Services.SaveService);
            Assert.AreEqual("Domains.Save.Services", t.Namespace,
                $"SaveService namespace mismatch: {t.Namespace}");
        }

        [Test]
        public void save_service_exposes_wire_dependencies()
        {
            Type t = typeof(Domains.Save.Services.SaveService);
            Assert.IsNotNull(t, "SaveService must exist");
            MethodInfo m = t.GetMethod("WireDependencies");
            Assert.IsNotNull(m, "SaveService must expose WireDependencies()");
        }

        [Test]
        public void save_service_exposes_has_any_save()
        {
            Type t = typeof(Domains.Save.Services.SaveService);
            Assert.IsNotNull(t, "SaveService must exist");
            MethodInfo m = t.GetMethod("HasAnySave", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(m, "SaveService must expose static HasAnySave()");
        }

        [Test]
        public void save_service_exposes_delete_save()
        {
            Type t = typeof(Domains.Save.Services.SaveService);
            Assert.IsNotNull(t, "SaveService must exist");
            MethodInfo m = t.GetMethod("DeleteSave", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(m, "SaveService must expose static DeleteSave()");
        }

        private static string GetRepoRoot()
        {
            string dir = Application.dataPath;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "CLAUDE.md"))) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Application.dataPath.Replace("/Assets", "");
        }
    }
}
