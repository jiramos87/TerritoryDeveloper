using UnityEngine;

namespace Territory.Persistence
{
    /// <summary>
    /// Master seed for procedural extended terrain + lake depression-fill (BUG-36).
    /// Rolled per New Game; ensured once per editor/session cold start for InitializeGeography.
    /// Not saved — load restores height + water from serialized data only.
    /// </summary>
    public static class MapGenerationSeed
    {
        private static bool hasMasterSeed;
        private static int masterSeed;

        /// <summary>Current session master seed. Valid after <see cref="EnsureSessionMasterSeed"/> or <see cref="RollNewMasterSeed"/>.</summary>
        public static int MasterSeed => masterSeed;

        /// <summary>Assign new random master seed. Call at start of each New Game.</summary>
        public static void RollNewMasterSeed()
        {
            masterSeed = Random.Range(int.MinValue, int.MaxValue);
            if (masterSeed == 0)
                masterSeed = 1;
            hasMasterSeed = true;
        }

        /// <summary>Ensure master seed exists for first geography init (e.g. Play in Editor without menu).</summary>
        public static void EnsureSessionMasterSeed()
        {
            if (!hasMasterSeed)
                RollNewMasterSeed();
        }

        /// <summary>
        /// Set session master seed from interchange <c>geography_init_params.seed</c> (TECH-41).
        /// Replaces prior master seed for this process (e.g. after <see cref="RollNewMasterSeed"/> from New Game menu).
        /// </summary>
        public static void SetSessionMasterSeed(int seed)
        {
            masterSeed = seed == 0 ? 1 : seed;
            hasMasterSeed = true;
        }

        /// <summary>Stable derived seed for Perlin offsets on extended terrain. Replaces fixed TerrainGenSeed.</summary>
        public static int GetTerrainProceduralOffsetSeed()
        {
            EnsureSessionMasterSeed();
            return Derive(masterSeed, 0x54455231u);
        }

        /// <summary>Secondary salt for micro-lake roughness noise offsets.</summary>
        public static int GetTerrainMicroLakeNoiseSalt()
        {
            EnsureSessionMasterSeed();
            return Derive(masterSeed, 0x4D43524Fu);
        }

        /// <summary>Carve threshold for sparse dips. Varies with map seed → each run exposes different lake seed density on procedural terrain.</summary>
        public static float GetMicroLakeCarveThreshold()
        {
            EnsureSessionMasterSeed();
            int v = Derive(masterSeed, 0x4D494352u);
            float u = (v & 0x7FFFFFFF) / (float)0x7FFFFFFF;
            return Mathf.Lerp(0.024f, 0.046f, u);
        }

        /// <summary>Feeds <see cref="Territory.Terrain.WaterMap.LakeFillSettings.RandomSeed"/> + terrain lake-feasibility shuffle.</summary>
        public static int GetLakeFillRandomSeed()
        {
            EnsureSessionMasterSeed();
            return Derive(masterSeed, 0x4C414B45u);
        }

        private static int Derive(int master, uint salt)
        {
            unchecked
            {
                long m = master;
                long s = salt;
                int h = (int)((m * 1103515245L + s * 12345L + 0x9E3779B9L) ^ (m >> 16));
                if (h == 0)
                    h = (int)(salt ^ (uint)master);
                return h;
            }
        }
    }
}
