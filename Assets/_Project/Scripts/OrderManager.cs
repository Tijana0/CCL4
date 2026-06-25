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
    public GameObject ingredientIconPrefab; // New prefab for dynamic icons

    [Header("Process Icons (shown under each card)")]
    [Tooltip("Icon for tea orders (made in the teapot/cooker).")]
    public Sprite teapotProcessIcon;
    [Tooltip("Icon for vision orders (made at the crystal ball).")]
    public Sprite crystalBallProcessIcon;
    [Tooltip("Icon for prophecy orders (made at the prophecy table).")]
    public Sprite prophecyProcessIcon;

    [Header("Configuration")]
    public List<Recipe> availableRecipes;
    public float spawnInterval = 9f;
    public int maxOrders = 3;

    // Room recipes set by RoomManager at runtime
    private List<RoomRecipe> roomRecipes = new List<RoomRecipe>();
    private bool usingRoomRecipes = false;
    private bool useProcedural = false;

    // Discovered procedural orders
    private List<RoomRecipe> discoveredOrders = new List<RoomRecipe>();

    private float timeSinceLastSpawn = 0f;

    /// <summary>Called by RoomManager to register this room's recipe list.</summary>
    public void SetRoomRecipes(List<RoomRecipe> recipes, bool procedural, List<ItemData> roomItems)
    {
        roomRecipes = recipes ?? new List<RoomRecipe>();
        useProcedural = procedural;
        usingRoomRecipes = roomRecipes.Count > 0 || useProcedural;

        if (useProcedural)
        {
            DiscoverProceduralOrders(roomItems);
        }

        Debug.Log($"[OrderManager] Initialized. Recipes: {roomRecipes.Count}, Procedural: {useProcedural} ({discoveredOrders.Count} discovered).");
    }

    private void DiscoverProceduralOrders(List<ItemData> items)
    {
        discoveredOrders.Clear();
        if (items == null) return;

        // 1. Add base ingredients as simple orders
        foreach (var item in items)
        {
            if (item == null) continue;
            discoveredOrders.Add(new RoomRecipe {
                recipeName = item.itemName,
                requiredOutput = item,
                scoreValue = 5,
                timeLimit = 45f
            });
        }

        // 2. Discover combinations
        foreach (var item in items)
        {
            if (item == null) continue;

            if (item.combineRules != null)
            {
                foreach (var rule in item.combineRules)
                {
                    if (rule == null) continue;
                    // If both inputs and output exist in the room (or are valid results)
                    if (rule.outputItem != null)
                    {
                        // Check if we already added this result to avoid duplicates
                        if (!discoveredOrders.Exists(r => r.requiredOutput == rule.outputItem))
                        {
                            discoveredOrders.Add(new RoomRecipe {
                                recipeName = rule.outputItem.itemName,
                                requiredOutput = rule.outputItem,
                                scoreValue = 15,
                                timeLimit = 60f
                            });
                        }
                    }
                }
            }

            // 3. Discover processing results
            if (item.processingRules != null)
            {
                foreach (var rule in item.processingRules)
                {
                    if (rule == null) continue;
                    if (rule.outputItem != null)
                    {
                        if (!discoveredOrders.Exists(r => r.requiredOutput == rule.outputItem))
                        {
                            discoveredOrders.Add(new RoomRecipe {
                                recipeName = rule.outputItem.itemName,
                                requiredOutput = rule.outputItem,
                                scoreValue = 10,
                                timeLimit = 50f
                            });
                        }
                    }
                }
            }
        }
    }

    private void Start()
    {
        // Dynamically find UI elements to prevent prefab reference loss
        if (orderContainer == null)
        {
            GameObject canvas = GameObject.Find("UI_Canvas");
            if (canvas != null)
            {
                Transform container = canvas.transform.Find("TopLeftBoxes");
                if (container != null) orderContainer = container;
            }
        }

        // Clear any existing editor placeholders
        if (orderContainer != null)
        {
            foreach (Transform child in orderContainer)
            {
                Destroy(child.gameObject);
            }
        }

        SpawnRandomOrder();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.timeRemaining <= 0) return;
        if (orderContainer == null) return;

        if (orderContainer.childCount < maxOrders)
        {
            timeSinceLastSpawn += Time.deltaTime;
            if (timeSinceLastSpawn >= spawnInterval)
            {
                timeSinceLastSpawn = 0f;
                SpawnRandomOrder();
            }
        }
    }

    public void SpawnRandomOrder()
    {
        // Prefer room recipes or procedural if registered
        if (usingRoomRecipes)
        {
            SpawnRoomRecipeOrder();
            return;
        }

        // Fallback to legacy visual recipes
        if (orderCardPrefab == null)
        {
            Debug.LogError("[OrderManager] orderCardPrefab is missing! Cannot spawn orders.");
            return;
        }
        if (orderContainer == null)
        {
            Debug.LogError("[OrderManager] orderContainer is missing! Cannot spawn orders.");
            return;
        }

        if (availableRecipes == null || availableRecipes.Count == 0)
        {
            Debug.LogWarning("[OrderManager] No availableRecipes or roomRecipes to spawn.");
            return;
        }

        Recipe recipe = availableRecipes[Random.Range(0, availableRecipes.Count)];
        SpawnLegacyOrderCard(recipe);
    }

    private void SpawnRoomRecipeOrder()
    {
        if (orderCardPrefab == null)
        {
            Debug.LogError("[OrderManager] orderCardPrefab is missing! Cannot spawn room recipe orders.");
            return;
        }
        if (orderContainer == null)
        {
            Debug.LogError("[OrderManager] orderContainer is missing! Cannot spawn room recipe orders.");
            return;
        }

        RoomRecipe recipe;
        if (useProcedural && discoveredOrders.Count > 0)
        {
            // Pick from discovered or manual recipes
            List<RoomRecipe> pool = new List<RoomRecipe>(discoveredOrders);
            pool.AddRange(roomRecipes);
            recipe = pool[Random.Range(0, pool.Count)];
        }
        else
        {
            if (roomRecipes.Count == 0) return;
            recipe = roomRecipes[Random.Range(0, roomRecipes.Count)];
        }

        GameObject newCard = Instantiate(orderCardPrefab, orderContainer);
        newCard.name = "ActiveOrder_" + recipe.recipeName;

        // Store recipe reference on the card for matching at hand-in
        var tracker = newCard.AddComponent<ActiveOrderTracker>();
        tracker.roomRecipe = recipe;
        tracker.SetupTimer(recipe.timeLimit, () => {
            Destroy(newCard);
            Debug.Log($"[OrderManager] Order expired: {recipe.recipeName}");
        });

        // Update card UI — use item icon if available
        Transform mainDish = newCard.transform.Find("MainDish");
        if (mainDish != null && recipe.requiredOutput?.icon != null)
        {
            Image img = mainDish.GetComponent<Image>();
            if (img != null) img.sprite = recipe.requiredOutput.icon;
        }

        // Dynamic Ingredients UI
        Transform ingredientsParent = newCard.transform.Find("Ingredients");
        if (ingredientsParent != null)
        {
            // Clear existing placeholder icons
            foreach (Transform child in ingredientsParent) Destroy(child.gameObject);

            // Find ingredients for this output
            List<ItemData> ings = GetIngredientsFor(recipe.requiredOutput);
            foreach (var ing in ings)
            {
                if (ing.icon == null) continue;
                
                GameObject iconObj = ingredientIconPrefab != null 
                    ? Instantiate(ingredientIconPrefab, ingredientsParent)
                    : new GameObject("IngredientIcon", typeof(RectTransform), typeof(Image));
                
                iconObj.transform.SetParent(ingredientsParent);
                // The icon prefab has a light "Chip" backing as its root and the actual
                // item sprite on a child called "Icon" (so dark icons like the green herb
                // stay visible). Fall back to the root Image if there's no child.
                Transform iconChild = iconObj.transform.Find("Icon");
                Image img = iconChild != null ? iconChild.GetComponent<Image>() : iconObj.GetComponent<Image>();
                img.sprite = ing.icon;
                img.raycastTarget = false;
            }
        }

        // Process icon (badge under the card): which station makes this order?
        Transform processT = newCard.transform.Find("ProcessIcon");
        if (processT != null)
        {
            Sprite ps = GetProcessIconFor(recipe.requiredOutput);
            Transform pIcon = processT.Find("Icon");
            Image pimg = pIcon != null ? pIcon.GetComponent<Image>() : processT.GetComponent<Image>();
            if (ps != null) { pimg.sprite = ps; processT.gameObject.SetActive(true); }
            else processT.gameObject.SetActive(false);
        }
    }

    /// <summary>Returns the process/station icon for a deliverable (teapot / crystal ball / prophecy).</summary>
    private Sprite GetProcessIconFor(ItemData target)
    {
        if (target == null) return null;
        foreach (var pc in FindObjectsByType<PortableCooker>(FindObjectsSortMode.None))
        {
            if (pc.recipes == null) continue;
            foreach (var r in pc.recipes)
                if (r != null && r.outputItem == target) return teapotProcessIcon;
        }
        foreach (var st in FindObjectsByType<MultiIngredientStation>(FindObjectsSortMode.None))
        {
            if (st.recipes == null) continue;
            foreach (var r in st.recipes)
                if (r != null && r.outputItem == target)
                {
                    if (st.stationType == StationType.CrystalBall) return crystalBallProcessIcon;
                    if (st.stationType == StationType.ProphecyTable) return prophecyProcessIcon;
                }
        }
        return null;
    }

    private List<ItemData> GetIngredientsFor(ItemData target)
    {
        List<ItemData> ings = new List<ItemData>();
        if (target == null) return ings;

        // ── Station recipes are the real source of truth ──────────────────────
        // Visions (crystal ball), prophecy cards (prophecy table) and teas (teapot)
        // are produced by stations, not by ItemData combine/process rules. Look
        // there first so an order shows the ingredients it's actually made of
        // (e.g. vision 2 = bookblue + crystal).
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

        // Check combinations (looking for results in other items)
        // Note: This is a simple 1-level search. 
        // We'll search all items in the project or room to see who produces this target.
        // For efficiency, we scan the room items.
        
        // This is a bit tricky because ItemData rules point forward (A+B=C).
        // To find ingredients for C, we scan all items to see which rule results in C.
        
        // Let's use a simpler heuristic: if we are in a room, check all available items.
        var roomManager = FindFirstObjectByType<RoomManager>();
        List<ItemData> allRoomItems = roomManager != null ? roomManager.GetAvailableItems() : new List<ItemData>();

        foreach (var item in allRoomItems)
        {
            if (item == null) continue;
            foreach (var rule in item.combineRules)
            {
                if (rule.outputItem == target)
                {
                    ings.Add(item);
                    ings.Add(rule.otherItem);
                    return ings;
                }
            }
            foreach (var rule in item.processingRules)
            {
                if (rule.outputItem == target)
                {
                    ings.Add(item);
                    return ings;
                }
            }
        }

        // If no ingredients found, it's a base item
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
            if (ing1 != null)
            {
                Image i1 = ing1.GetComponent<Image>();
                i1.sprite = recipe.ing1IsCircle ? circleSprite : null;
                i1.color = recipe.ing1Color;
            }
            Transform ing2 = ings.GetChild(1);
            if (ing2 != null)
            {
                Image i2 = ing2.GetComponent<Image>();
                i2.sprite = recipe.ing2IsCircle ? circleSprite : null;
                i2.color = recipe.ing2Color;
            }
        }
    }

    /// <summary>
    /// Attempts to complete an order matching the given item.
    /// Returns the score value if matched, 0 if no match found.
    /// Called by InstantStation (HandIn) when a player submits an item.
    /// </summary>
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

    public void SetupTimer(float duration, System.Action expirationCallback)
    {
        totalTime = duration;
        timeRemaining = duration;
        onExpired = expirationCallback;
        
        // Find timer bar (expecting a child named "TimerBar" with an Image component)
        Transform tBar = transform.Find("TimerBar");
        if (tBar != null) timerBar = tBar.GetComponent<Image>();
        
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        timeRemaining -= Time.deltaTime;
        
        if (timerBar != null)
        {
            timerBar.fillAmount = timeRemaining / totalTime;
            
            // Visual feedback: turn red when low
            if (timeRemaining < totalTime * 0.25f)
                timerBar.color = Color.red;
        }

        if (timeRemaining <= 0)
        {
            isInitialized = false;
            onExpired?.Invoke();
        }
    }
}
