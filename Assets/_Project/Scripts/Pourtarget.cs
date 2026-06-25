using UnityEngine;

/// <summary>
/// Add this to anything that can RECEIVE a pour from a PourSource.
///
/// Two modes, auto-detected:
///
/// 1. FIXED MODE (e.g. EmptyTeacup sitting on a Counter)
///    Set filledItem in the Inspector. Pouring transforms this object
///    into that fixed item.
///
/// 2. STATION MODE (e.g. Cauldron with MultiIngredientStation)
///    Leave filledItem empty. If a MultiIngredientStation is on the same
///    GameObject, pouring takes its currently brewed result (already
///    tinted/coloured) and hands it directly to the pourer instead.
/// </summary>
public class PourTarget : MonoBehaviour
{
    [Header("Fixed Mode (e.g. Teacup)")]
    [Tooltip("What this item becomes after being poured into. Leave empty to use Station Mode instead.")]
    public ItemData filledItem;

    private MultiIngredientStation linkedStation;

    private void Awake()
    {
        linkedStation = GetComponent<MultiIngredientStation>();
    }

    /// <summary>Called by PourSource when it pours into this object.</summary>
    public void OnPoured(SimplePlayerController pourer, ItemData pouredItem)
    {
        // If we have a TeacupFillTarget, use its dynamic mappings!
        TeacupFillTarget fillTarget = GetComponent<TeacupFillTarget>();
        if (fillTarget != null && pouredItem != null)
        {
            PourDynamicMode(pouredItem);
            return;
        }

        if (filledItem != null)
        {
            PourFixedMode();
        }
        else if (linkedStation != null)
        {
            PourStationMode(pourer);
        }
        else
        {
            Debug.LogWarning("[PourTarget] No filledItem/TeacupFillTarget assigned and no MultiIngredientStation found — nothing to pour into.");
        }
    }

    // ── Dynamic Mode (e.g. Teacup with dynamic mappings) ──────────────────────

    private void PourDynamicMode(ItemData pouredItem)
    {
        TeacupFillTarget fillTarget = GetComponent<TeacupFillTarget>();
        if (fillTarget == null) return;

        // Find the mapped result item for this poured item
        ItemData resultItem = null;
        foreach (var mapping in fillTarget.mappings)
        {
            if (mapping.teaItem == pouredItem)
            {
                resultItem = mapping.resultTeacupItem;
                break;
            }
        }

        if (resultItem == null)
        {
            Debug.LogWarning($"[PourTarget] No teacup mapping found for poured item: {pouredItem.itemName}");
            return;
        }

        if (resultItem.prefab == null)
        {
            Debug.LogWarning($"[PourTarget] Mapped result item {resultItem.itemName} has no prefab assigned.");
            return;
        }

        Transform parent = transform.parent;
        Vector3 pos = transform.position;

        GameObject newItem = Instantiate(resultItem.prefab, pos, Quaternion.identity, parent);
        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = resultItem;

        newItem.transform.localPosition = transform.localPosition;
        newItem.transform.localRotation = transform.localRotation; // Keep the same rotation!

        Rigidbody rb = newItem.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in newItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // If sitting on a station (like a counter), update the station's reference to point to the new filled cup!
        StationBase station = GetComponentInParent<StationBase>();
        if (station != null && station.itemOnStation == gameObject)
            station.itemOnStation = newItem;

        Debug.Log($"[PourTarget] Dynamic Filled! Became {resultItem.itemName} at {pos}");
        Destroy(gameObject);
    }

    // ── Fixed Mode ───────────────────────────────────────────────────────────

    private void PourFixedMode()
    {
        if (filledItem.prefab == null)
        {
            Debug.LogWarning("[PourTarget] filledItem has no prefab assigned.");
            return;
        }

        Transform parent = transform.parent;
        Vector3 pos = transform.position;

        GameObject newItem = Instantiate(filledItem.prefab, pos, Quaternion.identity, parent);
        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = filledItem;

        newItem.transform.localPosition = transform.localPosition;
        newItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = newItem.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in newItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        StationBase station = GetComponentInParent<StationBase>();
        if (station != null && station.itemOnStation == gameObject)
            station.itemOnStation = newItem;

        Debug.Log($"[PourTarget] Filled! Became {filledItem.itemName}");
        Destroy(gameObject);
    }

    // ── Station Mode (Cauldron) ─────────────────────────────────────────────

    private void PourStationMode(SimplePlayerController pourer)
    {
        GameObject potion = linkedStation.TakeResultItem();
        if (potion == null)
        {
            Debug.Log("[PourTarget] Nothing brewed to pour yet.");
            return;
        }

        if (pourer.heldItem == null)
        {
            potion.transform.SetParent(pourer.holdPoint);
            potion.transform.localPosition = Vector3.zero;
            potion.transform.localRotation = Quaternion.identity;
            pourer.heldItem = potion;

            Rigidbody rb = potion.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            foreach (Collider col in potion.GetComponentsInChildren<Collider>())
                col.enabled = false;

            Debug.Log("[PourTarget] Poured potion into hands.");
        }
        else
        {
            Debug.Log("[PourTarget] Player's hands full — potion left uncollected.");
        }
    }
}