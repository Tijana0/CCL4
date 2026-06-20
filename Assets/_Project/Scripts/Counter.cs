using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Plain counter — holds one item, but supports combining two items into one.
/// Combine rules can be defined directly on this station (checked first),
/// or fall back to ItemData combine rules.
/// </summary>
public class Counter : StationBase
{
    [Header("Combine Rules (defined on this station)")]
    [Tooltip("Define what two items become when combined here. Checked before ItemData combine rules.")]
    public List<StationCombineRule> combineRules = new List<StationCombineRule>();

    private void Awake()
    {
        stationType = StationType.Counter;
    }

    private void Start()
    {
        if (itemOnStation != null)
            PlaceItemOnStation(itemOnStation, null);
    }

    protected override void TryCombineOrSwap(SimplePlayerController player)
    {
        WorldItem heldWI = player.heldItem.GetComponent<WorldItem>();
        WorldItem stationWI = itemOnStation.GetComponent<WorldItem>();

        if (heldWI == null || stationWI == null)
        {
            Debug.Log("[Counter] One of the items has no WorldItem component — cannot combine.");
            return;
        }

        // Check station-defined combine rules first
        ItemData result = GetStationCombineResult(heldWI.itemData, stationWI.itemData);

        // Fall back to ItemData combine rules
        if (result == null)
            result = heldWI.itemData.GetCombineResult(stationWI.itemData);

        if (result == null)
        {
            Debug.Log($"[Counter] No combine rule between {heldWI.itemData.itemName} and {stationWI.itemData.itemName}");
            return;
        }

        Destroy(player.heldItem);
        Destroy(itemOnStation);
        player.heldItem = null;
        itemOnStation = null;

        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        Vector3 spawnPos = anchor.position + Vector3.up * 0.2f;

        WorldItem combined = WorldItem.CreateCombined(result, spawnPos, anchor);
        if (combined != null)
        {
            PlaceItemOnStation(combined.gameObject, null);
            Debug.Log($"[Counter] Combined into {result.itemName}!");
        }
    }

    private ItemData GetStationCombineResult(ItemData a, ItemData b)
    {
        foreach (var rule in combineRules)
        {
            if ((rule.itemA == a && rule.itemB == b) ||
                (rule.itemA == b && rule.itemB == a))
                return rule.outputItem;
        }
        return null;
    }
}

/// <summary>
/// A combine rule defined directly on a Counter station.
/// Order of itemA/itemB does not matter.
/// </summary>
[System.Serializable]
public class StationCombineRule
{
    [Tooltip("First item")]
    public ItemData itemA;
    [Tooltip("Second item")]
    public ItemData itemB;
    [Tooltip("What they combine into")]
    public ItemData outputItem;
}