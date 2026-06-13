using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerController : MonoBehaviour
{
    public int playerIndex = 0; 
    public float moveSpeed = 5f;
    
    [Header("Interaction")]
    public Transform holdPoint;
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
    }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        bool interactPressed = false;

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

            if (Gamepad.all.Count > 0)
            {
                Vector2 gamepadInput = Gamepad.all[0].leftStick.ReadValue();
                // prevent deadzone drift from overriding keyboard
                if (gamepadInput.sqrMagnitude > moveInput.sqrMagnitude && gamepadInput.sqrMagnitude > 0.05f)
                {
                    moveInput = gamepadInput;
                }
                
                if (Gamepad.all[0].buttonSouth.wasPressedThisFrame) interactPressed = true;
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

            if (Gamepad.all.Count > 1)
            {
                Vector2 gamepadInput = Gamepad.all[1].leftStick.ReadValue();
                if (gamepadInput.sqrMagnitude > moveInput.sqrMagnitude && gamepadInput.sqrMagnitude > 0.05f)
                {
                    moveInput = gamepadInput;
                }
                
                if (Gamepad.all[1].buttonSouth.wasPressedThisFrame) interactPressed = true;
            }
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
        // check from y=-0.1 to y=1.1 to hit floor items and counters
        Vector3 halfExtents = new Vector3(0.4f, 0.6f, 0.4f);
        
        Collider[] hitColliders = Physics.OverlapBox(snappedTileCenter, halfExtents);

        bool interacted = false;
        foreach (Collider hit in hitColliders)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactable))
            {
                if (interactable.CanInteract())
                {
                    interactable.Interact(this);
                    interacted = true;
                    break;
                }
            }
        }

        // drop item on the floor if hitting empty space
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