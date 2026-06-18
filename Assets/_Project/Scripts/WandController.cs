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
            
        // Gamepad logic
        InputDevice myDevice = GetMyDevice();
        if (myDevice != null)
        {
            // 1. Try standard RT
            if (myDevice is Gamepad g && g.rightTrigger.isPressed) return true;
            
            // 2. Try by index (Right Trigger is usually button 5 or 7 on HID)
            if (myDevice is Joystick j)
            {
                if (CheckJoystickButton(j, 5, true)) return true;
                if (CheckJoystickButton(j, 7, true)) return true;
            }
            
            // 3. Search by name
            foreach(var control in myDevice.allControls)
            {
                if (control.name.ToLower().Contains("righttrigger") || control.name.ToLower().Contains("rt"))
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl b && b.isPressed) return true;
                }
            }
        }
        return false;
    }

    private bool CheckActionPressed()
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.fKey.wasPressedThisFrame;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.enterKey.wasPressedThisFrame;

        // Gamepad logic
        InputDevice myDevice = GetMyDevice();
        if (myDevice != null)
        {
            if (myDevice is Gamepad g && g.rightTrigger.wasPressedThisFrame) return true;
            
            if (myDevice is Joystick j)
            {
                if (CheckJoystickButton(j, 5, false)) return true;
                if (CheckJoystickButton(j, 7, false)) return true;
            }
            
            foreach(var control in myDevice.allControls)
            {
                if (control.name.ToLower().Contains("righttrigger") || control.name.ToLower().Contains("rt"))
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) return true;
                }
            }
        }
        return false;
    }

    private InputDevice GetMyDevice()
    {
        // Allocation-free device check using System.Linq or similar would be nice but let's be direct
        var controllers = new System.Collections.Generic.List<InputDevice>();
        foreach (var device in InputSystem.devices)
            if (device is Gamepad || device is Joystick) controllers.Add(device);

        if (controllers.Count >= 2)
        {
            if (player.playerIndex < controllers.Count) return controllers[player.playerIndex];
        }
        else if (controllers.Count == 1 && player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex)
        {
            return controllers[0];
        }
        return null;
    }

    private bool CheckJoystickButton(Joystick j, int index, bool hold)
    {
        if (j.allControls.Count > index && j.allControls[index] is UnityEngine.InputSystem.Controls.ButtonControl b)
            return hold ? b.isPressed : b.wasPressedThisFrame;
        return false;
    }
}