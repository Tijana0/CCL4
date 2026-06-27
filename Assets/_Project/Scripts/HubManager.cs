using System.Collections;
using System.Collections.Generic;
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

    [Header("Footstep Trail (order them Level 1 -> Level 2)")]
    public List<RawImage> footsteps = new List<RawImage>();
    public float stepInterval = 0.18f;
    public float stepFadeTime = 0.12f;
    public bool animateOnlyOnce = true;

    private static bool hasWalkedThisSession = false;

    private void Awake()
    {
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

    private void Start()
    {
        // 0 = Bootstrap, 1 = Startscreen, 2 = HubScene, 3 = Level 1, 4 = Level 2
        int level1Index = 3;
        int level2Index = 4;

        bool level1Completed = false;

        if (PersistentGameState.Instance != null)
        {
            if (PersistentGameState.Instance.starsPerLevel.ContainsKey(level1Index))
            {
                int stars = PersistentGameState.Instance.starsPerLevel[level1Index];
                level1StarsText.text = $"Stars: {stars}";
                level1Completed = stars >= 1;
            }
            else
            {
                level1StarsText.text = "";
            }

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

        level2Button.interactable = level1Completed;

        // Configure colors for high visibility when selected
        if (level1Button != null)
        {
            var colors = level1Button.colors;
            colors.selectedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
            colors.highlightedColor = new Color(1f, 0.8f, 0.2f, 1f);
            level1Button.colors = colors;
        }
        if (level2Button != null)
        {
            var colors = level2Button.colors;
            colors.selectedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
            colors.highlightedColor = new Color(1f, 0.8f, 0.2f, 1f);
            level2Button.colors = colors;
        }

        // Setup explicit navigation between buttons
        if (level1Button != null && level2Button != null)
        {
            if (level1Completed)
            {
                Navigation nav1 = level1Button.navigation;
                nav1.mode = Navigation.Mode.Explicit;
                nav1.selectOnRight = level2Button;
                nav1.selectOnLeft = level2Button;
                level1Button.navigation = nav1;

                Navigation nav2 = level2Button.navigation;
                nav2.mode = Navigation.Mode.Explicit;
                nav2.selectOnLeft = level1Button;
                nav2.selectOnRight = level1Button;
                level2Button.navigation = nav2;
            }
            else
            {
                Navigation nav1 = level1Button.navigation;
                nav1.mode = Navigation.Mode.None;
                level1Button.navigation = nav1;
            }
        }

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

        if (level1Button != null) level1Button.Select();

        // --- FOOTSTEP TRAIL ---
        SetupFootsteps(level1Completed);
    }

    private void SetupFootsteps(bool unlocked)
    {
        if (footsteps == null || footsteps.Count == 0) return;

        if (!unlocked)
        {
            foreach (var f in footsteps)
                if (f != null) f.enabled = false;
            return;
        }

        bool shouldAnimate = !animateOnlyOnce || !hasWalkedThisSession;

        if (shouldAnimate)
        {
            foreach (var f in footsteps)
                if (f != null) { f.enabled = true; SetAlpha(f, 0f); }

            StartCoroutine(WalkFootsteps());
            hasWalkedThisSession = true;
        }
        else
        {
            foreach (var f in footsteps)
                if (f != null) { f.enabled = true; SetAlpha(f, 1f); }
        }
    }

    private IEnumerator WalkFootsteps()
    {
        foreach (var f in footsteps)
        {
            if (f == null) continue;
            yield return StartCoroutine(FadeIn(f));
            yield return new WaitForSeconds(stepInterval);
        }
    }

    private IEnumerator FadeIn(RawImage img)
    {
        float t = 0f;
        while (t < stepFadeTime)
        {
            t += Time.deltaTime;
            SetAlpha(img, Mathf.Clamp01(t / stepFadeTime));
            yield return null;
        }
        SetAlpha(img, 1f);
    }

    private void SetAlpha(RawImage img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void LoadLevel(int buildIndex)
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(buildIndex);
        else
            SceneManager.LoadScene(buildIndex);
    }
}