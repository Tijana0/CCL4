using UnityEngine;
using UnityEngine.UI;
using AK.Wwise;

public class VolumeSlider : MonoBehaviour
{
    public AK.Wwise.RTPC volumeRTPC;  // assign Master, Music, OR SFX RTPC per instance
    public Slider slider;

    void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);
            OnSliderChanged(slider.value);  // push initial value to the RTPC
        }
    }

    void OnSliderChanged(float value)
    {
        if (volumeRTPC != null)
            volumeRTPC.SetGlobalValue(value);
    }
}