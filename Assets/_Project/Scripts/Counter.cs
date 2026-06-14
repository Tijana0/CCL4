using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    public Transform counterTopPoint;
    public GameObject itemOnCounter;

    private void Start()
    {
        // If an item was placed on the counter in the editor, link it up
        if (itemOnCounter != null)
        {
            SetupItemOnCounter(itemOnCounter);
        }
    }

    public bool CanInteract()
    {
        return true; 
    }

    public void Interact(SimplePlayerController player)
    {
        // place item on empty counter
        if (player.heldItem != null && itemOnCounter == null)
        {
            itemOnCounter = player.heldItem;
            player.heldItem = null;
            SetupItemOnCounter(itemOnCounter);
        }
        // take item from counter
        else if (player.heldItem == null && itemOnCounter != null)
        {
            GameObject itemToTake = itemOnCounter;
            itemOnCounter = null;

            // CRITICAL: Ensure collider is disabled BEFORE parenting to player
            // This prevents the "physics push" glitch
            Collider col = itemToTake.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            player.heldItem = itemToTake;
            itemToTake.transform.SetParent(player.holdPoint);
            itemToTake.transform.localPosition = Vector3.zero;
            itemToTake.transform.localRotation = Quaternion.identity;
        }
    }

    private void SetupItemOnCounter(GameObject item)
    {
        item.transform.SetParent(counterTopPoint != null ? counterTopPoint : this.transform);
        item.transform.localRotation = Quaternion.identity;
        
        // Disable collider so it doesn't block interaction or cause physics jitter
        Collider itemCol = item.GetComponent<Collider>();
        if (itemCol != null) itemCol.enabled = false;

        // Ensure Rigidbody is kinematic
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // SIMPLE OFFSET: 
        // Based on your 1x1 grid and item scale, 0.2 is the exact half-height 
        // for most of your objects. This is much more reliable than bounds checks.
        item.transform.localPosition = new Vector3(0, 0.2f, 0);
    }
}
