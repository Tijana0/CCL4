using UnityEngine;
using UnityEngine.InputSystem;

public class RawInputDebugger : MonoBehaviour
{
    private void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is Gamepad || device is Joystick)
            {
                foreach (var control in device.allControls)
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button)
                    {
                        if (button.wasPressedThisFrame)
                        {
                            Debug.Log($"<color=magenta>RAW INPUT DETECTED -> Device: {device.name} | Control: {control.name} | Path: {control.path}</color>");
                        }
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        GUI.color = Color.magenta;
        GUI.Label(new Rect(10, 10, 800, 30), "Raw Input Debugger Active: Press A on Gamepad and check Console!");
    }
}
