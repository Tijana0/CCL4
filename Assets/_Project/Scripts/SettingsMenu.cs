using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Code-built Settings screen. A centred card that fills the screen with two
/// columns (Audio / Display) — no scrolling, everything visible at once.
/// Opened from the pause menu. Reads/writes through SettingsManager.
///
/// Full controller support: every control is reachable with up/down (the chain
/// snakes down the left column then the right, then Back). Left/right on a
/// selected slider adjusts its value; on a dropdown opens it. A control is always
/// selected while the menu is open, so a controller can always drive it.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private Action onBack;

    private CanvasGroup musicVolumeGroup;   // greyed out when music is disabled
    private Slider musicVolumeSlider;

    private DefaultControls.Resources uiRes;
    private readonly List<Selectable> navItems = new List<Selectable>();

    // Theme
    private static readonly Color Dim     = new Color(0.02f, 0.02f, 0.04f, 0.9f);
    private static readonly Color CardBg  = new Color(0.11f, 0.10f, 0.16f, 1f);
    private static readonly Color CardEdge= new Color(0.85f, 0.62f, 0.25f, 1f);
    private static readonly Color Gold    = new Color(0.88f, 0.66f, 0.28f);
    private static readonly Color TextCol = new Color(0.93f, 0.93f, 0.96f);

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Build(Canvas canvas, Action backCallback)
    {
        onBack = backCallback;
        uiRes = new DefaultControls.Resources();
        var s = SettingsManager.Instance;

        // Full-screen dim behind the card
        panel = NewRect("SettingsPanel", canvas.transform);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = Dim;

        // Gold edge (slightly bigger than the card) for a framed look
        var edge = NewRect("CardEdge", panel.transform);
        var edgeRT = edge.GetComponent<RectTransform>();
        edgeRT.anchorMin = new Vector2(0.085f, 0.075f); edgeRT.anchorMax = new Vector2(0.915f, 0.925f);
        edgeRT.offsetMin = Vector2.zero; edgeRT.offsetMax = Vector2.zero;
        edge.AddComponent<Image>().color = CardEdge;

        // Card
        var card = NewRect("Card", panel.transform);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.09f, 0.08f); cardRT.anchorMax = new Vector2(0.91f, 0.92f);
        cardRT.offsetMin = Vector2.zero; cardRT.offsetMax = Vector2.zero;
        card.AddComponent<Image>().color = CardBg;

        // Title + gold underline
        var title = NewText("SettingsTitle", card.transform, "SETTINGS", 40, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -14f); tr.sizeDelta = new Vector2(0f, 52f);

        var ul = NewRect("TitleAccent", card.transform);
        var ulRT = ul.GetComponent<RectTransform>();
        ulRT.anchorMin = new Vector2(0.5f, 1f); ulRT.anchorMax = new Vector2(0.5f, 1f); ulRT.pivot = new Vector2(0.5f, 1f);
        ulRT.anchoredPosition = new Vector2(0f, -64f); ulRT.sizeDelta = new Vector2(110f, 3f);
        ul.AddComponent<Image>().color = Gold;

        // Two columns
        var left  = Column(card.transform, new Vector2(0.05f, 0.16f), new Vector2(0.49f, 0.80f));
        var right = Column(card.transform, new Vector2(0.51f, 0.16f), new Vector2(0.95f, 0.80f));

        // ── LEFT: AUDIO ────────────────────────────────────────────────────────
        AddHeader(left, "AUDIO");
        AddSlider(left, "Master", s.masterVolume, v => SettingsManager.Instance.SetMasterVolume(v), out _, out _);
        AddToggle(left, "Background Music", s.musicEnabled, on =>
        {
            SettingsManager.Instance.SetMusicEnabled(on);
            SetMusicVolumeEnabled(on);
        });
        AddSlider(left, "Music", s.musicVolume, v => SettingsManager.Instance.SetMusicVolume(v),
                  out musicVolumeSlider, out musicVolumeGroup);
        SetMusicVolumeEnabled(s.musicEnabled);
        AddSlider(left, "SFX", s.sfxVolume, v => SettingsManager.Instance.SetSfxVolume(v), out _, out _);

        // ── RIGHT: DISPLAY ─────────────────────────────────────────────────────
        AddHeader(right, "DISPLAY");
        AddDropdown(right, "Resolution",
            new List<string>(SettingsManager.ResolutionLabels()), s.resolutionIndex,
            idx => SettingsManager.Instance.SetResolutionIndex(idx));
        AddDropdown(right, "Window Mode",
            new List<string> { "Fullscreen", "Windowed" }, s.fullscreen ? 0 : 1,
            idx => SettingsManager.Instance.SetFullscreen(idx == 0));

        // ── Back button ────────────────────────────────────────────────────────
        var back = MakeButton(card.transform, "BackButton", "Back");
        var backRT = back.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f); backRT.anchorMax = new Vector2(0.5f, 0f); backRT.pivot = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0f, 18f); backRT.sizeDelta = new Vector2(220f, 48f);
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

    // Keep a control selected while open, so a controller can always drive the menu.
    private void Update()
    {
        if (!IsOpen || EventSystem.current == null) return;
        var sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null || !sel.activeInHierarchy || !sel.transform.IsChildOf(panel.transform))
            FocusFirst();
    }

    private void SetupNavigation()
    {
        for (int i = 0; i < navItems.Count; i++)
        {
            var nav = navItems[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp   = navItems[(i - 1 + navItems.Count) % navItems.Count];
            nav.selectOnDown = navItems[(i + 1) % navItems.Count];
            nav.selectOnLeft = null; nav.selectOnRight = null; // left/right adjust sliders / open dropdowns
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
    private Transform Column(Transform card, Vector2 aMin, Vector2 aMax)
    {
        var col = NewRect("Column", card);
        var rt = col.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 14f; vlg.padding = new RectOffset(4, 4, 0, 0);
        return col.transform;
    }

    private GameObject Row(Transform parent, float height)
    {
        var row = NewRect("Row", parent);
        row.AddComponent<LayoutElement>().preferredHeight = height;
        return row;
    }

    private void AddHeader(Transform parent, string text)
    {
        var row = Row(parent, 40f);
        var t = NewText("Header", row.transform, text, 24, TextAlignmentOptions.BottomLeft);
        t.fontStyle = FontStyles.Bold; t.color = Gold;
        var rt = t.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(2f, 6f); rt.offsetMax = Vector2.zero;
        var rule = NewRect("Rule", row.transform);
        var rr = rule.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0f, 0f); rr.anchorMax = new Vector2(1f, 0f); rr.pivot = new Vector2(0.5f, 0f);
        rr.anchoredPosition = new Vector2(0f, 1f); rr.sizeDelta = new Vector2(0f, 2f);
        rule.AddComponent<Image>().color = new Color(Gold.r, Gold.g, Gold.b, 0.35f);
    }

    private void AddSlider(Transform parent, string label, float value, UnityEngine.Events.UnityAction<float> cb,
                           out Slider slider, out CanvasGroup group)
    {
        var row = Row(parent, 50f);
        group = row.AddComponent<CanvasGroup>();

        var lab = NewText("Label", row.transform, label, 20, TextAlignmentOptions.Left);
        var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(0.6f, 1f);
        lrt.pivot = new Vector2(0f, 1f); lrt.offsetMin = new Vector2(2f, -22f); lrt.offsetMax = new Vector2(0f, 0f);

        var valText = NewText("Value", row.transform, Mathf.RoundToInt(value) + "%", 20, TextAlignmentOptions.Right);
        valText.color = new Color(1f, 1f, 1f, 0.75f);
        var vt = valText.rectTransform; vt.anchorMin = new Vector2(0.6f, 1f); vt.anchorMax = new Vector2(1f, 1f);
        vt.pivot = new Vector2(1f, 1f); vt.offsetMin = new Vector2(0f, -22f); vt.offsetMax = new Vector2(0f, 0f);

        var sliderGO = DefaultControls.CreateSlider(uiRes);
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row.transform, false);
        var srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 0f); srt.pivot = new Vector2(0.5f, 0f);
        srt.offsetMin = new Vector2(2f, 4f); srt.offsetMax = new Vector2(-2f, 18f);

        slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
        slider.value = value;
        var valRef = valText;
        slider.onValueChanged.AddListener(v => { valRef.text = Mathf.RoundToInt(v) + "%"; cb(v); });
        navItems.Add(slider);
    }

    private void AddToggle(Transform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> cb)
    {
        var row = Row(parent, 38f);
        var lab = NewText("Label", row.transform, label, 20, TextAlignmentOptions.Left);
        var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.8f, 1f);
        lrt.offsetMin = new Vector2(2f, 0f); lrt.offsetMax = Vector2.zero;

        var toggleGO = DefaultControls.CreateToggle(uiRes);
        toggleGO.name = "Toggle";
        toggleGO.transform.SetParent(row.transform, false);
        var rt = toggleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f); rt.anchoredPosition = new Vector2(-2f, 0f); rt.sizeDelta = new Vector2(28f, 28f);
        var lbl = toggleGO.GetComponentInChildren<Text>();
        if (lbl != null) lbl.gameObject.SetActive(false);

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(cb);
        navItems.Add(toggle);
    }

    private void AddDropdown(Transform parent, string label, List<string> options, int value, UnityEngine.Events.UnityAction<int> cb)
    {
        var row = Row(parent, 56f);
        var lab = NewText("Label", row.transform, label, 20, TextAlignmentOptions.Left);
        var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(0f, 1f); lrt.offsetMin = new Vector2(2f, -22f); lrt.offsetMax = new Vector2(0f, 0f);

        var ddGO = DefaultControls.CreateDropdown(uiRes);
        ddGO.name = "Dropdown";
        ddGO.transform.SetParent(row.transform, false);
        var drt = ddGO.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0f, 0f); drt.anchorMax = new Vector2(1f, 0f); drt.pivot = new Vector2(0.5f, 0f);
        drt.offsetMin = new Vector2(2f, 2f); drt.offsetMax = new Vector2(-2f, 30f);

        var dd = ddGO.GetComponent<Dropdown>();
        dd.ClearOptions(); dd.AddOptions(options);
        dd.value = Mathf.Clamp(value, 0, options.Count - 1);
        dd.RefreshShownValue();
        dd.onValueChanged.AddListener(cb);
        navItems.Add(dd);
    }

    // ── Low-level helpers ────────────────────────────────────────────────────────
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
        t.enableWordWrapping = false;
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
        cb.normalColor = new Color(0.24f, 0.22f, 0.3f, 1f);
        cb.highlightedColor = Gold;
        cb.pressedColor = new Color(0.6f, 0.42f, 0.14f, 1f);
        cb.selectedColor = Gold;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        var t = NewText("Text", go.transform, text, 22, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return btn;
    }
}
