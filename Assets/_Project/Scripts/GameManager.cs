using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState { WaitingToStart, Playing, TimeUp }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public float gameTime = 800f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;

    [HideInInspector] public float timeRemaining;
    private int score = 0;
    private GameState currentState = GameState.WaitingToStart;

    [Header("Controller Input")]
    public InputAction switchPlayerAction = new InputAction("SwitchPlayer", binding: "<Gamepad>/leftShoulder");

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        switchPlayerAction.AddBinding("<Joystick>/button4");
        switchPlayerAction.AddBinding("<Joystick>/button5");
        switchPlayerAction.AddBinding("<HID::*>/button4");
        switchPlayerAction.AddBinding("<HID::*>/button5");

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
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        currentState = GameState.Playing;
        UpdateScoreUI();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        if (switchPlayerAction.WasPressedThisFrame())
            SimplePlayerController.activeGamepadPlayerIndex = (SimplePlayerController.activeGamepadPlayerIndex == 0) ? 1 : 0;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;
            UpdateTimerUI();
            if (timeRemaining <= 0) GameOver();
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

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            if (timeRemaining <= 10f) timerText.color = Color.red;
        }
    }

    private void GameOver()
    {
        currentState = GameState.TimeUp;
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = $"Final Score: {score}";
            UnityEngine.UI.Button restartBtn = gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            if (restartBtn != null) restartBtn.Select();
        }

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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextIndex < SceneManager.sceneCountInBuildSettings ? nextIndex : 0);
    }
}