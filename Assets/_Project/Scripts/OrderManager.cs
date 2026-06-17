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

    [Header("Configuration")]
    public List<Recipe> availableRecipes;
    public float spawnInterval = 9f;
    public int maxOrders = 3;

    // Room recipes set by RoomManager at runtime
    private List<RoomRecipe> roomRecipes = new List<RoomRecipe>();
    private bool usingRoomRecipes = false;

    private float timeSinceLastSpawn = 0f;

    /// <summary>Called by RoomManager to register this room's recipe list.</summary>
    public void SetRoomRecipes(List<RoomRecipe> recipes)
    {
        roomRecipes = recipes ?? new List<RoomRecipe>();
        usingRoomRecipes = roomRecipes.Count > 0;
        Debug.Log($"[OrderManager] Registered {roomRecipes.Count} room recipes.");
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
        // Prefer room recipes if registered
        if (usingRoomRecipes && roomRecipes.Count > 0)
        {
            SpawnRoomRecipeOrder();
            return;
        }

        // Fallback to legacy visual recipes
        if (availableRecipes == null || availableRecipes.Count == 0 || orderCardPrefab == null || orderContainer == null) return;

        Recipe recipe = availableRecipes[Random.Range(0, availableRecipes.Count)];
        SpawnLegacyOrderCard(recipe);
    }

    private void SpawnRoomRecipeOrder()
    {
        if (orderCardPrefab == null || orderContainer == null) return;

        RoomRecipe recipe = roomRecipes[Random.Range(0, roomRecipes.Count)];

        GameObject newCard = Instantiate(orderCardPrefab, orderContainer);
        newCard.name = "ActiveOrder_" + recipe.recipeName;

        // Store recipe reference on the card for matching at hand-in
        var tracker = newCard.AddComponent<ActiveOrderTracker>();
        tracker.roomRecipe = recipe;

        // Update card UI — use item icon if available
        Transform mainDish = newCard.transform.Find("MainDish");
        if (mainDish != null && recipe.requiredOutput?.icon != null)
        {
            Image img = mainDish.GetComponent<Image>();
            if (img != null) img.sprite = recipe.requiredOutput.icon;
        }
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
}
