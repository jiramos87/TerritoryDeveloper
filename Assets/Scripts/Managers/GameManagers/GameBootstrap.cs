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
        if (gameManager == null) Debug.LogWarning("[GameBootstrap] gameManager inspector ref not wired — wire in CityScene.");
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
                gameManager.LoadGame(GameStartInfo.PendingLoadPath);
            }
        }
        else if (GameStartInfo.Mode == GameStartInfo.StartMode.NewGame)
        {
            gameManager.CreateNewGame();
        }

        GameStartInfo.Clear();
        hasProcessed = true;
    }
}
}
