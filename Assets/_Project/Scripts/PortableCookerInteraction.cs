using UnityEngine;

/// <summary>
/// Add this alongside PortableCooker on the same Teapot item.
/// Implements IInteractable so OTHER PLAYERS can add ingredients to this
/// Teapot or take its finished result, the same way they'd interact with
/// any station — except this "station" happens to be a held/placed item.
///
/// How it works with the existing pickup system:
///   - If a player is already holding this Teapot, they can't also "interact"
///     with it (it's in their hands) — ingredients are added by walking up
///     to a DROPPED/PLACED Teapot while holding Water or a Herb, OR by
///     dropping the Teapot somewhere and interacting with it directly.
///   - If a player has empty hands and the Teapot is finished cooking,
///     interacting picks up the result item directly into their hands
///     (the Teapot itself stays in place, emptied).
///
/// Setup:
///   - Add to the same Teapot prefab as PortableCooker.
///   - No fields to configure — it reads everything from the sibling
///     PortableCooker component automatically.
/// </summary>
[RequireComponent(typeof(PortableCooker))]
public class PortableCookerInteraction : MonoBehaviour, IInteractable
{
    private PortableCooker cooker;

    private void Awake()
    {
        cooker = GetComponent<PortableCooker>();
    }

    public bool CanInteract() => true;

    public void Interact(SimplePlayerController player)
    {
        // Player holding empty hands and the cooker is done -> take the result
        if (player.heldItem == null && cooker.IsDone)
        {
            TakeResult(player);
            return;
        }

        // Player holding an item -> try adding it as an ingredient
        if (player.heldItem != null)
        {
            WorldItem heldWI = player.heldItem.GetComponent<WorldItem>();
            if (heldWI == null)
            {
                Debug.Log("[PortableCookerInteraction] Held item has no WorldItem component.");
                return;
            }

            bool added = cooker.TryAddIngredient(heldWI.itemData);
            if (added)
            {
                Destroy(player.heldItem);
                player.heldItem = null;
            }
            return;
        }

        // Empty hands and nothing finished to take -> pick up the pot itself.
        // This component is chosen over the sibling PickupObject, so we delegate to it
        // here to keep normal pickup working.
        PickupObject pickup = GetComponent<PickupObject>();
        if (pickup != null) pickup.Interact(player);
    }

    private void TakeResult(SimplePlayerController player)
    {
        ItemData result = cooker.ConsumeResult();
        if (result == null || result.prefab == null)
        {
            Debug.LogWarning("[PortableCookerInteraction] No result available to take.");
            return;
        }

        GameObject spawned = Instantiate(result.prefab, player.holdPoint.position, Quaternion.identity, player.holdPoint);
        WorldItem wi = spawned.GetComponent<WorldItem>();
        if (wi == null) wi = spawned.AddComponent<WorldItem>();
        wi.itemData = result;

        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;

        Rigidbody rb = spawned.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in spawned.GetComponentsInChildren<Collider>())
            col.enabled = false;

        player.heldItem = spawned;

        Debug.Log($"[PortableCookerInteraction] Took result: {result.itemName}. Teapot is now empty and reusable.");
    }
}