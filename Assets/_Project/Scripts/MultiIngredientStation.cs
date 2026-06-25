using UnityEngine;
using UnityEngine.UI;
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

    [Header("Ingredients")]
    [Tooltip("Maximum number of ingredients this station accepts. 0 = unlimited.")]
    public int maxIngredients = 0;
    [Tooltip("Default cooking time if no matching recipe is found yet.")]
    public float defaultCookingTime = 5f;

    [Header("Recipes")]
    public List<MultiIngredientRecipe> recipes = new List<MultiIngredientRecipe>();

    [Header("Wrong Combo / Improvised Potion")]
    [Tooltip("Item used as the fallback result when ingredients don't match any recipe. Its material gets tinted by the blended ingredient colours.")]
    public ItemData improvisedPotionItem;
    [Tooltip("If true, the result's renderer material colour is set to the blended ingredient colour at runtime.")]
    public bool tintImprovisedResult = true;

    [Header("Requires Ingredient To Start")]
    [Tooltip("Enable to require a specific ingredient before cooking can begin.")]
    public bool requireSpecificIngredient = true;
    [Tooltip("The ingredient that must be present before cooking starts (e.g. WaterBowl). Only used if Require Specific Ingredient is checked.")]
    public ItemData requiredStartIngredient;

    [Header("Container Swap (optional)")]
    [Tooltip("If true, a player holding containerItem can interact directly with this station once a result is ready, swapping their empty container for the filled result. E.g. Empty Teacup -> Filled Teacup, without needing to carry the Teapot away.")]
    public bool allowContainerSwap = false;
    [Tooltip("The empty container item that can be swapped here. E.g. EmptyTeacup.")]
    public ItemData containerItem;
    [Tooltip("Mappings from the brewed result to what the container becomes. E.g. Tea1 result + EmptyTeacup -> TeacupFilled1.")]
    public List<ContainerSwapMapping> containerSwapMappings = new List<ContainerSwapMapping>();

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

    public override bool HasReadyResult => resultItem != null;

    // ── Runtime state ────────────────────────────────────────────────────────
    private List<ItemData> ingredients = new List<ItemData>();
    private StationState stationState = StationState.Empty;
    private float cookingProgress = 0f;
    private float currentCookingTime = 5f;
    private float boilOverTimer = 0f;
    private SimplePlayerController cookingPlayer = null;
    private GameObject resultItem = null;
    private int lastProcessedFrame = -1;

    private enum StationState { Empty, HasIngredients, Cooking, Done, Ruined }

    private void Start()
    {
        if (stationType == StationType.CrystalBall)
        {
            if (activeVisual == null)
            {
                GameObject visualParent = new GameObject("CrystalBall_ActiveVisual");
                visualParent.transform.SetParent(this.transform, false);
                visualParent.transform.localPosition = new Vector3(0f, 0.8f, 0f);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "ShiningSphere";
                sphere.transform.SetParent(visualParent.transform, false);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                Renderer sphereRenderer = sphere.GetComponent<Renderer>();
                if (sphereRenderer != null)
                {
                    Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                     ?? Shader.Find("Unlit/Color");
                    Material glowMat = new Material(unlitShader);
                    glowMat.SetColor("_BaseColor", new Color(1f, 0.95f, 0.6f, 1f));
                    sphereRenderer.material = glowMat;
                }
                Collider sphereCollider = sphere.GetComponent<Collider>();
                if (sphereCollider != null) sphereCollider.enabled = false;

                GameObject lightObj = new GameObject("ShiningLight");
                lightObj.transform.SetParent(visualParent.transform, false);
                lightObj.transform.localPosition = Vector3.zero;
                Light lightComponent = lightObj.AddComponent<Light>();
                lightComponent.type = LightType.Point;
                lightComponent.color = new Color(0.95f, 0.9f, 0.6f);
                lightComponent.intensity = 8f;
                lightComponent.range = 5f;
                lightComponent.shadows = LightShadows.None;

                activeVisual = visualParent;
            }

            // Build a world-space progress bar — no parenting, use world position directly
            if (progressBarContainer == null && counterTopPoint != null)
            {
                GameObject canvasGO = new GameObject("CrystalBall_ProgressCanvas");
                if (stationType == StationType.CrystalBall)
                {
                    Vector3 localOffset = new Vector3(-2.5f, 1.18f, 2.5f);
                    BoxCollider col = GetComponent<BoxCollider>();
                    if (col != null) localOffset = col.center;
                    Vector3 visualCenter = counterTopPoint.position + counterTopPoint.rotation * localOffset;
                    canvasGO.transform.position = visualCenter + new Vector3(0f, 0.6f, 0f);
                }
                else
                {
                    canvasGO.transform.position = counterTopPoint.position + new Vector3(-1.5f, 1.7f, 1.5f);
                }
                canvasGO.transform.localScale = Vector3.one * 0.01f;

                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;

                RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(120f, 18f);

                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(canvasGO.transform, false);
                UnityEngine.UI.Image bgImg = bg.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
                RectTransform bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(canvasGO.transform, false);
                UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
                fillImg.color = new Color(0.2f, 0.8f, 1f, 1f);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(2f, 2f);
                fillRect.offsetMax = new Vector2(-2f, -2f);
                // Pivot at left edge so localScale.x=0→empty, 1→full fills left-to-right
                fillRect.pivot = new Vector2(0f, 0.5f);
                fill.transform.localScale = new Vector3(0f, 1f, 1f);

                progressBarContainer = canvasGO;
                progressBarFill = fillImg;
                canvasGO.SetActive(false);
            }
        }

        UpdateVisuals();
    }

    private void Update()
    {
        // Billboard the progress bar toward the camera
        if (progressBarContainer != null && Camera.main != null)
            progressBarContainer.transform.rotation = Camera.main.transform.rotation;

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
        // Container swap: player holds the designated empty container (e.g. EmptyTeacup)
        // and the result is ready -> swap container for filled result directly here.
        if (allowContainerSwap && stationState == StationState.Done && resultItem != null && player.heldItem != null)
        {
            WorldItem heldWI = player.heldItem.GetComponent<WorldItem>();
            if (heldWI != null && heldWI.itemData == containerItem)
            {
                TrySwapContainer(player, heldWI.itemData);
                return;
            }
        }

        // Pick up finished result directly (raw, no container needed)
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
        // Note: HoldToProcess is now driven by StartCookingIfValid() via the
        // dedicated Process button (R), not Interact (E). See below.
    }

    /// <summary>
    /// Called every frame by SimplePlayerController.TryProcess() while the
    /// Process button is held and this station is in range with empty hands.
    /// Only relevant when triggerMode == HoldToProcess.
    /// </summary>
    public void StartCookingIfValid(SimplePlayerController player)
    {
        if (triggerMode != TriggerMode.HoldToProcess) return;
        if (player.heldItem != null) return;
        if (stationState != StationState.HasIngredients && stationState != StationState.Cooking) return;

        lastProcessedFrame = Time.frameCount;

        if (stationState != StationState.Cooking)
        {
            RestartCooking(player);
        }
        else
        {
            cookingPlayer = player; // Re-assign if a different player took over
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

        // Check max ingredients
        if (maxIngredients > 0 && ingredients.Count >= maxIngredients)
        {
            Debug.Log($"[MultiIngredientStation] Max ingredients ({maxIngredients}) reached.");
            return;
        }

        // Add ingredient
        ingredients.Add(wi.itemData);
        Destroy(player.heldItem);
        player.heldItem = null;

        Debug.Log($"[MultiIngredientStation] Added {wi.itemData.itemName}. Total: {ingredients.Count}");

        stationState = StationState.HasIngredients;
        UpdateVisuals();

        // Always restart cooking timer when ingredient is added
        // (except HoldToProcess which waits for player to hold interact)
        switch (triggerMode)
        {
            case TriggerMode.OnFirstIngredient:
                // Start cooking immediately on every new ingredient — resets timer
                RestartCooking(null);
                break;

            case TriggerMode.OnLastIngredient:
                // Only start when ALL ingredients match a recipe
                MultiIngredientRecipe match = FindMatchingRecipe();
                if (match != null)
                    RestartCooking(null);
                else
                    Debug.Log($"[MultiIngredientStation] {ingredients.Count} ingredient(s) in — waiting for full recipe match.");
                break;

            case TriggerMode.HoldToProcess:
                // Player must hold interact — just log status
                MultiIngredientRecipe possible = FindMatchingRecipe();
                Debug.Log(possible != null
                    ? $"[MultiIngredientStation] Recipe ready: {possible.recipeName}. Hold interact to brew."
                    : $"[MultiIngredientStation] {ingredients.Count} ingredient(s) in — no recipe match yet.");
                break;
        }
    }

    // ── Cooking ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts or restarts cooking with current ingredients.
    /// Does NOT require a recipe match upfront — checks at completion.
    /// Called every time a new ingredient is added.
    /// </summary>
    private void RestartCooking(SimplePlayerController player)
    {
        if (requireSpecificIngredient && requiredStartIngredient != null)
        {
            if (!HasIngredient(requiredStartIngredient))
            {
                Debug.Log($"[MultiIngredientStation] Add {requiredStartIngredient.itemName} before cooking can begin.");
                return;
            }

            // The required ingredient alone is not enough — at least one OTHER
            // ingredient must also be present.
            if (ingredients.Count < 2)
            {
                Debug.Log($"[MultiIngredientStation] {requiredStartIngredient.itemName} alone isn't enough — add another ingredient.");
                return;
            }
        }

        // Get cooking time from best matching recipe, or use default
        MultiIngredientRecipe match = FindMatchingRecipe();
        currentCookingTime = match != null ? match.cookingTime : defaultCookingTime;

        cookingProgress = 0f;
        boilOverTimer = 0f; // Reset burn timer too
        if (player != null) cookingPlayer = player;
        stationState = StationState.Cooking;

        if (progressBarContainer != null) progressBarContainer.SetActive(true);
        if (progressBarFill != null) progressBarFill.transform.localScale = new Vector3(0f, 1f, 1f);

        Debug.Log($"[MultiIngredientStation] Cooking restarted with {ingredients.Count} ingredient(s) — {currentCookingTime}s");
    }

    // Kept for HoldToProcess trigger mode
    private void StartCooking(SimplePlayerController player)
    {
        RestartCooking(player);
    }

    private void UpdateCooking()
    {
        // HoldToProcess pauses if StartCookingIfValid wasn't called this/last frame
        // (i.e. player walked away or released the Process button)
        if (triggerMode == TriggerMode.HoldToProcess)
        {
            bool calledRecently = lastProcessedFrame == Time.frameCount || lastProcessedFrame == Time.frameCount - 1;
            if (!calledRecently) return;
        }

        cookingProgress += Time.deltaTime / currentCookingTime;
        if (progressBarFill != null)
            progressBarFill.transform.localScale = new Vector3(Mathf.Clamp01(cookingProgress), 1f, 1f);

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
            // No recipe matched — brew an improvised potion instead of nothing.
            // Still produces SOMETHING the player can hand in (even if it scores 0 or low).
            if (improvisedPotionItem != null)
            {
                Color blended = PotionColourUtility.BlendIngredients(ingredients);
                SpawnResult(improvisedPotionItem, tintImprovisedResult ? (Color?)blended : null);
                Debug.Log($"[MultiIngredientStation] No recipe matched — brewed improvised potion (colour: {blended}).");
            }
            else
            {
                Debug.LogWarning("[MultiIngredientStation] No recipe matched and no improvisedPotionItem assigned — nothing produced.");
            }

            ingredients.Clear();
            UpdateVisuals();
            return;
        }

        SpawnResult(resultData, null);
        ingredients.Clear();
        UpdateVisuals();
        Debug.Log($"[MultiIngredientStation] Done! Result: {resultData.itemName}");
    }

    private void SpawnResult(ItemData data, Color? tintColour)
    {
        Transform anchor = resultSpawnPoint != null ? resultSpawnPoint
                         : counterTopPoint != null ? counterTopPoint
                         : transform;

        WorldItem result = WorldItem.CreateCombined(data, anchor.position + Vector3.up * 0.3f, anchor);
        if (result != null)
        {
            resultItem = result.gameObject;
            if (stationType == StationType.CrystalBall)
            {
                Vector3 localOffset = new Vector3(-2.5f, 1.18f, 2.5f);
                BoxCollider col = GetComponent<BoxCollider>();
                if (col != null) localOffset = col.center;
                Vector3 visualCenter = anchor.position + anchor.rotation * localOffset;
                result.transform.position = visualCenter + new Vector3(0f, 0.3f, 0f);
            }
            else
            {
                result.transform.position = anchor.position + new Vector3(-1.5f, 1.5f, 1.5f);
            }

            Rigidbody rb = result.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in result.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Apply the blended potion colour to the liquid's material, if requested.
            if (tintColour.HasValue)
                ApplyTintToRenderer(result.gameObject, tintColour.Value);

            // For CrystalBall results: apply glowing material using the item's potionColour
            if (stationType == StationType.CrystalBall)
            {
                Color visionColor = (data != null && !data.isColourless)
                    ? data.potionColour
                    : new Color(0.2f, 0.8f, 1f, 1f);

                Renderer visionRenderer = result.GetComponentInChildren<Renderer>();
                if (visionRenderer != null)
                {
                    Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                     ?? Shader.Find("Unlit/Color");
                    Material glowMat = new Material(unlitShader);
                    glowMat.SetColor("_BaseColor", visionColor);
                    visionRenderer.material = glowMat;
                }

                foreach (Light l in result.GetComponentsInChildren<Light>())
                    l.enabled = false;

                GameObject lightGO = new GameObject("VisionPointLight");
                lightGO.transform.SetParent(result.transform, false);
                lightGO.transform.localPosition = Vector3.zero;
                Light point = lightGO.AddComponent<Light>();
                point.type = LightType.Point;
                point.color = visionColor;
                point.intensity = 3f;
                point.range = 4f;
                point.shadows = LightShadows.None;
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnResult] WorldItem.CreateCombined returned null for '{data?.itemName}'. Prefab assigned? {data?.prefab != null}");
        }
    }

    /// <summary>
    /// Tints the FIRST renderer found (including children) using an instanced material
    /// so other instances of the same prefab aren't affected.
    /// </summary>
    private void ApplyTintToRenderer(GameObject target, Color colour)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("[MultiIngredientStation] No Renderer found on improvised potion to tint.");
            return;
        }

        // .material (not sharedMaterial) creates a per-instance copy automatically
        renderer.material.color = colour;
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

    /// <summary>
    /// Removes and returns the finished result GameObject without giving it
    /// to any specific player. Used by CauldronPourTarget so a Bottle can
    /// "pour" the potion out instead of picking the whole liquid item up directly.
    /// Returns null if nothing is brewed.
    /// </summary>
    public GameObject TakeResultItem()
    {
        if (resultItem == null) return null;

        GameObject item = resultItem;
        resultItem = null;
        stationState = StationState.Empty;
        boilOverTimer = 0f;
        UpdateVisuals();
        return item;
    }

    /// <summary>
    /// Swaps the player's empty container for the filled result, based on
    /// what was actually brewed (read from resultItem's own WorldItem).
    /// </summary>
    private void TrySwapContainer(SimplePlayerController player, ItemData heldContainerItem)
    {
        WorldItem resultWI = resultItem.GetComponent<WorldItem>();
        ItemData brewedItem = resultWI != null ? resultWI.itemData : null;

        ItemData filledContainer = GetContainerSwapResult(brewedItem);
        if (filledContainer == null || filledContainer.prefab == null)
        {
            Debug.LogWarning($"[MultiIngredientStation] No container swap mapping found for {(brewedItem != null ? brewedItem.itemName : "null")}.");
            return;
        }

        // Destroy the player's empty container and the raw brewed result
        Destroy(player.heldItem);
        Destroy(resultItem);
        resultItem = null;
        stationState = StationState.Empty;
        boilOverTimer = 0f;
        UpdateVisuals();

        // Spawn the filled container directly into the player's hands
        GameObject spawned = Instantiate(filledContainer.prefab, player.holdPoint.position, Quaternion.identity, player.holdPoint);
        WorldItem newWI = spawned.GetComponent<WorldItem>();
        if (newWI == null) newWI = spawned.AddComponent<WorldItem>();
        newWI.itemData = filledContainer;

        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        Rigidbody rb = spawned.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in spawned.GetComponentsInChildren<Collider>())
            col.enabled = false;

        player.heldItem = spawned;

        Debug.Log($"[MultiIngredientStation] Container swapped -> {filledContainer.itemName}");
    }

    private ItemData GetContainerSwapResult(ItemData brewedItem)
    {
        foreach (var m in containerSwapMappings)
            if (m.brewedResultItem == brewedItem) return m.filledContainerItem;
        return null;
    }

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
    /// <summary>Returns true if the given item is currently present among the ingredients.</summary>
    private bool HasIngredient(ItemData item)
    {
        foreach (var i in ingredients)
            if (i == item) return true;
        return false;
    }

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

    /// <summary>
    // ── Visuals ──────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        if (emptyVisual  != null) emptyVisual.SetActive(stationState == StationState.Empty);
        if (activeVisual != null) activeVisual.SetActive(stationState == StationState.Cooking);
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

    private void OnDrawGizmos()
    {
        if (stationType != StationType.CrystalBall) return;
        if (counterTopPoint == null) return;

        // Magenta sphere = where the Vision result will spawn
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(counterTopPoint.position + new Vector3(-1.5f, 1.5f, 1.5f), 0.15f);

        // Cyan wire cube = where the progress bar canvas will sit
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(counterTopPoint.position + new Vector3(-1.5f, 1.7f, 1.5f), new Vector3(1.2f, 0.18f, 0.01f));
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
/// <summary>
/// Maps a brewed result item to what the player's empty container becomes
/// when swapped at the station. E.g. Tea1 -> TeacupFilled1.
/// </summary>
[System.Serializable]
public class ContainerSwapMapping
{
    [Tooltip("The item this station can brew (e.g. Tea1, Tea2, Tea3, TeaWrong)")]
    public ItemData brewedResultItem;
    [Tooltip("What the empty container becomes when swapped for this result (e.g. TeacupFilled1)")]
    public ItemData filledContainerItem;
}