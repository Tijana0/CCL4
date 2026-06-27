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

            Color yellowColor = new Color(1.0f, 0.85f, 0.0f, 1.0f); // Yellow

            // Adjust all particle systems in the stove flame
            foreach (var ps in stoveFlame.GetComponentsInChildren<ParticleSystem>(true))
            {
                // Instantiate the material and clear its hardcoded orange/red color to white,
                // so it can be tinted perfectly by the particle system's startColor.
                var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    Material instMat = psRenderer.material;
                    if (instMat != null)
                    {
                        instMat.color = Color.white;
                        if (instMat.HasProperty("_BaseColor"))
                        {
                            instMat.SetColor("_BaseColor", Color.white);
                        }
                    }
                }

                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(yellowColor);

                // Override Color over Lifetime to prevent the template's red/orange gradient from overriding our color
                var colorOverLifetime = ps.colorOverLifetime;
                if (colorOverLifetime.enabled)
                {
                    Gradient grad = new Gradient();
                    grad.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                    );
                    colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
                }

                var colorBySpeed = ps.colorBySpeed;
                colorBySpeed.enabled = false;

                // Make particles smaller but increase the count/density
                main.startSizeMultiplier *= 0.28f;
                main.startSpeedMultiplier *= 0.55f;
                
                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= 1.3f;
            }

            // If there are lights in the cloned flame, change their color too!
            foreach (var light in stoveFlame.GetComponentsInChildren<Light>(true))
            {
                light.color = yellowColor;
            }
        }
    }
}