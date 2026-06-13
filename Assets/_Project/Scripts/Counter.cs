using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    public Transform counterTopPoint;
    public GameObject itemOnCounter;

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

            itemOnCounter.transform.SetParent(counterTopPoint);
            itemOnCounter.transform.localPosition = Vector3.zero;
            itemOnCounter.transform.localRotation = Quaternion.identity;
        }
        // take item from counter
        else if (player.heldItem == null && itemOnCounter != null)
        {
            player.heldItem = itemOnCounter;
            itemOnCounter = null;

            player.heldItem.transform.SetParent(player.holdPoint);
            player.heldItem.transform.localPosition = Vector3.zero;
            player.heldItem.transform.localRotation = Quaternion.identity;
        }
    }
}