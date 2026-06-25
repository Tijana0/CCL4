using UnityEngine;

/// <summary>
/// Add this to a held item that can be POURED onto a PourTarget
/// (e.g. WarmTeapot poured into a Teacup, or Empty Bottle poured into a Cauldron).
///
/// Integrated with the player's standard Interact key through SimplePlayerController.
/// This item then resets to its "empty/reusable" state (or clears its contents in-place
/// if it is a PortableCooker) instead of being destroyed.
///
/// Setup on WarmTeapot prefab:
///   resetToItem = EmptyTeapot
///   pourRange = 1.5
/// </summary>
public class PourSource : MonoBehaviour
{
    [Header("Pour Settings")]
    [Tooltip("What this item becomes after pouring (e.g. WarmTeapot -> EmptyTeapot). Ignored for PortableCookers which clear in-place.")]
    public ItemData resetToItem;
    [Tooltip("How close a PourTarget must be to pour into it")]
    public float pourRange = 1.5f;

    private WorldItem selfWorldItem;

    private void Awake()
    {
        selfWorldItem = GetComponent<WorldItem>();
    }

    /// <summary>Checks if a pour is currently possible (a target is in range and cooker is done).</summary>
    public bool CanPour()
    {
        PourTarget target = FindNearbyTarget<PourTarget>();
        if (target == null) return false;

        PortableCooker cooker = GetComponent<PortableCooker>();
        if (cooker != null && !cooker.IsDone) return false;

        return true;
    }

    /// <summary>Executes the pour action onto the nearby target.</summary>
    public void ExecutePour(SimplePlayerController pourer)
    {
        PourTarget target = FindNearbyTarget<PourTarget>();
        if (target == null) return;

        ItemData pouredItem = null;
        PortableCooker cooker = GetComponent<PortableCooker>();
        if (cooker != null)
        {
            pouredItem = cooker.LastBrewedResult;
        }
        else if (selfWorldItem != null)
        {
            pouredItem = selfWorldItem.itemData;
        }

        // Trigger the pour on the target
        target.OnPoured(pourer, pouredItem);

        // Reset or clear ourselves
        ResetSelf(pourer, cooker);
    }

    private T FindNearbyTarget<T>() where T : Component
    {
        T[] targets = FindObjectsByType<T>(FindObjectsSortMode.None);
        T closest = null;
        float closestDist = float.MaxValue;

        foreach (var t in targets)
        {
            float dist = Vector3.Distance(t.transform.position, transform.position);
            if (dist <= pourRange && dist < closestDist)
            {
                closest = t;
                closestDist = dist;
            }
        }
        return closest;
    }

    private void ResetSelf(SimplePlayerController pourer, PortableCooker cooker)
    {
        // If we are a PortableCooker (e.g. Teapot), don't destroy ourselves!
        // Just consume the result and clear the contents to become empty/reusable in-place.
        if (cooker != null)
        {
            cooker.ConsumeResult();
            Debug.Log("[PourSource] PortableCooker (Teapot) poured and cleared in-place.");
            return;
        }

        // Otherwise, follow default behavior: destroy ourselves and instantiate resetToItem in hands
        if (resetToItem == null || resetToItem.prefab == null)
        {
            Debug.LogWarning("[PourSource] No resetToItem assigned — destroying instead.");
            Destroy(gameObject);
            return;
        }

        GameObject newItem = Instantiate(resetToItem.prefab, pourer.holdPoint.position, Quaternion.identity, pourer.holdPoint);
        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = resetToItem;

        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = newItem.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in newItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        pourer.heldItem = newItem;

        Debug.Log($"[PourSource] Poured! Reset to {resetToItem.itemName}");
        Destroy(gameObject);
    }
}