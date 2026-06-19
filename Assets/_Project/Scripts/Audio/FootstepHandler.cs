using UnityEngine;
using AK.Wwise;

/// Footstep audio driven by floor TAGS.
/// Put this on the player. Tag your floor colliders "Carpet", "Stone", "Wood"
/// in Unity, and name your Wwise switch values exactly the same.
public class FootstepHandler : MonoBehaviour
{
    [Header("Wwise")]
    public AK.Wwise.Event footstepEvent;          // play_Footsteps
    public string surfaceSwitchGroup = "SurfaceType"; // your Wwise Switch Group name

    [Header("Step timing")]
    public float stepInterval = 0.45f;   // seconds between steps while walking
    public float minMoveSpeed = 0.1f;    // ignore tiny movements

    [Header("Floor detection")]
    public string[] validSurfaces = { "Carpet", "Stone", "Wood" };
    public string defaultSurface = "Stone";
    public float raycastDistance = 2f;
    public LayerMask floorLayer = ~0;    // default: everything
    bool wasMoving = false;


    Vector3 lastPosition;
    float stepTimer;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        bool isMoving = speed >= minMoveSpeed;

        if (isMoving)
        {
            // Just started moving? Play one step instantly (kills the startup delay)
            if (!wasMoving)
            {
                PlayFootstep();
                stepTimer = 0f;
            }
            else
            {
                stepTimer += Time.deltaTime;
                if (stepTimer >= stepInterval)
                {
                    stepTimer = 0f;
                    PlayFootstep();
                }
            }
        }
        else
        {
            stepTimer = stepInterval; // so next move starts a step promptly, but no mid-stop reset spam
        }

        wasMoving = isMoving;
    }

    void PlayFootstep()
    {
        string surface = GetSurfaceTag();

        AkUnitySoundEngine.SetSwitch(surfaceSwitchGroup, surface, gameObject);

        if (footstepEvent != null && footstepEvent.IsValid())
            footstepEvent.Post(gameObject);

        Debug.Log("Footstep on: " + surface);
    }

    string GetSurfaceTag()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                            raycastDistance, floorLayer))
        {
            foreach (string s in validSurfaces)
                if (hit.collider.tag == s) return hit.collider.tag;
        }
        return defaultSurface;
    }
}