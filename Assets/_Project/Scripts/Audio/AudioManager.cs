using UnityEngine;
using AK.Wwise;

/// Central audio API. Lives on a persistent "AudioManager" object.
/// Gameplay scripts call these methods instead of posting Wwise events directly.
/// You build/tune the events; the dev just calls AudioManager.Instance.PlayPickup(obj).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("One-shot action events")]
    public AK.Wwise.Event pickupObject;
    public AK.Wwise.Event pickupHerb;
    public AK.Wwise.Event putDown;
    public AK.Wwise.Event chopNormal;
    public AK.Wwise.Event chopHerb;
    public AK.Wwise.Event dropGlass;
    public AK.Wwise.Event dropLiquid;
    public AK.Wwise.Event dropHeavy;
    public AK.Wwise.Event deliver;
    public AK.Wwise.Event failAction;
    public AK.Wwise.Event wandCast;
    public AK.Wwise.Event cards;
    public AK.Wwise.Event clickUI;
    public AK.Wwise.Event crystalBall;
    public AK.Wwise.Event teapotReady;
    public AK.Wwise.Event burnedItem;
    public AK.Wwise.Event boggart;
    public AK.Wwise.Event waterPour;    // water pouring
    public AK.Wwise.Event inventoryPop; // pop when taken from inventory
    public AK.Wwise.Event throwOut;     // throwing something out
    

    [Header("Scene events")]
    public AK.Wwise.Event musicStart;
    public AK.Wwise.Event musicStop;
    public AK.Wwise.Event levelStart;
    public AK.Wwise.Event timerWarning; // sound when time is about to end

    [Header("Volume control (RTPC)")]
    public AK.Wwise.RTPC musicVolume;   // assign MusicVolume RTPC in Inspector


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // enable if music must persist across scenes
    }

    // Generic helper: post any event on the object that made the sound,
    // so positional sounds emit from the right place.
    public void Play(AK.Wwise.Event ev, GameObject source)
    {
        if (ev != null && ev.IsValid())
            ev.Post(source != null ? source : gameObject);
    }

    // Convenience wrappers the gameplay scripts call:
    public void PlayPickup(GameObject o, bool isHerb) => Play(isHerb ? pickupHerb : pickupObject, o);
    public void PlayPutDown(GameObject o)            => Play(putDown, o);
    public void PlayChop(GameObject o, bool isHerb)  => Play(isHerb ? chopHerb : chopNormal, o);
    public void PlayDeliver(GameObject o)            => Play(deliver, o);
    public void PlayFailAction(GameObject o)         => Play(failAction, o);
    public void PlayWand(GameObject o)               => Play(wandCast, o);
    public void PlayClickUI()                        => Play(clickUI, gameObject);
    public void PlayCrystalBall(GameObject o)        => Play(crystalBall, o);
    public void PlayTeapotReady(GameObject o)        => Play(teapotReady, o);
    public void PlayWaterPour(GameObject o)    => Play(waterPour, o);
    public void PlayInventoryPop(GameObject o) => Play(inventoryPop, o);
    public void PlayThrowOut(GameObject o)     => Play(throwOut, o);
    public void PlayTimerWarning()             => Play(timerWarning, gameObject);
    // Drop with a type, since you have 3 variants:
    public enum DropType { Glass, Liquid, Heavy }
    public void PlayDrop(GameObject o, DropType type)
    {
        switch (type)
        {
            case DropType.Glass:  Play(dropGlass, o);  break;
            case DropType.Liquid: Play(dropLiquid, o); break;
            case DropType.Heavy:  Play(dropHeavy, o);  break;
        }
    }

    public void StartMusic() => Play(musicStart, gameObject);
    public void StopMusic()  => Play(musicStop, gameObject);
    public void PlayLevelStart() => Play(levelStart, gameObject);
    
    // Volume control
    public void SetMusicVolume(float value) => musicVolume?.SetGlobalValue(value);
}