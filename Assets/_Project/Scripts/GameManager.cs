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
    public TextMeshProUGUI finalScoreText;

    [HideInInspector] public float timeRemaining;
    private GameState currentState = GameState.WaitingToStart;

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

        // Dynamically bind the Game Over buttons to prevent prefab reference loss
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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
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
            // If no more levels, just restart the first one or loop
            SceneManager.LoadScene(0);
        }
    }
}
