using UnityEngine;

/// <summary>
/// Spawns a fresh copy of its assigned item when a player with empty hands interacts.
/// Used as ingredient dispensers — e.g. a shelf of Dragon Scales, a water barrel, etc.
///
/// Setup:
/// - Set stationType = ItemContainer
/// - Assign itemToDispense (an ItemData ScriptableObject)
/// - The prefab on the ItemData will be instantiated and given to the player
///
/// Note: Does NOT need a counterTopPoint — items go straight to the player's holdPoint.
/// </summary>
public class ItemContainerStation : StationBase
{
    [Header("Container Settings")]
    [Tooltip("The ItemData this container will dispense")]
    public ItemData itemToDispense;

    [Tooltip("Optional: visual indicator of what's inside (e.g. ingredient label)")]
    public SpriteRenderer itemIconRenderer;

    private void Awake()
    {
        stationType = StationType.ItemContainer;
    }

    private void Start()
    {
        // Show the item icon on the container if assigned
        if (itemIconRenderer != null && itemToDispense != null)
            itemIconRenderer.sprite = itemToDispense.icon;
    }

    public override void Interact(SimplePlayerController player)
    {
        if (player.heldItem != null)
        {
            Debug.Log("[ItemContainer] Player's hands must be empty to take an item.");
            return;
        }

        if (itemToDispense == null || itemToDispense.prefab == null)
        {
            Debug.LogWarning("[ItemContainer] No item or prefab assigned to dispense.");
            return;
        }

        // Spawn a fresh copy
        GameObject spawned = Instantiate(itemToDispense.prefab, player.holdPoint.position, Quaternion.identity, player.holdPoint);
        
        // Ensure WorldItem is set up
        WorldItem wi = spawned.GetComponent<WorldItem>();
        if (wi == null) wi = spawned.AddComponent<WorldItem>();
        wi.itemData = itemToDispense;

        // Attach to player
        player.heldItem = spawned;
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = spawned.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log($"[ItemContainer] Dispensed: {itemToDispense.itemName}");
    }
}