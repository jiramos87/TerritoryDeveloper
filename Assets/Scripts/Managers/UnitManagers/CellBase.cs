namespace Territory.Core
{
    /// <summary>
    /// Abstract base for all scale-typed cell classes (city, region, country).
    /// Carries only the scale-universal primitives shared by every cell kind.
    /// City-specific state (roads, buildings, zones, forests, water, cliffs, interstate,
    /// desirability) lives on the concrete <see cref="Cell"/> subclass.
    /// </summary>
    public abstract class CellBase : UnityEngine.MonoBehaviour
    {
        [UnityEngine.Header("Grid Position")]
        public int x;
        public int y;
        public int height;
        public int sortingOrder;
        public UnityEngine.Vector2 transformPosition;
    }
}
