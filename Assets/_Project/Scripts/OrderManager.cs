using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class Recipe
{
    public string recipeName;
    public Sprite mainDishSprite;
    public Vector2 mainDishSize;
    public bool ing1IsCircle;
    public Color ing1Color;
    public bool ing2IsCircle;
    public Color ing2Color;
}

public class OrderManager : MonoBehaviour
{
    [Header("References")]
    public GameObject orderCardPrefab;
    public Transform orderContainer;
    public Sprite circleSprite;
    public GameObject ingredientIconPrefab;

    [Header("Process Icons")]
    public Sprite teapotProcessIcon;
    public Sprite crystalBallProcessIcon;
    public Sprite prophecyProcessIcon;
    public Sprite mortarProcessIcon;
    public Sprite choppingProcessIcon;
    public Sprite cauldronProcessIcon;
    public Sprite brazierProcessIcon;

    [Header("Card Sprites")]
    [Tooltip("RoundedRect sprite for card panels. Leave empty for plain rectangles.")]
    public Sprite roundedRectSprite;
    [Tooltip("Optional decorative corner/stamp sprite shown behind the main dish.")]
    public Sprite dishBackSprite;

    [Header("Configuration")]
    public List<Recipe> availableRecipes;
    public float spawnInterval = 9f;
    public int maxOrders = 3;

    // ── Colours ───────────────────────────────────────────────────────────────
    static readonly Color ColBorder = new Color(0.80f, 0.65f, 0.15f, 1.00f); // gold
    static readonly Color ColBorderGlow = new Color(0.95f, 0.85f, 0.35f, 1.00f); // bright gold rim
    static readonly Color ColInner = new Color(0.09f, 0.07f, 0.05f, 0.98f); // near-black parchment
    static readonly Color ColPanel = new Color(0.14f, 0.11f, 0.08f, 1.00f); // slightly lighter panel
    static readonly Color ColDivider = new Color(0.80f, 0.65f, 0.15f, 0.40f); // faded gold line
    static readonly Color ColDishBack = new Color(0.20f, 0.16f, 0.10f, 1.00f); // dark gold-tinted circle behind dish
    static readonly Color ColIngBG = new Color(0.18f, 0.14f, 0.10f, 1.00f); // ingredient slot background
    static readonly Color ColIngRim = new Color(0.80f, 0.65f, 0.15f, 0.70f); // ingredient slot rim
    static readonly Color ColTimerBG = new Color(0.08f, 0.06f, 0.04f, 1.00f); // timer track
    static readonly Color ColTimer = new Color(0.80f, 0.65f, 0.15f, 1.00f); // timer fill gold
    static readonly Color ColTimerLow = new Color(0.85f, 0.18f, 0.10f, 1.00f); // timer fill red
    static readonly Color ColProcessBG = new Color(0.16f, 0.12f, 0.08f, 1.00f); // process badge bg

    // ── Layout constants ──────────────────────────────────────────────────────
    const float CardW = 138f;
    const float BorderPad = 5f;  // outer border thickness
    const float InnerPad = 10f;  // inner content padding
    const float DishSize = 72f;  // main result icon
    const float DishBackSize = 84f;  // decorative circle behind dish
    const float IngSize = 50f;  // ingredient icon slots
    const float IngGap = 6f;
    const float ProcSize = 32f;  // process badge icons
    const float ProcGap = 5f;
    const float DivH = 1.5f;
    const float TimerH = 7f;
    const float SectionGap = 8f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private List<RoomRecipe> roomRecipes = new List<RoomRecipe>();
    private bool usingRoomRecipes = false;
    private bool useProcedural = false;
    private List<RoomRecipe> discoveredOrders = new List<RoomRecipe>();
    private float timeSinceLastSpawn = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────────────────────────────────────

    public void SetRoomRecipes(List<RoomRecipe> recipes, bool procedural, List<ItemData> roomItems)
    {
        roomRecipes = recipes ?? new List<RoomRecipe>();
        useProcedural = procedural;
        usingRoomRecipes = roomRecipes.Count > 0 || useProcedural;
        if (useProcedural) DiscoverProceduralOrders(roomItems);
        Debug.Log($"[OrderManager] Recipes: {roomRecipes.Count}, Procedural: {useProcedural} ({discoveredOrders.Count} discovered).");
    }

    private void DiscoverProceduralOrders(List<ItemData> items)
    {
        discoveredOrders.Clear();
        if (items == null) return;
        foreach (var item in items)
        {
            if (item == null || !item.isDisposable) continue;
            discoveredOrders.Add(new RoomRecipe { recipeName = item.itemName, requiredOutput = item, scoreValue = 5, timeLimit = 45f });
        }
        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.combineRules != null)
                foreach (var rule in item.combineRules)
                    if (rule?.outputItem != null && !discoveredOrders.Exists(r => r.requiredOutput == rule.outputItem))
                        discoveredOrders.Add(new RoomRecipe { recipeName = rule.outputItem.itemName, requiredOutput = rule.outputItem, scoreValue = 15, timeLimit = 60f });
            if (item.processingRules != null)
                foreach (var rule in item.processingRules)
                    if (rule?.outputItem != null && !discoveredOrders.Exists(r => r.requiredOutput == rule.outputItem))
                        discoveredOrders.Add(new RoomRecipe { recipeName = rule.outputItem.itemName, requiredOutput = rule.outputItem, scoreValue = 10, timeLimit = 50f });
        }
    }

    private void Start()
    {
        if (orderContainer == null)
        {
            GameObject canvas = GameObject.Find("UI_Canvas");
            if (canvas != null)
            {
                Transform t = canvas.transform.Find("TopLeftBoxes");
                if (t != null) orderContainer = t;
            }
        }
        if (orderContainer != null)
            foreach (Transform child in orderContainer) Destroy(child.gameObject);
        SpawnRandomOrder();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.timeRemaining <= 0) return;
        if (orderContainer == null) return;
        if (orderContainer.childCount < maxOrders)
        {
            timeSinceLastSpawn += Time.deltaTime;
            if (timeSinceLastSpawn >= spawnInterval) { timeSinceLastSpawn = 0f; SpawnRandomOrder(); }
        }
    }

    public void SpawnRandomOrder()
    {
        if (usingRoomRecipes) { SpawnRoomRecipeOrder(); return; }
        if (orderCardPrefab == null || orderContainer == null) return;
        if (availableRecipes == null || availableRecipes.Count == 0) return;
        SpawnLegacyOrderCard(availableRecipes[Random.Range(0, availableRecipes.Count)]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Card builder
    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnRoomRecipeOrder()
    {
        if (orderContainer == null) return;

        RoomRecipe recipe;
        if (useProcedural && discoveredOrders.Count > 0)
        {
            var pool = new List<RoomRecipe>(discoveredOrders);
            pool.AddRange(roomRecipes);
            recipe = pool[Random.Range(0, pool.Count)];
        }
        else
        {
            if (roomRecipes.Count == 0) return;
            recipe = roomRecipes[Random.Range(0, roomRecipes.Count)];
        }

        // ── Gather data ───────────────────────────────────────────────────────
        List<ItemData> ings = GetIngredientsFor(recipe.requiredOutput);
        SortTeaIngredients(recipe.requiredOutput, ings);

        List<ItemData> displayIngs = new List<ItemData>();
        foreach (var ing in ings) if (ing != null && ing.icon != null) displayIngs.Add(ing);

        int ingCount = displayIngs.Count;
        int ingCols = Mathf.Min(ingCount, 2);
        int ingRows = ingCount == 0 ? 0 : Mathf.CeilToInt((float)ingCount / 2f);
        float ingAreaH = ingRows > 0 ? ingRows * IngSize + (ingRows - 1) * IngGap : 0f;

        List<Sprite> procSprites = GetProcessIconsFor(recipe.requiredOutput);
        float procAreaH = procSprites.Count > 0 ? ProcSize + SectionGap : 0f;

        // Total card height
        float totalH = BorderPad * 2f
                     + InnerPad
                     + DishSize
                     + SectionGap
                     + DivH
                     + SectionGap
                     + (ingAreaH > 0 ? ingAreaH + SectionGap : 0f)
                     + procAreaH
                     + TimerH
                     + InnerPad;

        // ── Root card (gold outer border) ─────────────────────────────────────
        GameObject card = new GameObject("ActiveOrder_" + recipe.recipeName, typeof(RectTransform));
        card.transform.SetParent(orderContainer, false);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(CardW, totalH);

        // Outer gold glow border (slightly larger, brighter)
        Image borderImg = card.AddComponent<Image>();
        borderImg.sprite = roundedRectSprite;
        borderImg.color = ColBorderGlow;
        borderImg.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        // Inner gold border
        GameObject border2GO = MakePanel(card, "Border", ColBorder,
            new Vector2(2f, 2f), new Vector2(-2f, -2f));

        // Dark inner background
        GameObject innerGO = MakePanel(border2GO, "Inner", ColInner,
            new Vector2(BorderPad, BorderPad), new Vector2(-BorderPad, -BorderPad));

        // ── Layout cursor from top of inner ───────────────────────────────────
        // We position children relative to card centre using anchoredPosition
        float halfH = totalH * 0.5f;
        float cursor = halfH - BorderPad - InnerPad; // start at top inner edge, move downward

        // ── Dish back circle ──────────────────────────────────────────────────
        cursor -= DishBackSize * 0.5f;
        GameObject dishBack = MakeChild(card, "DishBack");
        SetAnchored(dishBack, new Vector2(CardW * 0.5f, totalH * 0.5f),
                    new Vector2(DishBackSize, DishBackSize),
                    new Vector2(0f, cursor));
        Image dbImg = dishBack.AddComponent<Image>();
        dbImg.sprite = dishBackSprite != null ? dishBackSprite : roundedRectSprite;
        dbImg.color = ColDishBack;
        dbImg.type = Image.Type.Sliced;

        // ── Main dish icon ────────────────────────────────────────────────────
        if (recipe.requiredOutput?.icon != null)
        {
            GameObject dish = MakeChild(card, "MainDish");
            SetAnchored(dish, new Vector2(CardW * 0.5f, totalH * 0.5f),
                        new Vector2(DishSize, DishSize),
                        new Vector2(0f, cursor));
            Image dImg = dish.AddComponent<Image>();
            dImg.sprite = recipe.requiredOutput.icon;
            dImg.preserveAspect = true;
        }
        cursor -= DishBackSize * 0.5f + SectionGap;

        // ── Gold divider ──────────────────────────────────────────────────────
        GameObject div = MakeChild(card, "Divider");
        float divW = CardW - BorderPad * 4f;
        SetAnchored(div, new Vector2(CardW * 0.5f, totalH * 0.5f),
                    new Vector2(divW, DivH),
                    new Vector2(0f, cursor));
        div.AddComponent<Image>().color = ColDivider;
        cursor -= DivH + SectionGap;

        // ── Ingredient grid ───────────────────────────────────────────────────
        if (ingCount > 0)
        {
            float gridW = ingCols * IngSize + (ingCols - 1) * IngGap;
            float startX = -gridW * 0.5f + IngSize * 0.5f;

            for (int i = 0; i < ingCount; i++)
            {
                int col = i % 2;
                int row = i / 2;
                float ix = startX + col * (IngSize + IngGap);
                float iy = cursor - IngSize * 0.5f - row * (IngSize + IngGap);

                // Slot background circle
                GameObject slot = MakeChild(card, "IngSlot_" + i);
                SetAnchored(slot, new Vector2(CardW * 0.5f, totalH * 0.5f),
                            new Vector2(IngSize + 6f, IngSize + 6f),
                            new Vector2(ix, iy));
                Image slotImg = slot.AddComponent<Image>();
                slotImg.sprite = roundedRectSprite;
                slotImg.color = ColIngRim;
                slotImg.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

                // Slot inner
                GameObject slotInner = MakePanel(slot, "SlotInner", ColIngBG,
                    new Vector2(3f, 3f), new Vector2(-3f, -3f));

                // Icon — use prefab if assigned, otherwise plain Image
                GameObject iconObj = ingredientIconPrefab != null
                    ? Instantiate(ingredientIconPrefab, card.transform)
                    : MakeChild(card, "Ing_" + i);
                iconObj.name = "Ing_" + i;
                iconObj.transform.SetParent(card.transform, false);

                RectTransform iRT = iconObj.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.5f, 0.5f);
                iRT.anchorMax = new Vector2(0.5f, 0.5f);
                iRT.sizeDelta = new Vector2(IngSize - 6f, IngSize - 6f);
                iRT.anchoredPosition = new Vector2(ix, iy);

                Transform iconChild = iconObj.transform.Find("Icon");
                if (iconChild != null)
                {
                    RectTransform cRT = iconChild.GetComponent<RectTransform>();
                    if (cRT != null) { cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one; cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero; }
                }
                Image img = iconChild != null ? iconChild.GetComponent<Image>() : iconObj.GetComponent<Image>();
                if (img != null) { img.sprite = displayIngs[i].icon; img.preserveAspect = true; img.raycastTarget = false; }
            }
            cursor -= ingAreaH + SectionGap;
        }

        // ── Process station badges ────────────────────────────────────────────
        if (procSprites.Count > 0)
        {
            Color fireColor = new Color(0.75f, 0.35f, 0.15f, 1f);
            float totalPW = procSprites.Count * ProcSize + (procSprites.Count - 1) * ProcGap;
            float pStartX = -totalPW * 0.5f + ProcSize * 0.5f;
            float pY = cursor - ProcSize * 0.5f;

            for (int i = 0; i < procSprites.Count; i++)
            {
                // Badge background
                GameObject badge = MakeChild(card, "ProcBadge_" + i);
                SetAnchored(badge, new Vector2(CardW * 0.5f, totalH * 0.5f),
                            new Vector2(ProcSize + 4f, ProcSize + 4f),
                            new Vector2(pStartX + i * (ProcSize + ProcGap), pY));
                Image badgeImg = badge.AddComponent<Image>();
                badgeImg.sprite = roundedRectSprite;
                badgeImg.color = ColProcessBG;
                badgeImg.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

                // Badge icon
                GameObject pIcon = MakeChild(card, "ProcIcon_" + i);
                SetAnchored(pIcon, new Vector2(CardW * 0.5f, totalH * 0.5f),
                            new Vector2(ProcSize - 4f, ProcSize - 4f),
                            new Vector2(pStartX + i * (ProcSize + ProcGap), pY));
                Image pImg = pIcon.AddComponent<Image>();
                pImg.sprite = procSprites[i];
                pImg.color = GetProcessIconColor(procSprites[i], fireColor);
                pImg.preserveAspect = true;
            }
            cursor -= ProcSize + SectionGap;
        }

        // ── Timer bar ─────────────────────────────────────────────────────────
        float timerY = -halfH + InnerPad + TimerH * 0.5f;
        float timerBarW = CardW - BorderPad * 4f;

        // Track
        GameObject timerTrack = MakeChild(card, "TimerTrack");
        SetAnchored(timerTrack, new Vector2(CardW * 0.5f, totalH * 0.5f),
                    new Vector2(timerBarW, TimerH),
                    new Vector2(0f, timerY));
        Image trackImg = timerTrack.AddComponent<Image>();
        trackImg.sprite = roundedRectSprite;
        trackImg.color = ColTimerBG;
        trackImg.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        // Fill
        GameObject timerFill = MakeChild(timerTrack, "TimerBar");
        RectTransform tfRT = timerFill.GetComponent<RectTransform>();
        tfRT.anchorMin = new Vector2(0f, 0f); tfRT.anchorMax = new Vector2(1f, 1f);
        tfRT.offsetMin = new Vector2(1f, 1f); tfRT.offsetMax = new Vector2(-1f, -1f);
        Image tfImg = timerFill.AddComponent<Image>();
        tfImg.sprite = roundedRectSprite;
        tfImg.color = ColTimer;
        tfImg.type = Image.Type.Filled;
        tfImg.fillMethod = Image.FillMethod.Horizontal;
        tfImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        tfImg.fillAmount = 1f;

        // ── Tracker ───────────────────────────────────────────────────────────
        var tracker = card.AddComponent<ActiveOrderTracker>();
        tracker.roomRecipe = recipe;
        tracker.SetupTimer(recipe.timeLimit, () => {
            Destroy(card);
            Debug.Log($"[OrderManager] Order expired: {recipe.recipeName}");
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI helpers
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject MakeChild(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    /// <summary>Full-stretch panel with offset inset.</summary>
    private GameObject MakePanel(GameObject parent, string name, Color color,
                                  Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = MakeChild(parent, name);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.sprite = roundedRectSprite;
        img.color = color;
        img.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return go;
    }

    /// <summary>Centre-anchored child with explicit size and position offset from card centre.</summary>
    private void SetAnchored(GameObject go, Vector2 parentSize,
                              Vector2 size, Vector2 centreOffset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = centreOffset;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tea ingredient sort
    // ─────────────────────────────────────────────────────────────────────────

    private void SortTeaIngredients(ItemData output, List<ItemData> ings)
    {
        if (output == null || !output.itemName.ToLower().Contains("tea")) return;
        int waterIdx = -1, herbIdx = -1;
        for (int i = 0; i < ings.Count; i++)
        {
            if (ings[i] == null) continue;
            string n = ings[i].itemName.ToLower();
            if (n.Contains("teacup") || n.Contains("water") || n.Contains("cup")) waterIdx = i;
            else if (n.Contains("herb") || n.Contains("leaf") || n.Contains("tea")) herbIdx = i;
        }
        if (waterIdx != -1 && herbIdx != -1 && waterIdx < herbIdx)
        { var t = ings[waterIdx]; ings[waterIdx] = ings[herbIdx]; ings[herbIdx] = t; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Process icon logic (unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private Color GetProcessIconColor(Sprite sprite, Color fireColor)
    {
        if (sprite == teapotProcessIcon) return new Color(0.8f, 0.7f, 0.2f, 1f);
        if (sprite == brazierProcessIcon) return fireColor;
        if (sprite == crystalBallProcessIcon) return new Color(0.55f, 0.3f, 0.7f, 1f);
        if (sprite == prophecyProcessIcon) return new Color(0.3f, 0.5f, 0.7f, 1f);
        return Color.white;
    }

    private Sprite GetProcessIconFor(ItemData target)
    {
        if (target == null) return null;
        foreach (var pc in FindObjectsByType<PortableCooker>(FindObjectsSortMode.None))
            if (pc.recipes != null) foreach (var r in pc.recipes)
                    if (r?.outputItem == target) return teapotProcessIcon;
        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
            if (st.recipes != null) foreach (var r in st.recipes)
                    if (r?.outputItem == target)
                    {
                        if (st.stationType == StationType.CrystalBall) return crystalBallProcessIcon;
                        if (st.stationType == StationType.ProphecyTable) return prophecyProcessIcon;
                    }
        return null;
    }

    private List<Sprite> GetProcessIconsFor(ItemData target)
    {
        var icons = new List<Sprite>();
        if (target == null) return icons;

        Sprite mainIcon = null;
        ItemData brewedPrecursor = target;

        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
            if (st.containerSwapMappings != null)
                foreach (var m in st.containerSwapMappings)
                    if (m != null && m.filledContainerItem == target && m.brewedResultItem != null)
                    { brewedPrecursor = m.brewedResultItem; break; }

        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
        {
            if (st.recipes == null) continue;
            bool found = false;
            foreach (var r in st.recipes) if (r?.outputItem == brewedPrecursor) { found = true; break; }
            if (!found) continue;
            if (st.stationType == StationType.CrystalBall) mainIcon = crystalBallProcessIcon;
            else if (st.stationType == StationType.ProphecyTable) mainIcon = prophecyProcessIcon;
            else if (st.stationType == StationType.Cauldron) mainIcon = cauldronProcessIcon;
            break;
        }

        if (mainIcon == null)
            foreach (var pc in FindObjectsByType<PortableCooker>(FindObjectsSortMode.None))
            {
                if (pc.recipes == null) continue;
                bool found = false;
                foreach (var r in pc.recipes) if (r?.outputItem == target) { found = true; break; }
                if (found) { mainIcon = teapotProcessIcon; break; }
            }

        var ingredients = GetIngredientsFor(target);
        var prepStations = new HashSet<StationType>();
        foreach (var ing in ingredients)
        {
            if (ing == null) continue;
            foreach (var ps in FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None))
                if (ps.transformRules != null)
                    foreach (var rule in ps.transformRules)
                        if (rule?.outputItem == ing) prepStations.Add(ps.stationType);
        }

        foreach (var st in prepStations)
        {
            if (st == StationType.MortarAndPestle && mortarProcessIcon != null) icons.Add(mortarProcessIcon);
            else if (st == StationType.CuttingBoard && choppingProcessIcon != null) icons.Add(choppingProcessIcon);
        }

        if (mainIcon != null)
        {
            if (mainIcon == teapotProcessIcon && brazierProcessIcon != null) icons.Add(brazierProcessIcon);
            icons.Add(mainIcon);
        }

        if (icons.Count == 0) { var leg = GetProcessIconFor(target); if (leg != null) icons.Add(leg); }
        return icons;
    }

    private List<ItemData> GetIngredientsFor(ItemData target)
    {
        var ings = new List<ItemData>();
        if (target == null) return ings;

        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
        {
            if (st.containerSwapMappings == null) continue;
            foreach (var m in st.containerSwapMappings)
            {
                if (m == null || m.filledContainerItem != target || m.brewedResultItem == null || m.brewedResultItem == target) continue;
                var inner = GetIngredientsFor(m.brewedResultItem);
                if (m.containerItem != null && !inner.Contains(m.containerItem)) inner.Add(m.containerItem);
                if (inner.Count > 0) return inner;
            }
        }

        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
        {
            if (st.recipes == null) continue;
            foreach (var r in st.recipes)
            {
                if (r == null || r.outputItem != target || r.requiredIngredients == null) continue;
                foreach (var ing in r.requiredIngredients) if (ing != null) ings.Add(ing);
                if (ings.Count > 0) return ings;
            }
        }

        foreach (var pc in FindObjectsByType<PortableCooker>(FindObjectsSortMode.None))
        {
            if (pc.recipes == null) continue;
            foreach (var r in pc.recipes)
            {
                if (r == null || r.outputItem != target || r.requiredIngredients == null) continue;
                foreach (var ing in r.requiredIngredients) if (ing != null) ings.Add(ing);
                if (ings.Count > 0) return ings;
            }
        }

        var roomManager = FindFirstObjectByType<RoomManager>();
        var allRoomItems = roomManager != null ? roomManager.GetAvailableItems() : new List<ItemData>();
        foreach (var item in allRoomItems)
        {
            if (item == null) continue;
            foreach (var rule in item.combineRules)
                if (rule.outputItem == target) { ings.Add(item); ings.Add(rule.otherItem); return ings; }
            foreach (var rule in item.processingRules)
                if (rule.outputItem == target) { ings.Add(item); return ings; }
        }

        if (ings.Count == 0) ings.Add(target);
        return ings;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Legacy card (fallback, unchanged)
    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnLegacyOrderCard(Recipe recipe)
    {
        GameObject newCard = Instantiate(orderCardPrefab, orderContainer);
        newCard.name = "ActiveOrder_" + recipe.recipeName;
        Transform mainDish = newCard.transform.Find("MainDish");
        if (mainDish != null)
        {
            mainDish.GetComponent<Image>().sprite = recipe.mainDishSprite;
            mainDish.GetComponent<RectTransform>().sizeDelta = recipe.mainDishSize;
        }
        Transform ings = newCard.transform.Find("Ingredients");
        if (ings != null)
        {
            Transform ing1 = ings.GetChild(0);
            if (ing1 != null) { var i1 = ing1.GetComponent<Image>(); i1.sprite = recipe.ing1IsCircle ? circleSprite : null; i1.color = recipe.ing1Color; }
            Transform ing2 = ings.GetChild(1);
            if (ing2 != null) { var i2 = ing2.GetComponent<Image>(); i2.sprite = recipe.ing2IsCircle ? circleSprite : null; i2.color = recipe.ing2Color; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hand-in
    // ─────────────────────────────────────────────────────────────────────────

    public int TryCompleteOrder(ItemData submittedItem)
    {
        if (orderContainer == null) return 0;
        foreach (Transform card in orderContainer)
        {
            var tracker = card.GetComponent<ActiveOrderTracker>();
            if (tracker != null && tracker.roomRecipe.requiredOutput == submittedItem)
            {
                int score = tracker.roomRecipe.scoreValue;
                Destroy(card.gameObject);
                Debug.Log($"[OrderManager] Order complete! +{score} for {submittedItem.itemName}");
                return score;
            }
        }
        Debug.Log($"[OrderManager] No active order matched {submittedItem.itemName}");
        return 0;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ActiveOrderTracker — unchanged logic, updated timer path
// ─────────────────────────────────────────────────────────────────────────────

public class ActiveOrderTracker : MonoBehaviour
{
    public RoomRecipe roomRecipe;

    private float timeRemaining;
    private float totalTime;
    private System.Action onExpired;
    private bool isInitialized = false;

    private Image timerBar;
    private Image cardImage;
    private Color originalColor;

    private void Start()
    {
        cardImage = GetComponent<Image>();
        if (cardImage == null) cardImage = transform.Find("Inner")?.GetComponent<Image>();
        if (cardImage != null) originalColor = cardImage.color;

        // Timer fill is now at TimerTrack/TimerBar
        Transform track = transform.Find("TimerTrack");
        if (track != null) { Transform tb = track.Find("TimerBar"); if (tb != null) timerBar = tb.GetComponent<Image>(); }
        if (timerBar == null) { Transform tb = transform.Find("TimerBar"); if (tb != null) timerBar = tb.GetComponent<Image>(); }
    }

    public void SetupTimer(float duration, System.Action expirationCallback)
    {
        totalTime = duration; timeRemaining = duration; onExpired = expirationCallback; isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;
        timeRemaining -= Time.deltaTime;

        if (timerBar != null)
        {
            timerBar.fillAmount = Mathf.Clamp01(timeRemaining / totalTime);
            timerBar.color = timeRemaining < totalTime * 0.25f
                ? new Color(0.85f, 0.18f, 0.10f, 1f)
                : new Color(0.80f, 0.65f, 0.15f, 1f);
        }

        // Flash card border red when low
        if (cardImage != null)
        {
            if (timeRemaining <= 10f || timeRemaining <= totalTime * 0.25f)
            {
                float t = Mathf.PingPong(Time.time * 8f, 1f);
                cardImage.color = Color.Lerp(originalColor, new Color(0.8f, 0.15f, 0.08f, originalColor.a), t);
            }
            else if (cardImage.color != originalColor)
            {
                cardImage.color = originalColor;
            }
        }

        if (timeRemaining <= 0) { isInitialized = false; onExpired?.Invoke(); }
    }
}