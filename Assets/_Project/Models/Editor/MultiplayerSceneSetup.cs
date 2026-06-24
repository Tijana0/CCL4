using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MultiplayerSceneSetup
{
    [MenuItem("Tools/Setup Multiplayer Scene")]
    public static void SetupScene()
    {
        // Create a new empty scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Add a Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera camera = cameraObj.AddComponent<Camera>();
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0, 10, -10);
        cameraObj.transform.rotation = Quaternion.Euler(45, 0, 0);

        // Add a Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Add a Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5, 1, 5);
        
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null && groundRenderer.sharedMaterial != null)
        {
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (groundMat.shader == null) groundMat = new Material(Shader.Find("Standard")); // Fallback
            groundMat.color = new Color(0.2f, 0.2f, 0.2f);
            groundRenderer.sharedMaterial = groundMat;
        }

        // Add Player 1
        GameObject player1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player1.name = "Player 1 (WASD/Gamepad 1)";
        player1.transform.position = new Vector3(-2, 1, 0);
        Renderer p1Renderer = player1.GetComponent<Renderer>();
        if (p1Renderer != null && p1Renderer.sharedMaterial != null)
        {
            Material p1Mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (p1Mat.shader == null) p1Mat = new Material(Shader.Find("Standard")); // Fallback
            p1Mat.color = Color.blue;
            p1Renderer.sharedMaterial = p1Mat;
        }
        var p1Controller = player1.AddComponent<SimplePlayerController>();
        p1Controller.playerIndex = 0;

        // Add Player 2
        GameObject player2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player2.name = "Player 2 (Arrows/Gamepad 2)";
        player2.transform.position = new Vector3(2, 1, 0);
        Renderer p2Renderer = player2.GetComponent<Renderer>();
        if (p2Renderer != null && p2Renderer.sharedMaterial != null)
        {
            Material p2Mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (p2Mat.shader == null) p2Mat = new Material(Shader.Find("Standard")); // Fallback
            p2Mat.color = Color.red;
            p2Renderer.sharedMaterial = p2Mat;
        }
        var p2Controller = player2.AddComponent<SimplePlayerController>();
        p2Controller.playerIndex = 1;

        // Add a Pickup Object
        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = "Pickup Item";
        pickup.transform.position = new Vector3(0, 0.5f, 2);
        pickup.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Renderer pickupRenderer = pickup.GetComponent<Renderer>();
        if (pickupRenderer != null && pickupRenderer.sharedMaterial != null)
        {
            Material pickupMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (pickupMat.shader == null) pickupMat = new Material(Shader.Find("Standard")); // Fallback
            pickupMat.color = Color.green;
            pickupRenderer.sharedMaterial = pickupMat;
        }
        pickup.AddComponent<Rigidbody>();
        pickup.AddComponent<PickupObject>();

        // Add Pickup Objects
        CreatePickup("Gold Coin", new Vector3(-3, 0.5f, 3), Color.yellow, ItemType.Gold, 10, PrimitiveType.Cylinder);
        CreatePickup("Energy Crystal", new Vector3(3, 0.5f, 3), Color.cyan, ItemType.Energy, 5, PrimitiveType.Cube);
        CreatePickup("Secret Star", new Vector3(0, 0.5f, -3), Color.magenta, ItemType.Secret, 50, PrimitiveType.Sphere);

        // Ensure directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // Save the scene
        string scenePath = "Assets/Scenes/SimpleMultiplayer.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        
        Debug.Log("Multiplayer Scene created and saved at: " + scenePath);
        
        // Ping the scene in project window
        Object sceneAsset = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
        if (sceneAsset != null)
        {
            EditorGUIUtility.PingObject(sceneAsset);
        }
    }

    private static void CreatePickup(string name, Vector3 position, Color color, ItemType type, int value, PrimitiveType primitive)
    {
        GameObject item = GameObject.CreatePrimitive(primitive);
        item.name = name;
        item.transform.position = position;
        item.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader == null) mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
        
        item.AddComponent<Rigidbody>();
        var pickup = item.AddComponent<PickupObject>();
        pickup.itemType = type;
        pickup.value = value;
    }
}