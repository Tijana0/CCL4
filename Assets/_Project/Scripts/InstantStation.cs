using UnityEngine;

/// <summary>
/// Handles Bin, Sink, HandIn stations.
/// Bin and Sink bypass ALL whitelists — StationBase.Interact() handles this.
/// HandIn goes through normal whitelist flow.
/// </summary>
public class InstantStation : StationBase
{
    [Header("Hand-In Settings")]
    public OrderManager orderManager;

    private void Start()
    {
        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();
    }

    public override void Interact(SimplePlayerController player)
    {
        if (player.heldItem == null) return;

        // For Bin and Sink, act immediately — no whitelist, no placement
        if (stationType == StationType.Bin)
        {
            HandleBin(player);
            return;
        }
        if (stationType == StationType.Sink)
        {
            HandleSink(player);
            return;
        }

        // HandIn goes through normal StationBase flow (whitelist check applies)
        base.Interact(player);
    }

    protected override void OnItemPlaced(GameObject item)
    {
        // Called after HandIn whitelist passes — score and destroy
        if (stationType == StationType.HandIn)
        {
            WorldItem wi = item.GetComponent<WorldItem>();
            if (wi == null) return;

            int score = 0;
            if (orderManager != null)
                score = orderManager.TryCompleteOrder(wi.itemData);

            if (score > 0 && GameManager.Instance != null)
                GameManager.Instance.AddScore(score);

            Destroy(item);
            itemOnStation = null;
        }
    }

    private void HandleBin(SimplePlayerController player)
    {
        // Special case: PortableCooker items (e.g. Teapot) are NEVER destroyed
        // at the Bin — their contents are cleared instead, keeping the item reusable.
        PortableCooker cooker = player.heldItem.GetComponent<PortableCooker>();
        if (cooker != null)
        {
            cooker.ClearContents();
            Debug.Log("[Bin] Cleared contents — item kept in hand, now empty.");
            return;
        }

        WorldItem wi = player.heldItem.GetComponent<WorldItem>();

        if (wi != null && !wi.itemData.isDisposable)
        {
            Debug.Log($"[Bin] {wi.itemData.itemName} cannot be thrown away — not disposable.");
            return;
        }

        Debug.Log($"[Bin] Destroyed: {player.heldItem.name}");
        Destroy(player.heldItem);
        player.heldItem = null;
    }

    private void HandleSink(SimplePlayerController player)
    {
        WorldItem wi = player.heldItem.GetComponent<WorldItem>();
        Debug.Log($"[Sink] Cleared: {(wi != null ? wi.itemData.itemName : player.heldItem.name)}");
        Destroy(player.heldItem);
        player.heldItem = null;
    }
}