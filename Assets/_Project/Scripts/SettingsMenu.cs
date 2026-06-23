using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-built Settings screen: a scrollable panel with three labelled sections
/// (Audio, Controls, Display). Opened from the pause menu. Reads/writes through
/// SettingsManager. Built at runtime (no prefab), matching how PauseManager builds
/// its UI.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private Action onBack;

    private CanvasGroup musicVolumeGroup;   // greyed out when music is disabled
    private Slider musicVolumeSlider;

    private DefaultControls.Resources uiRes;

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Build(Canvas canvas, Action backCallback)
    {
        onBack = backCallback;
        // Leave sprites null — DefaultControls builds fully-functional controls without
        // them (they just render as plain solid shapes). Loading built-in editor sprites
        // via Resources.GetBuiltinResource fails at runtime and logs errors, which can
        // trip "Error Pause" and freeze play mode.
        uiRes = new DefaultControls.Resources();

        var s = SettingsManager.Instance;

        // Full-screen opaque panel (sits on top of the pause panel)
        panel = NewRect("SettingsPanel", canvas.transform);
        Stretch(panel.GetComponent<RectTransform>());
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 1f);

        // Title
        var title = NewText("SettingsTitle", panel.transform, "SETTINGS", 48, TextAlignmentOptions.Center);
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -20f); tr.sizeDelta = new Vector2(0f, 70f);

        // ── Scroll view ─────────────────────────────────────────────────────────
        var scrollGO = NewRect("Scroll", panel.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.15f, 0f); scrollRT.anchorMax = new Vector2(0.85f, 1f);
        scrollRT.offsetMin = new Vector2(0f, 90f);    // leave room for Back button
        scrollRT.offsetMax = new Vector2(0f, -100f);  // leave room for title
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 20f;
        scrollGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        var content = NewRect("Content", scrollGO.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f); contentRT.pivot = new Vector2(0.5f, 1f);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
        vlg.spacing = 12f; vlg.padding = new RectOffset(20, 20, 20, 20);
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRT;

        // ── AUDIO ────────────────────────────────────────────────────────────────
        AddHeader(content.transform, "AUDIO");
        AddSlider(content.transform, "Master Volume", s.masterVolume, v => SettingsManager.Instance.SetMasterVolume(v), out _, out _);

        AddToggle(content.transform, "Background Music", s.musicEnabled, on =>
        {
            SettingsManager.Instance.SetMusicEnabled(on);
            SetMusicVolumeEnabled(on);
        });

        AddSlider(content.transform, "Music Volume", s.musicVolume, v => SettingsManager.Instance.SetMusicVolume(v),
                  out musicVolumeSlider, out musicVolumeGroup);
        SetMusicVolumeEnabled(s.musicEnabled);

        AddSlider(content.transform, "SFX Volume", s.sfxVolume, v => SettingsManager.Instance.SetSfxVolume(v), out _, out _);

        // ── CONTROLS ──────────────────────────────────────────────────────────────
        AddHeader(content.transform, "CONTROLS");
        AddControlsPlaceholder(content.transform);

        // ── DISPLAY ────────────────────────────────────────────────────────────────
        AddHeader(content.transform, "DISPLAY");
        AddDropdown(content.transform, "Resolution",
            new List<string>(SettingsManager.ResolutionLabels()), s.resolutionIndex,
            idx => SettingsManager.Instance.SetResolutionIndex(idx));
        AddDropdown(content.transform, "Window Mode",
            new List<string> { "Fullscreen", "Windowed" }, s.fullscreen ? 0 : 1,
            idx => SettingsManager.Instance.SetFullscreen(idx == 0));

        // ── Back button ──────────────────────────────────────────────────────────
        var back = MakeButton(panel.transform, "BackButton", "Back");
        var backRT = back.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f); backRT.anchorMax = new Vector2(0.5f, 0f); backRT.pivot = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0f, 20f); backRT.sizeDelta = new Vector2(240f, 56f);
        back.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    public void Open()
    {
        if (panel == null) return;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        onBack?.Invoke();
    }

    private void SetMusicVolumeEnabled(bool enabled)
    {
        if (musicVolumeGroup != null)
        {
            musicVolumeGroup.alpha = enabled ? 1f : 0.4f;
            musicVolumeGroup.interactable = enabled;
            musicVolumeGroup.blocksRaycasts = enabled;
        }
        if (musicVolumeSlider != null) musicVolumeSlider.interactable = enabled;
    }

    // ── Builders ───────────────────────────────────────────────────────────────
    private GameObject Row(Transform parent, float height)
    {
        var row = NewRect("Row", parent);
        row.AddComponent<LayoutElement>().preferredHeight = height;
        return row;
    }

    private void AddHeader(Transform parent, string text)
    {
        var row = Row(parent, 44f);
        var t = NewText("Header", row.transform, text, 28, TextAlignmentOptions.Left);
        t.color = new Color(0.85f, 0.55f, 0.2f);
        Stretch(t.rectTransform);
    }

    private void AddSlider(Transform parent, string label, float value, UnityEngine.Events.UnityAction<float> cb,
                           out Slider slider, out CanvasGroup group)
    {
        var row = Row(parent, 46f);
        group = row.AddComponent<CanvasGroup>();
        LabelLeft(row.transform, label);

        var sliderGO = DefaultControls.CreateSlider(uiRes);
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row.transform, false);
        PlaceRight(sliderGO.GetComponent<RectTransform>(), 8f);

        slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
        slider.value = value;
        slider.onValueChanged.AddListener(cb);
    }

    private void AddToggle(Transform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> cb)
    {
        var row = Row(parent, 40f);
        LabelLeft(row.transform, label);

        var toggleGO = DefaultControls.CreateToggle(uiRes);
        toggleGO.name = "Toggle";
        toggleGO.transform.SetParent(row.transform, false);
        var rt = toggleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(30f, 30f);
        // hide the default "Toggle" text label that ships inside the control
        var lbl = toggleGO.GetComponentInChildren<Text>();
        if (lbl != null) lbl.gameObject.SetActive(false);

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(cb);
    }

    private void AddDropdown(Transform parent, string label, List<string> options, int value, UnityEngine.Events.UnityAction<int> cb)
    {
        var row = Row(parent, 46f);
        LabelLeft(row.transform, label);

        var ddGO = DefaultControls.CreateDropdown(uiRes);
        ddGO.name = "Dropdown";
        ddGO.transform.SetParent(row.transform, false);
        PlaceRight(ddGO.GetComponent<RectTransform>(), 0f);

        var dd = ddGO.GetComponent<Dropdown>();
        dd.ClearOptions();
        dd.AddOptions(options);
        dd.value = Mathf.Clamp(value, 0, options.Count - 1);
        dd.RefreshShownValue();
        dd.onValueChanged.AddListener(cb);
    }

    private void AddControlsPlaceholder(Transform parent)
    {
        var row = Row(parent, 180f);
        var img = NewRect("ControlsPlaceholder", row.transform);
        Stretch(img.GetComponent<RectTransform>());
        img.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var t = NewText("PlaceholderLabel", img.transform,
            "Keyboard & Controller layout\n(image coming soon)", 20, TextAlignmentOptions.Center);
        t.color = new Color(1f, 1f, 1f, 0.5f);
        Stretch(t.rectTransform);
    }

    // ── Low-level helpers ────────────────────────────────────────────────────────
    private void LabelLeft(Transform row, string text)
    {
        var t = NewText("Label", row, text, 22, TextAlignmentOptions.Left);
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private void PlaceRight(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, -12f); rt.offsetMax = new Vector2(-inset, 12f);
    }

    private GameObject NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.alignment = align; t.color = Color.white;
        t.enableWordWrapping = true;
        return t;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private Button MakeButton(Transform parent, string name, string text)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = Color.white;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        cb.highlightedColor = new Color(0.8f, 0.4f, 0.1f, 1f);
        cb.pressedColor = new Color(0.5f, 0.2f, 0.05f, 1f);
        cb.selectedColor = new Color(0.8f, 0.4f, 0.1f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        var t = NewText("Text", go.transform, text, 26, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return btn;
    }
}
