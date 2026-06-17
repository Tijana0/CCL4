using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Station that requires the player to HOLD INTERACT to process an item over time.
/// Used for: Mortar & Pestle (grinding), Cutting Board (chopping).
///
/// Setup:
/// - Set stationType (MortarAndPestle or CuttingBoard)
/// - Assign progressBarFill (Image, Fill type Horizontal)
/// - Assign progressBarContainer (parent GameObject to show/hide)
/// - Item's ItemData must have a ProcessingRule for this stationType
///
/// Usage:
/// - Place item → validated against processing rules
/// - Hold Interact (E / RightShift / Gamepad A) → progress fills → item transforms
/// - Release early → progress resets
/// </summary>
public class ProcessingStation : StationBase
{
    [Header("Progress Bar UI")]
    public Image progressBarFill;
    public GameObject progressBarContainer;

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
        if (!isProcessing) return;

        bool holdingInteract = CheckPlayerHoldingInteract();
        if (!holdingInteract)
        {
            CancelProcessing();
            return;
        }

        processingProgress += Time.deltaTime / processingDuration;
        if (progressBarFill != null)
            progressBarFill.fillAmount = processingProgress;

        if (processingProgress >= 1f)
            CompleteProcessing();
    }

    public override void Interact(SimplePlayerController player)
    {
        if (isProcessing) return;

        bool playerHasItem = player.heldItem != null;
        bool stationHasItem = itemOnStation != null;

        if (playerHasItem && !stationHasItem)
        {
            TryPlaceItem(player);
        }
        else if (!playerHasItem && stationHasItem)
        {
            WorldItem wi = itemOnStation.GetComponent<WorldItem>();
            ProcessingRule rule = wi != null ? GetRule(wi.itemData) : null;

            if (rule != null)
                StartProcessing(player);
            else
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

    private void StartProcessing(SimplePlayerController player)
    {
        isProcessing = true;
        processingProgress = 0f;
        processingPlayer = player;

        if (progressBarContainer != null) progressBarContainer.SetActive(true);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        // Get duration from the item's rule
        WorldItem wi = itemOnStation?.GetComponent<WorldItem>();
        ProcessingRule rule = wi != null ? GetRule(wi.itemData) : null;
        processingDuration = (rule != null && rule.processingTime > 0) ? rule.processingTime : 2f;

        Debug.Log($"[ProcessingStation] Hold to process... ({processingDuration}s)");
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

            Rigidbody rb = result.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Collider col = result.GetComponent<Collider>();
            if (col != null) col.enabled = false;
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

    private bool CheckPlayerHoldingInteract()
    {
        if (processingPlayer == null) return false;

        if (processingPlayer.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.eKey.isPressed;

        if (processingPlayer.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.rightShiftKey.isPressed;

        if (processingPlayer.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.buttonSouth.isPressed;

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