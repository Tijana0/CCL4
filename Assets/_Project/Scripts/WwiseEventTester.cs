using UnityEngine;
using UnityEngine.InputSystem; // new Input System
using AK.Wwise; // requires the Wwise integration to be in the project

/// AUDIO TEST scene helper.
/// Fire one-shot Wwise events with number keys so you can audition
/// your "action" sounds (pick up, combine, deliver, fail...) without
/// needing the real gameplay scripts yet.
///
/// HOW TO USE:
///  1. Put this on an empty GameObject called "SoundTester" in your test scene.
///  2. In the Inspector, drag the matching Wwise Event into each slot.
///  3. Press Play, then press the number keys to hear each sound.
public class WwiseEventTester : MonoBehaviour
{
    [Header("Assign Wwise Events (1-8 keys)")]
    public AK.Wwise.Event pickUp;     // key 1
    public AK.Wwise.Event putDown;    // key 2
    public AK.Wwise.Event chop;       // key 3
    public AK.Wwise.Event combine;    // key 4
    public AK.Wwise.Event deliver;    // key 5  (order complete "ding")
    public AK.Wwise.Event fail;       // key 6
    public AK.Wwise.Event cardFlip;   // key 7
    public AK.Wwise.Event glassClink; // key 8

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return; // no keyboard connected

        if (kb.digit1Key.wasPressedThisFrame) Post(pickUp,     "Pick Up");
        if (kb.digit2Key.wasPressedThisFrame) Post(putDown,    "Put Down");
        if (kb.digit3Key.wasPressedThisFrame) Post(chop,       "Chop");
        if (kb.digit4Key.wasPressedThisFrame) Post(combine,    "Combine");
        if (kb.digit5Key.wasPressedThisFrame) Post(deliver,    "Deliver");
        if (kb.digit6Key.wasPressedThisFrame) Post(fail,       "Fail");
        if (kb.digit7Key.wasPressedThisFrame) Post(cardFlip,   "Card Flip");
        if (kb.digit8Key.wasPressedThisFrame) Post(glassClink, "Glass Clink");
    }

    void Post(AK.Wwise.Event ev, string label)
    {
        if (ev != null && ev.IsValid())
        {
            ev.Post(gameObject);
            Debug.Log($"[WwiseEventTester] Played: {label}");
        }
        else
        {
            Debug.LogWarning($"[WwiseEventTester] No event assigned for: {label}");
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 200),
            "SOUND TESTER\n" +
            "1 = Pick Up\n2 = Put Down\n3 = Chop\n4 = Combine\n" +
            "5 = Deliver (ding)\n6 = Fail\n7 = Card Flip\n8 = Glass Clink\n" +
            "Walk with WASD.");
    }
}