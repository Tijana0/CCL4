using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Abstract base for all stations.
/// RoomConfig whitelist is the single source of truth for what goes where.
/// Bin and Sink are handled entirely by InstantStation — StationBase never
/// places items for them.
/// </summary>
public abstract class StationBase : MonoBehaviour, IInteractable
{
    [Header("Station Config")]
    public StationType stationType;
    public Transform counterTopPoint;

    [HideInInspector] public GameObject itemOnStation;

    private List<ItemData> allowedItems = new List<ItemData>();
    private bool whitelistSet = false;

    public void SetAllowedItems(List<ItemData> items)
    {
        allowedItems = items ?? new List<ItemData>();
        whitelistSet = true;
    }

    public bool CanInteract() => true;

    public virtual void Interact(SimplePlayerController player)
    {
        // Bin and Sink are handled entirely by InstantStation.Interact()
        // StationBase should never process these
        if (stationType == StationType.Bin || stationType == StationType.Sink)
            return;

        bool playerHasItem = player.heldItem != null;
        bool stationHasItem = itemOnStation != null;

        if (playerHasItem && !stationHasItem)
            TryPlaceItem(player);
        else if (!playerHasItem && stationHasItem)
            TakeItem(player);
        else if (playerHasItem && stationHasItem)
            TryCombineOrSwap(player);
    }

    protected virtual void TryPlaceItem(SimplePlayerController player)
    {
        WorldItem worldItem = player.heldItem.GetComponent<WorldItem>();

        if (worldItem == null)
        {
            PlaceItemOnStation(player.heldItem, player);
            return;
        }

        // RoomConfig whitelist — only source of truth
        if (whitelistSet && allowedItems.Count > 0 && !allowedItems.Contains(worldItem.itemData))
        {
            Debug.Log($"[Station:{stationType}] {worldItem.itemData.itemName} is not allowed here.");
            OnPlacementRejected(player, worldItem);
            return;
        }

        PlaceItemOnStation(player.heldItem, player);
    }

    protected void PlaceItemOnStation(GameObject item, SimplePlayerController player)
    {
        itemOnStation = item;
        if (player != null) player.heldItem = null;

        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        item.transform.SetParent(anchor);
        item.transform.localRotation = Quaternion.identity;

        // DYNAMIC OFFSET: Calculate height to prevent sinking
        float yOffset = 0.2f; // Default fallback
        Collider col = item.GetComponent<Collider>();
        if (col != null)
        {
            // Calculate how far the bottom of the collider is from the pivot in local space.
            // col.bounds is in world space, so we convert the bottom point to local space.
            Vector3 worldBottom = col.bounds.center - new Vector3(0, col.bounds.extents.y, 0);
            Vector3 localBottom = item.transform.InverseTransformPoint(worldBottom);
            
            // To place the bottom at Y=0 local, the pivot must be at -localBottom.y
            yOffset = -localBottom.y;
        }
        
        item.transform.localPosition = new Vector3(0, yOffset, 0);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        // Re-use 'col' from above if it exists to disable it
        if (col != null) col.enabled = false;

        OnItemPlaced(item);
    }

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

    protected virtual void TryCombineOrSwap(SimplePlayerController player) { }
    protected virtual void OnItemPlaced(GameObject item) { }
    protected virtual void OnItemTaken(GameObject item) { }
    protected virtual void OnPlacementRejected(SimplePlayerController player, WorldItem item) { }
}