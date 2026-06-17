using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState { WaitingToStart, Playing, TimeUp }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public float gameTime = 30f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public GameObject overallReportPanel;
    public TextMeshProUGUI finalScoreText;

    [HideInInspector] public float timeRemaining;
    private int score = 0;
    private GameState currentState = GameState.WaitingToStart;
    private int lastProcessedSeconds = -1;

    [Header("Controller Input")]
    public InputAction switchPlayerAction = new InputAction("SwitchPlayer", binding: "<Gamepad>/leftShoulder");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Add generic joystick fallback for player switching
        switchPlayerAction.AddBinding("<Joystick>/button4"); // Generic Left Bumper
        switchPlayerAction.AddBinding("<Joystick>/button5"); // Generic Right Bumper
        switchPlayerAction.AddBinding("<HID::*>/button4"); 
        switchPlayerAction.AddBinding("<HID::*>/button5"); 

        // Add generic joystick fallbacks for UI SUBMIT
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

    private void OnEnable() { switchPlayerAction.Enable(); }
    private void OnDisable() { switchPlayerAction.Disable(); }

    private void Start()
    {
        Time.timeScale = 1f; 
        timeRemaining = gameTime;
        score = 0;

        // Dynamically find UI elements to prevent prefab reference loss
        UnityEngine.GameObject canvas = UnityEngine.GameObject.Find("UI_Canvas");
        if (canvas != null)
        {
            if (timerText == null)
            {
                UnityEngine.Transform tText = canvas.transform.Find("TimerBackground/TimerText");
                if (tText == null) tText = canvas.transform.Find("TimerText");
                if (tText != null) timerText = tText.GetComponent<TextMeshProUGUI>();
            }

            if (scoreText == null)
            {
                UnityEngine.Transform sText = canvas.transform.Find("ScoreText");
                if (sText != null) scoreText = sText.GetComponent<TextMeshProUGUI>();
            }

            UnityEngine.Transform goPanel = canvas.transform.Find("GameOverPanel");
            if (goPanel != null)
            {
                gameOverPanel = goPanel.gameObject;
                
                // Bind finalScoreText
                UnityEngine.Transform fScoreText = goPanel.Find("GameOverSubtitle");
                if (fScoreText != null)
                {
                    finalScoreText = fScoreText.GetComponent<TextMeshProUGUI>();
                }
            }

            UnityEngine.Transform repPanel = canvas.transform.Find("OverallReportPanel");
            if (repPanel != null)
            {
                overallReportPanel = repPanel.gameObject;
            }
        }

        // Dynamically bind the Game Over buttons
        if (gameOverPanel != null)
        {
            UnityEngine.UI.Button returnBtn = gameOverPanel.transform.Find("ReturnHubButton")?.GetComponent<UnityEngine.UI.Button>();
            if (returnBtn != null)
            {
                returnBtn.onClick.RemoveAllListeners();
                returnBtn.onClick.AddListener(ReturnToHub);
            }

            gameOverPanel.SetActive(false);
        }

        if (overallReportPanel != null)
        {
            overallReportPanel.SetActive(false);
        }
        
        currentState = GameState.Playing;
        UpdateScoreUI();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        // Character switching logic
        if (switchPlayerAction.WasPressedThisFrame())
        {
            SimplePlayerController.activeGamepadPlayerIndex = (SimplePlayerController.activeGamepadPlayerIndex == 0) ? 1 : 0;
        }

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;
            
            int currentSeconds = Mathf.FloorToInt(timeRemaining);
            if (currentSeconds != lastProcessedSeconds)
            {
                lastProcessedSeconds = currentSeconds;
                UpdateTimerUI(currentSeconds);
            }

            if (timeRemaining <= 0)
            {
                GameOver();
            }
        }
    }

    /// <summary>Add points to the score. Called by InstantStation on successful hand-in.</summary>
    public void AddScore(int points)
    {
        score += points;
        UpdateScoreUI();
        Debug.Log($"[GameManager] Score: {score}");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void UpdateTimerUI(int totalSeconds)
    {
        if (timerText != null)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            
            if (totalSeconds <= 10)
            {
                timerText.color = Color.red;
            }
        }
    }

    private void GameOver()
    {
        currentState = GameState.TimeUp;
        Time.timeScale = 0f;

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
                finalScoreText.text = $"Final Score: {score}";
            }

            UnityEngine.UI.Button restartBtn = gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (restartBtn != null)
            {
                restartBtn.Select();
            }
        }

        SimplePlayerController[] players = FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            player.enabled = false;
        }
    }

    public void ReturnToHub()
    {
        Time.timeScale = 1f;
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(1); // Hub is now index 1
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}
