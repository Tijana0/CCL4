using UnityEngine;
using UnityEngine.InputSystem;
/// Flat mover for the AUDIO TEST scene only (isometric / fixed camera).
/// Glides the capsule across the floor so you can walk toward sound
/// sources and hear spatialization / attenuation change.
/// Put this on your "TestPlayer" capsule. The AkAudioListener also
/// lives on this capsule (NOT on the fixed camera).

public class SoundTestMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    void Start()
    {

    }

    void Update()
    {
    var kb = Keyboard.current;
    float h = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
    float v = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        // Move on world axes, flat across the floor (isometric: camera is fixed)
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }
}
