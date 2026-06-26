using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SimplePlayerController))]
public class PlayerAnimatorDriver : MonoBehaviour
{
    [Tooltip("Animator on the character model. Auto-found in children if left empty.")]
    public Animator animator;

    [Tooltip("Move-input magnitude above which the walk animation plays.")]
    public float moveThreshold = 0.1f;

    [Header("Casting visuals")]
    public GameObject wandModel;
    public GameObject glowEffect;
    [Tooltip("Seconds to wait after casting starts before the glow appears.")]
    public float glowDelay = 0.5f;

    private SimplePlayerController controller;
    private bool hasMovement, hasWalking, hasPickUp, hasCasting;
    private bool wasCasting = false;
    private Coroutine glowRoutine;

    private void Awake()
    {
        controller = GetComponent<SimplePlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (wandModel != null) wandModel.SetActive(false);
        if (glowEffect != null) glowEffect.SetActive(false);

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

        if (casting != wasCasting)
        {
            if (casting)
            {
                // wand appears immediately
                if (wandModel != null) wandModel.SetActive(true);
                // glow appears after a delay
                if (glowEffect != null)
                    glowRoutine = StartCoroutine(ShowGlowAfterDelay());
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayWand(this.gameObject);
            }
            else
            {
                // casting stopped — cancel pending glow and hide everything
                if (glowRoutine != null) { StopCoroutine(glowRoutine); glowRoutine = null; }
                if (wandModel != null) wandModel.SetActive(false);
                if (glowEffect != null) glowEffect.SetActive(false);
            }
            wasCasting = casting;
        }
    }

    private IEnumerator ShowGlowAfterDelay()
    {
        yield return new WaitForSeconds(glowDelay);
        if (glowEffect != null) glowEffect.SetActive(true);
        glowRoutine = null;
    }
}