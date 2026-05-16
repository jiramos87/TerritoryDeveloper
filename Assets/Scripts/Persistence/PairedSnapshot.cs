using Territory.SceneManagement;

namespace Territory.Persistence
{
    /// <summary>POCO — paired save slot data for city+region atomic write.</summary>
    public sealed class PairedSnapshot
    {
        /// <summary>Logical save slot identifier (e.g. "slot1").</summary>
        public string SaveId { get; set; } = "";

        /// <summary>Full serialized city snapshot bytes (JSON UTF-8).</summary>
        public byte[] CityBytes { get; set; } = System.Array.Empty<byte>();

        /// <summary>Full serialized region snapshot bytes (JSON UTF-8).</summary>
        public byte[] RegionBytes { get; set; } = System.Array.Empty<byte>();

        /// <summary>Which scene was active at save time.</summary>
        public IsoSceneContext ActiveScene { get; set; } = IsoSceneContext.City;
    }
}
