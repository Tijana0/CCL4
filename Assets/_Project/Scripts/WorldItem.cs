using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attached to every physical item GameObject in the world.
/// Carries its ItemData identity and tracks if it's a combined/stacked item.
/// </summary>
public class WorldItem : MonoBehaviour
{
    [Header("Item Identity")]
    public ItemData itemData;

    [Header("Combined Items (read-only at runtime)")]
    // When two items are stacked on a counter, this holds both their data.
    // The cauldron uses the CauldronStation's own ingredient list instead.
    [SerializeField] private List<ItemData> stackedItems = new List<ItemData>();

    public bool IsStacked => stackedItems.Count > 1;
    public List<ItemData> StackedItems => stackedItems;

    private void Start()
    {
        if (itemData != null && stackedItems.Count == 0)
        {
            stackedItems.Add(itemData);
        }
    }

    /// <summary>
    /// Called when this item is combined with another on a counter.
    /// Creates a new combined WorldItem from the recipe result.
    /// </summary>
    public static WorldItem CreateCombined(ItemData resultData, Vector3 position, Transform parent = null)
    {
        if (resultData == null || resultData.prefab == null)
        {
            Debug.LogWarning("[WorldItem] Cannot create combined item — resultData or prefab is null.");
            return null;
        }

        // Capture the prefab's own intended local scale BEFORE parenting,
        // so it isn't multiplied by a scaled station/counter parent.
        Vector3 prefabScale = resultData.prefab.transform.localScale;

        GameObject go = Instantiate(resultData.prefab, position, Quaternion.identity, parent);

        // Re-apply the prefab's original scale in local space relative to the new parent,
        // counteracting any non-uniform parent scale (the "big" and "flat" bug).
        if (parent != null)
        {
            Vector3 parentScale = parent.lossyScale;
            go.transform.localScale = new Vector3(
                parentScale.x != 0 ? prefabScale.x / parentScale.x : prefabScale.x,
                parentScale.y != 0 ? prefabScale.y / parentScale.y : prefabScale.y,
                parentScale.z != 0 ? prefabScale.z / parentScale.z : prefabScale.z
            );
        }
        else
        {
            go.transform.localScale = prefabScale;
        }

        WorldItem wi = go.GetComponent<WorldItem>();
        if (wi == null) wi = go.AddComponent<WorldItem>();
        wi.itemData = resultData;
        wi.stackedItems.Clear();
        wi.stackedItems.Add(resultData);
        return wi;
    }

    public override string ToString()
    {
        return itemData != null ? itemData.itemName : "Unknown Item";
    }
}