using UnityEngine;
using AK.Wwise;

public class BackgroundMusic : MonoBehaviour
{
    public AK.Wwise.Event musicEvent;   // assign in Inspector
    public AK.Wwise.Event musicStopEvent; // optional, for clean stop

    void Start()
    {
        if (musicEvent != null && musicEvent.IsValid())
            musicEvent.Post(gameObject);
    }

    void OnDisable()
    {
        // optional: stop music when leaving the scene
        if (musicStopEvent != null && musicStopEvent.IsValid())
            musicStopEvent.Post(gameObject);
    }
}