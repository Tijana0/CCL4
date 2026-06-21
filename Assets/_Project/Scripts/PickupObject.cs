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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        CenterPivotAroundVisuals();
    }

    private void CenterPivotAroundVisuals()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) return;

        // Calculate visual bounds center in world space
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        Vector3 visualCenter = bounds.center;

        // Store children to detach
        System.Collections.Generic.List<Transform> children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        // Detach children temporarily to preserve world positions/rotations
        foreach (var child in children)
        {
            child.SetParent(null);
        }

        // Move parent to visual center
        transform.position = visualCenter;

        // Reattach children
        foreach (var child in children)
        {
            child.SetParent(transform);
        }

        // Adjust collider center and size to match visual bounds.
        // bounds.size is world-space; boxCol.size is local-space, so divide by lossyScale.
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.center = Vector3.zero;
            Vector3 ls = transform.lossyScale;
            boxCol.size = new Vector3(
                ls.x != 0 ? bounds.size.x / ls.x : bounds.size.x,
                ls.y != 0 ? bounds.size.y / ls.y : bounds.size.y,
                ls.z != 0 ? bounds.size.z / ls.z : bounds.size.z
            );
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

            Vector3 worldScaleBeforePickup = transform.lossyScale;

            transform.SetParent(player.holdPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // Counteract holdPoint scale so item doesn't shrink/grow when picked up
            Vector3 holdScale = player.holdPoint.lossyScale;
            transform.localScale = new Vector3(
                holdScale.x != 0 ? worldScaleBeforePickup.x / holdScale.x : worldScaleBeforePickup.x,
                holdScale.y != 0 ? worldScaleBeforePickup.y / holdScale.y : worldScaleBeforePickup.y,
                holdScale.z != 0 ? worldScaleBeforePickup.z / holdScale.z : worldScaleBeforePickup.z
            );

            // Stop rotation Update when held
            this.enabled = false;
        }
    }

    public void Drop(Vector3 dropPosition)
    {
        Vector3 worldScaleBeforeDrop = transform.lossyScale;

        transform.SetParent(null);
        transform.position = dropPosition;
        transform.localScale = worldScaleBeforeDrop;
        
        if (rb != null) rb.isKinematic = true;
        // reenable collider to allow pickup again
        if (col != null) col.enabled = true; 
    }
}