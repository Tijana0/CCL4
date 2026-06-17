using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add this to any object that can be affected by a wand spell.
/// When a player casts their wand at this object, OnWandHit() is called.
///
/// Three built-in effects — pick one in the Inspector, or use the
/// UnityEvent to hook up anything custom:
///
///   TransformItem   — transforms the target WorldItem into a different item
///   ToggleObject    — enables/disables a GameObject (e.g. open a door)
///   CustomEvent     — fires a UnityEvent, hook up anything in the Inspector
///
/// Examples:
///   Locked chest:  WandTarget with ToggleObject → unlocks/opens it
///   Ice block:     WandTarget with TransformItem → IceBlock becomes WaterPuddle
///   Boggart:       WandTarget with CustomEvent → triggers Boggart defeat logic
/// </summary>
public class WandTarget : MonoBehaviour
{
    public enum WandEffect { TransformItem, ToggleObject, CustomEvent }

    [Header("Wand Effect")]
    public WandEffect effect = WandEffect.CustomEvent;

    [Header("TransformItem Settings")]
    [Tooltip("What this item becomes when hit by a wand.")]
    public ItemData outputItem;

    [Header("ToggleObject Settings")]
    public GameObject objectToToggle;

    [Header("CustomEvent Settings")]
    public UnityEvent onWandHit;

    [Header("Optional")]
    [Tooltip("Particle or visual to play when hit.")]
    public GameObject hitEffect;
    public float hitEffectDuration = 0.5f;

    public void OnWandHit(SimplePlayerController caster)
    {
        if (hitEffect != null)
            StartCoroutine(ShowHitEffect());

        switch (effect)
        {
            case WandEffect.TransformItem:
                HandleTransform();
                break;
            case WandEffect.ToggleObject:
                HandleToggle();
                break;
            case WandEffect.CustomEvent:
                onWandHit?.Invoke();
                break;
        }
    }

    private void HandleTransform()
    {
        WorldItem wi = GetComponent<WorldItem>();
        if (wi == null) wi = GetComponentInChildren<WorldItem>();

        if (wi == null || outputItem == null || outputItem.prefab == null)
        {
            Debug.LogWarning("[WandTarget] TransformItem: missing WorldItem or outputItem.");
            return;
        }

        Vector3 pos = transform.position;
        Transform parent = transform.parent;

        GameObject newItem = Instantiate(outputItem.prefab, pos, Quaternion.identity, parent);
        WorldItem newWI = newItem.GetComponent<WorldItem>();
        if (newWI == null) newWI = newItem.AddComponent<WorldItem>();
        newWI.itemData = outputItem;

        // Match kinematic/collider state of original
        Rigidbody rb = newItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = GetComponent<Rigidbody>()?.isKinematic ?? true;

        Debug.Log($"[WandTarget] Transformed {gameObject.name} into {outputItem.itemName}");
        Destroy(gameObject);
    }

    private void HandleToggle()
    {
        if (objectToToggle != null)
        {
            objectToToggle.SetActive(!objectToToggle.activeSelf);
            Debug.Log($"[WandTarget] Toggled {objectToToggle.name}");
        }
    }

    private System.Collections.IEnumerator ShowHitEffect()
    {
        hitEffect.SetActive(true);
        yield return new WaitForSeconds(hitEffectDuration);
        if (hitEffect != null) hitEffect.SetActive(false);
    }
}