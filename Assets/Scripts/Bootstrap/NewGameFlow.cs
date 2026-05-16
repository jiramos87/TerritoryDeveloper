using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Territory.Managers;
using Territory.Persistence;
using Domains.Registry;

namespace Territory.Bootstrap
{
    /// <summary>CoreScene new-game orchestrator. Calls SaveCoordinator.CreatePair after world generation so both .city + .region files exist from game-start. Invariant #3: resolve deps in Start.</summary>
    public class NewGameFlow : MonoBehaviour
    {
        private ISaveCoordinator _saveCoordinator;

        [Tooltip("Save slot id used for the initial paired files.")]
        public string InitialSaveId = "newgame";

        void Start()
        {
            _saveCoordinator = FindObjectOfType<SaveCoordinator>();
        }

        /// <summary>Call after world generation completes. Creates initial .city + .region pair on disk.</summary>
        public async Task StartNew(CancellationToken ct = default)
        {
            if (_saveCoordinator == null)
            {
                Debug.LogWarning("[NewGameFlow] ISaveCoordinator not found — skipping CreatePair.");
                return;
            }

            try
            {
                await _saveCoordinator.CreatePair(InitialSaveId, ct);
                Debug.Log($"[NewGameFlow] CreatePair succeeded for '{InitialSaveId}'.");
            }
            catch (SaveFailedException ex)
            {
                Debug.LogError($"[NewGameFlow] CreatePair failed: {ex.Message}");
            }
        }
    }
}
