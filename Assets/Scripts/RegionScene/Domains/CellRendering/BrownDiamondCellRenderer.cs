using Territory.Core;
using UnityEngine;

namespace Territory.RegionScene.CellRendering
{
    /// <summary>Default IRegionCellRenderer: draws a brown diamond at the isometric cell position.
    /// Registered into the single-renderer slot in RegionManager.Start when no sibling override is present.
    /// Anchor: CellRenderer_ContractAndEvent.</summary>
    public sealed class BrownDiamondCellRenderer : IRegionCellRenderer
    {
        private readonly Transform _parent;
        private readonly Sprite _brownDiamondSprite;
        private readonly SpriteRenderer[,] _renderers;
        private readonly int _gridSize;

        /// <summary>Number of cells rendered since last reset. Exposed for contract test assertions.</summary>
        public int RenderCallCount { get; private set; }

        public BrownDiamondCellRenderer(Transform parent, Sprite brownDiamondSprite, int gridSize)
        {
            _parent            = parent;
            _brownDiamondSprite = brownDiamondSprite;
            _gridSize          = gridSize;
            _renderers         = new SpriteRenderer[gridSize, gridSize];
        }

        /// <inheritdoc/>
        public void Render(RegionCell cell, PlayerCityState optionalCityState)
        {
            int x = cell.X;
            int y = cell.Y;
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return;

            RenderCallCount++;

            if (_renderers[x, y] == null)
            {
                var go = new GameObject($"BDCell_{x}_{y}");
                go.transform.SetParent(_parent);
                float wx = (x - y) * 0.32f;
                float wy = (x + y) * 0.16f;
                go.transform.position = new Vector3(wx, wy, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite       = _brownDiamondSprite;
                sr.sortingOrder = -(x + y) * 2;
                _renderers[x, y] = sr;
            }
            else
            {
                _renderers[x, y].sprite = _brownDiamondSprite;
            }
        }

        /// <summary>Reset render call counter (used by contract test between assertions).</summary>
        public void ResetCallCount() => RenderCallCount = 0;
    }
}
