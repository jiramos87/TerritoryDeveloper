using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Audio;

namespace Territory.UI.Themed
{
    /// <summary>Themed Button chrome variant — palette + frame_style token consumer.</summary>
    public class ThemedButton : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _buttonImage;

        public event System.Action OnClicked;

        protected override void Awake()
        {
            base.Awake();
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    BlipEngine.Play(BlipId.UiButtonClick);
                    OnClicked?.Invoke();
                });
            }

            // Stage 12 Step 13D — hover blip parity with MainMenu buttons.
            var trigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => BlipEngine.Play(BlipId.UiButtonHover));
            trigger.triggers.Add(entry);
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _buttonImage == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp) && ramp.ramp != null && ramp.ramp.Length > 0)
            {
                // Stage 12 Step 13 — paint button fill from the lightest ramp stop so the button
                // separates from the panel background (which uses ramp[1]). Ramp[0] = darkest.
                int fillIdx = ramp.ramp.Length - 1;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[fillIdx], out var c))
                {
                    _buttonImage.color = c;
                    var button = GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        var colors = button.colors;
                        colors.normalColor = c;
                        if (ramp.ramp.Length >= 2 && ColorUtility.TryParseHtmlString(ramp.ramp[ramp.ramp.Length - 2], out var hover))
                        {
                            colors.highlightedColor = hover;
                            colors.selectedColor = hover;
                        }
                        if (ramp.ramp.Length >= 3 && ColorUtility.TryParseHtmlString(ramp.ramp[ramp.ramp.Length - 3], out var pressed))
                        {
                            colors.pressedColor = pressed;
                        }
                        button.colors = colors;
                    }
                }
            }
            if (theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // FrameStyle currently models edge + innerShadowAlpha (no sprite ref); sprite swap lands later.
            }
        }
    }
}
