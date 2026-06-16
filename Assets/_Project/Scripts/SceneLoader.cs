using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    private CanvasGroup fadeCanvasGroup;
    private GameObject fadeOverlay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupFadeOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupFadeOverlay()
    {
        // Create a dedicated Canvas for the fade effect that persists
        GameObject canvasObj = new GameObject("SceneLoader_Canvas");
        canvasObj.transform.SetParent(this.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Ensure it's on top of everything

        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        fadeOverlay = new GameObject("FadeOverlay");
        fadeOverlay.transform.SetParent(canvasObj.transform, false);
        
        Image img = fadeOverlay.AddComponent<Image>();
        img.color = Color.black;
        
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        fadeCanvasGroup = fadeOverlay.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(false);
    }

    public void LoadScene(int buildIndex)
    {
        StartCoroutine(LoadSequence(buildIndex));
    }

    private IEnumerator LoadSequence(int buildIndex)
    {
        fadeOverlay.SetActive(true);

        // Fade In (to black)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        // Async Load
        AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
        while (!op.isDone)
        {
            yield return null;
        }

        // Small buffer to ensure scene initialization
        yield return new WaitForSecondsRealtime(0.1f);

        // Fade Out (to transparent)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeOverlay.SetActive(false);
    }
}
