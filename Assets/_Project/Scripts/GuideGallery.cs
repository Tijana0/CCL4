using UnityEngine;
using UnityEngine.UI;

public class GuideGallery : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject guidePanel;      // GuidePanel (starts disabled)
    public RawImage guideImage;        // the RawImage inside the panel
    public Texture[] guideTextures;    // your guide images, in order

    [Header("Hide these while the gallery is open")]
    public GameObject[] hideWhileOpen; // drag Logo, START button, etc. here

    private int index = 0;

    public void OpenGallery()
    {
        index = 0;
        ShowCurrent();
        guidePanel.SetActive(true);
        SetHiddenObjects(false);   // hide logo + start button
    }

    public void CloseGallery()
    {
        guidePanel.SetActive(false);
        SetHiddenObjects(true);    // bring them back
    }

    public void Next()
    {
        if (guideTextures.Length == 0) return;
        index = (index + 1) % guideTextures.Length;
        ShowCurrent();
    }

    public void Prev()
    {
        if (guideTextures.Length == 0) return;
        index = (index - 1 + guideTextures.Length) % guideTextures.Length;
        ShowCurrent();
    }

    private void ShowCurrent()
    {
        if (guideTextures.Length > 0 && guideImage != null)
            guideImage.texture = guideTextures[index];
    }

    private void SetHiddenObjects(bool visible)
    {
        foreach (var go in hideWhileOpen)
            if (go != null) go.SetActive(visible);
    }
}