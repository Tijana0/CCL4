using UnityEngine;

public enum ItemType
{
    Standard,
    Gold,
    Energy,
    Secret
}

public class PickupObject : MonoBehaviour
{
    public ItemType itemType = ItemType.Standard;
    public int value = 0;
    
    private Transform originalParent;
    private Rigidbody rb;
    private float rotationSpeed = 50f;

    private void Start()
    {
        originalParent = transform.parent;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Rotating while on the ground makes them look more like items
        if (transform.parent == originalParent)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    public void OnPickedUp(Transform newParent)
    {
        if (rb != null)
        {
            rb.isKinematic = true; // Disable physics while holding
        }
        transform.SetParent(newParent);
        transform.localPosition = new Vector3(0, 0.5f, 0.5f); // Position it relative to the player
    }

    public void OnDropped()
    {
        transform.SetParent(originalParent);
        if (rb != null)
        {
            rb.isKinematic = false; // Re-enable physics
        }
        
        // Put it on the ground
        transform.position = new Vector3(transform.position.x, 0.5f, transform.position.z);
    }
}