namespace Territory.Core
{
    /// <summary>
    /// Placeholder region-scale cell. Data-only; not a MonoBehaviour; not inserted
    /// into GridManager.gridArray. Inert until Step 2 region-sim work lands
    /// (see multi-scale-master-plan.md Step 2). Glossary: City cell / Region cell / Country cell.
    /// </summary>
    public class RegionCell
    {
        public int X { get; }
        public int Y { get; }
        public string ParentRegionId { get; }   // GUID string; matches GameSaveData.regionId

        public RegionCell(int x, int y, string parentRegionId)
        {
            X = x;
            Y = y;
            ParentRegionId = parentRegionId;
        }
    }
}
