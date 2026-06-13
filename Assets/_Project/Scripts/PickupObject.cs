using UnityEngine;

public enum ItemType
{
    Standard,
    Gold,
    Energy,
    Secret
}

public class PickupObject : MonoBehaviour, IInteractable
{
    public ItemType itemType = ItemType.Standard;
    public int value = 0;
    
    private Rigidbody rb;
    private Collider col;
    private float rotationSpeed = 50f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        // rotate only when placed in the world with active colliders
        if (col != null && col.enabled)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    public bool CanInteract()
    {
        return true;
    }

    public void Interact(SimplePlayerController player)
    {
        // only allow picking up if hands are empty
        if (player.heldItem == null)
        {
            player.heldItem = this.gameObject;
            
            if (rb != null) rb.isKinematic = true;
            // disable collider so overlapbox ignores it while held
            if (col != null) col.enabled = false; 

            transform.SetParent(player.holdPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public void Drop(Vector3 dropPosition)
    {
        transform.SetParent(null);
        transform.position = dropPosition;
        
        if (rb != null) rb.isKinematic = false;
        // reenable collider to allow pickup again
        if (col != null) col.enabled = true; 
    }
}