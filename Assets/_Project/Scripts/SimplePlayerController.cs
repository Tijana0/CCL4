using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SimplePlayerController : MonoBehaviour, IInteractable
{
    public static int activeGamepadPlayerIndex = 0;

    public int playerIndex = 0; 
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    
    [Header("Boundaries")]
    public bool useBoundaries = true;
    public Vector2 minBounds = new Vector2(21f, -17f);
    public Vector2 maxBounds = new Vector2(44f, -2f);

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

    /// <summary>Current movement input this frame (read-only). Used by PlayerAnimatorDriver.</summary>
    public Vector2 MoveInput => currentMoveInput;
    public bool IsDashing => isDashing;

    private static System.Collections.Generic.List<InputDevice> cachedControllers = new System.Collections.Generic.List<InputDevice>();
    private static bool controllersDirty = true;
    private static bool isSubscribedToEvents = false;

    // Constant button-alias sets, hoisted out of Update to avoid per-frame array allocations.
    private static readonly string[] InteractButtonAliases = { "buttonSouth", "button0", "a", "cross", "trigger" };
    private static readonly string[] ProcessButtonAliases = { "buttonWest", "button3", "x", "square", "button13" };
    private static readonly string[] DashButtonAliases = { "buttonEast", "button2", "b", "circle", "button12" };

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
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
        UpdateBoundariesFromRoomBase();
    }

    public void UpdateBoundariesFromRoomBase()
    {
        GameObject roomBase = GameObject.Find("room_base");
        if (roomBase != null)
        {
            Collider col = roomBase.GetComponent<Collider>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                // Walls take up 0.5m on Left (min X), Right (max X), and Back (max Z)
                // Front (min Z) has no wall.
                float wallThickness = 0.5f;
                float playerRadius = 0.5f;

                float leftWallX = bounds.min.x + wallThickness;
                float rightWallX = bounds.max.x - wallThickness;
                float backWallZ = bounds.max.z - wallThickness;
                float frontEdgeZ = bounds.min.z; // no wall!

                // Clamp player positions inside walls, accounting for player radius
                minBounds = new Vector2(leftWallX + playerRadius, frontEdgeZ + playerRadius);
                maxBounds = new Vector2(rightWallX - playerRadius, backWallZ - playerRadius);
            }
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

        PollInput(out Vector2 moveInput, out bool interactPressed, out bool processPressed, out bool dashPressed);

        if (moveInput.sqrMagnitude > 1)
        {
            moveInput.Normalize();
        }

        currentMoveInput = moveInput;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 lookDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Dash Trigger
        if (dashPressed && currentDashCooldown <= 0)
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

    // Reads keyboard + gamepad/joystick state for this player. Extracted from Update
    // to keep the per-frame movement loop readable.
    private void PollInput(out Vector2 moveInput, out bool interactPressed, out bool processPressed, out bool dashPressed)
    {
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

        moveInput = Vector2.zero;
        interactPressed = false;
        processPressed = false;
        dashPressed = false;

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

                if (myDevice is Gamepad g)
                {
                    if (g.buttonSouth.wasPressedThisFrame) gamepadInteract = true;
                    if (g.buttonWest.isPressed) gamepadProcess = true;
                    if (g.buttonEast.wasPressedThisFrame) gamepadDash = true;
                }

                // --- UNIVERSAL MAPPING (Works for Joystick/HID/Third-party) ---
                // Based on exact Mac Diagnostic Hardware Logs:
                // A (Interact) = trigger / button0
                // B (Dash) = button2
                // X (Process) = button3 (Assuming standard HID layout since B=2)
                gamepadInteract |= CheckButton(myDevice, InteractButtonAliases);
                gamepadProcess |= CheckButton(myDevice, ProcessButtonAliases, true);
                gamepadDash |= CheckButton(myDevice, DashButtonAliases);
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
    }

    private void TryProcess()
    {
        Vector3 interactionCenter = transform.position + transform.forward * 0.6f;
        interactionCenter.y = 0.5f;
        Vector3 halfExtents = new Vector3(0.6f, 0.5f, 0.6f);
        
        Collider[] hitColliders = Physics.OverlapBox(interactionCenter, halfExtents, transform.rotation);

        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject == this.gameObject) continue;

            if (hit.TryGetComponent<ProcessingStation>(out var pStation))
            {
                pStation.StartProcessingIfValid(this);
                break;
            }
            else if (hit.TryGetComponent<MultiIngredientStation>(out var mStation))
            {
                mStation.StartCookingIfValid(this);
                break;
            }
        }
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

            if (myDevice != null) 
            {
                return CheckButton(myDevice, new string[] { "buttonWest", "button3", "x", "square", "button13" }, true);
            }
        }

        return false;
    }

    private bool CheckButton(InputDevice device, string[] aliases, bool hold = false)
    {
        bool isDown = false;
        foreach (var control in device.allControls)
        {
            if (control is UnityEngine.InputSystem.Controls.ButtonControl b)
            {
                bool state = hold ? b.isPressed : b.wasPressedThisFrame;
                if (state)
                {
                    string cName = control.name.ToLower();
                    foreach (var alias in aliases)
                    {
                        // MUST be exact match to prevent "b" from matching "rightBumper"
                        // or "button" from matching "startButton"
                        if (cName == alias.ToLower()) isDown = true;
                    }
                }
            }
        }
        return isDown;
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
        if (useBoundaries && rb != null)
        {
            // Use rb.position instead of transform.position to keep the physics
            // engine in sync — setting transform.position directly on a Rigidbody
            // desyncs its internal state and breaks collision detection.
            Vector3 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.z = Mathf.Clamp(pos.z, minBounds.y, maxBounds.y);
            rb.position = pos;
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
        // If holding an item with PourSource, and there is a PourTarget in range,
        // prioritize pouring over normal interaction/dropping!
        if (heldItem != null)
        {
            PourSource pourSource = heldItem.GetComponent<PourSource>();
            if (pourSource != null && pourSource.CanPour())
            {
                pourSource.ExecutePour(this);
                return;
            }
        }

        // Instead of strict grid snapping, use a hitbox directly in front of the player
        Vector3 interactionCenter = transform.position + transform.forward * 0.6f;
        interactionCenter.y = 0.5f; // Keep it low to hit floor items
        
        // Expanded hitbox (1.2m wide, 1.2m deep, 1m tall)
        Vector3 halfExtents = new Vector3(0.6f, 0.5f, 0.6f);
        
        Collider[] hitColliders = Physics.OverlapBox(interactionCenter, halfExtents, transform.rotation);

        // Find all valid interactable targets and assign priority/distance
        List<InteractableTarget> targets = new List<InteractableTarget>();
        foreach (Collider hit in hitColliders)
        {
            // Don't interact with yourself!
            if (hit.gameObject == this.gameObject) continue;

            // Prefer a PortableCookerInteraction (teapot) over its sibling PickupObject, so that
            // holding an item adds an ingredient instead of hitting the no-op pickup path.
            IInteractable interactable = hit.GetComponent<PortableCookerInteraction>();
            if (interactable == null) hit.TryGetComponent<IInteractable>(out interactable);
            if (interactable != null && interactable.CanInteract())
            {
                int priority = 1; // Default priority (pickups, players)
                if (interactable is StationBase station)
                {
                    if (station.stationType == StationType.Counter)
                        priority = 0; // Generic counter gets lowest priority
                    else if (station is ItemContainerStation dispenser
                             && dispenser.extractionMode != ItemContainerStation.ExtractionMode.RequiresItem)
                        priority = 1; // Empty-hand dispensers don't outrank a nearby pickup — closest wins
                                      // (so a crystal dispenser next to a respawning mug doesn't steal it)
                    else
                        priority = 2; // Specialized stations get highest priority
                }

                float distance = Vector3.Distance(interactionCenter, hit.bounds.center);
                targets.Add(new InteractableTarget
                {
                    interactable = interactable,
                    priority = priority,
                    distance = distance
                });
            }
        }

        // Sort targets: if there is a significant distance difference, the closer one
        // always wins. Otherwise, if they are at similar distances (within 0.4m), 
        // the higher priority one wins. This prevents distant high-priority stations
        // (like the sink) from stealing interactions from extremely close lower-priority
        // items (like the stove teapot or counter slots).
        targets.Sort((a, b) =>
        {
            float distDiff = Mathf.Abs(a.distance - b.distance);
            if (distDiff > 0.4f)
            {
                return a.distance.CompareTo(b.distance); // Ascending distance
            }
            if (a.priority != b.priority)
                return b.priority.CompareTo(a.priority); // Descending priority
            return a.distance.CompareTo(b.distance); // Tie-breaker: ascending distance
        });

        bool interacted = false;
        bool holdingCooker = heldItem != null && heldItem.GetComponent<PortableCooker>() != null;

        foreach (var target in targets)
        {
            IInteractable interactable = target.interactable;

            // Can't pick up another item while already holding one. Skip plain pickups so a
            // no-op pickup doesn't swallow the interaction meant for placing the held item.
            if (heldItem != null && interactable is PickupObject)
                continue;

            // Skip empty stations (counters, sinks, bins) if player hands are also empty
            if (interactable is StationBase station)
            {
                bool playerHasItem = heldItem != null;
                bool stationHasItem = station.itemOnStation != null;
                if (!playerHasItem && !stationHasItem && !station.HasReadyResult && !(station is ItemContainerStation))
                {
                    continue;
                }

                // When holding an item, skip dispensers that require empty hands
                // (Instant / HoldToExtract). They can't accept the held item, so they
                // shouldn't "steal" the interaction from a station that can (bin, hand-in,
                // counter, or a RequiresItem dispenser like the sink).
                if (playerHasItem && station is ItemContainerStation ics
                    && ics.extractionMode != ItemContainerStation.ExtractionMode.RequiresItem)
                {
                    continue;
                }

                // A held pot (PortableCooker, e.g. the Teapot) must never be consumed as an
                // ingredient or parked on another station. Only a fill station (the sink) may
                // act on it; everything else is skipped so the pot drops & docks at its stand.
                if (holdingCooker)
                {
                    bool isFillStation = station is ItemContainerStation fics && fics.cookerFillItem != null;
                    if (!isFillStation) continue;
                }
            }

            interactable.Interact(this);
            interacted = true;
            break;
        }

        // drop item on the floor if hitting empty space and we didn't interact
        if (!interacted && heldItem != null)
        {
            PlaceHeldItem(interactionCenter);
        }
    }

    // Places the held item: snaps to a nearby counter or the floor, clamps to
    // room bounds, resolves the drop height, and refuses positions that would
    // clip into stairs/platforms. Extracted from TryInteract.
    private void PlaceHeldItem(Vector3 interactionCenter)
    {
        PickupObject pickup = heldItem.GetComponent<PickupObject>();
        if (pickup == null) return;

        // Use exact unsnapped coordinates (snapping only occurs if placed on a counter/station)
        float dX = interactionCenter.x;
        float dZ = interactionCenter.z;

        // CHECK FOR COUNTERS/DESKS NEARBY TO SNAP TO THEM DIRECTLY!
        Collider[] nearbyColliders = Physics.OverlapSphere(interactionCenter, 0.8f);
        float closestDist = 99f;
        Vector3 targetSnapPos = Vector3.zero;
        bool snappedToCounter = false;

        foreach (var cHit in nearbyColliders)
        {
            if (cHit.gameObject != this.gameObject && cHit.gameObject != heldItem)
            {
                Counter counter = cHit.GetComponent<Counter>();
                if (counter == null)
                {
                    counter = cHit.GetComponentInParent<Counter>();
                }

                if (counter != null)
                {
                    // Block snapping to dispenser slots
                    if (counter.gameObject.name == "counter2_slot_2" || 
                        counter.gameObject.name == "counter2_slot_3" || 
                        counter.gameObject.name == "counter2_slot_4")
                    {
                        continue;
                    }

                    Vector3 counterPos = counter.counterTopPoint != null ? counter.counterTopPoint.position : counter.transform.position;
                    float dist = Vector2.Distance(new Vector2(interactionCenter.x, interactionCenter.z), new Vector2(counterPos.x, counterPos.z));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetSnapPos = counterPos;
                        snappedToCounter = true;
                    }
                }
            }
        }

        if (snappedToCounter)
        {
            dX = targetSnapPos.x;
            dZ = targetSnapPos.z;
        }
        else
        {
            // Dynamically fetch room_base boundaries to prevent dropping over the edge/walls
            GameObject roomBase = GameObject.Find("room_base");
            if (roomBase != null)
            {
                Collider col = roomBase.GetComponent<Collider>();
                if (col != null)
                {
                    Bounds bounds = col.bounds;
                    float wallThickness = 0.5f; // Set to 0.5m

                    // Inner boundaries of the room floor
                    float leftWallX = bounds.min.x + wallThickness;
                    float rightWallX = bounds.max.x - wallThickness;
                    float backWallZ = bounds.max.z - wallThickness;
                    float frontEdgeZ = bounds.min.z; // no wall

                    // Clamp drop centers to stay within inner floor boundaries
                    float minX = leftWallX + 0.3f;
                    float maxX = rightWallX - 0.3f;
                    float minZ = frontEdgeZ + 0.3f;
                    float maxZ = backWallZ - 0.3f;

                    dX = Mathf.Clamp(dX, minX, maxX);
                    dZ = Mathf.Clamp(dZ, minZ, maxZ);
                }
            }
            else
            {
                // Fallback to controller bounds if room_base not found
                dX = Mathf.Clamp(dX, minBounds.x, maxBounds.x);
                dZ = Mathf.Clamp(dZ, minBounds.y, maxBounds.y);
            }
        }

        // Determine drop height dynamically using a Raycast down from above the target position
        float dropY = transform.position.y - 0.5f; // Fallback to estimated foot level
        if (snappedToCounter)
        {
            dropY = targetSnapPos.y;
        }
        else
        {
            Vector3 rayStart = new Vector3(dX, transform.position.y + 1.5f, dZ);
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f);
            float bestY = -99f;
            bool foundFloor = false;
            foreach (var hit in hits)
            {
                if (hit.collider != null && !hit.collider.isTrigger)
                {
                    if (hit.collider.gameObject != this.gameObject &&
                        hit.collider.gameObject != heldItem &&
                        hit.collider.GetComponent<SimplePlayerController>() == null)
                    {
                        // Ignore station colliders so non-snapped items fall to the actual ground
                        if (hit.collider.GetComponent<StationBase>() != null || 
                            hit.collider.GetComponentInParent<StationBase>() != null)
                        {
                            continue;
                        }

                        // We want the highest solid surface below the ray start
                        if (hit.point.y > bestY && hit.point.y <= transform.position.y + 0.5f)
                        {
                            bestY = hit.point.y;
                            foundFloor = true;
                        }
                    }
                }
            }
            if (foundFloor)
            {
                dropY = bestY;
            }
        }

        // Add offset based on the item's collider size to sit exactly on the surface
        float heightOffset = 0.1f;
        BoxCollider itemCol = pickup.GetComponent<BoxCollider>();
        if (itemCol != null)
        {
            heightOffset = itemCol.size.y * 0.5f;
        }
        Vector3 dropPos = new Vector3(dX, dropY + heightOffset, dZ);

        // Prevent placing items so they clip into stairs/platforms.
        // The box is shrunk to 85% so resting ON TOP of a platform is still allowed;
        // only positions that actually overlap the geometry are rejected.
        Vector3 blockHalf = (itemCol != null ? itemCol.size : new Vector3(0.5f, 0.3f, 0.5f)) * 0.5f * 0.85f;
        Collider[] blockHits = Physics.OverlapBox(dropPos, blockHalf, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider bh in blockHits)
        {
            if (bh == null) continue;
            if (bh.gameObject == this.gameObject || bh.gameObject == heldItem) continue;
            if (bh.isTrigger) continue;

            // Block dropping if it overlaps with any station (unless it is a valid snapped placement)
            if (!snappedToCounter && (bh.GetComponent<StationBase>() != null || bh.GetComponentInParent<StationBase>() != null))
            {
                Debug.Log($"[PlaceHeldItem] Drop blocked by station overlap: {bh.gameObject.name}");
                return;
            }

            string bn = bh.gameObject.name.ToLower();
            if (bn.StartsWith("platform") || bn.StartsWith("stairs"))
            {
                // Would overlap a platform/stairs — keep holding instead of dropping.
                return;
            }
        }

        pickup.Drop(dropPos);
        if (snappedToCounter)
        {
            Rigidbody rb = pickup.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
        heldItem = null;
    }

    // Forcibly drops the held item onto the floor at the player's feet, regardless of
    // nearby stations. Used when a station rejects an item (e.g. a non-herb shoved into
    // the Brazier) — the item bounces off and falls to the ground as a physical object.
    public void ForceDropHeldItem()
    {
        if (heldItem == null) return;
        PickupObject pickup = heldItem.GetComponent<PickupObject>();
        if (pickup == null) { heldItem = null; return; }

        Vector3 p = transform.position;
        float dropY = p.y - 0.5f;
        Vector3 rayStart = new Vector3(p.x, p.y + 1.5f, p.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 10f);
        float bestY = -99f; bool foundFloor = false;
        foreach (var hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger &&
                hit.collider.gameObject != this.gameObject &&
                hit.collider.gameObject != heldItem &&
                hit.collider.GetComponent<SimplePlayerController>() == null)
            {
                // Ignore station colliders so non-snapped items fall to the actual ground
                if (hit.collider.GetComponent<StationBase>() != null || 
                    hit.collider.GetComponentInParent<StationBase>() != null)
                {
                    continue;
                }

                if (hit.point.y > bestY && hit.point.y <= p.y + 0.5f) { bestY = hit.point.y; foundFloor = true; }
            }
        }
        if (foundFloor) dropY = bestY;

        float heightOffset = 0.1f;
        BoxCollider itemCol = pickup.GetComponent<BoxCollider>();
        if (itemCol != null) heightOffset = itemCol.size.y * 0.5f;

        pickup.Drop(new Vector3(p.x, dropY + heightOffset, p.z));
        heldItem = null;
    }

    // Forcibly drops the held item onto a custom position, enabling physics
    public void ForceDropHeldItem(Vector3 dropPosition)
    {
        if (heldItem == null) return;
        PickupObject pickup = heldItem.GetComponent<PickupObject>();
        if (pickup == null) { heldItem = null; return; }

        pickup.Drop(dropPosition);
        heldItem = null;
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

    private struct InteractableTarget
    {
        public IInteractable interactable;
        public int priority;
        public float distance;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 interactionCenter = transform.position + transform.forward * 0.6f;
        interactionCenter.y = 0.5f;
        
        Gizmos.matrix = Matrix4x4.TRS(interactionCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.2f, 1f, 1.2f)); // Double the halfExtents
    }
}
