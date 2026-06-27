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

    public bool IsItemAllowed(ItemData item)
    {
        if (!whitelistSet || allowedItems.Count == 0) return true;
        return allowedItems.Contains(item);
    }

    public bool CanInteract() => true;

    // Override in subclasses that have a ready result the player can pick up
    // even when itemOnStation is null (e.g. MultiIngredientStation after cooking).
    public virtual bool HasReadyResult => false;

    public virtual void Interact(SimplePlayerController player)
    {
        // Bin and Sink are handled entirely by InstantStation.Interact()
        // StationBase should never process these
        if (stationType == StationType.Bin || stationType == StationType.Sink)
            return;

        // Block placing/combining items on counter1 and counter2 dispenser slots
        if (player.heldItem != null && 
            (gameObject.name.StartsWith("counter1_slot") ||
             gameObject.name == "counter2_slot_2" || 
             gameObject.name == "counter2_slot_3" || 
             gameObject.name == "counter2_slot_4"))
        {
            Debug.Log($"[StationBase] Cannot place items on dispenser slot: {gameObject.name}");
            return;
        }

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
        // Block placing/combining items on counter1 and counter2 dispenser slots
        if (gameObject.name.StartsWith("counter1_slot") ||
            gameObject.name == "counter2_slot_2" || 
            gameObject.name == "counter2_slot_3" || 
            gameObject.name == "counter2_slot_4")
        {
            Debug.Log($"[StationBase] Cannot place items on dispenser slot in TryPlaceItem: {gameObject.name}");
            return;
        }

        WorldItem worldItem = player.heldItem.GetComponent<WorldItem>();

        if (worldItem == null)
        {
            PlaceItemOnStation(player.heldItem, player);
            return;
        }

        // Plain counters accept any item — only specialized stations enforce the whitelist.
        // RoomConfig whitelist — only source of truth for non-counter stations.
        if (stationType != StationType.Counter && whitelistSet && allowedItems.Count > 0 && !allowedItems.Contains(worldItem.itemData))
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

        // Preserve the item's intended world scale before reparenting —
        // prevents items shrinking/growing when placed on a scaled station.
        Vector3 worldScaleBeforeParent = item.transform.lossyScale;

        item.transform.SetParent(anchor);
        item.transform.localRotation = Quaternion.identity;

        // Re-derive localScale so the item's WORLD scale stays the same
        // regardless of the anchor's own scale.
        Vector3 anchorScale = anchor.lossyScale;
        item.transform.localScale = new Vector3(
            anchorScale.x != 0 ? worldScaleBeforeParent.x / anchorScale.x : worldScaleBeforeParent.x,
            anchorScale.y != 0 ? worldScaleBeforeParent.y / anchorScale.y : worldScaleBeforeParent.y,
            anchorScale.z != 0 ? worldScaleBeforeParent.z / anchorScale.z : worldScaleBeforeParent.z
        );

        // DYNAMIC OFFSET: Calculate height using local properties to avoid stale physics bounds.
        float yOffset = 0.2f; // Default fallback
        Collider col = item.GetComponent<Collider>();
        if (col != null)
        {
            float localBottomY = GetLocalBottomY(col);
            yOffset = -localBottomY * item.transform.localScale.y;
        }
        
        item.transform.localPosition = new Vector3(0, yOffset, 0);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        // Re-use 'col' from above if it exists to disable it
        if (col != null) col.enabled = false;

        if (player != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayPutDown(this.gameObject);

        OnItemPlaced(item);
    }

    private float GetLocalBottomY(Collider col)
    {
        if (col == null) return 0f;

        if (col is BoxCollider box)
        {
            return box.center.y - box.size.y * 0.5f;
        }
        else if (col is SphereCollider sphere)
        {
            return sphere.center.y - sphere.radius;
        }
        else if (col is CapsuleCollider capsule)
        {
            return capsule.center.y - capsule.height * 0.5f;
        }
        else if (col is MeshCollider meshCol && meshCol.sharedMesh != null)
        {
            return meshCol.sharedMesh.bounds.center.y - meshCol.sharedMesh.bounds.extents.y;
        }

        return 0f;
    }

    protected virtual void TakeItem(SimplePlayerController player)
    {
        GameObject item = itemOnStation;
        itemOnStation = null;

        player.heldItem = item;

        Vector3 worldScaleBeforeParent = item.transform.lossyScale;

        item.transform.SetParent(player.holdPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Vector3 holdScale = player.holdPoint.lossyScale;
        item.transform.localScale = new Vector3(
            holdScale.x != 0 ? worldScaleBeforeParent.x / holdScale.x : worldScaleBeforeParent.x,
            holdScale.y != 0 ? worldScaleBeforeParent.y / holdScale.y : worldScaleBeforeParent.y,
            holdScale.z != 0 ? worldScaleBeforeParent.z / holdScale.z : worldScaleBeforeParent.z
        );

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup(this.gameObject, false);

        OnItemTaken(item);
    }

    protected virtual void TryCombineOrSwap(SimplePlayerController player) { }
    protected virtual void OnItemPlaced(GameObject item) { }
    protected virtual void OnItemTaken(GameObject item) { }
    protected virtual void OnPlacementRejected(SimplePlayerController player, WorldItem item) { }
}