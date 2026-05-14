using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Territory.Persistence
{
/// <summary>
/// Runs in CityScene → handle game start intent from MainMenu. Detects <see cref="GameStartInfo"/>
/// (NewGame or Load) + invokes appropriate <see cref="GameManager"/> action after managers ready.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private bool hasProcessed;

    void Start()
    {
        // iter-18 (Effort 1 fix-up) — CityScene Inspector wiring drift leaves gameManager
        // null; resolve via FindObjectOfType so the boot path actually fires
        // CreateNewGame and the city starts with the default $20,000 treasury.
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
            Debug.LogWarning("[GameBootstrap] gameManager inspector ref not wired AND FindObjectOfType returned null — wire in CityScene.");
        StartCoroutine(ProcessStartIntent());
    }

    private IEnumerator ProcessStartIntent()
    {
        yield return null;

        if (hasProcessed || gameManager == null)
            yield break;

        if (GameStartInfo.Mode == GameStartInfo.StartMode.Load && !string.IsNullOrEmpty(GameStartInfo.PendingLoadPath))
        {
            if (System.IO.File.Exists(GameStartInfo.PendingLoadPath))
            {
                Debug.Log($"[GameBootstrap] Load mode → {GameStartInfo.PendingLoadPath}");
                gameManager.LoadGame(GameStartInfo.PendingLoadPath);
            }
        }
        else if (GameStartInfo.Mode == GameStartInfo.StartMode.NewGame)
        {
            Debug.Log("[GameBootstrap] NewGame mode → gameManager.CreateNewGame()");
            gameManager.CreateNewGame();
        }
        else
        {
            // iter-18 — Direct CityScene open from Editor (no MainMenu hand-off) leaves
            // GameStartInfo.Mode == None. Default to NewGame so CityStats.ResetCityStats
            // runs and money seeds at the default $20,000.
            Debug.Log("[GameBootstrap] No start mode set → defaulting to NewGame (Editor direct-open path).");
            gameManager.CreateNewGame();
        }

        GameStartInfo.Clear();
        hasProcessed = true;
    }
}
}
