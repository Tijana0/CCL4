using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Code-built Settings screen: a framed card filling the screen with a single
/// scrollable column — Audio settings on top, then large Keyboard / Controller
/// reference images below. Opened from the pause menu.
///
/// Full controller support: every audio control plus each control-image panel is
/// reachable with up/down (so a controller can scroll the big images into view),
/// left/right adjusts the selected slider. The scroll is clamped (no dragging
/// past the content) and follows the selected item.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private Action onBack;

    private CanvasGroup musicVolumeGroup;
    private Slider musicVolumeSlider;

    private DefaultControls.Resources uiRes;
    private readonly List<Selectable> navItems = new List<Selectable>();
    private ScrollRect scrollRect;
    private GameObject lastSelected;

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

        panel = NewRect("SettingsPanel", canvas.transform);
        Stretch(panel.GetComponent<RectTransform>());
        panel.AddComponent<Image>().color = Dim;

        var edge = NewRect("CardEdge", panel.transform);
        var edgeRT = edge.GetComponent<RectTransform>();
        edgeRT.anchorMin = new Vector2(0.085f, 0.055f); edgeRT.anchorMax = new Vector2(0.915f, 0.945f);
        edgeRT.offsetMin = Vector2.zero; edgeRT.offsetMax = Vector2.zero;
        edge.AddComponent<Image>().color = CardEdge;

        var card = NewRect("Card", panel.transform);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.09f, 0.06f); cardRT.anchorMax = new Vector2(0.91f, 0.94f);
        cardRT.offsetMin = Vector2.zero; cardRT.offsetMax = Vector2.zero;
        card.AddComponent<Image>().color = CardBg;

        // Title + underline (fixed at top)
        var title = NewText("SettingsTitle", card.transform, "SETTINGS", 38, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var tr = title.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -12f); tr.sizeDelta = new Vector2(0f, 48f);
        var ul = NewRect("TitleAccent", card.transform);
        var ulRT = ul.GetComponent<RectTransform>();
        ulRT.anchorMin = new Vector2(0.5f, 1f); ulRT.anchorMax = new Vector2(0.5f, 1f); ulRT.pivot = new Vector2(0.5f, 1f);
        ulRT.anchoredPosition = new Vector2(0f, -58f); ulRT.sizeDelta = new Vector2(110f, 3f);
        ul.AddComponent<Image>().color = Gold;

        // Scroll view (between title and Back)
        var scrollGO = NewRect("Scroll", card.transform);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0f); scrollRT.anchorMax = new Vector2(0.95f, 1f);
        scrollRT.offsetMin = new Vector2(0f, 70f);    // room for Back
        scrollRT.offsetMax = new Vector2(0f, -72f);   // room for title
        
        // Add a transparent Image to serve as the Raycast Target so mouse scrolling/dragging works
        var scrollImg = scrollGO.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollImg.raycastTarget = true;

        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.scrollSensitivity = 28f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollGO.AddComponent<RectMask2D>();
        scrollRect.viewport = scrollRT;

        var content = NewRect("Content", scrollGO.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f); contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
        vlg.spacing = 12f; vlg.padding = new RectOffset(8, 8, 4, 10);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ── AUDIO ──────────────────────────────────────────────────────────────
        AddHeader(content.transform, "AUDIO");
        AddSlider(content.transform, "Master", s.masterVolume, v => SettingsManager.Instance.SetMasterVolume(v), out _, out _);
        AddToggle(content.transform, "Background Music", s.musicEnabled, on =>
        {
            SettingsManager.Instance.SetMusicEnabled(on);
            SetMusicVolumeEnabled(on);
        });
        AddSlider(content.transform, "Music", s.musicVolume, v => SettingsManager.Instance.SetMusicVolume(v),
                  out musicVolumeSlider, out musicVolumeGroup);
        SetMusicVolumeEnabled(s.musicEnabled);
        AddSlider(content.transform, "SFX", s.sfxVolume, v => SettingsManager.Instance.SetSfxVolume(v), out _, out _);

        // ── CONTROLS (large reference images, below audio) ─────────────────────
        AddHeader(content.transform, "CONTROLS");
        AddControlImage(content.transform, "controls_keyboard", 280f);
        AddControlImage(content.transform, "controls_gamepad", 280f);

        // Back (fixed at bottom of card)
        var back = MakeButton(card.transform, "BackButton", "Back");
        var backRT = back.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0f); backRT.anchorMax = new Vector2(0.5f, 0f); backRT.pivot = new Vector2(0.5f, 0f);
        backRT.anchoredPosition = new Vector2(0f, 16f); backRT.sizeDelta = new Vector2(220f, 46f);
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
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        lastSelected = null;
        FocusFirst();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        lastSelected = null;
        onBack?.Invoke();
    }

    private void FocusFirst()
    {
        if (navItems.Count == 0 || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(navItems[0].gameObject);
    }

    private void Update()
    {
        if (!IsOpen || EventSystem.current == null) return;
        var sel = EventSystem.current.currentSelectedGameObject;
        if (sel == null || !sel.activeInHierarchy || !sel.transform.IsChildOf(panel.transform)) { FocusFirst(); return; }
        
        if (sel != lastSelected)
        {
            lastSelected = sel;
            var selRT = sel.GetComponent<RectTransform>();
            if (scrollRect != null && scrollRect.content != null && selRT != null && selRT.IsChildOf(scrollRect.content))
                EnsureVisible(selRT);
        }
    }

    private void EnsureVisible(RectTransform target)
    {
        RectTransform vp = scrollRect.viewport, ct = scrollRect.content;
        if (vp == null || ct == null) return;

        // Convert target's world corners to viewport's local space to do math in pixel space
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        for (int i = 0; i < 4; i++)
        {
            corners[i] = vp.InverseTransformPoint(corners[i]);
        }

        float vTop = vp.rect.yMax;
        float vBot = vp.rect.yMin;
        float tTop = corners[1].y;
        float tBot = corners[0].y;

        const float pad = 8f;
        Vector2 ap = ct.anchoredPosition;

        if (tTop > vTop - pad)
        {
            ap.y -= (tTop - (vTop - pad));
        }
        else if (tBot < vBot + pad)
        {
            ap.y += ((vBot + pad) - tBot);
        }
        else
        {
            return;
        }

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
        var row = Row(parent, 38f);
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
        var row = Row(parent, 46f);
        group = row.AddComponent<CanvasGroup>();
        var lab = NewText("Label", row.transform, label, 20, TextAlignmentOptions.Left);
        var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0f, 0.5f); lrt.anchorMax = new Vector2(0.35f, 1f);
        lrt.offsetMin = new Vector2(2f, 0f); lrt.offsetMax = Vector2.zero;

        var valText = NewText("Value", row.transform, Mathf.RoundToInt(value) + "%", 20, TextAlignmentOptions.Right);
        valText.color = new Color(1f, 1f, 1f, 0.75f);
        var vt = valText.rectTransform; vt.anchorMin = new Vector2(0.88f, 0.5f); vt.anchorMax = new Vector2(1f, 1f);
        vt.offsetMin = Vector2.zero; vt.offsetMax = Vector2.zero;

        var sliderGO = DefaultControls.CreateSlider(uiRes);
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row.transform, false);
        var srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.37f, 0.5f); srt.anchorMax = new Vector2(0.86f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
        srt.offsetMin = new Vector2(0f, -10f); srt.offsetMax = new Vector2(0f, 10f);

        slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
        slider.value = value;
        var valRef = valText;
        slider.onValueChanged.AddListener(v => { valRef.text = Mathf.RoundToInt(v) + "%"; cb(v); });
        navItems.Add(slider);
    }

    private void AddToggle(Transform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> cb)
    {
        var row = Row(parent, 40f);
        var lab = NewText("Label", row.transform, label, 20, TextAlignmentOptions.Left);
        var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.6f, 1f);
        lrt.offsetMin = new Vector2(2f, 0f); lrt.offsetMax = Vector2.zero;

        var toggleGO = DefaultControls.CreateToggle(uiRes);
        toggleGO.name = "Toggle";
        toggleGO.transform.SetParent(row.transform, false);
        var rt = toggleGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.37f, 0.5f); rt.anchorMax = new Vector2(0.37f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(28f, 28f);
        var lbl = toggleGO.GetComponentInChildren<Text>();
        if (lbl != null) lbl.gameObject.SetActive(false);

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(cb);
        navItems.Add(toggle);
    }

    // A large control-scheme reference image (Resources/UI/<name>.png). It carries a
    // no-op Selectable so a controller can navigate to it and scroll it into view.
    private void AddControlImage(Transform parent, string resourceName, float height)
    {
        var row = Row(parent, height);

        var imgGO = NewRect(resourceName + "_Image", row.transform);
        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400f, height);

        var img = imgGO.AddComponent<Image>();
        var sprite = LoadControlSprite(resourceName);
        if (sprite != null) { img.sprite = sprite; img.preserveAspect = true; img.color = Color.white; }
        else
        {
            img.color = new Color(1f, 1f, 1f, 0.06f);
            var ph = NewText("Placeholder", imgGO.transform,
                "Add  Resources/UI/" + resourceName + ".png", 16, TextAlignmentOptions.Center);
            ph.color = new Color(1f, 1f, 1f, 0.45f); Stretch(ph.rectTransform);
        }

        // navigable (no visual change) so controller users can scroll to the image
        var sel = imgGO.AddComponent<Button>();
        sel.transition = Selectable.Transition.None;
        sel.targetGraphic = img;
        navItems.Add(sel);
    }

    private Sprite LoadControlSprite(string name)
    {
        var sp = Resources.Load<Sprite>("UI/" + name);
        if (sp != null) return sp;
        var tex = Resources.Load<Texture2D>("UI/" + name);
        if (tex != null) return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        return null;
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
