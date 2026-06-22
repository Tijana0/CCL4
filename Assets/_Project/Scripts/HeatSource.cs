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
}