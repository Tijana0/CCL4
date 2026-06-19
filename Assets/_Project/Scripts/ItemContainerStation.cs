using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dispenser station — gives items to players.
/// Three extraction modes configurable per station in the Inspector.
///
/// Instant:         Press E → item goes straight to player's hands
/// HoldToExtract:   Hold E for extractionTime seconds → item given
/// RequiresItem:    Player must hold a specific item (e.g. empty bucket)
///                  and interact — that item is transformed into the output
///                  item in their hands (e.g. full bucket)
/// </summary>
public class ItemContainerStation : StationBase
{
    public enum ExtractionMode { Instant, HoldToExtract, RequiresItem }

    [Header("Dispenser Settings")]
    public ItemData itemToDispense;
    public ExtractionMode extractionMode = ExtractionMode.Instant;

    [Header("HoldToExtract Settings")]
    [Tooltip("Seconds to hold interact before item is given. Only used in HoldToExtract mode.")]
    public float extractionTime = 2f;

    [Header("RequiresItem Settings")]
    [Tooltip("Item the player must be holding to extract. E.g. EmptyBucket to get FullBucket.")]
    public ItemData requiredItem;
    [Tooltip("If true the required item is consumed. If false it stays in player hands alongside the output.")]
    public bool consumeRequiredItem = true;

    [Header("Optional Visuals")]
    public SpriteRenderer itemIconRenderer;

    // Hold-to-extract runtime state
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
        if (itemToDispense == null)
        {
            Debug.LogWarning("[ItemContainerStation] No item assigned to dispense.");
            return;
        }

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
        if (player.heldItem != null)
        {
            Debug.Log("[ItemContainerStation] Player's hands must be empty.");
            return;
        }
        GiveItemToPlayer(player);
    }

    // ── Hold To Extract ───────────────────────────────────────────────────────

    private void HandleHoldToExtract(SimplePlayerController player)
    {
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
            GiveItemToPlayer(extractingPlayer);

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
            Debug.Log($"[ItemContainerStation] You need to be holding {(requiredItem != null ? requiredItem.itemName : "a specific item")}.");
            return;
        }

        WorldItem heldWI = player.heldItem.GetComponent<WorldItem>();
        if (heldWI == null || heldWI.itemData != requiredItem)
        {
            Debug.Log($"[ItemContainerStation] Wrong item. Need: {(requiredItem != null ? requiredItem.itemName : "?")}");
            return;
        }

        // Consume the required item if set
        if (consumeRequiredItem)
        {
            Destroy(player.heldItem);
            player.heldItem = null;
        }

        GiveItemToPlayer(player);
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private void GiveItemToPlayer(SimplePlayerController player)
    {
        if (itemToDispense.prefab == null)
        {
            Debug.LogWarning($"[ItemContainerStation] {itemToDispense.itemName} has no prefab assigned.");
            return;
        }

        // Capture the prefab's own intended scale before parenting under holdPoint
        Vector3 prefabScale = itemToDispense.prefab.transform.localScale;

        GameObject spawned = Instantiate(itemToDispense.prefab, player.holdPoint.position, Quaternion.identity, player.holdPoint);

        WorldItem wi = spawned.GetComponent<WorldItem>();
        if (wi == null) wi = spawned.AddComponent<WorldItem>();
        wi.itemData = itemToDispense;

        player.heldItem = spawned;
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        // Counteract holdPoint's own scale so the item's world size matches the prefab
        Vector3 holdScale = player.holdPoint.lossyScale;
        spawned.transform.localScale = new Vector3(
            holdScale.x != 0 ? prefabScale.x / holdScale.x : prefabScale.x,
            holdScale.y != 0 ? prefabScale.y / holdScale.y : prefabScale.y,
            holdScale.z != 0 ? prefabScale.z / holdScale.z : prefabScale.z
        );

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = spawned.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log($"[ItemContainerStation] Dispensed: {itemToDispense.itemName}");
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