using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool isPaused = false;
    private GameObject pausePanel;
    private GameObject firstSelectedButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        CreatePauseUI();
    }

    private void CreatePauseUI()
    {
        // Find existing canvas or create one
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PauseCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Panel
        pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = pausePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Dark semi-transparent background

        // Title Text
        GameObject titleObj = new GameObject("PauseTitle");
        titleObj.transform.SetParent(pausePanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(400, 100);

        // Resume Button
        Button resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0.5f, 0.5f));
        resumeBtn.onClick.AddListener(ResumeGame);
        firstSelectedButton = resumeBtn.gameObject;

        // Hub Button
        Button hubBtn = CreateButton(pausePanel.transform, "HubButton", "Return to Hub", new Vector2(0.5f, 0.35f));
        hubBtn.onClick.AddListener(ReturnToHub);

        // Setup Explicit Navigation so they know they are above/below each other
        Navigation resNav = resumeBtn.navigation;
        resNav.mode = Navigation.Mode.Explicit;
        resNav.selectOnDown = hubBtn;
        resumeBtn.navigation = resNav;

        Navigation hubNav = hubBtn.navigation;
        hubNav.mode = Navigation.Mode.Explicit;
        hubNav.selectOnUp = resumeBtn;
        hubBtn.navigation = hubNav;

        pausePanel.SetActive(false);
    }

    private Button CreateButton(Transform parent, string name, string text, Vector2 anchor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchor;
        btnRect.anchorMax = anchor;
        btnRect.sizeDelta = new Vector2(300, 60);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        Button button = btnObj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 32;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private void Update()
    {
        bool pausePressed = false;

        // Keyboard
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
        {
            pausePressed = true;
        }

        // Gamepad (Check all to support split screen or any active controller)
        foreach (var device in InputSystem.devices)
        {
            if (device is Gamepad g && g.startButton.wasPressedThisFrame)
            {
                pausePressed = true;
            }
            
            // Ultra-permissive alias checking for third-party macOS gamepads
            if (CheckButton(device, new string[] { "start", "button9", "options", "menu" }))
            {
                pausePressed = true;
            }
        }

        if (pausePressed)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    private bool CheckButton(InputDevice device, string[] aliases)
    {
        bool isDown = false;
        foreach (var control in device.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl b)
            {
                if (b.wasPressedThisFrame)
                {
                    string cName = control.name.ToLower();
                    foreach (var alias in aliases)
                    {
                        if (cName == alias.ToLower()) isDown = true;
                    }
                }
            }
        }
        return isDown;
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        if (firstSelectedButton != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ReturnToHub()
    {
        ResumeGame();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToHub();
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}
