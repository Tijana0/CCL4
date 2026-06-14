using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonSelectionGlow : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public GameObject glowEffect;
    public TextMeshProUGUI buttonText;
    
    private Color originalColor = Color.white;

    private void Awake()
    {
        if (glowEffect != null) glowEffect.SetActive(false);
        if (buttonText != null) originalColor = buttonText.color;
    }

    private void OnEnable()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            ApplySelectionVisuals(true);
        }
        else
        {
            ApplySelectionVisuals(false);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        ApplySelectionVisuals(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ApplySelectionVisuals(false);
    }

    private void ApplySelectionVisuals(bool selected)
    {
        if (glowEffect != null) glowEffect.SetActive(selected);
        
        if (buttonText != null)
        {
            buttonText.color = selected ? Color.black : originalColor;
        }
    }
}
