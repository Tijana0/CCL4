using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this to a held item that can be POURED onto a PourTarget
/// (e.g. WarmTeapot poured into a Teacup, or Empty Bottle poured into a Cauldron).
///
/// Player holds this item, walks near a PourTarget, presses Q.
/// PourTarget decides what happens (transform itself, or hand over a
/// brewed potion from a MultiIngredientStation).
/// This item then resets to its "empty/reusable" state instead of being destroyed.
///
/// Setup on WarmTeapot prefab:
///   resetToItem = EmptyTeapot
///   pourRange = 1.5
/// </summary>
public class PourSource : MonoBehaviour
{
    [Header("Pour Settings")]
    [Tooltip("What this item becomes after pouring (e.g. WarmTeapot -> EmptyTeapot)")]
    public ItemData resetToItem;
    [Tooltip("How close a PourTarget must be to pour into it")]
    public float pourRange = 1.5f;

    private SimplePlayerController owner;
    private WorldItem selfWorldItem;

    private void Awake()
    {
        selfWorldItem = GetComponent<WorldItem>();
    }

    private void Update()
    {
        if (owner == null)
        {
            FindOwner();
            return;
        }

        if (owner.heldItem != gameObject)
        {
            owner = null;
            return;
        }

        if (!CheckActionPressed(owner)) return;

        // Fall back to the regular PourTarget (Cauldron, etc.)
        PourTarget target = FindNearbyTarget<PourTarget>();
        if (target != null)
        {
            target.OnPoured(owner);
            ResetSelf();
        }
        else
        {
            Debug.Log("[PourSource] No pour target in range.");
        }
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

    private void ResetSelf()
    {
        if (resetToItem == null || resetToItem.prefab == null)
        {
            Debug.LogWarning("[PourSource] No resetToItem assigned — destroying instead.");
            Destroy(gameObject);
            return;
        }

        GameObject newItem = Instantiate(resetToItem.prefab, owner.holdPoint.position, Quaternion.identity, owner.holdPoint);
        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = resetToItem;

        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = newItem.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider col in newItem.GetComponentsInChildren<Collider>())
            col.enabled = false;

        owner.heldItem = newItem;

        Debug.Log($"[PourSource] Poured! Reset to {resetToItem.itemName}");
        Destroy(gameObject);
    }

    private void FindOwner()
    {
        SimplePlayerController[] players = FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.heldItem == gameObject)
            {
                owner = p;
                return;
            }
        }
    }

    private bool CheckActionPressed(SimplePlayerController player)
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.qKey.wasPressedThisFrame;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.numpad0Key.wasPressedThisFrame;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.leftTrigger.wasPressedThisFrame;
        return false;
    }
}