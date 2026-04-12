using System;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Placeholders for basin / volume ↔ surface height <c>S</c> math. No gameplay until feature ships.
    /// </summary>
    public static class BasinVolumeMath
    {
        /// <summary>Reserved — estimate water volume from body surface + bed heights.</summary>
        /// <exception cref="NotImplementedException">Always until feature ships.</exception>
        public static float EstimateVolumeFromSurfaceHeight(int surfaceS, int bedHeightMin)
        {
            throw new NotImplementedException("FEAT-48: basin volume vs surface S — not implemented.");
        }

        /// <summary>Reserved — solve surface <c>S</c> given target volume + bathymetry.</summary>
        /// <exception cref="NotImplementedException">Always until feature ships.</exception>
        public static int EstimateSurfaceHeightFromVolume(float volumeUnits, int bedHeightMin)
        {
            throw new NotImplementedException("FEAT-48: basin volume vs surface S — not implemented.");
        }
    }
}
