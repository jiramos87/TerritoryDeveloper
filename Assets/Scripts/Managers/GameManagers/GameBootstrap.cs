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
        // CityScene Inspector wiring drift leaves gameManager null; resolve via
        // FindObjectOfType so the boot path actually fires CreateNewGame and the
        // city starts with the default $20,000 treasury.
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
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
                gameManager.LoadGame(GameStartInfo.PendingLoadPath);
        }
        else if (GameStartInfo.Mode == GameStartInfo.StartMode.NewGame)
        {
            gameManager.CreateNewGame();
        }
        else
        {
            // Direct CityScene open from Editor (no MainMenu hand-off) leaves
            // Mode == None. Default to NewGame so CityStats.ResetCityStats runs
            // and money seeds at the default $20,000.
            gameManager.CreateNewGame();
        }

        GameStartInfo.Clear();
        hasProcessed = true;
    }
}
}
