using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Dispenser station — gives items to players.
/// Three extraction modes configurable per station in the Inspector.
///
/// Instant:         Press E → item goes straight to player's hands (uses itemToDispense)
/// HoldToExtract:   Hold E for extractionTime seconds → item given (uses itemToDispense)
/// RequiresItem:    Player must hold one of several possible items —
///                  each maps to its own output. E.g. one Water Source handling
///                  BOTH "Empty Bucket -> Full Bucket" AND "Empty Teapot -> Full Teapot"
///                  on the same object. Uses requiredItemSwaps list instead of the
///                  single itemToDispense/requiredItem pair.
/// </summary>
public class ItemContainerStation : StationBase
{
    public enum ExtractionMode { Instant, HoldToExtract, RequiresItem }

    [System.Serializable]
    public class RequiredItemSwap
    {
        [Tooltip("Item the player must be holding to trigger this swap. E.g. EmptyBucket.")]
        public ItemData requiredItem;
        [Tooltip("What it becomes. E.g. FullBucket.")]
        public ItemData outputItem;
    }

    [Header("Dispenser Settings")]
    [Tooltip("Used by Instant and HoldToExtract modes.")]
    public ItemData itemToDispense;
    public ExtractionMode extractionMode = ExtractionMode.Instant;

    [Header("HoldToExtract Settings")]
    [Tooltip("Seconds to hold interact before item is given. Only used in HoldToExtract mode.")]
    public float extractionTime = 2f;

    [Header("RequiresItem Settings")]
    [Tooltip("List of possible swaps for RequiresItem mode. Add one entry per item this station can fill/transform. E.g. EmptyBucket->FullBucket AND EmptyTeapot->FullTeapot on the same station.")]
    public List<RequiredItemSwap> requiredItemSwaps = new List<RequiredItemSwap>();
    [Tooltip("If true the required item is consumed. If false it stays in player hands alongside the output.")]
    public bool consumeRequiredItem = true;

    [Header("Optional Visuals")]
    public SpriteRenderer itemIconRenderer;

    private bool isExtracting = false;
    private float extractProgress = 0f;
    private SimplePlayerController extractingPlayer = null;

    private void Awake()
    {
        stationType = StationType.ItemContainer;
    }

    private void Start()
    {
        if (itemIconRenderer != null && itemToDispense != null)
            itemIconRenderer.sprite = itemToDispense.icon;
    }

    private void Update()
    {
        if (extractionMode != ExtractionMode.HoldToExtract || !isExtracting) return;

        if (!CheckPlayerHoldingInteract(extractingPlayer))
        {
            CancelExtraction();
            return;
        }

        extractProgress += Time.deltaTime / extractionTime;
        if (extractProgress >= 1f)
            CompleteExtraction();
    }

    public override void Interact(SimplePlayerController player)
    {
        switch (extractionMode)
        {
            case ExtractionMode.Instant:
                HandleInstant(player);
                break;
            case ExtractionMode.HoldToExtract:
                HandleHoldToExtract(player);
                break;
            case ExtractionMode.RequiresItem:
                HandleRequiresItem(player);
                break;
        }
    }

    // ── Instant ──────────────────────────────────────────────────────────────

    private void HandleInstant(SimplePlayerController player)
    {
        if (itemToDispense == null)
        {
            Debug.LogWarning("[ItemContainerStation] No item assigned to dispense.");
            return;
        }
        if (player.heldItem != null)
        {
            Debug.Log("[ItemContainerStation] Player's hands must be empty.");
            return;
        }
        GiveItemToPlayer(player, itemToDispense);
    }

    // ── Hold To Extract ───────────────────────────────────────────────────────

    private void HandleHoldToExtract(SimplePlayerController player)
    {
        if (itemToDispense == null)
        {
            Debug.LogWarning("[ItemContainerStation] No item assigned to dispense.");
            return;
        }
        if (player.heldItem != null)
        {
            Debug.Log("[ItemContainerStation] Player's hands must be empty.");
            return;
        }

        if (!isExtracting)
        {
            isExtracting = true;
            extractProgress = 0f;
            extractingPlayer = player;
            Debug.Log($"[ItemContainerStation] Hold to extract {itemToDispense.itemName}...");
        }
    }

    private void CompleteExtraction()
    {
        isExtracting = false;
        extractProgress = 0f;

        if (extractingPlayer != null && extractingPlayer.heldItem == null)
            GiveItemToPlayer(extractingPlayer, itemToDispense);

        extractingPlayer = null;
    }

    private void CancelExtraction()
    {
        isExtracting = false;
        extractProgress = 0f;
        extractingPlayer = null;
    }

    // ── Requires Item ─────────────────────────────────────────────────────────

    private void HandleRequiresItem(SimplePlayerController player)
    {
        if (player.heldItem == null)
        {
            Debug.Log("[ItemContainerStation] You need to be holding an item to use this station.");
            return;
        }

        WorldItem heldWI = player.heldItem.GetComponent<WorldItem>();
        if (heldWI == null)
        {
            Debug.Log("[ItemContainerStation] Held item has no WorldItem component.");
            return;
        }

        ItemData output = GetSwapOutput(heldWI.itemData);
        if (output == null)
        {
            Debug.Log($"[ItemContainerStation] {heldWI.itemData.itemName} can't be used here.");
            return;
        }

        if (consumeRequiredItem)
        {
            Destroy(player.heldItem);
            player.heldItem = null;
        }

        GiveItemToPlayer(player, output);
    }

    private ItemData GetSwapOutput(ItemData heldItem)
    {
        foreach (var swap in requiredItemSwaps)
            if (swap.requiredItem == heldItem) return swap.outputItem;
        return null;
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private void GiveItemToPlayer(SimplePlayerController player, ItemData dataToGive)
    {
        if (dataToGive == null || dataToGive.prefab == null)
        {
            Debug.LogWarning($"[ItemContainerStation] {(dataToGive != null ? dataToGive.itemName : "item")} has no prefab assigned.");
            return;
        }

        GameObject spawned = Instantiate(dataToGive.prefab, player.holdPoint.position, Quaternion.identity, player.holdPoint);

        WorldItem wi = spawned.GetComponent<WorldItem>();
        if (wi == null) wi = spawned.AddComponent<WorldItem>();
        wi.itemData = dataToGive;

        player.heldItem = spawned;
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        Rigidbody rb = spawned.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in spawned.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Dispensing bypasses PickupObject.Interact, so play the pickup sound here too.
        if (AudioManager.Instance != null)
        {
            bool isHerb = dataToGive.itemName != null && dataToGive.itemName.ToLower().Contains("herb");
            AudioManager.Instance.PlayPickup(spawned, isHerb);
        }

        Debug.Log($"[ItemContainerStation] Dispensed: {dataToGive.itemName}");
    }

    private bool CheckPlayerHoldingInteract(SimplePlayerController player)
    {
        if (player == null) return false;
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.eKey.isPressed;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.rightShiftKey.isPressed;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.buttonSouth.isPressed;
        return false;
    }
}