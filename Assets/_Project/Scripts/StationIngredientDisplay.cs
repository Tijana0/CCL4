using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Drop this on ANY station or portable cooker to get Overcooked-style
/// ingredient bubbles floating above it. No other scripts need modification
/// EXCEPT making a few private fields public (see FIELDS TO MAKE PUBLIC below).
///
/// ?? FIELDS TO MAKE PUBLIC ????????????????????????????????????????????????????
///
/// In MultiIngredientStation.cs, change these four lines:
///   private List<ItemData> ingredients      ?  [HideInInspector] public List<ItemData> ingredients
///   private StationState stationState       ?  [HideInInspector] public StationState stationState
///   private GameObject resultItem           ?  [HideInInspector] public GameObject resultItem
///   private enum StationState { ... }       ?  public enum StationState { ... }
///
/// In ProcessingStation.cs, change these two lines:
///   private bool resultReady                ?  [HideInInspector] public bool resultReady
///   private float processingProgress        ?  [HideInInspector] public float processingProgress
///
/// In PortableCooker.cs, change these two lines:
///   private List<ItemData> ingredients      ?  [HideInInspector] public List<ItemData> ingredients
///   private ItemData lastBrewedResult       ?  already public via LastBrewedResult property — no change needed
///   (IsDone is already a public property — no change needed)
///
/// That's it. Then add THIS component to any station GameObject.
/// ?????????????????????????????????????????????????????????????????????????????
/// </summary>
public class StationIngredientDisplay : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("How high above this object's origin the bubbles float.")]
    public float hoverHeight = 1.8f;
    [Tooltip("World-space diameter of each bubble.")]
    public float bubbleSize = 0.16f;
    [Tooltip("World-space gap between bubbles.")]
    public float bubbleSpacing = 0.03f;

    [Header("Visuals")]
    public Color plateColor = new Color(0.13f, 0.10f, 0.08f, 0.93f);
    public Color rimColor = new Color(0.85f, 0.72f, 0.28f, 1f);
    public Color resultRimColor = new Color(0.98f, 0.92f, 0.30f, 1f);
    [Tooltip("Fallback fill colours for items with no icon. Cycles by slot index.")]
    public List<Color> fallbackColors = new List<Color>();

    // ?? Station references ????????????????????????????????????????????????????
    private MultiIngredientStation multiStation;
    private ProcessingStation processingStation;
    private PortableCooker portableCooker;

    // ?? Canvas ????????????????????????????????????????????????????????????????
    private GameObject canvasGO;
    private RectTransform canvasRect;

    // ?? Bubble slots ??????????????????????????????????????????????????????????
    private struct BubbleSlot { public GameObject root; public Image rim, plate, icon, fill; }
    private readonly List<BubbleSlot> slots = new List<BubbleSlot>();

    // ?? Change detection ??????????????????????????????????????????????????????
    private List<ItemData> shownItems = new List<ItemData>();
    private bool shownIsDone = false;

    // ?? Default pastel palette ????????????????????????????????????????????????
    private static readonly Color[] Pastels =
    {
        new Color(0.55f, 0.78f, 0.95f),
        new Color(0.95f, 0.65f, 0.55f),
        new Color(0.65f, 0.90f, 0.65f),
        new Color(0.90f, 0.80f, 0.55f),
        new Color(0.80f, 0.65f, 0.90f),
        new Color(0.90f, 0.90f, 0.55f),
    };

    private static Sprite _circle;
    private static Sprite Circle()
    {
        if (_circle == null)
            _circle = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        return _circle;
    }

    // ?? Lifecycle ?????????????????????????????????????????????????????????????

    private void Awake()
    {
        multiStation = GetComponent<MultiIngredientStation>();
        processingStation = GetComponent<ProcessingStation>();
        portableCooker = GetComponent<PortableCooker>();

        if (fallbackColors == null || fallbackColors.Count == 0)
            fallbackColors = new List<Color>(Pastels);

        BuildCanvas();
    }

    private void LateUpdate()
    {
        // Billboard — always face camera
        if (canvasGO != null && Camera.main != null)
            canvasGO.transform.rotation = Camera.main.transform.rotation;

        Poll();
    }

    // ?? Canvas setup ??????????????????????????????????????????????????????????

    private void BuildCanvas()
    {
        canvasGO = new GameObject("IngredientDisplay");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, hoverHeight, 0f);

        // Scale: 100 canvas pixels = bubbleSize world units
        canvasGO.transform.localScale = Vector3.one * (bubbleSize / 100f);

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.sortingOrder = 5;

        canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(500f, 150f);

        canvasGO.SetActive(false);
    }

    // ?? State polling ?????????????????????????????????????????????????????????

    private void Poll()
    {
        List<ItemData> toShow = new List<ItemData>();
        bool isDone = false;

        // ?? MultiIngredientStation ????????????????????????????????????????????
        if (multiStation != null)
        {
            bool isRuined = multiStation.stationState == MultiIngredientStation.StationState.Ruined;
            isDone = multiStation.stationState == MultiIngredientStation.StationState.Done || isRuined;

            if (isDone && multiStation.resultItem != null)
            {
                WorldItem wi = multiStation.resultItem.GetComponent<WorldItem>();
                if (wi != null) toShow.Add(wi.itemData);
            }
            else if (!isDone)
            {
                toShow.AddRange(multiStation.ingredients);
            }
        }
        // ?? ProcessingStation ?????????????????????????????????????????????????
        else if (processingStation != null)
        {
            isDone = processingStation.resultReady;

            if (processingStation.itemOnStation != null)
            {
                WorldItem wi = processingStation.itemOnStation.GetComponent<WorldItem>();
                if (wi != null) toShow.Add(wi.itemData);
            }
        }
        // ?? PortableCooker (Teapot) ???????????????????????????????????????????
        else if (portableCooker != null)
        {
            isDone = portableCooker.IsDone;

            if (isDone && portableCooker.LastBrewedResult != null)
            {
                toShow.Add(portableCooker.LastBrewedResult);
            }
            else if (!isDone)
            {
                toShow.AddRange(portableCooker.ingredients);
            }
        }

        // Rebuild only if something changed
        if (isDone == shownIsDone && ListsMatch(toShow, shownItems)) return;

        shownItems = new List<ItemData>(toShow);
        shownIsDone = isDone;
        Rebuild(toShow, isDone);
    }

    // ?? Slot rebuilding ???????????????????????????????????????????????????????

    private void Rebuild(List<ItemData> items, bool isDone)
    {
        foreach (var s in slots)
            if (s.root != null) Destroy(s.root);
        slots.Clear();

        if (items == null || items.Count == 0)
        {
            canvasGO.SetActive(false);
            return;
        }

        canvasGO.SetActive(true);

        float px = 100f;
        float gap = (bubbleSpacing / bubbleSize) * px;
        float totalW = items.Count * px + (items.Count - 1) * gap;

        canvasRect.sizeDelta = new Vector2(totalW + px * 0.6f, px * 1.5f);

        float startX = -(totalW * 0.5f) + px * 0.5f;

        for (int i = 0; i < items.Count; i++)
            slots.Add(MakeBubble(items[i], startX + i * (px + gap), px, isDone, i));
    }

    private BubbleSlot MakeBubble(ItemData item, float x, float px, bool isResult, int idx)
    {
        BubbleSlot s = new BubbleSlot();

        s.root = new GameObject(item != null ? item.itemName : "bubble");
        s.root.transform.SetParent(canvasRect, false);
        var rt = s.root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(px, px);
        rt.anchoredPosition = new Vector2(x, 0f);

        s.rim = Img(s.root, "Rim", Circle(), isResult ? resultRimColor : rimColor,
                      new Vector2(-7f, -7f), new Vector2(7f, 7f));
        s.plate = Img(s.root, "Plate", Circle(), plateColor,
                      Vector2.zero, Vector2.zero);

        bool hasIcon = item != null && item.icon != null;

        s.fill = Img(s.root, "Fill", Circle(),
                     hasIcon ? new Color(0, 0, 0, 0) : Fallback(item, idx),
                     new Vector2(10f, 10f), new Vector2(-10f, -10f));

        s.icon = Img(s.root, "Icon", item?.icon,
                     hasIcon ? Color.white : new Color(0, 0, 0, 0),
                     new Vector2(12f, 12f), new Vector2(-12f, -12f));
        if (s.icon != null) s.icon.preserveAspect = true;

        return s;
    }

    private Image Img(GameObject parent, string name, Sprite sprite, Color color,
                      Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
        return img;
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private Color Fallback(ItemData item, int idx)
    {
        if (item != null && item.potionColour != Color.white && item.potionColour != default)
            return item.potionColour;
        return fallbackColors.Count > 0
            ? fallbackColors[idx % fallbackColors.Count]
            : Pastels[idx % Pastels.Length];
    }

    private static bool ListsMatch(List<ItemData> a, List<ItemData> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}