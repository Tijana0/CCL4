using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this to any item prefab that has a special action when used.
/// The player presses the action button (Q / Numpad0 / Left Trigger)
/// while holding the item to trigger it.
///
/// Two use modes:
///   TransformToItem — item becomes a different ItemData (e.g. FullBucket → EmptyBucket after spill)
///   VisualOnly      — plays an effect but item stays the same (e.g. wand sparks)
///
/// Examples:
///   FullBucket:  UseMode = TransformToItem, outputItem = EmptyBucket
///   EmptyBucket: no UsableItem needed (just a regular item)
///   Wand:        handled by WandController instead (always-available tool)
/// </summary>
public class UsableItem : MonoBehaviour
{
    public enum UseMode { TransformToItem, VisualOnly }

    [Header("Use Settings")]
    public UseMode useMode = UseMode.TransformToItem;

    [Header("TransformToItem Settings")]
    [Tooltip("What this item becomes after being used. E.g. FullBucket becomes EmptyBucket.")]
    public ItemData outputItem;

    [Header("VisualOnly Settings")]
    [Tooltip("Particle effect or GameObject to activate briefly when used.")]
    public GameObject useEffect;
    [Tooltip("How long the effect plays before being hidden.")]
    public float effectDuration = 0.5f;

    [Header("Hold to Use")]
    [Tooltip("If true, player must hold the action button. If false, single press.")]
    public bool holdToUse = false;
    [Tooltip("Seconds to hold if holdToUse is true.")]
    public float holdDuration = 1f;

    private float holdProgress = 0f;
    private bool isHolding = false;
    private SimplePlayerController owner = null;

    private void Update()
    {
        // Find owner — the player currently holding this item
        if (owner == null)
        {
            FindOwner();
            return;
        }

        // Verify still held
        if (owner.heldItem != gameObject)
        {
            owner = null;
            isHolding = false;
            holdProgress = 0f;
            return;
        }

        bool actionPressed = CheckActionButton(owner);

        if (holdToUse)
        {
            if (actionPressed)
            {
                holdProgress += Time.deltaTime / holdDuration;
                isHolding = true;
                if (holdProgress >= 1f)
                {
                    holdProgress = 0f;
                    TriggerUse();
                }
            }
            else
            {
                holdProgress = 0f;
                isHolding = false;
            }
        }
        else
        {
            if (CheckActionButtonPressed(owner))
                TriggerUse();
        }
    }

    private void TriggerUse()
    {
        if (owner == null) return;

        switch (useMode)
        {
            case UseMode.TransformToItem:
                TransformItem();
                break;
            case UseMode.VisualOnly:
                PlayEffect();
                break;
        }
    }

    private void TransformItem()
    {
        if (outputItem == null || outputItem.prefab == null)
        {
            Debug.LogWarning("[UsableItem] No output item assigned.");
            return;
        }

        // Spawn the new item at same position
        GameObject newItem = Instantiate(outputItem.prefab, owner.holdPoint.position, Quaternion.identity, owner.holdPoint);

        WorldItem wi = newItem.GetComponent<WorldItem>();
        if (wi == null) wi = newItem.AddComponent<WorldItem>();
        wi.itemData = outputItem;

        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;

        Rigidbody rb = newItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = newItem.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        owner.heldItem = newItem;

        Debug.Log($"[UsableItem] {gameObject.name} transformed into {outputItem.itemName}");
        Destroy(gameObject);
    }

    private void PlayEffect()
    {
        if (useEffect != null)
            StartCoroutine(ShowEffect());

        Debug.Log($"[UsableItem] {gameObject.name} used (visual only)");
    }

    private System.Collections.IEnumerator ShowEffect()
    {
        useEffect.SetActive(true);
        yield return new WaitForSeconds(effectDuration);
        if (useEffect != null) useEffect.SetActive(false);
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

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>F / Enter / Right Trigger — action button held</summary>
    private bool CheckActionButton(SimplePlayerController player)
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.fKey.isPressed;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.enterKey.isPressed;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.rightTrigger.isPressed;
        return false;
    }

    /// <summary>F / Enter / Right Trigger — action button single press</summary>
    private bool CheckActionButtonPressed(SimplePlayerController player)
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.fKey.wasPressedThisFrame;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.enterKey.wasPressedThisFrame;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.rightTrigger.wasPressedThisFrame;
        return false;
    }
}