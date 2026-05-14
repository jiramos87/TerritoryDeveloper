using System.Collections.Generic;
using Territory.Audio;
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
        const string CardSpriteClassPrefix = "tool-subtype-picker__tier-card--";

        [SerializeField] UIDocument _doc;

        ToolSubtypePickerVM _vm;
        VisualElement _root;
        Button _card0, _card1, _card2;
        Label _label0, _label1, _label2;
        string _parentSlug;
        readonly string[] _activeSpriteClasses = new string[3];

        // Mapping parent-tool slug → N tier slug+label tuples. Iter-5: variable card count
        // per parent — show only as many cards as the parent has distinct subtypes.
        struct Tier { public string Slug; public string Label; }
        static readonly Dictionary<string, Tier[]> TiersByParent = new()
        {
            { "zone-r",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Dense" } } },
            { "zone-c",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Dense" } } },
            { "zone-i",          new[] { new Tier{ Slug="light", Label="Light" }, new Tier{ Slug="medium", Label="Medium" }, new Tier{ Slug="heavy", Label="Heavy" } } },
            // Single-variant families — picker shows just one card with the grid-tile sprite.
            { "services",        new[] { new Tier{ Slug="default", Label="Service" } } },
            { "road",            new[] { new Tier{ Slug="default", Label="Road" } } },
            { "building-power",  new[] { new Tier{ Slug="default", Label="Power" } } },
            { "building-water",  new[] { new Tier{ Slug="default", Label="Water" } } },
            { "landmark",        new[] { new Tier{ Slug="default", Label="Forest" } } },
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

            var rootEl = _doc.rootVisualElement;
            rootEl.style.position = Position.Absolute;
            rootEl.style.top = 0;
            rootEl.style.left = 0;
            rootEl.style.right = 0;
            rootEl.style.bottom = 0;
            rootEl.pickingMode = PickingMode.Ignore;
            _root = rootEl.Q<VisualElement>("tool-subtype-picker");
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

            // iter-22 (Effort 2) — hover + click blips on each tier card.
            ToolkitBlipBinder.BindClickAndHover(_card0, BlipId.UiButtonClick, BlipId.UiButtonHover);
            ToolkitBlipBinder.BindClickAndHover(_card1, BlipId.UiButtonClick, BlipId.UiButtonHover);
            ToolkitBlipBinder.BindClickAndHover(_card2, BlipId.UiButtonClick, BlipId.UiButtonHover);

            Hide();
        }

        void OnDisable()
        {
            ToolkitBlipBinder.UnbindAll(_card0);
            ToolkitBlipBinder.UnbindAll(_card1);
            ToolkitBlipBinder.UnbindAll(_card2);
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        /// <summary>Open the picker populated with the tier set for the given parent tool slug.</summary>
        public void Open(string parentSlug)
        {
            _parentSlug = parentSlug;
            if (!TiersByParent.TryGetValue(parentSlug, out var tiers))
                tiers = new[] { new Tier { Slug = "default", Label = "Place" } };

            var cards  = new[] { _card0,  _card1,  _card2  };
            var labels = new[] { _label0, _label1, _label2 };
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                bool show = i < tiers.Length;
                cards[i].style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show && labels[i] != null) labels[i].text = tiers[i].Label;
            }
            ApplySpriteClasses(parentSlug, tiers);
            ClearActive();
            if (_root != null)
            {
                _root.AddToClassList(OpenClass);
                _root.style.display = DisplayStyle.Flex;
            }
        }

        void ApplySpriteClasses(string parentSlug, Tier[] tiers)
        {
            var cards = new[] { _card0, _card1, _card2 };
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                if (!string.IsNullOrEmpty(_activeSpriteClasses[i]))
                    cards[i].RemoveFromClassList(_activeSpriteClasses[i]);
                string cls = i < tiers.Length
                    ? CardSpriteClassPrefix + parentSlug + "-" + tiers[i].Slug
                    : string.Empty;
                if (!string.IsNullOrEmpty(cls))
                    cards[i].AddToClassList(cls);
                _activeSpriteClasses[i] = cls;
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

        /// <summary>iter-14 (Effort 1 §16.1 fix-up) — picker remains open across tier confirms;
        /// HandleEscapePress queries this before popping other Esc frames.</summary>
        public bool IsOpen => _root != null && _root.style.display.value == DisplayStyle.Flex;

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
            // iter-14 (Effort 1 §16.1 fix-up) — picker stays open after tier confirm;
            // close only via Esc (HandleEscapePress consults SubTypePicker frame).
            ApplyTier(_parentSlug, tierSlug);
        }

        void ApplyTier(string parentSlug, string tierSlug)
        {
            var uim = FindObjectOfType<UIManager>();
            if (uim == null) return;
            // Iter-3: route to the existing UIManager tier-click handlers so the
            // active zone-type / ghost-preview / tool-selected channel actually fires.
            switch ((parentSlug, tierSlug))
            {
                case ("zone-r", "light"):  uim.OnLightResidentialButtonClicked();  break;
                case ("zone-r", "medium"): uim.OnMediumResidentialButtonClicked(); break;
                case ("zone-r", "heavy"):  uim.OnHeavyResidentialButtonClicked();  break;
                case ("zone-c", "light"):  uim.OnLightCommercialButtonClicked();   break;
                case ("zone-c", "medium"): uim.OnMediumCommercialButtonClicked();  break;
                case ("zone-c", "heavy"):  uim.OnHeavyCommercialButtonClicked();   break;
                case ("zone-i", "light"):  uim.OnLightIndustrialButtonClicked();   break;
                case ("zone-i", "medium"): uim.OnMediumIndustrialButtonClicked();  break;
                case ("zone-i", "heavy"):  uim.OnHeavyIndustrialButtonClicked();   break;
                case ("services", _):       uim.OnStateServiceZoningButtonClicked(); break;
                case ("road", _):           uim.OnTwoWayRoadButtonClicked();         break;
                case ("building-power", _): uim.OnNuclearPowerPlantButtonClicked();  break;
                case ("building-water", _): uim.OnWaterFamilyButtonClicked();        break;
                case ("landmark", _):       uim.OnForestsFamilyButtonClicked();      break;
                case ("bulldoze", _):       uim.OnBulldozeButtonClicked();           break;
            }
        }
    }
}
