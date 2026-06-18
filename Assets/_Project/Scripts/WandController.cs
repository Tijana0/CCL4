using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Add this to each Player GameObject (alongside SimplePlayerController).
/// The wand is always available — it's a child object of the player,
/// never picked up like a normal item.
///
/// Pressing the action button (Q / Numpad0 / Left Trigger) near a valid
/// target triggers the wand spell on it.
///
/// To make something wand-targetable, add a WandTarget component to it.
///
/// Setup:
///   1. Add WandController to your Player GameObject
///   2. Create a wand mesh as a child of the player, assign it to wandVisual
///   3. Add WandTarget to any station or item that can be spell-affected
/// </summary>
public class WandController : MonoBehaviour
{
    [Header("Wand Settings")]
    [Tooltip("The wand mesh child object — shown/hidden based on context")]
    public GameObject wandVisual;
    [Tooltip("How far the wand can reach to hit a target")]
    public float wandRange = 1.5f;
    [Tooltip("Hold action button for this long to cast. 0 = instant press.")]
    public float castHoldTime = 0f;

    private SimplePlayerController player;
    private float holdProgress = 0f;

    private void Awake()
    {
        player = GetComponent<SimplePlayerController>();
    }

    private void Update()
    {
        // Wand is hidden when player is holding an item
        if (wandVisual != null)
            wandVisual.SetActive(player.heldItem == null);

        // Don't cast while holding an item
        if (player.heldItem != null)
        {
            holdProgress = 0f;
            return;
        }

        bool actionHeld = CheckActionHeld();

        if (castHoldTime <= 0f)
        {
            // Instant press
            if (CheckActionPressed())
                TryCast();
        }
        else
        {
            // Hold to cast
            if (actionHeld)
            {
                holdProgress += Time.deltaTime / castHoldTime;
                if (holdProgress >= 1f)
                {
                    holdProgress = 0f;
                    TryCast();
                }
            }
            else
            {
                holdProgress = 0f;
            }
        }
    }

    private void TryCast()
    {
        // Look for a WandTarget in front of the player
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin + transform.forward * wandRange * 0.5f, wandRange * 0.5f);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            WandTarget target = hit.GetComponent<WandTarget>();
            if (target == null)
                target = hit.GetComponentInParent<WandTarget>();

            if (target != null)
            {
                target.OnWandHit(player);
                Debug.Log($"[WandController] Cast spell on {hit.gameObject.name}");
                return;
            }
        }

        Debug.Log("[WandController] No valid wand target in range.");
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private bool CheckActionHeld()
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.fKey.isPressed;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.enterKey.isPressed;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.rightTrigger.isPressed;
        return false;
    }

    private bool CheckActionPressed()
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