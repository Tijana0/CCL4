using UnityEngine;

/// <summary>
/// Simple AI for Cornish Pixies (or placeholders).
/// Moves procedurally in a slow, wandering pattern using Perlin Noise.
/// </summary>
public class PixieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float heightMin = 0.5f; // Lowered to hit players
    public float heightMax = 2.5f; 
    
    [Header("Interaction Settings")]
    public float freezeDuration = 3f;

    [Header("Wander Area")]
    public bool useRoomBounds = true;
    [Tooltip("Optional: assign a Transform here to use as the center of the wander box.")]
    public Transform roomAnchor;
    [Tooltip("Used if no anchor is assigned. If left at (0,0,0) and no Ground exists, this will default to the pixie's start position.")]
    public Vector3 roomCenter = Vector3.zero;
    [Tooltip("X = width, Y = depth (z)")]
    public Vector2 roomSize = new Vector2(15f, 10f);

    [Header("Noise Settings")]
    public float noiseFrequency = 0.3f;
    
    private float seedX;
    private float seedY;
    private float seedZ;

    private void Start()
    {
        // Ensure there is a trigger collider for player detection
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.25f; // Slightly larger for easier "touching"
        }
        else
        {
            col.isTrigger = true;
        }

        // Random seeds for unique patterns per pixie
        seedX = Random.value * 1000f;
        seedY = Random.value * 1000f;
        seedZ = Random.value * 1000f;

        // Auto-detect room center/size only if the user did not set them in the Inspector
        if (useRoomBounds && roomAnchor == null)
        {
            bool hadRoomCenterInInspector = roomCenter != Vector3.zero;
            bool hadRoomSizeInInspector = roomSize != Vector2.zero;

            GameObject ground = GameObject.Find("Ground");
            if (ground != null && (!hadRoomCenterInInspector || !hadRoomSizeInInspector))
            {
                // Only overwrite inspector values that are unset (zero)
                if (!hadRoomCenterInInspector)
                    roomCenter = ground.transform.position;
                
                var mf = ground.GetComponent<MeshFilter>();
                if (!hadRoomSizeInInspector && mf != null && mf.sharedMesh != null)
                {
                    Vector3 worldSize = Vector3.Scale(mf.sharedMesh.bounds.size, ground.transform.lossyScale);
                    roomSize = new Vector2(worldSize.x, worldSize.z);
                }
            }

            // If still unset (no Ground and inspector left zeros), default center to this object's start pos
            if (roomCenter == Vector3.zero)
                roomCenter = transform.position;
        }
    }

    private void Update()
    {
        // Calculate noise-based coordinates [0, 1]
        float time = Time.time * noiseFrequency;
        
        float nx = Mathf.PerlinNoise(seedX + time, 0f);
        float nz = Mathf.PerlinNoise(seedZ + time, 0f);
        float ny = Mathf.PerlinNoise(seedY + time, 0f);
        
        // Choose center: prefer runtime anchor if assigned
        Vector3 center = roomAnchor != null ? roomAnchor.position : roomCenter;

        // Map noise to room dimensions
        float worldX = center.x + (nx - 0.5f) * roomSize.x;
        float worldZ = center.z + (nz - 0.5f) * roomSize.y;
        float worldY = Mathf.Lerp(heightMin, heightMax, ny);

        Vector3 targetPos = new Vector3(worldX, worldY, worldZ);
        
        // Move towards target position smoothly
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * moveSpeed);

        // Look where it's going (smoothly)
        Vector3 movement = targetPos - transform.position;
        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * moveSpeed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a player
        SimplePlayerController player = other.GetComponent<SimplePlayerController>();
        if (player != null && !player.isFrozen)
        {
            player.Freeze(freezeDuration);
        }
    }

    // Draw the wander box in the editor when the object is selected
    private void OnDrawGizmosSelected() 
    {
        if (!useRoomBounds) return;

        Vector3 center = roomCenter;
        if (roomAnchor != null)
            center = roomAnchor.position;
        else if (center == Vector3.zero && Application.isPlaying)
            center = transform.position;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Vector3 size = new Vector3(roomSize.x, Mathf.Abs(heightMax - heightMin), roomSize.y);
        // Draw semi-transparent cube and wireframe
        Gizmos.DrawCube(center + Vector3.up * ((heightMin + heightMax) * 0.5f), size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireCube(center + Vector3.up * ((heightMin + heightMax) * 0.5f), size);
    }
}
