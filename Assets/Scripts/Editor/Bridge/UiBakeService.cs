using Domains.UI.Data;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>
    /// Facade service for UI bake operations (Editor-only). Implements IUiBake.
    /// Delegates to BakeOrchestrator which routes through UiBakeHandler during hub-thin transition.
    /// TECH-31989 — wired Stage 3.0.
    /// </summary>
    public class UiBakeService : Domains.UI.Editor.UiBake.IUiBake
    {
        /// <summary>Bake UI prefabs from a panels.json snapshot path. Returns null on success; error JSON on failure.</summary>
        public string BakeFromSnapshot(string panelsPath, string outDir, string themeSoPath)
        {
            var args = new Territory.Editor.Bridge.UiBakeHandler.BakeArgs
            {
                panels_path = panelsPath,
                out_dir = outDir,
                theme_so = themeSoPath,
            };
            return new BakeOrchestrator(args).RunFromSnapshot();
        }

        /// <summary>Parse a panels.json snapshot. Returns null on success; error JSON on parse fault.</summary>
        public string ParseSnapshot(string snapshotJson)
        {
            var (_, err) = IrParser.ParsePanelSnapshot(snapshotJson);
            if (err != null)
                return UnityEngine.JsonUtility.ToJson(err);
            return null;
        }
    }
}
