using System.Collections.Generic;
using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for the HUD-anchored tool-subtype-picker. NOT a modal — peer to the
    /// toolbar/time-controls strip. Bottom-left, ~320×96, 3 tier cards. ToolbarHost invokes
    /// <see cref="Open"/> directly; this Host stays out of ModalCoordinator's pause-owner
    /// path so picking a zone tier doesn't pause the game (Goal D real-screenshot correction
    /// from the recovery plan).
    /// </summary>
    public sealed class ToolSubtypePickerHost : MonoBehaviour
    {
        const string OpenClass = "is-open";
        const string CardActiveClass = "tool-subtype-picker__tier-card--active";

        [SerializeField] UIDocument _doc;

        ToolSubtypePickerVM _vm;
        VisualElement _root;
        Button _card0, _card1, _card2;
        Label _label0, _label1, _label2;
        string _parentSlug;

        // Mapping parent-tool slug → 3 tier slug+label triples. Plan §6 Phase D2 step 5.
        struct Tier { public string Slug; public string Label; }
        static readonly Dictionary<string, Tier[]> TiersByParent = new()
        {
            { "zone-r",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Dense" } } },
            { "zone-c",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Dense" } } },
            { "zone-i",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Heavy" } } },
            { "services",        new[] { new Tier{ Slug="police", Label="Police" }, new Tier{ Slug="fire", Label="Fire" }, new Tier{ Slug="health", Label="Health" } } },
            { "road",            new[] { new Tier{ Slug="street", Label="Street" }, new Tier{ Slug="avenue", Label="Avenue" }, new Tier{ Slug="highway", Label="Highway" } } },
            { "building-power",  new[] { new Tier{ Slug="coal", Label="Coal" }, new Tier{ Slug="gas", Label="Gas" }, new Tier{ Slug="solar", Label="Solar" } } },
            { "building-water",  new[] { new Tier{ Slug="pump", Label="Pump" }, new Tier{ Slug="tower", Label="Tower" }, new Tier{ Slug="treatment", Label="Treatment" } } },
            { "landmark",        new[] { new Tier{ Slug="park", Label="Park" }, new Tier{ Slug="plaza", Label="Plaza" }, new Tier{ Slug="monument", Label="Monument" } } },
        };

        void OnEnable()
        {
            _vm = new ToolSubtypePickerVM();
            _vm.CloseCommand = Hide;

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[ToolSubtypePickerHost] UIDocument or rootVisualElement null on enable.");
                return;
            }

            _root = _doc.rootVisualElement.Q<VisualElement>("tool-subtype-picker");
            _doc.rootVisualElement.SetCompatDataSource(_vm);

            _card0 = _doc.rootVisualElement.Q<Button>("tier-card-0");
            _card1 = _doc.rootVisualElement.Q<Button>("tier-card-1");
            _card2 = _doc.rootVisualElement.Q<Button>("tier-card-2");
            _label0 = _doc.rootVisualElement.Q<Label>("tier-card-0-label");
            _label1 = _doc.rootVisualElement.Q<Label>("tier-card-1-label");
            _label2 = _doc.rootVisualElement.Q<Label>("tier-card-2-label");

            if (_card0 != null) _card0.clicked += () => OnTierPicked(0);
            if (_card1 != null) _card1.clicked += () => OnTierPicked(1);
            if (_card2 != null) _card2.clicked += () => OnTierPicked(2);

            Hide();
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        /// <summary>Open the picker populated with the tier set for the given parent tool slug.</summary>
        public void Open(string parentSlug)
        {
            _parentSlug = parentSlug;
            if (!TiersByParent.TryGetValue(parentSlug, out var tiers))
            {
                // Default 1-tier "Place" card row when parent has no defined subtypes.
                tiers = new[]
                {
                    new Tier { Slug = "default", Label = "Place" },
                    new Tier { Slug = "alt-1",   Label = "Alt 1" },
                    new Tier { Slug = "alt-2",   Label = "Alt 2" },
                };
            }
            if (_label0 != null) _label0.text = tiers[0].Label;
            if (_label1 != null) _label1.text = tiers[1].Label;
            if (_label2 != null) _label2.text = tiers[2].Label;
            ClearActive();
            if (_root != null)
            {
                _root.AddToClassList(OpenClass);
                _root.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>Hide the picker without selecting.</summary>
        public void Hide()
        {
            if (_root != null)
            {
                _root.RemoveFromClassList(OpenClass);
                _root.style.display = DisplayStyle.None;
            }
            _parentSlug = null;
        }

        void ClearActive()
        {
            _card0?.RemoveFromClassList(CardActiveClass);
            _card1?.RemoveFromClassList(CardActiveClass);
            _card2?.RemoveFromClassList(CardActiveClass);
        }

        void OnTierPicked(int idx)
        {
            ClearActive();
            switch (idx)
            {
                case 0: _card0?.AddToClassList(CardActiveClass); break;
                case 1: _card1?.AddToClassList(CardActiveClass); break;
                case 2: _card2?.AddToClassList(CardActiveClass); break;
            }
            string tierSlug = "";
            if (TiersByParent.TryGetValue(_parentSlug ?? "", out var tiers) && idx >= 0 && idx < tiers.Length)
                tierSlug = tiers[idx].Slug;
            ApplyTier(_parentSlug, tierSlug);
            Hide();
        }

        void ApplyTier(string parentSlug, string tierSlug)
        {
            var uim = FindObjectOfType<UIManager>();
            if (uim == null) return;
            // Pre-migration parity: route through UIManager subtype helpers when the family
            // matches; otherwise drop into the catalog-driven path. Detailed dispatch lands
            // when ToolService gets a public surface (DEC-A28 follow-up).
            switch (parentSlug)
            {
                case "bulldoze":
                    uim.bulldozeMode = true;
                    break;
                default:
                    // Stash the picked tier on the existing slot so consumers can read it.
                    int hash = (parentSlug + ":" + tierSlug).GetHashCode();
                    uim.SetCurrentSubTypeId(System.Math.Abs(hash) % 100000);
                    break;
            }
        }
    }
}
