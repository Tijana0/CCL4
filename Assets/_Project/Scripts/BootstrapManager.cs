using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapManager : MonoBehaviour
{
    private void Start()
    {
        // 1. Initialize Persistent Game State
        if (PersistentGameState.Instance == null)
        {
            GameObject pgs = new GameObject("PersistentGameState");
            pgs.AddComponent<PersistentGameState>();
        }

        // 2. Initialize Scene Loader
        if (SceneLoader.Instance == null)
        {
            GameObject sl = new GameObject("SceneLoader");
            sl.AddComponent<SceneLoader>();
        }

        // 3. Load the Hub Scene (Index 1)
        SceneLoader.Instance.LoadScene(1);
    }
}
