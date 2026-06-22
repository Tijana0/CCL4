using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Add this to a portable container item (e.g. the Teapot) that can hold
/// ingredients, be carried around freely, and only cooks while resting
/// on a HeatSource (the fire stand) — like a pot in Overcooked.
///
/// Behaviour:
///   - Player can place ingredients into it (Water, Herb) at ANY time,
///     whether held, dropped, or sitting on a HeatSource.
///   - Cooking progresses AUTOMATICALLY, with no Process button, but
///     ONLY while this object is resting on (within range of) a HeatSource.
///   - Picking it up mid-cook while on the fire halts progress (resets to 0)
///     but keeps all current ingredients inside — nothing is lost.
///   - At the Bin: contents are cleared, but the Teapot itself survives —
///     it becomes empty and reusable rather than being destroyed.
///   - The single GameObject's visual appearance can change as it fills/cooks
///     by toggling child renderers (see OnIngredientsChanged / OnCookStateChanged).
///
/// Setup:
///   - Add to the Teapot prefab (alongside WorldItem, PickupObject, Rigidbody, Collider).
///   - Configure ingredientSlotItems if you want a strict whitelist of what
///     can go in (e.g. only Water and Herb1/2/3) — leave empty to accept anything.
///   - Add Recipes exactly like MultiIngredientStation.
/// </summary>
public class PortableCooker : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("What this item becomes if completely emptied (e.g. EmptyTeapot). Used after Bin clears contents, or if you want a visual reset.")]
    public ItemData emptyStateItem;

    [Header("Ingredients")]
    [Tooltip("Maximum ingredients this cooker accepts. 0 = unlimited.")]
    public int maxIngredients = 2;
    [Tooltip("Leave empty to accept any item. Otherwise only items in this list can be added.")]
    public List<ItemData> acceptedIngredients = new List<ItemData>();

    [Header("Heat Requirement")]
    [Tooltip("How close a HeatSource must be to count as 'on the fire'. Checked every frame.")]
    public float heatDetectionRange = 0.6f;

    [Header("Recipes")]
    public List<MultiIngredientRecipe> recipes = new List<MultiIngredientRecipe>();
    [Tooltip("Fallback output if ingredients don't match any recipe when cooking completes.")]
    public ItemData improvisedResultItem;
    [Tooltip("Default cooking time if ingredients are present but match no specific recipe yet.")]
    public float defaultCookingTime = 6f;

    [Header("Visuals (optional)")]
    [Tooltip("Shown when empty.")]
    public GameObject emptyVisual;
    [Tooltip("Shown when holding ingredients but not cooking.")]
    public GameObject filledVisual;
    [Tooltip("Shown while actively cooking on the fire.")]
    public GameObject cookingVisual;
    [Tooltip("Shown once cooking is complete (contents are now 'Tea1' etc conceptually, though the GameObject itself doesn't change).")]
    public GameObject doneVisual;

    // ── Runtime state ────────────────────────────────────────────────────────
    private List<ItemData> ingredients = new List<ItemData>();
    private bool isCooking = false;
    private float cookProgress = 0f;
    private float currentCookTime = 6f;
    private bool isDone = false;
    private ItemData lastBrewedResult = null;

    private HeatSource currentHeatSource = null;

    public bool IsEmpty => ingredients.Count == 0 && !isDone;
    public bool IsDone => isDone;
    public ItemData LastBrewedResult => lastBrewedResult;

    private void Update()
    {
        currentHeatSource = FindNearbyHeatSource();
        bool onFire = currentHeatSource != null;

        if (onFire && !isDone && ingredients.Count > 0)
        {
            if (!isCooking)
                StartCooking();

            cookProgress += Time.deltaTime / currentCookTime;
            if (cookProgress >= 1f)
                CompleteCooking();
        }
        else if (isCooking && !onFire)
        {
            // Taken off the fire mid-cook — halt progress, keep ingredients
            CancelCooking();
        }

        UpdateVisuals();
    }

    // ── Public API — called by interaction scripts ──────────────────────────

    /// <summary>Attempts to add an ingredient. Returns true if accepted.</summary>
    public bool TryAddIngredient(ItemData item)
    {
        if (isDone)
        {
            Debug.Log("[PortableCooker] Already finished cooking — empty it first (Bin) before adding more.");
            return false;
        }

        if (maxIngredients > 0 && ingredients.Count >= maxIngredients)
        {
            Debug.Log("[PortableCooker] Full — cannot add more ingredients.");
            return false;
        }

        if (acceptedIngredients.Count > 0 && !acceptedIngredients.Contains(item))
        {
            Debug.Log($"[PortableCooker] {item.itemName} cannot be added here.");
            return false;
        }

        ingredients.Add(item);
        Debug.Log($"[PortableCooker] Added {item.itemName}. Total: {ingredients.Count}");

        // Adding a new ingredient while cooking restarts progress
        if (isCooking)
        {
            cookProgress = 0f;
        }

        return true;
    }

    /// <summary>
    /// Clears all contents and finished state. Called by the Bin.
    /// The GameObject itself survives — it becomes empty and reusable.
    /// </summary>
    public void ClearContents()
    {
        ingredients.Clear();
        isCooking = false;
        cookProgress = 0f;
        isDone = false;
        lastBrewedResult = null;
        Debug.Log("[PortableCooker] Contents cleared — empty and reusable.");
    }

    /// <summary>
    /// Called when this cooker pours into a target (e.g. a Teacup) or is
    /// otherwise emptied of its finished result. Clears state but keeps
    /// the cooker itself, same as ClearContents.
    /// </summary>
    public ItemData ConsumeResult()
    {
        if (!isDone) return null;
        ItemData result = lastBrewedResult;
        ClearContents();
        return result;
    }

    // ── Cooking internals ────────────────────────────────────────────────────

    private void StartCooking()
    {
        isCooking = true;
        cookProgress = 0f;

        MultiIngredientRecipe match = FindMatchingRecipe();
        currentCookTime = match != null ? match.cookingTime : defaultCookingTime;

        Debug.Log($"[PortableCooker] Cooking started on the fire — {currentCookTime}s");
    }

    private void CancelCooking()
    {
        isCooking = false;
        cookProgress = 0f;
        Debug.Log("[PortableCooker] Taken off the fire — cooking halted, ingredients kept.");
    }

    private void CompleteCooking()
    {
        isCooking = false;
        isDone = true;
        cookProgress = 1f;

        MultiIngredientRecipe match = FindMatchingRecipe();
        lastBrewedResult = match != null ? match.outputItem : improvisedResultItem;

        ingredients.Clear();

        Debug.Log($"[PortableCooker] Done! Result: {(lastBrewedResult != null ? lastBrewedResult.itemName : "none")}");
    }

    private MultiIngredientRecipe FindMatchingRecipe()
    {
        foreach (var recipe in recipes)
        {
            if (recipe.requiredIngredients.Count != ingredients.Count) continue;

            List<ItemData> remaining = new List<ItemData>(recipe.requiredIngredients);
            bool allMatch = true;
            foreach (var item in ingredients)
            {
                if (!remaining.Remove(item)) { allMatch = false; break; }
            }
            if (allMatch && remaining.Count == 0) return recipe;
        }
        return null;
    }

    private HeatSource FindNearbyHeatSource()
    {
        HeatSource[] sources = FindObjectsByType<HeatSource>(FindObjectsSortMode.None);
        foreach (var hs in sources)
        {
            float dist = Vector3.Distance(hs.GetHeatPoint().position, transform.position);
            if (dist <= heatDetectionRange) return hs;
        }
        return null;
    }

    private void UpdateVisuals()
    {
        bool empty = IsEmpty;
        bool filled = !empty && !isCooking && !isDone;

        if (emptyVisual != null) emptyVisual.SetActive(empty);
        if (filledVisual != null) filledVisual.SetActive(filled);
        if (cookingVisual != null) cookingVisual.SetActive(isCooking);
        if (doneVisual != null) doneVisual.SetActive(isDone);
    }
}