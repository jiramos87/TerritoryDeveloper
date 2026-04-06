using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Serializable color and typography tokens for uGUI menus and HUD.
    /// Assign a single asset instance from scenes or coordinators; extend fields as panels migrate off Inspector literals.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Territory/UI/Ui Theme", order = 0)]
    public class UiTheme : ScriptableObject
    {
        [Header("Primary actions")]
        [SerializeField] private Color primaryButtonColor = new Color(0.157f, 0.173f, 0.208f, 1f);
        [SerializeField] private Color primaryButtonTextColor = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private int primaryButtonFontSize = 18;

        [Header("Menu (MainMenu strip)")]
        [SerializeField] private Color menuButtonColor = new Color(0.157f, 0.173f, 0.208f, 1f);
        [SerializeField] private Color menuButtonTextColor = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private int menuButtonFontSize = 18;

        [Header("Surfaces (city HUD)")]
        [Tooltip("Deepest chrome: fullscreen tint base.")]
        [SerializeField] private Color surfaceBase = new Color(0.0667f, 0.0745f, 0.0941f, 1f);
        [Tooltip("HUD / popup card backgrounds (includes alpha for map bleed-through).")]
        [SerializeField] private Color surfaceCardHud = new Color(0.11f, 0.122f, 0.149f, 0.88f);
        [Tooltip("Toolbar strip background (slightly more opaque than HUD cards).")]
        [SerializeField] private Color surfaceToolbar = new Color(0.0667f, 0.0745f, 0.0941f, 0.94f);
        [Tooltip("Elevated controls: active tool, tooltips, dropdowns.")]
        [SerializeField] private Color surfaceElevated = new Color(0.157f, 0.173f, 0.208f, 1f);
        [Tooltip("1 px dividers and subtle panel edges.")]
        [SerializeField] private Color borderSubtle = new Color(0.18f, 0.2f, 0.251f, 1f);

        [Header("Text")]
        [SerializeField] private Color textPrimary = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private Color textSecondary = new Color(0.545f, 0.561f, 0.643f, 1f);

        [Header("Accents")]
        [SerializeField] private Color accentPrimary = new Color(0.29f, 0.62f, 1f, 1f);
        [SerializeField] private Color accentPositive = new Color(0.204f, 0.78f, 0.349f, 1f);
        [SerializeField] private Color accentNegative = new Color(1f, 0.271f, 0.227f, 1f);

        [Header("Modal")]
        [Tooltip("Fullscreen dimmer behind popups.")]
        [SerializeField] private Color modalDimmerColor = new Color(0f, 0f, 0f, 0.667f);

        [Header("Typography (legacy Text)")]
        [SerializeField] private int fontSizeDisplay = 28;
        [SerializeField] private int fontSizeHeading = 18;
        [SerializeField] private int fontSizeBody = 14;
        [SerializeField] private int fontSizeCaption = 11;

        [Header("Spacing (px, reference for layout)")]
        [SerializeField] private int spacingUnit = 4;
        [SerializeField] private int panelPadding = 16;

        public Color PrimaryButtonColor => primaryButtonColor;
        public Color PrimaryButtonTextColor => primaryButtonTextColor;
        public int PrimaryButtonFontSize => primaryButtonFontSize;
        public Color MenuButtonColor => menuButtonColor;
        public Color MenuButtonTextColor => menuButtonTextColor;
        public int MenuButtonFontSize => menuButtonFontSize;

        public Color SurfaceBase => surfaceBase;
        public Color SurfaceCardHud => surfaceCardHud;
        public Color SurfaceToolbar => surfaceToolbar;
        public Color SurfaceElevated => surfaceElevated;
        public Color BorderSubtle => borderSubtle;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color AccentPrimary => accentPrimary;
        public Color AccentPositive => accentPositive;
        public Color AccentNegative => accentNegative;
        public Color ModalDimmerColor => modalDimmerColor;
        public int FontSizeDisplay => fontSizeDisplay;
        public int FontSizeHeading => fontSizeHeading;
        public int FontSizeBody => fontSizeBody;
        public int FontSizeCaption => fontSizeCaption;
        public int SpacingUnit => spacingUnit;
        public int PanelPadding => panelPadding;
    }
}
