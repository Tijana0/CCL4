using UnityEngine;

/// <summary>
/// Keeps a single pickable item sitting at a spawn point. When the item is taken
/// (picked up by a player, i.e. reparented away) or destroyed, a fresh one is
/// spawned after <see cref="respawnDelay"/> seconds. Designed to sit alongside a
/// cooking/combine station so the station keeps its own behaviour.
/// </summary>
public class RespawningDispenser : MonoBehaviour
{
    [Tooltip("Pickable item prefab to dispense (should have a PickupObject).")]
    public GameObject itemPrefab;

    [Tooltip("Where the item rests. If unset, this object's transform is used.")]
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
        SpawnNow();
    }

    private void Update()
    {
        if (current == null)
        {
            // Item was taken or destroyed — wait, then respawn.
            if (!counting) { counting = true; timer = 0f; }
            timer += Time.deltaTime;
            if (timer >= respawnDelay) SpawnNow();
        }
        else if (current.transform.parent != spawnPoint)
        {
            // Picked up (reparented to the player's hold point) — treat as taken.
            current = null;
        }
    }

    private void SpawnNow()
    {
        counting = false;
        timer = 0f;
        current = Instantiate(
            itemPrefab,
            spawnPoint.position + Vector3.up * heightOffset,
            spawnPoint.rotation,
            spawnPoint);
    }
}
