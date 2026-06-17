using UnityEngine;

/// <summary>
/// Simple AI for Cornish Pixies (or placeholders).
/// Moves procedurally in a slow, wandering pattern using Perlin Noise.
/// </summary>
public class PixieAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float wanderRange = 5f;
    public float heightMin = 1.5f;
    public float heightMax = 3.5f;
    
    [Header("Noise Settings")]
    public float noiseFrequency = 0.5f;
    
    private Vector3 startPosition;
    private float seedX;
    private float seedY;
    private float seedZ;

    private void Start()
    {
        startPosition = transform.position;
        
        // Random seeds for unique patterns per pixie
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
        seedZ = Random.value * 100f;
    }

    private void Update()
    {
        // Calculate noise-based offsets
        float time = Time.time * noiseFrequency;
        
        float offsetX = (Mathf.PerlinNoise(seedX + time, 0f) - 0.5f) * 2f * wanderRange;
        float offsetZ = (Mathf.PerlinNoise(seedZ + time, 0f) - 0.5f) * 2f * wanderRange;
        
        // Height variation
        float lerpY = Mathf.PerlinNoise(seedY + time, 0f);
        float targetY = Mathf.Lerp(heightMin, heightMax, lerpY);

        Vector3 targetPos = new Vector3(startPosition.x + offsetX, targetY, startPosition.z + offsetZ);
        
        // Move towards target position smoothly
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Optional: Look where it's going
        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
        }
    }
}
