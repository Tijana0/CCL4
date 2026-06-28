using UnityEngine;
using UnityEngine.UI;
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

    [Header("Home Dock (snaps back to its stand when set down nearby)")]
    [Tooltip("The transform this pot rests on (e.g. the stove's stand point). When set down within range, it snaps back here instead of being left as a loose physics object.")]
    public Transform homeAnchor;
    [Tooltip("Resting local position relative to homeAnchor.")]
    public Vector3 homeLocalPosition;
    [Tooltip("Resting local rotation (euler) relative to homeAnchor.")]
    public Vector3 homeLocalEuler;
    [Tooltip("Resting local scale relative to homeAnchor. Automatically captured at startup if not customized.")]
    public Vector3 homeLocalScale = Vector3.one;
    [Tooltip("How close (when not held) the pot must be to its home before it snaps back onto the stand.")]
    public float homeSnapRange = 1.0f;

    [Header("Recipes")]
    public List<MultiIngredientRecipe> recipes = new List<MultiIngredientRecipe>();
    [Tooltip("Fallback output if ingredients don't match any recipe when cooking completes.")]
    public ItemData improvisedResultItem;
    [Tooltip("Default cooking time if ingredients are present but match no specific recipe yet.")]
    public float defaultCookingTime = 6f;

    [Header("Overcook (left on the fire too long)")]
    [Tooltip("If true, a finished brew left on the fire too long evaporates — the pot empties back to a plain (empty) teapot.")]
    public bool enableOvercook = true;
    [Tooltip("Seconds after the brew finishes (the flashing-red warning window) before it evaporates. Only counts while still on the fire.")]
    public float overcookDelay = 5f;

    [Header("Progress Bar (auto-built world-space bar, like the brazier)")]
    public Image progressBarFill;
    public GameObject progressBarContainer;

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
    [HideInInspector] public List<ItemData> ingredients = new List<ItemData>();
    private bool isCooking = false;
    private float cookProgress = 0f;
    private float currentCookTime = 6f;
    private bool isDone = false;
    private ItemData lastBrewedResult = null;
    private float overcookTimer = 0f;

    private HeatSource currentHeatSource = null;

    private void Awake()
    {
        if (progressBarContainer == null)
            BuildProgressBar();
        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void Start()
    {
        // Capture the authored local scale if we start parented to our home anchor
        if (homeAnchor != null && transform.parent == homeAnchor)
        {
            homeLocalScale = transform.localScale;
        }
    }

    public bool IsEmpty => ingredients.Count == 0 && !isDone;
    public bool IsDone => isDone;
    public ItemData LastBrewedResult => lastBrewedResult;

    private void Update()
    {
        // When set down near its stand (not carried), snap it back onto the stand so it
        // returns exactly where it started instead of being left as a loose physics object.
        if (homeAnchor != null && !IsHeld() && transform.parent != homeAnchor)
        {
            Vector3 homeWorld = homeAnchor.TransformPoint(homeLocalPosition);
            if (Vector3.Distance(transform.position, homeWorld) <= homeSnapRange)
                DockAtHome();
        }

        currentHeatSource = FindNearbyHeatSource();
        // Only cook when actually SET DOWN on the fire — not while a player carries it
        // near the stove. Held items are parented under the player.
        bool onFire = currentHeatSource != null && !IsHeld();
        bool hasValidRecipe = FindMatchingRecipe() != null;

        if (onFire && !isDone && hasValidRecipe)
        {
            if (!isCooking)
                StartCooking();

            cookProgress += Time.deltaTime / currentCookTime;
            if (cookProgress >= 1f)
                CompleteCooking();
        }
        else if (isCooking && (!onFire || !hasValidRecipe))
        {
            CancelCooking();
        }

        // Overcook: a finished brew left on the fire too long evaporates back to empty.
        if (enableOvercook && isDone && onFire)
        {
            overcookTimer += Time.deltaTime;
            if (overcookTimer >= overcookDelay)
                Evaporate();
        }
        else if (!isDone)
        {
            overcookTimer = 0f;
        }

        UpdateProgressBar(onFire);
        UpdateVisuals();
    }

    private void UpdateProgressBar(bool onFire)
    {
        if (progressBarContainer == null) return;

        bool cooking     = isCooking && !isDone;
        bool overcooking = enableOvercook && isDone && onFire;
        bool show        = cooking || overcooking;

        if (progressBarContainer.activeSelf != show) progressBarContainer.SetActive(show);
        if (!show) return;

        // Float the bar above the (movable) pot and face the camera.
        progressBarContainer.transform.position = transform.position + Vector3.up * 0.9f;
        if (Camera.main != null)
            progressBarContainer.transform.rotation = Camera.main.transform.rotation;

        if (progressBarFill == null) return;
        if (overcooking)
        {
            progressBarFill.fillAmount = 1f;
            float a = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(Time.time * 6f)); // flash red
            progressBarFill.color = new Color(0.9f, 0.1f, 0.1f, a);
        }
        else
        {
            progressBarFill.fillAmount = Mathf.Clamp01(cookProgress);
            progressBarFill.color = new Color(1f, 0.5f, 0.1f, 1f); // fiery orange
        }
    }

    /// <summary>
    /// Finished brew was left on the fire past the warning window — it evaporates:
    /// the pot empties back to a plain (empty) teapot, contents lost.
    /// </summary>
    private void Evaporate()
    {
        ClearContents();
        overcookTimer = 0f;
        if (progressBarContainer != null) progressBarContainer.SetActive(false);
        Debug.Log("[PortableCooker] Left on the fire too long — brew evaporated, teapot is empty again.");
    }

    private void BuildProgressBar()
    {
        Sprite white = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f));

        GameObject canvasGO = new GameObject(name + "_CookProgress");
        canvasGO.transform.localScale = Vector3.one * 0.01f;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasGO.transform).sizeDelta = new Vector2(120f, 18f);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = white;
        bgImg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(canvasGO.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = white;
        fillImg.color = new Color(1f, 0.5f, 0.1f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f); fillRect.offsetMax = new Vector2(-2f, -2f);

        progressBarContainer = canvasGO;
        progressBarFill = fillImg;
        canvasGO.SetActive(false);
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
        overcookTimer = 0f;

        MultiIngredientRecipe match = FindMatchingRecipe();
        lastBrewedResult = match != null ? match.outputItem : improvisedResultItem;

        ingredients.Clear();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayTeapotReady(gameObject);

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

    /// <summary>True while a player is carrying this pot (it's parented under the player).</summary>
    private bool IsHeld()
    {
        return GetComponentInParent<SimplePlayerController>() != null;
    }

    /// <summary>Parks the pot back on its stand at the original resting pose (kinematic,
    /// colliders left enabled so it can still be picked up / have ingredients added).</summary>
    private void DockAtHome()
    {
        transform.SetParent(homeAnchor);
        transform.localPosition = homeLocalPosition;
        // Keep the rotation as is when putting it down instead of snapping to homeLocalEuler
        transform.localScale = homeLocalScale;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        // A docked pot must stay interactable (pick up / add ingredients), so make sure
        // its colliders are on even if it was last set down straight from being carried.
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = true;
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