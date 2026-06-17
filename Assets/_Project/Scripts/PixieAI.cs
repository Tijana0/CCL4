using UnityEngine;

/// <summary>
/// Simple AI for Cornish Pixies (or placeholders).
/// Moves procedurally in a slow, wandering pattern using Perlin Noise.
/// </summary>
public class PixieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float heightMin = 1.5f;
    public float heightMax = 4.0f;
    
    [Header("Wander Area")]
    public bool useRoomBounds = true;
    public Vector3 roomCenter = Vector3.zero;
    public Vector2 roomSize = new Vector2(15f, 10f);
    
    [Header("Noise Settings")]
    public float noiseFrequency = 0.3f;
    
    private float seedX;
    private float seedY;
    private float seedZ;

    private void Start()
    {
        // Random seeds for unique patterns per pixie
        seedX = Random.value * 1000f;
        seedY = Random.value * 1000f;
        seedZ = Random.value * 1000f;

        // Auto-detect room center if not set
        if (useRoomBounds)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                roomCenter = ground.transform.position;
                
                // Get ground size from mesh if possible
                var mf = ground.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 worldSize = Vector3.Scale(mf.sharedMesh.bounds.size, ground.transform.lossyScale);
                    roomSize = new Vector2(worldSize.x, worldSize.z);
                }
            }
        }
    }

    private void Update()
    {
        // Calculate noise-based coordinates [0, 1]
        float time = Time.time * noiseFrequency;
        
        float nx = Mathf.PerlinNoise(seedX + time, 0f);
        float nz = Mathf.PerlinNoise(seedZ + time, 0f);
        float ny = Mathf.PerlinNoise(seedY + time, 0f);
        
        // Map noise to room dimensions
        float worldX = roomCenter.x + (nx - 0.5f) * roomSize.x;
        float worldZ = roomCenter.z + (nz - 0.5f) * roomSize.y;
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
}
