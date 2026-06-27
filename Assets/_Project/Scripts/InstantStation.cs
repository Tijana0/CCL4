using UnityEngine;

/// <summary>
/// Handles Bin, Sink, HandIn stations.
/// Bin and Sink bypass ALL whitelists — StationBase.Interact() handles this.
/// HandIn goes through normal whitelist flow.
/// </summary>
public class InstantStation : StationBase
{
    [Header("Hand-In Settings")]
    public OrderManager orderManager;

    private Transform portalTransform;
    private Vector3 originalPortalScale;
    private Coroutine portalAnimationCoroutine;

    private void Start()
    {
        if (orderManager == null)
            orderManager = FindFirstObjectByType<OrderManager>();

        FindPortal();
    }

    private void FindPortal()
    {
        // Search children
        foreach (Transform child in transform)
        {
            if (child.name.Contains("Portal"))
            {
                portalTransform = child;
                originalPortalScale = portalTransform.localScale;
                return;
            }
        }
        // Search parent's children (siblings)
        if (transform.parent != null)
        {
            foreach (Transform sibling in transform.parent)
            {
                if (sibling.name.Contains("Portal"))
                {
                    portalTransform = sibling;
                    originalPortalScale = portalTransform.localScale;
                    return;
                }
            }
        }
    }

    public override void Interact(SimplePlayerController player)
    {
        if (player.heldItem == null) return;

        // For Bin and Sink, act immediately — no whitelist, no placement
        if (stationType == StationType.Bin)
        {
            HandleBin(player);
            return;
        }
        if (stationType == StationType.Sink)
        {
            HandleSink(player);
            return;
        }

        // HandIn goes through normal StationBase flow (whitelist check applies)
        base.Interact(player);
    }

    protected override void OnItemPlaced(GameObject item)
    {
        // Called after HandIn whitelist passes — score and destroy
        if (stationType == StationType.HandIn)
        {
            WorldItem wi = item.GetComponent<WorldItem>();
            if (wi == null) return;

            int score = 0;
            if (orderManager != null)
                score = orderManager.TryCompleteOrder(wi.itemData);

            if (score > 0 && GameManager.Instance != null)
                GameManager.Instance.AddScore(score);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayDeliver(this.gameObject);

            // Trigger portal animation
            TriggerPortalDeliveryAnimation();

            Destroy(item);
            itemOnStation = null;
        }
    }

    public void TriggerPortalDeliveryAnimation()
    {
        if (portalTransform == null) return;
        if (portalAnimationCoroutine != null) StopCoroutine(portalAnimationCoroutine);
        portalAnimationCoroutine = StartCoroutine(AnimatePortalShrinkExpand());
    }

    private System.Collections.IEnumerator AnimatePortalShrinkExpand()
    {
        float duration = 0.35f;
        float elapsed = 0f;

        // Shrink phase
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smoothly interpolate from original scale to 0 (t*t for ease-in)
            portalTransform.localScale = Vector3.Lerp(originalPortalScale, Vector3.zero, t * t);
            yield return null;
        }

        portalTransform.localScale = Vector3.zero;
        yield return new WaitForSeconds(0.15f);

        // Expand phase
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smoothly interpolate from 0 to original scale (1-(1-t)^2 for ease-out)
            portalTransform.localScale = Vector3.Lerp(Vector3.zero, originalPortalScale, 1f - (1f - t) * (1f - t));
            yield return null;
        }

        portalTransform.localScale = originalPortalScale;
        portalAnimationCoroutine = null;
    }

    private void HandleBin(SimplePlayerController player)
    {
        // Special case: PortableCooker items (e.g. Teapot) are NEVER destroyed
        // at the Bin — their contents are cleared instead, keeping the item reusable.
        PortableCooker cooker = player.heldItem.GetComponent<PortableCooker>();
        if (cooker != null)
        {
            cooker.ClearContents();
            Debug.Log("[Bin] Cleared contents — item kept in hand, now empty.");
            return;
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayThrowOut(this.gameObject);

        WorldItem wi = player.heldItem.GetComponent<WorldItem>();

        if (wi != null && !wi.itemData.isDisposable)
        {
            Debug.Log($"[Bin] {wi.itemData.itemName} cannot be thrown away — not disposable.");
            return;
        }

        Debug.Log($"[Bin] Destroyed: {player.heldItem.name}");
        Destroy(player.heldItem);
        player.heldItem = null;
    }

    private void HandleSink(SimplePlayerController player)
    {
        WorldItem wi = player.heldItem.GetComponent<WorldItem>();
        Debug.Log($"[Sink] Cleared: {(wi != null ? wi.itemData.itemName : player.heldItem.name)}");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWaterPour(this.gameObject);
        Destroy(player.heldItem);
        player.heldItem = null;
    }
}