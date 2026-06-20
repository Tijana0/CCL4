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
    [Header("Progress Bar UI")]
    public Image progressBarFill;
    public GameObject progressBarContainer;

    [Header("Overcook (optional)")]
    [Tooltip("If true, a finished result left unattended too long is destroyed instead of waiting forever. Used by the Brazier to evaporate burnt herbs.")]
    public bool enableOvercook = false;
    [Tooltip("Seconds after completion before the result evaporates/destroys. Only used if Enable Overcook is checked.")]
    public float overcookDelay = 8f;

    private bool isProcessing = false;
    private float processingProgress = 0f;
    private float processingDuration = 2f;
    private SimplePlayerController processingPlayer = null;
    private int lastProcessedFrame = -1;
    private bool resultReady = false;
    private float overcookTimer = 0f;

    private void Awake()
    {
        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void Update()
    {
        // If no one called StartProcessingIfValid this frame (player walked away
        // or released Process), cancel. We detect this by checking if the
        // frame counter wasn't bumped since last Update.
        if (isProcessing && lastProcessedFrame != Time.frameCount - 1 && lastProcessedFrame != Time.frameCount)
        {
            CancelProcessing();
        }

        // Overcook: result finished and sitting unattended too long evaporates entirely
        if (enableOvercook && resultReady && itemOnStation != null)
        {
            overcookTimer += Time.deltaTime;
            if (overcookTimer >= overcookDelay)
            {
                Evaporate();
            }
        }
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
        }
        resultReady = false;
        overcookTimer = 0f;
        Debug.Log($"[ProcessingStation] {stationType} evaporated — left too long.");
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
        ProcessingRule rule = wi != null ? GetRule(wi.itemData) : null;
        if (rule == null) return;

        // Mark this frame as "actively being processed" so Update() doesn't cancel it
        lastProcessedFrame = Time.frameCount;

        if (!isProcessing)
        {
            BeginProcessing(player, rule);
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
            TakeItem(player);
        }
    }

    protected override void TryPlaceItem(SimplePlayerController player)
    {
        WorldItem worldItem = player.heldItem.GetComponent<WorldItem>();
        if (worldItem == null)
        {
            Debug.Log("[ProcessingStation] Item has no WorldItem component.");
            return;
        }

        ProcessingRule rule = GetRule(worldItem.itemData);
        if (rule == null)
        {
            Debug.Log($"[ProcessingStation] {worldItem.itemData.itemName} cannot be processed at {stationType}.");
            return;
        }

        processingDuration = rule.processingTime > 0 ? rule.processingTime : 2f;
        PlaceItemOnStation(player.heldItem, player);
    }

    private void BeginProcessing(SimplePlayerController player, ProcessingRule rule)
    {
        isProcessing = true;
        processingProgress = 0f;
        processingPlayer = player;
        processingDuration = rule.processingTime > 0 ? rule.processingTime : 2f;

        if (progressBarContainer != null) progressBarContainer.SetActive(true);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        Debug.Log($"[ProcessingStation] Hold Process to work... ({processingDuration}s)");
    }

    private void CompleteProcessing()
    {
        isProcessing = false;
        if (progressBarContainer != null) progressBarContainer.SetActive(false);

        WorldItem wi = itemOnStation?.GetComponent<WorldItem>();
        if (wi == null) return;
        ProcessingRule rule = GetRule(wi.itemData);
        if (rule?.outputItem == null) return;

        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        Vector3 spawnPos = anchor.position + Vector3.up * 0.2f;

        Destroy(itemOnStation);
        itemOnStation = null;

        WorldItem result = WorldItem.CreateCombined(rule.outputItem, spawnPos, anchor);
        if (result != null)
        {
            itemOnStation = result.gameObject;
            result.transform.localPosition = new Vector3(0, 0.2f, 0);

            Rigidbody rb = result.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in result.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        processingPlayer = null;
        resultReady = true;
        overcookTimer = 0f;
        Debug.Log($"[ProcessingStation] Done! Result: {rule.outputItem.itemName}");
    }

    private void CancelProcessing()
    {
        isProcessing = false;
        processingProgress = 0f;
        processingPlayer = null;

        if (progressBarContainer != null) progressBarContainer.SetActive(false);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;
    }

    private ProcessingRule GetRule(ItemData data)
    {
        if (data == null) return null;
        foreach (var rule in data.processingRules)
            if (rule.stationType == stationType) return rule;
        return null;
    }
}