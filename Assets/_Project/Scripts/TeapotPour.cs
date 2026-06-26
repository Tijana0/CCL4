using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pours brewed tea from a finished Teapot (a <see cref="PortableCooker"/>) into a Teacup
/// (anything with a <see cref="TeacupFillTarget"/>). Put this on BOTH the Teapot and the
/// Teacup prefabs — it figures out the role from the components on its own GameObject.
///
/// While the item is held, pressing the Pour button (Q / Numpad0 / gamepad Left Trigger):
///   - Holding the TEAPOT (and it has finished brewing): fills a nearby teacup with the
///     brewed tea (Tea 1/2/3) and empties the teapot.
///   - Holding a TEACUP: pulls the brew out of a nearby finished teapot into this cup
///     (the cup becomes the tea) and empties the teapot.
/// Either way the tea flows pot -> cup, so you can carry whichever one to the other.
/// </summary>
public class TeapotPour : MonoBehaviour
{
    [Tooltip("How close the teapot and teacup must be to pour.")]
    public float pourRange = 1.6f;

    private SimplePlayerController owner;

    private void Update()
    {
        if (owner == null || owner.heldItem != gameObject)
        {
            owner = FindOwner();
            if (owner == null) return;
        }

        if (!PourPressed(owner)) return;

        // Holding the teapot -> pour its brew into a nearby cup.
        PortableCooker heldPot = GetComponent<PortableCooker>();
        if (heldPot != null)
        {
            if (!heldPot.IsDone) { Debug.Log("[TeapotPour] Nothing brewed to pour yet."); return; }
            GameObject cup = FindNearestCup(transform.position);
            if (cup != null) Pour(heldPot, cup, cupHeld: false);
            else Debug.Log("[TeapotPour] No teacup nearby to pour into.");
            return;
        }

        // Holding a cup -> pull the brew from a nearby finished teapot into this cup.
        if (GetComponent<TeacupFillTarget>() != null)
        {
            PortableCooker pot = FindNearestDonePot(transform.position);
            if (pot != null) Pour(pot, gameObject, cupHeld: true);
            else Debug.Log("[TeapotPour] No brewed teapot nearby to pour from.");
        }
    }

    private void Pour(PortableCooker pot, GameObject cup, bool cupHeld)
    {
        ItemData tea = pot.ConsumeResult(); // takes the brewed tea AND empties the teapot
        if (tea == null || tea.prefab == null)
        {
            Debug.LogWarning("[TeapotPour] Teapot had no brewed result to pour.");
            return;
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayWaterPour(gameObject);

        if (cupHeld)
        {
            // The cup is in the pourer's hands -> it becomes the tea, in hand.
            GameObject teaGO = Instantiate(tea.prefab, owner.holdPoint.position, Quaternion.identity, owner.holdPoint);
            SetupResult(teaGO, tea, held: true);
            teaGO.transform.localPosition = Vector3.zero;
            teaGO.transform.localRotation = Quaternion.identity;
            owner.heldItem = teaGO;
            Destroy(cup);
        }
        else
        {
            // The cup is resting in the world -> it becomes the tea where it sits.
            GameObject teaGO = Instantiate(tea.prefab, cup.transform.position, cup.transform.rotation, cup.transform.parent);
            SetupResult(teaGO, tea, held: false);
            StationBase st = cup.GetComponentInParent<StationBase>();
            if (st != null && st.itemOnStation == cup) st.itemOnStation = teaGO;
            Destroy(cup);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup(gameObject, false);
        Debug.Log($"[TeapotPour] Poured {tea.itemName} into the cup.");
    }

    private void SetupResult(GameObject go, ItemData data, bool held)
    {
        WorldItem wi = go.GetComponent<WorldItem>();
        if (wi == null) wi = go.AddComponent<WorldItem>();
        wi.itemData = data;

        Rigidbody rb = go.GetComponentInChildren<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        // Held items keep colliders off (so the overlap check ignores them); a cup left
        // sitting becomes a normal pickable, so its colliders stay on.
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) c.enabled = !held;
    }

    private GameObject FindNearestCup(Vector3 from)
    {
        TeacupFillTarget best = null; float bestD = float.MaxValue;
        foreach (var t in FindObjectsByType<TeacupFillTarget>(FindObjectsSortMode.None))
        {
            if (t.gameObject == gameObject) continue;
            float d = Vector3.Distance(t.transform.position, from);
            if (d <= pourRange && d < bestD) { best = t; bestD = d; }
        }
        return best != null ? best.gameObject : null;
    }

    private PortableCooker FindNearestDonePot(Vector3 from)
    {
        PortableCooker best = null; float bestD = float.MaxValue;
        foreach (var p in FindObjectsByType<PortableCooker>(FindObjectsSortMode.None))
        {
            if (p.gameObject == gameObject || !p.IsDone) continue;
            float d = Vector3.Distance(p.transform.position, from);
            if (d <= pourRange && d < bestD) { best = p; bestD = d; }
        }
        return best;
    }

    private SimplePlayerController FindOwner()
    {
        foreach (var p in FindObjectsByType<SimplePlayerController>(FindObjectsSortMode.None))
            if (p.heldItem == gameObject) return p;
        return null;
        
    }

    private bool PourPressed(SimplePlayerController player)
    {
        if (player.playerIndex == 0 && Keyboard.current != null)
            return Keyboard.current.qKey.wasPressedThisFrame;
        if (player.playerIndex == 1 && Keyboard.current != null)
            return Keyboard.current.numpad0Key.wasPressedThisFrame;
        if (player.playerIndex == SimplePlayerController.activeGamepadPlayerIndex && Gamepad.current != null)
            return Gamepad.current.leftTrigger.wasPressedThisFrame;
        return false;
    }
}
