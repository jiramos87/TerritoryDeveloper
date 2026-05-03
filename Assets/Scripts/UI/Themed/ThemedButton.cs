using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Audio;

namespace Territory.UI.Themed
{
    /// <summary>Themed Button chrome variant — palette + frame_style + sprite-state + motion-curve token consumer.</summary>
    public class ThemedButton : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _buttonImage;

        // Stage 1.3 (T1.3.3) — sprite-state + motion-curve extension fields.
        // Atlas resolve path: Inspector-bound 3-slot Sprites (path b per §Pending Decisions).
        // Slug surfaced for Stage 1.4 bake handler to populate; runtime reads sprites directly.
        [SerializeField] private string _spriteAtlasSlug;
        [SerializeField] private Sprite _spritePressed;
        [SerializeField] private Sprite _spriteHover;
        [SerializeField] private Sprite _spriteDisabled;
        [SerializeField] private string _motionCurveSlug;

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
            var button = GetComponent<UnityEngine.UI.Button>();
            if (theme.TryGetPalette(_paletteSlug, out var ramp) && ramp.ramp != null && ramp.ramp.Length > 0)
            {
                // Stage 12 Step 13 — paint button fill from the lightest ramp stop so the button
                // separates from the panel background (which uses ramp[1]). Ramp[0] = darkest.
                int fillIdx = ramp.ramp.Length - 1;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[fillIdx], out var c))
                {
                    _buttonImage.color = c;
                    if (button != null)
                    {
                        var colors = button.colors;
                        colors.normalColor = c;
                        // Stage 13.2 Bug 5 — selectedColor must equal normalColor so that
                        // after first click the button does NOT stay stuck at the highlight
                        // tint (Unity Selectable retains "selected" state until another
                        // Selectable receives focus; with selectedColor=highlightedColor the
                        // button reads as permanently hovered after click+exit).
                        colors.selectedColor = c;
                        if (ramp.ramp.Length >= 2 && ColorUtility.TryParseHtmlString(ramp.ramp[ramp.ramp.Length - 2], out var hover))
                        {
                            colors.highlightedColor = hover;
                        }
                        if (ramp.ramp.Length >= 3 && ColorUtility.TryParseHtmlString(ramp.ramp[ramp.ramp.Length - 3], out var pressed))
                        {
                            colors.pressedColor = pressed;
                        }
                        // Stage 1.3 (T1.3.3) — motion_curve_slug → fadeDuration (ms→s).
                        // Empty slug or miss → keep existing fadeDuration (Unity default).
                        if (!string.IsNullOrEmpty(_motionCurveSlug)
                            && theme.TryGetMotionCurve(_motionCurveSlug, out var curveSpec))
                        {
                            colors.fadeDuration = curveSpec.durationMs / 1000f;
                        }
                        button.colors = colors;
                    }
                }
            }
            // Stage 1.3 (T1.3.3) — SpriteState wiring from Inspector-bound 3-slot Sprites
            // (atlas-resolve path b per §Pending Decisions §1). Slug field reserved for Stage 1.4
            // bake-handler dispatch; runtime reads sprites directly. Graceful degrade — when all
            // 3 fields null, SpriteState assignment is a no-op visually (Selectable falls back to
            // colors-tint transition unless transition flips to SpriteSwap in the Inspector).
            if (button != null && (_spritePressed != null || _spriteHover != null || _spriteDisabled != null))
            {
                button.spriteState = new UnityEngine.UI.SpriteState
                {
                    pressedSprite = _spritePressed,
                    highlightedSprite = _spriteHover,
                    disabledSprite = _spriteDisabled,
                };
            }
            if (theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // FrameStyle currently models edge + innerShadowAlpha (no sprite ref); sprite swap lands later.
            }
        }
    }
}
