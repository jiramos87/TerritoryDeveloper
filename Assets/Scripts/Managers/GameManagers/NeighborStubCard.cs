using UnityEngine;
using TMPro;

namespace Territory.UI
{
    /// <summary>
    /// View-binder for one inert neighbor-stub card under
    /// <see cref="NeighborCityStubPanel"/>. Holds two TMP labels (display name
    /// + border direction). No input handling, no live data refresh — pure
    /// display surface populated by the panel via <see cref="SetDisplayName"/>
    /// + <see cref="SetBorder"/>.
    /// </summary>
    public class NeighborStubCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text _displayNameLabel;
        [SerializeField] private TMP_Text _borderLabel;

        public void SetDisplayName(string text)
        {
            if (_displayNameLabel != null)
                _displayNameLabel.text = text;
        }

        public void SetBorder(string text)
        {
            if (_borderLabel != null)
                _borderLabel.text = text;
        }

        /// <summary>
        /// Build TMP children at runtime when the card was instantiated without a
        /// prefab (programmatic creation path inside <see cref="NeighborCityStubPanel"/>).
        /// Prefab path leaves serialized field refs intact; runtime path needs
        /// this bootstrap.
        /// </summary>
        public void BootstrapRuntimeChildren()
        {
            if (_displayNameLabel == null)
                _displayNameLabel = CreateLabelChild("DisplayNameLabel", 14);
            if (_borderLabel == null)
                _borderLabel = CreateLabelChild("BorderLabel", 12);
        }

        private TMP_Text CreateLabelChild(string childName, int fontSize)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 18);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.text = string.Empty;
            return label;
        }
    }
}
