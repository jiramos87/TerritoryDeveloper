using UnityEngine;
using Territory.IsoSceneCore;
using Territory.RegionScene.Terrain;

namespace Territory.RegionScene.Terrain
{
    /// <summary>Sprite-per-cell renderer for RegionScene. Subscribes to IsoSceneChunkCuller.OnVisibleSetChanged; reads Region{Height,Water,Cliff}Map.
    /// Anchor: Region64x64TerrainRendersWithHeightWaterCliff.</summary>
    public class RegionCellRenderer : MonoBehaviour
    {
        [SerializeField] private Sprite grassSprite;
        [SerializeField] private Sprite waterSlopeSprite;
        [SerializeField] private Sprite cliffSouthSprite;
        [SerializeField] private Sprite cliffEastSprite;

        private RegionHeightMap _heightMap;
        private RegionWaterMap _waterMap;
        private RegionCliffMap _cliffMap;
        private IsoSceneChunkCuller _culler;

        private SpriteRenderer[,] _renderers;
        private int _visibleCellCount;

        /// <summary>Count of currently rendered visible cells. Used by test assertions.</summary>
        public int VisibleCellCount => _visibleCellCount;

        /// <summary>Wire maps + culler from RegionManager.Start().</summary>
        public void Configure(RegionHeightMap heightMap, RegionWaterMap waterMap, RegionCliffMap cliffMap, IsoSceneChunkCuller culler)
        {
            _heightMap = heightMap;
            _waterMap = waterMap;
            _cliffMap = cliffMap;
            _culler = culler;
            _renderers = new SpriteRenderer[RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize];
            _culler.OnVisibleSetChanged += OnVisibleSetChanged;
        }

        private void OnDestroy()
        {
            if (_culler != null)
                _culler.OnVisibleSetChanged -= OnVisibleSetChanged;
        }

        private void LateUpdate()
        {
            _culler?.UpdateVisibility();
        }

        private void OnVisibleSetChanged(int minCX, int maxCX, int minCY, int maxCY)
        {
            if (_heightMap == null) return;
            int chunkSize = 16; // matches IsoSceneChunkCuller default
            _visibleCellCount = 0;
            int gMin = RegionHeightMap.RegionGridSize;

            int xStart = Mathf.Max(0, minCX * chunkSize);
            int xEnd = Mathf.Min(RegionHeightMap.RegionGridSize - 1, (maxCX + 1) * chunkSize - 1);
            int yStart = Mathf.Max(0, minCY * chunkSize);
            int yEnd = Mathf.Min(RegionHeightMap.RegionGridSize - 1, (maxCY + 1) * chunkSize - 1);

            for (int x = xStart; x <= xEnd; x++)
            {
                for (int y = yStart; y <= yEnd; y++)
                {
                    RenderCell(x, y);
                    _visibleCellCount++;
                }
            }
        }

        private void RenderCell(int x, int y)
        {
            Sprite sprite = PickSprite(x, y);
            if (sprite == null) sprite = grassSprite; // fallback to grass when sprites unassigned

            if (_renderers[x, y] == null)
            {
                var go = new GameObject($"RC_{x}_{y}");
                go.transform.SetParent(transform);
                var sr = go.AddComponent<SpriteRenderer>();
                _renderers[x, y] = sr;
                // Isometric world position
                float wx = (x - y) * 0.5f;
                float wy = (x + y) * 0.25f + _heightMap.HeightAt(x, y) * 0.125f;
                go.transform.position = new Vector3(wx, wy, 0f);
                // Sort order: depth = y-based iso depth so back cliffs don't paint over front grass
                sr.sortingOrder = -(x + y) * 2 + _heightMap.HeightAt(x, y);
                sr.sprite = sprite;
            }
            else
            {
                _renderers[x, y].sprite = sprite;
            }
        }

        private Sprite PickSprite(int x, int y)
        {
            if (_waterMap != null && _waterMap.IsWater(x, y)) return waterSlopeSprite;
            if (_cliffMap != null && _cliffMap.IsCliff(x, y))
            {
                var face = _cliffMap.GetFace(x, y);
                return face == RegionCliffMap.CliffFace.South ? cliffSouthSprite : cliffEastSprite;
            }
            return grassSprite;
        }
    }
}
