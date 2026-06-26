using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Station that requires the player to HOLD the dedicated PROCESS button
/// (R / RightCtrl / Gamepad West) to process an item over time.
/// Used for: Mortar & Pestle (grinding), Cutting Board (chopping), Brazier (warming).
///
/// Driven by SimplePlayerController.TryProcess(), which calls
/// StartProcessingIfValid(player) every frame the Process button is held
/// while the player is near this station.
///
/// Input split:
///   E (Interact) — place item on station / pick item up from station
///   R (Process)  — hold near the station to start/continue processing
///
/// Setup:
/// - Set stationType (MortarAndPestle, CuttingBoard, etc.)
/// - Assign progressBarFill (Image, Fill type Horizontal)
/// - Assign progressBarContainer (parent GameObject to show/hide)
/// - Item's ItemData must have a ProcessingRule for this stationType
/// </summary>
public class ProcessingStation : StationBase
{
    [Header("Transform Rules (defined on this station)")]
    [Tooltip("Define what each item becomes when processed here, e.g. Herb1 -> BurntHerb1. Checked BEFORE the item's own ItemData processing rules.")]
    public System.Collections.Generic.List<StationTransformRule> transformRules = new System.Collections.Generic.List<StationTransformRule>();

    [Header("Progress Bar UI")]
    public Image progressBarFill;
    public GameObject progressBarContainer;

    [Header("Overcook (optional)")]
    [Tooltip("If true, a finished result left unattended too long is overcooked. During the grace period the progress bar turns red and flashes as a warning.")]
    public bool enableOvercook = false;
    [Tooltip("Seconds after completion (the flashing-red warning window) before the result overcooks. Only used if Enable Overcook is checked.")]
    public float overcookDelay = 8f;
    [Tooltip("If set, an overcooked result turns into THIS item (e.g. a charred black herb) instead of being destroyed. Leave null to evaporate it.")]
    public ItemData overcookResultItem;

    [Header("Auto / Rejection behaviour")]
    [Tooltip("If true, processing starts automatically the moment a valid item is placed — the player does NOT need to hold the Process button. Used by the Brazier.")]
    public bool autoProcess = false;
    [Tooltip("If true, an item with no transform rule for this station is dropped to the floor instead of staying in the player's hands. Used by the Brazier.")]
    public bool dropRejectedItems = false;
    [Tooltip("If true, an item sitting on this station is hidden (renderers off) so it looks like it's INSIDE the station — e.g. herbs burning inside the brazier. Only the progress bar indicates status. Shown again when picked up.")]
    public bool hideItemWhileOnStation = false;

    private bool isProcessing = false;
    private float processingProgress = 0f;
    private float processingDuration = 2f;
    private SimplePlayerController processingPlayer = null;
    private int lastProcessedFrame = -1;
    private bool resultReady = false;
    private float overcookTimer = 0f;
    private bool overcooked = false;

    private void Awake()
    {
        // Auto-build a world-space progress bar above the station if none was assigned.
        if (progressBarContainer == null && counterTopPoint != null)
            BuildProgressBar();
        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void Update()
    {
        // Auto mode: drive processing on our own, without the player holding Process.
        if (autoProcess && itemOnStation != null && !resultReady)
        {
            WorldItem awi = itemOnStation.GetComponent<WorldItem>();
            var (autoOut, autoTime) = awi != null ? GetTransform(awi.itemData) : (null, 2f);
            if (autoOut != null)
            {
                lastProcessedFrame = Time.frameCount; // keep the hold-mode cancel guard satisfied
                if (!isProcessing) BeginProcessing(null, autoTime);
                processingProgress += Time.deltaTime / processingDuration;
                if (progressBarFill != null) progressBarFill.fillAmount = processingProgress;
                if (processingProgress >= 1f) CompleteProcessing();
            }
        }

        // Hold mode: if no one called StartProcessingIfValid this frame (player walked
        // away or released Process), cancel.
        if (!autoProcess && isProcessing && lastProcessedFrame != Time.frameCount - 1 && lastProcessedFrame != Time.frameCount)
        {
            CancelProcessing();
        }

        // The result was picked up — clear any leftover state and hide the bar.
        if (resultReady && itemOnStation == null)
        {
            resultReady = false;
            overcooked = false;
            overcookTimer = 0f;
            if (progressBarContainer != null) progressBarContainer.SetActive(false);
        }

        // Overcook: a finished result left unattended too long chars/evaporates.
        // During the grace window the bar shows full and flashes red as a warning.
        if (enableOvercook && resultReady && !overcooked && itemOnStation != null)
        {
            overcookTimer += Time.deltaTime;

            if (progressBarContainer != null)
            {
                if (!progressBarContainer.activeSelf) progressBarContainer.SetActive(true);
                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = 1f;
                    float a = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(Time.time * 6f)); // flash
                    progressBarFill.color = new Color(0.9f, 0.1f, 0.1f, a);
                    AudioManager.Instance.PlayBeeping(this.gameObject);
                }
            }

            if (overcookTimer >= overcookDelay)
            {
                if (overcookResultItem != null) Overcook();
                else Evaporate();
            }
        }

        // Billboard the progress bar toward the camera
        if (progressBarContainer != null && progressBarContainer.activeSelf && Camera.main != null)
            progressBarContainer.transform.rotation = Camera.main.transform.rotation;
    }

    /// <summary>
    /// Builds a small world-space progress bar above the station's counter point.
    /// Uses a runtime white sprite (no asset dependency) so the Image's fillAmount
    /// renders correctly.
    /// </summary>
    private void BuildProgressBar()
    {
        Sprite white = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f));

        GameObject canvasGO = new GameObject(name + "_ProgressCanvas");
        canvasGO.transform.position = counterTopPoint.position + new Vector3(0f, 1.2f, 0f);
        canvasGO.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120f, 18f);

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
        fillImg.color = new Color(1f, 0.5f, 0.1f, 1f); // fiery orange
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

    /// <summary>
    /// Destroys the finished result with NO output and clears the station.
    /// Called when a result is left too long with Overcook enabled.
    /// </summary>
    private void Evaporate()
    {
        if (itemOnStation != null)
        {
            Destroy(itemOnStation);
            itemOnStation = null;
            if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayBurnedItem(this.gameObject);

        }
        resultReady = false;
        overcookTimer = 0f;
        Debug.Log($"[ProcessingStation] {stationType} evaporated — left too long.");
    }

    /// <summary>
    /// Replaces the finished result with the charred overcookResultItem (e.g. a black
    /// herb) when it's been left on the station past the grace period. The charred
    /// result stays put and can still be picked up.
    /// </summary>
    private void Overcook()
    {
        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        Vector3 spawnPos = anchor.position + Vector3.up * 0.2f;

        if (itemOnStation != null) { Destroy(itemOnStation); itemOnStation = null; }

        WorldItem charred = WorldItem.CreateCombined(overcookResultItem, spawnPos, anchor);
        if (charred != null)
        {
            itemOnStation = charred.gameObject;
            charred.transform.localPosition = new Vector3(0, 0.2f, 0);
            Rigidbody rb = charred.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in charred.GetComponentsInChildren<Collider>())
                col.enabled = false;
            if (hideItemWhileOnStation) SetItemRenderers(itemOnStation, false);
        }

        overcooked = true;      // don't overcook again
        overcookTimer = 0f;
        if (progressBarContainer != null) progressBarContainer.SetActive(false);
        Debug.Log($"[ProcessingStation] Overcooked into {overcookResultItem.itemName} — left too long.");
    }

    /// <summary>
    /// Called every frame by SimplePlayerController.TryProcess() while the
    /// Process button is held and this station is in range.
    /// Starts processing if valid, advances progress if already processing.
    /// </summary>
    public void StartProcessingIfValid(SimplePlayerController player)
    {
        if (itemOnStation == null) return;

        WorldItem wi = itemOnStation.GetComponent<WorldItem>();
        var (output, time) = wi != null ? GetTransform(wi.itemData) : (null, 2f);
        if (output == null) return;

        // Mark this frame as "actively being processed" so Update() doesn't cancel it
        lastProcessedFrame = Time.frameCount;

        if (!isProcessing)
        {
            BeginProcessing(player, time);
        }

        processingProgress += Time.deltaTime / processingDuration;
        if (progressBarFill != null)
            progressBarFill.fillAmount = processingProgress;

        if (processingProgress >= 1f)
            CompleteProcessing();
    }

    public override void Interact(SimplePlayerController player)
    {
        // E only places or picks up — never starts processing
        bool playerHasItem = player.heldItem != null;
        bool stationHasItem = itemOnStation != null;

        if (playerHasItem && !stationHasItem)
        {
            TryPlaceItem(player);
        }
        else if (!playerHasItem && stationHasItem && !isProcessing)
        {
            if (hideItemWhileOnStation) SetItemRenderers(itemOnStation, true); // reveal it as it goes into the hand
            TakeItem(player);
        }
    }

    /// <summary>Shows/hides an item's renderers (used to keep items hidden "inside" the brazier).</summary>
    private void SetItemRenderers(GameObject item, bool visible)
    {
        if (item == null) return;
        foreach (var r in item.GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }

    protected override void TryPlaceItem(SimplePlayerController player)
    {
        WorldItem worldItem = player.heldItem.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            Debug.Log("[ProcessingStation] Item has no WorldItem component.");
            return;
        }

        var (output, time) = GetTransform(worldItem.itemData);
        if (output == null)
        {
            Debug.Log($"[ProcessingStation] {worldItem.itemData.itemName} has no transform rule for {stationType}.");
            // Wrong item — bounce it off and drop to the floor instead of keeping it in hand.
            if (dropRejectedItems) player.ForceDropHeldItem();
            return;
        }

        processingDuration = time;
        PlaceItemOnStation(player.heldItem, player);
        if (hideItemWhileOnStation) SetItemRenderers(itemOnStation, false); // tuck it inside the brazier
    }

    private void BeginProcessing(SimplePlayerController player, float duration)
    {
        isProcessing = true;
        processingProgress = 0f;
        processingPlayer = player;
        processingDuration = duration > 0 ? duration : 2f;

        if (AudioManager.Instance != null)
        AudioManager.Instance.PlayBurnedItem(gameObject);

        if (progressBarContainer != null) progressBarContainer.SetActive(true);
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
            progressBarFill.color = new Color(1f, 0.5f, 0.1f, 1f); // reset to fiery orange (overcook turns it red)
        }

        Debug.Log($"[ProcessingStation] Hold Process to work... ({processingDuration}s)");
    }

    private void CompleteProcessing()
    {
        isProcessing = false;
        if (progressBarContainer != null) progressBarContainer.SetActive(false);

        WorldItem wi = itemOnStation?.GetComponent<WorldItem>();
        if (wi == null) return;
        var (outputItem, _) = GetTransform(wi.itemData);
        if (outputItem == null) return;

        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        Vector3 spawnPos = anchor.position + Vector3.up * 0.2f;

        Destroy(itemOnStation);
        itemOnStation = null;

        WorldItem result = WorldItem.CreateCombined(outputItem, spawnPos, anchor);
        if (result != null)
        {
            itemOnStation = result.gameObject;
            result.transform.localPosition = new Vector3(0, 0.2f, 0);

            Rigidbody rb = result.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in result.GetComponentsInChildren<Collider>())
                col.enabled = false;
            if (hideItemWhileOnStation) SetItemRenderers(itemOnStation, false); // keep the result hidden inside until picked up
        }

        processingPlayer = null;
        resultReady = true;
        overcooked = false;
        overcookTimer = 0f;
        Debug.Log($"[ProcessingStation] Done! Result: {outputItem.itemName}");
    }

    private void CancelProcessing()
    {
        isProcessing = false;
        processingProgress = 0f;
        processingPlayer = null;

        if (progressBarContainer != null) progressBarContainer.SetActive(false);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;
    }

    /// <summary>
    /// Finds the output + duration for a given input item.
    /// Checks this station's own Transform Rules first, then falls back
    /// to the item's own ItemData.processingRules.
    /// </summary>
    private (ItemData output, float time) GetTransform(ItemData data)
    {
        if (data == null) return (null, 2f);

        foreach (var rule in transformRules)
            if (rule.inputItem == data && rule.outputItem != null)
                return (rule.outputItem, rule.processingTime > 0 ? rule.processingTime : 2f);

        foreach (var rule in data.processingRules)
            if (rule.stationType == stationType && rule.outputItem != null)
                return (rule.outputItem, rule.processingTime > 0 ? rule.processingTime : 2f);

        return (null, 2f);
    }
}