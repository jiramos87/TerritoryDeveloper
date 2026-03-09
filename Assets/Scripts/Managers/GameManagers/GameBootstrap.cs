using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Territory.Persistence
{
/// <summary>
/// Runs in MainScene to handle game start intent from MainMenu.
/// Detects GameStartInfo (NewGame or Load) and invokes the appropriate
/// GameManager action after managers are ready.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private GameManager gameManager;
    private bool hasProcessed;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
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
