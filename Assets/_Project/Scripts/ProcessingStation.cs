using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Station that requires the player to HOLD the PROCESS button to process an item over time.
/// Used for: Mortar & Pestle (grinding), Cutting Board (chopping), Brazier (warming).
///
/// Input split:
///   E (Interact)        — place item on station / pick item up from station
///   Q (Process)          — hold near the station (empty-handed) to start/continue processing
///
/// This separation means E never accidentally starts processing, and Q never
/// accidentally picks up or places items.
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

    [Header("Process Range")]
    [Tooltip("How close a player must be to hold Q and process here")]
    public float processRange = 1.5f;

    private bool isProcessing = false;
    private float processingProgress = 0f;
    private float processingDuration = 2f;
    private SimplePlayerController processingPlayer = null;

    private void Awake()
    {
        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void Update()
    {
        // Only check for processing if there's an item here that has a rule
        if (itemOnStation == null)
        {
            if (isProcessing) CancelProcessing();
            return;
        }

        WorldItem wi = itemOnStation.GetComponent<WorldItem>();
        ProcessingRule rule = wi != null ? GetRule(wi.itemData) : null;
        if (rule == null)
        {
            if (isProcessing) CancelProcessing();
            return;
        }

        // Find a nearby empty-handed player holding Q
        SimplePlayerController holder = FindPlayerHoldingProcess();

        if (holder == null)
        {
            if (isProcessing) CancelProcessing();
            return;
        }

        if (!isProcessing)
            BeginProcessing(holder, rule);

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

        Debug.Log($"[ProcessingStation] Hold Q to process... ({processingDuration}s)");
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

    /// <summary>
    /// Finds the closest empty-handed player within processRange who is holding Q.
    /// </summary>
    private SimplePlayerController FindPlayerHoldingProcess()
    {
        SimplePlayerController[] players = FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.heldItem != null) continue; // must have empty hands to process

            float dist = Vector3.Distance(p.transform.position, transform.position);
            if (dist > processRange) continue;

            if (CheckPlayerHoldingProcessButton(p))
                return p;
        }

        return null;
    }

    /// <summary>
    /// Checks the dedicated PROCESS action button (Q / Numpad0 / Left Trigger).
    /// Kept separate from interact (E) so picking items up never accidentally
    /// triggers processing, and processing never accidentally picks things up.
    /// </summary>
    private bool CheckPlayerHoldingProcessButton(SimplePlayerController player)
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.qKey.isPressed;

        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.numpad0Key.isPressed;

        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.leftTrigger.isPressed;

        return false;
    }

    private ProcessingRule GetRule(ItemData data)
    {
        if (data == null) return null;
        foreach (var rule in data.processingRules)
            if (rule.stationType == stationType) return rule;
        return null;
    }
}