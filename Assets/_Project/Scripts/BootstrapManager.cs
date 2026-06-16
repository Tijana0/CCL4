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

        // 2. Load the Hub Scene (Index 1)
        SceneManager.LoadScene(1);
    }
}
