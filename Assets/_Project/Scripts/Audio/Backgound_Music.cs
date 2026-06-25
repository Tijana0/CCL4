using UnityEngine;
using AK.Wwise;

public class BackgroundMusic : MonoBehaviour
{
    public AK.Wwise.Event musicEvent;      // assign this scene's start event
    public AK.Wwise.Event musicStopEvent;  // assign this scene's stop event

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(musicEvent, musicStopEvent);
    }
}