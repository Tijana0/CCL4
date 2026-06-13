using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerController : MonoBehaviour, IInteractable
{
    public static int activeGamepadPlayerIndex = 0;

    public int playerIndex = 0; 
    public float moveSpeed = 5f;
    
    [Header("Interaction")]
    public Transform holdPoint;
    public GameObject selectionIndicator;
    [HideInInspector] public GameObject heldItem;

    private Rigidbody rb;
    private Vector2 currentMoveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        // Ensure indicator matches initial state
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(playerIndex == activeGamepadPlayerIndex);
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
        // Update indicator visibility based on static selection in GameManager
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(playerIndex == activeGamepadPlayerIndex);
        }

        Vector2 moveInput = Vector2.zero;
        bool interactPressed = false;

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
            }
        }

        // Gamepad only works for the active player
        if (Gamepad.current != null && playerIndex == activeGamepadPlayerIndex)
        {
            Vector2 gamepadInput = Gamepad.current.leftStick.ReadValue();
            // prevent deadzone drift from overriding keyboard
            if (gamepadInput.sqrMagnitude > moveInput.sqrMagnitude && gamepadInput.sqrMagnitude > 0.05f)
            {
                moveInput = gamepadInput;
            }
            
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) interactPressed = true;
        }

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

        // fallback translation if no rigidbody
        if (rb == null)
        {
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }

        if (interactPressed)
        {
            TryInteract();
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 targetVelocity = new Vector3(currentMoveInput.x, 0f, currentMoveInput.y) * moveSpeed;
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
                Vector3 dropPos = new Vector3(snappedTileCenter.x, 0.5f, snappedTileCenter.z);
                pickup.Drop(dropPos);
                heldItem = null;
            }
        }
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