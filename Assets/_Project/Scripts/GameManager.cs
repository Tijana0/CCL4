using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState
{
    WaitingToStart,
    Playing,
    TimeUp
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public float gameTime = 30f;
    
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel;
    public GameObject overallReportPanel;
    public TextMeshProUGUI finalScoreText;

    [HideInInspector] public float timeRemaining;
    private GameState currentState = GameState.WaitingToStart;

    [Header("Controller Input")]
    public InputAction switchPlayerAction = new InputAction("SwitchPlayer", binding: "<Gamepad>/leftShoulder");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ensure PersistentGameState exists
        if (PersistentGameState.Instance == null)
        {
            GameObject pgsObj = new GameObject("PersistentGameState");
            pgsObj.AddComponent<PersistentGameState>();
        }

        // Add generic joystick fallback for player switching
        switchPlayerAction.AddBinding("<Joystick>/button4"); // Generic Left Bumper
        switchPlayerAction.AddBinding("<Joystick>/button5"); // Generic Right Bumper
        switchPlayerAction.AddBinding("<HID::*>/button4"); 
        switchPlayerAction.AddBinding("<HID::*>/button5"); 

        // Add generic joystick fallbacks for UI SUBMIT
        // This ensures the A button on generic controllers works on the Game Over screen
        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var module = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module != null && module.submit != null && module.submit.action != null)
            {
                module.submit.action.AddBinding("<Joystick>/trigger");
                module.submit.action.AddBinding("<Joystick>/button0");
                module.submit.action.AddBinding("<Joystick>/button1");
                module.submit.action.AddBinding("<HID::*>/button2");
            }
        }
    }

    private void OnEnable()
    {
        switchPlayerAction.Enable();
    }

    private void OnDisable()
    {
        switchPlayerAction.Disable();
    }

    private void Start()
    {
        Time.timeScale = 1f; 
        timeRemaining = gameTime;

        // Dynamically find UI elements to prevent prefab reference loss
        UnityEngine.GameObject canvas = UnityEngine.GameObject.Find("UI_Canvas");
        if (canvas != null)
        {
            if (gameOverPanel == null)
            {
                UnityEngine.Transform goPanel = canvas.transform.Find("GameOverPanel");
                if (goPanel != null) gameOverPanel = goPanel.gameObject;
            }

            if (overallReportPanel == null)
            {
                UnityEngine.Transform repPanel = canvas.transform.Find("OverallReportPanel");
                if (repPanel != null) overallReportPanel = repPanel.gameObject;
            }
        }

        // Dynamically bind the Game Over buttons
        if (gameOverPanel != null)
        {
            UnityEngine.UI.Button restartBtn = gameOverPanel.transform.Find("RestartButton")?.GetComponent<UnityEngine.UI.Button>();
            if (restartBtn != null)
            {
                restartBtn.onClick.RemoveAllListeners();
                restartBtn.onClick.AddListener(RestartGame);
            }

            UnityEngine.UI.Button nextBtn = gameOverPanel.transform.Find("NextLevelButton")?.GetComponent<UnityEngine.UI.Button>();
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(LoadNextLevel);
            }

            gameOverPanel.SetActive(false);
        }

        if (overallReportPanel != null)
        {
            overallReportPanel.SetActive(false);
        }
        
        currentState = GameState.Playing;
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // Character switching logic (Handled here once per frame)
        if (switchPlayerAction.WasPressedThisFrame())
        {
            SimplePlayerController.activeGamepadPlayerIndex = (SimplePlayerController.activeGamepadPlayerIndex == 0) ? 1 : 0;
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            
            // Clamp to zero so we don't get negative numbers in the UI
            if (timeRemaining < 0) timeRemaining = 0;
            
            UpdateTimerUI();

            if (timeRemaining <= 0)
            {
                GameOver();
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            
            // Turn red in last 10 seconds
            if (timeRemaining <= 10f)
            {
                timerText.color = Color.red;
            }
        }
    }

    private void GameOver()
    {
        currentState = GameState.TimeUp;
        Time.timeScale = 0f; // Freeze all physics and animations
        Debug.Log("Game Over!");

        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        if (PersistentGameState.Instance != null)
        {
            PersistentGameState.Instance.currentLevelIndex = currentBuildIndex;
            // Temporarily award 3 stars automatically for completing the level
            PersistentGameState.Instance.AwardStars(currentBuildIndex, 3);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (finalScoreText != null)
            {
                finalScoreText.text = "Stars Earned: 3";
            }

            // Auto-select the button for gamepad support
            UnityEngine.UI.Button restartBtn = gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (restartBtn != null)
            {
                restartBtn.Select();
            }
        }

        // Disable all player controllers and stop their movement
        SimplePlayerController[] players = FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            player.enabled = false;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Important to reset before loading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            // Show overall report instead of looping
            ShowOverallReport();
        }
    }

    public void ShowOverallReport()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (overallReportPanel != null)
        {
            overallReportPanel.SetActive(true);

            float totalStars = 0;
            int levelsPlayed = 0;
            if (PersistentGameState.Instance != null)
            {
                foreach (var kvp in PersistentGameState.Instance.starsPerLevel)
                {
                    totalStars += kvp.Value;
                    levelsPlayed++;
                }
            }

            float avg = levelsPlayed > 0 ? totalStars / levelsPlayed : 0;
            
            // Find a text component in the report panel to show the score
            TextMeshProUGUI[] texts = overallReportPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name == "ReportText" || t.text.Contains("Score"))
                {
                    t.text = $"Overall Average Stars: {avg:F1}";
                }
            }

            UnityEngine.UI.Button restartRunBtn = overallReportPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (restartRunBtn != null)
            {
                restartRunBtn.onClick.RemoveAllListeners();
                restartRunBtn.onClick.AddListener(() => {
                    if (PersistentGameState.Instance != null) PersistentGameState.Instance.ResetRun();
                    SceneManager.LoadScene(0);
                });
                restartRunBtn.Select();
            }
        }
        else
        {
            // Fallback if no report panel exists
            if (PersistentGameState.Instance != null) PersistentGameState.Instance.ResetRun();
            SceneManager.LoadScene(0);
        }
    }
}
