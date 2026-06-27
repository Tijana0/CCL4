using UnityEngine;

/// <summary>
/// Simple billboard script that makes a GameObject match the rotation of the main camera.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
        else
        {
            mainCamera = Camera.main;
        }
    }
}
