using UnityEngine;

public class PickupObject : MonoBehaviour
{
    private Transform originalParent;
    private Rigidbody rb;

    private void Start()
    {
        originalParent = transform.parent;
        rb = GetComponent<Rigidbody>();
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