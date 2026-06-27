#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class SnapshotExporter : MonoBehaviour
{
    [MenuItem("Tools/Export Tea Snapshots")]
    public static void ExportSnapshots()
    {
        string[] prefabPaths = {
            "Assets/_Project/Prefabs/SceneObjects/Tea 1.prefab",
            "Assets/_Project/Prefabs/SceneObjects/Tea 2.prefab",
            "Assets/_Project/Prefabs/SceneObjects/Tea 3.prefab",
            "Assets/_Project/Prefabs/SceneObjects/Tea_wrong.prefab"
        };

        string[] destPaths = {
            "Assets/_Project/UI/ItemIcons/tea_1.png",
            "Assets/_Project/UI/ItemIcons/tea_2.png",
            "Assets/_Project/UI/ItemIcons/tea_3.png",
            "Assets/_Project/UI/ItemIcons/tea_wrong.png"
        };

        for (int i = 0; i < prefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab == null)
            {
                Debug.LogError($"Could not find prefab at {prefabPaths[i]}");
                continue;
            }

            // Create a temporary parent scene anchor
            GameObject stage = new GameObject("Stage");
            
            // Instantiate the prefab, shifted slightly to the right of the frame (so positive X)
            // The user wants it moved slightly in the right direction (negative on X axis?
            // Wait, in screen space, moving the cup to the right means it is at positive X relative to the camera).
            GameObject inst = Instantiate(prefab, new Vector3(0.04f, 0, 0), Quaternion.identity, stage.transform);
            inst.transform.localRotation = Quaternion.Euler(0, 45, 0); // rotated slightly for a nice angle

            // Create camera
            GameObject camGo = new GameObject("TempCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.transform.SetParent(stage.transform);
            
            // Position camera: high angle looking down into the cup
            // Position: slightly back and high up.
            cam.transform.localPosition = new Vector3(0, 0.28f, -0.2f);
            cam.transform.localRotation = Quaternion.Euler(55, 0, 0); // 55 degrees down
            
            cam.orthographic = false;
            cam.fieldOfView = 40;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0); // Transparent background

            // Create light
            GameObject lightGo = new GameObject("TempLight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.SetParent(stage.transform);
            light.transform.localRotation = Quaternion.Euler(50, -30, 0);
            light.intensity = 1.3f;
            light.color = Color.white;

            // Render to texture
            int res = 512;
            RenderTexture rt = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            
            // Force rendering
            cam.Render();

            // Read pixels
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(res, res, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, res, res), 0, 0);
            tex.Apply();

            // Clean up texture reference
            cam.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(rt);

            // Save to file
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(destPaths[i], bytes);
            DestroyImmediate(tex);

            // Clean up stage
            DestroyImmediate(stage);

            Debug.Log($"Exported snapshot for {prefab.name} to {destPaths[i]}");
        }

        AssetDatabase.Refresh();
        Debug.Log("All tea snapshots exported successfully!");
    }
}
#endif
