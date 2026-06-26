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
    private SettingsMenu settingsMenu;

    // Constant button-alias set, hoisted out of the per-frame check to avoid array allocations.
    private static readonly string[] PauseButtonAliases = { "start", "button9", "options", "menu" };

    // Shared gold accent colour (matches SettingsMenu / order cards).
    private static readonly Color Gold = new Color(0.88f, 0.66f, 0.28f);

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
        panelImage.color = new Color(0.04f, 0.03f, 0.07f, 0.88f); // deep purple-black dim, matches game theme

        // Gold-edged card framing the menu (decorative, behind title + buttons)
        GameObject edge = new GameObject("MenuEdge");
        edge.transform.SetParent(pausePanel.transform, false);
        Image edgeImg = edge.AddComponent<Image>();
        edgeImg.color = Gold;
        RectTransform edgeRect = edge.GetComponent<RectTransform>();
        edgeRect.anchorMin = new Vector2(0.5f, 0.5f); edgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        edgeRect.sizeDelta = new Vector2(388, 368); edgeRect.anchoredPosition = new Vector2(0, 5);

        GameObject card = new GameObject("MenuCard");
        card.transform.SetParent(pausePanel.transform, false);
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.11f, 0.10f, 0.16f, 1f); // card fill, matches settings
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f); cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(380, 360); cardRect.anchoredPosition = new Vector2(0, 5);

        // Title Text
        GameObject titleObj = new GameObject("PauseTitle");
        titleObj.transform.SetParent(pausePanel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Gold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(400, 100);

        // Gold accent under the title
        GameObject accent = new GameObject("TitleAccent");
        accent.transform.SetParent(pausePanel.transform, false);
        accent.AddComponent<Image>().color = Gold;
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0.5f, 0.7f); accentRect.anchorMax = new Vector2(0.5f, 0.7f);
        accentRect.sizeDelta = new Vector2(130, 3); accentRect.anchoredPosition = new Vector2(0, -46);

        // Resume Button
        Button resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0.5f, 0.55f));
        resumeBtn.onClick.AddListener(ResumeGame);
        firstSelectedButton = resumeBtn.gameObject;

        // Settings Button
        Button settingsBtn = CreateButton(pausePanel.transform, "SettingsButton", "Settings", new Vector2(0.5f, 0.42f));
        settingsBtn.onClick.AddListener(OpenSettings);

        // Hub Button
        Button hubBtn = CreateButton(pausePanel.transform, "HubButton", "Return to Hub", new Vector2(0.5f, 0.29f));
        hubBtn.onClick.AddListener(ReturnToHub);

        // Build the settings screen (hidden until opened)
        settingsMenu = gameObject.AddComponent<SettingsMenu>();
        settingsMenu.Build(canvas, OnSettingsClosed);

        // Explicit navigation, standard orientation.
        SetNav(resumeBtn,   up: hubBtn,      down: settingsBtn);
        SetNav(settingsBtn, up: resumeBtn,   down: hubBtn);
        SetNav(hubBtn,      up: settingsBtn, down: resumeBtn);

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
        btnImage.color = Color.white; // Base image color must be white for ColorBlock tinting to work properly
        
        Button button = btnObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        // Theme colors (deep purple base, gold highlight) to match the game UI
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.24f, 0.22f, 0.30f, 1f); // muted purple-grey
        colors.highlightedColor = Gold;
        colors.pressedColor = new Color(0.60f, 0.42f, 0.14f, 1f); // darker gold
        colors.selectedColor = Gold;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

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

    private void SetNav(Button b, Button up, Button down)
    {
        Navigation n = b.navigation;
        n.mode = Navigation.Mode.Explicit;
        n.selectOnUp = up;
        n.selectOnDown = down;
        b.navigation = n;
    }

    private void OpenSettings()
    {
        if (settingsMenu != null) settingsMenu.Open();
    }

    private void OnSettingsClosed()
    {
        // Re-focus the pause menu for gamepad users.
        if (firstSelectedButton != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
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
            if (CheckButton(device, PauseButtonAliases))
            {
                pausePressed = true;
            }
        }

        if (pausePressed)
        {
            // If the settings screen is open, the pause/back press closes it first.
            if (settingsMenu != null && settingsMenu.IsOpen) settingsMenu.Close();
            else if (isPaused) ResumeGame();
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
        if (settingsMenu != null && settingsMenu.IsOpen) settingsMenu.Close();
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
