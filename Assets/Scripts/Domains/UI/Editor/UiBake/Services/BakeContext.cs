using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Context bag passed to baker POCOs. Extracted from UiBakeHandler (TECH-31977).
    /// Editor-only — lives under UI.Editor.asmdef.</summary>
    public class BakeContext
    {
        /// <summary>Accumulates theme-ref sink GameObjects for post-bake WireThemeRef pass.</summary>
        public IList<GameObject> ThemeRefSink { get; set; }

        /// <summary>Repo root path for asset resolution.</summary>
        public string RepoRoot { get; set; }

        /// <summary>Warning log sink — non-fatal bake issues.</summary>
        public Action<string> WarningSink { get; set; }

        /// <summary>Audit log sink — informational bake trace.</summary>
        public Action<string> AuditSink { get; set; }
    }
}
