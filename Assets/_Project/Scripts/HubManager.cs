using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class HubManager : MonoBehaviour
{
    [Header("Level 1 UI")]
    public Button level1Button;
    public TextMeshProUGUI level1StarsText;

    [Header("Level 2 UI")]
    public Button level2Button;
    public TextMeshProUGUI level2StarsText;

    private void Awake()
    {
        // Add generic joystick fallbacks for UI SUBMIT
        // This ensures the A button on generic controllers works on the Hub screen
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

    private void Start()
    {
        // Build indices mapping (based on Build Settings)
        // 0 = Bootstrap, 1 = HubScene, 2 = Level 1, 3 = Level 2
        int level1Index = 2;
        int level2Index = 3;

        bool level1Completed = false;

        if (PersistentGameState.Instance != null)
        {
            // Check Level 1
            if (PersistentGameState.Instance.starsPerLevel.ContainsKey(level1Index))
            {
                int stars = PersistentGameState.Instance.starsPerLevel[level1Index];
                level1StarsText.text = $"Stars: {stars}";
                level1Completed = true;
            }
            else
            {
                level1StarsText.text = ""; // Don't show anything if unplayed
            }

            // Check Level 2
            if (PersistentGameState.Instance.starsPerLevel.ContainsKey(level2Index))
            {
                int stars = PersistentGameState.Instance.starsPerLevel[level2Index];
                level2StarsText.text = $"Stars: {stars}";
            }
            else
            {
                level2StarsText.text = level1Completed ? "" : "Locked";
            }
        }
        else
        {
            level1StarsText.text = "";
            level2StarsText.text = "Locked";
        }

        // Setup Level 2 Lock State
        level2Button.interactable = level1Completed;

        // Bind Button Listeners
        if (level1Button != null)
        {
            level1Button.onClick.RemoveAllListeners();
            level1Button.onClick.AddListener(() => LoadLevel(level1Index));
        }

        if (level2Button != null)
        {
            level2Button.onClick.RemoveAllListeners();
            level2Button.onClick.AddListener(() => LoadLevel(level2Index));
        }

        // Auto-select for gamepad support
        if (level1Button != null) level1Button.Select();
    }

    private void LoadLevel(int buildIndex)
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(buildIndex);
        }
        else
        {
            SceneManager.LoadScene(buildIndex);
        }
    }
}