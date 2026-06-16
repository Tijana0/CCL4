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
    public float spawnInterval = 9f; // Spawns a new order every 9 seconds
    public int maxOrders = 3;
    
    private float timeSinceLastSpawn = 0f;

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

        // Spawn the first order immediately
        SpawnRandomOrder();
    }

    private void Update()
    {
        // Stop spawning if the game is over
        if (GameManager.Instance != null && GameManager.Instance.timeRemaining <= 0) return;

        // Spawn new orders over time if we have room
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
        if (availableRecipes == null || availableRecipes.Count == 0 || orderCardPrefab == null || orderContainer == null) return;

        // Pick a random recipe
        Recipe recipe = availableRecipes[Random.Range(0, availableRecipes.Count)];
        
        // Instantiate the card
        GameObject newCard = Instantiate(orderCardPrefab, orderContainer);
        newCard.name = "ActiveOrder_" + recipe.recipeName;
        
        // Setup Main Dish
        Transform mainDish = newCard.transform.Find("MainDish");
        if (mainDish != null)
        {
            Image img = mainDish.GetComponent<Image>();
            img.sprite = recipe.mainDishSprite;
            RectTransform rt = mainDish.GetComponent<RectTransform>();
            rt.sizeDelta = recipe.mainDishSize;
        }

        // Setup Ingredients
        Transform ings = newCard.transform.Find("Ingredients");
        if (ings != null)
        {
            // Ingredient 1
            Transform ing1 = ings.GetChild(0);
            if (ing1 != null)
            {
                Image i1 = ing1.GetComponent<Image>();
                i1.sprite = recipe.ing1IsCircle ? circleSprite : null;
                i1.color = recipe.ing1Color;
            }
            
            // Ingredient 2
            Transform ing2 = ings.GetChild(1);
            if (ing2 != null)
            {
                Image i2 = ing2.GetComponent<Image>();
                i2.sprite = recipe.ing2IsCircle ? circleSprite : null;
                i2.color = recipe.ing2Color;
            }
        }
    }
}
