using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponentInChildren<Button>();
        if (btn == null) btn = Object.FindFirstObjectByType<Button>();
        if (btn != null)
        {
            // Configure colors for high visibility when selected
            var colors = btn.colors;
            colors.selectedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
            colors.highlightedColor = new Color(1f, 0.8f, 0.2f, 1f);
            btn.colors = colors;

            btn.Select();
        }
    }

    public void OnStartPressed()
    {
        AkUnitySoundEngine.PostEvent("Play_Level_Start", gameObject);
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(2);   // 2 = HubScene
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }
}