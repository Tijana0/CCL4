using UnityEngine;

/// <summary>
/// Marker component for "the fire" — a simple stand that a PortableCooker
/// item (e.g. the Teapot) can be placed on to enable cooking.
///
/// This is NOT a full station — it has no ingredients, no recipes, no
/// inventory of its own. It is purely a detection point. All cooking
/// logic and state lives on the PortableCooker item itself.
///
/// Setup:
///   - Add this to the fire/brazier stand GameObject.
///   - Add a Box Collider (trigger or solid, either works since detection
///     uses distance check, not physics collision events).
///   - heatPoint: optional child transform marking exactly where an item
///     should visually sit when placed here. Defaults to this transform.
/// </summary>
public class HeatSource : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Where an item visually sits when placed on this heat source. Defaults to this object's own position if left empty.")]
    public Transform heatPoint;

    [Tooltip("How close a PortableCooker must be to count as 'on this fire'.")]
    public float detectionRange = 0.6f;

    public Transform GetHeatPoint() => heatPoint != null ? heatPoint : transform;

    private void Start()
    {
        // Only add a flame if there is not already a ParticleSystem in our children (e.g. braziers already have one)
        if (GetComponentInChildren<ParticleSystem>() != null)
        {
            return;
        }

        // Clone the flame from the scene to have a nice fire animation on the stove!
        GameObject flameTemplate = GameObject.Find("Flame");
        if (flameTemplate == null)
        {
            // Fallback: try finding by type/tag if it is named differently in some scenes
            ParticleSystem existingPs = FindAnyObjectByType<ParticleSystem>();
            if (existingPs != null && existingPs.gameObject.name.Contains("Flame"))
            {
                flameTemplate = existingPs.gameObject;
            }
        }

        if (flameTemplate != null)
        {
            Transform point = GetHeatPoint();
            // Offset it slightly down so it sits nicely inside/on the stove burner
            Vector3 spawnPos = point.position + new Vector3(0f, -0.05f, 0f);
            GameObject stoveFlame = Instantiate(flameTemplate, spawnPos, Quaternion.identity, transform);
            stoveFlame.name = "StoveFlame";

            // Make particles smaller but increase the count/density
            ParticleSystem ps = stoveFlame.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 0.28f; // smaller particles
                main.startSpeedMultiplier = 0.55f; // slightly slower rising
                
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = 1.3f; // more particles (dense flame)
                
                // Also adjust any child particle systems if they exist
                foreach (var childPs in stoveFlame.GetComponentsInChildren<ParticleSystem>())
                {
                    if (childPs == ps) continue;
                    var cMain = childPs.main;
                    cMain.startSizeMultiplier *= 0.28f;
                    cMain.startSpeedMultiplier *= 0.55f;
                    var cEmission = childPs.emission;
                    cEmission.rateOverTimeMultiplier *= 1.3f;
                }
            }
        }
    }
}