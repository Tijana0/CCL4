using UnityEngine;

public class StartScreen : MonoBehaviour
{
    public void OnStartPressed()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(2);   // 2 = HubScene
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }
}