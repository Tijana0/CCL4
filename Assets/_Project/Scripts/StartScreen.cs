using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartScreen : MonoBehaviour
{
    private Outline buttonOutline;

    private void Start()
    {
        Button btn = GetComponentInChildren<Button>();
        if (btn == null) btn = Object.FindFirstObjectByType<Button>();
        if (btn != null)
        {
            // Set up outline component
            buttonOutline = btn.gameObject.GetComponent<Outline>();
            if (buttonOutline == null)
            {
                buttonOutline = btn.gameObject.AddComponent<Outline>();
            }
            buttonOutline.effectColor = Color.white;
            buttonOutline.effectDistance = new Vector2(6f, 6f); // Thick outline
            buttonOutline.enabled = false; // Start disabled

            // Add an EventTrigger to toggle the outline on selection/deselection
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry selectEntry = new EventTrigger.Entry();
            selectEntry.eventID = EventTriggerType.Select;
            selectEntry.callback.AddListener((data) => { SetOutlineActive(true); });
            trigger.triggers.Add(selectEntry);

            EventTrigger.Entry deselectEntry = new EventTrigger.Entry();
            deselectEntry.eventID = EventTriggerType.Deselect;
            deselectEntry.callback.AddListener((data) => { SetOutlineActive(false); });
            trigger.triggers.Add(deselectEntry);

            btn.Select();
            
            // If already selected by default, enable the outline
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == btn.gameObject)
            {
                SetOutlineActive(true);
            }
        }
    }

    private void SetOutlineActive(bool active)
    {
        if (buttonOutline != null)
        {
            buttonOutline.enabled = active;
        }
    }

    public void OnStartPressed()
    {
        AkUnitySoundEngine.PostEvent("Play_Level_Start", gameObject);
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(2);   // 2 = HubScene
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }
}