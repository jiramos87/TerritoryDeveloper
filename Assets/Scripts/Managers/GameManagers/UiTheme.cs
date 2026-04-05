using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Serializable color and typography tokens for uGUI menus and HUD rows.
    /// Assign a single asset instance from scenes or coordinators; extend fields as panels migrate off Inspector literals.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Territory/UI/Ui Theme", order = 0)]
    public class UiTheme : ScriptableObject
    {
        [Header("Primary actions")]
        [SerializeField] private Color primaryButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color primaryButtonTextColor = Color.white;
        [SerializeField] private int primaryButtonFontSize = 18;

        [Header("Menu (MainMenu strip)")]
        [SerializeField] private Color menuButtonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color menuButtonTextColor = Color.white;
        [SerializeField] private int menuButtonFontSize = 18;

        public Color PrimaryButtonColor => primaryButtonColor;
        public Color PrimaryButtonTextColor => primaryButtonTextColor;
        public int PrimaryButtonFontSize => primaryButtonFontSize;
        public Color MenuButtonColor => menuButtonColor;
        public Color MenuButtonTextColor => menuButtonTextColor;
        public int MenuButtonFontSize => menuButtonFontSize;
    }
}
