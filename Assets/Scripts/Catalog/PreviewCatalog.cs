using UnityEngine;

namespace Territory.Catalog
{
    /// <summary>
    /// Sandboxed preview component for the CatalogPreview scene (TECH-4627).
    /// Driven by AgentBridgeCommandRunner.CatalogPreview — handler calls
    /// Resolve() with the draft entity and optionally captures a screenshot.
    /// </summary>
    public class PreviewCatalog : MonoBehaviour
    {
        [SerializeField] private string _catalogEntryId;
        [SerializeField] private SpriteRenderer _previewRenderer;
        private bool _isResolved;

        public string CatalogEntryId => _catalogEntryId;
        public SpriteRenderer PreviewRenderer => _previewRenderer;
        public bool IsResolved => _isResolved;

        public void Resolve(CatalogEntity entry)
        {
            _catalogEntryId = entry.entity_id;
            _isResolved = true;
        }

        public void Reset()
        {
            _isResolved = false;
            if (_previewRenderer != null)
                _previewRenderer.sprite = null;
        }
    }
}
