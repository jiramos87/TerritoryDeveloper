using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Themed UI primitive contract — single hook to apply a <see cref="UiTheme"/> token snapshot.</summary>
    public interface IThemed
    {
        /// <summary>Apply token slugs from <paramref name="theme"/>; implementations null-guard accessor returns.</summary>
        void ApplyTheme(UiTheme theme);
    }
}
