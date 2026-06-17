using UnityEngine;

/// <summary>
/// Plain counter — holds one item, but supports combining two items into one
/// if a CombineRule exists between them (e.g. red ball + blue ball = purple ball).
/// </summary>
public class Counter : StationBase
{
    private void Awake()
    {
        stationType = StationType.Counter;
    }

    private void Start()
    {
        if (itemOnStation != null)
            PlaceItemOnStation(itemOnStation, null); // null player = editor pre-placed item
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

        // Check for a combine rule between the two items
        ItemData result = heldWI.itemData.GetCombineResult(stationWI.itemData);

        if (result == null)
        {
            Debug.Log($"[Counter] No combine rule between {heldWI.itemData.itemName} and {stationWI.itemData.itemName}");
            return;
        }

        // Destroy both items
        Destroy(player.heldItem);
        Destroy(itemOnStation);
        player.heldItem = null;
        itemOnStation = null;

        // Spawn result on the counter
        Transform anchor = counterTopPoint != null ? counterTopPoint : transform;
        Vector3 spawnPos = anchor.position + Vector3.up * 0.2f;

        WorldItem combined = WorldItem.CreateCombined(result, spawnPos, anchor);
        if (combined != null)
        {
            // Use the shared placement logic from StationBase to handle positioning/physics/colliders
            PlaceItemOnStation(combined.gameObject, null);
            Debug.Log($"[Counter] Combined into {result.itemName}!");
        }
    }
}
