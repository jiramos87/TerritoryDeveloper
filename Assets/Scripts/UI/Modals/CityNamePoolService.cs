using UnityEngine;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave A2 (TECH-27070) — runtime accessor for the city-name-pool-es string pool.
    /// Loads names from Resources/CityNamePoolEs.txt (newline-delimited) on first call.
    /// Fallback: returns null when resource absent (caller uses its own placeholder).
    /// </summary>
    public static class CityNamePoolService
    {
        private static string[] _names;
        private static bool _loaded;

        /// <summary>Roll a random name from the pool. Returns null when pool unavailable.</summary>
        public static string TryRollRandom()
        {
            EnsureLoaded();
            if (_names == null || _names.Length == 0) return null;
            return _names[Random.Range(0, _names.Length)];
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var asset = Resources.Load<TextAsset>("CityNamePoolEs");
            if (asset == null) return;
            _names = asset.text.Split(
                new[] { '\n', '\r' },
                System.StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
