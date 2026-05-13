using Domains.UI.Data;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>POCO orchestrator for UI bake pipeline. Extracted from UiBakeHandler (TECH-31985).
    /// Constructor takes BakeArgs; Run() and RunFromSnapshot() delegate to hub during transition.</summary>
    public class BakeOrchestrator
    {
        readonly BakeArgs _args;

        public BakeOrchestrator(BakeArgs args)
        {
            _args = args;
        }

        /// <summary>Run the full bake pipeline (panels_path → prefab write per panel).
        /// Delegates to UiBakeHandler.Bake during hub-thin transition. Returns null on success; error JSON on failure.</summary>
        public string Run()
        {
            if (_args == null)
                return "{\"ok\":false,\"error\":\"missing_arg\",\"details\":\"args\",\"path\":\"$\"}";

            var result = Territory.Editor.Bridge.UiBakeHandler.Bake(_args);
            if (result == null)
                return "{\"ok\":false,\"error\":\"null_result\",\"details\":\"bake_returned_null\",\"path\":\"$\"}";
            if (result.error != null)
                return UnityEngine.JsonUtility.ToJson(result.error);

            return null;
        }

        /// <summary>Run bake from panel snapshot path. Delegates to UiBakeHandler.BakeFromPanelSnapshot.</summary>
        public string RunFromSnapshot()
        {
            if (_args == null)
                return "{\"ok\":false,\"error\":\"missing_arg\",\"details\":\"args\",\"path\":\"$\"}";

            var result = Territory.Editor.Bridge.UiBakeHandler.BakeFromPanelSnapshot(_args);
            if (result == null)
                return "{\"ok\":false,\"error\":\"null_result\",\"details\":\"bake_returned_null\",\"path\":\"$\"}";
            if (result.error != null)
                return UnityEngine.JsonUtility.ToJson(result.error);

            return null;
        }
    }
}
