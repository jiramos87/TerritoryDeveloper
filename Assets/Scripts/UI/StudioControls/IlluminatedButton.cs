using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Territory.UI.StudioControls
{
    /// <summary>Illuminated push-button StudioControl variant; ApplyTheme resolves <see cref="UiTheme"/> illumination spec via cached slug.</summary>
    public class IlluminatedButton : StudioControlBase
    {
        [SerializeField] private IlluminatedButtonDetail _detail;

        /// <summary>Click surface — JuiceLayer + game-side listeners attach here. Stage 5 PulseOnEvent + SparkleBurst defaults wire to this event.</summary>
        [SerializeField] private UnityEvent _onClick = new UnityEvent();

        [SerializeField, Range(0f, 1f)] private float _illuminationAlpha = 1f;

        /// <inheritdoc />
        public override string Kind => "illuminated-button";

        // Sibling UnityEngine.UI.Button (when present from legacy bake or scene authoring)
        // drives its own Selectable transition (ColorTint) + navigation Selected state that
        // persists across pointer-exit after click → "hover-stick" (BUG-61 W9).
        // IlluminatedButtonRenderer already owns hover/press/click visuals via IPointer*Handler,
        // so neutralize the Selectable's transition + navigation here so the Button stays a
        // passive click receiver only (its onClick UnityEvent still fires through Renderer.OnPointerClick).
        protected override void Awake()
        {
            base.Awake();
            var legacyButton = GetComponent<Button>();
            if (legacyButton != null)
            {
                legacyButton.transition = Selectable.Transition.None;
                var nav = legacyButton.navigation;
                nav.mode = Navigation.Mode.None;
                legacyButton.navigation = nav;
            }
        }

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public IlluminatedButtonDetail Detail => _detail;

        /// <summary>Click event — runtime + JuiceLayer hook. Driven by external pointer / input source.</summary>
        public UnityEvent OnClick => _onClick;

        /// <summary>Illumination alpha [0,1]. JuiceLayer (PulseOnEvent) writes here; render layer reads.</summary>
        public float IlluminationAlpha
        {
            get => _illuminationAlpha;
            set => _illuminationAlpha = Mathf.Clamp01(value);
        }

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is IlluminatedButtonDetail bd)
            {
                _detail = bd;
                base.ApplyDetail(detail);
                return;
            }
            Debug.LogWarning($"[IlluminatedButton] ApplyDetail received non-IlluminatedButtonDetail row (slug={Slug})");
        }

        /// <inheritdoc />
        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _detail == null) return;
            if (string.IsNullOrEmpty(_detail.illuminationSlug)) return;
            if (!theme.TryGetIllumination(_detail.illuminationSlug, out _))
            {
                Debug.LogWarning($"[IlluminatedButton] illumination slug not found (slug={Slug}, illuminationSlug={_detail.illuminationSlug})");
            }
            // IlluminationSpec carries color + halo radius; runtime sprite/halo binding deferred to Stage 5.
        }
    }

    /// <summary>Typed detail row for <see cref="IlluminatedButton"/>.</summary>
    [Serializable]
    public class IlluminatedButtonDetail : IDetailRow
    {
        public string illuminationSlug;
        public string pulseOnEvent;
        // Stage 12 Step 16.D — bake-time icon sprite resolution.
        // Bake handler loads `Assets/Sprites/Buttons/{iconSpriteSlug}-target.png` (with fallback
        // to `Assets/Sprites/{iconSpriteSlug}-target.png`) via AssetDatabase + assigns to icon Image.
        // Empty/null = no icon (legacy flat-color body).
        public string iconSpriteSlug;
    }
}
