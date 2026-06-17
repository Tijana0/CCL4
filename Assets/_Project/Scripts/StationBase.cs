using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base for all stations. Handles common item placement/removal logic.
/// Subclass this for any new station type.
///
/// Item whitelists are set at runtime by RoomManager reading the RoomConfig.
/// You do NOT set whitelists on individual stations in the Inspector —
/// configure them in your scene's RoomConfig component instead.
/// </summary>
public abstract class StationBase : MonoBehaviour, IInteractable
{
    [Header("Station Config")]
    public StationType stationType;
    public Transform counterTopPoint;

    [HideInInspector] public GameObject itemOnStation;

    // Set by RoomManager on Start — which items this station accepts
    private List<ItemData> allowedItems = new List<ItemData>();
    private bool whitelistSet = false;

    /// <summary>Called by RoomManager to configure this station's item whitelist.</summary>
    public void SetAllowedItems(List<ItemData> items)
    {
        allowedItems = items ?? new List<ItemData>();
        whitelistSet = true;
    }

    public bool CanInteract() => true;

    public virtual void Interact(SimplePlayerController player)
    {
        bool playerHasItem = player.heldItem != null;
        bool stationHasItem = itemOnStation != null;

        if (playerHasItem && !stationHasItem)
            TryPlaceItem(player);
        else if (!playerHasItem && stationHasItem)
            TakeItem(player);
        else if (playerHasItem && stationHasItem)
            TryCombineOrSwap(player);
    }

    /// <summary>
    /// Attempt to place the player's held item onto this station.
    /// Checks the station's item whitelist before allowing placement.
    /// </summary>
    protected virtual void TryPlaceItem(SimplePlayerController player)
    {
        WorldItem worldItem = player.heldItem.GetComponent<WorldItem>();

        if (worldItem == null)
        {
            // Legacy item with no WorldItem — allow placement freely
            PlaceItemOnStation(player.heldItem, player);
            return;
        }

        // Check station whitelist (only if RoomManager has set one)
        if (whitelistSet && allowedItems.Count > 0 && !allowedItems.Contains(worldItem.itemData))
        {
            Debug.Log($"[Station:{stationType}] {worldItem.itemData.itemName} is not allowed here.");
            OnPlacementRejected(player, worldItem);
            return;
        }

        PlaceItemOnStation(player.heldItem, player);
    }

    /// <summary>Physically moves item from player hands to station.</summary>
    protected void PlaceItemOnStation(GameObject item, SimplePlayerController player)
    {
        itemOnStation = item;
        if (player != null) player.heldItem = null;

        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        item.transform.SetParent(anchor);
        item.transform.localRotation = Quaternion.identity;

        // DYNAMIC OFFSET: Calculate height to prevent sinking
        // Most primitives have pivots at the center, so we need half-height offset
        float yOffset = 0.2f; // Default fallback
        Collider col = item.GetComponent<Collider>();
        if (col != null)
        {
            // Use bounds extents for a generic solution that works for any shape
            yOffset = col.bounds.extents.y;
        }
        
        item.transform.localPosition = new Vector3(0, yOffset, 0);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        OnItemPlaced(item);
    }

    /// <summary>Player picks up the item from this station.</summary>
    protected virtual void TakeItem(SimplePlayerController player)
    {
        GameObject item = itemOnStation;
        itemOnStation = null;

        player.heldItem = item;
        item.transform.SetParent(player.holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        OnItemTaken(item);
    }

    /// <summary>
    /// Called when a player interacts while both player and station have an item.
    /// Base does nothing. Override in Counter for combining.
    /// </summary>
    protected virtual void TryCombineOrSwap(SimplePlayerController player) { }

    // Hooks for subclasses
    protected virtual void OnItemPlaced(GameObject item) { }
    protected virtual void OnItemTaken(GameObject item) { }
    protected virtual void OnPlacementRejected(SimplePlayerController player, WorldItem item) { }
}