using UnityEngine;

/// <summary>
/// Drives a character model's Animator from the player's gameplay state.
/// Put this on the player root (the object with SimplePlayerController); it finds
/// the Animator on the character model child automatically.
///
/// It maps gameplay state onto the animator parameters that exist on the assigned
/// controller (the Hermione and Luna controllers use slightly different names):
///   - walk bool : "Movement" (Hermione) and/or "Walking" (Luna) -> true while moving
///   - "PickUp"  : true while the player is carrying an item (Walking_Holding pose)
///   - "Casting" : true while the player holds the process/use button (Cast_Spell)
/// Only parameters that actually exist on the controller are set, so neither
/// controller logs "parameter does not exist" warnings.
/// </summary>
[RequireComponent(typeof(SimplePlayerController))]
public class PlayerAnimatorDriver : MonoBehaviour
{
    [Tooltip("Animator on the character model. Auto-found in children if left empty.")]
    public Animator animator;

    [Tooltip("Move-input magnitude above which the walk animation plays.")]
    public float moveThreshold = 0.1f;

    private SimplePlayerController controller;
    private bool hasMovement, hasWalking, hasPickUp, hasCasting;

    private void Awake()
    {
        controller = GetComponent<SimplePlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) return;

        foreach (var p in animator.parameters)
        {
            if (p.name == "Movement") hasMovement = true;
            else if (p.name == "Walking") hasWalking = true;
            else if (p.name == "PickUp") hasPickUp = true;
            else if (p.name == "Casting") hasCasting = true;
        }
    }

    private void Update()
    {
        if (animator == null || controller == null) return;

        bool moving = controller.MoveInput.sqrMagnitude > moveThreshold * moveThreshold;
        bool holding = controller.heldItem != null;
        bool casting = controller.IsProcessing();

        if (hasMovement) animator.SetBool("Movement", moving);
        if (hasWalking) animator.SetBool("Walking", moving);
        if (hasPickUp) animator.SetBool("PickUp", holding);
        if (hasCasting) animator.SetBool("Casting", casting);
    }
}
