namespace Territory.RegionScene.Evolution
{
    /// <summary>Per-cell evolution state. Terrain kind + pop + urban_area + optional owning city.</summary>
    [System.Serializable]
    public class RegionCellData
    {
        public RegionTerrainKind terrainKind;
        public int pop;
        public float urbanArea;
        public string owningCityId;
    }

    /// <summary>Broad terrain classification per region cell (prototype-grade; expanded post-prototype).</summary>
    public enum RegionTerrainKind
    {
        Flat = 0,
        Water = 1,
        Mountain = 2,
        Forest = 3,
    }
}
