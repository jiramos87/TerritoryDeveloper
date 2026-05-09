using System;
using System.IO;
using NUnit.Framework;
using Territory.Persistence;

namespace Territory.Tests.EditMode.UI
{
    /// <summary>
    /// Wave A1 (TECH-27066) — HasAnySave + GetMostRecentSave read-only API tests.
    /// Uses a temp directory; no disk side effects.
    /// </summary>
    public class GameSaveManagerApiTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"gsm_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ── HasAnySave ────────────────────────────────────────────────────────────

        [Test]
        public void HasAnySave_EmptyDir_ReturnsFalse()
        {
            Assert.IsFalse(GameSaveManager.HasAnySave(_tempDir));
        }

        [Test]
        public void HasAnySave_MissingDir_ReturnsFalse()
        {
            Assert.IsFalse(GameSaveManager.HasAnySave(Path.Combine(_tempDir, "no_such_dir")));
        }

        [Test]
        public void HasAnySave_WithJsonFile_ReturnsTrue()
        {
            File.WriteAllText(Path.Combine(_tempDir, "save1.json"), "{}");
            Assert.IsTrue(GameSaveManager.HasAnySave(_tempDir));
        }

        [Test]
        public void HasAnySave_NonJsonFiles_ReturnsFalse()
        {
            File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "hello");
            Assert.IsFalse(GameSaveManager.HasAnySave(_tempDir));
        }

        // ── GetMostRecentSave ────────────────────────────────────────────────────

        [Test]
        public void GetMostRecentSave_EmptyDir_ReturnsNull()
        {
            Assert.IsNull(GameSaveManager.GetMostRecentSave(_tempDir));
        }

        [Test]
        public void GetMostRecentSave_MissingDir_ReturnsNull()
        {
            Assert.IsNull(GameSaveManager.GetMostRecentSave(Path.Combine(_tempDir, "no_such_dir")));
        }

        [Test]
        public void GetMostRecentSave_SingleFile_ReturnsIt()
        {
            string path = Path.Combine(_tempDir, "city1.json");
            // Minimal GameSaveData shape so GetSaveMetadata can parse it.
            File.WriteAllText(path, "{\"saveName\":\"city1\",\"cityName\":\"TestCity\",\"realWorldSaveTimeTicks\":0}");
            var result = GameSaveManager.GetMostRecentSave(_tempDir);
            Assert.IsNotNull(result);
            Assert.AreEqual(path, result.FilePath);
        }

        [Test]
        public void GetMostRecentSave_MultipleFiles_ReturnsNewest()
        {
            long ticksOld  = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            long ticksNew  = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

            string oldPath = Path.Combine(_tempDir, "old.json");
            string newPath = Path.Combine(_tempDir, "new.json");

            File.WriteAllText(oldPath,
                $"{{\"saveName\":\"old\",\"cityName\":\"Old\",\"realWorldSaveTimeTicks\":{ticksOld}}}");
            File.WriteAllText(newPath,
                $"{{\"saveName\":\"new\",\"cityName\":\"New\",\"realWorldSaveTimeTicks\":{ticksNew}}}");

            var result = GameSaveManager.GetMostRecentSave(_tempDir);
            Assert.IsNotNull(result);
            Assert.AreEqual(newPath, result.FilePath);
        }
    }
}
