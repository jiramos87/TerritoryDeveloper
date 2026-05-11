using UnityEngine;
using Territory.Core;

namespace Domains.Grid.Services
{
    /// <summary>
    /// Pure query POCO: in-bounds checks + neighbor lookups extracted from GridManager.
    /// No MonoBehaviour lifecycle. Holds IGridManager ref via composition.
    /// Stage 3.0 carve-out: IsValidGridPosition, IsInBounds.
    /// Domain-leaf: refs Core only (no Game asmdef dep).
    /// </summary>
    public class GridQueryService
    {
        private readonly IGridManager _grid;

        public GridQueryService(IGridManager grid)
        {
            _grid = grid;
        }

        /// <summary>True if grid pos inside grid bounds.</summary>
        public bool IsValidGridPosition(Vector2 gridPosition)
        {
            int x = (int)gridPosition.x;
            int y = (int)gridPosition.y;
            return x >= 0 && x < _grid.width && y >= 0 && y < _grid.height;
        }

        /// <summary>True if (x,y) inside grid bounds.</summary>
        public bool IsInBounds(int x, int y)
            => x >= 0 && x < _grid.width && y >= 0 && y < _grid.height;

        /// <summary>CityCell at (x,y) or null if out of bounds.</summary>
        public CityCell GetCell(int x, int y)
            => IsInBounds(x, y) ? _grid.GetCell(x, y) : null;
    }
}
