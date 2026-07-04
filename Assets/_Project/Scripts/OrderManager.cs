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

    [Header("Process Icons (shown under each card)")]
    public Sprite teapotProcessIcon;
    public Sprite crystalBallProcessIcon;
    public Sprite prophecyProcessIcon;
    public Sprite mortarProcessIcon;
    public Sprite choppingProcessIcon;
    public Sprite cauldronProcessIcon;
    public Sprite brazierProcessIcon;

    [Header("Card Visuals")]
    [Tooltip("The RoundedRect sprite used for the card border and inner panel.")]
    public Sprite roundedRectSprite;
    [Tooltip("Card border / outer colour (gold).")]
    public Color cardBorderColor = new Color(0.65f, 0.35f, 0.85f, 1f);
    [Tooltip("Card inner background colour (dark).")]
    public Color cardInnerColor = new Color(0.18f, 0.08f, 0.28f, 0.97f);
    [Tooltip("Divider line colour.")]
    public Color dividerColor = new Color(0.75f, 0.45f, 0.95f, 0.5f);
    [Tooltip("Timer bar fill colour.")]
    public Color timerBarColor = new Color(0.80f, 0.55f, 1.00f, 1f);

    [Header("Card Layout")]
    public float cardWidth = 130f;
    public float mainDishSize = 70f;
    public float iconSize = 52f;
    public float iconGap = 6f;
    public float padding = 10f;
    public float timerBarHeight = 6f;
    public float processIconSize = 36f;

    [Header("Configuration")]
    public List<Recipe> availableRecipes;
    public float spawnInterval = 9f;
    public int maxOrders = 3;

    private List<RoomRecipe> roomRecipes = new List<RoomRecipe>();
    private bool usingRoomRecipes = false;
    private bool useProcedural = false;
    private List<RoomRecipe> discoveredOrders = new List<RoomRecipe>();
    private float timeSinceLastSpawn = 0f;

    public void SetRoomRecipes(List<RoomRecipe> recipes, bool procedural, List<ItemData> roomItems)
    {
        roomRecipes = recipes ?? new List<RoomRecipe>();
        useProcedural = procedural;
        usingRoomRecipes = roomRecipes.Count > 0 || useProcedural;
        if (useProcedural) DiscoverProceduralOrders(roomItems);
        Debug.Log($"[OrderManager] Initialized. Recipes: {roomRecipes.Count}, Procedural: {useProcedural} ({discoveredOrders.Count} discovered).");
    }

    private void DiscoverProceduralOrders(List<ItemData> items)
    {
        discoveredOrders.Clear();
        if (items == null) return;

        foreach (var item in items)
        {
            if (item == null) continue;
            if (!item.isDisposable) continue;
            discoveredOrders.Add(new RoomRecipe { recipeName = item.itemName, requiredOutput = item, scoreValue = 5, timeLimit = 150f });
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
                Transform container = canvas.transform.Find("TopLeftBoxes");
                if (container != null) orderContainer = container;
            }
        }
        if (orderContainer != null)
            foreach (Transform child in orderContainer)
                Destroy(child.gameObject);

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

    // ── Dynamic card builder ──────────────────────────────────────────────────

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

        // ── Gather data first so we know the total height ─────────────────────
        List<ItemData> ings = GetIngredientsFor(recipe.requiredOutput);

        // Tea sort — herb before water/cup
        if (recipe.requiredOutput != null && recipe.requiredOutput.itemName.ToLower().Contains("tea"))
        {
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

        // Only icons that exist
        List<ItemData> displayIngs = new List<ItemData>();
        foreach (var ing in ings) if (ing != null && ing.icon != null) displayIngs.Add(ing);

        int ingCount = displayIngs.Count;
        int ingCols = Mathf.Min(ingCount, 2);
        int ingRows = ingCount == 0 ? 0 : Mathf.CeilToInt((float)ingCount / 2f);
        float ingAreaH = ingRows > 0 ? ingRows * iconSize + (ingRows - 1) * iconGap : 0f;

        List<Sprite> processSprites = GetProcessIconsFor(recipe.requiredOutput);
        float processAreaH = processSprites.Count > 0 ? processIconSize + padding : 0f;

        // Total card height: padding + mainDish + padding + divider + padding + ingredients + padding + processIcons + padding + timerBar + padding
        float dividerH = 2f;
        float totalH = padding
                       + mainDishSize
                       + padding
                       + dividerH
                       + padding
                       + ingAreaH
                       + padding
                       + processAreaH
                       + timerBarHeight
                       + padding;

        // ── Card root ─────────────────────────────────────────────────────────
        GameObject card = new GameObject("ActiveOrder_" + recipe.recipeName, typeof(RectTransform));
        card.transform.SetParent(orderContainer, false);

        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(cardWidth, totalH);

        // Gold border (the card itself)
        Image border = card.AddComponent<Image>();
        border.sprite = roundedRectSprite;
        border.color = cardBorderColor;
        border.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        // ── Inner dark panel ──────────────────────────────────────────────────
        GameObject inner = MakeChild(card, "Inner");
        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(5f, 5f); innerRT.offsetMax = new Vector2(-5f, -5f);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.sprite = roundedRectSprite;
        innerImg.color = cardInnerColor;
        innerImg.type = roundedRectSprite != null ? Image.Type.Sliced : Image.Type.Simple;

        // ── Layout cursor — start from top ────────────────────────────────────
        float cursor = totalH * 0.5f - padding; // positive = above centre

        // ── Main dish icon ────────────────────────────────────────────────────
        cursor -= mainDishSize * 0.5f;
        if (recipe.requiredOutput?.icon != null)
        {
            GameObject dish = MakeChild(card, "MainDish");
            RectTransform dishRT = dish.GetComponent<RectTransform>();
            dishRT.anchorMin = new Vector2(0.5f, 0.5f); dishRT.anchorMax = new Vector2(0.5f, 0.5f);
            dishRT.sizeDelta = new Vector2(mainDishSize, mainDishSize);
            dishRT.anchoredPosition = new Vector2(0f, cursor);
            Image dishImg = dish.AddComponent<Image>();
            dishImg.sprite = recipe.requiredOutput.icon;
            dishImg.preserveAspect = true;
        }
        cursor -= mainDishSize * 0.5f + padding;

        // ── Divider ───────────────────────────────────────────────────────────
        GameObject div = MakeChild(card, "Divider");
        RectTransform divRT = div.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.1f, 0.5f); divRT.anchorMax = new Vector2(0.9f, 0.5f);
        divRT.sizeDelta = new Vector2(0f, dividerH);
        divRT.anchoredPosition = new Vector2(0f, cursor);
        div.AddComponent<Image>().color = dividerColor;
        cursor -= dividerH + padding;

        // ── Ingredient icons grid ─────────────────────────────────────────────
        if (ingCount > 0)
        {
            float gridW = ingCols * iconSize + (ingCols - 1) * iconGap;
            float startX = -gridW * 0.5f + iconSize * 0.5f;
            float topOfGrid = cursor;

            for (int i = 0; i < ingCount; i++)
            {
                int col = i % 2;
                int row = i / 2;

                GameObject iconObj = ingredientIconPrefab != null
                    ? Instantiate(ingredientIconPrefab, card.transform)
                    : MakeChild(card, "Ing_" + i);

                iconObj.transform.SetParent(card.transform, false);
                iconObj.name = "Ing_" + i;

                RectTransform iRT = iconObj.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.5f, 0.5f);
                iRT.anchorMax = new Vector2(0.5f, 0.5f);
                iRT.sizeDelta = new Vector2(iconSize, iconSize);
                iRT.anchoredPosition = new Vector2(
                    startX + col * (iconSize + iconGap),
                    topOfGrid - iconSize * 0.5f - row * (iconSize + iconGap)
                );

                // Resize Icon child to fill slot
                Transform iconChild = iconObj.transform.Find("Icon");
                if (iconChild != null)
                {
                    RectTransform cRT = iconChild.GetComponent<RectTransform>();
                    if (cRT != null) { cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one; cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero; }
                }

                Image img = iconChild != null ? iconChild.GetComponent<Image>() : iconObj.GetComponent<Image>();
                if (img != null) { img.sprite = displayIngs[i].icon; img.preserveAspect = true; img.raycastTarget = false; }
            }

            cursor -= ingAreaH + padding;
        }

        // ── Process icons ─────────────────────────────────────────────────────
        if (processSprites.Count > 0)
        {
            Color fireColor = new Color(0.75f, 0.35f, 0.15f, 1f);
            float totalPW = processSprites.Count * processIconSize + (processSprites.Count - 1) * iconGap;
            float pStartX = -totalPW * 0.5f + processIconSize * 0.5f;
            float pY = cursor - processIconSize * 0.5f;

            for (int i = 0; i < processSprites.Count; i++)
            {
                GameObject pIcon = MakeChild(card, "ProcessIcon_" + i);
                RectTransform pRT = pIcon.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0.5f, 0.5f); pRT.anchorMax = new Vector2(0.5f, 0.5f);
                pRT.sizeDelta = new Vector2(processIconSize, processIconSize);
                pRT.anchoredPosition = new Vector2(pStartX + i * (processIconSize + iconGap), pY);
                Image pImg = pIcon.AddComponent<Image>();
                pImg.sprite = processSprites[i];
                pImg.color = GetProcessIconColor(processSprites[i], fireColor);
                pImg.preserveAspect = true;
            }

            cursor -= processIconSize + padding;
        }

        // ── Timer bar ─────────────────────────────────────────────────────────
        float timerY = -totalH * 0.5f + padding + timerBarHeight * 0.5f;

        GameObject timerBG = MakeChild(card, "TimerBarBG");
        RectTransform tbgRT = timerBG.GetComponent<RectTransform>();
        tbgRT.anchorMin = new Vector2(0.5f, 0.5f); tbgRT.anchorMax = new Vector2(0.5f, 0.5f);
        tbgRT.sizeDelta = new Vector2(cardWidth - padding * 2f, timerBarHeight);
        tbgRT.anchoredPosition = new Vector2(0f, timerY);
        Sprite whiteBG = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        Image bgImg = timerBG.AddComponent<Image>();
        bgImg.sprite = whiteBG;
        bgImg.color = new Color(0.2f, 0.18f, 0.14f, 1f);

        GameObject timerFill = MakeChild(timerBG, "TimerBar");
        RectTransform tfRT = timerFill.GetComponent<RectTransform>();
        tfRT.anchorMin = new Vector2(0f, 0f); tfRT.anchorMax = new Vector2(1f, 1f);
        tfRT.offsetMin = Vector2.zero; tfRT.offsetMax = Vector2.zero;
        // Use a plain white sprite so Image.Type.Filled actually respects fillAmount
        Sprite whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));

        Image tfImg = timerFill.AddComponent<Image>();
        tfImg.sprite = whiteSprite;
        tfImg.color = timerBarColor;
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
        }, tfImg, border);
    }

    // ── Helper — make an empty RectTransform child ────────────────────────────
    private GameObject MakeChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    // ── Colour / icon helpers (unchanged) ─────────────────────────────────────

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
            if (pc.recipes != null) foreach (var r in pc.recipes) if (r?.outputItem == target) return teapotProcessIcon;
        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
            if (st.recipes != null) foreach (var r in st.recipes) if (r?.outputItem == target)
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

    private void SpawnLegacyOrderCard(Recipe recipe)
    {
        GameObject newCard = Instantiate(orderCardPrefab, orderContainer);
        newCard.name = "ActiveOrder_" + recipe.recipeName;

        Transform mainDish = newCard.transform.Find("MainDish");
        if (mainDish != null)
        {
            Image img = mainDish.GetComponent<Image>();
            img.sprite = recipe.mainDishSprite;
            RectTransform rt = mainDish.GetComponent<RectTransform>();
            rt.sizeDelta = recipe.mainDishSize;
        }

        Transform ings = newCard.transform.Find("Ingredients");
        if (ings != null)
        {
            Transform ing1 = ings.GetChild(0);
            if (ing1 != null) { Image i1 = ing1.GetComponent<Image>(); i1.sprite = recipe.ing1IsCircle ? circleSprite : null; i1.color = recipe.ing1Color; }
            Transform ing2 = ings.GetChild(1);
            if (ing2 != null) { Image i2 = ing2.GetComponent<Image>(); i2.sprite = recipe.ing2IsCircle ? circleSprite : null; i2.color = recipe.ing2Color; }
        }
    }

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
                Debug.Log($"[OrderManager] Order complete! +{score} points for {submittedItem.itemName}");
                return score;
            }
        }
        Debug.Log($"[OrderManager] No active order matched {submittedItem.itemName}");
        return 0;
    }
}

/// <summary>Sits on an active order card to track which RoomRecipe it represents.</summary>
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

    // timerBar passed directly — Awake fires before children exist
    public void SetupTimer(float duration, System.Action expirationCallback, Image timerBarImage, Image rootImage)
    {
        totalTime = duration;
        timeRemaining = duration;
        onExpired = expirationCallback;
        timerBar = timerBarImage;
        cardImage = rootImage;
        if (cardImage != null) originalColor = cardImage.color;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;
        timeRemaining -= Time.deltaTime;

        if (timerBar != null)
        {
            timerBar.fillAmount = Mathf.Clamp01(timeRemaining / totalTime);
            if (timeRemaining < totalTime * 0.25f) timerBar.color = Color.red;
        }

        if (timeRemaining <= 10f || timeRemaining <= totalTime * 0.25f)
        {
            if (cardImage != null)
            {
                float t = Mathf.PingPong(Time.time * 10f, 1f);
                cardImage.color = Color.Lerp(originalColor, new Color(1f, 0.2f, 0.2f, originalColor.a), t);
            }
        }
        else
        {
            if (cardImage != null && cardImage.color != originalColor)
                cardImage.color = originalColor;
        }

        if (timeRemaining <= 0) { isInitialized = false; onExpired?.Invoke(); }
    }
}