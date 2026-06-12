using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePlayerController : MonoBehaviour
{
    public int playerIndex = 0; // 0 for Player 1, 1 for Player 2
    public float moveSpeed = 5f;
    public float pickupRange = 2f;
    public Transform holdPoint;
    
    private PickupObject heldObject = null;

    private void Start()
    {
        // Create an empty game object as the hold point
        GameObject hp = new GameObject("HoldPoint");
        hp.transform.SetParent(this.transform);
        hp.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
        holdPoint = hp.transform;
    }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        bool interactPressed = false;

        if (playerIndex == 0)
        {
            // Player 1: WASD and E for Interact
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
                if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
                if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
                if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
                
                if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
            }

            // Player 1: Gamepad 1 and South Button for Interact
            if (Gamepad.all.Count > 0)
            {
                Vector2 gamepadInput = Gamepad.all[0].leftStick.ReadValue();
                if (gamepadInput.sqrMagnitude > 0.01f)
                {
                    moveInput = gamepadInput;
                }
                
                if (Gamepad.all[0].buttonSouth.wasPressedThisFrame) interactPressed = true;
            }
        }
        else if (playerIndex == 1)
        {
            // Player 2: Arrows and Right Shift for Interact
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
                if (Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
                if (Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
                if (Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;
                
                if (Keyboard.current.rightShiftKey.wasPressedThisFrame) interactPressed = true;
            }

            // Player 2: Gamepad 2 and South Button for Interact
            if (Gamepad.all.Count > 1)
            {
                Vector2 gamepadInput = Gamepad.all[1].leftStick.ReadValue();
                if (gamepadInput.sqrMagnitude > 0.01f)
                {
                    moveInput = gamepadInput;
                }
                
                if (Gamepad.all[1].buttonSouth.wasPressedThisFrame) interactPressed = true;
            }
        }

        // Normalize to prevent faster diagonal movement for keyboard
        if (moveInput.sqrMagnitude > 1)
        {
            moveInput.Normalize();
        }

        // Apply movement
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        // Handle Interact (Pick up / Drop)
        if (interactPressed)
        {
            if (heldObject != null)
            {
                DropObject();
            }
            else
            {
                TryPickupObject();
            }
        }
    }
    
    private void TryPickupObject()
    {
        PickupObject[] allPickups = FindObjectsByType<PickupObject>(FindObjectsSortMode.None);
        PickupObject closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var pickup in allPickups)
        {
            // Don't pick up objects currently held by someone else
            if (pickup.transform.parent != null && pickup.transform.parent.GetComponentInChildren<SimplePlayerController>() != null)
                continue;
                
            float dist = Vector3.Distance(transform.position, pickup.transform.position);
            if (dist < pickupRange && dist < closestDist)
            {
                closestDist = dist;
                closest = pickup;
            }
        }
        
        if (closest != null)
        {
            heldObject = closest;
            heldObject.OnPickedUp(holdPoint);
        }
    }
    
    private void DropObject()
    {
        if (heldObject != null)
        {
            heldObject.OnDropped();
            heldObject = null;
        }
    }
}