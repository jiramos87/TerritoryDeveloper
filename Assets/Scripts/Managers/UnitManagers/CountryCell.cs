namespace Territory.Core
{
    /// <summary>
    /// Placeholder country-scale cell. Data-only; not a MonoBehaviour; not inserted
    /// into GridManager.gridArray. Inert until country-sim work lands
    /// (see multi-scale-master-plan.md Stage 1.2 / Step 2). Glossary: City cell / Region cell / Country cell.
    /// </summary>
    public class CountryCell
    {
        public int X { get; }
        public int Y { get; }
        public string ParentCountryId { get; }   // GUID string; matches GameSaveData.countryId

        public CountryCell(int x, int y, string parentCountryId)
        {
            X = x;
            Y = y;
            ParentCountryId = parentCountryId;
        }
    }
}
