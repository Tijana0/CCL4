using UnityEngine;

/// <summary>
/// Add this to the Teacup item — both Empty and any "currently being filled" state.
/// Defines what fills it and from what sources, WITHOUT ever spilling itself.
///
/// Behaviour:
///   - A Teacup can be filled by EITHER a Full Water Bucket OR a brewed Teapot
///     being poured onto it (both use the existing PourSource -> here as PourTarget).
///   - The Teacup NEVER acts as a PourSource itself — it has no UsableItem/PourSource
///     component, so pressing the action button while holding it does nothing.
///   - The Teacup only empties via the Bin (InstantStation handles that automatically
///     once isDisposable is configured correctly — see notes below).
///
/// Setup:
///   - On the Teacup prefab (the EMPTY one sitting on a counter), add this component.
///   - filledByWater: drag the FullTeacup-with-water variant (e.g. TeacupWater)
///   - filledByTea: leave empty if Tea variant varies — handled by linkedStation
///     auto-detection (same as cauldron), OR set a fixed FullTeacup-with-tea item
///     if you want one universal "has tea" result regardless of which Tea was poured.
/// </summary>
public class TeacupFillTarget : MonoBehaviour
{
    public enum FillType { None, Water, Tea }

    [Header("Identity")]
    [Tooltip("What this Teacup currently contains. Set automatically once filled.")]
    public FillType currentFill = FillType.None;

    [Header("Fill From Water")]
    [Tooltip("What this Teacup becomes when a Full Water Bucket pours into it.")]
    public ItemData filledByWaterItem;

    [Header("Fill From Tea")]
    [Tooltip("What this Teacup becomes when a brewed Teapot pours into it. " +
             "If the Teapot's tea type varies (Tea1/2/3/Wrong), this is used as the " +
             "DEFAULT — actual tea variant is read from the pouring Teapot's WorldItem if possible.")]
    public ItemData filledByTeaItem;

    /// <summary>
    /// Called by PourSource (water bucket or teapot) when poured into this Teacup.
    /// Source identifies what's being poured via its own ItemData/PourSource config.
    /// </summary>
    public void OnPoured(SimplePlayerController pourer, ItemData sourceItemData)
    {
        if (currentFill != FillType.None)
        {
            Debug.Log("[TeacupFillTarget] Teacup is already full — empty it at the Bin first.");
            return;
        }

        bool isWaterSource = sourceItemData != null && sourceItemData.itemName.ToLower().Contains("water");
        ItemData result = isWaterSource ? filledByWaterItem : filledByTeaItem;

        if (result == null || result.prefab == null)
        {
            Debug.LogWarning("[TeacupFillTarget] No matching fill item configured for this pour source.");
            return;
        }

        Transform parent = transform.parent;
        Vector3 pos = transform.position;

        GameObject newItem = Instantiate(result.prefab, pos, Quaternion.identity, parent);
        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = result;

        newItem.transform.localPosition = transform.localPosition;
        newItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = newItem.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in newItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Keep the new Teacup instance correctly tracked by its parent station, if any
        StationBase station = GetComponentInParent<StationBase>();
        if (station != null && station.itemOnStation == gameObject)
            station.itemOnStation = newItem;

        Debug.Log($"[TeacupFillTarget] Teacup filled with {(isWaterSource ? "water" : "tea")} -> {result.itemName}");
        Destroy(gameObject);
    }
}