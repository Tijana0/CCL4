using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Code-built Settings screen: a scrollable panel with three labelled sections
/// (Audio, Controls, Display). Opened from the pause menu. Reads/writes through
/// SettingsManager. Built at runtime (no prefab), matching how PauseManager builds
/// its UI.
///
/// Supports full controller navigation (explicit up/down between every control,
/// auto-scrolls the view to keep the selected control visible) and clamps the
/// scroll so you can't drag past the content into empty space.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private Action onBack;

    private CanvasGroup musicVolumeGroup;   // greyed out when music is disabled
    private Slider musicVolumeSlider;

    private DefaultControls.Resources uiRes;

    private ScrollRect scrollRect;
    private readonly List<Selectable> navItems = new List<Selectable>();

    // Theme
    private static readonly Color Gold     = new Color(0.85f, 0.62f, 0.25f);
    private static readonly Color PanelBg  = new Color(0.07f, 0.06f, 0.11f, 1f);
    private static readonly Color CardBg   = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color TextCol  = new Color(0.92f, 0.92f, 0.95f);

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Build(Canvas canvas, Action backCallback)
    {
        onBack = backCallback;
        // Leave sprites null — DefaultControls builds fully-functional controls without
        // them (they just render as plain solid shapes). Loading built-in editor sprites
        // via Resources.GetBuiltinResource fails at runtime and logs errors.
        uiRes = new DefaultControls.Resources();

        var s = SettingsManager.Instance;

        // Full-screen opaque panel (sits on top of the pause menu)
        panel = NewRect("SettingsPanel", canvas.transform);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = PanelBg;

        // Title + gold underline accent
        var title = NewText("SettingsTitle", panel.transform, "SETTINGS", 46, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -18f); tr.sizeDelta = new Vector2(0f, 60f);

        var underline = NewRect("TitleAccent", panel.transform);
        var ulRT = underline.GetComponent<RectTransform>();
        ulRT.anchorMin = new Vector2(0.5f, 1f); ulRT.anchorMax = new Vector2(0.5f, 1f); ulRT.pivot = new Vector2(0.5f, 1f);
        ulRT.anchoredPosition = new Vector2(0f, -78f); ulRT.sizeDelta = new Vector2(120f, 3f);
        underline.AddComponent<Image>().color = Gold;

        // ── Scroll view ─────────────────────────────────────────────────────────
        var scrollGO = NewRect("Scroll", panel.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.1f, 0f); scrollRT.anchorMax = new Vector2(0.9f, 1f);
        scrollRT.offsetMin = new Vector2(0f, 92f);    // leave room for Back button
        scrollRT.offsetMax = new Vector2(0f, -92f);   // leave room for title + accent
        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.scrollSensitivity = 24f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped; // no over-scroll into blank space
        scrollGO.AddComponent<Image>().color = CardBg;
        scrollGO.AddComponent<RectMask2D>();
        scrollRect.viewport = scrollRT;

        var content = NewRect("Content", scrollGO.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f); contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        vlg.spacing = 10f; vlg.padding = new RectOffset(20, 20, 16, 16);
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

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
        backRT.anchoredPosition = new Vector2(0f, 22f); backRT.sizeDelta = new Vector2(240f, 54f);
        back.onClick.AddListener(Close);
        navItems.Add(back);

        SetupNavigation();

        panel.SetActive(false);
    }

    public void Open()
    {
        if (panel == null) return;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f; // start at top
        FocusFirst();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        onBack?.Invoke();
    }

    private void FocusFirst()
    {
        if (navItems.Count == 0 || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(navItems[0].gameObject);
    }

    // Keep the selected control in view when navigating with a controller.
    private void Update()
    {
        if (!IsOpen || scrollRect == null || EventSystem.current == null) return;
        var sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null) return;
        var selRT = sel.GetComponent<RectTransform>();
        if (selRT == null || scrollRect.content == null || !selRT.IsChildOf(scrollRect.content)) return;
        EnsureVisible(selRT);
    }

    private void EnsureVisible(RectTransform target)
    {
        RectTransform vp = scrollRect.viewport;
        RectTransform ct = scrollRect.content;
        if (vp == null || ct == null) return;

        var v = new Vector3[4]; vp.GetWorldCorners(v);
        var t = new Vector3[4]; target.GetWorldCorners(t);
        float vTop = v[1].y, vBot = v[0].y;
        float tTop = t[1].y, tBot = t[0].y;

        const float pad = 6f;
        Vector2 ap = ct.anchoredPosition;
        if (tTop > vTop - pad) ap.y -= (tTop - vTop) + pad;          // target above view -> scroll up
        else if (tBot < vBot + pad) ap.y += (vBot - tBot) + pad;     // target below view -> scroll down
        else return;

        float maxY = Mathf.Max(0f, ct.rect.height - vp.rect.height);
        ap.y = Mathf.Clamp(ap.y, 0f, maxY);
        ct.anchoredPosition = ap;
    }

    private void SetupNavigation()
    {
        for (int i = 0; i < navItems.Count; i++)
        {
            var nav = navItems[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp   = navItems[(i - 1 + navItems.Count) % navItems.Count];
            nav.selectOnDown = navItems[(i + 1) % navItems.Count];
            // left/right are left for the sliders/dropdowns to consume (value change)
            nav.selectOnLeft = null; nav.selectOnRight = null;
            navItems[i].navigation = nav;
        }
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
        var row = Row(parent, 46f);
        var t = NewText("Header", row.transform, text, 26, TextAlignmentOptions.BottomLeft);
        t.fontStyle = FontStyles.Bold;
        t.color = Gold;
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(2f, 6f); rt.offsetMax = Vector2.zero;
        // thin underline rule
        var rule = NewRect("Rule", row.transform);
        var rr = rule.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0f, 0f); rr.anchorMax = new Vector2(1f, 0f); rr.pivot = new Vector2(0.5f, 0f);
        rr.anchoredPosition = new Vector2(0f, 2f); rr.sizeDelta = new Vector2(0f, 2f);
        rule.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
    }

    private void AddSlider(Transform parent, string label, float value, UnityEngine.Events.UnityAction<float> cb,
                           out Slider slider, out CanvasGroup group)
    {
        var row = Row(parent, 48f);
        group = row.AddComponent<CanvasGroup>();
        LabelLeft(row.transform, label);

        // numeric value readout on the far right
        var valText = NewText("Value", row.transform, Mathf.RoundToInt(value) + "%", 20, TextAlignmentOptions.Right);
        valText.color = new Color(1f, 1f, 1f, 0.75f);
        var vt = valText.rectTransform;
        vt.anchorMin = new Vector2(1f, 0f); vt.anchorMax = new Vector2(1f, 1f); vt.pivot = new Vector2(1f, 0.5f);
        vt.sizeDelta = new Vector2(56f, 0f); vt.anchoredPosition = new Vector2(0f, 0f);

        var sliderGO = DefaultControls.CreateSlider(uiRes);
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row.transform, false);
        // sit between the label (left half) and the value readout (right ~60px)
        var srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
        srt.offsetMin = new Vector2(8f, -10f); srt.offsetMax = new Vector2(-64f, 10f);

        slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
        slider.value = value;
        var sliderRef = slider; var valRef = valText;
        slider.onValueChanged.AddListener(v => { valRef.text = Mathf.RoundToInt(v) + "%"; cb(v); });
        navItems.Add(slider);
    }

    private void AddToggle(Transform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> cb)
    {
        var row = Row(parent, 42f);
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
        navItems.Add(toggle);
    }

    private void AddDropdown(Transform parent, string label, List<string> options, int value, UnityEngine.Events.UnityAction<int> cb)
    {
        var row = Row(parent, 48f);
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
        navItems.Add(dd);
    }

    private void AddControlsPlaceholder(Transform parent)
    {
        var row = Row(parent, 96f);
        var img = NewRect("ControlsPlaceholder", row.transform);
        Stretch(img.GetComponent<RectTransform>());
        img.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
        var t = NewText("PlaceholderLabel", img.transform,
            "Keyboard & Controller layout\n(image coming soon)", 18, TextAlignmentOptions.Center);
        t.color = new Color(1f, 1f, 1f, 0.5f);
        Stretch(t.rectTransform);
    }

    // ── Low-level helpers ────────────────────────────────────────────────────────
    private void LabelLeft(Transform row, string text)
    {
        var t = NewText("Label", row, text, 22, TextAlignmentOptions.Left);
        var rt = t.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(2f, 0f); rt.offsetMax = Vector2.zero;
    }

    private void PlaceRight(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, -14f); rt.offsetMax = new Vector2(-inset, 14f);
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
        t.text = text; t.fontSize = size; t.alignment = align; t.color = TextCol;
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
        cb.normalColor = new Color(0.22f, 0.2f, 0.26f, 1f);
        cb.highlightedColor = new Color(0.85f, 0.62f, 0.25f, 1f);
        cb.pressedColor = new Color(0.6f, 0.4f, 0.12f, 1f);
        cb.selectedColor = new Color(0.85f, 0.62f, 0.25f, 1f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        var t = NewText("Text", go.transform, text, 24, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return btn;
    }
}
