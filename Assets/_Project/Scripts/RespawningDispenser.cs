using UnityEngine;

/// <summary>
/// Keeps a single pickable item sitting at a spawn point. When the item is taken
/// (picked up by a player, i.e. reparented away) or destroyed, a fresh one is
/// spawned after respawnDelay seconds.
///
/// Rules for the Teacup dispenser use case:
///   - Nothing can be PLACED on top of this dispenser by the player —
///     the spawn point is reserved for the auto-spawned item only.
///   - The sitting item CAN be filled (e.g. TeacupFillTarget accepts a pour
///     from a Teapot) while still sitting here.
///   - Once picked up, the item cannot be placed back — it's now a
///     free-standing item in the world and the dispenser spawns a fresh one.
///
/// Alignment notes (Angelina x Hajar codebase):
///   - Pickup detection uses transform.parent check, compatible with
///     PickupObject.Interact() which reparents to player.holdPoint.
///   - Scale correction on spawn matches WorldItem.CreateCombined and
///     StationBase.PlaceItemOnStation scale logic.
///   - Does NOT inherit from StationBase — intentionally separate so it
///     never appears in RoomManager's station whitelist pass.
/// </summary>
public class RespawningDispenser : MonoBehaviour
{
    [Tooltip("Pickable item prefab to dispense (should have PickupObject + WorldItem).")]
    public GameObject itemPrefab;

    [Tooltip("Where the item rests. Defaults to this object's transform if unset.")]
    public Transform spawnPoint;

    [Tooltip("Seconds before a new item appears after the current one is taken.")]
    public float respawnDelay = 2f;

    [Tooltip("Extra vertical offset so the item visually rests on the surface.")]
    public float heightOffset = 0f;

    private GameObject current;
    private float timer;
    private bool counting;

    private void Start()
    {
        if (spawnPoint == null) spawnPoint = transform;

        if (itemPrefab == null)
        {
            Debug.LogWarning($"[RespawningDispenser] '{name}' has no itemPrefab assigned — disabling.", this);
            enabled = false;
            return;
        }

        SpawnNow();
    }

    private void Update()
    {
        if (current == null)
        {
            // Item was taken or destroyed — wait, then respawn
            if (!counting) { counting = true; timer = 0f; }
            timer += Time.deltaTime;
            if (timer >= respawnDelay) SpawnNow();
        }
        else if (current.transform.parent != spawnPoint)
        {
            // Picked up (reparented to player's holdPoint) — treat as taken,
            // start the respawn countdown. The picked-up item is now independent.
            current = null;
        }
    }

    /// <summary>
    /// Called by SimplePlayerController.TryInteract() via the IInteractable
    /// interface search. We intentionally return false here so the player
    /// can NEVER place an item onto this dispenser — the spawn point is
    /// reserved for the auto-spawned item only.
    ///
    /// The spawned item itself (TeacupFillTarget, PickupObject) handles its
    /// own interactions — picking it up and pouring into it both work
    /// directly on the item, not on this dispenser.
    /// </summary>
    public bool CanInteract() => false;

    private void SpawnNow()
    {
        counting = false;
        timer = 0f;

        current = Instantiate(
            itemPrefab,
            spawnPoint.position + Vector3.up * heightOffset,
            spawnPoint.rotation,
            spawnPoint);

        // Preserve the prefab's intended world scale even if spawnPoint is scaled —
        // matches the same correction used in WorldItem.CreateCombined and
        // StationBase.PlaceItemOnStation.
        Vector3 parentScale = spawnPoint.lossyScale;
        Vector3 prefabScale = itemPrefab.transform.localScale;
        current.transform.localScale = new Vector3(
            parentScale.x != 0f ? prefabScale.x / parentScale.x : prefabScale.x,
            parentScale.y != 0f ? prefabScale.y / parentScale.y : prefabScale.y,
            parentScale.z != 0f ? prefabScale.z / parentScale.z : prefabScale.z);
    }
}