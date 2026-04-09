using System.IO;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Resolves committed <see cref="Territory.Persistence.GameSaveData"/> scenario files under
    /// <c>tools/fixtures/scenarios/</c> by <b>scenario id</b> (<b>kebab-case</b>, ASCII).
    /// </summary>
    public static class ScenarioPathResolver
    {
        const string FixturesRelativeDir = "tools/fixtures/scenarios";

        /// <summary>
        /// Repository root (parent of <c>Assets</c>), using <see cref="Application.dataPath"/>.
        /// </summary>
        public static string GetRepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        /// <summary>
        /// Absolute path to the scenarios root (<c>tools/fixtures/scenarios</c>).
        /// </summary>
        public static string GetScenariosRootDirectory()
        {
            return Path.GetFullPath(Path.Combine(GetRepositoryRoot(), FixturesRelativeDir));
        }

        /// <summary>
        /// Tries <c>{id}/save.json</c> then <c>{id}.json</c> under the scenarios folder.
        /// </summary>
        /// <param name="scenarioId"><b>Scenario id</b> (file-system safe; expected <b>kebab-case</b>).</param>
        /// <param name="absolutePath">Resolved path when return value is true.</param>
        /// <returns>True when a file exists.</returns>
        public static bool TryResolveScenarioId(string scenarioId, out string absolutePath)
        {
            absolutePath = null;
            if (string.IsNullOrWhiteSpace(scenarioId))
                return false;

            string id = scenarioId.Trim();
            if (id.Length == 0 || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;
            if (id.Contains(Path.DirectorySeparatorChar) || id.Contains(Path.AltDirectorySeparatorChar))
                return false;

            string root = GetScenariosRootDirectory();
            string nested = Path.Combine(root, id, "save.json");
            if (File.Exists(nested))
            {
                absolutePath = nested;
                return true;
            }

            string flat = Path.Combine(root, id + ".json");
            if (File.Exists(flat))
            {
                absolutePath = flat;
                return true;
            }

            return false;
        }
    }
}
