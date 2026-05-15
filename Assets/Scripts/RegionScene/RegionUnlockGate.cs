using Territory.Persistence;

namespace Territory.RegionScene
{
    /// <summary>Stateless gate: reads regionUnlocked flag from CityData or GameSaveData. Single named constant per SUGGESTION-3.</summary>
    public static class RegionUnlockGate
    {
        /// <summary>Prototype unlock threshold: city pop must reach this value to access region view.</summary>
        public const int RegionUnlockPopThreshold = 1000;

        /// <summary>Returns true if the city has unlocked region access (pop threshold OR cheat flag).</summary>
        public static bool IsUnlocked(CityData city, bool cheatFlag = false)
        {
            if (city == null) return false;
            return cheatFlag || city.regionUnlocked || city.pop >= RegionUnlockPopThreshold;
        }

        /// <summary>Overload reading directly from GameSaveData (main-menu path).</summary>
        public static bool IsUnlocked(GameSaveData save, bool cheatFlag = false)
        {
            if (save == null) return false;
            return cheatFlag || save.regionUnlocked;
        }

        /// <summary>Stamp unlock flag when threshold crossed. Call from city evolution / save path.</summary>
        public static void TrySetUnlocked(CityData city, GameSaveData save)
        {
            if (city == null || save == null) return;
            if (city.pop >= RegionUnlockPopThreshold)
            {
                city.regionUnlocked = true;
                save.regionUnlocked = true;
            }
        }
    }
}
