using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Replaces CauldronStation. Handles any station that takes multiple
/// ingredients and produces a result. Fully configurable per station
/// via the Inspector — no code changes needed for new station types.
///
/// TRIGGER MODES:
///   OnFirstIngredient — starts processing as soon as first item is added
///   HoldToProcess     — player must hold interact to brew (like original cauldron)
///   OnLastIngredient  — starts automatically when the final ingredient is added
///
/// RECIPE ORDER:
///   orderMatters = false — any order works, checks all ingredients present
///   orderMatters = true  — wrong order = wrong result (uses wrongOrderResult)
///
/// BOIL OVER:
///   enableBoilOver = true  — ruins result if left too long after done
///   enableBoilOver = false — result waits forever until picked up
///
/// SETUP:
///   1. Add this component to your station object
///   2. Set stationType (Cauldron, or add new types to StationType enum)
///   3. Pick a TriggerMode
///   4. Add recipes in the Inspector
///   5. Assign CounterTopPoint child
///   6. Optionally assign state visuals and progress bar
/// </summary>
public class MultiIngredientStation : StationBase
{
    // ── Trigger mode ────────────────────────────────────────────────────────
    public enum TriggerMode
    {
        OnFirstIngredient,  // Starts processing when first item is added
        HoldToProcess,      // Player holds interact to brew
        OnLastIngredient    // Starts automatically when last ingredient added
    }

    [Header("Station Behaviour")]
    public TriggerMode triggerMode = TriggerMode.HoldToProcess;

    [Header("Boil Over")]
    public bool enableBoilOver = true;
    [Tooltip("Seconds after cooking completes before boiling over. Only used if enableBoilOver is true.")]
    public float boilOverDelay = 15f;

    [Header("Recipes")]
    public List<MultiIngredientRecipe> recipes = new List<MultiIngredientRecipe>();

    [Header("State Visuals (optional)")]
    [Tooltip("Shown when station is empty")]
    public GameObject emptyVisual;
    [Tooltip("Shown while cooking/processing")]
    public GameObject activeVisual;
    [Tooltip("Shown when result is ready")]
    public GameObject doneVisual;
    [Tooltip("Shown when boiled over / ruined")]
    public GameObject ruinedVisual;

    [Header("Progress Bar UI (optional)")]
    public Image progressBarFill;
    public GameObject progressBarContainer;

    [Header("Result Spawn Point (optional)")]
    public Transform resultSpawnPoint;

    // ── Runtime state ────────────────────────────────────────────────────────
    private List<ItemData> ingredients = new List<ItemData>();
    private StationState stationState = StationState.Empty;
    private float cookingProgress = 0f;
    private float currentCookingTime = 5f;
    private float boilOverTimer = 0f;
    private SimplePlayerController cookingPlayer = null;
    private GameObject resultItem = null;

    private enum StationState { Empty, HasIngredients, Cooking, Done, Ruined }

    private void Start()
    {
        UpdateVisuals();
        if (progressBarContainer != null) progressBarContainer.SetActive(false);
    }

    private void Update()
    {
        switch (stationState)
        {
            case StationState.Cooking:
                UpdateCooking();
                break;
            case StationState.Done:
                if (enableBoilOver) UpdateBoilOver();
                break;
        }
    }

    // ── Interaction ──────────────────────────────────────────────────────────

    public override void Interact(SimplePlayerController player)
    {
        // Pick up finished result
        if ((stationState == StationState.Done || stationState == StationState.Ruined) && resultItem != null)
        {
            if (player.heldItem == null)
            {
                PickUpResult(player);
                return;
            }
        }

        bool playerHasItem = player.heldItem != null;

        if (playerHasItem)
        {
            TryAddIngredient(player);
        }
        else if (stationState == StationState.HasIngredients && triggerMode == TriggerMode.HoldToProcess)
        {
            // Player holds interact to start cooking
            StartCooking(player);
        }
    }

    // ── Ingredient handling ──────────────────────────────────────────────────

    private void TryAddIngredient(SimplePlayerController player)
    {
        WorldItem wi = player.heldItem.GetComponent<WorldItem>();
        if (wi == null)
        {
            Debug.Log("[MultiIngredientStation] Item has no WorldItem component.");
            return;
        }

        // Reject items that need processing first
        if (wi.itemData.requiresProcessingBeforeCauldron)
        {
            Debug.Log($"[MultiIngredientStation] {wi.itemData.itemName} must be processed first!");
            return;
        }

        // Reject if already cooking or done
        if (stationState == StationState.Cooking || stationState == StationState.Done)
        {
            Debug.Log("[MultiIngredientStation] Station is busy.");
            return;
        }

        // Add ingredient
        ingredients.Add(wi.itemData);
        Destroy(player.heldItem);
        player.heldItem = null;

        Debug.Log($"[MultiIngredientStation] Added {wi.itemData.itemName}. Total: {ingredients.Count}");

        stationState = StationState.HasIngredients;
        UpdateVisuals();

        // Check trigger mode
        switch (triggerMode)
        {
            case TriggerMode.OnFirstIngredient:
                if (ingredients.Count == 1)
                    StartCooking(null);
                break;

            case TriggerMode.OnLastIngredient:
                // Check if any recipe is now fully satisfied
                MultiIngredientRecipe match = FindMatchingRecipe();
                if (match != null)
                    StartCooking(null);
                break;

            case TriggerMode.HoldToProcess:
                MultiIngredientRecipe possible = FindMatchingRecipe();
                if (possible != null)
                    Debug.Log($"[MultiIngredientStation] Recipe ready: {possible.recipeName}. Hold interact to brew.");
                break;
        }
    }

    // ── Cooking ──────────────────────────────────────────────────────────────

    private void StartCooking(SimplePlayerController player)
    {
        MultiIngredientRecipe match = FindMatchingRecipe();
        if (match == null && triggerMode != TriggerMode.OnFirstIngredient)
        {
            Debug.Log("[MultiIngredientStation] No matching recipe for current ingredients.");
            return;
        }

        cookingProgress = 0f;
        cookingPlayer = player;
        currentCookingTime = match != null ? match.cookingTime : 5f;
        stationState = StationState.Cooking;

        if (progressBarContainer != null) progressBarContainer.SetActive(true);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        UpdateVisuals();
        Debug.Log($"[MultiIngredientStation] Cooking started — {currentCookingTime}s");
    }

    private void UpdateCooking()
    {
        // HoldToProcess pauses if player releases
        if (triggerMode == TriggerMode.HoldToProcess && !CheckPlayerHoldingInteract())
            return;

        cookingProgress += Time.deltaTime / currentCookingTime;
        if (progressBarFill != null)
            progressBarFill.fillAmount = cookingProgress;

        if (cookingProgress >= 1f)
            CompleteCooking();
    }

    private void CompleteCooking()
    {
        cookingPlayer = null;
        stationState = StationState.Done;
        boilOverTimer = 0f;

        if (progressBarContainer != null) progressBarContainer.SetActive(false);

        // Find result — check ordered recipes first, then unordered
        MultiIngredientRecipe match = FindMatchingRecipe();
        ItemData resultData = match?.outputItem;

        if (resultData == null)
        {
            Debug.LogWarning("[MultiIngredientStation] No recipe matched at completion.");
            ingredients.Clear();
            stationState = StationState.Empty;
            UpdateVisuals();
            return;
        }

        SpawnResult(resultData);
        ingredients.Clear();
        UpdateVisuals();
        Debug.Log($"[MultiIngredientStation] Done! Result: {resultData.itemName}");
    }

    private void SpawnResult(ItemData data)
    {
        Transform anchor = resultSpawnPoint != null ? resultSpawnPoint
                         : counterTopPoint != null ? counterTopPoint
                         : transform;

        WorldItem result = WorldItem.CreateCombined(data, anchor.position + Vector3.up * 0.3f, anchor);
        if (result != null)
        {
            resultItem = result.gameObject;
            result.transform.localPosition = new Vector3(0, 0.3f, 0);

            Rigidbody rb = result.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Collider col = result.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    // ── Boil over ────────────────────────────────────────────────────────────

    private void UpdateBoilOver()
    {
        boilOverTimer += Time.deltaTime;
        if (boilOverTimer >= boilOverDelay)
        {
            stationState = StationState.Ruined;
            UpdateVisuals();
            Debug.Log("[MultiIngredientStation] Boiled over — result ruined!");
        }
    }

    // ── Pick up result ───────────────────────────────────────────────────────

    private void PickUpResult(SimplePlayerController player)
    {
        if (resultItem == null || player.heldItem != null) return;

        player.heldItem = resultItem;
        resultItem.transform.SetParent(player.holdPoint);
        resultItem.transform.localPosition = Vector3.zero;
        resultItem.transform.localRotation = Quaternion.identity;

        Collider col = resultItem.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        resultItem = null;
        stationState = StationState.Empty;
        boilOverTimer = 0f;
        UpdateVisuals();
    }

    // ── Recipe matching ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds the best matching recipe for current ingredients.
    /// Ordered recipes are checked first — if order is wrong, returns wrongOrderResult.
    /// Then unordered recipes are checked.
    /// </summary>
    private MultiIngredientRecipe FindMatchingRecipe()
    {
        // Check ordered recipes first
        foreach (var recipe in recipes)
        {
            if (!recipe.orderMatters) continue;
            if (recipe.requiredIngredients.Count != ingredients.Count) continue;

            bool ordered = true;
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != recipe.requiredIngredients[i])
                {
                    ordered = false;
                    break;
                }
            }

            if (ordered) return recipe;

            // Wrong order — check if same items are present regardless of order
            if (recipe.wrongOrderResult != null && HasSameItems(recipe.requiredIngredients, ingredients))
            {
                // Return a temporary recipe with the wrong order result
                return new MultiIngredientRecipe
                {
                    recipeName          = recipe.recipeName + " (wrong order)",
                    outputItem          = recipe.wrongOrderResult,
                    cookingTime         = recipe.cookingTime,
                    orderMatters        = false
                };
            }
        }

        // Check unordered recipes
        foreach (var recipe in recipes)
        {
            if (recipe.orderMatters) continue;
            if (recipe.requiredIngredients.Count != ingredients.Count) continue;
            if (HasSameItems(recipe.requiredIngredients, ingredients)) return recipe;
        }

        return null;
    }

    private bool HasSameItems(List<ItemData> a, List<ItemData> b)
    {
        if (a.Count != b.Count) return false;
        List<ItemData> remaining = new List<ItemData>(a);
        foreach (var item in b)
            if (!remaining.Remove(item)) return false;
        return remaining.Count == 0;
    }

    // ── Input check ──────────────────────────────────────────────────────────

    private bool CheckPlayerHoldingInteract()
    {
        if (cookingPlayer == null) return false;

        if (cookingPlayer.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.eKey.isPressed;
        if (cookingPlayer.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.rightShiftKey.isPressed;
        if (cookingPlayer.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.buttonSouth.isPressed;

        return false;
    }

    // ── Visuals ──────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        if (emptyVisual  != null) emptyVisual.SetActive(stationState == StationState.Empty);
        if (activeVisual != null) activeVisual.SetActive(stationState == StationState.Cooking || stationState == StationState.HasIngredients);
        if (doneVisual   != null) doneVisual.SetActive(stationState == StationState.Done);
        if (ruinedVisual != null) ruinedVisual.SetActive(stationState == StationState.Ruined);
    }

    // ── Public utility ───────────────────────────────────────────────────────

    /// <summary>Clears the station — called by Sink or Bin if needed.</summary>
    public void ClearStation()
    {
        if (resultItem != null) Destroy(resultItem);
        resultItem = null;
        ingredients.Clear();
        stationState = StationState.Empty;
        cookingProgress = 0f;
        boilOverTimer = 0f;
        cookingPlayer = null;

        if (progressBarContainer != null) progressBarContainer.SetActive(false);
        UpdateVisuals();
    }
}

/// <summary>
/// A recipe for a MultiIngredientStation.
/// orderMatters = true  — ingredients must be added in the listed order.
///                        Wrong order produces wrongOrderResult if assigned.
/// orderMatters = false — any order works, just checks all ingredients present.
/// </summary>
[System.Serializable]
public class MultiIngredientRecipe
{
    public string recipeName;
    public List<ItemData> requiredIngredients = new List<ItemData>();
    public ItemData outputItem;
    [Tooltip("Seconds to process. Used by all trigger modes.")]
    public float cookingTime = 5f;
    [Tooltip("If true, ingredients must be added in the exact order listed above.")]
    public bool orderMatters = false;
    [Tooltip("Result if orderMatters is true but player adds ingredients in wrong order. Leave empty to produce nothing.")]
    public ItemData wrongOrderResult;
}