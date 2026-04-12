namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Pure grid metric helpers (Chebyshev / Manhattan on integer cells). For previews +
    /// MCP <c>grid_distance</c>. Does not define pathfinding edge costs.
    /// </summary>
    public static class GridDistanceMath
    {
        public static int Chebyshev(int ax, int ay, int bx, int by)
        {
            int dx = ax > bx ? ax - bx : bx - ax;
            int dy = ay > by ? ay - by : by - ay;
            return dx > dy ? dx : dy;
        }

        public static int Manhattan(int ax, int ay, int bx, int by)
        {
            int dx = ax > bx ? ax - bx : bx - ax;
            int dy = ay > by ? ay - by : by - ay;
            return dx + dy;
        }
    }
}
