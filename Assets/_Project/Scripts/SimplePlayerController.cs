using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerController : MonoBehaviour, IInteractable
{
    public static int activeGamepadPlayerIndex = 0;

    public int playerIndex = 0; 
    public float moveSpeed = 5f;
    
    [Header("Boundaries")]
    public bool useBoundaries = true;
    public Vector2 minBounds = new Vector2(-5.5f, -9.5f);
    public Vector2 maxBounds = new Vector2(10.5f, 2.5f);

    [Header("Interaction")]
    public Transform holdPoint;
    [HideInInspector] public GameObject heldItem;

    [Header("Movement & Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float dashTimer = 0f;
    private float currentDashCooldown = 0f;
    private bool isDashing = false;

    [Header("Controller Input")]
    public InputAction gamepadMoveAction = new InputAction("GamepadMove", binding: "<Gamepad>/leftStick", expectedControlType: "Vector2");
    public InputAction gamepadInteractAction = new InputAction("GamepadInteract", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");
    public InputAction gamepadProcessAction = new InputAction("GamepadProcess", type: InputActionType.Button, binding: "<Gamepad>/buttonWest");
    public InputAction gamepadDashAction = new InputAction("GamepadDash", type: InputActionType.Button, binding: "<Gamepad>/buttonEast");
    
    [Tooltip("Check this if pulling down on the joystick moves the character up")]
    public bool invertGamepadY = true;

    [Header("Status Effects")]
    public bool isFrozen = false;
    public float freezeTimer = 0f;
    public TMPro.TextMeshPro freezeLabel; // Assign in inspector or code

    private Rigidbody rb;
    private Vector2 currentMoveInput;

    private static System.Collections.Generic.List<InputDevice> cachedControllers = new System.Collections.Generic.List<InputDevice>();
    private static bool controllersDirty = true;
    private static bool isSubscribedToEvents = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (freezeLabel != null) freezeLabel.gameObject.SetActive(false);

        // Add generic joystick fallbacks
        gamepadMoveAction.AddBinding("<Joystick>/stick");
        
        // South = A/Cross (Pick Up)
        gamepadInteractAction.AddBinding("<Joystick>/button0"); 
        gamepadInteractAction.AddBinding("<Joystick>/trigger"); // Generic South/A
        gamepadInteractAction.AddBinding("<HID::*>/button0");

        // West = X/Square (Process)
        gamepadProcessAction.AddBinding("<Joystick>/button2");
        gamepadProcessAction.AddBinding("<HID::*>/button2");

        // East = B/Circle (Dash)
        gamepadDashAction.AddBinding("<Joystick>/button1");
        gamepadDashAction.AddBinding("<HID::*>/button1");

        if (!isSubscribedToEvents)
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            isSubscribedToEvents = true;
        }
    }

    private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
        {
            controllersDirty = true;
        }
    }

    private void OnEnable()
    {
        gamepadMoveAction.Enable();
        gamepadInteractAction.Enable();
        gamepadProcessAction.Enable();
        gamepadDashAction.Enable();
    }

    private void OnDisable()
    {
        gamepadMoveAction.Disable();
        gamepadInteractAction.Disable();
        gamepadProcessAction.Disable();
        gamepadDashAction.Disable();
    }

    private void Start()
    {
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(this.transform);
            hp.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            holdPoint = hp.transform;
        }
    }

    public bool CanInteract()
    {
        // other players can interact with me if I am holding something
        return heldItem != null;
    }

    public void Interact(SimplePlayerController interactingPlayer)
    {
        if (heldItem != null && interactingPlayer.heldItem == null)
        {
            // transfer the item to the other player
            GameObject itemToTransfer = heldItem;
            heldItem = null;

            PickupObject pickup = itemToTransfer.GetComponent<PickupObject>();
            if (pickup != null)
            {
                // we use the existing pickup logic to attach it to the new player
                pickup.Interact(interactingPlayer);
            }
        }
    }

    private void Update()
    {
        if (isFrozen)
        {
            freezeTimer -= Time.deltaTime;
            if (freezeLabel != null)
            {
                freezeLabel.text = Mathf.CeilToInt(freezeTimer).ToString();
                
                // BILLBOARDING: Always face the camera
                if (Camera.main != null)
                {
                    freezeLabel.transform.rotation = Camera.main.transform.rotation;
                }
            }

            if (freezeTimer <= 0)
            {
                Unfreeze();
            }
            return; // Block all input/movement while frozen
        }

        // --- Dash Logic ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) isDashing = false;
        }
        if (currentDashCooldown > 0) currentDashCooldown -= Time.deltaTime;

        // Allocation-free controller caching
        if (controllersDirty)
        {
            cachedControllers.Clear();
            foreach (var device in InputSystem.devices)
            {
                if (device is Gamepad || device is Joystick) cachedControllers.Add(device);
            }
            controllersDirty = false;
        }

        int controllerCount = cachedControllers.Count;
        bool isDualControllerMode = (controllerCount >= 2);

        Vector2 moveInput = Vector2.zero;
        bool interactPressed = false;
        bool processPressed = false;
        bool dashPressed = false;

        // Keyboard inputs always work for their respective players
        if (playerIndex == 0)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
                
                if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
                if (Keyboard.current.rKey.isPressed) processPressed = true;
                if (Keyboard.current.spaceKey.wasPressedThisFrame) dashPressed = true;
            }
        }
        else if (playerIndex == 1)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
                if (Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
                if (Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
                if (Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;
                
                if (Keyboard.current.rightShiftKey.wasPressedThisFrame) interactPressed = true;
                if (Keyboard.current.rightCtrlKey.isPressed || Keyboard.current.rightCommandKey.isPressed) processPressed = true;
                
                // Slash / Minus key for P2 Dash
                if (Keyboard.current.slashKey.wasPressedThisFrame || Keyboard.current.minusKey.wasPressedThisFrame) dashPressed = true;
            }
        }

        // --- Gamepad / Joystick Logic ---
        Vector2 gamepadInput = Vector2.zero;
        bool gamepadInteract = false;
        bool gamepadProcess = false;
        bool gamepadDash = false;

        if (controllerCount > 0)
        {
            InputDevice myDevice = null;
            if (isDualControllerMode)
            {
                if (playerIndex < controllerCount) myDevice = cachedControllers[playerIndex];
            }
            else if (playerIndex == activeGamepadPlayerIndex)
            {
                myDevice = cachedControllers[0];
            }

            if (myDevice != null)
            {
                gamepadInput = GetCorrectedInput(myDevice);

                // Use the shared actions to check for presses on THIS specific device
                if (IsActionPressedOnDevice(gamepadInteractAction, myDevice)) gamepadInteract = true;
                if (IsActionHeldOnDevice(gamepadProcessAction, myDevice)) gamepadProcess = true;
                if (IsActionPressedOnDevice(gamepadDashAction, myDevice)) gamepadDash = true;
            }
        }

        // Apply gamepad input if it exists
        if (gamepadInput.sqrMagnitude > 0.05f)
        {
            if (gamepadInput.sqrMagnitude > moveInput.sqrMagnitude) moveInput = gamepadInput;
        }

        if (gamepadInteract) interactPressed = true;
        if (gamepadProcess) processPressed = true;
        if (gamepadDash) dashPressed = true;
        // ---------------------

        if (moveInput.sqrMagnitude > 1)
        {
            moveInput.Normalize();
        }

        currentMoveInput = moveInput;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Dash Trigger
        if (dashPressed && currentDashCooldown <= 0 && moveInput.sqrMagnitude > 0.1f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            currentDashCooldown = dashCooldown;
        }

        // fallback translation if no rigidbody
        if (rb == null)
        {
            float speed = isDashing ? dashSpeed : moveSpeed;
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * speed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }

        if (interactPressed)
        {
            TryInteract();
        }

        // Processing (Hold Interaction)
        if (processPressed)
        {
            TryProcess();
        }
    }

    private bool IsActionPressedOnDevice(InputAction action, InputDevice device)
    {
        if (!action.WasPressedThisFrame()) return false;
        
        // If multiple devices are mapped, we must check if the one that fired matches
        var control = action.activeControl;
        return control != null && control.device == device;
    }

    private bool IsActionHeldOnDevice(InputAction action, InputDevice device)
    {
        if (!action.IsPressed()) return false;
        
        // IsPressed is state-based, so we check if any control on the target device is currently down
        foreach (var control in action.controls)
        {
            if (control.device == device && control is UnityEngine.InputSystem.Controls.ButtonControl button)
            {
                if (button.isPressed) return true;
            }
        }
        return false;
    }

    private void TryProcess()
    {
        // Many stations check if the player is "holding interact"
        // This method can be called or the processPressed bool can be checked by stations.
        // For now, we reuse the logic where stations check player interaction state.
    }

    public bool IsProcessing() 
    {
        // Simple helper for stations to check if this player is pressing the 'Process' key
        if (playerIndex == 0 && Keyboard.current != null && Keyboard.current.rKey.isPressed) return true;
        if (playerIndex == 1 && Keyboard.current != null && (Keyboard.current.rightCtrlKey.isPressed || Keyboard.current.rightCommandKey.isPressed)) return true;

        // Gamepad check
        int controllerCount = cachedControllers.Count;
        if (controllerCount > 0)
        {
            InputDevice myDevice = null;
            if (controllerCount >= 2)
            {
                if (playerIndex < controllerCount) myDevice = cachedControllers[playerIndex];
            }
            else if (playerIndex == activeGamepadPlayerIndex)
            {
                myDevice = cachedControllers[0];
            }

            if (myDevice != null) return IsActionHeldOnDevice(gamepadProcessAction, myDevice);
        }

        return false;
    }

    private Vector2 GetCorrectedInput(InputDevice device)
    {
        Vector2 raw = Vector2.zero;
        if (device is Gamepad g) raw = g.leftStick.ReadValue();
        else if (device is Joystick j) raw = j.stick.ReadValue();

        float yMult = invertGamepadY ? -1f : 1f;

        // --- Mac Wireless Xbox Specific Fix ---
        // On Mac, Bluetooth Xbox controllers often report Y-axis inverted compared to wired ones
        string dName = device.name.ToLower();
        string dProduct = (device.description.product != null) ? device.description.product.ToLower() : "";
        
        if (dName.Contains("xbox") && (dName.Contains("wireless") || dProduct.Contains("wireless") || dName.Contains("bluetooth")))
        {
            yMult *= -1f; // Flip the inversion just for this device
        }

        return new Vector2(raw.x, raw.y * yMult);
    }

    private void LateUpdate()
    {
        if (useBoundaries)
        {
            float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
            float clampedZ = Mathf.Clamp(transform.position.z, minBounds.y, maxBounds.y);
            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            float speed = isDashing ? dashSpeed : moveSpeed;
            Vector3 targetVelocity = new Vector3(currentMoveInput.x, 0f, currentMoveInput.y) * speed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
    }

    private void TryInteract()
    {
        Vector3 pointInFront = transform.position + transform.forward;
        
        // snap to the center of the 1x1 grid tile
        float snappedX = Mathf.Round(pointInFront.x);
        float snappedZ = Mathf.Round(pointInFront.z);
        float snappedY = 0.5f; 
        
        Vector3 snappedTileCenter = new Vector3(snappedX, snappedY, snappedZ);
        // check from y=-0.1 to y=1.1 to hit floor items, players, and counters
        Vector3 halfExtents = new Vector3(0.4f, 0.6f, 0.4f);
        
        Collider[] hitColliders = Physics.OverlapBox(snappedTileCenter, halfExtents);

        bool interacted = false;

        // prioritize interacting with something in the world (including other players)
        foreach (Collider hit in hitColliders)
        {
            // Don't interact with yourself!
            if (hit.gameObject == this.gameObject) continue;

            if (hit.TryGetComponent<IInteractable>(out var interactable))
            {
                if (interactable.CanInteract())
                {
                    // If we are already holding something, we can only interact with certain things
                    // (like a counter to swap/place). But for stealing, the victim's Interact
                    // handles checking if the thief's hands are empty.
                    interactable.Interact(this);
                    interacted = true;
                    break;
                }
            }
        }

        // drop item on the floor if hitting empty space and we didn't interact
        if (!interacted && heldItem != null)
        {
            PickupObject pickup = heldItem.GetComponent<PickupObject>();
            if (pickup != null)
            {
                // Clamp drop position to stay within the ground area
                float dX = Mathf.Clamp(snappedTileCenter.x, minBounds.x, maxBounds.x);
                float dZ = Mathf.Clamp(snappedTileCenter.z, minBounds.y, maxBounds.y);
                Vector3 dropPos = new Vector3(dX, 0.5f, dZ);

                pickup.Drop(dropPos);
                heldItem = null;
            }
        }
    }

    public void Freeze(float duration)
    {
        if (isFrozen) return; // Don't re-freeze if already frozen
        
        isFrozen = true;
        freezeTimer = duration;
        currentMoveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (freezeLabel != null)
        {
            freezeLabel.gameObject.SetActive(true);
            freezeLabel.text = Mathf.CeilToInt(duration).ToString();
        }
    }

    private void Unfreeze()
    {
        isFrozen = false;
        if (freezeLabel != null) freezeLabel.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pointInFront = transform.position + transform.forward;
        float snappedX = Mathf.Round(pointInFront.x);
        float snappedZ = Mathf.Round(pointInFront.z);
        Vector3 snappedTileCenter = new Vector3(snappedX, 0.5f, snappedZ);
        Gizmos.DrawWireCube(snappedTileCenter, new Vector3(0.8f, 1.2f, 0.8f));
    }
}
