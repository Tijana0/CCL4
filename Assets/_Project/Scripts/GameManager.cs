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

    [Header("Win / Report")]
    public int targetScore = 100;     // points needed to PASS
    public int twoStarScore = 150;
    public int threeStarScore = 250;
    public ReportCard reportCard;     // drag your ReportCardPanel here
    public string classNameForReport = "Potions";   // set per scene

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

        // LB (Left Bumper) is button5 on this Mac gamepad
        switchPlayerAction.AddBinding("<Joystick>/button5"); 
        switchPlayerAction.AddBinding("<HID::*>/button5"); 

        // Add generic joystick fallbacks for UI SUBMIT and NAVIGATE
        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var module = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module != null)
            {
                if (module.submit != null && module.submit.action != null)
                {
                    module.submit.action.AddBinding("<Joystick>/trigger");
                    module.submit.action.AddBinding("<Joystick>/button0");
                    module.submit.action.AddBinding("<Joystick>/button11"); // Mac alias
                    module.submit.action.AddBinding("<HID::*>/button0");
                }
                if (module.move != null && module.move.action != null)
                {
                    module.move.action.AddBinding("<Joystick>/stick");
                }
            }
        }

        // Auto-attach PauseManager so it's always available
        if (GetComponent<PauseManager>() == null)
        {
            gameObject.AddComponent<PauseManager>();
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

            UnityEngine.Transform rcPanel = canvas.transform.Find("ReportCardPanel");
            if (rcPanel != null)
            {
                if (reportCard == null) reportCard = rcPanel.GetComponent<ReportCard>();
                rcPanel.gameObject.SetActive(false);
                Debug.Log("ReportCard found? " + (reportCard != null));   // ← add this
            }
            else
            {
                Debug.LogWarning("ReportCardPanel NOT found under UI_Canvas");  // ← and this
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
                EndGame(score >= targetScore);
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

public void WinGame()  => EndGame(true);
public void LoseGame() => EndGame(false);

public void EndGame(bool won)
{
    if (currentState == GameState.TimeUp) return;   // already ended
    currentState = GameState.TimeUp;
    Time.timeScale = 0f;

    int stars = won ? CalculateStars() : 0;

    int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
    if (PersistentGameState.Instance != null)
    {
        PersistentGameState.Instance.currentLevelIndex = currentBuildIndex;
        PersistentGameState.Instance.AwardStars(currentBuildIndex, stars);
    }

    if (reportCard != null)
        reportCard.Show(classNameForReport, stars, score, won);
    else if (gameOverPanel != null)   // fallback to old panel
    {
        gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = $"Final Score: {score}";
    }

    FreezePlayers();
}

int CalculateStars()
{
    if (score >= threeStarScore) return 3;
    if (score >= twoStarScore)   return 2;
    return 1;
}

void FreezePlayers()
{
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
            SceneLoader.Instance.LoadScene(2); // Hub is now index 2
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }
}
