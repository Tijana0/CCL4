using UnityEngine;
using UnityEngine.UI;
using AK.Wwise;

public class VolumeSlider : MonoBehaviour
{
    public AK.Wwise.RTPC musicVolumeRTPC;  // assign the MusicVolume RTPC in Inspector
    public Slider slider;                   // your UI slider (0 to 100)

    void Start()
    {
        // optional: set slider to current value
        if (slider != null)
            slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        // global RTPC (no specific game object) for master/music volume
        if (musicVolumeRTPC != null)
            musicVolumeRTPC.SetGlobalValue(value);
    }
}