using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Territory.SceneManagement
{
    /// <summary>CoreScene hub — owns additive scene load/unload + allowSceneActivation gate. Invariant #3: resolve in Start, never Update.</summary>
    public class SceneOrchestratorManager : MonoBehaviour
    {
        /// <summary>Load scene additive. Returns when scene is ready (activation pending).</summary>
        public async Task<AsyncOperation> LoadAdditive(string sceneName, CancellationToken ct)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneOrchestratorManager] LoadAdditive: scene '{sceneName}' not found in build settings.");
                return null;
            }
            op.allowSceneActivation = false;
            while (!ct.IsCancellationRequested && op.progress < 0.9f)
                await Task.Yield();
            return op;
        }

        /// <summary>Activate a loaded-but-held scene (sets allowSceneActivation=true).</summary>
        public async Task Activate(AsyncOperation op)
        {
            if (op == null) return;
            op.allowSceneActivation = true;
            while (!op.isDone)
                await Task.Yield();
        }

        /// <summary>Unload scene by name asynchronously.</summary>
        public async Task Unload(string sceneName, CancellationToken ct)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null) return;
            while (!ct.IsCancellationRequested && !op.isDone)
                await Task.Yield();
        }
    }
}
