using UnityEngine;
using AK.Wwise;

/// Moves the pixie around and plays an occasional spatialized sound.
/// The AkGameObj on this object makes each sound emit from the pixie's
/// current position, so it pans/fades as the pixie flies.
public class PixieMover : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float changeDirectionEvery = 2f;
    public Vector3 areaCenter = Vector3.zero;
    public float areaRadius = 5f;

    [Header("Sound")]
    public AK.Wwise.Event pixieSound;       // play_Pixie (one-shot, NOT looping)
    public float soundEvery = 3f;            // seconds between pixie sounds
    public float soundRandomness = 1f;       // +/- variation so it's not robotic

    Vector3 target;
    float moveTimer;
    float soundTimer;
    float nextSoundTime;

    void Start()
    {
        PickNewTarget();
        ScheduleNextSound();
    }

    void Update()
    {
        // --- movement ---
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        moveTimer += Time.deltaTime;
        if (moveTimer >= changeDirectionEvery || Vector3.Distance(transform.position, target) < 0.2f)
        {
            PickNewTarget();
            moveTimer = 0f;
        }

        // --- occasional sound ---
        soundTimer += Time.deltaTime;
        if (soundTimer >= nextSoundTime)
        {
            if (pixieSound != null && pixieSound.IsValid())
                pixieSound.Post(gameObject);   // posted on the pixie = spatialized
            soundTimer = 0f;
            ScheduleNextSound();
        }
    }

    void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * areaRadius;
        target = areaCenter + new Vector3(r.x, transform.position.y, r.y);
    }

    void ScheduleNextSound()
    {
        nextSoundTime = soundEvery + Random.Range(-soundRandomness, soundRandomness);
    }
}