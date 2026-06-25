using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2D HUD Time-Turner. Does NOT run a clock — it reads timeRemaining from GameManager.
/// Spins a flat UI Image around Z (the screen axis) and pulses a red halo when time is low.
/// Put this on the TimerGroup (or the TimeTurner Image) in your UI_Canvas.
/// </summary>
public class TimeTurnerDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Time-Turner UI Image that spins.")]
    public RectTransform turner;
    [Tooltip("Red glow Image placed BEHIND the turner. Starts invisible.")]
    public Graphic halo;

    [Header("Spin (around Z — the screen axis)")]
    public float baseSpinSpeed = 60f;       // degrees per second at normal pace
    public float maxSpinMultiplier = 3f;    // how much faster near 0
    public bool clockwise = true;           // stopwatch direction

    [Header("Low-time warning")]
    [Tooltip("Seconds left when the halo starts. Matches GameManager's red-text point (10).")]
    public float warningThreshold = 10f;
    public Color haloColor = Color.red;
    [Range(0f, 1f)] public float haloMinAlpha = 0.2f;
    [Range(0f, 1f)] public float haloMaxAlpha = 0.9f;
    public float pulseSpeed = 5f;

    void Start()
    {
        if (halo) SetHaloAlpha(0f);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        float timeLeft = GameManager.Instance.timeRemaining;

        // urgency: 0 normally, ramps 0 -> 1 as time crosses the threshold toward 0
        float urgency = (timeLeft <= warningThreshold && timeLeft > 0f)
            ? 1f - (timeLeft / warningThreshold)
            : 0f;

        // SPIN around Z. Time.deltaTime is 0 while paused / after GameOver
        // (timeScale = 0), so it stops on its own then.
        if (turner)
        {
            float speed = baseSpinSpeed * (1f + urgency * (maxSpinMultiplier - 1f));
            float dir = clockwise ? -1f : 1f;   // -Z reads as clockwise on screen
            turner.Rotate(0f, 0f, dir * speed * Time.deltaTime);
        }

        // RED HALO pulse near zero
        if (halo)
        {
            if (urgency > 0f)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed * (1f + urgency)) + 1f) * 0.5f;
                SetHaloAlpha(Mathf.Lerp(haloMinAlpha, haloMaxAlpha, pulse));
            }
            else
            {
                SetHaloAlpha(0f);
            }
        }
    }

    void SetHaloAlpha(float a)
    {
        Color c = haloColor;
        c.a = a;
        halo.color = c;
    }
}