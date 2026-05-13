using System;
using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Themed title row archetype — composes title + optional sub-title <see cref="ThemedLabel"/> children.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined; USS section-header class replaces.</remarks>
    [Obsolete("ThemedSectionHeader quarantined (TECH-32929). Use USS section-header class / UI Toolkit Label. Deletion deferred to uGUI purge plan.")]
    public class ThemedSectionHeader : ThemedPrimitiveBase
    {
        [SerializeField] private ThemedLabel _titleLabel;
        [SerializeField] private ThemedLabel _subTitleLabel;

        /// <summary>Title text proxy → child <c>_titleLabel.Detail</c>; null-guard returns <see cref="string.Empty"/>.</summary>
        public string Title
        {
            get => _titleLabel != null ? _titleLabel.Detail : string.Empty;
            set { if (_titleLabel != null) _titleLabel.Detail = value; }
        }

        /// <summary>Sub-title text proxy → child <c>_subTitleLabel.Detail</c>; null-guard returns <see cref="string.Empty"/>.</summary>
        public string SubTitle
        {
            get => _subTitleLabel != null ? _subTitleLabel.Detail : string.Empty;
            set { if (_subTitleLabel != null) _subTitleLabel.Detail = value; }
        }

        /// <summary>True when sub-title slot wired in Inspector.</summary>
        public bool HasSubTitle => _subTitleLabel != null;

        // No ApplyTheme override — base no-op delegates; child labels apply own theme on Awake.
    }
}
