using UnityEngine;

/// <summary>
/// Handles stations that act INSTANTLY when an item is placed.
/// Bin, Sink, HandIn — now with score integration via OrderManager.
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

        switch (stationType)
        {
            case StationType.Bin:    HandleBin(player);    break;
            case StationType.Sink:   HandleSink(player);   break;
            case StationType.HandIn: HandleHandIn(player); break;
            default:
                Debug.LogWarning($"[InstantStation] Unhandled type: {stationType}");
                break;
        }
    }

    private void HandleBin(SimplePlayerController player)
    {
        Debug.Log($"[Bin] Destroyed: {player.heldItem.name}");
        Object.Destroy(player.heldItem);
        player.heldItem = null;
    }

    private void HandleSink(SimplePlayerController player)
    {
        WorldItem wi = player.heldItem.GetComponent<WorldItem>();
        Debug.Log($"[Sink] Cleared: {(wi != null ? wi.itemData.itemName : player.heldItem.name)}");
        Object.Destroy(player.heldItem);
        player.heldItem = null;
    }

    private void HandleHandIn(SimplePlayerController player)
    {
        WorldItem wi = player.heldItem.GetComponent<WorldItem>();
        if (wi == null)
        {
            Debug.Log("[HandIn] Item has no WorldItem — cannot score.");
            return;
        }

        int score = 0;
        if (orderManager != null)
            score = orderManager.TryCompleteOrder(wi.itemData);

        if (score > 0 && GameManager.Instance != null)
            GameManager.Instance.AddScore(score);

        Object.Destroy(player.heldItem);
        player.heldItem = null;
    }
}